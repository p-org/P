/-
PLean.Verify.Obligation — synthesise per-handler Hoare-triple
obligations from registry data.

For each `Proof { prove X using Y, …; }` directive in the registry,
walk every (machine, state, handler) triple and emit one Hoare-triple
theorem per (handler, target lemma) pair: `triple <pre> <handler>
<post>`. The precondition conjoins target + using-lemmas + the three
default invariants + `InitConditions` + the dispatcher contract; the
post drops the using-lemmas. `prove default;` swaps the bundle for
`DefaultInvariants` and the discharger for `pverify_default`.

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

open Lean Elab Command

namespace PLean
namespace Verify

/-! ## Helpers -/

private def idSig : Ident := mkIdent `Sig

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

/-- Result of attempting to discharge one obligation. -/
inductive ObligationOutcome where
  | provedBySmt        -- closed by the auto `pverify` (incl. SMT) path
  | userProved         -- user supplied a @[pverifyProof] theorem
  | failed             -- tactic failed; user must write a manual proof
  deriving Inhabited, Repr

/-- Build the per-handler theorem and elaborate it. The proof tactic
is `pverify` for non-default lemmas, `pverify_default` for
`prove default`.

`varNames` is the list of `var` names declared on the owning machine
— their `<v>_get` / `<v>_set` accessors get added to the `unfold` list
so `wpgen` can step through them. `lemmaInvNames` is the target
lemma's per-invariant defs, similarly unfolded. -/
def emitOneObligation (modName : Name) (mname sname evname : Name)
    (target : Name) (isDefault : Bool) (usingNames : Array Name)
    (hasPayload : Bool) (varNames : Array Name)
    (lemmaInvNames : Array Name)
    (proofTag : Name) (proofIdx : Nat) :
    CommandElabM ObligationOutcome := do
  let thmName : Name :=
    obligationName mname sname evname target isDefault usingNames proofTag proofIdx
  -- The `@[pverifyProof]` registry is keyed on the fully-qualified
  -- theorem name (`<Mod>.<thmName>` — the obligation generator emits
  -- inside the `<Mod>` namespace).
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
    let initsId : Ident := mkIdent `InitConditions
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
        #[lemmaPred] ++ usingPreds ++ #[defaultPred, ← `($initsId)]
      buildConjAt preds sId
    -- The dispatcher contract: an existential `∃ lbl, inflight … ∧ …`
    -- witnessing the framework's runtime guarantee that this handler
    -- only fires when an inflight label of the right shape exists.
    -- Handlers don't re-establish the contract on exit, so it appears
    -- only in the precondition.
    let dispatcherClause ← do
      if hasPayload then
        `(∃ lbl : ($idSig).Label,
            PLean.inflight lbl s ∧
            lbl.target = this.ref ∧
            (s.machines this.ref).currentState = $stateAlias ∧
            lbl.action = .event ($evCtor param))
      else
        `(∃ lbl : ($idSig).Label,
            PLean.inflight lbl s ∧
            lbl.target = this.ref ∧
            (s.machines this.ref).currentState = $stateAlias ∧
            lbl.action = .event $evCtor)
    let preTerm : TSyntax `term ← `(fun (s : PLean.GlobalState $idSig) =>
                                      $basePre ∧ $dispatcherClause)
    let postBody ← buildConjAt #[lemmaPred, defaultPred, ← `($initsId)] sId
    let postTerm : TSyntax `term ← `(fun (_ : Unit) (s : PLean.GlobalState $idSig) =>
                                       $postBody)
    let handlerTerm : TSyntax `term ←
      if hasPayload then
        `($handlerId this param)
      else
        `($handlerId this)
    let handlerUnfold : Ident := mkIdent handlerName
    let lemmaUnfold : Ident :=
      if isDefault then mkIdent ``PLean.DefaultInvariants
      else                mkIdent target
    let initsUnfold : Ident := mkIdent `InitConditions
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
    let hasAccessors := !accessorUnfolds.isEmpty
    let hasUsings    := !usingUnfolds.isEmpty
    let tail : TSyntax `tactic ←
      if isDefault then `(tactic| pverify_default)
      else                `(tactic| pverify)
    -- Build the proof tactic sequence programmatically. WHY this order:
    -- handler → target lemma → InitConditions → using-lemmas → per-machine
    -- accessors → PLean primitives → final tactic. Accessors must
    -- precede primitives because reversing the order trips `wpgen` into
    -- `WPGen.default` (see PVerifyConditional regression). The
    -- `isDefault` branch skips the lemma unfold — `lemmaPred` is
    -- `DefaultInvariants` and `target` is the literal `default`, which
    -- the `default_inv` tactic unfolds itself.
    let mut steps : Array (TSyntax `tactic) := #[]
    steps := steps.push (← `(tactic| unfold $handlerUnfold:ident))
    unless isDefault do
      steps := steps.push (← `(tactic| try unfold $lemmaUnfold:ident))
    steps := steps.push (← `(tactic| try unfold $initsUnfold:ident))
    if hasUsings then
      steps := steps.push (← `(tactic| try unfold $[$usingUnfolds:ident]*))
    if hasAccessors then
      steps := steps.push (← `(tactic| try unfold $[$accessorUnfolds:ident]*))
    steps := steps.push (← `(tactic|
      try unfold PLean.send PLean.goto PLean.raise
                 PLean.markReceived PLean.announce))
    steps := steps.push tail
    let proofTacSeq : TSyntax ``Lean.Parser.Tactic.tacticSeq ←
      `(Lean.Parser.Tactic.tacticSeq| $[$steps]*)
    -- Wrap in `first | <chain> | sorry` so elaboration always
    -- succeeds; on tactic failure Lean inserts a `sorryAx` into the
    -- value, which the post-elaboration `hasSorry` check below treats
    -- as a failed obligation. The IDE may still surface the inner
    -- tactic error as an informational hint; the synthesise loop
    -- emits the consolidated "supply @[pverifyProof]" warning.
    let wrappedProof : TSyntax ``Lean.Parser.Tactic.tacticSeq ←
      `(Lean.Parser.Tactic.tacticSeq|
          first | ($proofTacSeq) | sorry)
    if hasPayload then
      `(set_option linter.unusedTactic false in
        theorem $thmId
            (this : $mIdent) (param : $payloadTy) :
            triple (l := $prpAbbrev) $preTerm $handlerTerm $postTerm := by
          $wrappedProof)
    else
      `(set_option linter.unusedTactic false in
        theorem $thmId
            (this : $mIdent) :
            triple (l := $prpAbbrev) $preTerm $handlerTerm $postTerm := by
          $wrappedProof)
  elabCommand stx
  -- A `sorryAx` in the elaborated value means the tactic chain fell
  -- through to the `first | … | sorry` fallback. This catches both
  -- synchronous and async-snapshot tactic errors that message-log
  -- inspection alone would miss.
  let env ← getEnv
  match env.find? fullThmName with
  | some (.thmInfo info) =>
    if info.value.hasSorry then return .failed
    return .provedBySmt
  | _ => return .failed

/-! ## Walking the registry — `synthesise` is the entry point. -/

/-- Per-obligation outcome counts. The failure array carries
`(machine, state, ev, lemma, theoremName)` for the report. -/
structure SynthesiseResult where
  attempted   : Nat := 0
  smtProved   : Nat := 0
  userProved  : Nat := 0
  failed      : Array (Name × Name × Name × Name × Name) := #[]
  deriving Inhabited

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

/-- Process one obligation, emitting it (or skipping if user-proved)
and capturing the outcome. Synchronous error messages from elaboration
are dropped — the consolidated "obligation incomplete; supply
`@[pverifyProof]`" warning is what the user sees. -/
private def processOne (modName mname sname evname : Name)
    (target : Name) (isDefault : Bool) (usingNames : Array Name)
    (hasPayload : Bool) (varNames : Array Name)
    (lemmaInvNames : Array Name)
    (proofTag : Name) (proofIdx : Nat)
    (acc : SynthesiseResult) : CommandElabM SynthesiseResult := do
  let acc := { acc with attempted := acc.attempted + 1 }
  let thmName :=
    obligationName mname sname evname target isDefault usingNames proofTag proofIdx
  let savedSt ← get
  let outcome ← try
    emitOneObligation modName mname sname evname target isDefault
      usingNames hasPayload varNames lemmaInvNames proofTag proofIdx
  catch e =>
    let errMsg ← e.toMessageData.toString
    logWarning m!"obligation failed for {mname}.{sname}.{evname} (lemma {target}): {errMsg}"
    pure ObligationOutcome.failed
  -- Drop synchronous error messages from elaboration — the `hasSorry`
  -- check already accounted for them. Keep info / warning entries.
  let curSt ← get
  let preMsgsArr := savedSt.messages.toArray
  let postMsgsArr := curSt.messages.toArray
  let newMsgs := postMsgsArr.extract preMsgsArr.size postMsgsArr.size
  let hadSyncError := newMsgs.any
    (fun (m : Lean.Message) => m.severity matches .error)
  if hadSyncError then
    let kept := newMsgs.filter
      (fun (m : Lean.Message) => !(m.severity matches .error))
    let mergedMsgs : MessageLog :=
      kept.foldl (init := savedSt.messages) (fun ml m => ml.add m)
    modify fun st => { st with messages := mergedMsgs }
  match outcome with
  | .userProved =>
    return { acc with userProved := acc.userProved + 1 }
  | .failed =>
    logWarning m!"obligation incomplete for {mname}.{sname}.{evname} \
                  (lemma {target}); SMT could not close. Write a \
                  `@[pverifyProof] theorem {thmName} : ... := by ...` \
                  to supply a manual proof."
    return { acc with
      failed := acc.failed.push (mname, sname, evname, target, thmName) }
  | .provedBySmt =>
    return { acc with smtProved := acc.smtProved + 1 }

/-- For each `Proof` block's `prove X` directive, walk every
`(machine, state, event)` and emit an obligation. After the user's
directives, auto-emit a `prove default;` obligation for every
`(M, S, ev)` not already covered. -/
def synthesise (modName : Name) (ctx : LocalPModuleCtx) :
    CommandElabM SynthesiseResult := do
  detectUsingCycles ctx
  let mut result : SynthesiseResult := {}
  let mut explicitDefault : Std.HashSet (Name × Name × Name) := {}
  -- Per-lemma invariant-name lookup used to feed `lemmaInvNames` into
  -- `processOne`: the obligation generator unfolds each individual
  -- invariant in addition to the bundle predicate.
  let lemmaInvariantsOf (n : Name) : Array Name :=
    match ctx.lemmas.find? n with
    | some l => l.invariants
    | none   => #[]
  for hProof : proofIdx in [0:ctx.proofs.size] do
    let proof := ctx.proofs[proofIdx]'hProof.upper
    for dir in proof.directives do
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
              lemmaInvNames proof.name proofIdx result
  -- Auto-default pass: synthetic `block_auto_default` tag avoids
  -- collisions with user-tagged emissions; index past-the-end of the
  -- proofs array.
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
          #[] autoTag autoIdx result
  return result

end Verify
end PLean
