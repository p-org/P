/-
PLean.Verify.Obligation — synthesise per-handler Hoare-triple
obligations from registry data.

For each `Proof { prove X using Y, …; }` directive in the registry,
walk every (machine, state, handler) triple and emit one Hoare-triple
theorem per (handler, target lemma) pair: `triple <pre> <handler>
<post>`. The precondition conjoins target + using-lemmas + the three
default invariants + the dispatcher contract; the post drops the
using-lemmas. `prove default;` swaps the bundle for
`DefaultInvariants` and the discharger for `pverify_default`.

`InitConditions` does NOT appear in any per-handler triple — it would
be unsound to assume mid-trace, and per-handler obligations are
inductive-step checks (`Inv ∧ Step ⇒ Inv`). The initiation leg
(`InitConditions ⇒ Inv`) is discharged by `emitBaseCaseObligation`:
one VC per (directive, individual-invariant-in-target-lemma) pair, so
a failed base case names exactly which invariant doesn't follow from
init. Premises don't get a base-case VC from a directive that uses
them — only goals do — matching PVerifier's behaviour where only
`cmd.Goals` get a UCLID `invariant` declaration (whose base case
UCLID checks automatically).

`synthesise` consults the `pverifyProofExt` registry first: a theorem
tagged `@[pverifyProof]` under the obligation's name skips auto
emission. Otherwise the emitted theorem ends in `by first | <chain> |
sorry`; a `sorry` in the elaborated value flags a failed obligation.
After user directives, a synthetic `block_auto_default` pass emits a
default obligation for every (M, S, ev) not already covered.
-/
import Lean
import PLean.Internal.Decls
import PLean.Internal.Registry
import PLean.Surface.Machine
import PLean.Verify.Tactic
import PLean.Verify.ProofRegistry
import PLean.Verify.CexModel

open Lean Elab Command

namespace PLean
namespace Verify

/-! ## Helpers -/

private def idSig : Ident := mkIdent `Sig

-- Unhygienic binders for the emitted obligation theorem. Without
-- these, the bare `this` / `param` inside the macro-quotation acquire
-- macro scopes and the rendered signature carries ✝ marks that break
-- the copy-paste manual-proof skeleton. Mirrors the same convention
-- used in `Surface/Stmt.lean` and `Commands/GenModule.lean`.
private def idThis  : Ident := mkIdent `this
private def idParam : Ident := mkIdent `param

/-- Resolve a `prove` target name (lemma) to its corresponding bundle
predicate. `default` resolves to `PLean.DefaultInvariants` (a closed
`PProp Sig` predicate, applied via partial application). User lemmas
resolve to the locally-emitted `<Mod>.<X>` def. -/
private def lemmaPredIdent (target : Name) (isDefault : Bool) :
    MacroM (TSyntax `term) := do
  if isDefault then
    `(PLean.DefaultInvariants)
  else
    let id := mkIdent target
    `($id)

/-- Resolve `using` lemma names to their bundle predicates. -/
private def usingPredIdents (usingNames : Array Name) :
    MacroM (Array (TSyntax `term)) := do
  usingNames.mapM fun n => `($(mkIdent n))

/-- Build the conjunction `(p1) s ∧ (p2) s ∧ ... ∧ True`. -/
private def buildConjAt (preds : Array (TSyntax `term))
    (sIdent : TSyntax `term) :
    MacroM (TSyntax `term) := do
  let mut body : TSyntax `term ← `(True)
  for p in preds.reverse do
    body ← `(($p) $sIdent ∧ $body)
  return body

/-! ## Theorem-name builder

The obligation generator and the `@[pverifyProof]` lookup must agree
on the emitted theorem name shape. -/

/-- Build the name `<M>.<S>.<ev>_correct_<proofTag>_<target>[_using_<L1>_<L2>...]`.

`proofTag` is the owning `Proof` block's tag (`Name.anonymous` if
anonymous) and `proofIdx` is its 0-based index. Both are embedded so
two `Proof { prove safety; }` blocks produce distinct names. -/
def obligationName (mname sname evname target : Name) (isDefault : Bool)
    (usingNames : Array Name) (proofTag : Name) (proofIdx : Nat) : Name :=
  let usingTag : String :=
    if usingNames.isEmpty then ""
    else "_using_" ++
      String.intercalate "_" (usingNames.toList.map Name.toString)
  let proofTagStr : String :=
    if proofTag == Name.anonymous then s!"block{proofIdx}"
    else proofTag.toString
  let tail :=
    proofTagStr ++ "_" ++
    (if isDefault then "default" else target.toString) ++ usingTag
  mname ++ sname ++ Name.mkSimple ((evname.toString) ++ "_correct_" ++ tail)

/-! ## One per-handler obligation -/

/-- Whether the event has a payload (used to pick the dispatcher
contract shape and the handler param list). -/
private def eventHasPayload (ctx : LocalPModuleCtx) (evName : Name) : Bool :=
  match ctx.events.find? evName with
  | some e => e.payload.isSome
  | none   => false

/-- Discharge result for one obligation. Failure variants carry the
solver / tactic diagnostic so the per-obligation report can show a
counter-example, an `unknown` reason, or a translator rejection. -/
inductive ObligationOutcome where
  | provedBySmt
  | userProved
  | disproved (cex : String)
  | unknown (reason : String)
  | tacticError (msg : String)
  | unfinished
  deriving Inhabited

namespace ObligationOutcome

def glyph : ObligationOutcome → String
  | provedBySmt    => "✓"
  | userProved     => "✓"
  | disproved _    => "✗"
  | unknown _      => "?"
  | tacticError _  => "✗"
  | unfinished     => "✗"

def tag : ObligationOutcome → String
  | provedBySmt    => "[SMT]"
  | userProved     => "[manual]"
  | disproved _    => "[SMT: counter-example]"
  | unknown _      => "[SMT: unknown]"
  | tacticError _  => "[tactic error]"
  | unfinished     => "[no diagnostic]"

def isFailure : ObligationOutcome → Bool
  | provedBySmt | userProved => false
  | _ => true

end ObligationOutcome

/-- Build the per-handler theorem and elaborate it. The proof tactic
is `pverify` for non-default lemmas, `pverify_default` for
`prove default`. `varNames` are the owning machine's `var` accessor
names (added to the `unfold` chain so `wpgen` can step through state
reads/writes); `lemmaInvNames` are the target lemma's per-invariant
defs (similarly unfolded); `machineNames` are every machine kind in the
pmodule (used to unfold `<M>_allocated` / `<M>_kind` so kind-tag checks
in user invariants reduce to plain arithmetic the SMT solver can see).
`is_<ev>` predicates are intentionally NOT added to the unfold chain —
their `match` bodies trip lean-auto's monomorphizer; leaving them folded
lets SMT treat them as uninterpreted predicates. -/
def emitOneObligation (modName : Name) (mname sname evname : Name)
    (target : Name) (isDefault : Bool) (usingNames : Array Name)
    (hasPayload : Bool) (varNames : Array Name)
    (lemmaInvNames : Array Name)
    (machineNames : Array Name)
    (eventNames : Array Name)
    (proofTag : Name) (proofIdx : Nat) :
    CommandElabM ObligationOutcome := do
  let thmName : Name :=
    obligationName mname sname evname target isDefault usingNames proofTag proofIdx
  let fullThmName : Name := modName ++ thmName
  if ← liftCoreM (hasPVerifyProof fullThmName) then
    logInfo m!"obligation {fullThmName} picked up from `@[pverifyProof]`"
    return .userProved
  let thmId : Ident := mkIdent thmName
  let handlerName : Name :=
    mname ++ sname ++ (evname.appendAfter "_handler")
  let handlerId : Ident := mkIdent handlerName
  let mIdent : Ident := mkIdent mname
  let stateAlias : Ident := mkIdent (sname.appendAfter "_st")
  let evCtor : Ident := mkIdent (`E ++ evname)
  let stx ← liftMacroM do
    let lemmaPred ← lemmaPredIdent target isDefault
    let usingPreds ← usingPredIdents usingNames
    let payloadTy := mkIdent (evname.appendAfter "_payload")
    let prpAbbrev : TSyntax `term := ← `(PProp $idSig)
    -- Pre/post both include `DefaultInvariants` so the three sanity
    -- invariants flow through every obligation. For `prove default;`
    -- directives `lemmaPred` *is* `DefaultInvariants`, so the
    -- duplicate is harmless (same predicate twice in the conjunction).
    let defaultPred : TSyntax `term ← `(PLean.DefaultInvariants)
    let sId : TSyntax `term := ← `(s)
    let basePre ← do
      let preds : Array (TSyntax `term) :=
        #[lemmaPred] ++ usingPreds ++ #[defaultPred]
      buildConjAt preds sId
    -- Dispatcher contract: existential witness that this handler
    -- only fires when an inflight label of the right shape exists.
    -- Handlers don't re-establish it on exit, so it lives only in the
    -- precondition.
    let dispatcherClause ← do
      if hasPayload then
        `(∃ lbl : ($idSig).Label,
            PLean.inflight lbl s ∧
            lbl.target = ($idThis).ref ∧
            (s.machines ($idThis).ref).currentState = $stateAlias ∧
            lbl.action = .event ($evCtor $idParam))
      else
        `(∃ lbl : ($idSig).Label,
            PLean.inflight lbl s ∧
            lbl.target = ($idThis).ref ∧
            (s.machines ($idThis).ref).currentState = $stateAlias ∧
            lbl.action = .event $evCtor)
    let preTerm : TSyntax `term ← `(fun (s : PLean.GlobalState $idSig) =>
                                      $basePre ∧ $dispatcherClause)
    let postBody ← buildConjAt #[lemmaPred, defaultPred] sId
    let postTerm : TSyntax `term ← `(fun (_ : Unit) (s : PLean.GlobalState $idSig) =>
                                       $postBody)
    let handlerTerm : TSyntax `term ←
      if hasPayload then
        `($handlerId $idThis $idParam)
      else
        `($handlerId $idThis)
    let handlerUnfold : Ident := mkIdent handlerName
    let lemmaUnfold : Ident :=
      if isDefault then mkIdent ``PLean.DefaultInvariants
      else                mkIdent target
    let mut accessorUnfolds : Array Ident := #[]
    for v in varNames do
      accessorUnfolds := accessorUnfolds.push
        (mkIdent (mname ++ (v.appendAfter "_get")))
      accessorUnfolds := accessorUnfolds.push
        (mkIdent (mname ++ (v.appendAfter "_set")))
    -- Per-invariant unfolds: after the bundle `safety := fun s => i s ∧ …`
    -- expands, the conjuncts `i s` are still opaque applications; we
    -- unfold each `i` so SMT sees the user's actual proposition.
    let usingNamesAll : Array Ident := (usingNames ++ lemmaInvNames).map mkIdent
    let usingUnfolds : Array Ident := usingNamesAll
    -- Per-machine kind helpers: user invariants frequently reference
    -- `<M>_allocated m s` (or the `is_<M>` alias the materialiser emits)
    -- and the `<M>_kind` numeric tag. Unfolding all three exposes
    -- `(s.machines m).kind` as an applied projection — SMT-translatable
    -- as a uninterpreted function symbol — and reduces kind equality to
    -- a Nat literal comparison.
    --
    -- Order matters: `is_<M>` MUST be unfolded before `<M>_allocated`,
    -- because `is_<M>` is `@[inline] def is_<M> m := <M>_allocated m` —
    -- when the goal carries `is_<M>` calls, attempting to unfold
    -- `<M>_allocated` first finds nothing to unfold (the constant
    -- doesn't appear yet). Once `is_<M>` is unfolded, `<M>_allocated`
    -- shows up and its unfold can fire.
    let mut kindUnfolds : Array Ident := #[]
    for m in machineNames do
      kindUnfolds := kindUnfolds.push
        (mkIdent (Name.mkSimple ("is_" ++ m.toString)))
      kindUnfolds := kindUnfolds.push (mkIdent (m.appendAfter "_allocated"))
      kindUnfolds := kindUnfolds.push (mkIdent (m.appendAfter "_kind"))
    let _ := eventNames -- bridging lemmas not injected (see file header)
    let hasAccessors := !accessorUnfolds.isEmpty
    let tail : TSyntax `tactic ←
      if isDefault then `(tactic| pverify_default)
      else                `(tactic| pverify)
    -- Build the proof tactic sequence programmatically. WHY this order:
    -- handler → target lemma → using-lemmas → per-machine kind helpers
    -- → accessors → PLean primitives → final tactic. Accessors must
    -- precede primitives because reversing the order trips `wpgen` into
    -- `WPGen.default` (see PVerifyConditional regression). The
    -- `isDefault` branch skips the lemma unfold — `lemmaPred` is
    -- `DefaultInvariants` and `target` is the literal `default`, which
    -- the `default_inv` tactic unfolds itself.
    let mut steps : Array (TSyntax `tactic) := #[]
    steps := steps.push (← `(tactic| unfold $handlerUnfold:ident))
    unless isDefault do
      steps := steps.push (← `(tactic| try unfold $lemmaUnfold:ident))
    -- WHY per-name `try unfold` rather than one batched `try unfold a b c …`:
    -- `unfold` fails atomically if ANY listed name is missing from the goal.
    -- Wrapping the batch in `try` would then drop EVERY unfold in that batch.
    -- Emitting one `try unfold` per name lets each succeed or fail independently.
    for u in usingUnfolds do
      steps := steps.push (← `(tactic| try unfold $u:ident))
    for u in kindUnfolds do
      steps := steps.push (← `(tactic| try unfold $u:ident))
    if hasAccessors then
      steps := steps.push (← `(tactic| try unfold $[$accessorUnfolds:ident]*))
    steps := steps.push (← `(tactic|
      try unfold PLean.send PLean.goto PLean.raise
                 PLean.markReceived PLean.announce))
    steps := steps.push tail
    let proofTacSeq : TSyntax ``Lean.Parser.Tactic.tacticSeq ←
      `(Lean.Parser.Tactic.tacticSeq| $[$steps]*)
    -- Wrap in `pverify_log_failure_else_sorry` so a tactic failure
    -- (translator rejection, SMT `sat`/`unknown`, etc.) is logged as
    -- a recoverable error before elaboration falls through to sorry.
    -- The post-elaboration scan in `processOne` reads that error from
    -- the message-log slice to classify the failure (counter-example
    -- vs. unknown vs. tactic error). Without the wrapper, an enclosing
    -- `first | … | sorry` would discard the SMT diagnostic entirely.
    let wrappedProof : TSyntax ``Lean.Parser.Tactic.tacticSeq ←
      `(Lean.Parser.Tactic.tacticSeq|
          pverify_log_failure_else_sorry $proofTacSeq)
    if hasPayload then
      `(set_option linter.unusedTactic false in
        theorem $thmId
            ($idThis : $mIdent) ($idParam : $payloadTy) :
            triple (l := $prpAbbrev) $preTerm $handlerTerm $postTerm := by
          $wrappedProof)
    else
      `(set_option linter.unusedTactic false in
        theorem $thmId
            ($idThis : $mIdent) :
            triple (l := $prpAbbrev) $preTerm $handlerTerm $postTerm := by
          $wrappedProof)
  elabCommand stx
  -- A `sorryAx` in the elaborated value means the tactic chain fell
  -- through to the `first | … | sorry` fallback. This catches both
  -- synchronous and async-snapshot tactic errors that message-log
  -- inspection alone would miss. The fine-grained sub-classification
  -- (disproved / unknown / tacticError) is added by `processOne`,
  -- which has access to the message-log slice.
  let env ← getEnv
  match env.find? fullThmName with
  | some (.thmInfo info) =>
    if info.value.hasSorry then return .unfinished
    return .provedBySmt
  | _ => return .unfinished

/-! ## Base-case obligation emission

For each `prove G [using P1, …]` directive, every individual invariant
`i` that constitutes the bundle `G` (its `lemma.invariants`) gets its
own theorem: `∀ s, InitConditions s → i s`. One VC per invariant lets a
failure report name the exact clause that doesn't follow from the init
state, instead of pointing at the conjunction.

`prove default` expands to one base-case VC per default-invariant
(`UniqueActions`, `IncreasingCount`, `ReceivedSubsetSent`); all three
hold vacuously at init (no labels sent yet) so they should close
trivially via SMT.

Premises (`using P`) do NOT get base-case VCs from this directive —
matching PVerifier's behaviour. They get their own base-case VCs when
they appear as the target of a separate `prove P` directive. -/

/-- Build the name `<Mod>.base_<proofTag>_<invariant>`. The proof-tag
prefix avoids collisions when two `Proof` blocks both prove the same
invariant (with different `using`-clauses). -/
def baseCaseName (invName : Name) (proofTag : Name) (proofIdx : Nat) : Name :=
  let proofTagStr : String :=
    if proofTag == Name.anonymous then s!"block{proofIdx}"
    else proofTag.toString
  Name.mkSimple ("base_" ++ proofTagStr ++ "_" ++ invName.toString)

/-- Emit one base-case obligation:
`∀ s : GlobalState Sig, InitConditions s → <invName> s`. The kind-helper
unfolds (`<M>_allocated`, `is_<M>`, `<M>_kind`) and the invariant
unfold are added to the proof so SMT sees the user's actual
proposition. `InitConditions` is unfolded too — its body is a closed
conjunction of `init-holds` clauses. -/
def emitBaseCaseObligation (modName : Name) (invName : Name)
    (isDefaultInv : Bool) (machineNames : Array Name)
    (eventNames : Array Name)
    (proofTag : Name) (proofIdx : Nat) :
    CommandElabM ObligationOutcome := do
  let thmName : Name := baseCaseName invName proofTag proofIdx
  let fullThmName : Name := modName ++ thmName
  if ← liftCoreM (hasPVerifyProof fullThmName) then
    logInfo m!"obligation {fullThmName} picked up from `@[pverifyProof]`"
    return .userProved
  let thmId : Ident := mkIdent thmName
  -- Unhygienic `s` binder: bare `s` inside a macro quotation acquires
  -- a macro scope and renders as `s✝` in the pretty-printed signature,
  -- breaking the copy-paste manual-proof skeleton. Same convention as
  -- `idThis` / `idParam` in the per-handler emitter.
  let idS : Ident := mkIdent `s
  let stx ← liftMacroM do
    let initsId : Ident := mkIdent `InitConditions
    let invIdent : Ident :=
      if isDefaultInv then mkIdent (`PLean ++ invName)
      else                 mkIdent invName
    let prpAbbrev : TSyntax `term ← `(PProp $idSig)
    let goalType : TSyntax `term ←
      `(∀ $idS : PLean.GlobalState $idSig,
          ($initsId : $prpAbbrev) $idS → ($invIdent) $idS)
    -- Unfold chain: `InitConditions`, the invariant, kind helpers,
    -- then close with `pverify_smt_close` (the base case never
    -- involves `wpgen` or handler unfolds).
    let mut steps : Array (TSyntax `tactic) := #[]
    steps := steps.push (← `(tactic| try unfold $initsId:ident))
    steps := steps.push (← `(tactic| try unfold $invIdent:ident))
    for m in machineNames do
      let isPred  := mkIdent (Name.mkSimple ("is_" ++ m.toString))
      let alloc   := mkIdent (m.appendAfter "_allocated")
      let kindLit := mkIdent (m.appendAfter "_kind")
      steps := steps.push (← `(tactic| try unfold $isPred:ident))
      steps := steps.push (← `(tactic| try unfold $alloc:ident))
      steps := steps.push (← `(tactic| try unfold $kindLit:ident))
    let _ := eventNames -- bridging lemmas not injected (see file header)
    if isDefaultInv then
      steps := steps.push (← `(tactic|
        try unfold PLean.UniqueActions PLean.IncreasingCount
                   PLean.ReceivedSubsetSent))
    steps := steps.push (← `(tactic|
      first | (intros; trivial) | pverify_smt_close))
    let proofTacSeq : TSyntax ``Lean.Parser.Tactic.tacticSeq ←
      `(Lean.Parser.Tactic.tacticSeq| $[$steps]*)
    let wrappedProof : TSyntax ``Lean.Parser.Tactic.tacticSeq ←
      `(Lean.Parser.Tactic.tacticSeq|
          pverify_log_failure_else_sorry $proofTacSeq)
    `(set_option linter.unusedTactic false in
      theorem $thmId : $goalType := by
        $wrappedProof)
  elabCommand stx
  let env ← getEnv
  match env.find? fullThmName with
  | some (.thmInfo info) =>
    if info.value.hasSorry then return .unfinished
    return .provedBySmt
  | _ => return .unfinished

/-! ## Walking the registry — `synthesise` is the entry point. -/

/-- One obligation's record: provenance, theorem name, pretty-printed
signature (used in the failure-skeleton dump), and outcome. -/
structure ObligationRecord where
  mname     : Name
  sname     : Name
  evname    : Name
  target    : Name
  thmName   : Name
  signature : String
  outcome   : ObligationOutcome
  deriving Inhabited

/-- Per-obligation records plus tally counters. -/
structure SynthesiseResult where
  attempted   : Nat := 0
  smtProved   : Nat := 0
  userProved  : Nat := 0
  disproved   : Nat := 0
  unknown     : Nat := 0
  tacticErr   : Nat := 0
  unfinished  : Nat := 0
  records     : Array ObligationRecord := #[]
  deriving Inhabited

namespace SynthesiseResult

/-- Total number of obligations that did NOT discharge. -/
def failures (r : SynthesiseResult) : Nat :=
  r.disproved + r.unknown + r.tacticErr + r.unfinished

end SynthesiseResult

/-! ## Cycle detection on the `using` graph. -/

private partial def dfsCheck (graph : NameMap (Array Name))
    (node : Name) (path : Array Name) (visited : NameSet) :
    CommandElabM NameSet := do
  if path.contains node then
    let cyc := (path.toList.dropWhile (· != node)) ++ [node]
    let cycStr := String.intercalate " → " (cyc.map Name.toString)
    throwError s!"cycle in `using`-lemmas detected: {cycStr}"
  if visited.contains node then return visited
  let mut v := visited.insert node
  let path' := path.push node
  match graph.find? node with
  | none      => return v
  | some succs =>
    for s in succs do
      v ← dfsCheck graph s path' v
    return v

private def detectUsingCycles (ctx : LocalPModuleCtx) :
    CommandElabM Unit := do
  let mut graph : NameMap (Array Name) := {}
  for proof in ctx.proofs do
    for dir in proof.directives do
      if dir.isDefault then continue
      let prev := (graph.find? dir.target).getD #[]
      graph := graph.insert dir.target (prev ++ dir.usingLemmas)
  let mut visited : NameSet := {}
  for (node, _) in graph.toList do
    visited ← dfsCheck graph node #[] visited

/-- Pull the machine's `var` names from its retained body. -/
private def machineVarNames (m : PMachineDecl) : Array Name := Id.run do
  let mut out : Array Name := #[]
  for it in m.body do
    if it.getKind == ``PLean.pMachineVar then
      if let some i := it[1]? then
        if i.isIdent then out := out.push i.getId
  return out

/-- Whether a structure field is a machine reference — its projection
returns either `PLean.MachineRef` or a machine-wrapper struct (`Server`,
`Node`, …). A machine ref is a reducible `Nat`, and a wrapper is a
`structure … where ref : MachineRef`; both denote a machine reference,
so the renderer labels either as `<Kind>#<ref>`. `machineLeanNames` is
the set of fully-qualified wrapper type names (`<Mod>.<M>`). -/
private def fieldIsRef (env : Environment) (machineLeanNames : Array Name)
    (structName fieldName : Name) : Bool :=
  let projName := structName ++ fieldName
  match env.find? projName with
  | some ci =>
    -- `projFn : <struct> → <fieldTy>`; the field type is the body.
    match ci.type with
    | .forallE _ _ body _ =>
      match body.getAppFn.constName? with
      | some ``PLean.MachineRef => true
      | some n => machineLeanNames.contains n
      | none   => false
    | _ => false
  | none => false

/-- Field names of a materialised structure in declaration order, paired
with whether each is `MachineRef`-typed. Read from the environment — the
registry's `defStx` is cleared by `#gen_module` before `synthesise`
runs, so re-parsing it is not an option. -/
private def structFields (env : Environment) (machineLeanNames : Array Name)
    (structName : Name) : Array (String × Bool) :=
  match Lean.getStructureInfo? env structName with
  | some info =>
    info.fieldNames.map (fun f =>
      (f.toString, fieldIsRef env machineLeanNames structName f))
  | none => #[]

/-- Build the name context the counter-example renderer needs from the
registry: state-constructor → (machine, state), the global `Fields`
order as `(machine, var)`, each event's payload field names, and the set
of all machine-reference field/var names (rendered as machine labels). -/
private def buildCexNameCtx (modName : Name) (ctx : LocalPModuleCtx) :
    CommandElabM Verify.CexNameCtx := do
  let env ← getEnv
  -- Fully-qualified wrapper type names (`<Mod>.<M>`), so a field typed
  -- by a machine wrapper (`var server : Server`) counts as a ref.
  let machineLeanNames : Array Name :=
    ctx.machineOrder.filterMap (fun mn =>
      if (ctx.machines.find? mn).isSome then some (modName ++ mn) else none)
  let mut stateCtors : Array (String × String × String) := #[]
  let mut fieldOrder : Array (String × String) := #[]
  let mut refFields : Array String := #[]
  let mut machineKinds : Array String := #[]
  for mname in ctx.machineOrder do
    let some m := ctx.machines.find? mname | continue
    let mStr := mname.toString
    machineKinds := machineKinds.push mStr
    for sd in m.states do
      let key := mStr ++ "_" ++ sd.name.toString
      stateCtors := stateCtors.push (key, mStr, sd.name.toString)
    -- Machine vars live in the global `<Mod>.Fields` struct as
    -- `<machine>_<var>`; check each for a machine-reference type.
    for v in machineVarNames m do
      fieldOrder := fieldOrder.push (mStr, v.toString)
      if fieldIsRef env machineLeanNames (modName ++ `Fields)
          (Name.mkSimple (mStr ++ "_" ++ v.toString)) then
        refFields := refFields.push v.toString
  let mut eventFields : Array (String × Array String) := #[]
  for ename in ctx.eventOrder do
    let some e := ctx.events.find? ename | continue
    let some payloadName := e.payload | continue
    let flds := structFields env machineLeanNames (modName ++ payloadName)
    unless flds.isEmpty do
      eventFields := eventFields.push (ename.toString, flds.map (·.1))
      for (fname, isRef) in flds do
        if isRef && !refFields.contains fname then
          refFields := refFields.push fname
  return { stateCtors, fieldOrder, eventFields, refFields, machineKinds }

/-! ## Failure classification helpers. -/

private def hasSubstring (s : String) (pattern : String) : Bool :=
  let pLen := pattern.length
  let sLen := s.length
  if pLen == 0 then true
  else if pLen > sLen then false
  else Id.run do
    let mut i : Nat := 0
    while i + pLen ≤ sLen do
      if s.extract ⟨i⟩ ⟨i + pLen⟩ == pattern then
        return true
      i := i + 1
    return false

/-- Cap a multi-line diagnostic so the report doesn't degenerate into a
wall of solver output. `maxLines` / `maxChars` default to the tight
bound used for tactic / unknown diagnostics; the disproved path passes a
looser bound so a decoded counter-example survives intact. -/
private def truncateForReport (s : String)
    (maxLines : Nat := 12) (maxChars : Nat := 1500) : String :=
  let lines := s.splitOn "\n"
  let joined := String.intercalate "\n" (lines.take maxLines)
  if joined.length > maxChars then joined.take maxChars ++ " …" else joined

/-- Turn `loom_smt`'s SAT diagnostic into a readable counter-example.
Decodes the embedded model into the per-machine state table + the
`sent` trace ordered by `actionCount`, using `ctx` to recover field /
state / payload names. Falls back to the de-mangled raw model when the
decode yields nothing, so the output is never worse than the verbatim
dump. -/
private def renderCex (smtMsg : String) (ctx : Verify.CexNameCtx) : String :=
  match Verify.extractModelText smtMsg with
  | none => truncateForReport smtMsg 40 4000
  | some modelText =>
    match Verify.renderModelText modelText ctx with
    | some rendered => truncateForReport rendered 60 6000
    | none =>
      let cleaned :=
        match Verify.parseModel modelText with
        | some defs =>
          "\n".intercalate (defs.toList.map (fun d =>
            if d.args.isEmpty then s!"{d.name} = {Verify.renderValue d.body}"
            else s!"{d.name} {Verify.renderValue (.app d.args)} = {Verify.renderValue d.body}"))
        | none => modelText
      truncateForReport cleaned 40 4000

/-- Classify a failure from the diagnostic refs set during emission.
The SMT diagnostic discriminates `sat` (counter-example) from
`unknown` (incomplete theory / timeout); anything else collapses to
`tacticError`. -/
private def classifyFailure : CommandElabM ObligationOutcome := do
  if let some smtMsg ← pverifySmtDiagRef.get then
    if hasSubstring smtMsg "the goal is false" then
      let ctx := (← Verify.cexNameCtxRef.get).getD {}
      return .disproved (renderCex smtMsg ctx)
    if hasSubstring smtMsg "the goal is unknown" then
      return .unknown (truncateForReport smtMsg)
    return .tacticError (truncateForReport smtMsg)
  if let some tacMsg ← pverifyTacDiagRef.get then
    return .tacticError (truncateForReport tacMsg)
  return .unfinished

private def renderSignature (fullThmName : Name) : CommandElabM String := do
  match (← getEnv).find? fullThmName with
  | some _ =>
    try
      let sig ← liftTermElabM (Lean.PrettyPrinter.ppSignature fullThmName)
      return sig.fmt.pretty
    catch _ => return ""
  | none => return ""

/-- Shared bookkeeping: clear diag refs, run the emitter, classify
the outcome, scrub sync-error messages, and append a record. The
emitter returns the raw outcome; `classifyFailure` upgrades a bare
`.unfinished` via the diagnostic refs. -/
private def runEmitterAndRecord (modName mname sname evname target thmName : Name)
    (emitter : CommandElabM ObligationOutcome)
    (acc : SynthesiseResult) : CommandElabM SynthesiseResult := do
  let acc := { acc with attempted := acc.attempted + 1 }
  let fullThmName := modName ++ thmName
  pverifySmtDiagRef.set none
  pverifyTacDiagRef.set none
  let savedSt ← get
  let outcomeRaw ← try emitter
    catch e =>
      let errMsg ← e.toMessageData.toString
      pure (ObligationOutcome.tacticError (truncateForReport errMsg))
  let outcome ← match outcomeRaw with
    | .unfinished => classifyFailure
    | other       => pure other
  -- Scrub per-obligation noise from the slice so only the command's
  -- one consolidated report reaches the user. Drop (a) sync-error
  -- messages — the diagnostic refs already carry what we need — and
  -- (b) Loom's `loom_smt` "Goal proven by <solver>" info, emitted once
  -- per discharged obligation; the report's "N proved by SMT" summary
  -- subsumes it.
  let curSt ← get
  let preMsgsArr := savedSt.messages.toArray
  let postMsgsArr := curSt.messages.toArray
  let newMsgs := postMsgsArr.extract preMsgsArr.size postMsgsArr.size
  let isNoise (m : Lean.Message) : CommandElabM Bool := do
    if m.severity matches .error then return true
    let s ← m.data.toString
    return hasSubstring s "Goal proven by" || hasSubstring s "Trusting SMT solver"
  if ← newMsgs.anyM isNoise then
    let kept ← newMsgs.filterM (fun m => return !(← isNoise m))
    let mergedMsgs := kept.foldl (init := savedSt.messages) (·.add ·)
    modify fun st => { st with messages := mergedMsgs }
  let signature ← renderSignature fullThmName
  let record : ObligationRecord :=
    { mname, sname, evname, target, thmName := fullThmName,
      signature, outcome }
  let acc := { acc with records := acc.records.push record }
  match outcome with
  | .userProved      =>
    return { acc with userProved  := acc.userProved + 1 }
  | .provedBySmt     =>
    return { acc with smtProved   := acc.smtProved + 1 }
  | .disproved _     =>
    return { acc with disproved   := acc.disproved + 1 }
  | .unknown _       =>
    return { acc with unknown     := acc.unknown + 1 }
  | .tacticError _   =>
    return { acc with tacticErr   := acc.tacticErr + 1 }
  | .unfinished      =>
    return { acc with unfinished  := acc.unfinished + 1 }

/-- Emit one per-handler obligation. -/
private def processOne (modName mname sname evname : Name)
    (target : Name) (isDefault : Bool) (usingNames : Array Name)
    (hasPayload : Bool) (varNames : Array Name)
    (lemmaInvNames : Array Name)
    (machineNames : Array Name)
    (eventNames : Array Name)
    (proofTag : Name) (proofIdx : Nat)
    (acc : SynthesiseResult) : CommandElabM SynthesiseResult := do
  let thmName :=
    obligationName mname sname evname target isDefault usingNames proofTag proofIdx
  runEmitterAndRecord modName mname sname evname target thmName
    (emitOneObligation modName mname sname evname target isDefault
      usingNames hasPayload varNames lemmaInvNames machineNames eventNames
      proofTag proofIdx)
    acc

/-- Emit one base-case obligation for a single invariant in a directive's
target lemma. `mname`/`sname`/`evname` are recorded as `anonymous` —
base-case VCs are pmodule-scoped, not handler-scoped. -/
private def processBaseCase (modName invName : Name) (isDefaultInv : Bool)
    (machineNames : Array Name)
    (eventNames : Array Name)
    (proofTag : Name) (proofIdx : Nat)
    (acc : SynthesiseResult) : CommandElabM SynthesiseResult := do
  let thmName := baseCaseName invName proofTag proofIdx
  runEmitterAndRecord modName Name.anonymous Name.anonymous Name.anonymous
    invName thmName
    (emitBaseCaseObligation modName invName isDefaultInv machineNames
      eventNames proofTag proofIdx)
    acc

/-- For each `Proof` block's `prove X` directive, walk every
`(machine, state, event)` and emit an obligation. After the user's
directives, auto-emit a `prove default;` obligation for every
`(M, S, ev)` not already covered. -/
def synthesise (modName : Name) (ctx : LocalPModuleCtx) :
    CommandElabM SynthesiseResult := do
  detectUsingCycles ctx
  Verify.cexNameCtxRef.set (some (← buildCexNameCtx modName ctx))
  let mut result : SynthesiseResult := {}
  let mut explicitDefault : Std.HashSet (Name × Name × Name) := {}
  -- Per-lemma invariant-name lookup used to feed `lemmaInvNames` into
  -- `processOne`: the obligation generator unfolds each individual
  -- invariant in addition to the bundle predicate.
  let lemmaInvariantsOf (n : Name) : Array Name :=
    match ctx.lemmas.find? n with
    | some l => l.invariants
    | none   => #[]
  -- Every non-spec machine's name in registration order. Drives the
  -- `<M>_allocated` / `<M>_kind` unfold chain in `emitOneObligation`,
  -- so user invariants that reference machine-kind predicates reduce to
  -- plain arithmetic for the SMT solver.
  let allMachineNames : Array Name := Id.run do
    let mut out : Array Name := #[]
    for mn in ctx.machineOrder do
      if let some md := ctx.machines.find? mn then
        if !md.isSpec then out := out.push mn
    return out
  -- Every event name in the pmodule. Drives the `<ev>_payload_of`
  -- unfold chain in `emitOneObligation` / `emitBaseCaseObligation` so
  -- the field-projection sugar `e.<f>` reduces under SMT prep.
  let allEventNames : Array Name := ctx.eventOrder
  -- Names of the three default invariants — the base case for
  -- `prove default;` enumerates them so the failure report names which
  -- one didn't hold at init (rather than the bundled `DefaultInvariants`).
  let defaultInvNames : Array Name :=
    #[`UniqueActions, `IncreasingCount, `ReceivedSubsetSent]
  for hProof : proofIdx in [0:ctx.proofs.size] do
    let proof := ctx.proofs[proofIdx]'hProof.upper
    for dir in proof.directives do
      -- Base case: one VC per individual invariant in the target lemma's
      -- bundle (or per default-invariant for `prove default`). Premises
      -- (`using P`) intentionally do NOT get base-case VCs from this
      -- directive — they get one when they themselves are a `prove`
      -- target. Matches PVerifier's "only Goals get UCLID `invariant`
      -- declarations" semantics.
      let baseInvs : Array Name :=
        if dir.isDefault then defaultInvNames
        else lemmaInvariantsOf dir.target
      for inv in baseInvs do
        result ← processBaseCase modName inv dir.isDefault allMachineNames
          allEventNames proof.name proofIdx result
      -- Inductive step: per-handler triples.
      for mname in ctx.machineOrder do
        let some m := ctx.machines.find? mname | continue
        if m.isSpec then
          logInfo m!"spec machine `{mname}` skipped — Phase 4 owns spec obligations"
          continue
        let varNames := machineVarNames m
        for sd in m.states do
          for ev in sd.handles do
            if sd.gotos.contains ev then
              continue
            let hasPayload := eventHasPayload ctx ev
            if dir.isDefault then
              explicitDefault := explicitDefault.insert (mname, sd.name, ev)
            -- Aggregate invariant unfolds: target lemma's invariants
            -- + each `using`-lemma's. Duplicates are harmless.
            let mut lemmaInvNames : Array Name :=
              if dir.isDefault then #[] else lemmaInvariantsOf dir.target
            for u in dir.usingLemmas do
              lemmaInvNames := lemmaInvNames ++ lemmaInvariantsOf u
            result ← processOne modName mname sd.name ev
              dir.target dir.isDefault dir.usingLemmas hasPayload varNames
              lemmaInvNames allMachineNames allEventNames
              proof.name proofIdx result
  -- Auto-default pass: synthetic `block_auto_default` tag avoids
  -- collisions with user-tagged emissions; index past-the-end of the
  -- proofs array. No base case emitted here — the default invariants'
  -- base case is so trivial (vacuously true on the empty buffer) that
  -- duplicating it per (M, S, ev) gap would just pad the report.
  let autoTag : Name := `block_auto_default
  let autoIdx : Nat := ctx.proofs.size
  for mname in ctx.machineOrder do
    let some m := ctx.machines.find? mname | continue
    if m.isSpec then continue
    let varNames := machineVarNames m
    for sd in m.states do
      for ev in sd.handles do
        if sd.gotos.contains ev then continue
        if explicitDefault.contains (mname, sd.name, ev) then continue
        let hasPayload := eventHasPayload ctx ev
        result ← processOne modName mname sd.name ev
          `default true #[] hasPayload varNames
          #[] allMachineNames allEventNames autoTag autoIdx result
  return result

end Verify
end PLean
