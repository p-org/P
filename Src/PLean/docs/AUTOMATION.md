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

## 5. Planned: proof caching

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

---

## 6. Priority order

1. **§3.5 split-then-SMT** — smallest change, may auto-close obligations
   that are manual today; do first to measure how many manual proofs §3
   actually needs to eliminate.
2. **§5 proof cache** — biggest wall-clock win on iterative builds, fully
   local to the SMT boundary, sound by construction.
3. **§4 parallel SMT** — largest speedup on a cold run; more invasive
   (touches `synthesise`'s control flow and process management).
4. **§3.1–3.4 reusable tactics** — quality-of-life for the manual proofs
   that remain after §3.5; reduces the LockServer-style boilerplate.
