/-
PLean.Verify.Tactic — user-facing `pverify_*` tactics.

`#pverify M` is an SMT-discharge command; the tactics here drive
manual proofs of obligations SMT can't close (registered via
`@[pverifyProof]`). The pre-SMT simp set lives in
`Verify/SimpLemmas.lean` under the `pverifySimp` attribute.

User-facing tactics (a typical manual proof composes some of these):

- `pverify` — auto-discharge: step WP + close-chain.
- `pverify_step_wp` — run `wpgen` and clean up the resulting `WPGen`
  shapes, leaving a propositional VC.
- `pverify_smt_prep`, `pverify_smt` — pre-SMT normalisation
  and the SMT discharger.
- `pverify_split_smt`, `pverify_grind` — fallbacks for goals
  the single-shot solver doesn't decide.
- `default_inv` — discharges any of the four default-invariant
  constants by case analysis.
- Manual-proof helpers (see "Manual-proof helpers" section below):
  `pverify_carry_through`, `pverify_carry_after_recv`,
  `pverify_not_inflight`, `pverify_inflight_by`.

Macro-hygiene rule: every simp lemma name lives inside a named tactic
helper. The obligation generator and manual proofs both call the named
tactics; bare lemma references would get hygiene marks at expansion.
-/
import Lean
import Loom.MonadAlgebras.WP.Basic
import Loom.MonadAlgebras.WP.Tactic
import Loom.SMT
import Mathlib.Tactic.Tauto
import PLean.Semantics.Default
import PLean.Semantics.GlobalState
import PLean.Semantics.Predicates
import PLean.Semantics.Primitives
import PLean.Verify.SimpLemmas
import PLean.Verify.Profile

open Lean Elab Tactic Meta

namespace PLean

/-! ## Diagnostic refs

`pverify_log_failure_else_sorry` wraps each obligation's tactic chain.
On failure it stashes the diagnostic in `pverifyDiagMap` keyed by
obligation name and closes with `sorry`; the obligation generator
inspects the elaborated value for `sorry` and reads the diag map to
classify the outcome.

Keying by name lets concurrent obligation bodies (under `Elab.async =
true`) write to disjoint slots. The key is supplied via the
`pverify.obligationKey` option, set per-emission with
`set_option pverify.obligationKey "…" in theorem …`. -/

register_option pverify.obligationKey : String := {
  defValue := ""
  descr := "Per-obligation diagnostic key. The obligation generator \
            sets it via `set_option … in theorem …` at each emission \
            site so the tactic stashes its failure diagnostic under \
            that key. Empty falls back to a process-global default \
            cell — harmless when only one obligation is in flight."
}

/-- Diagnostic for one obligation. SMT diagnostics (counter-example
/ `unknown` reason) live in `smt`; non-SMT tactic errors (lean-auto
rejection, no-goals etc.) live in `tac`. -/
structure ObligationDiag where
  smt : Option String := none
  tac : Option String := none
  deriving Inhabited

initialize pverifyDiagMap : IO.Ref (Std.HashMap String ObligationDiag) ←
  IO.mkRef ∅

def resetDiagMap : IO Unit :=
  pverifyDiagMap.set ∅

def getDiag (key : String) : IO ObligationDiag := do
  return ((← pverifyDiagMap.get).get? key).getD {}

def modifyDiag (key : String) (f : ObligationDiag → ObligationDiag) :
    IO Unit := do
  pverifyDiagMap.modify fun m =>
    let cur := (m.get? key).getD {}
    m.insert key (f cur)

def currentObligationKey : Lean.Elab.Tactic.TacticM String := do
  return pverify.obligationKey.get (← Lean.getOptions)

/-- Run a tactic chain; on throw or unclosed goals, stash the
diagnostic in `pverifyDiagMap` under the current obligation key and
close with `sorry` so the enclosing `theorem … := by …` still
elaborates. -/
syntax "pverify_log_failure_else_sorry " tacticSeq : tactic
elab_rules : tactic
  | `(tactic| pverify_log_failure_else_sorry $ts:tacticSeq) =>
      withMainContext do
        let savedGoals ← getGoals
        let mut diagnostic : Option String := none
        try
          evalTactic (← `(tactic| ($ts:tacticSeq)))
        catch e =>
          diagnostic := some (← e.toMessageData.toString)
        if diagnostic.isNone && !(← getGoals).isEmpty then
          diagnostic := some
            "the `pverify` tactic chain did not close the goal"
        match diagnostic with
        | some msg =>
          setGoals savedGoals
          let key ← currentObligationKey
          liftM (modifyDiag key fun d => { d with tac := some msg })
          evalTactic (← `(tactic| sorry))
        | none => pure ()

/-- Run `wpgen` and clean up the post-`wpgen` plumbing.

After `wpgen`, the goal carries `WPGen.bind` / `WPGen.pure` shapes,
`GlobalState` update terms, and (for conditional handlers) Loom's
`WithName` / `iInf` machinery from `if_pos` / `if_neg` branches. We
strip each in sequence so the goal arrives at SMT prep as a clean
propositional fact. -/
syntax "pverify_step_wp" : tactic
macro_rules
  | `(tactic| pverify_step_wp) => `(tactic| (
      wpgen <;> first | apply WPGen.default | skip
      try simp only [loomLogicSimp, loomWpSimp, loomWPGenRewrite]
      try simp only [PLean.GlobalState.addSent, PLean.GlobalState.bumpActionCount,
                     PLean.GlobalState.addReceived, PLean.GlobalState.updateMachine]
      try simp only [WithName.mk', WithName.erase, typeWithName.erase]
      try simp only [iInf_apply, iInf_Prop_eq, iSup_apply, iSup_Prop_eq]
      try simp only [if_true, if_false]
    ))

/-! ## Pre-SMT preparation (internal to `pverify_smt`)

The next three tactics — `sdestruct_state`, `abstract_machine_lookups`,
and `pverify_smt_prep` — together transform a propositional VC into a
shape lean-auto can translate (no `GlobalState`-typed locals, no
compound `machines _` lookups under an opaque extractor). Manual
proofs call `pverify_smt`, which calls `pverify_smt_prep`, which
calls the other two; calling the internal tactics directly is rarely
useful. -/

/-- Destruct every `GlobalState`-typed local into its four fields,
making them top-level uninterpreted symbols rather than struct
projections. Without this, lean-auto rejects with "Higher order input?"
on the first `s.sent l` projection. Internal to `pverify_smt_prep`. -/
syntax "sdestruct_state" : tactic
elab_rules : tactic
  | `(tactic| sdestruct_state) => withMainContext do
    let lctx ← getLCtx
    for ldecl in lctx do
      if ldecl.isImplementationDetail then continue
      -- Whnf so abbreviations like `GS = GlobalState Sig` reduce to the head.
      let ty ← Lean.Meta.whnf (← Lean.Meta.inferType ldecl.toExpr)
      if ty.consumeMData.isAppOfArity ``PLean.GlobalState 1 then
        let hName := ldecl.userName
        -- Stable, unhygienic field names so downstream tactics in this
        -- file can refer to them by name. A second GlobalState local —
        -- rare at SMT time — just shadows these; harmless.
        let stx ← `(tactic|
          obtain ⟨$(mkIdent `gsSent), $(mkIdent `gsReceived),
                  $(mkIdent `gsMachines), $(mkIdent `gsActionCount)⟩ :=
            $(mkIdent hName))
        try evalTactic stx
        catch _ => pure ()

/-- Abstract `s.machines ((<ev>_payload_of e).<field>)` — a machines-
field lookup through an opaque payload extractor — to a fresh
`MachineState` local. Such compound lookups make lean-auto emit an
identity lambda and abort ("`Unexpected head term ... lam`"). The
generalisation removes the offending argument; the invariant body
then reads plain `MachineState` projections, which translate cleanly.
Internal to `pverify_smt_prep`.

Gated on the argument mentioning a `…_payload_of` constant so it
doesn't fire on ordinary `s.machines n.ref` lookups (over-abstracting
those would sever the link between two reads at the same ref). Sound:
it only names a subterm. -/
syntax "abstract_machine_lookups" : tactic
elab_rules : tactic
  | `(tactic| abstract_machine_lookups) => withMainContext do
    let isMachineStateTy (t : Expr) : MetaM Bool := do
      return (← Lean.Meta.whnf t).isAppOf ``PLean.MachineState
    -- Whether `a` is derived from an opaque `<ev>_payload_of` extractor.
    let viaPayloadExtractor (a : Expr) : Bool :=
      a.find? (fun sub =>
        match sub.getAppFn.constName? with
        | some n => n.toString.endsWith "_payload_of"
        | none   => false) |>.isSome
    -- First `machines`-field lookup `f a` in `e` whose result is
    -- `MachineState`, `f : _ → MachineState`, and `a` goes through a
    -- payload extractor. Recurses into subterms.
    let rec firstLookup (e : Expr) : MetaM (Option Expr) := do
      if e.isApp then
        let arg := e.appArg!
        if viaPayloadExtractor arg && (← isMachineStateTy (← Lean.Meta.inferType e)) then
          match ← Lean.Meta.inferType e.appFn! with
          | .forallE _ _ body _ =>
            if !body.hasLooseBVars && (← isMachineStateTy body) then
              return some e
          | _ => pure ()
      for sub in e.getAppArgs.push e.getAppFn do
        if sub != e then
          if let some hit ← firstLookup sub then return hit
      return none
    -- Bounded loop: each pass abstracts one compound lookup, then
    -- re-scans (the goal changed). 8 is a safe cap.
    for _ in [0:8] do
      match ← firstLookup (← getMainTarget) with
      | none   => break
      | some e =>
        let stx ← `(tactic| generalize $(← e.toSyntax) = ms at *)
        try evalTactic stx catch _ => break

/-- Pre-SMT normalisation: simp the `pverifySimp` set, destruct
state hypotheses, strip `WithName` wrappers, abstract compound machine
lookups, then unfold the default-invariant predicates so lean-auto's
monomorphizer sees applied uninterpreted symbols and concrete atoms
instead of `Higher order input?`-flagged shapes.

Step ordering matters: `simp [pverifySimp]` precedes `sdestruct_state`
so the state's `addSent` / `addReceived` / … expand into record
literals while it's still a struct; the destruct + `dsimp only`
iota-reduces `{ sent := f, … }.sent l` to `f l`.
`abstract_machine_lookups` runs after the destruct so it sees the
bare `machines` field. `PLean.stateOf` is unfolded early so
`stateOf x s = <S>_st` surfaces as the underlying
`(s.machines _).currentState` projection, which lean-auto translates
via the `<S>_st` `@[reducible] def` aliases. -/
syntax "pverify_smt_prep" : tactic
macro_rules
  | `(tactic| pverify_smt_prep) => `(tactic| (
      try intros
      try simp only [pverifySimp] at *
      try unfold PLean.stateOf at *
      try sdestruct_state
      try unfold WithName at *
      try dsimp only at *
      try abstract_machine_lookups
      try unfold PLean.DefaultInvariants at *
      try unfold PLean.UniqueActions at *
      try unfold PLean.IncreasingCount at *
      try unfold PLean.ReceivedSubsetSent at *
      try dsimp only at *
    ))

/-! ## Obligation cache

Hash the **elaborated obligation type** (a `Lean.Expr`) and consult a
per-project `<project>/.lake/build/pverify_cache/` directory. On a hit,
the obligation is closed by `Loom.SMT.trust_smt` directly, skipping
`pverify_smt_prep`, the lean-auto translation, AND the solver call.
Mirrors PVerifier's approach (`PCompiler/.../Uclid5CodeGenerator.cs`
`PVerifierCache`) which checksums the generated UCLID source file.

**Soundness.** Entries are written only after a real
`pverify_smt` succeeded (the solver returned `unsat`). Hits
close the obligation via the same `Loom.SMT.trust_smt` axiom that
`loom_smt` uses on `unsat`. 64-bit `String.hash` collision risk at
10⁴ entries is <10⁻⁷.

**Stability.** The hash is over `Lean.Meta.ppExpr` output of the
elaborated obligation type, with macro-scope marks stripped. Stable
across elaborations of the same surface obligation; an unrelated edit
elsewhere in the file leaves the obligation type byte-identical and
the cache hits. -/

scoped syntax (name := pverifyHere) "pverifyHere!" : term

open Lean Elab Term in
@[term_elab pverifyHere] def elabPverifyHere : TermElab
  | `(pverifyHere!), _ => do
    let ctx ← readThe Lean.Core.Context
    let srcPath := System.FilePath.mk ctx.fileName
    let some srcDir := srcPath.parent
      | throwError "cannot compute parent directory of '{srcPath}'"
    return mkStrLit s!"{srcDir}"
  | _, _ => throwUnsupportedSyntax

/-- `<project>/.lake/build/pverify_cache/`. Anchored to this file's
location (`PLean/Verify/Tactic.lean`) so the path is stable regardless
of which downstream file is being compiled. -/
def pverifyCacheDir : System.FilePath :=
  System.mkFilePath [pverifyHere!] / ".." / ".." / ".lake" /
    "build" / "pverify_cache"

/-- Strip macro-scope marks (`✝`) so two elaborations of the same
surface obligation produce the same canonical form. -/
private def canonicalise (s : String) : String :=
  s.foldl (init := "") (fun acc c =>
    if c == '✝' then acc else acc.push c)

/-- Hex hash of an obligation's canonicalised pretty-printed type. -/
def pverifyHash (s : String) : String :=
  toString (canonicalise s).hash

def pverifyCachePath (hash : String) : System.FilePath :=
  pverifyCacheDir / s!"{hash}.ok"

/-- Pretty-print an `Expr` for cache hashing. -/
def pverifyExprToCacheText (e : Expr) : MetaM String := do
  let f ← Lean.Meta.ppExpr e
  return f.pretty (width := 120)

/-- Cache-text for a tactic goal: every visible local hypothesis's
type, plus the goal target. Required for soundness — a hit would
otherwise close a goal whose hypotheses don't actually suffice. We
include the hypothesis NAMES too (after `canonicalise` strips macro-
scope marks) so a manual proof that references hypotheses by name
isn't mis-cached against one that doesn't.

The implementation uses **raw `Expr.toString`** (constructor-shape
printer) over each hyp/goal-target `Expr`, after `instantiateMVars`
and `Expr.consumeMData`. This is ~48× cheaper than `Lean.Meta.ppExpr`
(which delaborates + pretty-prints) and proved stable enough across
elaborations for our goal set: warm-path cache hits 12/12 on
DistributedLock and 33/34 on LockServer. The string contains
constructor shape (e.g. `Expr.app`, `Expr.forallE`) and de Bruijn
indices, so it doesn't suffer from macro-scope `✝` drift the way
`ppExpr` did.

Performance (profiled 2026-06-19, see `Tests/Verify/ProfileProbe.lean`):
- ppExpr-based key (prior): 191 ms / 12 obligations = 16 ms/each
- Expr.toString-based (this): 4 ms / 12 = 0.3 ms/each (~48×)

Hyp ordering is the local-context order the user introduced them in,
so the hash is deterministic. -/
def pverifyGoalToCacheText : MVarId → MetaM String := fun mv => do
  mv.withContext do
    -- Fast key: serialise each hypothesis type and the goal target via
    -- the raw `Expr.toString` (constructor-shape printer) after
    -- `instantiateMVars` + `Expr.eraseMData`. This is ~20× cheaper than
    -- `Lean.Meta.ppExpr` (which delaborates + pretty-prints) and
    -- deterministic on identical normalised `Expr`s. We canonicalise
    -- via `canonicalise` (strips `✝` macro marks) for cross-elaboration
    -- stability. ppExpr is kept here as a fallback comment for reference.
    let lctx ← getLCtx
    let mut parts : Array String := #[]
    for ldecl in lctx do
      if ldecl.isImplementationDetail then continue
      let ty ← Lean.instantiateMVars ldecl.type
      let tyText := toString (Lean.Expr.consumeMData ty)
      parts := parts.push s!"{ldecl.userName} : {tyText}"
    let goalTy ← Lean.instantiateMVars (← mv.getType)
    let goalText := toString (Lean.Expr.consumeMData goalTy)
    return String.intercalate "\n" parts.toList ++ "\n⊢ " ++ goalText

def pverifyCacheHas (hash : String) : IO Bool := do
  try (pverifyCachePath hash).pathExists
  catch _ => return false

/-- Record an obligation as certified `unsat`. Stores the human-
readable text alongside the hash for debugging / auditing. -/
def pverifyCacheInsert (hash : String) (humanText : String) : IO Unit := do
  try
    try IO.FS.createDirAll pverifyCacheDir catch _ => pure ()
    IO.FS.writeFile (pverifyCachePath hash) humanText
  catch _ => pure ()

register_option pverify.cache : Bool := {
  defValue := true
  descr := "If true (default), `#pverify` caches obligations already \
            certified `unsat` in <project>/.lake/build/pverify_cache/. \
            On a hit, the obligation is closed directly via the \
            Loom.SMT.trust_smt axiom — bypassing pverify_smt_prep, \
            lean-auto translation, and the solver invocation. Hashing \
            is by elaborated obligation `Expr`, so unrelated edits to \
            the same file don't invalidate. Reset with `lake clean`."
}

register_option pverify.profile : Bool := {
  defValue := false
  descr := "If true, `pverify_smt` takes an inlined branch that \
            instruments each stage (cache lookup, prep, lean-auto, \
            solver, assign) with `IO.monoNanosNow` timers and records \
            into `PLean.Verify.Profile.stateRef`. `#pverify` emits a \
            summary table on completion. OFF by default — the inlined \
            branch is not bit-identical to upstream `loom_smt` and is \
            kept off the hot path."
}

/-! ## SMT discharge

`pverify_smt` consults the obligation cache, then on miss runs
`pverify_smt_prep; loom_smt [*]`. Under `set_option pverify.profile
true`, the work is inlined into PLean (via `pverifySmtCloseProfiled`
below) so each stage can be timed separately; the default path stays
on the unmodified `loom_smt` macro.

`loom_smt` logs a "Goal proven by <solver>" info on success — one per
obligation. Under `#pverify` that's per-obligation noise the command's
consolidated report already subsumes, so the elab below drops new
info-severity messages produced by either path (errors/warnings kept).
-/

/-- Default (unprofiled) path: identical to the pre-profile-instrumentation
behaviour. Consults the cache, then runs `pverify_smt_prep; loom_smt [*]`
on a miss. -/
def pverifySmtCloseDefault : TacticM Unit := do
  let useCache := pverify.cache.get (← getOptions)
  let mv ← getMainGoal
  let goalType ← mv.getType
  let cacheHash? : Option (String × String) ←
    if useCache then
      try
        let text ← pverifyGoalToCacheText mv
        pure (some (pverifyHash text, text))
      catch _ => pure none
    else pure none
  let cacheHit : Bool ←
    match cacheHash? with
    | some (hash, _) =>
      if (← liftM (pverifyCacheHas hash)) then
        mv.assign (mkApp (mkConst ``Loom.SMT.trust_smt) goalType)
        pure true
      else pure false
    | none => pure false
  unless cacheHit do
    evalTactic (← `(tactic| (pverify_smt_prep; loom_smt [*])))
    if let some (hash, text) := cacheHash? then
      liftM (pverifyCacheInsert hash text)

/-- Instrumented path: inlines `loom_smt`'s `prepareLeanAutoQuery +
querySolver + trust_smt.assign` so we can time each segment separately.
Records into `PLean.Verify.Profile.inFlightRowsRef` under this
obligation's key. NOT bit-identical to upstream `loom_smt` (no `Goal
proven by …` log, no `retryOnUnknown` cross-solver fallback by
default), so we keep it behind `set_option pverify.profile true`. -/
def pverifySmtCloseProfiled : TacticM Unit := do
  let useCache := pverify.cache.get (← getOptions)
  let key ← currentObligationKey
  let mv ← getMainGoal
  let goalType ← mv.getType
  let cacheHash? : Option (String × String) ←
    if useCache then
      try
        let (text, ppNs) ← liftM (m := MetaM)
          (PLean.Verify.Profile.timeMetaNanos (pverifyGoalToCacheText mv))
        liftM (m := IO) (PLean.Verify.Profile.modifyRow key
          (fun r => { r with cachePp := r.cachePp + ppNs }))
        let (h, hashNs) ← liftM (m := IO)
          (PLean.Verify.Profile.timeNanos (pure (pverifyHash text)))
        liftM (m := IO) (PLean.Verify.Profile.modifyRow key
          (fun r => { r with cacheHash := r.cacheHash + hashNs }))
        pure (some (h, text))
      catch _ => pure none
    else pure none
  let cacheHit : Bool ←
    match cacheHash? with
    | some (hash, _) =>
      let (hit, fsNs) ← liftM (m := IO)
        (PLean.Verify.Profile.timeNanos (pverifyCacheHas hash))
      liftM (m := IO) (PLean.Verify.Profile.modifyRow key
        (fun r => { r with cacheFs := r.cacheFs + fsNs }))
      if hit then
        let (_, assignNs) ← liftM (m := MetaM)
          (PLean.Verify.Profile.timeMetaNanos
            (mv.assign (mkApp (mkConst ``Loom.SMT.trust_smt) goalType)))
        liftM (m := IO) (PLean.Verify.Profile.modifyRow key (fun r =>
          { r with cached := true, smtAssign := r.smtAssign + assignNs }))
        pure true
      else pure false
    | none => pure false
  unless cacheHit do
    let (_, prepNs) ← PLean.Verify.Profile.timeTacticNanos
      (evalTactic (← `(tactic| pverify_smt_prep)))
    liftM (m := IO) (PLean.Verify.Profile.modifyRow key (fun r =>
      { r with smtPrep := r.smtPrep + prepNs }))
    -- Re-enter `withMainContext` so `prepareLeanAutoQuery` sees the
    -- post-prep local context. Without this, lean-auto's
    -- `collectAllLemmas (hints := [*])` collects against a stale
    -- context and rejects the goal.
    withMainContext do
    let mv' ← getMainGoal
    let opts ← getOptions
    let withTimeout := loom.solver.smt.timeout.get opts
    let hints : TSyntax `Auto.hints ← `(Auto.hints| [*])
    let (cmdString, autoNs) ← PLean.Verify.Profile.timeTacticNanos
      (Loom.SMT.prepareLeanAutoQuery mv' hints)
    liftM (m := IO) (PLean.Verify.Profile.modifyRow key (fun r =>
      { r with smtAuto := r.smtAuto + autoNs }))
    let ((res, solverUsed), solverNs) ← liftM (m := MetaM)
      (PLean.Verify.Profile.timeMetaNanos
        (Loom.SMT.querySolver cmdString withTimeout
          (forceSolver := Loom.SMT.specifiedSmtSolver (loom.solver.get opts))
          (retryOnUnknown := loom.solver.smt.retryOnUnknown.get opts)))
    liftM (m := IO) (PLean.Verify.Profile.modifyRow key (fun r =>
      { r with smtSolver := r.smtSolver + solverNs }))
    match res with
    | .Sat none       => throwError s!"{Loom.SMT.satGoalStr solverUsed}"
    | .Sat (some m)   => throwError s!"{Loom.SMT.satGoalStr solverUsed}:{m}"
    | .Unknown reason =>
      let suffix := match reason with | some r => s!": {r}" | none => ""
      throwError s!"{Loom.SMT.unknownGoalStr solverUsed}{suffix}"
    | .Failure reason =>
      let suffix := match reason with | some r => s!": {r}" | none => ""
      throwError s!"{Loom.SMT.failureGoalStr solverUsed}{suffix}"
    | .Unsat =>
      let mvPost ← getMainGoal
      let goalTypePost ← mvPost.getType
      let (_, assignNs) ← liftM (m := MetaM)
        (PLean.Verify.Profile.timeMetaNanos
          (mvPost.assign (mkApp (mkConst ``Loom.SMT.trust_smt) goalTypePost)))
      liftM (m := IO) (PLean.Verify.Profile.modifyRow key (fun r =>
        { r with smtAssign := r.smtAssign + assignNs }))
      if let some (hash, text) := cacheHash? then
        liftM (m := IO) (pverifyCacheInsert hash text)

syntax "pverify_smt" : tactic
elab_rules : tactic
  | `(tactic| pverify_smt) =>
      withMainContext do
        let log0 := (← getThe Core.State).messages
        let profile := pverify.profile.get (← getOptions)
        try
          if profile then pverifySmtCloseProfiled
          else            pverifySmtCloseDefault
          let log1 := (← getThe Core.State).messages
          let fresh := log1.toArray.extract log0.toArray.size log1.toArray.size
          let kept := fresh.filter (fun m => !(m.severity matches .information))
          modifyThe Core.State fun st =>
            { st with messages := kept.foldl (·.add ·) log0 }
        catch e =>
          let msg ← e.toMessageData.toString
          let key ← currentObligationKey
          liftM (modifyDiag key fun d => { d with smt := some msg })
          throw e

/-- Arithmetic / boolean fallback for default-invariant goals SMT
can't translate (e.g., when the goal involves `GlobalState`'s
function-typed fields in a shape that defeats `funextEq`). -/
syntax "pverify_grind" : tactic
macro_rules
  | `(tactic| pverify_grind) => `(tactic| (
      try intros
      first | grind | (refine ⟨?_, ?_⟩ <;> grind) | omega | tauto | assumption
    ))

/-- Walk the goal target and `refine ⟨?_, ?_⟩`-split every top-level
`∧`, then call `pverify_smt` on each resulting subgoal. Sound: a
proof of `A ∧ B` follows from independent proofs of `A` and `B`.

Motivation: large user-invariant bundles (LockServer's 11-conjunct
`system_config`, the 5-conjunct `safety` etc.) often return `unknown`
from the solver as a single-shot query even though each conjunct is
individually decidable in well under the timeout. Splitting before
discharge keeps the per-query size small and shifts more obligations
from `unknown` to closed-by-SMT.

Cost: N solver invocations instead of 1 on the bundle. Wired as a
*fallback* after the whole-bundle `pverify_smt` so the common
single-shot case is unaffected.

The 32-iteration cap is a safety bound (real bundles flatten in <16
levels); a goal with no `∧` head goes straight to the closing tactic
on the unmodified goal. -/
syntax "pverify_split_smt" : tactic
elab_rules : tactic
  | `(tactic| pverify_split_smt) => withMainContext do
      let mut iter := 0
      let mut keepGoing := true
      while keepGoing && iter < 32 do
        iter := iter + 1
        keepGoing := false
        -- Snapshot the current goal list; only split goals headed by `And`.
        let goals ← getGoals
        let mut nextGoals : List MVarId := []
        for g in goals do
          let ty ← g.getType
          if ty.consumeMData.isAppOfArity ``And 2 then
            setGoals [g]
            try
              evalTactic (← `(tactic| refine ⟨?_, ?_⟩))
              keepGoing := true
              nextGoals := nextGoals ++ (← getGoals)
            catch _ =>
              nextGoals := nextGoals ++ [g]
          else
            nextGoals := nextGoals ++ [g]
        setGoals nextGoals
      -- Drop trailing `True` goals (the `... ∧ True` bundle terminator
      -- our `buildConjAt` emits leaves a literal `True` after splitting).
      let final ← getGoals
      let mut keep : List MVarId := []
      for g in final do
        let ty ← g.getType
        if ty.consumeMData.isConstOf ``True then
          try
            setGoals [g]
            evalTactic (← `(tactic| exact True.intro))
          catch _ => keep := keep ++ [g]
        else
          keep := keep ++ [g]
      setGoals keep
      -- Close each remaining subgoal by SMT. `all_goals` keeps the
      -- iteration single-pass; per-conjunct failure throws and propagates,
      -- letting the surrounding `first |` fall through. If `True.intro`
      -- already cleared every goal (all-True bundle), skip the SMT call —
      -- `all_goals` on the empty goal list otherwise errors "no goals".
      unless (← getGoals).isEmpty do
        evalTactic (← `(tactic| all_goals pverify_smt))

/-! ## `default_inv` — `DefaultInvariants` discharge

The proof shape for `UniqueActions` / `IncreasingCount` /
`ReceivedSubsetSent` preservation is mechanical and depends only on
the handler's primitive footprint. The chain below splits the 3-way
conjunction, intros the per-conjunct labels, simps `addSent`-shaped
post-state reads to a disjunction, `rcases`-splits new vs old, and
closes each leaf via `solve_by_elim` / `Nat.lt_irrefl` /
`Nat.lt_succ_*` / `grind` / `omega`. The auto-discharge path doesn't
rely on user-named hypotheses — `solve_by_elim` finds the pre-state
hypothesis by type. -/
syntax "default_inv_guard" : tactic
syntax "default_inv" : tactic

/-- Internal head-symbol guard for `default_inv`: fails unless the
goal mentions one of the four default-invariant constants
(`DefaultInvariants` / `UniqueActions` / `IncreasingCount` /
`ReceivedSubsetSent`). Without this guard `default_inv`'s
`refine ⟨?_, ?_, ?_⟩` would mangle unrelated 3-conjunct goals. -/
elab_rules : tactic
  | `(tactic| default_inv_guard) => withMainContext do
      let goal ← getMainTarget
      let allowed : List Name :=
        [``PLean.DefaultInvariants, ``PLean.UniqueActions,
         ``PLean.IncreasingCount, ``PLean.ReceivedSubsetSent]
      let mentions := goal.find? fun e =>
        match e.getAppFn.constName? with
        | some n => allowed.contains n
        | none   => false
      if mentions.isNone then
        throwError "default_inv: this goal does not mention any of \
                    `DefaultInvariants` / `UniqueActions` / \
                    `IncreasingCount` / `ReceivedSubsetSent`; tactic \
                    declined to fire (would over-split a generic \
                    n-conjunct goal)"

macro_rules
  | `(tactic| default_inv) => `(tactic| (
      default_inv_guard
      -- Goal might be `(WPGen.default x).get post s` or already
      -- `DefaultInvariants s'`; reduce both via `WPGen.default` +
      -- the WP rewrite set so we land at `DefaultInvariants <update>`.
      try simp [WPGen.default, loomWPGenRewrite, loomLogicLiftSimp]
      -- Unfold `DefaultInvariants` AND its three components in the
      -- local context so pre-state hypotheses are in implication form
      -- (otherwise `grind` can't peel the named constants).
      try unfold PLean.DefaultInvariants
      try simp only [PLean.UniqueActions, PLean.IncreasingCount,
                     PLean.ReceivedSubsetSent] at *
      try refine ⟨?_, ?_, ?_⟩
      all_goals first
        | (intro a b hne ha hb
           simp at ha hb
           rcases ha with rfl | hAprev <;>
             rcases hb with rfl | hBprev <;>
             first
               | (exact (hne rfl).elim)
               | (intro hEq
                  exfalso
                  -- One label is new (count = s.actionCount), the
                  -- other old (count < s.actionCount by IC). solve_by_elim
                  -- finds IC; Nat.lt_irrefl closes.
                  solve_by_elim [Nat.lt_irrefl])
               | -- Both old labels: pre-state UA closes.
                 (solve_by_elim)
               | grind
               | omega)
        | (intro a ha
           simp at ha
           rcases ha with rfl | hPrev
           · first | exact Nat.lt_succ_self _ | omega | grind
           · first | (apply Nat.lt_succ_of_lt; first | grind | omega)
                    | omega | grind)
        | (intro a ha
           simp at ha
           simp
           first
             | (rcases ha with rfl | hPrev
                · -- new received = inflight lbl; conclusion: it was sent.
                  first | (right; grind) | grind | tauto
                · -- old received: pre-state RS suffices.
                  first | (right; grind) | grind | tauto)
             | first | grind | tauto | assumption)
        | -- No-send / no-receive fallback: pre-state's UA/IC/RS holds.
          (first | grind | tauto | assumption | omega)
    ))

/-- Walk the local context and `obtain`-split every `(A ∧ B)`
hypothesis. After `pverify_step_wp` + `intros` the precondition lives
as one folded conjunction; flattening lets `solve_by_elim` /
`assumption` find each clause by type. Internal to `pverify`. -/
syntax "split_conjunction_hyps" : tactic
elab_rules : tactic
  | `(tactic| split_conjunction_hyps) => withMainContext do
    let mut keepGoing := true
    let mut iter := 0
    while keepGoing && iter < 16 do
      iter := iter + 1
      keepGoing := false
      let lctx ← getLCtx
      for ldecl in lctx do
        if ldecl.isImplementationDetail then continue
        let ty ← Lean.Meta.inferType ldecl.toExpr
        if ty.consumeMData.isAppOfArity ``And 2 then
          let hName := ldecl.userName
          let stx ← `(tactic| obtain ⟨_, _⟩ := $(mkIdent hName))
          try
            evalTactic stx
            keepGoing := true
            break
          catch _ =>
            continue

/-! ## Manual-proof helpers (send-handler clause shapes)

These helpers compose into per-conjunct dispatch for `@[pverifyProof]`
proofs of send-handler obligations. Each helper names a specific
combination of *which kind of clause* (carry / inflight predicate /
kind guard) and *what the step did* (received, sent, field-only
update).

The shape comments use Hoare-triple notation: `{ Pre } step { Post }`,
where `Pre` lists the hypotheses available, `step` is the primitive
footprint, and `Post` is the goal the tactic discharges. `s` is the
pre-state, `s'` the post-state, `lbl` the dispatched label.

| Helper                                             | { Pre }                                                | step                               | { Post }                            |
|----------------------------------------------------|--------------------------------------------------------|------------------------------------|-------------------------------------|
| `pverify_carry_after_recv h`                       | `{ h : P s }`                                          | `markReceived lbl`                 | `{ P s' }`  (P recv-monotone)       |
| `pverify_not_inflight h, hisE, isW`                | `{ h : ¬ inflight e s, hisE : is_<ev> e }`             | `send <newEv>`                     | `{ ¬ inflight e s' }`               |
| `pverify_not_inflight_by <K>, hPre, isW`           | per-conjunct prologue + `hPre`                         | field-only update + `send <newEv>` | `{ ¬ inflight e s' }`               |
| `pverify_inflight_by h using x => …`               | `{ h : inflight e s' }` + user-supplied discriminator  | `send <newLbl>`                    | `{ inflight e s }`                  |
| `pverify_machine_has_type <K> on <r>`              | post-state form of the kind predicate visible to lctx  | field-only update                  | `{ is_<K> <r> }` (closes the goal)  |
| `pverify_machine_has_type hPre : <K> r from hPost` | `{ hPost : is_<K> r post }`                            | field-only update                  | introduces `hPre : is_<K> r s`      |

The "carry" / "transfer" case — where the post-state predicate already
holds verbatim from a pre-state hypothesis — is closed by plain
`assumption`; no named helper for that.

`pverify_not_inflight` and `pverify_inflight_by` handle the routing-
clause case where the clause's `∀ e` quantifier ranges over a buffer
that the step grew. `pverify_not_inflight_by` composes
`pverify_machine_has_type` (kind-bridge) with `pverify_not_inflight`
for the recurring "field-only update + wrong-event routing clause"
shape. `pverify_machine_has_type` is the underlying primitive: it
asserts a machine ref's kind by exploiting that the kind triple is
preserved by any field-only update. -/

/-- Carry an `inflight`-monotone clause from the pre-state through a
step whose only footprint is `markReceived lbl`. Such a step grows
`received` and leaves every other component (machines, `sent`,
`actionCount`) untouched, so `received` growing only shrinks
`inflight`; any predicate of the form `¬ inflight …` or
`inflight … → P` therefore transfers verbatim.

```
  { hPre : P s }   markReceived lbl   ⊢  P s'
```

**Argument.**
- `hPre` is a *proof of the pre-state form of the goal*, applied to
  whatever quantified witnesses the clause introduces. Look for a
  pre-state hypothesis in the local context whose statement matches
  the goal modulo replacing `s'` with `s`; if the goal opens
  `∀ x …, …`, intro those binders first, then supply
  `<preHyp> x …` as the argument. The helper handles both
  `¬(A ∧ B)` and `(A ∧ B) → C` surface shapes (via `not_and` /
  `and_imp` normalisation), so either form for the pre-state
  hypothesis is accepted. -/
syntax "pverify_carry_after_recv " term : tactic
macro_rules
  | `(tactic| pverify_carry_after_recv $hPre:term) => `(tactic| (
      have hpre__rcv := $hPre
      try simp only [PLean.inflight] at hpre__rcv ⊢
      simp only [not_and, and_imp] at hpre__rcv ⊢
      intro hsent hrecv
      first
        | (exact hpre__rcv hsent
            (by simp only [Bool.or_eq_false_iff] at hrecv; exact hrecv.2))
        | exact hpre__rcv hsent hrecv))

/-- Close a routing clause `¬ inflight e s'` (a goal where `e` is a
universally-quantified label constrained by `is_<ev> e`) across a
step that sends a fresh label with a *different* event tag than
`<ev>`. The freshly-sent label is excluded by event-tag mismatch
against the bound `is_<ev>` hypothesis; old labels fall through to
the pre-state's `¬ inflight e s`.

```
  { hPre : ¬ inflight e s, hisE : is_<ev> e }   send <newEv> ...
  ⊢  ¬ inflight e s'
```

**Arguments.**
- `hPre` — a *proof of the pre-state form of the goal*, applied to
  the clause's witnesses. The clause is typically `∀ e m, is_<ev> e
  → e targets m → is_<K> m s → ¬ inflight e s`, so after intro'ing
  `e m hisE hTgt hm`, pass `<preHyp> e m hisE hTgt hm` (or whatever
  shape the pre-state hypothesis takes — both `¬(A ∧ B)` and `(A ∧ B)
  → C` are accepted).
- `hisE` — the in-scope hypothesis `is_<ev> e` (introduced by the
  preceding `intro` for the clause's bound event predicate).
- `<isWrong>` — the *name* of an `is_<ev'>` predicate (`<ev'> ≠
  <ev>`) that the freshly-sent label satisfies. Used to derive a
  contradiction on the new-label branch: `is_<ev'> e` (from the new
  label) and `is_<ev> e` (from `hisE`) are incompatible. To find:
  look at what the step's `send` emits; its event predicate is
  `<isWrong>`. -/
syntax "pverify_not_inflight " term ", " ident ", " ident : tactic
macro_rules
  | `(tactic| pverify_not_inflight
        $hPre:term, $hisE:ident, $isWrong:ident) =>
      `(tactic| (
        have hpre__nve := $hPre
        -- Uncurry both surface shapes — `¬(A ∧ B)` via `not_and`,
        -- `(A ∧ B) → C` via `and_imp` — into `A → B → C`, so the helper
        -- accepts whichever form the invariant happens to be written in.
        try simp only [not_and, and_imp] at hpre__nve
        rw [not_and, Bool.or_eq_true, decide_eq_true_eq]
        intro hsent
        rcases hsent with hNew | hOld
        · subst hNew; simp only [$isWrong:ident] at $hisE:ident
        · rw [Bool.or_eq_false_iff, decide_eq_false_iff_not]
          try simp only [PLean.inflight] at hpre__nve
          intro hrecv
          exact absurd (hpre__nve hOld) (by simp [hrecv.2])))

/-- Transport `inflight e s'` *back* to `inflight e s` across a step
that performs one fresh `send` (and `markReceived`). Used when the
clause has the **positive** form `inflight e s → P` and the caller
needs to discharge the antecedent. Unlike `pverify_not_inflight`, the
new-label exclusion is *not* automatic — there's no event-tag
mismatch available because the new label and the clause's `e` may
share an event tag.

```
  { hinfe : inflight e s' }   send <newLbl> ...
  ⊢  inflight e s   -- (under the user-supplied discriminator)
```

```lean
pverify_inflight_by <hinfe> using <h> => <tac>
```

**Arguments.**
- `<hinfe>` — the post-state hypothesis `inflight e s'` available
  after `apply <preHyp> ... ?_ ...` introduces the
  `inflight e s` subgoal. Look in the local context for the
  hypothesis that came from the goal's `inflight e s' → …`
  antecedent (typically named after the binder, e.g. `hinfe`).
- `<h>` — *binder name* for the new-label witness `e = <newLbl>`
  inside `<tac>`. Choose any fresh local name; you'll reference it
  inside `<tac>` to derive `False`.
- `<tac>` — a tactic sequence that closes a `False` goal using `<h>`
  (which says `e` IS the freshly-sent label). The caller knows what's
  contradictory about that — common shapes:
  - **`goto`-handler**: the new label's action is `goto _`, not
    `event _`. If the clause asserts `e.action = .event …` (via a
    hypothesis like `hacte`), then `rw [<h>] at hacte; simp at hacte`
    derives the contradiction.
  - **Forwarding branch**: the proof has already case-split on
    `e = <newLbl>` via `by_cases hee : …` and the current branch is
    `¬ (e = <newLbl>)`. Discharge by `exact hee <h>`.

Companion to `pverify_not_inflight` (negative form, automatic event-
tag discriminator) and `pverify_carry_after_recv` (no fresh send). -/
syntax "pverify_inflight_by " term " using " ident " => " tacticSeq : tactic
macro_rules
  | `(tactic| pverify_inflight_by $hinfe:term using $h:ident => $ts:tacticSeq) =>
      `(tactic| (
        have hinfe__back := $hinfe
        simp only [PLean.inflight, Bool.or_eq_false_iff, Bool.or_eq_true,
                   decide_eq_true_eq, decide_eq_false_iff_not] at hinfe__back ⊢
        refine ⟨?_, hinfe__back.2.2⟩
        rcases hinfe__back.1 with $h:ident | hOld__back
        · exfalso; ($ts:tacticSeq)
        · exact hOld__back))

/-- Assert that a `MachineRef` has a given machine kind.

The canonical kind triple `is_<K>` / `<K>_allocated` / `<K>_kind`
(emitted by `#gen_module` for every registered machine) is preserved
by any step that updates a machine *field* — the same triple holds
before and after, because `kind`, `currentState`, and ref-typed
fields are not touched. The tactic exploits this with a `simp only`
unfold and a case-split on `<r> = this.ref`.

Two surface forms:

```lean
-- Closing form: discharge a goal of shape `is_<K> <r>`. (`<r>` need
-- not be the goal's literal ref — it's the one the case-split is on.)
pverify_machine_has_type <K> on <r>

-- Bridging form: introduce `<hPre>` as a new hypothesis with the
-- pre-state form, using `<hPost>` as the source.
pverify_machine_has_type <hPre> : <K> <r> from <hPost>
```

**Arguments.**
- `<K>` — *name* of a machine kind registered by `#gen_module` in
  the current `pmodule` (e.g. the name of a `machine M { … }`
  declaration). The tactic derives the simp-set names
  `is_<K>` / `<K>_allocated` / `<K>_kind` from `<K>` by string
  concatenation.
- `<r>` — the `MachineRef`-typed *term* the kind predicate is about.
  Typically a binder introduced by the surrounding `intro` (e.g.
  the `m` in `∀ m, is_<K> m s → …`), or a `<wrap>.ref` projection.
- `<hPre>` (bridging form only) — *binder name* to introduce. Choose
  any fresh local name; you'll reference it downstream.
- `<hPost>` (bridging form only) — *name* of an in-scope hypothesis
  whose statement is the post-state form `is_<K> <r> s'`. Find it
  by looking at the clause's `is_<K> <r>` antecedent, which the
  surrounding `intro` brought in.

**Required ambient context.** Both forms expect a `this` binder in
scope whose type is the machine wrapper struct (i.e., the handler's
`this : <M>` parameter); the case-split discriminator is
`<r> = this.ref`. -/
syntax "pverify_machine_has_type " ident " on " term : tactic
syntax "pverify_machine_has_type " ident " : " ident term " from " ident : tactic
macro_rules
  | `(tactic| pverify_machine_has_type $kind:ident on $r:term) => do
      let k := kind.getId.toString
      let alloc  := mkIdentFrom kind (Name.mkSimple (k ++ "_allocated"))
      let kindN  := mkIdentFrom kind (Name.mkSimple (k ++ "_kind"))
      let isKind := mkIdentFrom kind (Name.mkSimple ("is_" ++ k))
      -- `this` is a user-namespace identifier from the calling proof's
      -- context; quote it via `mkIdent` so it doesn't acquire hygiene
      -- marks at macro expansion.
      let thisId : Ident := mkIdent `this
      `(tactic| (
        simp only [$isKind:ident, $alloc:ident, $kindN:ident] at *
        by_cases hMHT__t : $r = ($thisId).ref <;> simp_all))
  | `(tactic| pverify_machine_has_type $hPre:ident :
        $kind:ident $r:term from $hPost:ident) => do
      let k := kind.getId.toString
      let alloc  := mkIdentFrom kind (Name.mkSimple (k ++ "_allocated"))
      let kindN  := mkIdentFrom kind (Name.mkSimple (k ++ "_kind"))
      let isKind := mkIdentFrom kind (Name.mkSimple ("is_" ++ k))
      let thisId : Ident := mkIdent `this
      let sId    : Ident := mkIdent `s
      `(tactic| (
        have $hPre:ident : $isKind:ident $r $sId := by
          simp only [$isKind:ident, $alloc:ident, $kindN:ident] at $hPost:ident ⊢
          by_cases hMHT__t : $r = ($thisId).ref <;> simp_all))

/-- Composite for the recurring "field-only update + wrong-event
routing clause" shape. Equivalent to manually writing:

```lean
  intro e m hisE hTgt hm
  pverify_machine_has_type hmPre : <K> m from hm
  pverify_not_inflight (<hPre> e m hisE hTgt hmPre), hisE, <isWrong>
```

Applies when:
- the goal is a routing clause `∀ e m, is_<ev> e → e targets m →
  is_<K> m s → ¬ inflight e s` (so the prologue is `intro e m hisE
  hTgt hm`),
- the step performs a *field-only* machine update plus one fresh
  `send` of an event whose tag differs from `<ev>`.

**Arguments.**
- `<K>` — *name* of the machine kind in the clause's `is_<K> m s`
  guard (the one needing to bridge from post-state to pre-state).
- `<hPre>` — *name* of the in-scope pre-state hypothesis that
  proves the clause for `s`. The composite applies it to the
  intro'd witnesses as `<hPre> e m hisE hTgt hmPre`, so it must
  have the matching `∀ e m, is_<ev> e → … → is_<K> m s → …` shape.
  Look for the hypothesis named after the clause that was bound by
  the `obtain ⟨…⟩` flattening earlier in the proof.
- `<isWrong>` — *name* of the `is_<ev'>` predicate the freshly-sent
  label satisfies (`<ev'> ≠ <ev>`). See `pverify_not_inflight`'s
  docstring for how to find it. -/
syntax "pverify_not_inflight_by " ident ", " ident ", " ident : tactic
macro_rules
  | `(tactic| pverify_not_inflight_by $kind:ident, $hPre:ident, $isWrong:ident) =>
      `(tactic| (
        intro e__fo m__fo hisE__fo hTgt__fo hm__fo
        pverify_machine_has_type hmPre__fo : $kind m__fo from hm__fo
        pverify_not_inflight
          ($hPre:ident e__fo m__fo hisE__fo hTgt__fo hmPre__fo),
          hisE__fo, $isWrong:ident))

syntax (name := pverifyTactic) "pverify" : tactic
syntax (name := pverifyDefaultTactic) "pverify_default" : tactic

/-- Internal close-chain used by `pverify`: try `default_inv` (cheap
when the goal head is a default-invariant constant — guarded
otherwise), then single-shot SMT, then split-per-conjunct SMT for
`unknown`-prone bundles, then the arithmetic / boolean fallback. The
split branch costs N solver calls on an N-conjunct goal, so it sits
AFTER single-shot — the common case is one query. -/
syntax "pverify_close_chain" : tactic
macro_rules
  | `(tactic| pverify_close_chain) => `(tactic| (
      first
        | default_inv
        | pverify_smt
        | pverify_split_smt
        | pverify_grind))

/-- Internal close-chain used by `pverify_default`: SMT first because
the goal arrives with explicit `DefaultInvariants` content the SMT
path handles directly without needing `default_inv`'s case split. -/
syntax "pverify_close_chain_smt_first" : tactic
macro_rules
  | `(tactic| pverify_close_chain_smt_first) => `(tactic| (
      first
        | pverify_smt
        | pverify_split_smt
        | default_inv
        | pverify_grind))

/-- The headline auto-discharge tactic. Three branches in order:
trivial / `True`-shaped post; post-equals-pre (any handler whose
post matches a pre-clause); full chain — step WP, intros, flatten the
precondition's conjunction, then run the close-chain. -/
macro_rules
  | `(tactic| pverify) => `(tactic| (
      first
        -- Trivial-handler branch: step WP closes it directly. `done`
        -- guards against a follow-on `intros` erroring on no-goals.
        | (pverify_step_wp; done)
        -- Post-equals-pre branch.
        | (pverify_step_wp
           intros
           assumption)
        -- Full chain.
        | (pverify_step_wp
           intros
           split_conjunction_hyps
           pverify_close_chain)
    ))

macro_rules
  | `(tactic| pverify_default) => `(tactic| (
      pverify_step_wp
      first
        | (intro s h; exact h)
        | (intros
           -- Unfold the bundle so `split_conjunction_hyps` sees three
           -- conjuncts instead of an opaque `DefaultInvariants s`.
           simp only [PLean.DefaultInvariants, PLean.UniqueActions,
                      PLean.IncreasingCount, PLean.ReceivedSubsetSent] at *
           split_conjunction_hyps
           pverify_close_chain_smt_first)
    ))

end PLean
