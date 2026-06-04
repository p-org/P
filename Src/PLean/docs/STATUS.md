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
| 2 — Registry + minimal surface   | ☐ | — | — | — | |
| 3 — Verification declarations    | ☐ | — | — | — | |
| 4 — Spec machines                | ☐ | — | — | — | |
| 5 — Remaining surface            | ☐ | — | — | — | |
| 6 — Tutorial port                | ☐ | — | — | — | |
| 7 — Stretch / future             | ⊘ | — | — | — | post-v1 |

---

## Active Work

_Phase 1 complete. Phase 2 (Registry + minimal surface) is next: repoint
the `Surface/Stmt.lean` macros from `PLean.Stub` onto the real
`PLean.send`/`goto`/etc., have `#gen_module` synthesise per-program
`EventOrGoto`/`MachineState` unions and emit `#derive_lifted_wp` for
the `get`/`set` operations on the concrete `GlobalState P`._

**Phase 2 entry point** — re-express the Phase-1 hand-written ping-pong
([`HandPingPong.lean`](../Tests/Semantics/HandPingPong.lean)) in PLean
surface syntax and verify M2 (the surface-syntax variant verifies via
the same triple shapes M1 proved). The hand-written file is the
elaboration target the macros must produce.

**Current surface keyword truth** (authoritative; the per-file syntax
decls are the source):
- From P verbatim: `event`, `eventset`, `enum`, `type`, `machine`,
  `spec`, `state`, `entry`, `on`, `goto`, `var`, `send`, `raise`,
  `announce`, `invariant`.
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

### Phase 0 — Bootstrap · 2026-05-29
- **What landed**:
  - Lake project skeleton (`lakefile.lean`, `lean-toolchain` v4.24.0,
    `dependencies.toml`, Loom pinned to velvet's revision).
  - Internal: `Stub.lean` (PM := Id stub), `Decls.lean` (metadata
    records — no deep AST), `Registry.lean` (two-tier env extension:
    scoped `localPModuleCtx` + persistent `pmoduleExt` with field-wise
    fragment merging on `addImportedFn`).
  - Surface: `Module.lean` (`pmodule M`), `Types.lean`
    (foreign sort via `opaque … : NonemptyType` + projection,
    abbrev alias, named-tuple via `structure`, enum via `inductive`),
    `Events.lean` (event tag + payload abbrev), `Machine.lean`
    (`machine`/`spec`/`state`/`entry`/`on _ (param : T) {…}`/
    `on _ goto _`, with `_handler` suffix to avoid event-name shadowing),
    `Stmt.lean` (named-tuple send `(f = v, …)` ascribed via
    `<ev>.payload`, plus raw-term and no-payload forms), `Verify.lean`
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
  - `Semantics/Label.lean` — `Label`, `EventOrGoto`, `MachineState`,
    plus `payloadOf`/`actionCount` accessors. Generic in `(E, G, S, F)`
    so the same record shape compiles once for any program; concrete
    unions are user-supplied (Phase-1 hand-written) or synthesised
    (Phase 2 `#gen_module`).
  - `Semantics/GlobalState.lean` — `ProgramSig` bundle of program
    union types, `GlobalState P` record (`sent`/`received`/`machines`/
    `actionCount`), pure update helpers (`addSent`, `addReceived`,
    `bumpActionCount`, `updateMachine`), `Inhabited` instance.
  - `Semantics/Monad.lean` — `PM P α := NonDetT (StateT (GlobalState P)
    DivM) α`, `PProp P := GlobalState P → Prop`, plus a compile-time
    sanity check (`MAlgOrdered (PM TrivialSig) (PProp TrivialSig)`)
    confirming the layer order resolves once `PartialCorrectness
    DemonicChoice` is open.
  - `Semantics/Primitives.lean` — `send` / `raise` / `goto` /
    `announce` / `newMachine` / `markReceived` as `PM` combinators
    that update `GlobalState` exactly per `Uclid5CodeGenerator.cs:
    1967-1999`.
  - `Semantics/Predicates.lean` — `inflight` / `sent` / `received` /
    `Label.isEvent?` (= P's `is`) / `Label.targets?` / `stateOf` /
    `precedes` (= P's `≺`, defined as `actionCount` comparison per
    decision D6).
  - `Semantics/Default.lean` — the three sanity invariants from
    `Uclid5CodeGenerator.cs:1189-1201`: `UniqueActions`,
    `IncreasingCount`, `ReceivedSubsetSent`, plus a `DefaultInvariants`
    bundle.
  - `Tests/Semantics/StackSpike.lean` — Task 1: confirms every
    `MAlgOrdered`/`Monad`/`LawfulMonad` instance synthesises;
    discharges two trivial triples via `wpgen`. Locks D1.
  - `Tests/Semantics/HandPingPong.lean` — **M1**. A 2-machine
    (Server/Client), 4-handler ping-pong written directly as Lean
    defs over the real `PM`; proves the temporal user invariant
    "every `ePong` is preceded by some `ePing`" via the `≺` operator
    (the headline feature PVerifier cannot express). All four handler
    Hoare triples discharge end-to-end.
  - `Tests/Semantics/Combinators.lean` — `.run`-based regression for
    primitives. `decide`-evaluates buffer/counter deltas to match
    expectations (Cashmere's `#eval (prog).run.run.run init` pattern).
  - `Tests/Semantics/SmtRoundtrip.lean` — Task 10: confirms `loom_smt`
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
- [ ] **M2 — Surface-syntax ping-pong verifies** (end of Phase 2).
- [ ] **M3 — `#pverify` synthesizes Hoare-triple obligations from registry
      and dispatches them to `loom_solve`** (end of Phase 3). `#pwf` remains
      available as the fast subset check.
- [ ] **M4 — Tutorial/1_ClientServer ports + verifies in PLean** (Phase 6).
- [ ] **M5 — Tutorial/2_TwoPhaseCommit ports + verifies; v1 cut**.

---

_Last updated: 2026-06-04 (Phase 1 closed; M1 reached. Real `PM` over
Loom + `≺`-based temporal invariant proved end-to-end on hand-written
ping-pong. SMT round-trip via cvc5 confirmed.)_

## Document Index
- [`PLAN.md`](PLAN.md) — overall implementation plan (all phases). NOTE: its
  Phase-1 monad order and `loom_solve` exit criterion are superseded by
  PLAN_P1's Decisions D1/D3.
- [`PLAN_P0.md`](PLAN_P0.md) — detailed Phase 0 (Bootstrap) plan
- [`PLAN_P1.md`](PLAN_P1.md) — detailed Phase 1 (Semantic core) plan
- `STATUS.md` (this file) — living tracker
