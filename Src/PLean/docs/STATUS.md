# PLean — Status

Living tracker for in-progress and completed work. Update whenever a phase
checkbox in [`PLAN.md`](PLAN.md) flips, when a blocker appears or clears, or
when a design decision changes. Keep notes terse — link to commits/PRs/issues
rather than narrating.

**Conventions**
- Status: ☐ not started · ◐ in progress · ☑ done · ✗ blocked · ⊘ deferred
- "Owner" = whoever has it on their plate this week (initials are fine)
- Dates use ISO format (YYYY-MM-DD)

---

## At a Glance

| Phase | Status | Owner | Started | Finished | Notes |
|---|---|---|---|---|---|
| 0 — Bootstrap                    | ☑ | — | 2026-05-28 | 2026-05-29 | M0 reached |
| 1 — Semantic core                | ☑ | — | 2026-06-01 | 2026-06-04 | M1 reached |
| 2 — Registry + minimal surface   | ☑ | — | 2026-06-05 | 2026-06-05 | M2 reached |
| 3 — Verification declarations    | ◐ | — | 2026-06-06 | — | infra in place; tactic R15 |
| 4 — Spec machines                | ☐ | — | — | — | |
| 5 — Remaining surface            | ☐ | — | — | — | |
| 6 — Tutorial port                | ☐ | — | — | — | |
| 7 — Stretch / future             | ⊘ | — | — | — | post-v1 |

---

## Active Work

_Phase 3 (Verification declarations) is in progress (started 2026-06-06).
The full surface + obligation-generator infrastructure is in place
and a trivial-handler PingPong demo verifies clean via `#pverify` —
see [`Tests/Surface/Phase3PingPong.lean`](../Tests/Surface/Phase3PingPong.lean).
M3 acceptance (the three Tutorial/Advanced benchmarks) is gated on
R15 (`pverify` doesn't yet step through `<v>_get`/`<v>_set` accessors
and `send`/`goto`/`raise` primitives — fix is to emit per-accessor
`#derive_lifted_wp` declarations alongside the accessors). Detailed
plan in [`PLAN_P3.md`](PLAN_P3.md)._

**Phase 3 entry point** — [`#pverify M`](../PLean/Commands/PVerify.lean#L54-L72) becomes the user-facing
verification command: walks the registry, synthesises one per-handler
triple per (`Lemma`/`Theorem`, `prove`-directive, handler) tuple,
discharges each via the new PLean `pverify` tactic. The user writes
`Lemma`/`Theorem`/`Proof` blocks and **no hand-written `theorem
... := by ...`**.

Highlights from PLAN_P3:
- `Lemma`/`Theorem`/`Proof` blocks (D19) parse and register into the
  pmodule registry; `prove X using Y, Z;` directives become
  obligation-generation directives.
- `m is <MachineKind>` machine-kind predicate (D20) extends the
  Phase-2 `is` notation; needed by every benchmark with multiple
  machine kinds. Resolves R14.
- `init-condition`s aggregate into a single `<Mod>.InitConditions`
  precondition that flows into every obligation (D21).
- `pverify` tactic (D22) — PLean's `loom_solve`-equivalent. The
  Phase-2 experiment confirmed `CaseStudies.Tactic.loom_solve`
  doesn't fit (it requires Cashmere's `WithName` registration),
  so PLean re-composes `wpgen` + simp + `loom_intro` chain + grind
  fallback in `Verify/Tactic.lean` (file does not yet exist).
- Handler `_wrapped` form (D27) injects `markReceived` automatically,
  closing the M2 surface↔M1 gap.

**M3 acceptance set** — three Tutorial/Advanced benchmarks port to
PLean and verify via `#pverify`, no hand-written triples:
1. [`Tutorial/Advanced/6_DistributedLock`](../../../Tutorial/Advanced/6_DistributedLock/) — minimum (40 lines, ~8 VCs).
2. [`Tutorial/Advanced/8_LockServer`](../../../Tutorial/Advanced/8_LockServer/) — exercises machine-kind `is` (96 lines, ~20 VCs).
3. [`Tutorial/Advanced/3_RingLeaderVerification`](../../../Tutorial/Advanced/3_RingLeaderVerification/) — exercises `Lemma using` chain (92 lines, ~25 VCs).

Heavier benchmarks (Consensus, 2PC, ChainReplication, Paxos) need
Phase 4 (spec machines) and/or Phase 5 (`foreach`, maps) and are
deferred from M3.

**Current surface keyword truth** (authoritative; the per-file syntax
decls are the source):
- From P verbatim: `event`, `eventset`, `enum`, `type`, `machine`,
  `spec`, `state`, `entry`, `on`, `goto`, `var`, `send`, `raise`,
  `announce`, `invariant`.
- From P (capital initials, Phase 3 — D19): `Lemma`, `Theorem`, `Proof`,
  `prove`, `using`. The identifier `default` is reserved as a
  `prove default;` sentinel; `Lemma default { ... }` and
  `Theorem default { ... }` are rejected at registration time.
- `p`-prefixed (Lean collision): `pmodule`, `paxiom`, `pinstance`.
- Renamed (Lean collision): `pure` → `function`, `init` → `init-holds`.
- Finalisation command: `#gen_module M`; checks: `#pwf M`, `#pverify M`;
  debug: `#print_pmodule M`.
- Event payload abbrev is `<ev>_payload` (underscore, not dot).

**Note on the Done log below**: the first "Phase 0 — Bootstrap" entry
records the *initial* landing and mentions superseded spellings
(`<ev>.payload`, `init`, `pure`, `#endmachine`/`end M`). The
"Refinement" entry immediately after it supersedes those. When in doubt,
trust this Active-Work summary and the code, not the historical entry.

<!-- Template — copy when starting a task:
### <Phase N — short task name>
- **Status**: ◐ in progress
- **Owner**:
- **Started**: YYYY-MM-DD
- **Branch / PR**:
- **Summary**: one sentence on what's being built
- **Done when**: concrete exit criterion
- **Notes**:
-->

---

## Done

### Phase 3 — Verification declarations (partial) · 2026-06-06
- **What landed**:
  - [`Internal/Decls.lean`](../PLean/Internal/Decls.lean) — new
    records `PLemmaDecl` and `PProofDecl` with `PProveDirective`
    (D19). Tracks `Lemma`/`Theorem` invariant-bundle membership and
    `Proof` directive lists.
  - [`Internal/Registry.lean`](../PLean/Internal/Registry.lean) —
    `LocalPModuleCtx` extended with `lemmas` / `lemmaOrder` /
    `proofs` / `invariantOrder`; cross-file merge handles them.
    `addLemma` / `addProof` helpers.
  - [`Surface/Verify.lean`](../PLean/Surface/Verify.lean) —
    `Lemma <name> { invariant ... }`, `Theorem <name> { ... }`, and
    `Proof <name>? { prove X using Y, Z; prove default; }`
    commands (D19). Each `invariant` inside a Lemma block is also
    registered as a free-standing `PInvariantDecl` so cross-references
    via `using` resolve to the individual prop. The `default` token
    is *not* introduced as a keyword (would break Lean's `default`
    term used by Inhabited derives); the elaborator dispatches on
    the identifier name string.
  - [`Semantics/Label.lean`](../PLean/Semantics/Label.lean) —
    `MachineState` extended with `kind : Nat := 0` field (D20). The
    default keeps M1/M2 hand-written examples building. Also
    propagated through `Primitives.goto` and the `_set` accessor
    emission so the `kind` field flows through state updates.
  - [`Commands/GenModule.lean`](../PLean/Commands/GenModule.lean) —
    new `emitMachineKinds` step (D20) emits per-pmodule
    `<Mod>.MKind` inductive (one ctor per machine, plus a `_none`
    sentinel for R20 — kind=0 reserved for "unset"),
    `<Mod>.<M>_kind : Nat` constants, `<Mod>.<M>_allocated` predicates,
    and top-level `is_<M>` aliases that the surface `is` macro
    reaches via Lean's namespace search. Plus new
    `emitInitConditions` (D21) folding all `init-holds <prop>`
    clauses into `<Mod>.InitConditions : GS → Prop`,
    `emitLemmaBundles` (D19) emitting per-lemma bundle predicates
    `<Mod>.<X> : GS → Prop`, and `emitUserInv` (D18) for the
    free-standing invariant union.
  - [`Verify/Tactic.lean`](../PLean/Verify/Tactic.lean) (NEW) —
    `pverify` / `pverify_default` / `default_inv` tactic syntax
    (D22, D28). The trivial-handler branch (`intro s h; exact h`)
    closes obligations whose handlers don't mutate state. Complex
    bodies (handlers reading/writing `var`s, calling `send`/`goto`)
    leave residual `WPGen.default (..._get/_set ...)` opaque
    applications — that's tracked under R15 below.
  - [`Verify/DispatcherContract.lean`](../PLean/Verify/DispatcherContract.lean)
    (NEW) — builds the per-handler precondition existential (D27)
    that mirrors M1's manual `inflight lbl ∧ lbl.target = this ∧ ...`
    shape.
  - [`Verify/Obligation.lean`](../PLean/Verify/Obligation.lean) (NEW) —
    `synthesise` walks the registry, emits one
    `theorem <Mod>.<M>.<S>.<ev>_correct_<X>` per (handler, prove-
    directive) pair (D18, D23, D24, D25), threading in the lemma
    bundle, `using`-clause assumptions, `InitConditions`, and the
    dispatcher contract. Per-obligation tactic failures are captured
    via command-level message-log inspection; failures populate
    `SynthesiseResult.failed` for the report (R19, partial — async
    snapshot errors still surface as build-level errors).
  - [`Commands/PVerify.lean`](../PLean/Commands/PVerify.lean) —
    rewired (D17 graduates) to call `Verify.synthesise` after
    `#pwf`. The output reports
    `<modName>: N obligations from K prove-directives, all ✓` or
    fails with the failed-obligation list.
  - [`Tests/Surface/Phase3Parse.lean`](../Tests/Surface/Phase3Parse.lean)
    (NEW) — parse-only smoke test for `Lemma`/`Theorem`/`Proof`.
  - [`Tests/Surface/Phase3PingPong.lean`](../Tests/Surface/Phase3PingPong.lean)
    (NEW) — **first end-to-end auto-discharge demo**: a no-`var`
    pmodule with a single trivial-handler theorem; `#pverify`
    walks the registry, emits 1 obligation, and closes it via the
    `pverify` tactic. **No hand-written `theorem ..._correct`
    line** — the headline Phase-3 deliverable.
  - [`Tests/Surface/PVerifyTactic.lean`](../Tests/Surface/PVerifyTactic.lean)
    (NEW) — confirms `pverify` discharges the M2 trivial-handler
    case in one line (replacing M2's `unfold; wpgen; intro _ h; exact h`
    tail).
  - [`Tests/Surface/Phase3DistributedLock.lean`](../Tests/Surface/Phase3DistributedLock.lean),
    [`Tests/Surface/Phase3LockServer.lean`](../Tests/Surface/Phase3LockServer.lean)
    (NEW skeletons) — port the surface of two M3 benchmarks. They
    parse and `#pwf` reports clean; `#pverify` is held until R15.
- **Notable design points**:
  - **D20 simplification**: per-machine `<M>.allocated` and the
    top-level `is_<M>` predicate are emitted as flat top-level
    defs (`<Mod>.<M>_allocated`, `<Mod>.is_<M>`) rather than under
    `namespace <M>`. The wrapper struct claims the `<M>` slot first;
    nesting `def allocated` under `namespace <M>` (where `<M>`
    is the structure) proved unreliable.
  - **`default` is not a keyword**. The `prove default;` form uses
    the identifier `default` and dispatches on its name string in the
    elaborator. Reserving it as a token would break the bare
    `default` term used by Inhabited-derived `⟨ctor default⟩`
    instances inside `Surface/Types.lean`'s named-tuple emission and
    `Commands/GenModule.lean`'s `<Mod>.E` Inhabited derive.
  - **Async-snapshot tactic-error capture is partial**. Modern
    Lean 4 reports `theorem ... := by ...` errors via the snapshot
    system rather than synchronously through `(← get).messages`.
    The obligation generator's per-obligation rollback catches
    synchronous errors but not snapshot-resolved ones (PLAN_P3 R19
    flagged this as expected). Work-around: failed obligations
    surface as build-level errors that name the failing
    `(handler, lemma)` triple.
- **REVIEW_P3 second-pass follow-up sweep (2026-06-06)**: the code
  review at [`docs/REVIEW_P3.md`](REVIEW_P3.md) was re-run after the
  first follow-up sweep. The second pass flagged residual issues
  (`default_inv` over-fire, theorem-name collision shapes, missing
  `DefaultInvariants` in obligation post, dead `idGS` / `idPP`,
  missing regression tests, Phase2PingPong rewrite, missing
  `ObligationShape.lean`). All addressed in this sweep:
  - **§1 (sharpened)** `default_inv` is now head-symbol-aware via
    `default_inv_guard` (an `elab_rules` tactic that fails unless
    the goal head is one of `DefaultInvariants` / `UniqueActions` /
    `IncreasingCount` / `ReceivedSubsetSent`). With the guard in
    place, `pverify` chains `(first | default_inv | pverify_solve)`
    safely — the over-fire risk that motivated the first pass's
    "don't wire it into `pverify`" workaround is gone.
  - **§A.1** Theorem-name collisions across `Proof` blocks now fully
    impossible: `<M>.<S>.<ev>_correct_<proofTag>_<target>[_using_...]`
    embeds the Proof block's tag (or `block<idx>` for anonymous
    blocks). Two `Proof Block1 { prove safety; }` /
    `Proof Block2 { prove safety; }` directives produce distinct
    theorem names.
  - **§A.3** Obligation pre/post both now include `DefaultInvariants`
    (PLAN_P3 D18 prescribed this; it was missing). Without this,
    a `prove safety;` chain could violate the sanity invariants
    between handlers as long as user-`safety` survived.
  - **§2.4 / §2.6** `default_inv` rewritten to `simp only [...]` (no
    bare `simp`); D28's stability promise restored.
  - **§2.4** `idGS` / `idPP` removed (dead code).
  - **§B.2** `synthesise` now `logInfo`-s when it skips a spec
    machine, so a `Proof { prove X; }` directive on a spec doesn't
    silently drop.
  - **§B.3** Doc comment on the per-obligation rollback now also
    notes that env state isn't rolled back (a partially-elaborated
    `theorem` decl can leak even when the message log is restored).
  - **§4 / §B.1** New
    [`Tests/Surface/Phase3Errors.lean`](../Tests/Surface/Phase3Errors.lean)
    `#guard_msgs`-pins each error path: `Lemma default` reserved,
    `Theorem default` reserved, `prove <unknown>;`, and
    `prove ... using <unknown>;`.
  - **§C / §6.6** Phase 2 / M2 manual proofs moved to
    [`Phase2PingPong_manual.lean`](../Tests/Surface/Phase2PingPong_manual.lean)
    (under pmodule `Phase2PingPongManual`); a new
    [`Phase2PingPong.lean`](../Tests/Surface/Phase2PingPong.lean)
    uses `Theorem` / `Proof` blocks against a trivial invariant. The
    full `PongAfterPing` triple from M2 still requires R15 to
    auto-discharge under `#pverify`, so the new file's `#pverify` is
    commented out for now (matching DistributedLock / LockServer).
  - **§6.7** New
    [`Tests/Surface/ObligationShape.lean`](../Tests/Surface/ObligationShape.lean)
    `#guard_msgs`-pins the exact obligation theorem signatures the
    generator emits — including the new Proof-block-tag suffix and
    the now-included `DefaultInvariants` post. Future refactors of
    `emitOneObligation` cannot silently change emission shape.
- **REVIEW_P3 third-pass polish sweep (2026-06-06)**: a narrow third
  pass against [`docs/REVIEW_P3.md`](REVIEW_P3.md) caught residual
  details after the second sweep landed. Addressed:
  - **§2.1** `synthesise` previously rolled back the *entire*
    message log on per-obligation failure, which erased prior
    `logInfo`/warning entries (e.g., the `B.2` spec-skip messages
    from earlier obligations in the same `synthesise` run). The
    rollback now filters only `.error`-severity messages from the
    new-message slice and re-merges the rest into `savedSt.messages`.
  - **§2.2** `default_inv_guard`'s error message used to name the
    *user* constant the goal head resolved to ("'`safety`' is not a
    default-invariant constant" — confusing). Replaced with a
    goal-shape-agnostic "`default_inv`: applies only to goals headed
    by `DefaultInvariants`, `UniqueActions`, `IncreasingCount`, or
    `ReceivedSubsetSent`".
  - **§2.3** Added explicit positive regression
    [`Tests/Surface/Phase3DuplicateTarget.lean`](../Tests/Surface/Phase3DuplicateTarget.lean):
    two `Proof Block1`/`Block2 { prove safety; }` directives + two
    `#check`s that confirm both disambiguated theorem names exist.
    `ObligationShape.lean` covered this implicitly; this file covers
    it explicitly.
  - **§2.5** Dropped the unused empty-pmodule pre-declaration in
    `Phase3Errors.lean`'s `Lemma default` test (refactor leftover);
    file-level comment now notes that `#guard_msgs` must appear
    *inside* the open pmodule, not before/after.
  - **§2.6** Added [`Tests/Surface/Phase3R20.lean`](../Tests/Surface/Phase3R20.lean) —
    four `decide`-based examples pinning the R20 mitigation: `kind = 0`
    is rejected by `<M>_allocated`, kind matching `<M>_kind` is
    accepted, kind matching the *other* machine's kind is rejected.
    If the `kind ≠ 0` half of the conjunction is reverted, this file
    fails to elaborate.
  - **Implication-chain note** in `Verify/Obligation.lean`: the
    obligation generator's pre-shape comment now records that
    `A → B → C → D` (curried implications in user invariant bodies)
    is logically equivalent to `A ∧ B ∧ C → D` for grind / omega
    purposes — users can write either shape.
  - **`Tests/Semantics/SmtRoundtrip.lean` strengthened** (separate
    review item, same day): from a single trivial `x = x` proof to
    three cvc5-discharged goals — quantified linear arithmetic,
    existential over an uninterpreted `Nat → Prop`, plus the
    original binary-reachability check. Finding logged in the file
    header: `loom_smt`'s lean-auto translation rejects PLean
    `GlobalState`-bearing goals as "higher-order input"; Phase 3's
    deferred `loom_smt` fallback in `pverify_solve` (REVIEW_P3 §2.5)
    will need a defunctionalisation pre-pass or a goal-shape gate
    before SMT can discharge GlobalState-shaped triples.
- **REVIEW_P3 first-pass follow-up sweep (2026-06-06)**: code review at
  [`docs/REVIEW_P3.md`](REVIEW_P3.md) called out infrastructure bugs
  beyond R15. Addressed in the same day:
  - **§1.1** `default_inv` was declared but never invoked. Wired it
    into `pverify_default` (`first | default_inv | pverify_solve`);
    intentionally *not* wired into the non-default `pverify` because
    its `refine ⟨?_, ?_, ?_⟩` over-fires on any 3-conjunct user
    invariant — see `Verify/Tactic.lean` docstring.
    (*Second-pass update*: `default_inv` is now head-symbol-gated and
    *is* wired into `pverify` — see §1 sharpened in the sweep above.)
  - **§1.2 / §1.4** `using`-lemma bundles in the precondition stayed
    opaque in the goal. `Verify/Obligation.lean::emitOneObligation`
    now builds `usingUnfolds` and adds `try unfold $[...]:ident*`
    lines to all four proof-tactic branches (default × accessors).
  - **§1.3** Theorem names embed a `_using_<L1>_<L2>_...` suffix when
    `using`-clauses exist, so `prove safety;` and
    `prove safety using system_config;` no longer collide.
  - **§2.3** Stripped misleading docstring claims about
    `pverify using ...` / `pverify default_only` (neither
    implemented). Replaced with an honest "Variants (Phase-3 MVP)"
    section noting the obligation generator handles `using`-unfolding.
  - **§2.4** Added `detectUsingCycles` DFS pass (~30 lines) before
    `synthesise`'s emission loop. Raises an explicit error
    `cycle in `using`-lemmas detected: A → B → A` (R17 mitigation).
  - **§4.1** Documented `Verify/DispatcherContract.lean` as inert
    (the helper's `fun (this) (param) (s) => ...` shape doesn't
    match the inline existential body Obligation.lean builds; kept
    file as docstring source-of-truth, not a live import).
  - **§4.3** Tightened `pverify_solve`: replaced blind
    `(constructor <;> grind)` with And-only `refine ⟨?_, ?_⟩ <;> grind`,
    added explicit `import Mathlib.Tactic.Tauto`, kept `tauto` as the
    terminal fallback (regressed Phase3PingPong without it; needed
    for the conjunction-projection-with-extra-existential goal shape).
  - **§4.6** `elabPProof` now validates that every `prove <X>` and
    `using <Y>` target name is either `default` or a registered
    `Lemma`/`Theorem` in the current pmodule. Typos surface at the
    `prove` line, not as cryptic tactic failures.
  - **§4.7** `Lemma default { ... }` and `Theorem default { ... }`
    are rejected at registration time (the name is reserved for the
    `prove default;` sentinel).
  - **§5.3** `<Mod>.<M>_allocated` predicate now requires
    `kind ≠ 0 ∧ kind = <M>_kind` per PLAN_P3 R20 mitigation.
  Build remains green for the test files that exercise it: Phase 2 /
  M2 hand-written triples (now in
  [`Phase2PingPong_manual.lean`](../Tests/Surface/Phase2PingPong_manual.lean)
  under pmodule `Phase2PingPongManual`); the smaller
  [`Phase2PingPong.lean`](../Tests/Surface/Phase2PingPong.lean) emits a
  trivial-invariant obligation through `#pverify`; Phase3PingPong's
  trivial-handler `#pverify` closes its 1 obligation;
  [`ObligationShape.lean`](../Tests/Surface/ObligationShape.lean)
  pins the obligation generator's emitted theorem-shape via
  `#guard_msgs` (REVIEW_P3 §6.7);
  [`Phase3Errors.lean`](../Tests/Surface/Phase3Errors.lean) pins the
  `#guard_msgs`-tested error paths for the §4.6 / §4.7 validations.
  Phase3DistributedLock and Phase3LockServer parse and `#pwf`-clean,
  but their `#pverify` invocations remain commented out pending R15
  (see "Open follow-ups" below). "Build green" therefore means
  "syntax / well-formedness / obligation-shape regressions all hold",
  not "every M3 obligation closes" — that's gated on R15.
- **Deferred from REVIEW_P3 (action-class b — design judgment first)**:
  - **§2.1** D27 `_handler_wrapped` form not implemented. Existing
    obligations keep the M2-shape existential precondition. Either
    the wrapper lands in a Phase-3b pass, or PLAN_P3 D27 is
    rewritten to record the existential as the chosen approach.
  - **§2.2** `default_inv` is still a placeholder (single
    `simp [...]` chain with `try omega`, not the bounded
    `mini-tactic + rcases` case-table D28 prescribes). Working for
    M1/M2 but won't survive `markReceived + send` handlers.
    Re-implementation tracked but not blocking.
  - **§2.5** No `loom_smt` SMT fallback in `pverify_solve` yet
    (D22 step 4). Gating decision (on-by-default vs `loom.solver`-
    gated) deferred to whoever ships the first benchmark needing it.
  - **§2.6** `is` macro is not registry-aware (D20 prescribes
    explicit dispatch + bespoke error). Currently relies on Lean's
    name resolution. Either macro is reworked or PLAN_P3 D20 is
    softened to match.
- **Open follow-ups (R15 — still gating M3)**:
  - Emit `#derive_lifted_wp` per-accessor (`<v>_get` / `<v>_set`)
    alongside their definitions in `emitVarAccessors`. Without
    these, `wpgen` falls back to `WPGen.default` on every state
    read/write inside a handler body.
  - Emit `loomSpec` lemmas for `PLean.send` / `PLean.raise` /
    `PLean.goto` / `PLean.markReceived` so `wpgen` can step through
    the framework primitives without `WPGen.default` opacity.
  - With those in place, extend `pverify` to actually close the
    DistributedLock/LockServer obligations end-to-end. M3 is then
    achievable.
  - `RingLeaderVerification` requires `paxiom` over `pure` functions
    (`le`, `btw`, `right`) flowing into the `pverify` context as
    hypotheses; the `paxiom` machinery exists (Phase 0) but the
    flow into `pverify` is wiring not yet done (R18).
- **What landed**:
  - [`Surface/Notation.lean`](../PLean/Surface/Notation.lean) (NEW) — scoped `notation:50 a " ≺ " b =>
    PLean.precedes a b`, plus `lbl is ev` / `lbl targets m` notations
    that desugar to the predicates in [`Semantics/Predicates.lean`](../PLean/Semantics/Predicates.lean)
    (decision D16).
  - [`Commands/GenModule.lean`](../PLean/Commands/GenModule.lean) (NEW; [`#gen_module`](../PLean/Commands/GenModule.lean#L401-L440) extracted from
    [`Surface/Machine.lean`](../PLean/Surface/Machine.lean)) — synthesises per-pmodule union types
    `<Mod>.E`/`<Mod>.G`/`<Mod>.S`/`<Mod>.Fields` from the registry
    (D8), emits the `<Mod>.Sig`/`PM'`/`GS` aliases, and issues the two
    `#derive_lifted_wp` calls for `get`/`set` (D14). Per-machine
    wrapper structs `structure <MName> where ref : MachineRef
    deriving Inhabited, DecidableEq` plus `instance : Coe <MName>
    MachineRef` land here too (D10) — see [`emitMachineWrappers`](../PLean/Commands/GenModule.lean#L100-L111).
  - [`Surface/Stmt.lean`](../PLean/Surface/Stmt.lean) — macros repointed onto real PM primitives
    (D11). [`send target, ev, payload`](../PLean/Surface/Stmt.lean#L73-L110) becomes
    `PLean.send (P := Sig) target (E.<ev> payload)`; [`goto S`](../PLean/Surface/Stmt.lean#L137-L149) becomes
    a real transition via the per-machine `<S>_st` alias (D13);
    [`var x = expr`](../PLean/Surface/Stmt.lean#L167-L178) writes through the per-pmodule `Fields` slice via
    `<x>_set this.ref expr` and rebinds `x` to the new value (D12).
  - [`Surface/Machine.lean`](../PLean/Surface/Machine.lean) — handler defs now take `(this : <MName>)`
    (the wrapper struct) and have type `<Mod>.PM' Unit` (real
    `NonDetT (StateT (GlobalState Sig) DivM) Unit`). Every handler
    body is prefixed with `let v ← v_get this.ref` for each machine
    var, so user-level reads compile (D11/D12).
  - `Internal/Stub.lean` — **deleted** (D15) (no longer in tree). `PLean.lean` no longer
    imports it; nothing else does.
  - [`Commands/PVerify.lean`](../PLean/Commands/PVerify.lean) — extended past the [`#pwf`](../PLean/Commands/PWf.lean#L113-L132) delegation with
    a Phase-2 [structural check on handler-def existence](../PLean/Commands/PVerify.lean#L31-L72) (D17).
    Phase-3 obligation generation lands later.
  - [`Tests/Surface/Phase2PingPong.lean`](../Tests/Surface/Phase2PingPong.lean) — **M2**. Ports a small
    surface ping-pong (no payloads on `ePong`, single-field `ePing`
    payload `MachineRef`) onto `#gen_module`-emitted handlers and
    proves two handler triples by `wpgen` + the same manual tail M1
    used. The user invariant uses the `≺` notation. *Note:* the
    handler bodies don't include `markReceived` (the dispatcher's
    responsibility in production), so the M2 proofs are slimmer than
    M1's — flagged as the intentional surface↔M1 difference.
  - [`Tests/Surface/Combinators.lean`](../Tests/Surface/Combinators.lean) — `.run`-based regression for the
    surface emission. Verifies `var count = count + 1` actually
    increments the field across handler invocations and that
    `<S>_st`/`goto` keep `currentState` consistent.
- **Notable design points**:
  - **Macro hygiene**: a long stretch of debugging revolved around
    bare identifiers inside macro quotations getting hygiene marks.
    The convention now is: any identifier that needs to resolve
    against a user-namespace constant emitted by `#gen_module` is
    constructed via `mkIdent` and spliced in (`$idSig`,
    `$idMachineRef`, etc.). `Stmt.lean` and `GenModule.lean` document
    this with a header comment.
  - **`Inhabited E`**: derived for the empty-event case via a `_none`
    placeholder ctor; for the non-empty case via an explicit
    `instance : Inhabited E := ⟨E.<firstEv> default⟩` over the first
    declared event's payload. Worked out cleanly because Phase-2
    auto-derives `Inhabited` on named-tuple structs.
  - **`DecidableEq`** on machine wrapper structs and named-tuple
    payloads matters for `<Mod>.E`'s derive: a user `event ev :
    SomeStruct` chains into `DecidableEq SomeStruct`, which our
    `structure` materialisation derives. Phase 0 left these
    underived; Phase 2 adds them.
  - **`open PartialCorrectness DemonicChoice` inside emission**: the
    `#derive_lifted_wp` call is wrapped in a per-call `open …in` so
    the scoped `MAlgOrdered` instances are available *only* during
    that elaboration step. We don't bake the `open` into a surrounding
    file; that would force every importer of a generated pmodule into
    partial-correctness + demonic-choice mode, which we want as a
    user-visible decision in the test/proof file (M1, M2 both do it).
  - **No `markReceived` in surface emission**: Phase-2 surface lacks
    a way to say "first mark this label received." M1 hand-writes it
    inside each handler. The Phase-2 surface emission omits it; the
    M2 proofs adapt by carrying a precondition existential rather
    than relying on `lbl`. Phase 3's obligation generator will
    introduce a uniform `markReceived` prologue.
- **Anticipated risks resolved**:
  - **R8 — Synthesised inductive ordering** ✓. The fixed pipeline in
    [`Commands/GenModule.lean`](../PLean/Commands/GenModule.lean#L401-L440) (machine wrappers → types → events →
    Sig/E/G/S/Fields → `#derive_lifted_wp` → per-machine accessors +
    handlers → verify decls) is rigid and documented inline.
  - **R10 — Named-tuple payload positionality** ✓. The `send`
    macro's named-tuple form ascribes `({ … } : <ev>_payload)` and
    feeds the result to `E.<ev>`; works for the structure form. The
    Phase-2 PingPong demo (Examples) exercises the multi-field
    case.
  - **R11 — Multiple machines sharing a state name** ✓ via
    `<MName>_<SName>` ctor naming in `<Mod>.S`.
  - **R13 — `Coe <MName> MachineRef` doesn't fire through every
    macro emission site** ✓. Macros explicitly thread `(target :
    MachineRef)` ascriptions where they're called for; Coe only
    needs to fire at user-written call sites where it's reliable.
- **Open follow-ups for Phase 3**:
  - Synthesise per-handler triple lemmas mechanically (M3 milestone).
  - Ship PLean's own `pverify` tactic wrapping `wpgen` + a
    configurable `loom_solve`-equivalent.
  - Auto-generate the `default`-invariant obligations.
  - Decide how to handle `markReceived` uniformly (likely as a
    handler-prologue inserted by the obligation generator).

### Phase 0 — Bootstrap · 2026-05-29
- **What landed**:
  - Lake project skeleton (`lakefile.lean`, `lean-toolchain` v4.24.0,
    `dependencies.toml`, Loom pinned to velvet's revision).
  - Internal: `Stub.lean` (PM := Id stub; deleted in Phase 2), [`Decls.lean`](../PLean/Internal/Decls.lean) (metadata
    records — no deep AST), [`Registry.lean`](../PLean/Internal/Registry.lean) (two-tier env extension:
    scoped `localPModuleCtx` + persistent `pmoduleExt` with field-wise
    fragment merging on `addImportedFn`).
  - Surface: [`Module.lean`](../PLean/Surface/Module.lean) (`pmodule M`), [`Types.lean`](../PLean/Surface/Types.lean)
    (foreign sort via `opaque … : NonemptyType` + projection,
    abbrev alias, named-tuple via `structure`, enum via `inductive`),
    [`Events.lean`](../PLean/Surface/Events.lean) (event tag + payload abbrev), [`Machine.lean`](../PLean/Surface/Machine.lean)
    (`machine`/`spec`/`state`/`entry`/`on _ (param : T) {…}`/
    `on _ goto _`, with `_handler` suffix to avoid event-name shadowing),
    [`Stmt.lean`](../PLean/Surface/Stmt.lean) (named-tuple send `(f = v, …)` ascribed via
    `<ev>.payload`, plus raw-term and no-payload forms), [`Verify.lean`](../PLean/Surface/Verify.lean)
    (`invariant`/`paxiom`/`init`/`pure`/`pinstance`).
  - Commands: `#pwf` (well-formedness validator), `#pverify` (Phase-0
    wrapper over `#pwf`; Phase 3+ adds obligation generation),
    `#print_pmodule` (registry pretty-printer).
  - PingPong demo (Events / Server / Client / Top — four files, one
    `pmodule PingPong`) builds end-to-end and reports
    "well-formed (3 types, 2 events, 2 machines, 1 invariants, …)".
  - Bootstrap tests: `SingleFile.lean` (one-file pmodule with all
    surface forms, including `pinstance` over uninterpreted + built-in
    types), `MultiFile/{Events,Machine,Top}.lean` (cross-file
    aggregation), `Errors.lean` (`#guard_msgs`-pinned diagnostics for
    decl-outside-pmodule, undeclared-event-handler, undeclared-spec-
    observed-event).
- **Notable design choices made during implementation**:
  - Foreign sorts use `opaque … : NonemptyType` + `Type := pointed.type`
    + `Nonempty` instance (mathlib's standard pattern). Phase 1 will
    confirm Loom accepts this for SMT.
  - `paxiom`/`pinstance` got the `p` prefix because Lean's builtins
    intercepted the un-prefixed forms.
  - Dropped P's optional `receives [...]` / `sends [...]` clauses on
    `machine` headers — `#pwf` derives them from handlers/`send` calls.
    Can be added later without breaking the registry.
  - Dropped P's `do` token from `on ev (param : T) {…}` because Lean's
    tokenizer eagerly consumes `do` as a do-block opener.
  - Registry uses field-wise merge on import (`mergeCtx`) so two files
    contributing partial fragments to the same `pmodule M` aggregate
    correctly.
- **Follow-ups for Phase 1**: (1) Real `PM α := StateT GlobalState
  (NonDetT DivM) α` replaces the stub; (2) MAlgOrdered composition
  spike; (3) `≺` precedence operator wired up via
  `actionCount`-on-Label; (4) Loom integration smoke test on a
  hand-written ping-pong.

### Phase 1 — Semantic core · 2026-06-04
- **What landed**:
  - [`Semantics/Label.lean`](../PLean/Semantics/Label.lean) — `Label`, `EventOrGoto`, `MachineState`,
    plus `payloadOf`/`actionCount` accessors. Generic in `(E, G, S, F)`
    so the same record shape compiles once for any program; concrete
    unions are user-supplied (Phase-1 hand-written) or synthesised
    (Phase 2 `#gen_module`).
  - [`Semantics/GlobalState.lean`](../PLean/Semantics/GlobalState.lean) — `ProgramSig` bundle of program
    union types, `GlobalState P` record (`sent`/`received`/`machines`/
    `actionCount`), pure update helpers (`addSent`, `addReceived`,
    `bumpActionCount`, `updateMachine`), `Inhabited` instance.
  - [`Semantics/Monad.lean`](../PLean/Semantics/Monad.lean) — `PM P α := NonDetT (StateT (GlobalState P)
    DivM) α`, `PProp P := GlobalState P → Prop`, plus a compile-time
    sanity check (`MAlgOrdered (PM TrivialSig) (PProp TrivialSig)`)
    confirming the layer order resolves once `PartialCorrectness
    DemonicChoice` is open.
  - [`Semantics/Primitives.lean`](../PLean/Semantics/Primitives.lean) — `send` / `raise` / `goto` /
    `announce` / `newMachine` / `markReceived` as `PM` combinators
    that update `GlobalState` exactly per `Uclid5CodeGenerator.cs:
    1967-1999`.
  - [`Semantics/Predicates.lean`](../PLean/Semantics/Predicates.lean) — `inflight` / `sent` / `received` /
    `Label.isEvent?` (= P's `is`) / `Label.targets?` / `stateOf` /
    [`precedes`](../PLean/Semantics/Predicates.lean#L69-L72) (= P's `≺`, defined as `actionCount` comparison per
    decision D6).
  - [`Semantics/Default.lean`](../PLean/Semantics/Default.lean) — the three sanity invariants from
    `Uclid5CodeGenerator.cs:1189-1201`: [`UniqueActions`](../PLean/Semantics/Default.lean#L23), [`IncreasingCount`](../PLean/Semantics/Default.lean#L36), [`ReceivedSubsetSent`](../PLean/Semantics/Default.lean#L46), plus a [`DefaultInvariants`](../PLean/Semantics/Default.lean#L53-L54)
    bundle.
  - [`Tests/Semantics/StackSpike.lean`](../Tests/Semantics/StackSpike.lean) — Task 1: confirms every
    `MAlgOrdered`/`Monad`/`LawfulMonad` instance synthesises;
    discharges two trivial triples via `wpgen`. Locks D1.
  - `Tests/Semantics/HandPingPong.lean` — **M1**. A 2-machine
    (Server/Client), 4-handler ping-pong written directly as Lean
    defs over the real `PM`; proves the temporal user invariant
    "every `ePong` is preceded by some `ePing`" via the `≺` operator
    (the headline feature PVerifier cannot express). All four handler
    Hoare triples discharge end-to-end.
  - [`Tests/Semantics/Combinators.lean`](../Tests/Semantics/Combinators.lean) — `.run`-based regression for
    primitives. `decide`-evaluates buffer/counter deltas to match
    expectations (Cashmere's `#eval (prog).run.run.run init` pattern).
  - [`Tests/Semantics/SmtRoundtrip.lean`](../Tests/Semantics/SmtRoundtrip.lean) — Task 10: confirms `loom_smt`
    finds and runs cvc5 from a PLean source dir (R3 cleared). Output:
    "Goal proven by cvc5. Trusting SMT solver result."
- **Notable design points**:
  - **Deviation from PLAN.md confirmed**: `PM` is `NonDetT (StateT _
    DivM)` (NonDetT outermost), not the other way around. Cashmere's
    shape, every shipping Loom example uses it, the loop VC generator
    assumes it.
  - **`abbrev` over `def` for `Sig`** — the `ProgramSig` bundle uses
    `abbrev` so projections like `Sig.E = Ev` reduce by definition;
    this lets `DecidableEq Sig.E` synthesise from `Ev`'s `deriving`.
  - **`#derive_lifted_wp` for `get`/`set`** — Loom's `Loom.Meta`
    command (NOT in CaseStudies) registers `loomSpec` lemmas that
    teach `wpgen` how to step through `liftM (get : StateT _ DivM _)`
    and `liftM (set _)`. Without these, `wpgen` stalls at the first
    state read/write inside a handler body. Hand-written examples
    issue these per program; Phase 2 will emit them automatically.
  - **`wpgen` + `WPGen.default` fallback** — handler bodies that
    contain non-`loomSpec`-tagged operations get a residual `WPGen
    (liftM …)` goal; the fallback `apply WPGen.default` closes them
    by reducing to raw `wp` reasoning, which the manual proof tail
    handles. PLean's own `pverify` tactic (Phase 3) will bake this
    pattern in.
  - **Universe gotcha**: `abbrev PM (α : Type) := NonDetT _ α` must
    NOT carry an explicit `: Type` codomain annotation. `NonDetT m α`
    lives in `Type _` (Lean infers); pinning it to `Type` causes a
    misleading `failed to synthesize Monad PM` error. Documented in
    `StackSpike.lean`'s preamble for next-time-you-hit-this.
  - **`noncomputable example` for the synth check** — the scoped
    `MAlgOrdered (NonDetT m) l` instance is `noncomputable`; the
    in-`Monad.lean` synth-assertion `example` that uses
    `inferInstance` therefore needs the `noncomputable` modifier.
- **Anticipated risks resolved**:
  - **R1 (MAlgOrdered composition)** ✓ — Cashmere's identical stack
    + the StackSpike `#synth` lines confirm composition. Downgraded.
  - **R3 (SMT round-trip)** ✓ — `SmtRoundtrip.lean` runs cvc5 from a
    PLean source dir cleanly. The lakefile's `loomBuildDir` solver
    download wires up correctly with `currentDirectory!`.
- **Open follow-ups for Phase 2**:
  - Repoint `Surface/Stmt.lean` macros from `PLean.Stub.send` etc.
    onto `PLean.send` / `PLean.goto` etc. Reconciles `Stub.send`'s
    polymorphic-target signature with the real `MachineRef` target.
  - `#gen_module` must synthesise the per-program `Ev`/`GotoP`/`St`/
    `Fields` unions from the registry (matching the
    `MachineAdt`/`EventAdt` shape PVerifier emits) and issue
    `#derive_lifted_wp` for the concrete `(GlobalState P)`-typed
    `get`/`set` so users don't write them by hand.
  - Add `≺` notation in `Surface/Notation.lean` (`a ≺ b :=
    a.actionCount < b.actionCount`).
  - Re-express `Examples/PingPong/*.lean` and `Tests/Bootstrap/*` to
    use the real PM under the hood (currently they still elaborate
    onto `PLean.Stub`; `Stub.lean` is retired in Phase 2 once the
    macros repoint).

### Phase 0 — Refinement: curly braces + deferred elaboration · 2026-05-29
- **What changed**:
  - Surface restructured to use curly-brace blocks. `machine M { ... }`,
    `state S { ... }`, and `entry { ... }` / `on ev (p : T) { ... }`
    now nest cleanly via `declare_syntax_cat`. The explicit
    `#endmachine` / `#endstate` markers are gone.
  - Cross-machine references (`var server : Server` inside `Client`)
    work via two-phase elaboration. `pmodule` declarations *register*
    metadata only; `#gen_module M` is the user-visible finalisation
    command that emits Lean defs in dependency order:
      machine type-aliases → types/enums → events → machine bodies →
      verification declarations.
  - `LocalPModuleCtx` gained `typeOrder`/`eventOrder`/`machineOrder`
    arrays that preserve registration order. NameMap iteration is
    alphabetical, which silently breaks named-tuples that reference
    earlier-declared enums (e.g., `type Msg = (… : Phase)` after
    `enum Phase {…}`). The order arrays fix that.
  - Machine handlers now take `this : MachineRef` as an *explicit*
    first parameter. We tried using a `variable (this : MachineRef)`
    section binder but it doesn't reliably flow through
    `elabCommand`-driven nesting. The explicit binder is built with
    `mkIdent \`this` (unhygienic) so the name matches the user's
    `this` references.
  - `var name : Type` inside a machine emits `variable (name : Type)`
    at materialisation time. Lean's auto-include picks up the var
    when the handler body references it, threading it through as a
    closure-captured parameter (e.g., `Server.Idle.ePing_handler :
    Client → MachineRef → PingPayload → PM Unit`).
  - Naming collisions resolved: `axiom`/`instance` keep their
    `p`-prefixes (`paxiom` / `pinstance`); `pure` became `function`
    to avoid colliding with Lean's builtin `pure ()` term; `init`
    became `init-holds` to avoid Lean's `(init := ...)` named-arg.
  - Stub `Stub.send` made polymorphic in target type so
    `var server : Server` (= `MachineRef`) works as a target.
  - Event payload abbrev renamed `<ev>.payload` → `<ev>_payload` to
    avoid name-collision with field-accessor desugaring on event-tag
    defs.
  - PingPong demo rewritten to follow the Tutorial pattern: each side
    holds a `var` reference to the other, populated via a typed
    `entry (input : ...)` payload, sends use the local var.
  - Machine `receives` / `sends` are *derived*, not user-specified:
    `receives` = union of events handled across states; `sends` =
    events named in `send` statements (collected by walking the body
    Syntax at registration time). `#print_pmodule` shows the real sets
    (e.g., `machine Server receives [ePing] sends [ePong]`).
- **Surface keyword summary** (post-Phase 0):
  - From P verbatim: `event`, `eventset`, `enum`, `type`, `machine`,
    `spec`, `state`, `entry`, `on`, `goto`, `var`, `send`, `raise`,
    `announce`, `invariant`.
  - `p`-prefixed (Lean keyword collision):
    `pmodule`, `paxiom`, `pinstance`.
  - Renamed (Lean keyword collision):
    `pure` → `function`, `init` → `init-holds`.
- **Follow-ups for Phase 1** (extends the prior list):
  - `#gen_module` will need to also emit MachineAdt/EventAdt unions
    once the real `GlobalState` materialises.
  - The `pAssign` no-op stub references both sides via `let _ := …`
    only to keep auto-include happy; Phase 1 replaces with real
    state-record updates.

<!-- Template — copy when finishing a task:
### <Phase N — short task name>  · YYYY-MM-DD
- **PR / commit**:
- **What landed**: one sentence
- **Follow-ups**: link to any spawned tickets
-->

---

## Blockers / Open Questions

_None yet._

<!-- Template:
### <short title>
- **Raised**: YYYY-MM-DD by <owner>
- **Phase**:
- **Question / blocker**:
- **Decision needed by**:
- **Resolution**:
-->

### Anticipated (from PLAN.md "Risks")
These aren't blockers yet, just things to watch:

1. **MAlgOrdered composition** — *downgraded 2026-06-01.* No longer a "may
   need a bespoke instance" risk: Cashmere's `NonDetT (ExceptT … (StateT …))`
   stack proves Loom's layer instances compose by typeclass resolution. The
   stack is now `NonDetT (StateT GlobalState DivM)` (NonDetT outermost — see
   Decision Log). Residual risk is only (a) confirming the layer order and
   (b) the `scoped` instances needing an `open PartialCorrectness
   DemonicChoice`. Both resolved in PLAN_P1 Task 1 (~½ day). NEW sibling risk:
   `loom_solve` is CaseStudies-only — PLean discharges with raw `wpgen`+`grind`
   in Phase 1 and builds its own tactic in Phase 3.
2. **Helper-function unfolding** — handlers don't call handlers (passive
   dispatch), but handler bodies call helper `fun` decls. PVerifier inlines
   them; PLean should too in v1 (`pverify` unfolds before `loom_solve`).
   Compositional helper specs only matter once we add foreign `fun` with
   `requires`/`ensures` (Phase 5).
3. **Map/seq SMT encoding parity** with PVerifier's `[K]Option V` encoding —
   verify before proofs start drifting from PVerifier.
4. **Foreign types / uninterpreted symbols** — confirm Loom accepts them in
   goals (small spike in Phase 1 or Phase 5).
5. **SMT scaling** for large protocols (Paxos, Raft) — out of scope for v1.
6. **Temporal precedence operator `≺`** — PLean introduces a single binary
   primitive `a ≺ b` ("event `a` was sent before event `b`"). PVerifier
   cannot express this. Recommended encoding: reuse PVerifier's existing
   per-label `actionCount` as a logical timestamp, defining
   `a ≺ b := a.actionCount < b.actionCount`. Decision lands in Phase 1
   (so `GlobalState` shape is final), notation lands in Phase 2, no
   verification-pipeline changes needed. Full LTL (`prev`, `since`,
   `eventually`, `always`), liveness, and refinement remain out of scope
   for v1. Detailed plan in
   [`PLAN.md` → "Open Design Problems"](PLAN.md#open-design-problems).

---

## Decision Log

Material design decisions made after [`PLAN.md`](PLAN.md) was written. Append,
don't rewrite history.

### 2026-06-05 — Phase 2 close-out: `is` semantics, `loom_solve` triage
- **Context**: Two design clarifications made during the M2 sign-off
  pass. Both are referenced from PLAN_P3 (D20, D22, R20) but worth
  capturing in the decision log because they were live design
  questions during Phase 2 finalisation.
- **Decisions**:
  - **`is` is a ctor-tag check, not full equality**. The Phase-2
    initial implementation defined `Label.isEvent? lbl e := lbl.action
    = .event e` and a `notation:50 lbl " is " ev`. That checks both
    *tag* and *payload* — wrong vs. P's surface semantics, which
    asks "is this label tagged with `<ev>`, ignoring payload". The
    fix: `is` becomes a term-level **macro** (`Surface/Notation.lean`)
    that rewrites `lbl is <evIdent>` to `is_<evIdent> lbl`, where
    `is_<ev> : Sig.Label → Prop` is emitted per event by
    `Commands/GenModule.lean` ([`emitIsPredicates`](../PLean/Commands/GenModule.lean#L226)). The predicate
    pattern-matches on the action's constructor only, payload
    underscored. The old `Label.isEvent?` is removed from
    [`Semantics/Predicates.lean`](../PLean/Semantics/Predicates.lean).

    Phase 3 extends `is` to also handle the `m is <MachineKind>`
    form (D20 in PLAN_P3) — same macro, dispatches on whether the
    RHS is a registered event or machine name.

  - **`loom_solve` (CaseStudies) doesn't fit PLean's emission**. We
    confirmed empirically at end of Phase 2: importing
    `CaseStudies.Tactic` and calling `loom_solve` on M2's triples
    fails with `Failed to parse an assertion without names: WPGen
    (liftM get)`. The tactic queries `loomAssertionsMap` for
    user-named assertions registered via Cashmere's `bdef` /
    `prove_correct` macros. PLean's `#gen_module` doesn't produce
    those `WithName` wrappers, so the very first thing `loom_solve`
    does (`getAssertionInfo`) errors out.

    This is exactly the failure mode PLAN_P1 D3 anticipated: PLean
    discharges with raw `wpgen` + `grind` in Phase 1 and builds its
    own `loom_solve`-equivalent in Phase 3 (PLAN_P3 D22). The
    building blocks (`triple`, `wpgen`, `loom_intro`, `loom_smt`,
    `WPGen`/`loomSpec`) are in the core `Loom` lib and *are*
    reachable. PLean's lakefile keeps `CaseStudies` out of the
    require chain.

- **Consequences**: M2 verifies via the manual `wpgen + grind`-style
  tail (decision D3); the Phase-3 `pverify` tactic owns the recompose
  task. PLAN_P3 D22 lays out the pipeline explicitly.

### 2026-06-05 — Phase 2 implementation refinements
- **Context**: Several decisions during the Phase-2 implementation
  pass refine PLAN_P2's design. Captured here so PLAN_P2 stays
  declarative.
- **Decisions**:
  - **D8/D14 emission location**: All per-pmodule synthesis lives in
    `Commands/GenModule.lean` (extracted from `Surface/Machine.lean`
    as PLAN_P2 anticipated). Order is fixed: machine wrappers (D10)
    → types/enums → events (`<ev>_payload` abbrevs) → `Sig` unions
    → `#derive_lifted_wp` (D14) → per-machine var accessors +
    state-tag aliases → handler defs.
  - **D12 var read scheme**: Each handler def is prefixed with `let
    v ← v_get this.ref` for every machine var, so the user's body
    sees `v` as a Lean local. Assignments compile to
    `v_set this.ref <expr>; let v ← v_get this.ref` — the explicit
    rebinding ensures intra-handler reads-after-writes return the
    written value. Simpler than the "compile every read site to a
    `← get`" the plan sketched, and avoids walking the user-written
    body Syntax.
  - **`markReceived` deferred**: Surface emission does NOT call
    `markReceived` at handler entry. M1 hand-writes it; the Phase-2
    M2 demo therefore does *not* match M1's trace exactly. Phase 3's
    obligation generator will introduce a uniform handler prologue
    that calls `markReceived` (or whatever bookkeeping the spec
    requires). M2's proofs are slimmer than M1's by exactly that
    amount.
  - **Macro hygiene through `mkIdent`**: bare identifiers inside
    `\`(...)` quotations pick up hygiene marks during macro
    expansion. Identifiers that need to resolve against
    user-namespace constants emitted by `#gen_module` (`Sig`, `E`,
    `G`, `this`, `<S>_st`, ...) are constructed via `mkIdent` so
    they remain unhygienic. Documented in file headers of
    `Surface/Stmt.lean` and `Commands/GenModule.lean`.
  - **`Inhabited E` and `DecidableEq E` derivation**: deriving
    `DecidableEq` on `<Mod>.E` recursively requires
    `DecidableEq` on each event's payload type. Phase 2 adds
    `deriving DecidableEq` to named-tuple struct emission in
    `Surface/Types.lean`. `Inhabited E` is provided via an explicit
    `instance ⟨E.<firstEv> default⟩` rather than `deriving
    Inhabited` (Lean's default-derive picks the first ctor and
    requires it Inhabited; a payload-bearing first ctor needs the
    payload Inhabited too — provided by the auto-derive on the
    structure).
  - **`open PartialCorrectness DemonicChoice in #derive_lifted_wp`**:
    wrapped per-call rather than at file scope. Avoids forcing every
    importer of a generated pmodule into partial-correctness mode.
- **Alternatives considered**: (a) walk the saved body Syntax to
  rewrite `<v>` references into `← v_get` calls — rejected as too
  invasive and brittle to user expressions like `req.id` (would need
  to distinguish var refs from arbitrary identifiers). (b) Bake
  `open PartialCorrectness DemonicChoice` into `Commands/GenModule.lean`
  — rejected: forces a global verification-mode choice on every
  pmodule importer.
- **Consequences**: The macro-emission code in `Commands/GenModule.lean`
  is more complex than `Surface/Machine.lean`'s old version, but the
  surface user experience is identical. The M2 test (`Phase2PingPong.lean`)
  is shorter than M1 (`HandPingPong.lean`) because two of the
  M1-shape handlers reduce to trivial `pure ()` bodies under
  Phase-2 surface emission; the substantive `ePing_handler` triple
  remains and exercises the full `wpgen` chain.

### 2026-06-01 — Phase 1 planning: three deviations from PLAN.md
- **Context**: Writing [`PLAN_P1.md`](PLAN_P1.md) meant reading the *pinned*
  Loom revision (`d10340821daf…`, the rev in [`lakefile.lean`](../lakefile.lean))
  as it ships in PLean's build tree. Three of PLAN.md's Phase-1 assumptions
  don't hold against that revision. Captured here so PLAN.md can stay
  as-written and PLAN_P1 is the authority for Phase 1.
- **Decisions**:
  - **Reference DSL is Cashmere, not Velvet.** Our Loom pin's HEAD commit is
    titled "Remove velvet (#43)" — Velvet is deleted. PLAN.md cites Velvet
    ~5× as the shallow-embedding precedent (`velvetObligations`, "the trick
    velvet uses"). The surviving template is `CaseStudies/Cashmere/`. (The
    `velvetObligations`/`VelvetObligation` env-extension scaffold still exists,
    but under `CaseStudies/Extension.lean`.)
  - **Monad stack: `PM := NonDetT (StateT GlobalState DivM)`** — NonDetT
    *outermost*, reversing PLAN.md's `StateT GlobalState (NonDetT DivM)`.
    Cashmere uses `NonDetT (ExceptT String (StateT Bal DivM))` and its comment
    states the `MAlgOrdered` instance is derived automatically; Loom's loop VC
    generator and `MonadLift` direction both assume NonDetT on top. PLean drops
    Cashmere's `ExceptT` layer (no exception effect in safety verification).
    This *downgrades* Anticipated risk #1 from "spike before committing / may
    need a bespoke instance" to "confirm in the first ½ day" — the composition
    is known to work; only the layer order and the `scoped` instances need
    confirming.
  - **`loom_solve` is CaseStudies-only — PLean owns its proof tactic.** PLean's
    lakefile requires the Loom *package* but only globs the `Loom` lean_lib;
    `loom_solve`/`loom_solver`/`bdef`/`prove_correct` all live in the
    `CaseStudies` lib, which we don't require. The building blocks (`triple`,
    `wpgen`, `loom_intro`, `loom_smt`, `WPGen`/`loomSpec`) *are* in `Loom`.
    Phase 1 discharges M1 with raw `wpgen` + `grind` (the default
    `loom.solver`, which needs no external SMT binary). PLean's own
    `loom_solve`-equivalent — modeled on `CaseStudies/Tactic.lean` — lands in
    `Verify/Tactic.lean` in Phase 3, as already scheduled.
  - **v1 verification mode: partial-correctness + demonic choice.** Loom's
    `MAlgOrdered DivM Prop` and `MAlgOrdered (NonDetT m) l` instances are
    `scoped` in `PartialCorrectness.DemonicChoice`; they're invisible until
    opened. The semantics facade (`Semantics/Monad.lean`) bakes the `open` in.
    Demonic = safety holds for every nondeterministic schedule.
- **Alternatives considered**: (a) Add `CaseStudies` as a dep target and
  `import CaseStudies.Tactic` wholesale — rejected: example-grade code, pulls
  ProofWidgets, not a stable API. (b) Keep PLAN.md's stack order — rejected:
  no shipping Loom example uses NonDetT-innermost.
- **Consequences**: M1's exit criterion is "four handler triples discharge via
  `wpgen`+`grind`," not "via `loom_solve`." M1 likely needs no SMT solver at
  all (grind default). The stub `PM := Id` macro path is untouched in Phase 1;
  repointing it onto the real `PM` is Phase-2 surface work.

### 2026-05-29 — Phase 0 implementation refinements
- **Context**: Several PLAN_P0 design decisions were adjusted during
  Phase 0 implementation. Captured here so PLAN_P0 can stay
  declarative.
- **Decisions**:
  - `axiom` and `instance` keywords renamed to `paxiom` and `pinstance`.
    Lean's builtin `axiom`/`instance` commands intercepted the
    un-prefixed forms before our gating logic ran. The `p` prefix
    matches `pmodule`'s reasoning.
  - P's optional `receives [e1, ...]` / `sends [e1, ...]` clauses on
    `machine` headers are not parsed in Phase 0. `#pwf` will derive
    actual receive/send sets from handler bodies and `send` calls. The
    clauses can be re-added as access-control hints later without
    changing the registry shape.
  - P's `do` token in `on ev do (param : T) { … }` is dropped: Lean's
    tokenizer eagerly consumes `do` as a do-block opener. PLean
    surface is `on ev (param : T) { … }`.
  - Foreign sort encoding uses `opaque … : NonemptyType` + projection
    + `Nonempty` instance (mathlib's standard pattern). Phase 1 will
    confirm Loom accepts it for SMT.
  - Handler defs are named `<machine>.<state>.<event>_handler`; the
    `_handler` suffix avoids shadowing the event-tag def of the same
    name when the handler body references the event in `send`.
- **Consequences**: Surface keyword list is now
  `pmodule`/`paxiom`/`pinstance` (prefixed) + the rest verbatim from P.

### 2026-05-28 — Phase 0 expanded; multi-file `pmodule` aggregation
- **Context**: Original Phase 0 was a 4-bullet "lake bootstrap" stub. User
  asked for a real bootstrap that lets P programs be authored in Lean across
  multiple files — modeled on Veil's `veil module ... end` pattern.
- **Decision**: Added a new module construct `pmodule M ... end M` that may
  appear in many files; fragments accumulate via a `SimplePersistentEnvExtension`
  keyed on module name. Lean `import` carries fragments across files for free.
  Detailed plan in [`PLAN_P0.md`](PLAN_P0.md).
- **Alternatives considered**: Veil's `includes` (parametric module
  composition with explicit aliasing). Rejected for v1 because Lean's `import`
  already gives us the cross-file behaviour we need; `includes` is for
  *parametric* module reuse and we don't need that yet.
- **Consequences**: Phase 0 grows from ~2 days to ~1 week. Adds a stub `PM`
  (`PLean.Stub.PM := Id`) so machine bodies have something to elaborate into
  before Phase 1 lands the real monad. Two top-level commands: `#pwf M`
  (well-formedness only; lives forever as a fast subset check) and
  `#pverify M` (end-to-end; in Phase 0 delegates to `#pwf`, in Phase 3+
  also generates Hoare-triple obligations and dispatches them to
  `loom_solve`). Surface keywords match P's grammar (`event`, `machine`,
  `invariant`, `axiom`, `init`, `pure`, `enum`, `type`, `eventset`,
  `spec`); `pmodule` keeps its `p` prefix to avoid collision with P's
  reserved `module` keyword. Uninterpreted sorts (`type N` with no body)
  and axioms over them are first-class — same pattern as Veil's
  `type node` / `assumption`. Axiom *bundles* are supported via
  `instance nm : Class T`, which elaborates to `variable [nm : Class T]`
  (Veil's `instantiate` pattern, renamed for natural reading). Works
  uniformly over any `T` — uninterpreted sort, alias, enum, primitive,
  built-in (e.g., `MachineRef`).

<!-- Template:
### YYYY-MM-DD — <decision title>
- **Context**:
- **Decision**:
- **Alternatives considered**:
- **Consequences**:
-->

---

## Milestones

- [x] **M0 — Multi-file `pmodule` aggregates and `#pwf` reports clean**
      (end of Phase 0; reached 2026-05-29). Four-file PingPong demo
      (Events / Server / Client / Top) plus a single-file test exercising
      `pinstance` over an uninterpreted sort and a built-in type.
      `#pverify` is a thin wrapper over `#pwf`.
- [x] **M1 — Hand-written ping-pong verifies** (end of Phase 1; reached
      2026-06-04). Four-handler ping-pong over the real `PM := NonDetT
      (StateT (GlobalState Sig) DivM)` proves the temporal invariant
      "every ePong is preceded by some ePing" using `≺` (the headline
      PLean feature PVerifier cannot express). Discharged via `wpgen` +
      raw Loom primitives (decision D3); SMT round-trip confirmed
      separately (R3 cleared). See
      [`Tests/Semantics/HandPingPong.lean`](../Tests/Semantics/HandPingPong.lean).
- [x] **M2 — Surface-syntax ping-pong verifies** (end of Phase 2;
      reached 2026-06-05). Two surface-emitted handler triples
      (`Server.Idle.ePing_correct`, `Client.Booting.ePong_correct`)
      discharge in `Tests/Surface/Phase2PingPong.lean` via `wpgen` +
      the same manual proof tail used in M1. The
      `#gen_module`-synthesised `<Mod>.Sig`, `<Mod>.PM'`, and
      `#derive_lifted_wp` lemmas reach the real `PM`; surface macros
      (`send`, `goto`, `var =`) target real primitives (D11/D12/D13);
      machine wrappers are distinct types (D10); `≺` notation lives
      in `Surface/Notation.lean` (D16); `Internal/Stub.lean` is
      deleted (D15).
- [ ] **M3 — `#pverify` synthesizes Hoare-triple obligations from registry
      and dispatches them to `loom_solve`** (end of Phase 3). `#pwf` remains
      available as the fast subset check.
- [ ] **M4 — Tutorial/1_ClientServer ports + verifies in PLean** (Phase 6).
- [ ] **M5 — Tutorial/2_TwoPhaseCommit ports + verifies; v1 cut**.

---

_Last updated: 2026-06-05 (Phase 2 closed; M2 reached.
[`PLAN_P3.md`](PLAN_P3.md) drafted with decisions D18–D28 and the
M3 acceptance set targeting three Tutorial/Advanced benchmarks
(6_DistributedLock, 8_LockServer, 3_RingLeaderVerification). Phase 3
ahead: `Lemma`/`Theorem`/`Proof` blocks, the `pverify` tactic with
focused `default_inv` automation (D28), per-handler obligation
synthesis, and `markReceived`-injecting handler wrappers.)_

## Document Index
- [`PLAN.md`](PLAN.md) — overall implementation plan (all phases). NOTE: its
  Phase-1 monad order and `loom_solve` exit criterion are superseded by
  PLAN_P1's Decisions D1/D3.
- [`PLAN_P0.md`](PLAN_P0.md) — detailed Phase 0 (Bootstrap) plan
- [`PLAN_P1.md`](PLAN_P1.md) — detailed Phase 1 (Semantic core) plan
- [`PLAN_P2.md`](PLAN_P2.md) — detailed Phase 2 (Registry + minimal surface)
  plan — repoints the Phase-0 macro path onto Phase-1's real `PM`,
  retires `Stub.lean`, target M2
- [`PLAN_P3.md`](PLAN_P3.md) — detailed Phase 3 (Verification declarations)
  plan — `Lemma`/`Theorem`/`Proof` blocks, `pverify` tactic, obligation
  generator, target M3 via three Tutorial/Advanced benchmarks
- `STATUS.md` (this file) — living tracker
