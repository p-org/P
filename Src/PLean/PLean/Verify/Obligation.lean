/-
PLean.Verify.Obligation — synthesise per-handler Hoare-triple
obligations from registry data.

For each `Proof { prove X using Y, …; }` directive, walk every
(machine, state, handler) triple and emit one Hoare-triple theorem
per (handler, target lemma) pair. `prove default;` swaps the bundle
for `DefaultInvariants` and the discharger for `pverify_default`.
The inductive-step VC carries the user invariant + dispatcher
contract in its precondition; the initiation leg
(`InitConditions ⇒ Inv`) is a separate base-case VC per individual
invariant. After user directives a `block_auto_default` pass emits a
default obligation for every (M, S, ev) not already covered.

A theorem registered under `@[pverifyProof]` is discharged via
`exact @<userThm>` inside a `_check` shim that re-builds the expected
type, so a wrong-type or sorried user proof is reported as failed.
-/
import Lean
import PLean.Internal.Decls
import PLean.Internal.Registry
import PLean.Surface.Machine
import PLean.Verify.Tactic
import PLean.Verify.ProofRegistry
import PLean.Verify.CexModel
import PLean.Verify.Profile

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
private def idLbl   : Ident := mkIdent `lbl

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

/-- Classify a previously-emitted obligation by reading the env. Looks
up `<thmName>` (or `<thmName>_check` for a manual proof) and inspects
its value for `sorry`. Under `Elab.async = true` the env-lookup blocks
on the theorem's body-elab Task, so calling this AFTER all emissions
are queued lets the bodies elaborate concurrently. `manualProof` must
match the value used at emission time. -/
private def classifyOneObligation (fullThmName : Name) (manualProof : Bool) :
    CommandElabM ObligationOutcome := do
  let checkName : Name :=
    if manualProof then fullThmName.appendAfter "_check" else fullThmName
  let env ← getEnv
  match env.find? checkName with
  | some (.thmInfo info) =>
    if info.value.hasSorry then return .unfinished
    if manualProof then
      -- `_check`'s value is `@<userThm> args`, so `hasSorry` on it doesn't
      -- see through to the user theorem's body. Inspect the user theorem
      -- directly so a sorried `@[pverifyProof]` is reported as unfinished.
      match env.find? fullThmName with
      | some (.thmInfo userInfo) =>
        if userInfo.value.hasSorry then return .unfinished
      | _ => return .unfinished
      logInfo m!"obligation {fullThmName} discharged by `@[pverifyProof]` (type checked)"
      return .userProved
    return .provedBySmt
  | _ => return .unfinished

/-- Emit the per-handler theorem. Returns `true` if the obligation has
a `@[pverifyProof]`-supplied manual proof (passed to
`classifyOneObligation` later). Under `Elab.async = true` `elabCommand`
returns once the signature is committed; the body — which is where SMT
runs — elaborates on a worker thread. Classification is deferred so
bodies overlap.

The proof tactic is `pverify` (or `pverify_default` for `prove default`).
`varNames`, `lemmaInvNames`, and `machineNames` feed the `unfold` chain
so `wpgen` steps through state reads/writes, invariant bundles unfold,
and kind-tag checks reduce. `is_<ev>` predicates are intentionally left
folded — their `match` bodies trip lean-auto's monomorphizer, but as
opaque predicates they translate fine. -/
def emitOneObligation (modName : Name) (mname sname evname : Name)
    (target : Name) (isDefault : Bool) (usingNames : Array Name)
    (hasPayload : Bool) (varNames : Array Name)
    (lemmaInvNames : Array Name)
    (machineNames : Array Name)
    (eventNames : Array Name)
    (axiomNames : Array Name)
    (proofTag : Name) (proofIdx : Nat) :
    CommandElabM Bool := do
  let thmName : Name :=
    obligationName mname sname evname target isDefault usingNames proofTag proofIdx
  let fullThmName : Name := modName ++ thmName
  -- A `@[pverifyProof]`-registered theorem isn't trusted on name alone:
  -- we build the obligation's exact expected type and discharge it via
  -- `exact @<userThm>` inside a `<thmName>_check` shim. A wrong-type
  -- user theorem fails the `exact` and the obligation is reported as
  -- failed.
  let manualProof ← liftCoreM (hasPVerifyProof fullThmName)
  let thmId : Ident :=
    if manualProof then mkIdent (thmName.appendAfter "_check") else mkIdent thmName
  let userThmId : Ident := mkIdent fullThmName
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
    -- `DefaultInvariants` (UA/IC/RS) is a well-formedness bundle,
    -- proven once by `prove default` obligations and not assumed in
    -- user-invariant obligations. Keeping it out of the user-invariant
    -- pre keeps those goals out of quantified UF+LIA, where SMT
    -- returns `unknown`. A user invariant that needs the buffer
    -- ordering should name it via an explicit `using` premise.
    let sId : TSyntax `term := ← `(s)
    let basePre ← do
      let preds : Array (TSyntax `term) :=
        #[lemmaPred] ++ usingPreds
      buildConjAt preds sId
    -- Dispatcher contract conjuncts: an inflight `lbl` of the right
    -- shape exists. The contract lives only in the precondition (the
    -- handler doesn't re-establish it).
    --
    -- `is_<M> this.ref s` pins `this` to its kind. Without this guard
    -- the solver could fabricate a `this` whose `currentState` is the
    -- right state but whose `kind` is some other machine's — flat
    -- `MachineState` doesn't enforce the coupling structurally.
    --
    -- `lbl` is a universal binder rather than an existential so the
    -- executed program (`markReceived lbl >>= handler`) can refer to it.
    let isThisKind : Ident := mkIdent (Name.mkSimple ("is_" ++ mname.toString))
    let dispatcherClause ← do
      if hasPayload then
        `(PLean.inflight $idLbl s ∧
          ($idLbl).target = ($idThis).ref ∧
          $isThisKind ($idThis).ref s ∧
          (s.machines ($idThis).ref).currentState = $stateAlias ∧
          ($idLbl).action = .event ($evCtor $idParam))
      else
        `(PLean.inflight $idLbl s ∧
          ($idLbl).target = ($idThis).ref ∧
          $isThisKind ($idThis).ref s ∧
          (s.machines ($idThis).ref).currentState = $stateAlias ∧
          ($idLbl).action = .event $evCtor)
    let preTerm : TSyntax `term ← `(fun (s : PLean.GlobalState $idSig) =>
                                      $basePre ∧ $dispatcherClause)
    -- Post checks `lemmaPred` only. For `prove default` that's
    -- `DefaultInvariants`; for user invariants the defaults stay
    -- assumption-only (in the pre).
    let postBody ← buildConjAt #[lemmaPred] sId
    let postTerm : TSyntax `term ← `(fun (_ : Unit) (s : PLean.GlobalState $idSig) =>
                                       $postBody)
    -- Mark `lbl` received before running the handler — without the
    -- prologue the consumed event would stay in-flight in the
    -- post-state and break any "in-flight <ev> ⇒ …" invariant.
    let handlerCall : TSyntax `term ←
      if hasPayload then `($handlerId $idThis $idParam)
      else                `($handlerId $idThis)
    let handlerTerm : TSyntax `term ←
      `(PLean.markReceived (P := $idSig) $idLbl >>= fun _ => $handlerCall)
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
    -- expands, each `i s` is still an opaque application; unfold every
    -- `i` so SMT sees the user's actual proposition.
    let usingNamesAll : Array Ident := (usingNames ++ lemmaInvNames).map mkIdent
    let usingUnfolds : Array Ident := usingNamesAll
    -- Per-machine kind helpers reduce `is_<M>` / `<M>_allocated` /
    -- `<M>_kind` to plain (Nat) comparisons on `(s.machines m).kind`.
    -- Order matters: `is_<M> := <M>_allocated …`, so `is_<M>` must be
    -- unfolded first (otherwise `<M>_allocated` is invisible).
    let mut kindUnfolds : Array Ident := #[]
    for m in machineNames do
      kindUnfolds := kindUnfolds.push
        (mkIdent (Name.mkSimple ("is_" ++ m.toString)))
      kindUnfolds := kindUnfolds.push (mkIdent (m.appendAfter "_allocated"))
      kindUnfolds := kindUnfolds.push (mkIdent (m.appendAfter "_kind"))
    -- Bring each event's `<ev>_payload_of_spec` into the local context.
    -- The extractor is sealed `@[irreducible]` so SMT treats it as an
    -- uninterpreted symbol; the `_spec` fact is its defining equation
    -- and lets a routing invariant `∀ e, is_<ev> e → … (<ev>_payload_of
    -- e) …` close over a freshly-sent label.
    let mut specHaves : Array (TSyntax `tactic) := #[]
    for en in eventNames do
      let specId : Ident :=
        mkIdent (Name.mkSimple (en.toString ++ "_payload_of_spec"))
      let hypId : Ident :=
        mkIdent (Name.mkSimple ("hspec_" ++ en.toString))
      specHaves := specHaves.push (← `(tactic| have $hypId:ident := $specId:ident))
    -- One `have` per pmodule-declared `paxiom`. `loom_smt [*]` reads
    -- only the local context, so top-level Lean `axiom`s (produced by
    -- `paxiom` / `pinstance`-field synthesis) need to be lifted in
    -- explicitly.
    let mut axiomHaves : Array (TSyntax `tactic) := #[]
    for an in axiomNames do
      let axId : Ident := mkIdent an
      let hypId : Ident :=
        mkIdent (Name.mkSimple ("hax_" ++ an.toString))
      axiomHaves := axiomHaves.push (← `(tactic| have $hypId:ident := @$axId))
    let hasAccessors := !accessorUnfolds.isEmpty
    let tail : TSyntax `tactic ←
      if isDefault then `(tactic| pverify_default)
      else                `(tactic| pverify)
    -- Unfold order: handler → target lemma → using-lemmas → kind
    -- helpers → accessors → PLean primitives → closing tactic.
    -- Accessors must precede primitives, otherwise reversing the
    -- order trips `wpgen` into `WPGen.default`.
    let mut steps : Array (TSyntax `tactic) := #[]
    steps := steps.push (← `(tactic| unfold $handlerUnfold:ident))
    unless isDefault do
      steps := steps.push (← `(tactic| try unfold $lemmaUnfold:ident))
    -- Per-name `try unfold` rather than a batched form: `unfold` fails
    -- atomically if any listed name is missing, and wrapping the
    -- batch in `try` would drop every unfold in it.
    for u in usingUnfolds do
      steps := steps.push (← `(tactic| try unfold $u:ident))
    for u in kindUnfolds do
      steps := steps.push (← `(tactic| try unfold $u:ident))
    if hasAccessors then
      steps := steps.push (← `(tactic| try unfold $[$accessorUnfolds:ident]*))
    steps := steps.push (← `(tactic|
      try unfold PLean.send PLean.goto PLean.raise
                 PLean.markReceived PLean.announce))
    for h in specHaves do
      steps := steps.push h
    for h in axiomHaves do
      steps := steps.push h
    steps := steps.push tail
    let proofTacSeq : TSyntax ``Lean.Parser.Tactic.tacticSeq ←
      `(Lean.Parser.Tactic.tacticSeq| $[$steps]*)
    -- For an auto obligation the body is the `pverify` chain; for a
    -- manual obligation the body delegates to the user's
    -- `@[pverifyProof]` theorem via `exact @<userThm>`. Wrapping in
    -- `pverify_log_failure_else_sorry` stashes any tactic failure in
    -- the diag map and closes with `sorry` so elaboration of the
    -- enclosing theorem still succeeds.
    let bodyTac : TSyntax ``Lean.Parser.Tactic.tacticSeq ←
      if manualProof then
        if hasPayload then `(Lean.Parser.Tactic.tacticSeq| exact $userThmId $idThis $idParam $idLbl)
        else                `(Lean.Parser.Tactic.tacticSeq| exact $userThmId $idThis $idLbl)
      else pure proofTacSeq
    let wrappedProof : TSyntax ``Lean.Parser.Tactic.tacticSeq ←
      `(Lean.Parser.Tactic.tacticSeq|
          pverify_log_failure_else_sorry $bodyTac)
    -- `pverify.obligationKey` is captured by Lean's `wrapAsync` and
    -- propagated into the body-elab worker, so the per-obligation
    -- tactic finds its own slot in the diag map.
    let keyLit := Syntax.mkStrLit fullThmName.toString
    if hasPayload then
      `(set_option linter.unusedTactic false in
        set_option pverify.obligationKey $keyLit in
        theorem $thmId
            ($idThis : $mIdent) ($idParam : $payloadTy) ($idLbl : ($idSig).Label) :
            triple (l := $prpAbbrev) $preTerm $handlerTerm $postTerm := by
          $wrappedProof)
    else
      `(set_option linter.unusedTactic false in
        set_option pverify.obligationKey $keyLit in
        theorem $thmId
            ($idThis : $mIdent) ($idLbl : ($idSig).Label) :
            triple (l := $prpAbbrev) $preTerm $handlerTerm $postTerm := by
          $wrappedProof)
  elabCommand stx
  return manualProof

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

/-- Emit one base-case obligation
`∀ s : GlobalState Sig, InitConditions s → <invName> s`. Unfolds
`InitConditions`, the invariant, and the kind helpers so SMT sees
the user's actual proposition. Returns `true` for a
`@[pverifyProof]`-supplied proof. Classification is deferred to
`classifyOneObligation` (same parallelism story as
`emitOneObligation`). -/
def emitBaseCaseObligation (modName : Name) (invName : Name)
    (isDefaultInv : Bool) (machineNames : Array Name)
    (eventNames : Array Name)
    (axiomNames : Array Name)
    (proofTag : Name) (proofIdx : Nat) :
    CommandElabM Bool := do
  let thmName : Name := baseCaseName invName proofTag proofIdx
  let fullThmName : Name := modName ++ thmName
  let manualProof ← liftCoreM (hasPVerifyProof fullThmName)
  let thmId : Ident :=
    if manualProof then mkIdent (thmName.appendAfter "_check") else mkIdent thmName
  let userThmId : Ident := mkIdent fullThmName
  -- Unhygienic `s` binder so the pretty-printed signature shows `s`
  -- (not `s✝`) in the copy-paste manual-proof skeleton.
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
    -- Unfold chain (no `wpgen` needed for a base case): inits,
    -- invariant, kind helpers, then close via SMT.
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
    let _ := eventNames
    if isDefaultInv then
      steps := steps.push (← `(tactic|
        try unfold PLean.UniqueActions PLean.IncreasingCount
                   PLean.ReceivedSubsetSent))
    -- Lift each `paxiom` into the local context (same reason as in
    -- `emitOneObligation`: `loom_smt [*]` reads only the lctx).
    for an in axiomNames do
      let axId : Ident := mkIdent an
      let hypId : Ident :=
        mkIdent (Name.mkSimple ("hax_" ++ an.toString))
      steps := steps.push (← `(tactic| have $hypId:ident := @$axId))
    steps := steps.push (← `(tactic|
      first | (intros; trivial) | pverify_smt))
    let proofTacSeq : TSyntax ``Lean.Parser.Tactic.tacticSeq ←
      `(Lean.Parser.Tactic.tacticSeq| $[$steps]*)
    let bodyTac : TSyntax ``Lean.Parser.Tactic.tacticSeq ←
      if manualProof then `(Lean.Parser.Tactic.tacticSeq| exact $userThmId)
      else pure proofTacSeq
    let wrappedProof : TSyntax ``Lean.Parser.Tactic.tacticSeq ←
      `(Lean.Parser.Tactic.tacticSeq|
          pverify_log_failure_else_sorry $bodyTac)
    let keyLit := Syntax.mkStrLit fullThmName.toString
    `(set_option linter.unusedTactic false in
      set_option pverify.obligationKey $keyLit in
      theorem $thmId : $goalType := by
        $wrappedProof)
  elabCommand stx
  return manualProof

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

/-- Pull `(state, event)` pairs for which the state body has `on
$ev goto $tgt` — those events have no `_handler` def, so the
obligation generator must skip them. -/
private def machineGotoHandlers (m : PMachineDecl) :
    Std.HashSet (Name × Name) := Id.run do
  let mut out : Std.HashSet (Name × Name) := {}
  for it in m.body do
    let kind := it.getKind
    -- pMachineStartState: "start" "state" ident "{" body "}" (idx 2 = ident, 4 = body)
    -- pMachineState:      "state" ident "{" body "}" (idx 1 = ident, 3 = body)
    let (sidIdx, bodyIdx) :=
      if kind == ``PLean.pMachineStartState then (2, 4)
      else if kind == ``PLean.pMachineState then (1, 3)
      else (0, 0)
    if sidIdx == 0 then continue
    let some sidStx := it[sidIdx]? | continue
    unless sidStx.isIdent do continue
    let sname := sidStx.getId
    let some items := it[bodyIdx]? | continue
    for sit in items.getArgs do
      if sit.getKind == ``PLean.pStateOnGoto then
        -- pStateOnGoto: "on" ident "goto" ident (idx 1 = event ident)
        if let some evStx := sit[1]? then
          if evStx.isIdent then out := out.insert (sname, evStx.getId)
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
  -- `<M>_kind` Nat tags, assigned 1,2,… in registration order — mirrors
  -- `GenModule.lean::emitMachineKinds`'s `idx := 1` numbering (same
  -- `ctx.machineOrder` loop, same skip of missing machines), so the
  -- renderer can flag a row whose runtime `kind` field contradicts the
  -- kind its `currentState` implies.
  let mut machineKindIdx : Array (String × Int) := #[]
  for mname in ctx.machineOrder do
    let some m := ctx.machines.find? mname | continue
    let mStr := mname.toString
    machineKinds := machineKinds.push mStr
    machineKindIdx := machineKindIdx.push (mStr, Int.ofNat (machineKindIdx.size + 1))
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
  return { stateCtors, fieldOrder, eventFields, refFields, machineKinds, machineKindIdx }

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

/-- Classify a failure from the per-obligation diagnostic map.
The SMT diagnostic discriminates `sat` (counter-example) from
`unknown` (incomplete theory / timeout); anything else collapses to
`tacticError`. -/
private def classifyFailure (key : String) : CommandElabM ObligationOutcome := do
  let diag ← liftM (getDiag key : IO _)
  if let some smtMsg := diag.smt then
    if hasSubstring smtMsg "the goal is false" then
      let ctx := (← Verify.cexNameCtxRef.get).getD {}
      return .disproved (renderCex smtMsg ctx)
    if hasSubstring smtMsg "the goal is unknown" then
      return .unknown (truncateForReport smtMsg)
    return .tacticError (truncateForReport smtMsg)
  if let some tacMsg := diag.tac then
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

/-- One emitted-but-not-yet-classified obligation.
`manualProof` is `true` for a `@[pverifyProof]`-supplied proof.
`emitError?` carries any synchronous exception thrown by the emitter,
so the classify pass can report it as a tactic error without
inspecting an env entry that may not exist. -/
private structure PendingObligation where
  mname        : Name
  sname        : Name
  evname       : Name
  target       : Name
  fullThmName  : Name
  key          : String
  manualProof  : Bool
  emitError?   : Option String
  deriving Inhabited

/-- Run the emitter, record a `PendingObligation`, and scrub per-
obligation noise from the message log. Classification is deferred to
`classifyOnePending` so all bodies can elaborate concurrently under
`Elab.async = true`.

Message scrubbing runs inline before the next emission appends. It
drops sync error messages (their content is already in `emitError?` /
the diag map) and Loom's per-obligation info logs. A buggy user
`@[pverifyProof]` whose declaration name collides with the emitter's
output produces a duplicate-decl error here — without the scrub that
would break `#guard_msgs` even though the obligation's own classify
path doesn't depend on it. -/
private def runEmitOnly (modName mname sname evname target thmName : Name)
    (emitter : CommandElabM Bool)
    (acc : SynthesiseResult) :
    CommandElabM (SynthesiseResult × PendingObligation) := do
  let acc := { acc with attempted := acc.attempted + 1 }
  let fullThmName := modName ++ thmName
  let key := fullThmName.toString
  liftM (PLean.Verify.Profile.beginObligation key : IO Unit)
  let savedSt ← get
  let preMsgsSize := savedSt.messages.toArray.size
  let (manualProof, emitError?) ← try
      let m ← emitter
      pure (m, none)
    catch e =>
      let errMsg ← e.toMessageData.toString
      pure (false, some (truncateForReport errMsg))
  -- Operate on the persistent `MessageLog.unreported` directly; the
  -- `Array.foldl` rebuild path lost the persistent structure that
  -- Lean's snapshot machinery relies on. Quick-scan first so the slow
  -- rebuild only runs when there's actual noise to drop.
  let curSt ← get
  let curUnreported := curSt.messages.unreported
  if curUnreported.size > preMsgsSize then
    let isNoise (m : Lean.Message) : CommandElabM Bool := do
      if m.severity matches .error then return true
      let s ← m.data.toString
      return hasSubstring s "Goal proven by"
        || hasSubstring s "Trusting SMT solver"
        || hasSubstring s "discharged by `@[pverifyProof]`"
    let mut hasNoise : Bool := false
    for i in [preMsgsSize:curUnreported.size] do
      if ← isNoise (curUnreported.get! i) then hasNoise := true; break
    if hasNoise then
      let mut keptTail : Array Lean.Message := #[]
      for i in [preMsgsSize:curUnreported.size] do
        let m := curUnreported.get! i
        unless ← isNoise m do keptTail := keptTail.push m
      let preMsgs := savedSt.messages.unreported
      let mergedUnreported : Lean.PersistentArray Lean.Message :=
        keptTail.foldl (·.push ·) preMsgs
      modify fun st =>
        { st with messages := { st.messages with unreported := mergedUnreported } }
  let pending : PendingObligation := {
    mname, sname, evname, target, fullThmName, key, manualProof, emitError?
  }
  return (acc, pending)

/-- Classify a previously-emitted obligation. Reads the env (blocking
on the body-elab Task under `Elab.async = true`), promotes
`.unfinished` via the per-obligation diag map, and appends a record
to `acc.records`. The log was already scrubbed in `runEmitOnly`. -/
private def classifyOnePending (pending : PendingObligation)
    (acc : SynthesiseResult) : CommandElabM SynthesiseResult := do
  let { mname, sname, evname, target, fullThmName, key,
        manualProof, emitError?, .. } := pending
  let outcomeRaw : ObligationOutcome ← match emitError? with
    | some msg => pure (.tacticError msg)
    | none =>
      try classifyOneObligation fullThmName manualProof
      catch e =>
        let errMsg ← e.toMessageData.toString
        pure (.tacticError (truncateForReport errMsg))
  let outcome ← match outcomeRaw with
    | .unfinished => classifyFailure key
    | other       => pure other
  let signature ← renderSignature fullThmName
  let record : ObligationRecord :=
    { mname, sname, evname, target, thmName := fullThmName,
      signature, outcome }
  let acc := { acc with records := acc.records.push record }
  liftM (PLean.Verify.Profile.endObligation key : IO Unit)
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

/-- Emit one per-handler obligation; returns a `PendingObligation`
record for later classification. -/
private def processOneEmit (modName mname sname evname : Name)
    (target : Name) (isDefault : Bool) (usingNames : Array Name)
    (hasPayload : Bool) (varNames : Array Name)
    (lemmaInvNames : Array Name)
    (machineNames : Array Name)
    (eventNames : Array Name)
    (axiomNames : Array Name)
    (proofTag : Name) (proofIdx : Nat)
    (acc : SynthesiseResult) :
    CommandElabM (SynthesiseResult × PendingObligation) := do
  let thmName :=
    obligationName mname sname evname target isDefault usingNames proofTag proofIdx
  runEmitOnly modName mname sname evname target thmName
    (emitOneObligation modName mname sname evname target isDefault
      usingNames hasPayload varNames lemmaInvNames machineNames eventNames
      axiomNames proofTag proofIdx)
    acc

/-- Emit one base-case obligation for a single invariant in a directive's
target lemma. `mname`/`sname`/`evname` are recorded as `anonymous` —
base-case VCs are pmodule-scoped, not handler-scoped. -/
private def processBaseCaseEmit (modName invName : Name) (isDefaultInv : Bool)
    (machineNames : Array Name)
    (eventNames : Array Name)
    (axiomNames : Array Name)
    (proofTag : Name) (proofIdx : Nat)
    (acc : SynthesiseResult) :
    CommandElabM (SynthesiseResult × PendingObligation) := do
  let thmName := baseCaseName invName proofTag proofIdx
  runEmitOnly modName Name.anonymous Name.anonymous Name.anonymous
    invName thmName
    (emitBaseCaseObligation modName invName isDefaultInv machineNames
      eventNames axiomNames proofTag proofIdx)
    acc

/-- For each `Proof` block's `prove X` directive, walk every
`(machine, state, event)` and emit an obligation. After the user's
directives, auto-emit a `prove default;` obligation for every
`(M, S, ev)` not already covered.

Runs in two passes: pass 1 emits every obligation (queueing body
elaboration on worker threads under `Elab.async = true`); pass 2
reads each theorem's value from the env, blocking on its own
body-elab Task. The two-pass split is what lets the bodies overlap
— a single-pass classify-after-each-emit serialises them. -/
def synthesise (modName : Name) (ctx : LocalPModuleCtx) :
    CommandElabM SynthesiseResult := do
  detectUsingCycles ctx
  Verify.cexNameCtxRef.set (some (← buildCexNameCtx modName ctx))
  let mut result : SynthesiseResult := {}
  let mut explicitDefault : Std.HashSet (Name × Name × Name) := {}
  -- Preserves emission order so the report is deterministic.
  let mut pendings : Array PendingObligation := #[]
  let lemmaInvariantsOf (n : Name) : Array Name :=
    match ctx.lemmas.find? n with
    | some l => l.invariants
    | none   => #[]
  let allMachineNames : Array Name := Id.run do
    let mut out : Array Name := #[]
    for mn in ctx.machineOrder do
      if let some md := ctx.machines.find? mn then
        if !md.isSpec then out := out.push mn
    return out
  -- Events whose `<ev>_payload_of_spec` lemma was actually emitted by
  -- `#gen_module`. Currently `emitPayloadCharacterizations` only emits
  -- the lemma for named-tuple payloads, so an event with a single-type
  -- payload (e.g. `event ePing : MachineRef`) is excluded — otherwise
  -- the obligation generator would emit a `have hspec_<ev> := <ev>_payload_of_spec`
  -- referencing a constant that doesn't exist.
  let env ← getEnv
  let allEventNames : Array Name := Id.run do
    let mut out : Array Name := #[]
    for en in ctx.eventOrder do
      if let some e := ctx.events.find? en then
        unless e.payload.isSome do continue
        let specName : Name :=
          modName ++ Name.mkSimple (en.toString ++ "_payload_of_spec")
        if env.contains specName then out := out.push en
    return out
  let allAxiomNames : Array Name := Id.run do
    let mut out : Array Name := #[]
    for (_, d) in ctx.axioms.toList do
      out := out.push d.leanName
    return out
  let defaultInvNames : Array Name :=
    #[`UniqueActions, `IncreasingCount, `ReceivedSubsetSent]
  -- ── Pass 1: emit every obligation ─────────────────────────────────
  for hProof : proofIdx in [0:ctx.proofs.size] do
    let proof := ctx.proofs[proofIdx]'hProof.upper
    for dir in proof.directives do
      -- Base case: one VC per individual invariant in the target
      -- lemma's bundle (or per default-invariant for `prove default`).
      -- Premises (`using P`) get a base-case VC only when they are
      -- themselves a `prove` target — matching PVerifier.
      let baseInvs : Array Name :=
        if dir.isDefault then defaultInvNames
        else lemmaInvariantsOf dir.target
      for inv in baseInvs do
        let (result', pending) ← processBaseCaseEmit modName inv dir.isDefault
          allMachineNames allEventNames allAxiomNames proof.name proofIdx result
        result := result'
        pendings := pendings.push pending
      -- Inductive step: per-handler triples.
      for mname in ctx.machineOrder do
        let some m := ctx.machines.find? mname | continue
        if m.isSpec then
          logInfo m!"spec machine `{mname}` skipped — spec obligations are not yet supported"
          continue
        let varNames := machineVarNames m
        let gotoHandlers := machineGotoHandlers m
        for sd in m.states do
          for ev in sd.handles do
            if gotoHandlers.contains (sd.name, ev) then continue
            let hasPayload := eventHasPayload ctx ev
            if dir.isDefault then
              explicitDefault := explicitDefault.insert (mname, sd.name, ev)
            -- Target lemma's invariants + each `using`-lemma's get
            -- unfolded individually. Duplicates are harmless.
            let mut lemmaInvNames : Array Name :=
              if dir.isDefault then #[] else lemmaInvariantsOf dir.target
            for u in dir.usingLemmas do
              lemmaInvNames := lemmaInvNames ++ lemmaInvariantsOf u
            let (result', pending) ← processOneEmit modName mname sd.name ev
              dir.target dir.isDefault dir.usingLemmas hasPayload varNames
              lemmaInvNames allMachineNames allEventNames allAxiomNames
              proof.name proofIdx result
            result := result'
            pendings := pendings.push pending
  -- Auto-default pass: synthetic `block_auto_default` tag avoids
  -- collisions with user-tagged emissions; index past-the-end of the
  -- proofs array. No base case emitted here — the default invariants
  -- hold vacuously at init, so duplicating per (M, S, ev) gap would
  -- only pad the report.
  let autoTag : Name := `block_auto_default
  let autoIdx : Nat := ctx.proofs.size
  for mname in ctx.machineOrder do
    let some m := ctx.machines.find? mname | continue
    if m.isSpec then continue
    let varNames := machineVarNames m
    let gotoHandlers := machineGotoHandlers m
    for sd in m.states do
      for ev in sd.handles do
        if gotoHandlers.contains (sd.name, ev) then continue
        if explicitDefault.contains (mname, sd.name, ev) then continue
        let hasPayload := eventHasPayload ctx ev
        let (result', pending) ← processOneEmit modName mname sd.name ev
          `default true #[] hasPayload varNames
          #[] allMachineNames allEventNames allAxiomNames autoTag autoIdx result
        result := result'
        pendings := pendings.push pending
  -- Pass 2: each `classifyOnePending` blocks on its own body-elab
  -- Task; work overlaps across tasks already running in Lean's
  -- worker pool.
  for pending in pendings do
    result ← classifyOnePending pending result
  return result

end Verify
end PLean
