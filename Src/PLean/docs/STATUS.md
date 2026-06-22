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
| 3 — Verification declarations    | ◐ | — | 2026-06-06 | — | M3 partial — pipeline is sound; **2026-06-19: DistributedLock 12/12 and LockServer 37/37 — both fully verified**. LockServer's port now carries the full P-source invariant set (10 `system_config` + 8 `safety`); 34 obligations close by SMT (via the new `<ev>_payload_of_spec`/`_mk` characterisations + irreducible extractor), 3 send-handlers by `@[pverifyProof]`. Two soundness holes found+fixed this session (GlobalState-shadow guard generalised to all binders; sorried `@[pverifyProof]` now fails). |
| 4 — Spec machines                | ☐ | — | — | — | plan in [`PLAN_P4.md`](PLAN_P4.md) |
| 5 — Remaining surface            | ☐ | — | — | — | |
| 6 — Tutorial port                | ☐ | — | — | — | |
| 7 — Stretch / future             | ⊘ | — | — | — | post-v1 |

---

## Active Work

_Phase 3 (Verification declarations) — **architectural pivot landed
2026-06-09**, **SMT-prep recipe shipped 2026-06-10 (mid)**,
**soundness fix shipped 2026-06-10 (final-final)**.

`#pverify M` is an SMT-discharge command that walks the registry,
consults a `@[pverifyProof]` attribute for manual proofs, and emits
`theorem ... := by pverify` for the remaining obligations. The
`pverify_*` family in [`Verify/Tactic.lean`](../PLean/Verify/Tactic.lean)
is the user-facing primitive set for manual proofs (Veil's
`#check_invariants` / `@[invProof]` design)._

_**Soundness status (post-fix).** Invariants are explicitly bound
via a `system <s> { … }` block:_

```lean
Theorem safety {
  system s {
    invariant unique_holder :
      ∀ n1 n2 : Node,
        Node_allocated n1.ref s → Node_allocated n2.ref s →
        (s.machines n1.ref).fields.Node_held = true →
        (s.machines n2.ref).fields.Node_held = true →
        n1 = n2
  }
}
```

_The `system <s>` binder is materialised as the lambda binder of
`def unique_holder : GS → Prop := fun s => <body>`, so references
to `s` inside the body resolve to the obligation's state argument.
Bare `invariant <name> : <body>` outside a `system` block is
materialised as `fun _ => <body>` — only valid for state-independent
properties; any state reference becomes a clean `unknown identifier`
error. Inside a `system` block, an inner `∀ s : GlobalState Sig, …`
shadowing pattern is detected and rejected at materialisation time
with an actionable error.

This replaces the original (buggy) pre-2026-06-10 design where the
materialiser emitted `def name : Prop := <body>` (closed proposition)
and the bundle `fun _ => name ∧ True` ignored `s` — letting SMT
trivially "verify" falsehoods like `∀ b, b.x = 42` on a machine that
increments x._

_**Current closure rates (post 2026-06-17 base/inductive split +
kind-guard auto-injection):**_
- _DistributedLock: **10/12** (5/5 base-case, 2/2 default inductive,
  3/5 user-invariant inductive; the two failing inductive-step
  obligations on `eGrant`/`eAccept` are a genuine inductiveness gap)._
- _LockServer: **17/22** (all default base + inductive close; 4
  user-invariant inductive-step + 1 base-case `unique_lock_holder`
  remain as genuine inductiveness gaps)._
- _All other tests close 100% via SMT or `@[pverifyProof]`._

_Headline tests (all green at HEAD, 3397 jobs after the
field-projection sugar work):_
- _[`Phase3PingPong.lean`](../Tests/Surface/Phase3PingPong.lean) —
  trivial-handler auto-discharge._
- _[`PVerifyManualProof.lean`](../Tests/Surface/PVerifyManualProof.lean) —
  end-to-end `@[pverifyProof]` workflow._
- _[`PVerifyProofRegistry.lean`](../Tests/Surface/PVerifyProofRegistry.lean) —
  auto vs. manual paths side-by-side._
- _[`PVerifyConditional.lean`](../Tests/Surface/PVerifyConditional.lean) —
  `if cond then … else …` with and without an inner `send`._
- _[`SoundnessRegression.lean`](../Tests/Surface/SoundnessRegression.lean) —
  pins (a) `name : GS → Prop` shape, (b) false invariant fails,
  (c) inner-`∀ s` shadowing rejected._
- _[`FieldProjectionSugar.lean`](../Tests/Surface/FieldProjectionSugar.lean) —
  pins the `n.<v>` / `e.<f>` rewrites and the pass-through behaviour
  on non-registered fields._

_M3 acceptance: the SMT-discharge pipeline closes every obligation
that's actually inductive. The DistributedLock and LockServer ports
omit some of the original P source's invariants (e.g.
`not_held_after_release`, `transfer_to_higher`), so their `prove
safety` obligations correctly fail SMT and need either the missing
invariants or `@[pverifyProof]` manual proofs._

### Session 2026-06-19 (later) — reusable manual-proof tactics; LockServer manual proofs −114 lines

Three reusable tactic primitives from [`AUTOMATION.md`](AUTOMATION.md)
§3 landed in [`Verify/Tactic.lean`](../PLean/Verify/Tactic.lean), plus a
split-then-SMT fallback (§3.5) in the automatic discharge chain. The
LockServer send-handler manual proofs were refactored against them.

**New tactics:**
- `pverify_split_smt_close` — splits every top-level `∧` in the goal,
  closes each conjunct by `pverify_smt_close` independently, and
  short-circuits trivial `True` leaves. Wired into `pverify_close_chain`
  AFTER the single-shot `pverify_smt_close` (the common 1-query case is
  unaffected) and AFTER `default_inv`. Motivation: large bundles
  (LockServer's 11-conjunct `system_config`) return `unknown` from the
  solver as a single shot but each conjunct is individually decidable;
  the fallback recovers them automatically.
- `pverify_carry_through` — discharges a clause unchanged by the step
  (`assumption` → `solve_by_elim` → `simp_all` fallback). Replaces the
  verbatim `exact h<X>` pattern in else-branches where machines and the
  buffer are untouched.
- `pverify_carry_after_recv h` — discharges a routing clause's received-
  monotone shape (step marks `lbl` received, nothing else changes).
  Pre-state hypothesis passed as a term; the helper normalises
  `inflight` / `not_and` / `Bool.or_eq_false_iff` and exact-applies.
- `pverify_not_inflight hPre, hisE, is_<wrong-ev>` — the routing-
  clause "new label is a different event" case-split: the new-label
  branch closes by `is_<wrong-ev>` contradiction on `hisE`, old labels
  fall through to `hPre`.

**LockServer manual proofs.** Both else-branches (10 conjuncts each)
collapsed to the helper one-liners; the eAquire then-branch (4 wrong-
event conjuncts) and the eLock/eRelease then-branches (4 wrong-event
conjuncts each, with field-only kind-bridge prologues) reduced
similarly. Headline numbers:
- `Tests/Surface/Phase3LockServer.lean` went from 689 → 575 lines
  (−114 lines, ~16% file shrinkage; the three manual proofs themselves
  dropped from ~480 to ~340 lines of tactic code).
- LockServer remains **37/37** (34 SMT + 3 manual); DistributedLock
  remains **12/12** (11 SMT + 1 manual).
- Build wall-clock on `Tests` lean_lib unchanged (~73 s).

**Regression test:**
[`Tests/Verify/ManualProofHelpers.lean`](../Tests/Verify/ManualProofHelpers.lean)
pins each helper on synthetic miniature goals (no full `#gen_module` so
the test is independent of the M3 ports).

Full suite green at 3403 jobs.

**SMT-query cache (§5) — landed second attempt (Expr-level keying).**

First attempt keyed on the lean-auto query string and was reverted
same-session: `prepareLeanAutoQuery` (computing the key) cost as much
as the solver call it skipped → net 0% speedup. See
[`AUTOMATION.md`](AUTOMATION.md) §5 post-mortem.

Second attempt keys on the **(local context, goal target)** pair —
pretty-printed and `String.hash`-ed before `pverify_smt_prep` runs,
so a hit skips the prep simp set, the lean-auto translation, AND the
solver. Mirrors PVerifier's approach (checksum the generated UCLID
source) but at the Lean-`Expr` level. Storage: one file per cached
obligation under `<project>/.lake/build/pverify_cache/<hash>.ok`.
`lake clean` invalidates.

**Soundness.** Hits close the obligation via the same
`Loom.SMT.trust_smt` axiom `loom_smt` uses on `unsat`. The hash
includes every visible local hypothesis (name + type), so a goal that
needs hypothesis H₁ to discharge can't be closed by a cache entry
that was certified under a different hypothesis context.
[`Tests/Verify/CacheSoundness.lean`](../Tests/Verify/CacheSoundness.lean)
pins this with three goals where the target is the same but the
hypotheses differ — three distinct hashes confirmed.

**Measured wall-clock (truly cold via removing `.olean` + cache):**
- `Phase3DistributedLock`: cold 15.8s → warm 14.1s (~11% reduction).
- `Phase3LockServer`: cold 54.0s → warm 46.5s (~14% reduction).

Speedup grows with SMT-heavy workloads; on lighter tests it's a wash.
Cache entries: 20 for DistLock, 34 for LockServer (every SMT-discharged
obligation cached).

**Not yet landed from `AUTOMATION.md`:** parallel SMT (§4) and the
`pverify_send_handler` macro driver (§3.4). Parallel SMT is the
biggest remaining wall-clock win on cold runs; the cache compounds
nicely with it (cache cuts the work set, parallelism speeds up what
remains).

**Profile breakdown of the cold path** (added 2026-06-19;
opt-in via `set_option pverify.profile true`; see
[`Tests/Verify/ProfileProbe.lean`](../Tests/Verify/ProfileProbe.lean)
for the harness). Measured on a synthetic DistLock-equivalent
12-obligation pmodule, cache OFF:
- `smt.auto` (lean-auto translation, `prepareLeanAutoQuery`): 941 ms (**57%**)
- `smt.solver` (cvc5 process): 464 ms (**28%**)
- `smt.prep` (defunctionalisation simp chain): 252 ms (15%)
- `smt.assign` (`trust_smt` term build): 0.3 ms (<1%)

**Lean-auto translation is ~2× slower than the solver itself.** This
is the reason the goal-Expr cache works: hits skip both translation
and solver, not just solver. It also flags lean-auto as the highest-
value optimisation target for cold runs — replacing it (or pre-
caching its output per goal) would beat parallel-SMT.

**Warm-path (cache hit) profile.** All `smt.*` stages → 0; only
`cache.pp` (pretty-printing for the cache key) remains.

**Optimisation landed 2026-06-19: replace `Lean.Meta.ppExpr` with raw
`Expr.toString` for the cache key.** The pretty-printer's delaboration
pass was 191 ms/12 obligations (~16 ms each); the raw constructor-
shape printer (after `instantiateMVars` + `Expr.consumeMData`) does
the same job for cache hashing in 4 ms total (~0.3 ms each) — a **48×
speedup on `cache.pp`**, the warm path's dominant cost.

End-to-end measurement:

| | ppExpr key (prior) | toString key (now) | Speedup |
|---|---|---|---|
| Warm path (12 obligations) | 192 ms | **5 ms** | **38×** |
| `cache.pp` total | 191 ms | 4 ms | 48× |
| Cold-vs-warm (DistLock) | 9.6× | ~340× | — |

Soundness preserved: 12/12 + 37/37 cache hits maintained across runs
(`Expr.toString` is stable on identical normalised `Expr`s; macro-
scope `✝` drift that broke `Expr.hash`-based proposals doesn't affect
`toString`'s shape-based output).

### Session 2026-06-19 — LockServer 37/37; payload-characterisation infra; two soundness holes closed

**LockServer is now fully verified (37/37).** The port was completed and two
genuine soundness holes in the VC generator were found (via an adversarial
audit) and fixed.

**1. LockServer port completion.** The earlier skeleton dropped 5 of
`system_config`'s invariants and 7 of `safety`'s. Restoring the full P-source
set ([`8_LockServer/PSrc/System.p`](../../../Tutorial/Advanced/8_LockServer/PSrc/System.p))
made the previously-disproved `eGrant` safety obligation inductive (the
mutually-inductive strengthening invariants — `unique_grant`,
`no_lock_while_grant`, `node_server_mutex`, …) and the routing obligations
provable (`node_send_lock`/`node_send_unlock` are the load-bearing additions).
34 obligations now close by SMT.

**2. Payload-characterisation infrastructure** (generalises the manual
`eAccept_payload_of_mk` DistributedLock hand-wrote). `#gen_module` now emits,
per payload-bearing event:
- `<ev>_payload_of_mk` — the extractor's value on a literal `Label.mk`;
- `<ev>_payload_of_spec` — its value from any label with a known action;
- `attribute [irreducible] <ev>_payload_of` — so lean-auto translates the
  extractor as a true uninterpreted symbol even under a `∀` binder (an
  un-sealed `def` trips `lamTerm2STermAux :: Unexpected head term … lam`).

`emitOneObligation` brings every `<ev>_payload_of_spec` into context (`have`)
so SMT can compute a freshly-sent label's payload. DistributedLock's
hand-written `eAccept_payload_of_mk` was removed (now generated).

**3. The 3 send-handler obligations** (`eLock`/`eAquire`/`eRelease` on
`system_config`) return `unknown` from both solvers as a single-shot
11-invariant query, so they carry hand-written `@[pverifyProof]` proofs: split
the bundle into named conjuncts, do a new-vs-old-label case analysis per
conjunct (the fresh label's event tag mismatches non-matching routing clauses;
topology clauses transfer because the field-only machine update preserves
kind/control-state/`Node_server`).

**4. Two soundness holes found + fixed** (adversarial audit of the VC
pipeline):
- **GlobalState-shadow guard was ∀-only** ([`Surface/Verify.lean`](../PLean/Surface/Verify.lean)).
  `rejectExplicitStateBinder` matched only `∀ … : GlobalState …`, so a
  `let`/`have`/`fun`/`∃` binder of the state type
  (`let s : GlobalState Sig := default; …`) shadowed the `system`-block `s`,
  made the invariant state-independent, and let a **false** property verify as
  a clean pass. `containsExplicitStateBinder` now rejects *any* mention of the
  `GlobalState` type identifier in a `system`-bound invariant body (a
  well-formed body never names it). The same guard (`rejectStateShadowIn`) now
  also runs on `init-holds` bodies in `emitInitConditions`.
- **Sorried `@[pverifyProof]` reported as a pass**
  ([`Verify/Obligation.lean`](../PLean/Verify/Obligation.lean)). The manual-
  proof path discharges the obligation by `exact @<userThm>` in a
  `<name>_check` theorem and checked `_check`'s value for `sorry` — but that
  value is just a const reference, so `hasSorry` never saw a `sorry` in the
  user theorem's body. Both emitters now also inspect the user theorem's own
  value; a sorried manual proof is reported as unfinished (so `#pverify`
  fails under the default `failOnIncomplete`).

Both holes pinned by new probes 4–6 in
[`SoundnessRegression.lean`](../Tests/Surface/SoundnessRegression.lean).
(The audit also flagged the `on _ goto _` event/state name-collision as a
risk; verified it is a completeness bug — always yields a tactic-error
failure, never a false pass — not a soundness hole.)

### Session 2026-06-18 — SMT-discharge VC fixes; DistributedLock 12/12

A debugging pass on the M3 `unknown`/`disproved` residuals traced every
one to a VC-generator gap (not a missing invariant), and closed
DistributedLock end-to-end. Six fixes landed, each verified against
PVerifier's [`Uclid5CodeGenerator.cs`](../../PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs):

1. **`is_<ev>` characterisation lemmas** (commit `eb0eac3dc`).
   `#gen_module` now emits `is_<ev>_iff : is_<ev> l ↔ l.action = .event
   (E.<ev> …)` tagged `@[pverifySimp]`. `is_<ev>` was opaque to SMT, so
   any obligation with an `e is <ev>` guard returned `unknown`; the
   `@[pverifySimp]` rewrite turns it into a concrete action equality
   before `loom_smt`. Pairwise disjointness is derived by the solver from
   constructor injectivity. Same commit: the counter-example renderer
   flags kind/state desync (`[kind=N ≠ <M>_kind=K; not is_<M> — fields
   unconstrained]`) so spurious rows read as such.

2. **Default invariants decoupled** (commit `28ec6140e`). PVerifier only
   `ensures` the three sanity invariants on `prove default` obligations
   (`generateSanityChecks = cmd.Name == "default"`); PLean was conjoining
   `DefaultInvariants` into every obligation's pre and post, dragging the
   `actionCount` linear-integer arithmetic (from `≺`/`IncreasingCount`)
   into goals that don't need it → quantified UF+LIA → `unknown`. Now
   user-invariant obligations carry no `DefaultInvariants`; they're a
   separate well-formedness bundle. Sound (strictly fewer hypotheses) and
   ~4× faster (LockServer build 103s → 27s).

3. **`abstract_machine_lookups`** in `pverify_smt_prep` (commit
   `90fbed5f4`). `s.machines ((<ev>_payload_of e).field)` — the function
   field applied to a ref through the opaque payload extractor — made
   lean-auto emit an identity lambda and abort. The new step generalises
   such compound lookups (gated on a `…_payload_of` argument so it doesn't
   over-abstract plain `s.machines n.ref`).

4. **Dispatcher kind-guard** (commit `9a8645a2d`). The per-handler
   dispatcher contract now includes `is_<M> this.ref s` — PVerifier gets
   this structurally from `requires MachineStateAdtInS`; PLean's flat
   `MachineState` (independent `kind`/`currentState`) needs it stated, else
   the solver fabricates a `this` of the right control state but wrong kind
   (observed on DistributedLock eGrant: `this = Node#3` with `kind=5 ≠
   Node_kind=1`). Flipped both DL eGrant and a LockServer obligation from
   spuriously-disproved to honest.

5. **`@[pverifyProof]` type-checking** (commit `555641970`). The manual-
   proof registry was keyed on theorem *name* only: a theorem
   `<obligationName> : True := trivial` was accepted as discharging the
   real `triple …` obligation — a soundness hole. Both emitters now build
   the obligation's exact expected type and discharge it by `exact
   @<userThm>` in a `<name>_check` theorem, so the elaborator verifies the
   user proved precisely that proposition.

6. **`markReceived` prologue** (commit `df7acad25`). PVerifier marks the
   dispatched label received at handler entry (`received[label -> true]`);
   PLean did not, so the consumed event stayed in-flight in the post-state,
   spuriously violating any "in-flight `<ev>` ⇒ …" invariant (DL eAccept's
   `no_lock_while_transfer` was a genuine `sat`). The handler obligation
   now binds `lbl` as a universal parameter, states the dispatch conditions
   as plain preconditions, and runs `markReceived lbl >>= handler`. eAccept
   then closes by SMT.

**DistributedLock: 12/12** — 11 obligations by SMT, 1 (`eGrant` safety) by
a hand-written `@[pverifyProof]` (commit `cea639ae5`, updated for the
markReceived shape). The eGrant proof splits `safety` into its five
invariants across the two `if`-branches; nine of ten leaves close by
`pverify_smt_close`, and `transfer_to_higher` on the then-branch uses a
local `eAccept_payload_of_mk` characterisation + a new-vs-old-label case
split (the opaque extractor blocks SMT from computing the new label's
payload). Triage (split each conjunct, solve standalone) confirmed BOTH DL
obligations are fully inductive — no missing invariant.

**LockServer: 23/25** — unchanged. Its `safety` Theorem ports only
`unique_lock_holder`; it is a genuinely incomplete port missing the
mutually-inductive strengthening invariants from the P source
(`node_server_mutex`, `no_lock_while_grant`, `grant_server_unlocked`,
`unique_grant`, `unique_unlock`, …). Finishing it is a port-completeness
task, not a VC fix.

Follow-ups surfaced (not yet done): emit `<ev>_payload_of_mk`
characterisation lemmas from `#gen_module` (like `is_<ev>_iff`) to remove
that manual step; abstract `MachineRef` to an uninterpreted sort
(PVerifier uses `type M_Ref_t;`, not `Int`) to remove the wrapper round-
trip facts that make cvc5's quantifier instantiation fragile.

Full suite green at 3402 jobs.

### Session 2026-06-17 — VC fix: kind/state coupling in `<M>_allocated`

Reading a LockServer base-case counter-example with the v1 renderer
surfaced a genuine VC-generation defect, not just a lossy port. The
rendered model showed `n1 = Node#1` and `n2 = Node#4` whose machine
state was `Server@Serving` — a machine with `kind = Node_kind` but
`currentState` set to a *Server* state. `MachineState` flattens `kind :
Nat` and `currentState : S` (a union of every machine's states) as
independent fields, and `<M>_allocated` checked only the `kind` tag, so
the solver could fabricate impossible machines. A minimal probe
confirmed it: `∀ n : Node, (s.machines n.ref).currentState ≠
Server.SrvA_st` was *disproved at the base case* (it should be a
well-formedness tautology).

Fix ([`Commands/GenModule.lean::emitMachineKinds`](../PLean/Commands/GenModule.lean)):
`<M>_allocated m s` now also requires `(s.machines m).currentState ∈
<M>'s states`. PVerifier gets this for free from its typed per-machine
state arrays; PLean's flat encoding must state it. The conjunct only
ever weakens a guard antecedent (`is_<M> m s → …`) so it cannot make a
real obligation harder, and `goto` preserves it (transitions stay within
a machine's own states). For the coupling to reach SMT, `<S>_st` aliases
([`emitStateAliases`](../PLean/Commands/GenModule.lean)) are now
`@[reducible]` so prep's `dsimp only` reduces `currentState = <S>_st` to
the raw constructor.

The probe now closes (5/5). Benchmark rates are *unchanged*
(DistributedLock 10/12, LockServer 17/22) — confirming the fix removed
only spurious models, not honest failures. The residual LockServer
failures are now provably faithful: with well-formed models, the
`unique_lock_holder` base case fails because the port's `init-holds`
dropped the P source's `∀ n : Node :: !n.has_lock` (Nodes start
unlocked) and `const_server` clauses. Full suite green at 3401 jobs.
[`Tests/Surface/Phase3R20.lean`](../Tests/Surface/Phase3R20.lean) gains
a `decide` regression pinning the desync exclusion.

### Session 2026-06-17 (latest) — Counter-example rendering (v1)

`#pverify`'s disproved obligations used to print the solver's
`(get-model)` reply verbatim: a one-line wall of `(define-fun
valid_fact_N () Bool …)` boilerplate interleaved with lean-auto-mangled
symbols (`_machines.116_`, `_sent.1546_`), capped at 12 lines / 1500
chars. The clause that broke, the per-machine state, and the send order
were all unreadable. v1 decodes the model into a structured
counter-example. Plan: [`PLAN_CEX.md`](PLAN_CEX.md).

What landed:
- [`Verify/CexParse.lean`](../PLean/Verify/CexParse.lean) (NEW) —
  `extractModelText` strips `loom_smt`'s `the goal is false:` prefix;
  `parseModel` re-parses the model S-expression via
  `Auto.Parser.SMTSexp` and keeps non-boilerplate `define-fun`s;
  `demangle` maps lean-auto's `"_" ++ delab(expr)` names back to
  readable bases (`_sent.1546_ → sent`, `_Label.actionCount →
  Label.actionCount`) by dropping the leading `_` and trailing all-digit
  gensym segments. No Loom / lean-auto changes — the full untruncated
  model already reaches PLean in `pverifySmtDiagRef`.
- [`Verify/CexModel.lean`](../PLean/Verify/CexModel.lean) (NEW) —
  `CexModel.decode` reads the `machines` table (a `MachineState.mk …`
  per `MachineRef`) and the `sent` set, rendering against a
  `CexNameCtx` (see the registry-aware follow-up below). Every section
  degrades to a de-mangled raw value when context is missing, so output
  is never worse than the verbatim dump.
- [`Verify/Obligation.lean`](../PLean/Verify/Obligation.lean) —
  `classifyFailure`'s `.disproved` branch now routes through `renderCex`
  (decode → render, raw fallback). `truncateForReport` gained
  configurable caps; the disproved path uses a looser bound (60 lines /
  6000 chars) so a decoded CEX survives.
- Tests: [`Tests/Verify/CexParserGolden.lean`](../Tests/Verify/CexParserGolden.lean)
  pins exact rendered output on synthetic models (both encodings) +
  de-mangling; [`Tests/Verify/CexEndToEnd.lean`](../Tests/Verify/CexEndToEnd.lean)
  asserts structural invariants on a captured cvc5 model (machine table
  + sorted sent trace present, mangling / `valid_fact_*` absent).

Registry-aware rendering (follow-up, same session): the renderer now
takes a `CexNameCtx` (built in `synthesise` from the registry and stashed
in `cexNameCtxRef`) carrying state-ctor → (machine, state), the global
`Fields` order as `(machine, var)`, and event payload field names (read
from the materialised `<Mod>.<payloadType>` structure via
`getStructureInfo?`, since `#gen_module` clears `defStx` before
`synthesise` runs). With it:
- machines render `Node@Act(epoch=9, held=false)` — machine kind, control
  state, and named `var` fields (sliced from the global `Fields.mk` by
  the state's owning machine);
- the `sent` set is collected as every `Label.mk …` subterm (robust to
  the `or`-of-equalities / `ite`-chain / `let`-bound shapes the solver
  emits), rendered `eGrant(node=4, epoch=7719)`, ordered by
  `actionCount`, with a `[delivered]` marker from `received`; an empty
  set prints `[]`;
- the `other` section is now `witnesses (handler & skolem bindings)` —
  the obligation's `this` / payload / skolem witnesses — with
  uninterpreted-sort universe values (`Type!val!0`), `<state>_st` aliases,
  `is_*` / `*_payload_of` tables, and `k!*` skolems filtered out;
- negative `Int` literals collapse from `(- 1)` to `-1`;
- `MachineRef`-typed values render as machine labels `<Kind>#<ref>`
  (e.g. `eGrant(node=Node#8, …)`, target `→ Node#8`, `this = Node#3`).
  `MachineRef` is a reducible `Nat` alias, so the model can't distinguish
  a ref from any other `Nat`: `buildCexNameCtx` reads each payload/var
  field's projection-function return type from the environment to find
  the `MachineRef`-typed fields (`refFields`), and the renderer learns
  each ref's kind from the `machines` table (`Node@Act → ref is a
  Node`). A ref not present in `machines` has unknown kind and renders
  bare (`#24`).

Live DistributedLock `eGrant` CEX, after:
```
✗ DistributedLock.Node.Act.eGrant_correct_Safety_safety  [SMT: counter-example]
      counter-example:
        machines:
          machine[8]  = Node@Act(epoch=9, held=false)
          machine[42] = Node@Act(epoch=-1, held=false)
        sent (ordered by actionCount):
          @5246  → Node#8  eGrant(node=Node#42, epoch=72)
          @11100 → #3      eGrant(node=#4, epoch=7719)
        actionCount = 14099
        witnesses (handler & skolem bindings):
          this = Node#8
          param = (tGrant.mk 4 7719)
          sk0 = eGrant(node=#4, epoch=7719)
```

Closure rates unchanged (DistributedLock 10/12) — the change is purely
presentational. All CEX + Phase-3 tests green.

Deferred to v1.5 / v2 (see [`PLAN_CEX.md`](PLAN_CEX.md)): CVC5
`--finite-model-find` re-query for cleaner finite universes; pre/post
state diff for inductive-step obligations; exposing lean-auto's
`h2lMap` / `l2hMap` for exact symbol→`Expr` recovery instead of the
heuristic de-mangle; `(get-abduct …)` missing-invariant suggestions.

### Session 2026-06-17 (later) — Field-projection sugar inside `system` blocks

Inside an invariant body (or `init-holds` clause) under a `system <s>
{ … }` block, two new desugarings remove the verbose
`(s.machines x.ref).fields.<M>_<v>` / `(<ev>_payload_of e).<f>`
shapes:

| User wrote | Materialiser emits |
|---|---|
| `n.<v>`  (where `n : <M>`, `<v>` is a registered machine var) | `(s.machines n.ref).fields.<M>_<v>` |
| `e.<f>`  (where `e : <ev>`, `<f>` is a payload field)         | `(<ev>_payload_of e).<f>` |

The rewrite is gated on the field name being registered, so `n.ref`,
`e.action`, `e.target`, `e.actionCount`, `s.machines`, etc. pass
through unchanged. Bare top-level invariants (no `system` block) skip
the rewrite — they have no `s` binder to feed the machine-field
expansion.

Implementation:
- [`Surface/Verify.lean::rewriteFieldProjections`](../PLean/Surface/Verify.lean)
  walks the body Syntax with a `KindRef` env tracking quantifier-bound
  machine / event names. Handles both Lean's `Term.forall` shape and
  mathlib's `«term∀_,_»` / `«term∃_,_»` macros. Runs **before**
  `injectKindGuards` so the original quantifier types are visible
  (kind-guard injection retypes event binders to `Sig.Label`, which
  would defeat the lookup).
- [`Commands/GenModule.lean::emitEventPayloadAccessors`](../PLean/Commands/GenModule.lean)
  emits one `<ev>_payload_of : Sig.Label → <PayloadTy>` per event with
  a named-tuple payload. The def is left **opaque** to SMT — lean-auto
  treats it as an uninterpreted function, which is the right SMT-level
  interpretation: `<ev>_payload_of e` becomes an applied uninterpreted
  symbol whose return value the user invariant constrains transitively
  via `is_<ev>` (also an uninterpreted predicate at SMT level).
- Field maps `machineFields : NameMap NameSet` and
  `eventPayloadFields : NameMap NameSet` are built once in
  `elabPGenModule` and threaded through `materialiseInvariant` and
  `emitInitConditions`.

**Migrated tests**: `Tests/Surface/Phase3LockServer.lean` and
`Tests/Surface/Phase3DistributedLock.lean` now use `n.has_lock` /
`n.held` / `e.source` etc. instead of the verbose forms.

**Closure rates unchanged**: `Phase3DistributedLock` 10/12,
`Phase3LockServer` 17/22 — the rewrite produces definitionally equal
terms.

**Regression test**:
[`Tests/Surface/FieldProjectionSugar.lean`](../Tests/Surface/FieldProjectionSugar.lean)
— five pmodules pinning the rewrites for machine fields, event
payloads, mixed quantifiers, pass-through (non-registered fields), and
`init-holds`.

### Session 2026-06-17 — VC partition + kind-guard auto-injection

Three changes landed this session, each motivated by a careful read of
PVerifier's [`Uclid5CodeGenerator.cs`](../../PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs)
to keep PLean's semantics aligned with the reference.

1. **`Higher order input?` translator rejection fixed.** Per-handler
   triples now unfold the per-pmodule `is_<M>`, `<M>_allocated`, and
   `<M>_kind` predicates before SMT. Without the unfolds, lean-auto
   saw function-typed `GlobalState` fields at sort-level and rejected
   the goal. Order matters: `is_<M>` precedes `<M>_allocated` because
   the former delegates to the latter; `unfold` fails atomically when
   a name isn't present, so we emit one `try unfold` per name. See
   [`Verify/Obligation.lean::emitOneObligation`](../PLean/Verify/Obligation.lean).

2. **Per-handler triples no longer carry `InitConditions`.** The pre
   *and* post used to conjoin `InitConditions s ∧ …`, which was
   unsound on both sides:
   - mid-trace, the user's init-condition is no longer assumable;
   - a handler that, e.g., adds a label to `sent` cannot preserve a
     "no labels ever sent" init clause as a post-condition.

   PVerifier is faithful to the consecution discipline: its handler
   procedures' `requires`/`ensures` mention only `InFlight + state-tag
   + event-tag + default invariants + user invariants` ([`Uclid5CodeGenerator.cs:1432-1562`](../../PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs#L1432-L1562)).
   Init clauses live exclusively inside UCLID's `init {}` block, where
   they are `assume`d once at startup ([`:1266-1286`](../../PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs#L1266-L1286)).
   UCLID's `invariant`-declaration discipline then base-case-checks
   each goal automatically; PVerifier emits no separate "Init ⇒ Inv"
   obligation because UCLID gets it for free.

   PLean has no UCLID and no BMC engine, so the base case has to be
   discharged explicitly. We now emit **one base-case VC per
   individual invariant in each `prove G [using P]` directive's target
   bundle**, of the form `∀ s, InitConditions s → i s`. Per-invariant
   (rather than one conjoined VC) so a failed base case names exactly
   which clause doesn't follow from init. Premises (`using P`) do NOT
   get base-case VCs from a directive that uses them — only goals do
   — matching PVerifier's "only `cmd.Goals` get a UCLID `invariant`
   declaration" semantics. See
   [`Verify/Obligation.lean::emitBaseCaseObligation`](../PLean/Verify/Obligation.lean)
   and the `synthesise` driver.

   `<Mod>.InitConditions` was extended to fold in the framework start
   state alongside user `init-holds` clauses, mirroring
   `GenerateInitBlock`: buffers `sent` / `received` start empty,
   `actionCount` starts at 0. Without this, the trivial
   `UniqueActions`/`IncreasingCount`/`ReceivedSubsetSent` base cases
   would be unprovable (they implicitly depend on the empty buffer).
   `InStart`/`InEntry` for every `MachineRef` is a PVerifier
   convention PLean does not yet model — to add when initialization-
   action support lands.

3. **Kind-guard auto-injection on machine and event quantifiers.**
   PLean wraps machines in `structure <M> where ref : MachineRef`
   (D10) so `∀ n : <M>, P n` quantifies over every `<M>`-wrapped
   ref — including ones whose state slot is unallocated or has the
   wrong kind. The convention had been to follow each such quantifier
   with a manual `is_<M> n.ref s →` guard. The new
   [`Surface/Verify.lean::injectKindGuards`](../PLean/Surface/Verify.lean)
   does this automatically at materialisation: every `∀`/`∃` over a
   machine kind gets `is_<M> x.ref s` injected, every `∀`/`∃` over an
   event kind has its binder retyped to `Sig.Label` and gets `is_<ev>
   x` injected (matching PVerifier's `∀ (e : eAccept)` surface).
   Multi-binder forms (`∀ x y : T, …` and `∀ (x : T) (y : U), …`) are
   normalised to nested singles before the matcher fires.

   The `Phase3DistributedLock` test exercises all three forms (machine
   multi-ident, event single, event multi-ident in the bracketed
   `∀ (n1 : Node) (e : Sig.Label) (p : tAccept), …` shape).

**Headline tally at HEAD (commit `209768396`):**
- DistributedLock: 2/4 → **10/12**
- LockServer: 2/15 → **17/22**
- SoundnessRegression's false invariant now disproves on BOTH legs
  (base case + inductive step) — two independent checks instead of
  one.
- Full test suite: 3396 jobs green.

**Outstanding** (deferred, unchanged from prior session): port
`3_RingLeaderVerification` (M3 stretch); supply `@[pverifyProof]`
theorems for the residual inductive-step gaps in DistributedLock /
LockServer once the missing inductive-strengthening invariants are
identified.

### Session 2026-06-09 (afternoon) — Phase-3 architectural pivot landed

Implements the Plan recorded in the morning session (below). All 1007
test jobs pass; M3 acceptance is no longer gated on a single tactic
ladder — the user-facing manual-proof escape hatch (`@[pverifyProof]`)
plus a configurable strict-mode option (`pverify.failOnIncomplete`)
make every benchmark file build clean even when the SMT pass leaves
residual obligations.

What landed (kept in tree, full test suite green):

- [`Semantics/Primitives.lean`](../PLean/Semantics/Primitives.lean) —
  `send` / `raise` / `goto` / `announce` / `markReceived` /
  `newMachine` are now `@[reducible] def` so the obligation
  generator's `unfold` step + `wpgen`'s `whnf` reduce them to the
  underlying `get` / `set`. Combined with the existing per-pmodule
  `#derive_lifted_wp` that registers `loomSpec` lemmas for `get` /
  `set`, this resolves R-P3.2 (per-primitive `loomSpec` lemmas) by
  routing through the existing get/set spec rather than emitting a
  separate spec per primitive.
- [`Commands/GenModule.lean::emitVarAccessors`](../PLean/Commands/GenModule.lean) —
  per-machine `<v>_get` / `<v>_set` accessors gain `@[reducible]`;
  this resolves R-P3.1 (per-accessor `#derive_lifted_wp`) for the
  same reason as above.
- [`Verify/Tactic.lean`](../PLean/Verify/Tactic.lean) — rewritten
  around atomic primitives the user composes by hand:
  `pverify_open_triple`, `pverify_step_wp`, `pverify_unfold`,
  `pverify_normalize_state`, `pverify_smt_close`, `pverify_grind`,
  plus the head-symbol-gated `default_inv`. The headline `pverify`
  tactic is now `pverify_step_wp ; <intros> ; first | default_inv
  | pverify_smt_close | pverify_grind`; the SMT call is the
  load-bearing discharger.
- [`Verify/ProofRegistry.lean`](../PLean/Verify/ProofRegistry.lean)
  (NEW) — `@[pverifyProof]` attribute + persistent env extension
  keyed on theorem names. Mirrors Veil's `@[invProof]`. The
  obligation generator consults this registry first, before
  attempting auto-discharge.
- [`Verify/Obligation.lean`](../PLean/Verify/Obligation.lean) —
  rewritten to reflect the new architecture. `emitOneObligation`:
  (1) consults `pverifyProofExt` for a manual proof, (2) emits
  `theorem ... := by first | <pverify chain> | sorry`, (3)
  inspects the elaborated theorem's value with `info.value.hasSorry`
  to detect failures (catches both sync and async-snapshot tactic
  errors that the message-log capture misses). `synthesise` D24:
  walks every (M, S, ev) and auto-emits `prove default;` when the
  user didn't list it explicitly (uses synthetic
  `block_auto_default` proof tag for collision avoidance). The
  `SynthesiseResult` carries separate `smtProved` / `userProved` /
  `failed` counters.
- [`Commands/PVerify.lean`](../PLean/Commands/PVerify.lean) — output
  format updated to `<modName>: N obligations from K
  prove-directives (M proved by SMT, J user-proved, L failed)`.
  New `pverify.failOnIncomplete` option (default `true`):
  obligation failures throwError under `true`, log a warning under
  `false`. Mirrors Veil's `veil.failedCheckThrowsError`.
- New tests:
  [`PVerifyManualProof.lean`](../Tests/Surface/PVerifyManualProof.lean)
  (end-to-end manual proof workflow) and
  [`PVerifyProofRegistry.lean`](../Tests/Surface/PVerifyProofRegistry.lean)
  (auto vs manual paths side-by-side).
- [`Phase3DistributedLock.lean`](../Tests/Surface/Phase3DistributedLock.lean)
  and
  [`Phase3LockServer.lean`](../Tests/Surface/Phase3LockServer.lean)
  now invoke `#pverify` under
  `set_option pverify.failOnIncomplete false`. Output: the
  obligation count + the copy-paste `@[pverifyProof] theorem ... :=
  by sorry` skeletons users fill in. M3 acceptance becomes a matter
  of writing those manual proofs (the architectural blocker is
  gone).

Outstanding for next session:
- Author the `@[pverifyProof]` theorems for DistributedLock's 4 and
  LockServer's 15 obligations using the atomic `pverify_*` tactics.
  Once those land, drop `pverify.failOnIncomplete false` from the
  benchmark files and report (M proved by SMT, K user-proved,
  0 failed).
- Port [`3_RingLeaderVerification`](../../../Tutorial/Advanced/3_RingLeaderVerification/PSrc/System.p)
  (M3 stretch).
- Decide whether `loom_smt`'s defunctionalisation pre-pass for
  `GlobalState`-shaped goals lands in P3 or P4 (per the
  `Tests/Semantics/SmtRoundtrip.lean` header note).

### Session 2026-06-09 (morning) — registry / accessor groundwork; pverify tactic ladder unfinished

What landed (kept in tree, all 1004 tests pass):

- [`Internal/Decls.lean`](../PLean/Internal/Decls.lean) —
  `PMachineDecl` gained a `materialised : Bool := false` flag. The
  body Syntax is now retained after `#gen_module`'s last step; the
  flag is what `#pwf` consults to detect "did the user run
  `#gen_module`" (replaces the prior body-empty heuristic).
- [`Commands/GenModule.lean`](../PLean/Commands/GenModule.lean) —
  step-8 finalisation only sets `materialised := true` instead of
  resetting `body := #[]`, so `Verify/Obligation.lean::synthesise`
  can re-walk machine bodies to extract `var` declarations and
  build per-handler accessor-unfold lists.
- [`Commands/PWf.lean`](../PLean/Commands/PWf.lean) —
  materialisation check repointed onto the new flag.
- [`Semantics/Primitives.lean`](../PLean/Semantics/Primitives.lean) —
  `send` / `raise` / `goto` / `announce` / `markReceived` /
  `newMachine` reverted from `abbrev` back to plain `def` (an
  `abbrev` experiment was reverted because Loom's `wpgen` matches
  on the `def`-shape constant, not the underlying body).

What did **not** land (rolled back to keep the test suite green at HEAD):

- The `pverify` tactic ladder: I re-architected `pverify` /
  `pverify_default` around an outer `wpgen <;> first | apply
  WPGen.default | skip` followed by a Loom WP-stepping simp set,
  destructure-precondition, `pverify_split_state` + a closing
  chain. Phase3PingPong (trivial-handler) closed under this ladder,
  but the M3 benchmarks did not. The macro-hygiene path through the
  obligation generator hygienically marked every simp lemma name
  (`wp_bind✝`, `StateT.wp_get✝`), so the generator-emitted simp
  pass made no progress on its own. I introduced two helper
  tactics — `pverify_simp_step` (post-`wpgen` cleanup) and
  `pverify_simp_post` (after destructure) — to host the simp set
  inside named tactics so the lemma references stay un-hygienic;
  that fixed the hygiene side but the ladder still leaves a
  `wp get (fun x ↦ wp get …)` chain when the goal sits over
  `NonDetT (StateT _ _)` — `StateT.wp_get` doesn't fire because
  the lift hasn't been peeled. Reverted to the prior baseline so
  the test suite (Phase3PingPong, ObligationShape,
  Phase3DuplicateTarget, PVerifyTactic, Phase3Errors, Phase3R20)
  stays green; the in-progress branch is preserved as session
  notes here.

### Phase-3 close-out plan — superseded

The detailed 8-step plan recorded here on 2026-06-09 (re-architect
`#pverify` as an SMT-discharge command + `@[pverifyProof]` attribute
+ atomic `pverify_*` tactic library + R-P3.1/R-P3.2 follow-ups +
M3 benchmarks) **has shipped**. The Decision Log entries below
record what actually landed:

- 2026-06-09 (afternoon) — Phase-3 architectural pivot landed (steps 3–7).
- 2026-06-10 (later still) — R-P3.1 / R-P3.2 closed via the
  `(get : ...)` ascription drop in accessors / primitives.
- 2026-06-10 (later) — Veil-recipe SMT-prep ported.
- 2026-06-10 (final) — `WithName` strip + per-conjunct unfolds added
  to the SMT-prep ladder.
- 2026-06-10 (final-final-fix) — soundness hole closed; invariants
  state-parameterised.
- 2026-06-10 (system-binder) — explicit `system <s> { … }` block
  syntax replaces the unhygienic `s` trick.
- 2026-06-17 — base/inductive split + kind-guard auto-injection on
  machine and event quantifiers.

Retain only the cross-cutting takeaways that survive into the
implementation:

- **`#pverify M` is an SMT-discharge command, not a tactic engine.**
  For each obligation it (a) consults `@[pverifyProof]` for a
  user-supplied theorem matching the obligation's name, (b) emits
  `theorem ... := by first | <pverify chain> | sorry`, (c) inspects
  `info.value.hasSorry` to detect failure. **Don't bake substantive
  automation into `#pverify` itself**; that work belongs in the
  user-callable `pverify_*` tactics or `@[pverifyProof]`-tagged
  theorems.
- **The atomic `pverify_*` library lives in
  [`Verify/Tactic.lean`](../PLean/Verify/Tactic.lean).** Users compose
  these by hand for manual proofs (mirrors Veil's
  `solve_clause` / `sts_induction` / `sdestruct`). The obligation
  generator emits the same chain in its preamble for the auto path.
- **M3 acceptance set** — three Tutorial/Advanced benchmarks port to
  PLean and verify via `#pverify`, no hand-written triples:
  1. [`Tutorial/Advanced/6_DistributedLock`](../../../Tutorial/Advanced/6_DistributedLock/) — minimum (40 lines, ~8 VCs).
  2. [`Tutorial/Advanced/8_LockServer`](../../../Tutorial/Advanced/8_LockServer/) — exercises machine-kind `is` (96 lines, ~20 VCs).
  3. [`Tutorial/Advanced/3_RingLeaderVerification`](../../../Tutorial/Advanced/3_RingLeaderVerification/) — exercises `Lemma using` chain (92 lines, ~25 VCs).
  Heavier benchmarks (Consensus, 2PC, ChainReplication, Paxos) need
  Phase 4 (spec machines) and/or Phase 5 (`foreach`, maps) and are
  deferred from M3.

### Pitfalls observed in the 2026-06-09 attempt (avoid repeating)

- **Macro hygiene on simp lemma names**: `simp [wp_bind, …]` inside
  a macro defined under `namespace PLean` rewrites the bare `wp_bind`
  to `wp_bind✝` at expansion. Symptom: `simp` makes no progress
  even though the lemmas exist. Workaround: route every simp through
  a named tactic helper (e.g., `pverify_simp_post`) where the
  references are stable.
- **`obtain ⟨a, b, …⟩ := hpre` is greedy across `∃`**: when the
  precondition is `(stuff) ∧ ∃ lbl, P`, `obtain ⟨a, b, c, d, e⟩`
  doesn't flatten as you'd expect — it descends into the existential
  too. Two-step destructuring (`have hLemma := hpre.1; obtain ⟨…⟩
  := hpre.2`) is more predictable.
- **`unfold triple; intro s hpre` then `wpgen` fails** because
  `wpgen` matches on the `triple ?pre ?body ?post` head; once you've
  unfolded `triple`, the goal is `?pre ≤ ?wpg.get post` and
  `wpgen` no longer applies. Either let `wpgen` fire first (it
  unfolds `triple` internally) or use raw `wp` reasoning.
- **Hygienic `unfold X✝` fails**: the obligation generator emits
  `try unfold $someIdent:ident*` which can pick up hygiene marks
  on common-name idents like `Node.Act.eGrant_handler` only when
  threaded through certain quotation paths. Confirmed reliable
  pattern: pass the ident array via `mkIdent (mname ++ …)` and
  splice via `$[$ids:ident]*` in the proofTacSeq quotation. Avoid
  re-quoting in nested macros where possible.
- **`#pwf` materialisation check** — see Decision Log
  2026-06-09 entry. The body-retention change broke `#pwf` until
  the new `materialised` flag was added; both pieces ship together.

**Phase 3 highlights** (PLAN_P3 D19–D28; surface-keyword summary is
now in [`../CLAUDE.md`](../CLAUDE.md)):
- `Lemma`/`Theorem`/`Proof` blocks (D19) parse and register into the
  pmodule registry; `prove X using Y, Z;` directives drive
  obligation-generation.
- `m is <MachineKind>` (D20) extends the Phase-2 `is` notation
  (resolves R14).
- `init-condition`s aggregate into `<Mod>.InitConditions` (D21).
- `pverify` tactic (D22) — PLean's `loom_solve`-equivalent. Phase-2
  confirmed `CaseStudies.Tactic.loom_solve` doesn't fit (it needs
  Cashmere's `WithName` registration); PLean re-composes the pieces
  in [`Verify/Tactic.lean`](../PLean/Verify/Tactic.lean).
- Handler `_wrapped` form (D27) for auto `markReceived` is **deferred**
  to Phase 3b — current obligations carry the M2-style existential
  precondition.

**Note on the Done log below**: the first "Phase 0 — Bootstrap" entry
records the *initial* landing and mentions superseded spellings
(`<ev>.payload`, `init`, `pure`, `#endmachine`/`end M`). The
"Refinement" entry immediately after it supersedes those. When in doubt,
trust this Active-Work summary and the code, not the historical entry.

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

---

## Blockers / Open Questions

_None yet._

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

### 2026-06-10 (system-binder) — Explicit `system <s> { … }` block for invariant state binding

Refinement of the soundness fix (entry below). The fix bound the
invariant body's `s` via an unhygienic-`mkIdent` trick: the
materialiser emitted `def name : GS → Prop := fun s => <body>` with
an unhygienic `s` so the user's bare `s` reference would resolve.
That worked but was invisible to the reader, brittle (any binder
named `s` would shadow), and gave confusing errors on typos.

Replaced with an explicit binder block:

```lean
Theorem safety {
  system s {
    invariant unique_holder : ∀ n1 n2, ... s.machines ...
    invariant no_lock_while_transfer : ...
  }
}
```

- **Surface** ([`Surface/Verify.lean`](../PLean/Surface/Verify.lean)): new
  `pSystemInv` syntax category and `system <ident> { pSystemInv* }`
  command (top-level) plus `pLemmaSystemBlock` (Lemma/Theorem-internal).
  `PInvariantDecl` gains a `stateBinder : Option Name` field that the
  materialiser consults.
- **Materialisation** ([`Surface/Verify.lean::materialiseInvariant`](../PLean/Surface/Verify.lean)):
  `stateBinder = some n` → `fun n => <body>` (n is an unhygienic
  `mkIdent`); `stateBinder = none` → `fun _ => <body>` (the body is
  state-independent by construction; any state reference becomes
  `unknown identifier`).
- **Defense-in-depth**: the leading-`∀ s : GlobalState Sig, …`
  rejection (which was a soundness back-stop in the original fix)
  now only fires *inside* a `system` block — that's the only place
  where the inner ∀ would shadow. Outside `system`, an explicit
  `∀ s : GlobalState Sig, P` is allowed because the outer binder is
  the wildcard `_` (no shadowing risk).
- **Tests**: all 11 `#pverify` benchmark files migrated to
  `system <s> { … }`. Same closure rates as before
  (DistributedLock 2/4, LockServer 2/15 — the 13 failures were and
  remain genuine inductiveness gaps in the ports).
  [`Tests/Surface/SoundnessRegression.lean`](../Tests/Surface/SoundnessRegression.lean)
  pins (a) shape `name : GS → Prop`, (b) the false-invariant correctly
  fails with `1 failed`, (c) the inner-∀-binder shadowing rejection
  inside a `system` block.

Build: 985 jobs green.

### 2026-06-10 (final-final-fix) — Soundness hole in invariant materialisation closed

**Severity: critical.** Pre-fix, `#pverify` could "verify" trivially-false safety properties. Confirmed empirically:

```lean
pmodule SoundnessBreak
  event eGo
  machine Bad {
    var x : Nat
    start state Act { on eGo { x = x + 1 } }
  }
  Theorem broken {
    invariant always_x_is_42 :
      ∀ s : GlobalState Sig, ∀ b : Bad, Bad_allocated b.ref s →
        (s.machines b.ref).fields.Bad_x = 42
  }
  Proof Safety { prove broken ; }
end SoundnessBreak
#gen_module SoundnessBreak
#pverify    SoundnessBreak  -- pre-fix: "2 proved by SMT, 0 failed" ✗
```

**Root cause.** Three pieces conspired:

1. [`Surface/Verify.lean::materialiseInvariant`](../PLean/Surface/Verify.lean) emitted `def unique_holder : Prop := <body>` — a closed `Prop`, not a `GlobalState Sig → Prop` predicate.
2. [`Commands/GenModule.lean::emitLemmaBundles`](../PLean/Commands/GenModule.lean) emitted `def safety : GS → Prop := fun _s => unique_holder ∧ True` — the `_s` argument was thrown away; the conjunction referenced the closed `Prop` `unique_holder` directly.
3. The user, writing P-style invariants, would put `∀ s : GlobalState Sig, P(s)` inside the body to "introduce" the state. The body became a closed proposition (the universal binder is meaningless; you can prove `∀ s, anything → anything`).

The obligation generator's pre/post both used `safety s` — but `safety s` reduced to `unique_holder ∧ True`, **independent of `s`**. So every `prove <safety>` obligation became `<closed-prop> ∧ ... → <closed-prop> ∧ ...` — a tautology, regardless of what the handler did.

**Fix.** Three coordinated changes:

- **Materialise invariants as state-parameterised** ([`Surface/Verify.lean`](../PLean/Surface/Verify.lean)). Emit `def name : GS → Prop := fun s => <body>` where `s` is unhygienic so the user's body can reference it. The user writes `invariant unique_holder : ∀ n1 n2, … s.machines …` with bare `s`.
- **Bundle predicates apply each invariant to `s`** ([`Commands/GenModule.lean::emitLemmaBundles` / `emitUserInv`](../PLean/Commands/GenModule.lean)). Emit `def safety : GS → Prop := fun s => unique_holder s ∧ ... ∧ True`.
- **Reject the buggy `∀ s : GlobalState Sig, …` shadowing pattern** ([`Surface/Verify.lean::rejectExplicitStateBinder`](../PLean/Surface/Verify.lean)). At materialisation, look for the leading-`∀`-binder-typed-`GlobalState` syntax and throw an explicit error pointing at the implicit form. This prevents porters who copy old PLean code from re-introducing the bug.
- **Per-invariant unfolds in the obligation generator** ([`Verify/Obligation.lean`](../PLean/Verify/Obligation.lean)). The bundle predicate `safety s = unique_holder s ∧ no_lock_while_transfer s ∧ ...` only exposes opaque applications; SMT needs the per-invariant defs unfolded too. The obligation generator now collects `lemma.invariants` from the registry and adds each to the `unfold` chain.

**Tests added.** [`Tests/Surface/SoundnessRegression.lean`](../Tests/Surface/SoundnessRegression.lean) pins both: (1) the false-invariant case — `1 failed` count via `#guard_msgs`; (2) the rejected-pattern case — registration-time error via `#guard_msgs`. Plus a `#check` that pins `invariant_name : GS → Prop` shape to catch a future regression to the closed-`Prop` form.

**Closure-rate impact** (the previous "all green" was the bug):

| Test | Pre-fix (buggy) | Post-fix (sound) |
|---|---|---|
| Phase3DistributedLock | 4/4 — most were tautologies | 2/4 — defaults pass, 2 genuine inductiveness gaps |
| Phase3LockServer | 15/15 — most were tautologies | 2/15 — defaults pass, 13 genuine inductiveness gaps |
| Phase2PingPong | (vacuously true `True`) | **4/4** — genuine SMT discharge |
| All other tests (Phase3PingPong, ObligationShape, PVerifyConditional*, PVerifyManualProof, PVerifyProofRegistry, Phase3DuplicateTarget) | unchanged ✓ | unchanged ✓ |

**M3 status.** Reverted from ☑ to ◐. The `#pverify` infrastructure works — it just now correctly *rejects* obligations that aren't actually inductive. To close the residual M3 obligations, either (a) port the missing invariants from the P sources (DistributedLock has 5 invariants in P; only 3 made it into PLean), or (b) write `@[pverifyProof]` manual proofs that case-split on conditional branches and use the missing assumptions explicitly.

### 2026-06-10 (final) — SMT prep recipe extended: `WithName` strip + `dsimp only` + per-conjunct unfolds → DistributedLock 4/4 + LockServer 15/15

> ⚠ **Superseded by the 2026-06-10 (final-final-fix) entry above.** The
> "DistributedLock 4/4 + LockServer 15/15" closure rate this entry
> reports was an artifact of the soundness bug fixed below: invariants
> were being materialised as closed `Prop`s, so the bundle predicate
> dropped its state argument and obligations reduced to tautologies.
> Post-fix, the honest closure rates are DistributedLock 2/4 +
> LockServer 2/15 (the rest are genuine inductiveness gaps in the
> ports). The mechanical SMT-prep changes this entry describes are
> still in tree and load-bearing; only the closure-rate claim is
> superseded.

The Veil-recipe entry below got `pverify_smt_close` working on default-invariant goals, but the M3 benchmarks still failed at `#pverify`-time on the conditional-bearing `eGrant` / `eAccept` handlers and the `using`-chained safety obligations. Two missing simp steps in `pverify_step_wp` and a re-ordered `pverify_smt_prep` close the gap:

- **`pverify_step_wp` extension** ([`Verify/Tactic.lean`](../PLean/Verify/Tactic.lean)). After Loom's `wpgen + loomLogicSimp/loomWpSimp/loomWPGenRewrite + GlobalState updates`, the goal still carried Loom's `WithName α n = α` wrappers from `if_pos`/`if_neg` branches and `(⨅ (_ : P), Q) s` infimum-over-Prop residuals. Added three `simp only` calls — `[WithName.mk', WithName.erase, typeWithName.erase]`, `[iInf_apply, iInf_Prop_eq, iSup_apply, iSup_Prop_eq]`, `[if_true, if_false]` — that mirror Loom's own `loom_goals_intro` simp set verbatim. After this, the post-`pverify_step_wp` goal contains no Loom-specific machinery.

- **`pverify_smt_prep` re-ordered**. Previous order: `intros → sdestruct_state → simp [pverifySimp]`. New order: `intros → simp [pverifySimp] → sdestruct_state → unfold WithName → dsimp only → unfold DefaultInvariants/UniqueActions/IncreasingCount/ReceivedSubsetSent`. Three load-bearing changes:
  1. `simp [pverifySimp]` runs **before** `sdestruct_state` so `addSent` / `addReceived` / `bumpActionCount` / `updateMachine` expand into record literals while the state still has its struct form. After destruct + `dsimp only at *`, projection-of-mk reduces `{ sent := f, ... }.sent l` to `f l` — exactly the applied-uninterpreted-symbol form lean-auto translates.
  2. `unfold WithName at *` strips any Loom wrappers that survived `pverify_step_wp` (e.g., when `pverify_smt_close` is invoked standalone in a manual proof).
  3. `unfold PLean.DefaultInvariants at *` followed by per-conjunct `unfold PLean.UniqueActions / IncreasingCount / ReceivedSubsetSent at *` (each in its own `try`-wrapped statement so a missing constant doesn't short-circuit the chain). This expands the default-invariant predicates into their `∀ a b, ... → a.actionCount ≠ b.actionCount` bodies, which lean-auto translates as quantified Bool/Nat constraints.

- **Closure-rate impact** (measured at HEAD, full test suite green at 985 jobs):

  | Test | Pre-prep-fix | Post-prep-fix |
  |---|---|---|
  | `Phase3DistributedLock` | 1/4 SMT | **4/4 SMT** ✓ |
  | `Phase3LockServer` | 7/15 SMT | **15/15 SMT** ✓ |
  | `PVerifyConditionalProbeSend` (eGrant + send) | 0/1 | **1/1 SMT** ✓ |
  | All other tests (Phase2PingPong, Phase3PingPong, ObligationShape, etc.) | unchanged | unchanged ✓ |

  Total Phase-3 obligations across 11 `#pverify` calls: **34/34 closed**. Two of the 34 are deliberately user-proved via `@[pverifyProof]` to exercise the manual-proof workflow (`PVerifyManualProof.lean`, `PVerifyProofRegistry.lean`). The benchmark files dropped their `set_option pverify.failOnIncomplete false` workaround.

- **Why this works empirically.** [Velvet](../../../velvet/) gets away without these steps because its monad has no `StateT` (state lives in method arguments — see the 2026-06-10 Velvet-study entry below). PLean's `GlobalState` record DOES carry `sent : Label → Bool` as a function-typed field, so we need [Veil](https://github.com/verse-lab/veil)'s preprocessing recipe (`sdestruct_hyps + simp [smtSimp, funextEq, mk.injEq]`). The 2026-06-10 (mid) entry got the Veil recipe ported as `pverifySimp` + `sdestruct_state` + `pverify_smt_prep`; this final entry adds the missing pieces:
  1. The `mk.injEq`-equivalent step (PLean uses iota-reduction via `dsimp only at *` instead of an injection lemma — same effect, same correctness).
  2. The Loom-specific `WithName` / `iInf_apply` cleanup that Veil doesn't need (Veil's DSL doesn't go through Loom's WPGen).
  3. The per-conjunct unfolds that surface the Bool/Nat content of `DefaultInvariants`.

- **Pinned regression checks.** [`Tests/Semantics/SmtVeilRecipe.lean`](../Tests/Semantics/SmtVeilRecipe.lean) keeps the synthetic `DefaultInvariants`-preservation probes; [`Tests/Surface/PVerifyConditional.lean`](../Tests/Surface/PVerifyConditional.lean) pins the conditional + send case.

### 2026-06-10 (later still) — R-P3.1 / R-P3.2 closed: drop type-ascription on `get`/`set` in accessors and primitives

Building on the Veil-recipe entry below, R-P3.1 (per-accessor `#derive_lifted_wp` / `loomSpec`) and R-P3.2 (per-primitive `loomSpec`) had blocked SMT closure on every benchmark with `var`-bearing handlers. Investigation revealed the actual bug was much simpler than "we need new spec lemmas":

- **The diagnosis** ([`Tests/Surface/WpgenAccessorProbe.lean`](../Tests/Surface/WpgenAccessorProbe.lean) — three side-by-side probes):
  - Probe 1: `(get : PM' GS)` (top-level type ascription) → `wpgen` finds the spec.
  - Probe 2: bare `get` (no ascription) → `wpgen` finds the spec.
  - Probe 3: `← (get : StateT GS DivM GS)` (the previous accessor body's shape) → `wpgen` falls through to `WPGen.default (liftM get)`.

  The internal type ascription `(get : StateT GS DivM GS)` makes the elaborated `liftM get` term have a different `discrTree` key than what `#derive_lifted_wp for (get : StateT GS DivM GS) as PM' GS` registered. Lean's elaborator inserts the right `liftM` either way, but the resulting expression doesn't match the registered spec.

- **The fix** (one-line change per accessor / primitive):
  - [`Commands/GenModule.lean::emitVarAccessors`](../PLean/Commands/GenModule.lean) — replace `let s ← (get : StateT $idGS $idDivM $idGS)` with `let s ← get`. Same for `set`. Per-machine accessors `<v>_get`/`<v>_set` no longer ascribe.
  - [`Semantics/Primitives.lean`](../PLean/Semantics/Primitives.lean) — same fix to `send` / `goto` / `markReceived` (the three primitives whose bodies use `← get` / `set`).

- **The unfold-order fix** ([`Verify/Obligation.lean`](../PLean/Verify/Obligation.lean)):
  - The obligation generator's `proofTacSeq` now unfolds **per-machine accessors first, then PLean primitives** (previously the order was reversed). When primitives are unfolded first, their inlined `liftM get` ends up in a position where the spec lookup misses; unfolding accessors first puts the relevant `liftM get` terms in the outer position where they match cleanly.

- **Closure-rate impact** (measured on the test suite at HEAD):

  | Test | Pre-R-P3.1 | Post-R-P3.1 |
  |---|---|---|
  | Phase2PingPong | 2/4 | **3/4** (+1) |
  | Phase3PingPong (trivial) | 2/2 | 2/2 |
  | Phase3LockServer | 5/15 | **7/15** (+2) |
  | Phase3DistributedLock | 0/4 | **1/4** (+1) |
  | PVerifyConditionalProbe (no-send if-handler) | 0/1 | **1/1** (+1) |
  | PVerifyConditionalProbeSend (if + send) | — | 0/1 (residual; see below) |
  | ObligationShape, DuplicateTarget, ManualProof, ProofRegistry | unchanged | unchanged ✓ |

  All 3396 tests pass.

- **Residual case: conditional + send.** The `PVerifyConditionalProbeSend` benchmark — handler with `if cond then do <updates>; send ... else pure ()` — still doesn't auto-discharge. The standalone `pverify_step_wp` trace shows a clean propositional goal (no `WPGen.default` residue), but invoking the same step from inside `pverify_default` (the auto-discharge ladder) leaves `WPGen.default (liftM get)` on the inner `send`'s `get`. The two invocations differ only in elaboration context (the obligation generator's emitted obligation has a slightly different precondition shape than the standalone `example`), and that difference makes `findSpec`'s discr-tree lookup miss. This is documented in [`Tests/Surface/PVerifyConditionalProbe.lean`](../Tests/Surface/PVerifyConditionalProbe.lean) as a follow-up to investigate.

- **What R-P3.1 originally proposed but turned out unnecessary.** PLAN_P4 R-P3.1/R-P3.2 prescribed emitting *per-accessor* `#derive_lifted_wp` lemmas. The empirical investigation showed this isn't needed — the per-pmodule `#derive_lifted_wp` for the underlying `get`/`set` is sufficient *as long as the accessor body's `← get` is unascribed*. A ~10-line code change replaced what would have been ~50 lines of additional emission per pmodule.

- **Pinned regression check.** [`Tests/Surface/WpgenAccessorProbe.lean`](../Tests/Surface/WpgenAccessorProbe.lean) keeps the three side-by-side probes as a regression marker. If a future Lean / Loom change makes the ascribed form match the spec (or breaks the unascribed form), the probe's `trace_state` output will diverge and the `sorry`-padded examples will start failing in unexpected ways.

### 2026-06-10 (later) — Veil's SMT recipe ported; `GlobalState` shape kept

After the Velvet study (entry below), reading [Veil](https://github.com/verse-lab/veil)'s SMT pipeline showed how to make function-typed record fields go through lean-auto without a refactor:

- **The fix is *preprocessing*, not a different data shape.** Veil keeps the same shape PLean has — `relation sent : Label → Bool`, `function machines : MachineRef → MachineState` — as record fields with function types ([`Veil/DSL/Specification/Syntax.lean:126-145`](https://github.com/verse-lab/veil/blob/main/Veil/DSL/Specification/Syntax.lean)). It runs through the same lean-auto translator PLean uses (default `veil.smt.translator = .leanAuto`).
- **The recipe** ([`Veil/Tactic/Main.lean:175-228`](https://github.com/verse-lab/veil/blob/main/Veil/Tactic/Main.lean)):
  1. `intros` to free the universally-quantified labels/refs.
  2. `sdestruct` on the state struct — replaces the struct hypothesis with its components, so each function-typed field becomes a bare local rather than a struct projection.
  3. `simp [smtSimp]` — Veil's pre-SMT simp set, including `funextEq` (`(f = g) ↔ ∀ x, f x = g x`), `iff_eq_eq`, `tupleEq`/`tupleForall`/`tupleExists`, plus the state struct's `mk.injEq`.
  4. `loom_smt [<all-hypotheses>]` — pass *every* hypothesis, not selectively.

After this preprocessing, function-typed fields appear only in *applied* form (`s.sent lbl`, never `s.sent` as a value). lean-auto translates them as uninterpreted function symbols.

- **Empirical confirmation in PLean.** [`Tests/Semantics/SmtEncodingProbe.lean`](../Tests/Semantics/SmtEncodingProbe.lean) Encoding 1 was the failing baseline (`UniqueActions` after `addSent` rejected with "Higher order input?"). Encoding 5 (same shape, recipe applied) closes via cvc5. [`Tests/Semantics/SmtVeilRecipe.lean`](../Tests/Semantics/SmtVeilRecipe.lean) confirms all three default invariants (`UniqueActions`/`IncreasingCount`/`ReceivedSubsetSent`) close on PLean's actual `GlobalState` type.

- **What landed in PLean** (commit pending; tracked under "Open follow-ups (R15)"):
  - [`Verify/SimpAttrs.lean`](../PLean/Verify/SimpAttrs.lean) (NEW) — registers the `pverifySimp` simp attribute. Lives in its own file because `register_simp_attr` must precede any `@[pverifySimp]` use site (mirrors Veil's `Veil/Base.lean` / `Veil/SMT/Preparation.lean` split).
  - [`Verify/Tactic.lean`](../PLean/Verify/Tactic.lean) gains:
    - `funextEq'` lemma + `funextEq` simproc — ports Veil's [`Veil/SMT/Preparation.lean:19-46`](https://github.com/verse-lab/veil/blob/main/Veil/SMT/Preparation.lean) verbatim.
    - `iff_eq_eq` / `tupleEq` / `tupleForall` / `tupleExists` — ports of Veil's `smtSimp` set.
    - `@[pverifySimp]` tags on the four `GlobalState` update functions plus `inflight`/`sent`/`received`.
    - `sdestruct_state` — destructs every `GlobalState`-typed local in the context (uses `Lean.Meta.whnf` to see through the `GS` abbrev).
    - `pverify_smt_prep` — packages the recipe (`intros` + `sdestruct_state` + `simp [pverifySimp] at *`).
    - `pverify_smt_close` — now runs `pverify_smt_prep` before `loom_smt [*]`.
  - `pverify_default` — SMT moved to the *first* branch (before `default_inv` / `pverify_grind`); it closes most default-invariant obligations directly now.

- **Closure-rate impact** (measured on the test suite at HEAD):
  - Phase2PingPong: `(0 SMT + manual)` → `(2 SMT + 0 user) of 4` (rest are user-invariant goals over an existential).
  - Phase3LockServer: `5/15 → 5/15` (no change — the LockServer obligations that closed before were already in the SMT-friendly subset; the new pipeline didn't unlock additional ones in this benchmark, which is dominated by user-invariant goals over `s.machines m.ref`-shaped state reads).
  - Phase3DistributedLock: `0/4 → 0/4` (conditional handlers leave nested `WPGen.default (liftM get)` residues — see "Limitation" below).

- **Limitation: handlers with conditionals still don't auto-discharge.** [`Tests/Surface/PVerifyConditionalProbe.lean`](../Tests/Surface/PVerifyConditionalProbe.lean) documents this. After `pverify_step_wp`, `wpgen` falls back to `WPGen.default (liftM get)` on every `<v>_get` accessor read, and these opaque residues survive into `pverify_smt_close` where lean-auto rejects them as `Cont (GlobalState … → Prop) is not a ∀`. The fix is the **R-P3.1** follow-up listed in PLAN_P4: emit `#derive_lifted_wp` per-accessor + `loomSpec` lemmas per-primitive so `wpgen` doesn't bail. This is independent of the Veil-recipe work and remains tracked.

- **`#guard_msgs`-pinned shape regression.** [`Tests/Semantics/SmtEncodingProbe.lean`](../Tests/Semantics/SmtEncodingProbe.lean) keeps the *failing* `Label → Bool`-as-record-field test pinned via `#guard_msgs`. If a future Lean / lean-auto / Loom upgrade changes the rejection message (or removes the rejection entirely), the probe fails loud, prompting us to revisit the recipe.

- **Why this is much better than the `List Label` refactor we considered.**
  | Aspect | List refactor | Veil recipe (shipped) |
  |---|---|---|
  | `GlobalState` / `Primitives` / `Predicates` / `Default` changes | Major rewrite | None |
  | PVerifier-faithful encoding | Lost (membership-by-cons, not `[Label]boolean`) | Preserved |
  | Existing 26 files referencing `s.sent` / `s.machines` | Need rewrite | Untouched |
  | `inflight` semantics | Changes (`∈` on List) | Unchanged |
  | M3 closure-rate ceiling without further work | List-membership closes UA/IC/RS but `machines` map needs `find?` lambdas which lean-auto rejects | Default-invariants close; user invariants over `s.machines` still need R-P3.1 |
  | Risk to existing 1009-test green | High | Confirmed green at 3395 tests |

The shipped approach is the strict superset: it reaches the same SMT-closing power as the List refactor for default invariants, *plus* keeps the door open for user invariants that involve `s.machines m.ref` once R-P3.1 lands.

### 2026-06-10 — Velvet's SMT story studied; PLean inherits the *manual fallback* pattern, not the SMT-everywhere pattern
- **Context**: Phase-3 architectural pivot landed `#pverify` as an SMT-discharge command + `@[pverifyProof]` for manual proofs. The question raised was: how does Velvet (the deleted-from-Loom Dafny-like reference, lives at `~/Downloads/velvet/`) get away with SMT discharge on richer state shapes than Cashmere's single `Bal := Int`? Why doesn't Velvet hit the lean-auto "higher-order input" wall PLean hits on `GlobalState.sent : Label → Bool`?
- **Findings** (from reading `~/Downloads/velvet/Velvet/`):
  - **Velvet's monad has no StateT**: `abbrev VelvetM α := NonDetT DivM α` (`Velvet/VelvetTheory.lean:3`). State lives entirely in *method arguments*. The `mut x : T` syntax desugars to: `x` is a regular function argument, every assignment creates a let-shadowed new local, and the method's return type is `VelvetM (originalRet × mutType₁ × mutType₂ × ...)`. Mutated values are returned by value as part of the result tuple.
  - **Consequences for SMT**: Velvet's residual VCs are over plain values (`Array Int`, `Nat`, `List addr`, `SpV Int` = struct of arrays/nats). lean-auto's translation handles these via SMT-LIB's Array/Int/List theories — no higher-order rejection.
  - **Higher-order data IS supported when threaded as arguments**: `mem_alloc (saddr : addr → addr) (block_size : addr → ℕ) (mut next : addr → addr)` (`Velvet/Examples/MemAlloc.lean:35-37`) works because the function-typed arguments stay quantified at the meta-level — they don't get inlined into a record field SMT must encode.
  - **For goals SMT can't close, Velvet falls back to hand-written lemmas**: see `mem_alloc`'s proof (`MemAlloc.lean:449-465`): `prove_correct mem_alloc by loom_solve; apply goal1 <;> assumption; apply goal2 <;> assumption; ...`. The `goal1`–`goal7` are user-supplied helper lemmas closed by `aesop`/`grind`/`induction`.
  - **`@[loomAbstractionSimp]` in Velvet** is for *unfolding user-level definitions* before SMT (so `lrBounds s l r` in a goal becomes `l ≤ r ∧ r < s.size` so SMT sees the body, not an opaque constant). It's not a defunctionalisation pre-pass for higher-order state.
- **Decision**: PLean's current architecture is **correctly aligned with Velvet's pattern** — use SMT where it works, fall back to user-supplied `@[pverifyProof]` lemmas for the rest. The `pverify_smt_close` tactic IS the right tool; the architectural difference is that Velvet's case studies happen to have data shapes lean-auto handles, while PLean's `GlobalState` record (with `sent : Sig.Label → Bool`) triggers rejection. The remediation paths, in priority order:
  1. **Restructure the obligation precondition / postcondition** so `s.sent`-style accesses are abstracted into named hypotheses *before* `pverify_smt_close` runs. The `inflight lbl s` predicate (which expands to `s.sent lbl ∧ ¬ s.received lbl`) is the main offender — lift it into a `have h_inflight : ...` and unfold *only after* SMT. (Untested; tracked as a follow-up.)
  2. **Tag user invariants with `@[pverifyAbstraction]`** (analogue of `@[loomAbstractionSimp]`) and run `simp only [pverifyAbstraction] at *` before `pverify_smt_close` so the *invariant predicates* (`unique_holder`, `no_lock_while_transfer`, etc.) reduce to their bodies — but keep `s.sent`-shaped record accesses opaque to SMT. (Plausible if (1) doesn't suffice.)
  3. **For the irreducible cases**, the user writes a `@[pverifyProof] theorem` using `pverify_step_wp` + `default_inv` / hand-written rcases — the path that already works for `Phase3LockServer.lean`'s 5/15 SMT-proved obligations.
- **Consequences**:
  - PLean's design (atomic tactics + `@[pverifyProof]` + auto-emit obligation skeletons) IS correct.
  - The manual-proof fallback is the load-bearing piece; M3 acceptance for `DistributedLock` / `LockServer` / `RingLeaderVerification` is *expected* to involve some hand-written lemmas, just like Velvet's `MemAlloc` does.
  - `pverify_smt_close` should remain in the auto-discharge ladder — it closes a meaningful slice of obligations (5/15 in LockServer, 1/2 in PVerifyManualProof, etc.) — and its expected failure mode (the lean-auto rejection error) is a clean signal for the user to write a manual proof.
  - The next concrete step toward better SMT coverage is **(1)** — abstracting `inflight` / `sent` / `received` access patterns into a form lean-auto can translate. Best evaluated empirically against the M3 benchmarks before committing.


### 2026-06-09 — Phase-3 session: retain machine body post-`#gen_module`; macro hygiene takeaways
- **Context**: Mid-session work on the M3 acceptance benchmarks. The
  obligation generator needs each machine's `var` declarations to
  build accessor-unfold lists for `pverify`'s proof preamble; under
  the prior `body := #[]` reset that information was unavailable
  after `#gen_module` had run. The session also surfaced two
  recurring macro-hygiene pitfalls.
- **Decisions**:
  - **Retain `m.body` after materialisation**;
    `PMachineDecl.materialised : Bool` becomes the explicit "did
    `#gen_module` run" flag (replacing the body-empty heuristic).
    `Verify/Obligation.lean::synthesise` re-walks the body to
    extract `var` idents. `#pwf` consults the new flag.
  - **Primitives stay as `def`, not `abbrev`**. An experiment
    re-defined `send`/`goto`/`raise`/`announce`/`markReceived` as
    `abbrev` to make `wpgen` "see through" them, but Loom's `wpgen`
    matches on the head constant via `discrTree`, so the definition
    needs to be a `def` (the spec is registered against that constant).
    Reverted to `def`.
  - **Macro hygiene affects simp lemma names**. Inline `simp [<lemma>]`
    inside a `pverify`-family macro defined under `namespace PLean`
    rewrites the bare `<lemma>` reference to a hygienic `<lemma>✝` at
    expansion. Symptom: `simp` makes no progress because the
    hygienic name doesn't resolve. Convention going forward: simp
    sets that need to fire across macro boundaries must be hosted
    inside a named tactic helper (e.g.,
    `pverify_simp_step` / `pverify_simp_post`) so the lemma
    references remain un-hygienic at the use-site.
  - **`obtain ⟨…⟩ := hpre` is greedy across `∃`**: when the pre is
    `(stuff) ∧ ∃ lbl, P`, an outer 5-tuple destructure descends
    into the existential. Two-step destructuring (`have hLemma :=
    hpre.1; obtain ⟨…⟩ := hpre.2`) is more predictable.
- **Consequences**:
  - `#pwf` and `#pverify` no longer see emptied bodies; the
    accessor-unfold path is unblocked, but the WP-stepping ladder
    that uses it still needs work (R-P3.1 / R-P3.2 unchanged).
  - The macro-hygiene rule shows up in PLAN_P3 R15 follow-up: the
    next iteration of `Verify/Tactic.lean` should host every simp
    set inside named tactic helpers, never inline them in the
    obligation generator's `proofTacSeq` quotation.

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
      and dispatches them to SMT** (Phase 3, partial). As of 2026-06-18:
      `Phase3DistributedLock` is **12/12 — fully verified** (11 by SMT, 1
      by a `@[pverifyProof]` manual proof), after a sweep of VC-generator
      fixes (`is_<ev>` characterisation lemmas, default-invariant
      decoupling, dispatcher kind-guard, `markReceived` prologue) plus
      `@[pverifyProof]` type-checking. `Phase3LockServer` is 23/25 — its
      2 residuals are a genuinely incomplete port (the P source's
      strengthening invariants aren't ported). Closing the last M3
      benchmark is a port-completeness task + the same manual technique.
- [ ] **M4 — Tutorial/1_ClientServer ports + verifies in PLean** (Phase 6).
- [ ] **M5 — Tutorial/2_TwoPhaseCommit ports + verifies; v1 cut**.

---

_Last updated 2026-06-18. Phase 3 is in progress. This session: a sweep of
SMT-discharge VC-generator fixes (`is_<ev>` characterisation lemmas,
default-invariant decoupling, `abstract_machine_lookups`, dispatcher
kind-guard, `markReceived` prologue) + `@[pverifyProof]` type-checking +
one hand-written eGrant manual proof took **DistributedLock to 12/12 —
fully verified**. LockServer is **23/25** (genuinely incomplete port —
missing strengthening invariants). All other tests 100%; full suite green
at 3402 jobs. See "Session 2026-06-18" above for per-fix detail and the
follow-ups (auto-emit `<ev>_payload_of_mk`; abstract `MachineRef`). Probe / debug files
(`Tests/Surface/PVerifyDefaultDebug.lean`,
`Tests/Surface/PVerifySmtProbe.lean`,
`Tests/Surface/WpgenAccessorProbe.lean`,
`Tests/Semantics/SmtEncodingProbe.lean`) are intentionally
untracked. Earlier waypoints are in the Decision Log above._

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
- [`PLAN_P4.md`](PLAN_P4.md) — detailed Phase 4 (Spec machines + residual P3)
  plan — `spec X observes [...]` flattening, `assert` obligations,
  send-time spec dispatch (D29–D35); also collects the R-P3.x residue
  (R15, D27, D28-fullcase, D22 SMT fallback) so a P3-then-P4 reader
  sees the full debt
- [`REVIEW_P3.md`](REVIEW_P3.md) — code-review passes against
  PLAN_P3 / STATUS — drives the R-P3.x list in PLAN_P4
- `STATUS.md` (this file) — living tracker
