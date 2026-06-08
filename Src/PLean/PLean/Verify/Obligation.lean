/-
PLean.Verify.Obligation — synthesise per-handler Hoare-triple
obligations from registry data (D18, D23, D24, D25).

For every `Proof <name>? { prove X using Y, Z; ... }` directive in the
pmodule, we emit one obligation per (machine, state, handler) triple:

  theorem <Mod>.<M>.<S>.<ev>_correct_<X>
      (this : <M>) (param : <ev>_payload) :
      triple (l := PProp Sig)
        (fun s =>
          (X) s ∧
          (Y) s ∧ (Z) s ∧
          InitConditions s ∧
          ∃ lbl, inflight lbl s ∧ lbl.target = this.ref ∧
                 (s.machines this.ref).currentState = <S>_st ∧
                 lbl.action = .event (E.<ev> param))
        (<M>.<S>.<ev>_handler this param)
        (fun _ s => (X) s ∧ InitConditions s) := by
    pverify

For `prove default;` (no `X` lemma), the bundle expands to
`DefaultInvariants` and the proof tactic switches to `pverify_default`.

PLAN_P3 D18 / D23 / D24 / D25 are the design decisions; this file
materialises them.
-/
import Lean
import PLean.Internal.Decls
import PLean.Internal.Registry
import PLean.Surface.Machine
import PLean.Verify.Tactic
-- NOTE: `Verify/DispatcherContract.lean` was previously imported here;
-- its `buildDispatcherContractTerm` helper was inert (the obligation
-- generator builds the dispatcher clause inline below). REVIEW_P3 §2.3
-- flagged this as "code that loads but doesn't run"; the import is
-- dropped to remove the false signal. The file is retained as a
-- docstring source-of-truth for the dispatcher-contract design.

open Lean Elab Command

namespace PLean
namespace Verify

/-! ## Helpers -/

private def idSig : Ident := mkIdent `Sig
-- `idGS` and `idPP` were dead code; obligations reach `GS` and `PProp`
-- via `PLean.GlobalState $idSig` / `PProp $idSig` directly. Removed in
-- the second-pass review sweep (REVIEW_P3 §2.4).

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

/-! ## One per-handler obligation

Given a `(machine, state, event)` triple and a `prove X using Y,Z`
directive, build and elaborate the obligation theorem. -/

/-- Whether the event has a payload (used to pick the dispatcher
contract shape and the handler param list). -/
private def eventHasPayload (ctx : LocalPModuleCtx) (evName : Name) : Bool :=
  match ctx.events.find? evName with
  | some e => e.payload.isSome
  | none   => false

/-- Build the per-handler theorem and elaborate it. The proof tactic
is `pverify` for non-default lemmas and `pverify_default` for
`prove default`. Failures are captured by the caller (R19).

`varNames` is the list of `var` names declared on the owning machine
— their `<v>_get` / `<v>_set` accessors get added to the `unfold` list
so `wpgen` can step through them.

`proofTag` is the owning `Proof` block's tag (`Name.anonymous` if the
block was anonymous) and `proofIdx` is its 0-based index across all
`Proof` blocks. Both are embedded in the theorem name to keep emissions
unique across multiple `Proof` blocks targeting the same lemma
(REVIEW_P3 §A.1: two `Proof { prove safety; }` blocks would otherwise
collide on `<M>.<S>.<ev>_correct_safety`). The tag is used when the
user provided one; the index is the deterministic fallback. -/
def emitOneObligation (modName : Name) (mname sname evname : Name)
    (target : Name) (isDefault : Bool) (usingNames : Array Name)
    (hasPayload : Bool) (varNames : Array Name)
    (proofTag : Name) (proofIdx : Nat) :
    CommandElabM Unit := do
  let _ := modName
  -- Theorem name. For uniqueness:
  --   <M>.<S>.<ev>_correct_<P>_<target>[_using_<L1>_<L2>...]
  -- where <P> = the Proof-block tag (if non-anonymous) or
  -- `block<idx>` for anonymous blocks. The index disambiguates two
  -- anonymous blocks; the tag is human-readable when present
  -- (REVIEW_P3 §A.1 / §5.2).
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
  let thmName : Name :=
    mname ++ sname ++ Name.mkSimple ((evname.toString) ++ "_correct_" ++ tail)
  let thmId : Ident := mkIdent thmName
  -- Identifiers for the handler def (`<M>.<S>.<ev>_handler`).
  let handlerName : Name :=
    mname ++ sname ++ (evname.appendAfter "_handler")
  let handlerId : Ident := mkIdent handlerName
  let mIdent : Ident := mkIdent mname
  let stateAlias : Ident := mkIdent (sname.appendAfter "_st")
  let evCtor : Ident := mkIdent (`E ++ evname)
  -- Build the term-level pieces inside MacroM so we can splice.
  let stx ← liftMacroM do
    let lemmaPred ← lemmaPredIdent target isDefault
    let usingPreds ← usingPredIdents usingNames
    let initsId : Ident := mkIdent `InitConditions
    let payloadTy := mkIdent (evname.appendAfter "_payload")
    let prpAbbrev : TSyntax `term := ← `(PProp $idSig)
    -- PLAN_P3 D18: pre/post both include `DefaultInvariants` so the
    -- three sanity invariants flow through every obligation. Without
    -- this, a `prove safety` chain would be free to violate
    -- `UniqueActions` / `IncreasingCount` / `ReceivedSubsetSent`
    -- between handler firings as long as user-`safety` survives
    -- (REVIEW_P3 §A.3). For `prove default;` directives `lemmaPred`
    -- *is* `DefaultInvariants`, so the duplicate is harmless (same
    -- predicate appears twice in the conjunction).
    --
    -- Implication-chain equivalence (REVIEW_P3 second-pass): a user
    -- invariant body written as `A → B → C → D` is logically
    -- identical to `A ∧ B ∧ C → D` (Lean's `→` curries). Grind /
    -- omega in `pverify_solve` treats both shapes the same; we
    -- therefore don't normalise — the user can write whichever shape
    -- is most natural in the `invariant <name> : <prop>` body.
    let defaultPred : TSyntax `term ← `(PLean.DefaultInvariants)
    -- Pre-condition (s) := <lemmaPred> s ∧ <usingPreds> s ∧
    --                      DefaultInvariants s ∧ InitConditions s ∧
    --                      <dispatcher contract>.
    let sId : TSyntax `term := ← `(s)
    let basePre ← do
      let preds : Array (TSyntax `term) :=
        #[lemmaPred] ++ usingPreds ++ #[defaultPred, ← `($initsId)]
      buildConjAt preds sId
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
    -- Post-condition (s) := <lemmaPred> s ∧ DefaultInvariants s ∧
    --                       InitConditions s. Same shape as the pre,
    --                       minus the dispatcher contract (handlers
    --                       don't re-establish the inflight label —
    --                       that's the runtime's contract).
    let postBody ← buildConjAt #[lemmaPred, defaultPred, ← `($initsId)] sId
    let postTerm : TSyntax `term ← `(fun (_ : Unit) (s : PLean.GlobalState $idSig) =>
                                       $postBody)
    -- Handler term and proof tactic.
    let handlerTerm : TSyntax `term ←
      if hasPayload then
        `($handlerId this param)
      else
        `($handlerId this)
    -- The handler def name. We unfold it before `wpgen` so the body
    -- (do-notation over `_get`/`_set`/`send`/`raise`/`goto`/`if`)
    -- becomes visible.
    let handlerUnfold : Ident := mkIdent handlerName
    -- The lemma bundle predicate name (for non-default lemmas) — unfold
    -- so `pverify` can simp through the conjunction.
    let lemmaUnfold : Ident :=
      if isDefault then mkIdent ``PLean.DefaultInvariants
      else                mkIdent target
    let initsUnfold : Ident := mkIdent `InitConditions
    -- Build var-accessor unfold idents `<MachineName>.<v>_get` /
    -- `<MachineName>.<v>_set`. Accessors live in the per-machine
    -- namespace; from the pmodule namespace we reach them via
    -- `<MachineName>.<accessor>`.
    let mut accessorUnfolds : Array Ident := #[]
    for v in varNames do
      accessorUnfolds := accessorUnfolds.push
        (mkIdent (mname ++ (v.appendAfter "_get")))
      accessorUnfolds := accessorUnfolds.push
        (mkIdent (mname ++ (v.appendAfter "_set")))
    -- Build using-lemma unfolds (REVIEW_P3 §1.2 / §1.4). The using
    -- lemmas were added to the precondition above as opaque bundle
    -- predicates; without unfolding them in the proof tail, `pverify`'s
    -- grind step can't see the assumed lemma's invariant clauses
    -- (D25). Mirror the pattern used for `accessorUnfolds`.
    let usingUnfolds : Array Ident := usingNames.map mkIdent
    let hasAccessors := !accessorUnfolds.isEmpty
    let hasUsings    := !usingUnfolds.isEmpty
    -- Unfold ordering convention (kept consistent across all 8 cases):
    --   handler def → target lemma → InitConditions → using-lemmas →
    --   PLean primitives → per-machine accessors → final tactic.
    -- `usingUnfolds` lands *before* `accessorUnfolds`/primitives so the
    -- assumed-lemma's invariant clauses become visible first; the
    -- subsequent unfolds may then push into bodies those invariants
    -- mention. (REVIEW_P3 §1.2 fix; ordering pinned consistently to
    -- avoid the asymmetry the second-pass review flagged.)
    let proofTacSeq : TSyntax ``Lean.Parser.Tactic.tacticSeq ←
      match isDefault, hasAccessors, hasUsings with
      | true,  false, false =>
        `(Lean.Parser.Tactic.tacticSeq|
            unfold $handlerUnfold:ident
            try unfold $initsUnfold:ident
            try unfold PLean.send PLean.goto PLean.raise
                       PLean.markReceived PLean.announce
            pverify_default)
      | true,  true,  false =>
        `(Lean.Parser.Tactic.tacticSeq|
            unfold $handlerUnfold:ident
            try unfold $initsUnfold:ident
            try unfold PLean.send PLean.goto PLean.raise
                       PLean.markReceived PLean.announce
            try unfold $[$accessorUnfolds:ident]*
            pverify_default)
      | true,  false, true =>
        `(Lean.Parser.Tactic.tacticSeq|
            unfold $handlerUnfold:ident
            try unfold $initsUnfold:ident
            try unfold $[$usingUnfolds:ident]*
            try unfold PLean.send PLean.goto PLean.raise
                       PLean.markReceived PLean.announce
            pverify_default)
      | true,  true,  true =>
        `(Lean.Parser.Tactic.tacticSeq|
            unfold $handlerUnfold:ident
            try unfold $initsUnfold:ident
            try unfold $[$usingUnfolds:ident]*
            try unfold PLean.send PLean.goto PLean.raise
                       PLean.markReceived PLean.announce
            try unfold $[$accessorUnfolds:ident]*
            pverify_default)
      | false, false, false =>
        `(Lean.Parser.Tactic.tacticSeq|
            unfold $handlerUnfold:ident
            try unfold $lemmaUnfold:ident
            try unfold $initsUnfold:ident
            try unfold PLean.send PLean.goto PLean.raise
                       PLean.markReceived PLean.announce
            pverify)
      | false, true,  false =>
        `(Lean.Parser.Tactic.tacticSeq|
            unfold $handlerUnfold:ident
            try unfold $lemmaUnfold:ident
            try unfold $initsUnfold:ident
            try unfold PLean.send PLean.goto PLean.raise
                       PLean.markReceived PLean.announce
            try unfold $[$accessorUnfolds:ident]*
            pverify)
      | false, false, true =>
        `(Lean.Parser.Tactic.tacticSeq|
            unfold $handlerUnfold:ident
            try unfold $lemmaUnfold:ident
            try unfold $initsUnfold:ident
            try unfold $[$usingUnfolds:ident]*
            try unfold PLean.send PLean.goto PLean.raise
                       PLean.markReceived PLean.announce
            pverify)
      | false, true,  true =>
        `(Lean.Parser.Tactic.tacticSeq|
            unfold $handlerUnfold:ident
            try unfold $lemmaUnfold:ident
            try unfold $initsUnfold:ident
            try unfold $[$usingUnfolds:ident]*
            try unfold PLean.send PLean.goto PLean.raise
                       PLean.markReceived PLean.announce
            try unfold $[$accessorUnfolds:ident]*
            pverify)
    if hasPayload then
      `(theorem $thmId
            (this : $mIdent) (param : $payloadTy) :
            triple (l := $prpAbbrev) $preTerm $handlerTerm $postTerm := by
          $proofTacSeq)
    else
      `(theorem $thmId
            (this : $mIdent) :
            triple (l := $prpAbbrev) $preTerm $handlerTerm $postTerm := by
          $proofTacSeq)
  elabCommand stx

/-! ## Walking the registry

`synthesise` is the entry point called by `#pverify`. -/

/-- Result of running synthesise: counts of attempted / succeeded /
failed obligations, with failure detail for the report. -/
structure SynthesiseResult where
  attempted : Nat := 0
  failed    : Array (Name × Name × Name × Name) := #[]
  -- (machine, state, ev, lemma)
  deriving Inhabited

/-! ## Cycle detection on the `using` graph (R17).

Build a directed graph `target → using-lemma` from every `prove X
using Y, Z;` directive across all `Proof` blocks. Run DFS; throw on
a back-edge so a user writing `prove A using B; prove B using A;`
gets a clear error rather than an unsoundness (each obligation's
precondition would otherwise be strengthened by the *other* lemma's
invariants). -/

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
  -- Build target → using-lemma adjacency. `default` has no edges.
  let mut graph : NameMap (Array Name) := {}
  for proof in ctx.proofs do
    for dir in proof.directives do
      if dir.isDefault then continue
      let prev := (graph.find? dir.target).getD #[]
      graph := graph.insert dir.target (prev ++ dir.usingLemmas)
  let mut visited : NameSet := {}
  for (node, _) in graph.toList do
    visited ← dfsCheck graph node #[] visited

/-- For each `Proof` block's `prove X` directive, walk every
`(machine, state, event)` and emit an obligation. Each emission is
wrapped in error-log capture so a failing tactic doesn't abort the
whole batch (R19) — failures land in the result for the report.

Before walking, we run a DFS over the `using` graph to reject cycles
(R17): `prove A using B; prove B using A;` is unsound because each
obligation's precondition would borrow strength from the other.

Implementation note: a `tactic` failure inside `theorem ... := by ...`
shows up as `logError` calls in the message log rather than as a
thrown exception, so we capture the log between emissions and
inspect for new errors. -/
def synthesise (modName : Name) (ctx : LocalPModuleCtx) :
    CommandElabM SynthesiseResult := do
  detectUsingCycles ctx
  let mut result : SynthesiseResult := {}
  for hProof : proofIdx in [0:ctx.proofs.size] do
    let proof := ctx.proofs[proofIdx]'hProof.upper
    for dir in proof.directives do
      for mname in ctx.machineOrder do
        let some m := ctx.machines.find? mname | continue
        if m.isSpec then
          -- REVIEW_P3 §B.2: log the skip so the user knows their
          -- `Proof { prove X; }` directive on a `spec` machine isn't
          -- silently dropped. Phase 4 owns spec verification.
          logInfo m!"spec machine `{mname}` skipped — Phase 4 owns spec obligations"
          continue
        -- Collect the machine's `var` names (for accessor-unfolding
        -- in the proof tactic).
        let varNames : Array Name := Id.run do
          let mut out : Array Name := #[]
          for it in m.body do
            if it.getKind == ``PLean.pMachineVar then
              -- `var <ident> : <term>` — child index 1 is the ident.
              if let some i := it[1]? then
                if i.isIdent then out := out.push i.getId
          return out
        for sd in m.states do
          for ev in sd.handles do
            -- Skip pure-goto handlers (they have no body def to verify).
            if sd.gotos.contains ev then
              continue
            let hasPayload := eventHasPayload ctx ev
            result := { result with attempted := result.attempted + 1 }
            let savedSt ← get
            try
              emitOneObligation modName mname sd.name ev
                dir.target dir.isDefault dir.usingLemmas hasPayload varNames
                proof.name proofIdx
            catch e =>
              let errMsg ← e.toMessageData.toString
              logWarning m!"obligation failed for {mname}.{sd.name}.{ev} (lemma {dir.target}): {errMsg}"
              result := { result with
                failed := result.failed.push (mname, sd.name, ev, dir.target) }
            -- Inspect newly-added messages — tactic errors emitted by
            -- `pverify` if it couldn't close all goals.
            let curSt ← get
            let preMsgsArr := savedSt.messages.toArray
            let postMsgsArr := curSt.messages.toArray
            let newMsgs := postMsgsArr.extract preMsgsArr.size postMsgsArr.size
            let hasError := newMsgs.any
              (fun (m : Lean.Message) => m.severity matches .error)
            -- Note: in modern Lean 4, `theorem ... := by ...` errors are
            -- often reported asynchronously via the snapshot system,
            -- which means they don't appear in `(← get).messages` at
            -- this point. We capture what we can; remaining failures
            -- surface as build-level errors. Additionally (REVIEW_P3
            -- §B.3), this rollback restores the message log only — a
            -- failed `theorem` whose elaboration progressed past
            -- type-checking but stalled in `by ...` may still leak a
            -- partially-elaborated decl into the env. Phase-3 ships
            -- with both leaks documented; full async-snapshot + env
            -- rollback is a follow-up. (PLAN_P3 R19 explicit
            -- caveat — full async-snapshot capture is a follow-up).
            if hasError then
              -- REVIEW_P3 second-pass §2.1: only filter out the new
              -- *error* messages — keep info/warning entries that
              -- landed during this emission (e.g., the spec-skip
              -- `logInfo`s from §B.2, or other diagnostics emitted
              -- before the tactic fault). A blanket reset to
              -- `savedSt.messages` would erase those.
              let kept := newMsgs.filter
                (fun (m : Lean.Message) => !(m.severity matches .error))
              let mergedMsgs : MessageLog :=
                kept.foldl (init := savedSt.messages) (fun ml m => ml.add m)
              modify fun st => { st with messages := mergedMsgs }
              result := { result with
                failed := result.failed.push (mname, sd.name, ev, dir.target) }
              logWarning m!"obligation incomplete for {mname}.{sd.name}.{ev} (lemma {dir.target})"
  return result

end Verify
end PLean
