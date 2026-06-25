# PLean — Roadmap

The authoritative
state lives in [`STATUS.md`](STATUS.md) (running tracker, decision log,
milestones) and [`PLAN.md`](PLAN.md) / [`PLAN_P{0..4}.md`](.) (per-phase
designs); this file is the index that turns those into pickable chunks
of work.

> **Update cadence.** Refresh after each phase flips, after each major
> work item lands, and at the start of any sprint planning session.
> If this doc and [`STATUS.md`](STATUS.md) disagree, [`STATUS.md`](STATUS.md) wins.

---

## At a glance — 2026-06-23

| Phase | Status | What it delivers |
|---|---|---|
| 0 — Bootstrap | ☑ done | Lake skeleton, registry, multi-file `pmodule M`, `#pwf`, `#pverify` shell |
| 1 — Semantic core | ☑ done | `PM := NonDetT (StateT (GlobalState Sig) DivM)`, primitives, default invariants, **M1** |
| 2 — Registry + surface | ☑ done | `#gen_module M`, surface macros target real PM, **M2** |
| 3 — Verification declarations | ☑ done | `Lemma`/`Theorem`/`Proof`/`system <s> { … }`, SMT-discharge `#pverify`, `@[pverifyProof]` registry, `paxiom`/`pinstance` axiomatic-fact bridge. **M3 reached** — `Examples/DistributedLock` **12/12**, `Examples/LockServer` **37/37**, `Examples/RingLeader` **14/14** |
| 4 — Spec machines | ☐ next | `spec X observes [...] { ... }` flattening, `assert` obligations, send-time spec dispatch — **M4 acceptance: ChainReplication** |
| 5 — Remaining surface | ◐ partial | `map[K,V]` / `set[T]` / `seq[T]` / `option[T]` shipped 2026-06-25; `foreach` / `while` (with `invariant` / `done_with` / `decreasing`) shipped 2026-06-26; remaining: `assume`, loop-aware `default_inv`, `WPGen.if` for `DivM`-backed `PM` |
| 6 — Tutorial port | ☐ not started | **M5: `Tutorial/1_ClientServer`**, **M6: `Tutorial/2_TwoPhaseCommit`** verify under PLean |
| 7 — Stretch | ⊘ deferred | PChecker bridge, lean-smt evaluation, counter-example surfacing |

Build state: 3414 jobs green at HEAD.

---

## Project overview

PLean is a port of the P language and its PVerifier verification
backend into Lean 4, using Loom as the proof backend. P programs —
state-machine-based models of distributed protocols — are authored in
a P-like surface embedded in Lean; PLean elaborates each program into
per-handler Hoare-triple obligations and discharges them via SMT or,
where SMT cannot close a goal, via a manual-proof registry.

The v1 deliverable is end-to-end verification of the canonical P
tutorial protocols (`1_ClientServer`, `2_TwoPhaseCommit`) under PLean
with no hand-written proof scaffolding required from the user.

### Current state

Phases 0–2 are complete; Phase 3 is in progress. The verification
pipeline is functional end-to-end: P programs parse, register, and
materialise into Lean definitions; `#pverify M` walks the registry,
synthesises one obligation per `(machine, state, event)` triple,
consults the `@[pverifyProof]` attribute for user-supplied proofs,
and discharges the remainder via the `pverify_*` tactic library
backed by `loom_smt`. The 2026-06-10 soundness fix (the `system <s>
{ … }` binder block) ensures invariants are materialised as
state-parameterised predicates, eliminating a class of unsound
"verifications" of trivially false properties.

### Outstanding work

Four work areas remain on the path to v1.

1. **Phase 3 closure (M3).** The verification infrastructure is in
   place but the three M3 acceptance benchmarks — `6_DistributedLock`,
   `8_LockServer`, `3_RingLeaderVerification` — are not all green.
   DistributedLock and LockServer port some, but not all, of the
   inductive invariants from the original P sources, and the third
   benchmark is not yet ported. Closing M3 requires either
   reproducing the missing invariants or supplying manual proofs via
   `@[pverifyProof]`. This is protocol-modelling work rather than
   framework work.

2. **Phase 4 — spec machines (M4).** P's `spec X observes [...] {
   ... }` construct introduces protocol-level observers that monitor
   `send` events and assert correctness conditions. Phase 4 extends
   `#gen_module` to materialise per-spec state and dispatch
   procedures, patches the `send` macro to fire spec handlers
   synchronously, and adds an obligation form for `assert` sites.
   The acceptance benchmark is `1_ChainReplicationVerification`, the
   sole Tutorial/Advanced benchmark with a `spec` block.

3. **Phase 5 — remaining surface.** Partially shipped. `map[K,V]` /
   `set[T]` / `seq[T]` / `option[T]` landed 2026-06-25 with
   PVerifier-parity SMT encoding (`K → Option V` for maps, hoisted
   `Containers` struct for the multi-ref pattern); `foreach` and
   `while` (with `invariant` / `done_with` / `decreasing` clauses)
   landed 2026-06-26 via a PLean-local `pforeach` primitive + Loom's
   `WPGen.forWithInvariantLoop`. Still pending: `assume <prop>;`, a
   loop-aware `default_inv` so the auto-default obligation under
   loops doesn't disprove on trivial invariants, and `WPGen.if` for
   `DivM`-backed `PM` (so `if` inside handler bodies steps cleanly).
   ChainReplication's spec body remains gated on Phase-4 spec
   machines; Consensus and Paxos still need loop-aware default
   invariants before their `prove default;` obligations close.

4. **Phase 6 — tutorial ports (M5, M6).** Once Phases 4 and 5 land,
   port `Tutorial/1_ClientServer` and `Tutorial/2_TwoPhaseCommit`
   into PLean. Phase 6 also includes a wall-clock comparison against
   PVerifier+UCLID5 on the same examples.

A PChecker bridge — emitting `.p` source from the registry so
PLean-authored programs can run under the existing model checker —
is tracked as a post-v1 stretch goal (Phase 7).

### Sequencing and effort

Phases 4 and 5 are mutually independent and can be developed in
parallel; Phase 6 depends on both. Phase 3 closure is independent of
Phases 4 and 5 and may proceed concurrently. Estimated effort to v1
is approximately 6–8 person-weeks, parallelisable across 2–3
developers; see [Suggested division of labor](#suggested-division-of-labor)
for a sample sprint allocation. Per-step sizing and file-level work
breakdown follow in the [workstream sections below](#remaining-work--by-workstream).

---

## Repository layout

```
Src/PLean/
  PLean/
    Internal/      Decls.lean, Registry.lean       — env-extension metadata
    Semantics/     Label / GlobalState / Monad / Primitives / Predicates / Default
    Surface/       Module / Types / Events / Machine / Stmt / Notation / Verify
    Commands/      GenModule, PWf, PVerify, PrintModule
    Verify/        Obligation, Tactic, ProofRegistry, SimpAttrs
  Examples/        PingPong demo
  Tests/           Bootstrap, Semantics, Surface
  docs/            PLAN.md, PLAN_P{0..4}.md, STATUS.md, REVIEW_P3.md, ROADMAP.md (this)
```

Phase ownership of files (for "who touches what"):

| Area | Active edits expected | Stable for now |
|---|---|---|
| `Semantics/*` | `GlobalState` widened to carry per-spec state when spec machines land | Otherwise stable since Phase 1 |
| `Syntax/*` | `Stmt.lean` extended so `send` fires observing specs; `Notation.lean` extended for registry-aware `is`. Container syntax (`set` / `map` / `seq` / `option`) lives in `Syntax/Containers.lean`; loop syntax (`foreach` / `while`) in `Syntax/Loop.lean` (both shipped). | The user-facing surface is stable apart from spec-machine wiring |
| `Commands/GenModule.lean` | Spec-state and dispatch emission; collection-type emission | Largest single file (~700 LOC) — coordinate edits |
| `Verify/*` | New `SpecObligation.lean` for `assert`-site obligations; targeted tactic polish (see workstreams below) | The atomic tactic library users call from `@[pverifyProof]` proofs is stable |

---

## What is in place today

The verbose history with decision rationale lives in
[`STATUS.md` § Done](STATUS.md#done) and the per-phase plans. The
short summary:

- **Surface language.** A P-faithful surface — `event`, `machine`,
  `state`, `invariant`, `Lemma` / `Theorem` / `Proof`, `≺` for
  temporal precedence — embedded in Lean. A small set of P keywords
  is renamed where they collide with Lean reserved words or builtins
  (e.g. `module` → `pmodule`, `pure` → `function`, `init` →
  `init-holds`); the full table lives in
  [`CLAUDE.md` § "Surface keywords vs. P keywords"](../CLAUDE.md).
  A `pmodule M` declaration may span multiple files; fragments
  aggregate automatically.

- **Materialisation.** `#gen_module M` walks the registry and emits
  the per-program union types (events, goto targets, states,
  per-machine field records), machine-wrapper structs, per-field
  accessors, per-event and per-machine `is_*` predicates, and the
  `InitConditions` precondition that flows into every obligation.

- **Verification command.** `#pverify M` synthesises one Hoare-triple
  obligation per `(machine, state, event)` triple. For each
  obligation it first consults the `@[pverifyProof]` attribute for a
  user-supplied manual proof and falls back to an automated tactic
  chain backed by `loom_smt`. Successes, manual proofs, and failures
  are reported with copy-paste skeletons for any remaining gaps.

- **Manual-proof escape hatch.** When SMT cannot discharge an
  obligation, the user writes a theorem tagged `@[pverifyProof]`
  using the `pverify_*` tactic library
  ([`Verify/Tactic.lean`](../PLean/Verify/Tactic.lean)).
  [`Tests/Surface/PVerifyManualProof.lean`](../Tests/Surface/PVerifyManualProof.lean)
  demonstrates the workflow end-to-end.

- **Soundness.** Invariants are bound to a global-state argument via
  the `system <s> { … }` block, so the verifier cannot accept a
  body that ignores state. The earlier closed-proposition
  materialisation (which silently let the verifier "prove" trivially
  false properties) is fixed and pinned by
  [`Tests/Surface/SoundnessRegression.lean`](../Tests/Surface/SoundnessRegression.lean).
  Current M3 closure rates: [`Examples/DistributedLock`](../Examples/DistributedLock.lean)
  **12/12**, [`Examples/LockServer`](../Examples/LockServer.lean) **37/37**,
  [`Examples/RingLeader`](../Examples/RingLeader.lean) **14/14**.

---

## Remaining work — by workstream

Workstreams are independently pickable. Each lists its goal,
file-level action items, exit criterion, blockers, and approximate
size (S ≈ ½ day, M ≈ 1–2 days, L ≈ 3–5 days). Workstreams marked
*parallel-safe* can proceed without coordinating with another
in-flight workstream.

### W1 — Distributed-lock benchmarks ☑ **done**

`Examples/DistributedLock` closes **12/12** (11 SMT + 1 manual) and
`Examples/LockServer` closes **37/37** (34 SMT + 3 manual). Routine
discharge for both. The 4 manual proofs across the two files are
send-handler bundles where SMT returns `unknown` as a single shot —
each proof splits the bundle and dispatches per-conjunct via the
helpers documented in [`AUTOMATION.md`](AUTOMATION.md) §3.

### W2 — Ring-leader benchmark ☑ **done**

`Examples/RingLeader` closes **14/14** (12 SMT + 2 manual). The two
manual proofs are the inductive steps through `goto Won`, where
lean-auto rejects `(_.machines _).currentState` reads under a `∀`
("Higher order input?") — each was discharged by a short proof
splitting on whether `x = this.ref` (for state-dependent invariants)
and whether `e` is the freshly-sent label (for routing invariants).
The relational facts about `le` / `btw` / `right` are stated as two
`pinstance` declarations (`LeOrder`, `RingTopology`); the obligation
generator's `have hax_<name>` injection brings every class-field
axiom into every VC's local context.

### W3 — Add spec machines to the language *(L, sequential within)*

**Goal.** Implement P's `spec X observes [...] { ... }` construct,
which introduces protocol-level observers that watch `send`
events, maintain their own state, and assert correctness conditions.
The acceptance benchmark is
[`Tutorial/Advanced/1_ChainReplicationVerification`](../../../Tutorial/Advanced/1_ChainReplicationVerification/PSrc/System.p),
the only benchmark in `Tutorial/Advanced/` with a `spec` block.
Detailed design is in [`PLAN_P4.md`](PLAN_P4.md); proceed in order.

**Action items.**

1. *Auto-emit default obligations.* Currently the user must write
   `prove default;` explicitly to obtain the per-handler
   default-invariant obligations. Extend the obligation generator
   ([`PLean/Verify/Obligation.lean`](../PLean/Verify/Obligation.lean))
   so a default obligation is emitted for every `(machine, state,
   event)` triple even when no `prove default;` directive is
   present, while remaining idempotent against an explicit one.
2. *Carry per-spec state in `GlobalState`.* Add a `Specs` type
   parameter to `ProgramSig` and a `specs : P.Specs` field to
   `GlobalState` ([`PLean/Semantics/GlobalState.lean`](../PLean/Semantics/GlobalState.lean)),
   defaulting `Specs` to `Unit` so existing programs build
   unchanged.
3. *Materialise spec bodies.* In
   [`PLean/Commands/GenModule.lean`](../PLean/Commands/GenModule.lean),
   emit a per-spec state-tag inductive, a per-spec field record,
   and per-field accessors operating on the new `specs` slice. Emit
   each spec handler as a `PM Sig Unit` definition over those
   accessors. For each `(spec, observed-event)` pair, emit a
   dispatch definition that case-splits on the spec's current state
   and forwards to the matching handler.
4. *Trigger spec dispatch from `send`.* Extend the `send` macro
   ([`PLean/Surface/Stmt.lean`](../PLean/Surface/Stmt.lean)) so a
   `send target, ev, payload` call additionally invokes every
   dispatch definition for specs that observe `ev`. The handler's
   weakest-precondition computation already runs over the resulting
   `do` block; no new tactic plumbing is required.
5. *Generate `assert` obligations.* Each `assert <prop>;` inside a
   spec handler becomes a per-handler triple: precondition is the
   spec's dispatcher contract plus the user invariants;
   postcondition is `<prop>` evaluated against the post-state.
   Implement in a new `PLean/Verify/SpecObligation.lean` modelled
   on the existing impl-obligation generator, and have `#pverify`
   call it after the impl pass.
6. *Extend the `is` notation for spec names.* `m is <Spec>` should
   reduce to a vacuously-false predicate — specs are singletons,
   not allocated machine instances. Required for invariants that
   universally quantify over all machines.
7. *Tests.* Add a small end-to-end test (`Phase4SpecPingPong.lean`)
   exercising the trivial-assert path; a structural port of
   ChainReplication (`Phase4ChainReplication.lean`); and a
   `#guard_msgs`-pinned test of the spec-obligation theorem
   signatures (`Phase4SpecObligationShape.lean`) to lock the
   emission shape against future refactors.

**Exit criterion.** A pmodule containing one impl machine and one
observing spec materialises, `#pverify` produces both impl and spec
obligations, and the trivial-assert test discharges all of them.
ChainReplication's surface parses and emits the expected obligation
counts; closing those obligations end-to-end is gated on W4.

**Blockers.** Steps 1 and 2 are independent prerequisites and can
be done first by anyone. Steps 3–6 are sequential. Step 7 rides
along with whichever of 3–6 it covers.

### W4 — Add collection types and remaining surface *(L, parallel-safe with W3)*

**Goal.** Support the language features real protocols depend on but
PLean does not yet handle — primarily collection types and richer
control flow. Without these, the spec port from W3 produces well-
formed but vacuous obligations (the spec body falls back to a
no-op stub at unsupported syntax).

**Action items.**

1. *`map[K,V]` and `set[T]` types.* **✓ shipped 2026-06-25.** Surface
   syntax in `Syntax/Containers.lean`, hoisted `Containers` struct in
   `Semantics/GlobalState.lean`, materialised via `#gen_module`'s
   `var`-classification pass. Maps encode as `K → Option V` (PVerifier
   parity); the multi-ref pattern (`∀ n1 n2, k ∈ n1.kv → k ∈ n2.kv →
   n1 = n2`) verifies cleanly because the projection
   `s.containers.<M>_<v> (n.ref, k)` reaches lean-auto as a flat
   applied symbol. Exercised by
   [`Examples/ShardedKV`](../Examples/ShardedKV.lean) — 11/11 SMT.
2. *Membership and indexing.* **✓ shipped 2026-06-25.** `if (k in kv)`
   and `kv[k]` read/write supported via the `pverifySimp` set's
   lookup-after-mutation lemmas (`mapInsert_eq`, `mapErase_ne`, …).
3. *Default values for collections.* **✓ shipped 2026-06-25.**
   `default(map[K, V])` reads as `fun _ => Option.none`, `default(set[T])`
   as Mathlib's empty set.
4. *`assume <prop>;`* — let users tighten the precondition of the
   surrounding handler obligation. Implement at the tactic level as
   the introduction of a hypothesis that the obligation also
   discharges. (Still pending; not blocking immediate benchmarks.)
5. *Conditionals and loops in handler bodies.* `while` and `foreach`
   **shipped 2026-06-26** via a PLean-local `pforeach` primitive +
   Loom's `WPGen.forWithInvariantLoop`; the rigid gadget-chain body
   `do invariantGadget …; onDoneGadget …; decreasingGadget …; if cond
   then body else break` matches `wpgen` automatically.
   `pverify_step_wp` carries the `Pi.inf_apply` / `inf_Prop_eq` simp
   set that reduces the post-`wpgen` lattice meet to a `Prop`-level
   conjunction SMT decides. Remaining: `if` *inside* loop bodies still
   falls through to `WPGen.default` (Loom has no `WPGen.if` for
   `DivM`-backed `PM`); and the auto-emitted `prove default;`
   obligation under loops disproves on trivial invariants without a
   loop-aware `default_inv`.

**Exit criterion.** `Phase4ChainReplication.lean`'s spec body
elaborates against a real (not stubbed) handler body, and
`#pverify` either discharges its `assert`s via SMT or reports
specific failed obligations the user can fill in manually.

**Blockers.** Step 1 is the foundation; subsequent steps build on
it. Independent of W3 — a developer can prototype the encoding
against a small synthetic test before the spec-machine wiring lands.

### W5 — Verifier polish *(M, parallel-safe)*

**Goal.** Close known rough edges in the verifier surface that are
not blockers but improve the user experience. Each item is small;
pick them up between heavier workstreams.

**Action items.**

1. *Make the `is` notation registry-aware.* Today `m is Server`
   relies on Lean's name-resolution to find `is_Server`; a typo
   produces a generic "unknown identifier" error. Look up the
   right-hand side in the local pmodule registry at expansion time
   and emit a bespoke error if it is neither a registered event
   nor a registered machine.
2. *Extract a handler wrapper that injects `markReceived`.* Today
   the obligation generator emits triples directly against the
   user-visible handler definition and carries the dispatcher
   contract via an existential precondition. A wrapper that takes
   `lbl` as an explicit parameter and prepends `markReceived` would
   make the precondition concrete and is closer to the runtime
   semantics. Land in a new `PLean/Verify/Wrapper.lean`.
3. *Strengthen the default-invariant tactic.* The current
   `default_inv` tactic is a guarded `simp only` plus arithmetic
   fallback; it works on simple handlers but does not implement
   the per-primitive case table (no-`send`, single-`send`,
   `markReceived` + `send`, `goto`) that handles the full range of
   well-formed handlers deterministically. Implement the case
   table in [`PLean/Verify/Tactic.lean`](../PLean/Verify/Tactic.lean).
4. *Add a configurable SMT fallback for the user-facing `pverify`
   tactic.* The chain currently terminates at `tauto`. Adding
   `loom_smt` as a final step (gated on the `loom.solver` option)
   closes goals that decide arithmetically but for which `tauto`
   gives up.
5. *User-facing tactic conveniences.* `pverify using L1, L2` to
   conjoin extra lemmas into the precondition; `pverify!` to report
   unsolved goals as errors; `pverify?` to print the explicit
   tactic script. Each is small and individually deferable.
6. *Sweep the small-distributed-protocol benchmarks once item 1
   lands* so they invariably write `m is Node` (the user-facing
   form) rather than `Node_allocated m` (the underlying predicate).

**Blockers.** Items 1 and 6 must land in that order; items 2–5 are
independent of each other.

### W6 — Port the canonical P tutorials *(L, sequential after W3 + W4)*

**Goal.** Demonstrate end-to-end PLean verification on the standard
P tutorials, the agreed v1 cut.

**Action items.**

1. Port [`Tutorial/1_ClientServer/`](../../../Tutorial/1_ClientServer/)
   into a new `Tests/Surface/Tutorial1_ClientServer.lean`. Run
   `#pverify`; close any residual obligations using the same
   approach as W1.
2. Port [`Tutorial/2_TwoPhaseCommit/`](../../../Tutorial/2_TwoPhaseCommit/)
   the same way.
3. Measure wall-clock verification time for both tutorials under
   PLean and under PVerifier+UCLID5 on the same hardware; record
   the comparison alongside the test files.

**Exit criterion.** Both tutorials verify under PLean with no
hand-written proof scaffolding, and the wall-clock comparison is
recorded.

**Blockers.** Both tutorials use spec machines and collection types,
so W3 and W4 must land first.

### W7 — Stretch goals *(deferred)*

A bridge that emits `.p` source from the registry so PLean-authored
programs can run under the existing PChecker model checker; an
evaluation of `lean-smt` as an alternative to Loom for the SMT
backend; and counter-example surfacing for failed obligations.
Tracked in [`PLAN.md` § Phase 7](PLAN.md). Not on the v1 critical
path.

---

## Division of effort

### Total effort to v1

Approximately 6–8 person-weeks of focused engineering, distributable
across 2–3 developers. The estimate covers W1 through W6; W7 is
post-v1 and unscoped here. Wall-clock time depends on parallelism: a
single developer working serially is closer to the upper bound,
two developers running W1+W2 alongside W3+W4 brings it down to
roughly four calendar weeks plus W6.

### Per-workstream sizing

Sizes are calendar-day estimates for a single focused developer
(S ≈ ½ day, M ≈ 1–2 days, L ≈ 3–5 days). The size column on each
workstream heading repeats this for quick reference.

| Workstream | Size | Estimate | Notes |
|---|---|---|---|
| W1 — close DistributedLock + LockServer | M | 1–2 days | Path-dependent: porting an extra invariant is ½ day; manual proofs may extend it |
| W2 — port ring-leader benchmark | M | 1–2 days | The structural port is ½ day; closing the lemma chain may add a day |
| W3 — add spec machines | L | 5–7 days | The bulk of Phase-4 work. Steps 1–2 are S each; steps 3–6 are M each; tests in step 7 are S |
| W4 — collection types and remaining surface | L | 4–6 days | SMT encoding is the largest unknown; the rest is mechanical |
| W5 — verifier polish | M | 2–3 days total | Seven independent items, each S; pick up between heavier work |
| W6 — port canonical tutorials | L | 3–5 days | One day per tutorial port plus measurement; assumes W3 + W4 are clean |
| W7 — stretch | deferred | — | Out of v1 scope |

### Effort by area

| Area | Workstreams | Skills emphasised |
|---|---|---|
| Protocol modelling | W1, W2, W6 | Reading the original P semantics; writing inductive invariants; manual proofs against the `pverify_*` tactic primitives |
| Lean elaboration and macros | W3 (steps 3, 4, 6), W5 (steps 2, 3) | Macro hygiene (the `mkIdent` discipline in [`CLAUDE.md`](../CLAUDE.md)); registry walks during expansion; `do`-block emission |
| SMT encoding and weakest-precondition rules | W4 (all steps), W5 (steps 4, 5) | Translating collection types and control flow into shapes lean-auto / `loom_smt` can discharge |
| Obligation generation | W3 (steps 1, 5), W5 (step 3) | Building Hoare-triple statements from registry data; threading dispatcher contracts |
| Test-suite curation | W3 (step 7), W6 (step 3) | `#guard_msgs` pinning; wall-clock measurement |

A developer comfortable in any one area can pick a workstream
matching that area without needing depth in the others.

### Critical-path summary

The v1 critical path runs **W3 → W4 → W6**. W3 and W4 can run in
parallel; W6 must follow both. W1, W2, and W5 are off the critical
path and can be picked up opportunistically without affecting the
v1 schedule.

A two-developer team running the critical path takes the longer of
W3 and W4, then W6. A three-developer team adds capacity for W1, W2,
and W5 to land alongside the critical path, which is what the
[Suggested division of labor](#suggested-division-of-labor) table
below assumes.

---

## Dependency graph

```
W1 ── close the small distributed-protocol benchmarks ┐
W2 ── port the ring-leader benchmark                  │
W5 ── verifier polish (independent items)             ├── feeds into W6
W3 ── add spec machines                               │
W4 ── add collection types and remaining surface      ┘

W3 + W4  ────  W6  (port the canonical tutorials → v1 cut)

W7 (stretch) — independent, deferred until after v1.
```

Reading the graph:

- W1 and W2 close out the small-benchmark verification effort. They
  are independent of each other and of all other workstreams.
- W3 and W4 are mutually independent; both are needed for the
  ChainReplication structural port to discharge real (rather than
  stubbed) spec obligations and for W6 to begin.
- W5 is a collection of small polish items. Most are independent of
  one another; pick them up between heavier workstreams.
- W6 is the v1 deliverable and waits on W3 and W4.

---

## Suggested division of labor

The table below is illustrative; adjust based on developer
strengths. The spec-machine and obligation-generator work in W3 is
heavy on Lean elaboration and macro hygiene (see the `mkIdent`
discipline documented in [`CLAUDE.md`](../CLAUDE.md)); W4's
collection-type work is heavy on SMT encoding and parity with
PVerifier; W1, W2, and W6 are heavier on protocol-modelling than
on framework engineering.

| Sprint | Developer A | Developer B | Developer C |
|---|---|---|---|
| 1 | W1: close DistributedLock and LockServer | W3 steps 1–3: auto-emit default obligations, widen `GlobalState`, materialise spec bodies | W4 steps 1–2: collection-type SMT encoding spike, membership and indexing |
| 2 | W2: port the ring-leader benchmark; then W5 step 2 (registry-aware `is`) and step 7 (sweep benchmarks to use `m is Node`) | W3 steps 4–6: spec dispatch from `send`, `assert` obligations, `is` for spec names | W4 steps 3–4: default values for collections, `assume` |
| 3 | W3 step 7: trivial-assert test, ChainReplication structural port, obligation-shape pin | W6 step 1: port `1_ClientServer` | W5 steps 3–6: handler wrapper, default-invariant case table, SMT fallback, tactic conveniences |
| 4 | W6 step 2: port `2_TwoPhaseCommit` | W6 step 3: wall-clock comparison; W7 spike if time permits | Backlog grooming, post-mortem, v1 cut |

---

## Onboarding for a new contributor

1. **Read [`CLAUDE.md`](../CLAUDE.md)** (top of `Src/PLean/`) — the
   build cookbook, hygiene rules, and `Stub.lean`-deletion gotcha.
2. **Skim [`PLAN.md`](PLAN.md)**, then jump to the **phase plan
   matching what you're picking up** (PLAN_P3 for Phase 3 residue,
   PLAN_P4 for spec machines, etc.).
3. **Check [`STATUS.md`](STATUS.md) "Active Work" and "Decision Log"**
   for what's currently in flight or just landed.
4. **Build & test from inside `Src/PLean/`:**
   ```bash
   cd Src/PLean
   lake build PLean
   lake build Tests Examples       # full regression + protocol suite
   lake build Examples.RingLeader  # single benchmark
   ```
5. **Pick a workstream above** and update [`STATUS.md`](STATUS.md)'s
   "At a Glance" table when you start (set Owner + Started date).

For verification work specifically: read
[`Tests/Surface/PVerifyManualProof.lean`](../Tests/Surface/PVerifyManualProof.lean)
to see the manual-proof workflow end-to-end, then
[`Verify/Tactic.lean`](../PLean/Verify/Tactic.lean) for the atomic
tactic library.

---

## Where things live (cheatsheet)

| Question | Look here |
|---|---|
| Why is feature X designed the way it is? | `PLAN.md` for the headline; `PLAN_P{0..4}.md` for the matching phase |
| What just landed / what's blocked? | [`STATUS.md`](STATUS.md) |
| Why was Phase 3 reviewed three times? | [`REVIEW_P3.md`](REVIEW_P3.md) |
| What does `#pverify` actually do? | [`Commands/PVerify.lean`](../PLean/Commands/PVerify.lean) → [`Verify/Obligation.lean`](../PLean/Verify/Obligation.lean) → [`Verify/Tactic.lean`](../PLean/Verify/Tactic.lean) |
| How is a `pmodule M` materialised? | [`Commands/GenModule.lean`](../PLean/Commands/GenModule.lean) (start at the top — it's pipelined) |
| What's a `system <s> { … }` block? | [`STATUS.md` § "2026-06-10 (system-binder)"](STATUS.md) |
| What's the macro-hygiene rule? | [`CLAUDE.md` § Conventions](../CLAUDE.md) |

---

_Maintainer: whoever is driving sprint planning. When this document
and [`STATUS.md`](STATUS.md) or [`PLAN.md`](PLAN.md) disagree, update
this document — those are authoritative._
