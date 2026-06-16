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
defs (similarly unfolded). -/
def emitOneObligation (modName : Name) (mname sname evname : Name)
    (target : Name) (isDefault : Bool) (usingNames : Array Name)
    (hasPayload : Bool) (varNames : Array Name)
    (lemmaInvNames : Array Name)
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
  -- inspection alone would miss. The fine-grained sub-classification
  -- (disproved / unknown / tacticError) is added by `processOne`,
  -- which has access to the message-log slice.
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

/-- Cap a multi-line diagnostic at ~12 lines and 1500 chars so the
report doesn't degenerate into a wall of solver model output. -/
private def truncateForReport (s : String) : String :=
  let lines := s.splitOn "\n"
  let joined := String.intercalate "\n" (lines.take 12)
  if joined.length > 1500 then joined.take 1500 ++ " …" else joined

/-- Classify a failure from the diagnostic refs set during emission.
The SMT diagnostic discriminates `sat` (counter-example) from
`unknown` (incomplete theory / timeout); anything else collapses to
`tacticError`. -/
private def classifyFailure : CommandElabM ObligationOutcome := do
  if let some smtMsg ← pverifySmtDiagRef.get then
    if hasSubstring smtMsg "the goal is false" then
      return .disproved (truncateForReport smtMsg)
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

/-- Emit one obligation, classify the outcome, and append a record to
the running `SynthesiseResult`. Errors logged during elaboration are
filtered out of the message log; the structured report consumes the
diagnostic refs directly. -/
private def processOne (modName mname sname evname : Name)
    (target : Name) (isDefault : Bool) (usingNames : Array Name)
    (hasPayload : Bool) (varNames : Array Name)
    (lemmaInvNames : Array Name)
    (proofTag : Name) (proofIdx : Nat)
    (acc : SynthesiseResult) : CommandElabM SynthesiseResult := do
  let acc := { acc with attempted := acc.attempted + 1 }
  let thmName :=
    obligationName mname sname evname target isDefault usingNames proofTag proofIdx
  let fullThmName := modName ++ thmName
  pverifySmtDiagRef.set none
  pverifyTacDiagRef.set none
  let savedSt ← get
  let outcomeRaw ← try
    emitOneObligation modName mname sname evname target isDefault
      usingNames hasPayload varNames lemmaInvNames proofTag proofIdx
  catch e =>
    let errMsg ← e.toMessageData.toString
    pure (ObligationOutcome.tacticError (truncateForReport errMsg))
  let outcome ← match outcomeRaw with
    | .unfinished => classifyFailure
    | other       => pure other
  -- Drop sync-error messages from the slice; the diagnostic refs
  -- already carry what we need, and surfacing the raw tactic errors
  -- would just clutter the build log alongside our structured report.
  let curSt ← get
  let preMsgsArr := savedSt.messages.toArray
  let postMsgsArr := curSt.messages.toArray
  let newMsgs := postMsgsArr.extract preMsgsArr.size postMsgsArr.size
  if newMsgs.any (fun m => m.severity matches .error) then
    let kept := newMsgs.filter (fun m => !(m.severity matches .error))
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
