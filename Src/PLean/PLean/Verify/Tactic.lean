/-
PLean.Verify.Tactic — atomic `pverify_*` tactics.

`#pverify M` is an SMT-discharge command; the tactics here are the
user-facing primitives for manual proofs of obligations SMT can't
close, registered via `@[pverifyProof]`. The pre-SMT simp set lives
in `Verify/SimpAttrs.lean` (lemmas + the `pverifySimp` attribute);
this file is purely tactic syntax.

Composing a manual proof:

```lean
@[pverifyProof] theorem MyMod.M.S.<ev>_correct_safety
    (this : M) (param : payload) :
    triple ... := by
  pverify_open_triple
  pverify_step_wp
  intro s hpre
  obtain ⟨hLemma, hUA, hIC, hRS, lbl, hInflight, hTarget, hStateOf, hAction⟩ := hpre
  pverify_smt_close
```

Macro-hygiene rule: every simp lemma name lives inside a named tactic
helper. The obligation generator and manual proofs both call the named
tactics; neither inlines lemma names — bare references would get
hygiene marks at expansion.
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

`#pverify`'s obligation generator wraps each obligation in
`pverify_log_failure_else_sorry`. When the inner chain fails, the
SMT solver's diagnostic (counter-example / `unknown` reason) and any
non-SMT tactic-error text are stashed in these refs before the chain
falls through to `sorry`. The obligation generator clears them before
each obligation and reads them after.

We need `IO.Ref`s rather than message-log entries because the surrounding
`first | … | …` ladder rolls back the log on rollback; the refs survive
tactic-state restoration. -/

initialize pverifySmtDiagRef : IO.Ref (Option String) ← IO.mkRef none
initialize pverifyTacDiagRef : IO.Ref (Option String) ← IO.mkRef none

/-- Run a tactic chain; on throw or unclosed goals, stash the
diagnostic in `pverifyTacDiagRef` and close with `sorry` so the
enclosing `theorem ... := by ...` still elaborates. The post-elab
`info.value.hasSorry` check tells the reporting layer which
obligations failed; the ref carries the WHY. -/
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
          pverifyTacDiagRef.set (some msg)
          evalTactic (← `(tactic| sorry))
        | none => pure ()

/-- Peel a `triple pre body post` goal: replaces it with
`pre ≤ wpg.get post` for a synthesised `wpg : WPGen body`. Alias for
Loom's `wpgen_intro`. -/
syntax "pverify_open_triple" : tactic
macro_rules
  | `(tactic| pverify_open_triple) => `(tactic| (apply WPGen.intro; rotate_right))

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

/-- Unfold framework-level state predicates so subsequent simp / grind
/ SMT calls see boolean / arithmetic content. User invariants and
lemma bundles are unfolded by the obligation generator's preamble. -/
syntax "pverify_unfold" : tactic
macro_rules
  | `(tactic| pverify_unfold) => `(tactic| (
      try unfold PLean.DefaultInvariants PLean.UniqueActions
                 PLean.IncreasingCount PLean.ReceivedSubsetSent
                 PLean.inflight PLean.sent PLean.received
                 PLean.precedes PLean.Label.targets? PLean.stateOf
    ))

/-- Aggressive state-update simp at the propositional VC level (after
`intro s hpre`). Same simp set as `pverify_step_wp` but applied at
`*`. -/
syntax "pverify_normalize_state" : tactic
macro_rules
  | `(tactic| pverify_normalize_state) => `(tactic| (
      try simp only [PLean.GlobalState.addSent, PLean.GlobalState.bumpActionCount,
                     PLean.GlobalState.addReceived, PLean.GlobalState.updateMachine] at *
    ))

/-- Destruct every `GlobalState`-typed local into its four fields,
making them top-level uninterpreted symbols rather than struct
projections. Without this, lean-auto rejects with "Higher order input?"
on the first `s.sent l` projection. -/
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
        -- Name the four fields with stable, unhygienic identifiers so
        -- `pverify_defunctionalize_state` (a user-invokable helper) can
        -- reference `gsMachines`. A second GlobalState local — rare at
        -- SMT time — just shadows these names; harmless.
        let stx ← `(tactic|
          obtain ⟨$(mkIdent `gsSent), $(mkIdent `gsReceived),
                  $(mkIdent `gsMachines), $(mkIdent `gsActionCount)⟩ :=
            $(mkIdent hName))
        try evalTactic stx
        catch _ => pure ()

/-- Abstract every `machines`-field lookup whose ref argument is derived
from an *opaque event-payload extractor* (`<ev>_payload_of`) to a fresh
`MachineState` local.

The `machines` field is `MachineRef → MachineState`. lean-auto translates
`s.machines x` (x a bound/free variable, or a plain wrapper projection
like `n.ref`) as an applied uninterpreted function — fine. But
`s.machines ((<ev>_payload_of e).sender)` — the field applied to a ref
pulled through the *opaque* payload extractor — makes lean-auto emit an
identity lambda and abort with `lamTerm2STermAux :: Unexpected head term
... lam`. Generalising the whole `machines <compound>` application to a
fresh `ms : MachineState` removes the offending argument before SMT sees
it; the invariant body then reads plain `MachineState` projections
(`ms.kind`, …), which translate cleanly. Sound: it only names a subterm.

The trigger is gated on the argument mentioning a `…_payload_of` constant
so it does NOT fire on ordinary `s.machines n.ref` lookups (over-
abstracting those would sever the link between two reads at the same ref,
e.g. `unique_lock_holder`'s `n1`/`n2`). Generalisation also only succeeds
on a closed term, so it abstracts the post-negation skolem case and
leaves genuinely-under-binder occurrences for lean-auto. -/
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

/-- User-invokable helper: find a local of type
`MachineRef → MachineState …` in the context (named or anonymous,
e.g. `gsMachines` or `machines✝` after `sdestruct_state`) and abstract
every `(<that local> m).currentState` read in the goal into a fresh
opaque `pStateOf m : S`.

After `sdestruct_state`, the `machines` field is in scope as a local
with type `MachineRef → MachineState ProgramSig.…`. The
`.currentState` projection's codomain is the per-pmodule `S` union of
every machine's states — lean-auto rejects that record-of-an-inductive
shape with "Higher order input?" whenever the read appears under a
quantifier. Introducing
`∃ pStateOf : MachineRef → _, ∀ m, (f m).currentState = pStateOf m`
(closed by `rfl`) and rewriting the goal turns those reads into a
first-order `MachineRef → S` uninterpreted function lean-auto can
translate.

NOT run by the default prep chain — most obligations close without it
via the `<S>_st` `@[reducible] def` aliases. Invoke this in a manual
`@[pverifyProof]` proof body, just before `pverify_smt_close`, when
the goal contains `(_.machines _).currentState` reads under a `∀` and
SMT returns "Higher order input?". -/
syntax "pverify_defunctionalize_state" : tactic
elab_rules : tactic
  | `(tactic| pverify_defunctionalize_state) => withMainContext do
      let lctx ← getLCtx
      let mut found : Option Lean.FVarId := none
      for ldecl in lctx do
        let ty ← Lean.Meta.whnf (← Lean.Meta.inferType ldecl.toExpr)
        match ty with
        | .forallE _ dom body _ =>
          let domH ← Lean.Meta.whnf dom
          -- The body's head is `ProgramSig.MachineState` (an `abbrev`,
          -- but `whnf` doesn't always reduce projections). Match on
          -- either `PLean.MachineState` (post-reduction) or accept any
          -- forall whose domain is `MachineRef` and codomain head ends
          -- in `MachineState`. The second pass uses `isDefEq` against
          -- an existentially-quantified `MachineState`-codomained arrow.
          if domH.isConstOf ``PLean.MachineRef then
            let bodyHead := body.getAppFn.constName?
            match bodyHead with
            | some n =>
              if n == ``PLean.MachineState || n.toString.endsWith "MachineState" then
                found := some ldecl.fvarId
                break
            | _ => pure ()
        | _ => pure ()
      match found with
      | none => pure ()
      | some fvarId =>
        let fvarExpr := Lean.Expr.fvar fvarId
        let fvarStx ← fvarExpr.toSyntax
        let stx ← `(tactic| (
          obtain ⟨pStateOf, hStateOf⟩ :
              ∃ pStateOf : PLean.MachineRef → _,
                ∀ m, ($fvarStx m).currentState = pStateOf m :=
                ⟨_, fun _ => rfl⟩
          simp only [hStateOf] at *))
        try evalTactic stx
        catch _ => pure ()

/-- Pre-SMT normalisation: simp the `pverifySimp` set, destruct
state hypotheses, strip `WithName` wrappers, abstract compound machine
lookups, then unfold the default-invariant predicates so lean-auto's
monomorphizer sees applied uninterpreted symbols and concrete
`Label`/`Nat`/`Bool` atoms instead of `Higher order input?`-flagged
shapes.

Step ordering: `simp [pverifySimp]` must precede `sdestruct_state` so
`addSent` / `addReceived` / etc. expand into record literals while
the state still has its struct form; the subsequent destruct +
`dsimp only` iota-reduces `{ sent := f, ... }.sent l` to `f l`.
`abstract_machine_lookups` runs after the destruct so it sees the bare
`machines` field applied to any compound payload-extractor ref.
`PLean.stateOf` is unfolded early so state-comparison reads in
quantifier bodies (`stateOf x s = <S>_st`) surface as the underlying
`(s.machines _).currentState` projections, which lean-auto translates
via the `<S>_st` `@[reducible] def` aliases to raw `S` constructors
the solver can decide. -/
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
`pverify_smt_close` succeeded (the solver returned `unsat`). Hits
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
  descr := "If true, `pverify_smt_close` takes an inlined branch that \
            instruments each stage (cache lookup, prep, lean-auto, \
            solver, assign) with `IO.monoNanosNow` timers and records \
            into `PLean.Verify.Profile.stateRef`. `#pverify` emits a \
            summary table on completion. OFF by default — the inlined \
            branch is not bit-identical to upstream `loom_smt` and is \
            kept off the hot path."
}

/-! ## SMT discharge

`pverify_smt_close` consults the obligation cache, then on miss runs
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
Records into `PLean.Verify.Profile.currentRowRef`. NOT bit-identical
to upstream `loom_smt` (no `Goal proven by …` log, no
`retryOnUnknown` cross-solver fallback by default), so we keep it
behind `set_option pverify.profile true`. -/
def pverifySmtCloseProfiled : TacticM Unit := do
  let useCache := pverify.cache.get (← getOptions)
  let mv ← getMainGoal
  let goalType ← mv.getType
  let cacheHash? : Option (String × String) ←
    if useCache then
      try
        let (text, ppNs) ← liftM (m := MetaM)
          (PLean.Verify.Profile.timeMetaNanos (pverifyGoalToCacheText mv))
        liftM (m := IO) (PLean.Verify.Profile.currentRowRef.modify
          (fun r => { r with cachePp := r.cachePp + ppNs }))
        let (h, hashNs) ← liftM (m := IO)
          (PLean.Verify.Profile.timeNanos (pure (pverifyHash text)))
        liftM (m := IO) (PLean.Verify.Profile.currentRowRef.modify
          (fun r => { r with cacheHash := r.cacheHash + hashNs }))
        pure (some (h, text))
      catch _ => pure none
    else pure none
  let cacheHit : Bool ←
    match cacheHash? with
    | some (hash, _) =>
      let (hit, fsNs) ← liftM (m := IO)
        (PLean.Verify.Profile.timeNanos (pverifyCacheHas hash))
      liftM (m := IO) (PLean.Verify.Profile.currentRowRef.modify
        (fun r => { r with cacheFs := r.cacheFs + fsNs }))
      if hit then
        let (_, assignNs) ← liftM (m := MetaM)
          (PLean.Verify.Profile.timeMetaNanos
            (mv.assign (mkApp (mkConst ``Loom.SMT.trust_smt) goalType)))
        liftM (m := IO) (PLean.Verify.Profile.currentRowRef.modify (fun r =>
          { r with cached := true, smtAssign := r.smtAssign + assignNs }))
        pure true
      else pure false
    | none => pure false
  unless cacheHit do
    let (_, prepNs) ← PLean.Verify.Profile.timeTacticNanos
      (evalTactic (← `(tactic| pverify_smt_prep)))
    liftM (m := IO) (PLean.Verify.Profile.currentRowRef.modify (fun r =>
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
    liftM (m := IO) (PLean.Verify.Profile.currentRowRef.modify (fun r =>
      { r with smtAuto := r.smtAuto + autoNs }))
    let ((res, solverUsed), solverNs) ← liftM (m := MetaM)
      (PLean.Verify.Profile.timeMetaNanos
        (Loom.SMT.querySolver cmdString withTimeout
          (forceSolver := Loom.SMT.specifiedSmtSolver (loom.solver.get opts))
          (retryOnUnknown := loom.solver.smt.retryOnUnknown.get opts)))
    liftM (m := IO) (PLean.Verify.Profile.currentRowRef.modify (fun r =>
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
      liftM (m := IO) (PLean.Verify.Profile.currentRowRef.modify (fun r =>
        { r with smtAssign := r.smtAssign + assignNs }))
      if let some (hash, text) := cacheHash? then
        liftM (m := IO) (pverifyCacheInsert hash text)

syntax "pverify_smt_close" : tactic
elab_rules : tactic
  | `(tactic| pverify_smt_close) =>
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
          pverifySmtDiagRef.set (some msg)
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
`∧`, then call `pverify_smt_close` on each resulting subgoal. Sound: a
proof of `A ∧ B` follows from independent proofs of `A` and `B`.

Motivation: large user-invariant bundles (LockServer's 11-conjunct
`system_config`, the 5-conjunct `safety` etc.) often return `unknown`
from the solver as a single-shot query even though each conjunct is
individually decidable in well under the timeout. Splitting before
discharge keeps the per-query size small and shifts more obligations
from `unknown` to closed-by-SMT.

Cost: N solver invocations instead of 1 on the bundle. Wired as a
*fallback* after the whole-bundle `pverify_smt_close` so the common
single-shot case is unaffected.

The 32-iteration cap is a safety bound (real bundles flatten in <16
levels); a goal with no `∧` head goes straight to the closing tactic
on the unmodified goal. -/
syntax "pverify_split_smt_close" : tactic
elab_rules : tactic
  | `(tactic| pverify_split_smt_close) => withMainContext do
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
        evalTactic (← `(tactic| all_goals pverify_smt_close))

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

/-- Sub-expression guard: fail unless the goal type mentions one of the
four default-invariant constants (`DefaultInvariants` / `UniqueActions`
/ `IncreasingCount` / `ReceivedSubsetSent`) somewhere in its tree.

`default_inv` runs `refine ⟨?_, ?_, ?_⟩` and per-conjunct rcases
tactics shaped for those four constants, so applying it to a goal that
contains *no* default-invariant content (e.g., a user invariant
`P1 ∧ P2 ∧ P3` of unrelated 3-arity) could mangle the goal. The guard
makes that fall-through cheap: `first | default_inv | …` rejects
unfit goals at entry. The check is permissive — the obligation
generator's post is `lemmaPred s ∧ DefaultInvariants s ∧ … `, so any
real obligation contains the constants. Pure user-invariant goals do
not. -/
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

/-- Walk the local context and `obtain`-split every `(A ∧ B)` /
`(A ∧ B ∧ C)` hypothesis. After `pverify_step_wp` + `intros` the
precondition lives as a folded conjunction in one anonymous hypothesis;
flattening lets `solve_by_elim` / `assumption` find each clause by
type.

The 16-iteration cap is a safety bound, not a real limit (any real
obligation flattens in a handful of steps). Implemented as `elab_rules`
so it can use `Lean.Meta.inferType` to inspect hypothesis types. -/
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

Every send-handler `@[pverifyProof]` in the M3 ports follows the same
mechanical bundle-split + per-conjunct dispatch. The four helpers below
name the (clause shape, step footprint) pair so a proof reads as a
dispatch table.

The shape comments use Hoare-triple notation: `{ Pre } step { Post }`,
where `Pre` lists the hypotheses available, `step` is the primitive
footprint, and `Post` is the goal the tactic discharges. `s` is the
pre-state, `s'` the post-state.

| Helper                              | { Pre }                                                | step                | { Post }                            |
|-------------------------------------|--------------------------------------------------------|---------------------|-------------------------------------|
| `pverify_carry_through`             | `{ h : P s' }`                                         | (any)               | `{ P s' }`                          |
| `pverify_carry_after_recv h`        | `{ h : P s }`                                          | `markReceived lbl`  | `{ P s' }`     (P is recv-monotone) |
| `pverify_not_inflight h, hisE, isW` | `{ h : ¬ inflight e s, hisE : is_<ev> e }`             | `send <newEv>`      | `{ ¬ inflight e s' }`               |
| `pverify_inflight_by h using x => …`| `{ h : inflight e s', user discriminator on new lbl }` | `send <newLbl>`     | `{ inflight e s }`                  |

`pverify_carry_through` and `pverify_carry_after_recv` close goals where
the post-state clause already follows from a pre-state hypothesis (no
quantifier instantiation needed). `pverify_not_inflight` and
`pverify_inflight_by` handle the routing-clause case where the clause's
`∀ e` quantifier ranges over a buffer that the step grew, so the
freshly-sent label has to be ruled out before the pre-state hypothesis
fires on old labels. -/

/-- Close a goal whose post-state predicate already holds verbatim from
some pre-state hypothesis. The clause's content (and the step) need not
satisfy any structural invariant: this is the "the proof obligation is
literally something we already know" case.

```
  { h : P s' }  (any step)  ⊢  P s'
```

Tries `assumption`, `solve_by_elim`, then `simp_all` (as a last resort
to close residual definitional unfolding). Typical use: `else`-branches
of an `if` in the handler, where the machine update is conditional and
the inactive branch leaves the entire post-state predicate equal to a
pre-state conjunct already in scope. -/
syntax "pverify_carry_through" : tactic
macro_rules
  | `(tactic| pverify_carry_through) => `(tactic| (
      try intros
      first
        | assumption
        | solve_by_elim
        | (try simp_all
           first | assumption | solve_by_elim)))

/-- Carry a clause from the pre-state through a step whose only
footprint is `markReceived lbl`. Such a step grows `received` and
leaves every other component (machines, `sent`, `actionCount`)
untouched — so `received↑` only shrinks `inflight`, and any
`inflight`-monotone predicate (typically `¬ inflight …` or
`inflight … → P`) transfers verbatim.

```
  { h : P s }   markReceived lbl   ⊢  P s'
```

`P` is supplied as a term applied to the clause's witnesses
(`hAcq e m hisE hTgt hm`, say). The helper normalises both surface
shapes the goal may take —
- `¬(A ∧ B)` (after `inflight` unfolds) and
- `(A ∧ B) → C` —
via `not_and` / `and_imp`, then discharges by feeding the user's
hypothesis through. -/
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
universally-quantified label) across a step that sends a fresh label
with a *different* event tag. The freshly-sent label is excluded by
event-tag mismatch against the bound `is_<ev> e` hypothesis; old
labels fall through to the pre-state's `¬ inflight e s`.

```
  { hPre : ¬ inflight e s, hisE : is_<ev> e }   send <newEv> ...
  ⊢  ¬ inflight e s'
```
where `<newEv> ≠ <ev>` is captured by passing `is_<wrong>` (the
predicate the freshly-sent label satisfies but the clause's `e` does
not).

Arguments:
1. `hPre` — the pre-state hypothesis already applied to the clause
   witnesses (e.g. `hAcq e m hisE hTgt hm`).
2. `hisE` — name of the `is_<ev>` hypothesis in context (typically
   introduced by the surrounding `intro e m hisE …`).
3. `is_<wrong>` — the freshly-sent label's event predicate; used to
   discharge the new-label case by contradiction on `hisE`.

Idiomatic call site:
```lean
case aq =>
  intro e m hisE hTgt hm
  pverify_not_inflight (hAcq e m hisE hTgt hm), hisE, is_eAquire
``` -/
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
that performs one fresh `send` (and `markReceived`). The case for the
freshly-sent label is left to the caller via a user-supplied
discriminator — used when no automatic event-tag mismatch is
available (the new label and `e` share an event tag, or the
discriminator is on the action constructor rather than the tag).

```
  { hinfe : inflight e s', user closes "e ≠ newLbl" or its consequence }
  send <newLbl> ...
  ⊢  inflight e s
```

```lean
pverify_inflight_by $hinfe using $hNewIdent => <tac>
```

`$hNewIdent` is bound to `e = newLbl` (the new-label witness) inside
`<tac>`, which must derive `False`. Two recurring shapes:
- `goto`-branch (different action constructor on the new label):
  `using h => rw [h] at hacte; simp at hacte` — the new label's
  action is `goto _`, not `event _`, contradicting the clause's
  `e.action = .event …`.
- forwarding-branch (same event tag, but an explicit `hee : e ≠ newLbl`
  is in context): `using h => exact hee h`.

Companion to `pverify_not_inflight` (negative form, automatic
discriminator) and to `pverify_carry_after_recv` (no fresh send). -/
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

syntax (name := pverifyTactic) "pverify" : tactic
syntax (name := pverifyDefaultTactic) "pverify_default" : tactic

/-- Shared closing chain for `pverify` / `pverify_default`. Tries
`default_inv` first (cheap when the goal head matches one of the four
default-invariant constants — guarded otherwise), then single-shot SMT,
then split-per-conjunct SMT for `unknown`-prone bundles, then the
arithmetic / boolean fallback. The split branch costs N solver calls on
an N-conjunct goal, so it sits AFTER single-shot — the common case is
one query. -/
syntax "pverify_close_chain" : tactic
macro_rules
  | `(tactic| pverify_close_chain) => `(tactic| (
      first
        | default_inv
        | pverify_smt_close
        | pverify_split_smt_close
        | pverify_grind))

/-- Inverse-ordered close (SMT first), used by `pverify_default` whose
goal arrives with explicit `DefaultInvariants` content the SMT path
handles directly without needing `default_inv`'s case split. -/
syntax "pverify_close_chain_smt_first" : tactic
macro_rules
  | `(tactic| pverify_close_chain_smt_first) => `(tactic| (
      first
        | pverify_smt_close
        | pverify_split_smt_close
        | default_inv
        | pverify_grind))

/-- The headline auto-discharge tactic.

`pverify_step_wp` already runs `wpgen` (which itself opens the triple
via `wpgen_intro`); calling `pverify_open_triple` here would re-try
`WPGen.intro` past the triple boundary and fail. `pverify_open_triple`
remains exposed standalone for users who want full control over the
manual proof's structure. -/
macro_rules
  | `(tactic| pverify) => `(tactic| (
      first
        | -- `pverify_step_wp` alone closes it (trivial / `True`-shaped
          -- post). Guard with `done` so this branch only succeeds when
          -- no goals remain — otherwise a follow-on `intros` would error
          -- with "no goals" on an already-closed goal.
          (pverify_step_wp
           done)
        | -- Post-equals-pre branch: the post-state predicate is
          -- structurally equal to one of the introduced pre-state
          -- hypotheses, so `assumption` closes the goal directly. This
          -- subsumes the trivial `pure ()` handler case but does NOT
          -- require the body to be `pure ()` — any handler whose post
          -- happens to match a pre-clause hits this branch first.
          (pverify_step_wp
           intros
           assumption)
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
           -- Unfold so the precondition's `DefaultInvariants s` becomes
           -- `(∀ a b, ...) ∧ (∀ a, ...) ∧ (∀ a, ...)` and can be split.
           simp only [PLean.DefaultInvariants, PLean.UniqueActions,
                      PLean.IncreasingCount, PLean.ReceivedSubsetSent] at *
           split_conjunction_hyps
           pverify_close_chain_smt_first)
    ))

end PLean
