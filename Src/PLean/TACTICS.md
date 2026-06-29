# PLean — Tactics

Reference for the public `pverify_*` tactics exported from
[`PLean/Verify/Tactic.lean`](PLean/Verify/Tactic.lean). These are
the user-facing primitives that `#pverify M` composes for
auto-discharge, and that `@[pverifyProof]` manual proofs reach for
when SMT alone won't close an obligation.

Companion to [`docs/ProofSkill.md`](docs/ProofSkill.md), which
covers the *workflow* (finding inductive invariants, the
counter-example → invariant iteration, coping with `unknown`). This
document is the *catalogue* (what each tactic does, what arguments
it takes, when to reach for it).

---

## Cheat sheet

| Tactic | Purpose | Use when |
|---|---|---|
| `pverify` | Headline auto-discharge | An obligation needs to close in one shot |
| `pverify_step_wp` | Run `wpgen` + clean up the post-`wpgen` shapes | First step of every manual proof |
| `pverify_smt_prep` | Pre-SMT normalisation (no SMT call) | Diagnosing what `loom_smt` sees |
| `pverify_smt` | Prep + `loom_smt [*]`, cached | Standard SMT discharge |
| `pverify_split_smt` | Split top-level `∧`, SMT each conjunct | A bundle returns `unknown` as one shot |
| `pverify_grind` | `grind` / `omega` / `tauto` fallback | Arithmetic-shaped residuals SMT skips |
| `default_inv` | Discharge a `DefaultInvariants`-headed goal | Auto-emitted default obligations |
| `pverify_default` | `pverify`-style loop-aware default discharge | Default obligations under simple bodies |
| `pverify_carry_after_recv` | Carry an inflight-monotone clause through `markReceived` | Recv-only steps |
| `pverify_not_inflight` | Routing clause `¬ inflight e s'` across a wrong-event `send` | Send-handler routing leg |
| `pverify_not_inflight_by` | `pverify_machine_has_type` + `pverify_not_inflight` composite | Field-only update + wrong-event routing |
| `pverify_inflight_by` | Transport `inflight e s'` back to `inflight e s` | Positive `inflight … → P` clauses |
| `pverify_machine_has_type` | Assert / bridge `is_<K> r` across field-only updates | Kind-bridge step in send/entry handlers |

---

## Auto-discharge

### `pverify`

The headline tactic. Three branches in order: trivial / `True`-shaped
post; post-equals-pre (any handler whose post matches a pre-clause);
full chain — step WP, intros, flatten the precondition's conjunction,
then run the close-chain (`default_inv` → `pverify_smt` →
`pverify_split_smt` → `pverify_grind`).

```lean
@[pverifyProof] theorem … := by pverify
```

If `pverify` fails, copy the failing obligation skeleton from
`#pverify`'s output and switch to the manual-proof pattern (§
"Manual-proof helpers" below).

### `pverify_step_wp`

Runs Loom's `wpgen` macro then strips the post-`wpgen` plumbing:
`WPGen.bind` / `WPGen.pure` shapes, `GlobalState` update terms,
Loom's `WithName` / `iInf` machinery from `if_pos` / `if_neg`
branches. For loop-bearing handlers also reduces
`forWithInvariantLoop`'s `Pi`-typed lattice meet (`Pi.inf_apply`,
`inf_Prop_eq`, …) to a plain `Prop` conjunction.

After this, the goal is a clean propositional VC ready for
`intros`, `obtain`-flattening, and SMT.

---

## SMT discharge

### `pverify_smt_prep`

Pre-SMT normalisation, internal to `pverify_smt`. Calling directly is
rarely useful but helpful for diagnosing translation failures:

1. `simp only [pverifySimp] at *` — the curated simp set
   (`Verify/SimpLemmas.lean`) reduces `GlobalState` updates,
   container lookup-after-mutation, and event-tag predicates.
2. `sdestruct_state` — destructure every `GlobalState`-typed local
   into `gsSent` / `gsReceived` / `gsMachines` / `gsContainers` /
   `gsActionCount` so lean-auto sees uninterpreted symbols, not
   struct projections. Multiple `GlobalState` locals get subscript
   suffixes (`gsSent₁`, `gsSent₂`, …).
3. `abstract_machine_lookups` — generalise
   `s.machines ((<ev>_payload_of e).<field>)` lookups to a fresh
   `MachineState` local. Gated on the argument going through a
   `_payload_of` extractor so it doesn't sever ordinary
   `s.machines n.ref` reads.
4. `destruct_machine_state` — generalise + destructure
   `MachineState`-typed projections when `Fields` carries any
   function-typed component (gating avoids degrading the all-first-
   order case).
5. Unfold `PLean.DefaultInvariants` / `UniqueActions` /
   `IncreasingCount` / `ReceivedSubsetSent`.

### `pverify_smt`

Cache lookup → on miss `pverify_smt_prep; loom_smt [*]` → record
success in the cache. The cache key is over the canonicalised
(`✝`-stripped) pretty-print of every visible local hypothesis's
type plus the goal target — soundness pinned by
`Tests/Verify/CacheSoundness.lean`.

Drops `loom_smt`'s per-obligation "Goal proven by …" info logs —
`#pverify`'s consolidated report subsumes them.

Options:

- `set_option pverify.cache false` — bypass the cache.
- `set_option pverify.profile true` — inline `loom_smt`'s stages
  (prep, lean-auto, solver, assign) with `IO.monoNanosNow` timers;
  `#pverify` emits a summary table on completion.
- `set_option loom.solver "cvc5"` (or `"z3"`) — force a single
  solver, skipping detection.
- `set_option loom.solver.smt.timeout <seconds>` — bump for known-
  hard bundles.
- `set_option loom.solver.smt.retryOnUnknown false` — surface
  failures fast (the cvc5→z3 retry doubles the wait).

### `pverify_split_smt`

Walks the goal target, splits every top-level `∧` into its
conjuncts (filling `True`-typed leaves with `True.intro`), then
calls `pverify_smt` per conjunct. Sound: a proof of `A ∧ B` follows
from independent proofs of `A` and `B`.

Costs *N* solver invocations on an *N*-conjunct goal — wired as a
*fallback* after the whole-bundle `pverify_smt`, so the common
single-shot path is unaffected. The 32-iteration cap on splitting
is a safety bound; real bundles flatten in fewer than 16 levels.

### `pverify_grind`

`grind` / `omega` / `tauto` / `assumption` fallback for arithmetic
or boolean-shape residuals SMT can't translate (e.g. when the goal
involves `GlobalState`'s function-typed fields in a shape that
defeats `funextEq`). Last branch of `pverify`'s close-chain.

---

## `default_inv` and `pverify_default`

### `default_inv`

Discharge a goal whose head is one of the four default-invariant
constants — `DefaultInvariants`, `UniqueActions`, `IncreasingCount`,
`ReceivedSubsetSent`. A `default_inv_guard` head-symbol check
prevents the tactic from mangling unrelated 3-conjunct goals.

The proof shape is mechanical: split the 3-way conjunction, intros
per-conjunct labels, simp `addSent`-shaped post-state reads to a
disjunction, `rcases`-split new vs old, close each leaf via
`solve_by_elim` / `Nat.lt_irrefl` / `grind` / `omega`.

### `pverify_default`

`pverify_step_wp` + a default-specific close-chain (SMT first
because the goal arrives with explicit `DefaultInvariants` content
the SMT path handles directly without needing `default_inv`'s case
split).

For loop-bearing handlers, the auto-emitted `prove default;`
obligation may disprove on a too-weak loop invariant; the manual
escape is `apply triple_pforeach_with (Q := DefaultInvariants);
intro _; unfold PLean.send; pverify` — see the docstring on
`pverify_default` in `Verify/Tactic.lean` for the full walk.

---

## Manual-proof helpers (send-handler clause shapes)

These compose into per-conjunct dispatch for `@[pverifyProof]`
proofs of send-handler obligations. Each names a specific
combination of *which kind of clause* (carry / inflight predicate /
kind guard) and *what the step did* (received, sent, field-only
update).

The shape comments use Hoare-triple notation: `{ Pre } step { Post }`,
where `s` is the pre-state, `s'` the post-state, `lbl` the
dispatched label.

### `pverify_carry_after_recv hPre`

```
{ hPre : P s }   markReceived lbl   ⊢  P s'   (P recv-monotone)
```

Carries an `inflight`-monotone clause through a `markReceived lbl`
step. Such a step grows `received` and leaves everything else
untouched, so any predicate of the form `¬ inflight …` or
`inflight … → P` transfers verbatim.

`hPre` is a *proof of the pre-state form of the goal*, applied to
whatever quantified witnesses the clause introduces. After
intro'ing the binders, supply `<preHyp> x …` as the argument. Both
`¬(A ∧ B)` and `(A ∧ B) → C` surface shapes are normalised via
`not_and` / `and_imp`.

### `pverify_not_inflight hPre, hisE, isWrong`

```
{ hPre : ¬ inflight e s, hisE : is_<ev> e }   send <newEv> ...
⊢  ¬ inflight e s'
```

Closes a routing clause `¬ inflight e s'` across a step that sends
a fresh label with a *different* event tag than `<ev>`. The
freshly-sent label is excluded by event-tag mismatch against the
bound `is_<ev> e` hypothesis; old labels fall through to the
pre-state's `¬ inflight e s`.

Arguments:
- `hPre` — pre-state form, applied to the clause's witnesses.
- `hisE` — in-scope hypothesis `is_<ev> e`.
- `isWrong` — *name* of the `is_<ev'>` predicate
  (`<ev'> ≠ <ev>`) that the freshly-sent label satisfies. Found by
  looking at what the step's `send` emits.

### `pverify_not_inflight_by <K>, hPre, isWrong`

The recurring composite — `pverify_machine_has_type` (kind-bridge)
chained into `pverify_not_inflight`. Equivalent to:

```lean
intro e m hisE hTgt hm
pverify_machine_has_type hmPre : <K> m from hm
pverify_not_inflight (<hPre> e m hisE hTgt hmPre), hisE, <isWrong>
```

Applies when the goal is a routing clause
`∀ e m, is_<ev> e → e targets m → is_<K> m s → ¬ inflight e s` and
the step is a field-only update plus one fresh wrong-event send.

### `pverify_inflight_by hinfe using x => tac`

```
{ hinfe : inflight e s' }   send <newLbl> ...
⊢  inflight e s   (under a user-supplied discriminator)
```

Transport `inflight e s'` *back* to `inflight e s` across a step
that performs one fresh `send`. Companion to
`pverify_not_inflight`: same setup, but for the positive
`inflight e s → P` clause where the antecedent needs discharging.

The new-label exclusion is *not* automatic (the new label and the
clause's `e` may share an event tag), so the caller supplies a
discriminator `tac` that closes `False` from `h : e = <newLbl>`.
Common shapes:

- **`goto`-handler**: the new label's action is `goto _`, not
  `event _`. If a hypothesis says `e.action = .event …`, then
  `rw [h] at hacte; simp at hacte` derives `False`.
- **Forwarding branch**: the proof already case-split on
  `e = <newLbl>` via `by_cases hee : …`; discharge by `exact hee h`.

### `pverify_machine_has_type` — kind-bridge primitive

Two surface forms:

```lean
-- Closing form: discharge a goal of shape `is_<K> r`.
pverify_machine_has_type <K> on <r>

-- Bridging form: introduce `hPre : is_<K> r s` from a post-state
-- `hPost : is_<K> r s'`.
pverify_machine_has_type hPre : <K> <r> from hPost
```

Exploits that the kind triple `is_<K>` / `<K>_allocated` /
`<K>_kind` (emitted by `#gen_module` for every registered machine)
is preserved by any step that updates a machine *field* — the same
triple holds before and after, because `kind`, `currentState`, and
ref-typed fields are not touched.

Arguments:
- `<K>` — *name* of a registered machine kind. The simp-set names
  `is_<K>` / `<K>_allocated` / `<K>_kind` are derived by string
  concatenation.
- `<r>` — the `MachineRef`-typed term. Typically a binder
  introduced by `intro`, or a `<wrap>.ref` projection.
- `hPre` / `hPost` (bridging form) — fresh binder name and the
  source post-state hypothesis.

Required ambient context: a `this` binder is in scope with the
handler's machine wrapper type; the case-split discriminator is
`<r> = this.ref`.

---

## Internal tactics

Tactics callers rarely invoke directly but show up in stack traces:

- `pverify_log_failure_else_sorry` — wraps each auto-emitted
  obligation's tactic chain. On failure stashes the diagnostic in
  `pverifyDiagMap` keyed by obligation name and closes with
  `sorry`; the obligation generator inspects the elaborated value
  for `sorry` to classify the outcome.
- `sdestruct_state`, `abstract_machine_lookups`,
  `destruct_machine_state` — sub-steps of `pverify_smt_prep`. See
  the docstrings in `Verify/Tactic.lean` for their gating
  conditions.
- `split_conjunction_hyps` — walks the local context and
  `obtain`-splits every `A ∧ B` hypothesis. Used by `pverify`
  after `intros` so `solve_by_elim` finds each clause by type.
- `default_inv_guard` — head-symbol guard for `default_inv`.
- `pverify_close_chain`, `pverify_close_chain_smt_first` —
  internal close-chain ordering used by `pverify` and
  `pverify_default`.

---

## Adding a new helper

The catalogue grew empirically from the manual proofs in
`LockServer`, `RingLeader`, `DistributedLock`, `ClockBound`, and
`Consensus`. The rule of thumb: when the same proof shape appears
in 2+ manual proofs, factor it into a tactic and replace the
inlined copies.

Conventions to preserve:

- **One recognisable step per tactic.** Don't bake "prove A, then
  use A to close B" into a single monolithic tactic — implement
  two atomic tactics and (if needed) a `tactic|`-macro that
  sequences them. Otherwise you can't reuse for "prove A but
  close C differently" later.
- **Document the Hoare-triple shape.** Every public helper's
  docstring includes the `{ Pre } step { Post }` form and the
  arguments' provenance ("found by looking at what the step's
  `send` emits"). Without this the catalogue stops being
  callable as a reference.
- **Soundness first.** Before adding a tactic that closes more
  goals, verify it can't close *false* goals. The two pinned
  soundness guards (`GlobalState`-shadowed binders rejected;
  sorried `@[pverifyProof]` must fail) live in
  `Tests/Syntax/SoundnessRegression.lean`.
- **Tag new `GlobalState` update helpers with `@[pverifySimp]`.**
  Without this, SMT prep doesn't reduce them and the obligation
  returns `unknown` on an opaque atom.
