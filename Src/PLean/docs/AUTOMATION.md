# PLean — Verification Automation (current + planned)

How `#pverify` discharges obligations today, the reusable tactics that
keep manual proofs short, and the two performance features we will add
next (parallel SMT, proof caching). Written against the LockServer port
experience (2026-06-19), where three send-handler obligations needed
hand-written proofs whose bulk was pure boilerplate.

Companion docs: [`PLAN_P3.md`](PLAN_P3.md) (verification-declaration
design), [`STATUS.md`](STATUS.md) (per-session change log),
[`PLAN_CEX.md`](PLAN_CEX.md) (counter-example rendering).

---

## 1. How an obligation is discharged today

`#pverify M` emits one Hoare-triple theorem per
`(machine, state, handler, prove-directive)` plus one base-case VC per
invariant ([`Verify/Obligation.lean`](../PLean/Verify/Obligation.lean)).
For each it:

1. consults the `@[pverifyProof]` registry — a user theorem of the
   *exact* obligation type is delegated to by `exact @userThm` (the
   statement is re-checked; a sorried body is rejected — see
   [`STATUS.md` "Session 2026-06-19"](STATUS.md));
2. otherwise emits `theorem … := by pverify` (or `pverify_default`),
   wrapped in `pverify_log_failure_else_sorry` so a solver `sat`/`unknown`
   or translator rejection is logged then `sorry`-ed;
3. classifies the outcome by inspecting the elaborated value for `sorry`
   plus the captured SMT diagnostic.

The closing tactic chain is `pverify_step_wp → intros →
split_conjunction_hyps → (default_inv | pverify_smt_close |
pverify_grind)`. `pverify_smt_close` runs `pverify_smt_prep` (Veil-style
defunctionalisation: `simp [pverifySimp]`, `sdestruct_state`,
`abstract_machine_lookups`, default-invariant unfolds) then `loom_smt [*]`
→ cvc5/z3.

**When SMT is enough.** Most obligations close fully automatically. The
new `<ev>_payload_of_spec` / `_mk` characterisations + the
`@[irreducible]` seal on `<ev>_payload_of` (emitted by `#gen_module`,
2026-06-19) let *send-handler* obligations reach SMT at all.

**When it isn't.** A handler that `send`s a fresh event and must
re-establish a routing invariant `∀ e, is_<ev> e → … (<ev>_payload_of e) …`
produces a quantified goal the solver returns `unknown` on as a single
shot once the invariant bundle is large (LockServer's `system_config` has
11 conjuncts; both cvc5 and z3 time out). Those go to `@[pverifyProof]`.

---

## 2. The manual-proof shape (and why LockServer's proofs are long)

Every send-handler `@[pverifyProof]` in
[`Phase3LockServer.lean`](../Tests/Surface/Phase3LockServer.lean) follows
the same skeleton:

```
pverify_step_wp; intro s hpre
simp only [<bundle>, <each invariant>, is_<M>?, <M>_allocated, <M>_kind, inflight] at hpre ⊢
obtain ⟨h1, …, hN, _⟩ := hpre          -- name each pre-state invariant
intro <dispatcher facts>
refine ⟨?c1, …, ?cN, trivial⟩          -- split the post conjunction
case c1 => …                            -- one tactic block per invariant
```

and each `case` is one of a **small fixed set of shapes**:

- **`pre_transfer`** — clause unaffected by the step; holds verbatim
  from its pre-state hypothesis (`exact hConst`).
- **`received_monotone`** — a `¬inflight`/`inflight → …` clause where
  the step only marks `lbl` received: `received` growing shrinks
  `inflight`, so the pre-state clause transfers.
  (`intro …; have := h e m …; simp [not_and] …`).
- **`field_only_kind`** — a topology/kind clause where the step writes a
  machine *field* only (preserving `kind`/`currentState`/ref fields):
  bridge `is_<M> m post ↔ is_<M> m s` by
  `by_cases m = this.ref <;> simp_all`, then apply the pre-state clause.
- **`new_vs_old_wrong_event`** — a routing clause `∀ e, is_<ev> e → …`
  where the freshly-sent label is a *different* event: case-split
  `decide(e = new) ∨ s.sent e`; the new label fails `is_<ev>`
  (`simp only [is_<ev>] at hisE`), the old falls to the pre-state clause.
- **`new_vs_old_target`** — the load-bearing case: the fresh label's
  target/sender determines the clause. New label ⟹ compute its payload
  via `<ev>_payload_of_mk`/`_spec`, derive the target's kind from
  `const_server`/`unique_server`, contradiction or direct discharge;
  old label ⟹ pre-state clause.

LockServer's three proofs are ~150 lines **only because these five
shapes are re-spelled by hand for each of ~12 conjuncts × 3 handlers**.
The logic per case is 3–8 lines and almost entirely mechanical.

---

## 3. Proposed reusable tactics

Goal: a send-handler `@[pverifyProof]` should be a short driver that
*names* which shape each conjunct takes, not a re-derivation. These live
in [`Verify/Tactic.lean`](../PLean/Verify/Tactic.lean) alongside the
existing `pverify_*` family; the macro-hygiene rule (every simp-lemma
name inside a named tactic) applies.

**Status (2026-06-19, later session):** §3.1, §3.5 and an additional
`pverify_new_ev_split` helper landed and were applied to the
LockServer manual proofs (−114 lines / ~16% file shrinkage, 37/37
closure rate maintained). §3.2 / §3.3 / §3.4 are not yet built —
their highest-leverage application is the kind-bridge step in the
eLock/eRelease then-branches, currently still inline (≈ 4 lines per
clause).

### 3.1 `pverify_pre_transfer` / `pverify_received_monotone`

Close a conjunct that the step preserves directly. Takes the pre-state
hypothesis by name or finds it by `assumption`/`solve_by_elim`:

```lean
-- clause unchanged by the step
macro "pverify_pre_transfer" : tactic =>
  `(tactic| first | assumption | (intros; solve_by_elim))

-- `received[lbl]:=true` only: peel the `decide (e = lbl) || …`
-- and reuse the pre-state clause.
syntax "pverify_received_monotone" term : tactic   -- term = pre hyp
```

These collapse the `aq`/`rel`/`gr`/`ulk`/`nsl`/`nsu` else-branch cases
(currently 4 lines each) to one line.

### 3.2 `pverify_kind_bridge`

The recurring `is_<M> m post ↔ is_<M> m s` step for a field-only machine
update. An `elab` tactic that, given the post-state `is_<M> m`
hypothesis, rewrites it to the pre-state form by case-splitting on
`m = this.ref` and discharging via `simp_all` over
`<M>_allocated`/`<M>_kind`. Eliminates the `have hmPre : is_<M> m s := by
… by_cases … <;> simp_all` block repeated in every `field_only_kind`
case.

### 3.3 `pverify_new_label_split`

The new-vs-old core. Expands a goal/hypothesis over
`decide (e = <newLabel>) || s.sent e` into two named subgoals
(`case new`, `case old`), and in `case old` automatically weakens the
post-state `received`/`is_<M>` back to pre-state. The driver then only
fills `case new` (and often `case old` closes by `pverify_pre_transfer`).

### 3.4 `pverify_send_handler` (the driver)

A `macro` that runs the fixed prologue
(`pverify_step_wp; intro s hpre; simp only […] at hpre ⊢; obtain …;
intro …; refine ⟨…⟩`) and then dispatches each resulting conjunct
through `first | pverify_pre_transfer | pverify_received_monotone _ |
(pverify_kind_bridge; …) | (pverify_new_label_split <;> …)`. The author
supplies only the genuinely-hard `new_vs_old_target` case(s) by name.

**Target:** LockServer's three proofs drop from ~150 lines to ~15–25,
and most future Tutorial ports need *no* manual send-handler proof at
all (the driver's automatic cases cover everything but the one
target-determining clause).

### 3.5 Lower-effort win — split-then-SMT inside `pverify`

Before building 3.1–3.4, try the cheapest lever: have `pverify` **split
the post conjunction and call `pverify_smt_close` per conjunct** rather
than once on the whole bundle. Probe evidence (2026-06-19): the
11-invariant bundle returns `unknown` as one query, but the same
conjuncts each closed individually in earlier probes. Per-conjunct
splitting is sound (proving `A ∧ B` by proving `A`, `B`) and may close
some currently-manual obligations with no new tactics — at the cost of N
solver calls (see §4, parallel SMT, which makes this cheap). Gate it
behind the `first | (whole-bundle SMT) | (split; all_goals SMT)` so the
common single-shot case is unaffected.

---

## 4. Planned: parallel SMT calls

**Problem.** `#pverify` discharges obligations sequentially, one
`loom_smt` (hence one cvc5/z3 process) at a time. LockServer emits 37
obligations; a clean run is dominated by serial solver latency, and the
30 s-timeout obligations stack up. Per-conjunct splitting (§3.5) would
multiply the call count.

**Plan.** Run independent solver queries concurrently. The obligations
within a `synthesise` run are independent (each is its own
`theorem`), and so are the conjuncts within a split goal, so this is
embarrassingly parallel.

**Insertion points.**
- `Verify/Obligation.lean::synthesise` drives the per-obligation loop —
  the unit of parallelism. Today it `elabCommand`s each obligation
  inline; a parallel design elaborates each obligation's *goal* to an SMT
  query string up front, dispatches the solver processes concurrently
  (bounded pool, ~cores−2), then assigns results back.
- `Loom/SMT.lean::querySolver` already shells out to a child `cvc5`/`z3`
  process (`createSolver` → `IO.Process`); it is the natural concurrency
  primitive. A `querySolverMany : Array String → MetaM (Array SmtResult)`
  that spawns and `IO.wait`s a batch is the smallest addition.
- The `retryOnUnknown` cross-solver retry (`querySolver`, default on)
  becomes "race cvc5 and z3, take the first `unsat`" rather than
  sequential fallback — strictly faster on the `unknown`-prone
  send-handler queries.

**Caveats.** `loom_smt` closes the goal with the `trust_smt` axiom only
*after* the solver says `unsat`; the parallel layer must preserve that
(produce the query strings purely, run solvers off the elaboration
thread, then close each goal on the main thread with its verified
result). Determinism of the report ordering must be kept (sort results
by obligation name before printing).

---

## 5. Proof caching — landed second attempt (2026-06-19)

**Status.** First design (cmdString-keyed) was reverted same-session
because key calculation cost as much as the solver call. Second design
keys on the **(local context, goal target)** pair before
`pverify_smt_prep`, so a hit skips the prep simp set, the lean-auto
translation, AND the solver. Measured wall-clock: 11–14% cold-vs-warm
reduction on the M3 benchmarks. Both versions documented below.

**Problem.** Re-running `#pverify` after an *unrelated* edit (a comment,
a different machine, a doc change) re-invokes the solver on every
obligation even when its SMT query is byte-identical to last time. On
LockServer/DistributedLock that is minutes of redundant cvc5/z3 work per
build.

**Plan.** Cache by the *content of the SMT query*, not by obligation
name. After `pverify_smt_prep` + `prepareLeanAutoQuery`
([`Loom/SMT.lean:63`](../.lake/packages/Loom/Loom/SMT.lean)) produce the
final SMT-LIB string for a goal, hash it; if the hash is in the cache
with an `unsat` verdict, close the goal via `trust_smt` immediately
without spawning a solver.

**Why hash the query, not the obligation.** The obligation theorem name
is stable across edits, but its *meaning* is not — change an invariant
body and the name stays the same while the goal changes. Conversely, an
unrelated edit leaves the query identical. Keying on the normalised query
string (post-`prepareLeanAutoQuery`, so it already abstracts goal-local
gensyms) makes the cache both sound (same query ⇒ same verdict, since
`trust_smt` trusts exactly that query's `unsat`) and maximally re-usable.

**Insertion points.**
- Hash at the boundary in `Loom/SMT.lean::elabLoomSmt` (or a PLean
  wrapper around it): `cmdString ← prepareLeanAutoQuery …; if let some
  .unsat := cache[hash cmdString] then close-by-trust_smt else
  querySolver …; cache[hash] := result`.
- Persist the cache in the **session/build directory** (alongside Loom's
  solver download dir) keyed by `(solver, solver-version, timeout, query
  hash)` — solver version and timeout are part of the verdict's validity.
  A Lake-aware location invalidates on toolchain/solver bumps for free.
- Store only `unsat` verdicts (the only ones that close a goal).
  `sat`/`unknown` should NOT be cached as failures — they depend on
  timeout/seed and a longer run or the other solver may succeed; caching
  them would make a transient `unknown` sticky.

**Soundness.** The cache never proves anything the solver didn't: a hit
is exactly "this identical query was already certified `unsat` by this
solver version at ≥ this timeout." It only skips re-derivation. Pair it
with a `pverify.cache false` escape hatch (mirrors
`pverify.failOnIncomplete`) for paranoia / cache-poisoning debugging, and
a `--no-cache`-style full re-verify for CI gates that must not trust a
stored verdict.

**Interaction with §4.** Caching runs *before* dispatch: check the cache,
then send only the misses to the parallel solver pool. The two features
compose — caching cuts the work set, parallelism speeds up what remains.

**Post-mortem of the 2026-06-19 attempt.** Two issues, both load-bearing:

1. *The cache key calculation is the work we're trying to avoid.*
   `loom_smt`'s wall-clock cost is dominated by `prepareLeanAutoQuery`
   (the lean-auto translation) rather than the cvc5/z3 invocation —
   for the M3 benchmarks, the solver returns `unsat` in <100ms but
   the translation costs ~400ms. Caching the **query string** still
   requires running `prepareLeanAutoQuery` to compute the key, so a
   cache hit saves only the ~100ms solver time, not the ~400ms
   translation. Cold/warm timings on `Tests.Surface.Phase3DistributedLock`
   were 15.7s and 16.2s respectively (the cache adds ~0.5s of overhead
   per call from the duplicate `prepareLeanAutoQuery`).
2. *The query string is not stable across elaborations.* lean-auto's
   monomorphiser delab's local hypotheses to fresh-gensym names; the
   gensym counter resets per elaboration but the **order of declared
   constants** can shift across runs (e.g. when an unrelated `def`
   earlier in the file changes ctor index). On `Phase3DistributedLock`,
   13 of 20 obligations had stable cmdStrings (cache hits on re-elab);
   the other 7 produced different bytes each time and never cached.
   The cache only papered over the half-deterministic cases.

A useful cache would key on the **Lean goal Expr** (post-`pverify_smt_prep`)
with gensym names normalised, and skip both `prepareLeanAutoQuery`
and the solver on a hit.

### 5b. Second attempt — Expr-level keying (landed, 11–14% wall-clock win)

Keys on the canonicalised pretty-printed `(local context, goal target)`
pair at the entry of `pverify_smt_close`, **before** `pverify_smt_prep`
runs. On a hit, the goal is closed by `Loom.SMT.trust_smt` directly,
bypassing the prep simp set, the lean-auto translation, and the
solver call. Mirrors PVerifier's
`PCompiler/.../Uclid5CodeGenerator.cs::PVerifierCache` design (which
checksums the generated UCLID source) but at the Lean-`Expr` level.

**Why this design beats the cmdString variant.**
- The hash is computed via raw `Expr.toString` (a fraction of a ms per
  obligation, see §5c warm-path table), not `prepareLeanAutoQuery`
  (hundreds of ms). Hits genuinely skip work.
- `Expr.toString` is stable across elaborations on the same normalised
  `Expr` (`instantiateMVars` + `Expr.consumeMData` upfront). Goals
  that the cmdString variant failed to cache (7/20 of DistLock's
  obligations) are stable here. (Initial version used `Lean.Meta.ppExpr`;
  swapped to `Expr.toString` 2026-06-19 for 48× speedup on cache.pp.)

**Hash composition (sound).** The hash includes every visible local
hypothesis's `userName + ppType` plus the goal target's `ppExpr`,
joined with `\n` separators. Including hypotheses is required for
soundness: `trust_smt` only proves the target type, so the cache hit
is only valid if the hypotheses-in-context entail the target. A
target-only hash would let an unrelated hypothesis context close the
same target — wrong. [`Tests/Verify/CacheSoundness.lean`](../Tests/Verify/CacheSoundness.lean)
pins three goals where the target is the same but the hypothesis
contexts differ, confirming three distinct hashes.

**Persistence.** One file per entry, named `<hash>.ok`, body is the
canonical text (for `cat`-debuggability). Directory:
`<project>/.lake/build/pverify_cache/`. `lake clean` invalidates.

**Measured.**
- `Phase3DistributedLock`: cold 15.8s → warm 14.1s. 20 entries cached.
- `Phase3LockServer`: cold 54.0s → warm 46.5s. 34 entries cached.

The speedup is bounded by the fraction of wall-clock spent in
`pverify_smt_prep + lean-auto + solver` (which is what the hit skips).
For SMT-light tests it's a wash; for SMT-heavy tests (LockServer's
big bundles) it's worth more.

**Future improvements.** Larger gains require:
- Parallel SMT (§4) — cuts the misses' wall-clock.
- A finer `Expr`-level canonicaliser that ignores irrelevant
  re-ordering of hypotheses (today, swapping two `intro` order
  invalidates the cache, even when the goal is logically identical).
- A goal-fingerprint that strips known-irrelevant metadata (binder
  names that don't appear in the body, `_` underscores). Would
  increase the hit rate on cosmetic edits.

---

## 5c. Where does the cold-path time actually go? — profiled 2026-06-19

`Verify/Profile.lean` adds per-stage `IO.monoNanosNow` instrumentation
behind `set_option pverify.profile true`. The instrumented branch
inlines `loom_smt [*]` into PLean so each segment can be timed
separately (it's NOT bit-identical to the upstream `loom_smt` macro —
no `Goal proven by …` info log, no `retryOnUnknown` cross-solver
fallback by default — hence opt-in).

The probe harness is [`Tests/Verify/ProfileProbe.lean`](../Tests/Verify/ProfileProbe.lean):
a synthetic 12-obligation pmodule modelled on `Phase3DistributedLock`.

**Cold-path breakdown (cache OFF, all 12 obligations go through solver):**

| Stage | Total | Per-obligation | % |
|---|---|---|---|
| smt.prep (defunctionalisation simp chain) | 252 ms | 21 ms | 15% |
| **smt.auto (lean-auto translation)** | **941 ms** | **78 ms** | **57%** |
| **smt.solver (cvc5 process)** | **464 ms** | **39 ms** | **28%** |
| smt.assign (`trust_smt` term build) | 0.3 ms | 0.02 ms | <1% |
| **Total** | **1657 ms** | **138 ms** | |

**Headline finding.** lean-auto's `prepareLeanAutoQuery` (monomorphisation
+ lam→SMT-LIB serialisation) costs **~2× the solver itself**. The
solver is fast (~40 ms / obligation on these small bundles). This
flips intuition: optimising the solver call is a small lever; reducing
lean-auto cost (or skipping it entirely via a cache hit) is the big
one. This is the empirical justification for the goal-Expr cache (§5b)
covering both stages — caching only the solver result would skip <30%
of the cost.

**Warm-path breakdown (cache ON, all 12 obligations hit):**

| Stage | Total | % |
|---|---|---|
| cache.pp (pretty-print for cache key) | 191 ms | **99%** |
| cache.hash | 0.01 ms | <1% |
| cache.fs (file-existence check) | 1.2 ms | 1% |
| smt.assign | 0.15 ms | <1% |
| **Total** | **192 ms** | |

The warm-path was essentially pure `Lean.Meta.ppExpr` of the goal +
local context. Net cold→warm speedup: 9.6× (then).

**Optimisation landed 2026-06-19: raw `Expr.toString` cache key.**
The cache key now uses Lean's constructor-shape printer (after
`instantiateMVars` + `Expr.consumeMData`) instead of `Lean.Meta.ppExpr`'s
delaboration pass. `Expr.toString` produces a string that uniquely
identifies the normalised `Expr` and is deterministic across runs —
without the macro-scope `✝` drift that `Expr.hash`-based proposals
would have to canonicalise.

Updated warm-path numbers (12 obligations):

| Stage | Time | % |
|---|---|---|
| **cache.pp** (raw Expr.toString) | **4 ms** | **78%** |
| cache.fs (file-existence check) | 1 ms | 18% |
| smt.assign | 0.15 ms | 3% |
| **Total** | **5 ms** | |

**48× speedup on cache.pp, 38× speedup on warm-path total.** Cold/warm
ratio is now ~340× on DistLock (cold 1655 ms / warm 5 ms).

The change is in [`PLean/Verify/Tactic.lean::pverifyGoalToCacheText`](../PLean/Verify/Tactic.lean) —
~5 line edit. Cache file format is unchanged (entries written under
old ppExpr hashes are simply invalidated; `lake clean` clears them or
they're regenerated on first re-run). 12/12 DistributedLock + 33/34
LockServer SMT obligations cache-hit across re-runs.

---

## 6. Priority order

1. **§3.5 split-then-SMT** — ✅ landed 2026-06-19. Wired into
   `pverify_close_chain` after single-shot SMT. Did not change the M3
   pass/fail mix on the current bundles (which all close as one shot or
   need a manual proof for a non-SMT reason), but is in tree as a
   no-cost fallback for future ports.
2. **§3.1 reusable manual tactics** — ✅ landed 2026-06-19. Three
   helpers (`pverify_unchanged`, `pverify_recv_only`,
   `pverify_new_ev_split`) shrunk LockServer's three manual proofs
   by ~114 lines, with the 37/37 closure rate maintained.
3. **§5 proof cache** — ✅ landed 2026-06-19 (second design). Keys on
   the `(local context, goal target)` Expr pretty-print rather than
   the lean-auto query string, so a hit skips lean-auto and the
   solver. 11–14% wall-clock reduction on the M3 benchmarks. First
   cmdString-keyed attempt reverted same session; see §5 for the
   full post-mortem.
4. **§4 parallel SMT** — largest speedup on a cold run; more invasive
   (touches `synthesise`'s control flow and process management). Not yet
   landed.
5. **§3.2–3.4 remaining helpers** — `pverify_kind_bridge` (field-only
   update bridge) and the `pverify_send_handler` driver. Next port that
   exercises these (RingLeaderVerification, or one of the heavier
   benchmarks) is the right time to extract them.
