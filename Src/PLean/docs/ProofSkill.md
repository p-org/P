# PLean — Proof Skill

Practical guide for writing PLean proofs. Captures techniques learned
across the four protocol ports (DistributedLock, LockServer,
RingLeader, ClockBound) — what to do when SMT gets `unknown`, when to
reach for a manual proof, how to spot and add the right inductive
invariant.

Companion: [`AUTOMATION.md`](AUTOMATION.md) for the *what* (tactic
library + discharge pipeline). This doc is the *how* (workflow,
debugging recipes, anti-patterns).

---

## 1. Finding inductive invariants

The first invariant set is **almost never inductive**. The workflow:

### 1.1 Start with the safety property as a `Theorem`

State G1, G2, G3 (or whatever the spec calls for) verbatim. Run
`#pverify`. Expect: base cases close trivially; inductive steps fail.

### 1.2 Read the obligation name

PLean's failure report names the failing obligation by
`<Mod>.<M>.<S>.<ev>_correct_<proofTag>_<invariant>`. The handler
`(M, S, ev)` *and* the failing invariant are right there.

For each failure, ask:
- What does the handler change? (vars, sent set, machine state)
- Why might that change violate the invariant?

A failure on `LC.Waiting.eGlobalResponse → G3` means: the LC's
eGlobalResponse handler doesn't preserve G3. Look at what the handler
does (`currEarlyBound := early`, sends a new `eLocalResponse`) and
think about how that interacts with G3's universal quantifier over
sent eLocalResponse pairs.

### 1.3 Find the counter-example

When the failure is `[SMT: counter-example]` (not `unknown`), the
diag tells you a concrete model. PLean's CEX renderer
([`Verify/CexModel.lean`](../PLean/Verify/CexModel.lean)) decodes it
to a machine table + sent trace. Read it: which event is violating?
What's its payload?

`unknown` is harder — no model, just the solver giving up. Treat it
as "the invariant is probably not inductive, AND/OR the SMT query is
too complex". Try both fixes.

### 1.4 Add strengthening invariants

A strengthening invariant `I` is one that:

- holds initially (or you add an `init-holds` for it),
- is preserved by every handler,
- combined with the user's safety property, makes the inductive step
  go through.

**Standard strengthening shapes** seen across the four ports:

| Shape | Example | When |
|---|---|---|
| **Topology** | `∀ g : GlobalClock, g = global_clock` | Single distinguished machine; need to rule out spurious models with two |
| **Routing** | `e is eAcquire → e targets m → is_Server m → ¬ inflight e` | Specifies which event-type goes to which kind |
| **Machine-state ↔ in-flight set** | `state lc = Idle → ¬ ∃ e in-flight to lc` | Causal-chain invariants needed when a handler should only fire in certain states |
| **State-var monotone** | `e.earliest ≤ lc.currEarlyBound` | A sent payload's field is bounded by a current var because the var only grows |
| **Field-payload match** | `lbl.target = (payload_of lbl).target` | The handler set both label-target and payload-target to the same value; needed when invariants quantify over `Sig.Label` but use payload extractors |
| **Uniqueness** | `∀ e1 e2 in-flight, target(e1) = target(e2) → e1 = e2` | "At most one outstanding per target" — required when LC handlers fire in response |
| **Total-order linking** | `e1 was sent before e2 → e1.trueTime ≤ e2.trueTime` | Time/version monotonicity threading through the protocol |

### 1.5 Iterate: counter-example → invariant → re-verify

When you add invariant `I`, two new obligations appear:
- Base case: `InitConditions s → I s` — usually trivial. Failing here
  means the protocol's init state doesn't satisfy `I`; add an
  `init-holds` line.
- Inductive step: per handler, preserving `I`. This is the new work.

If the new inductive step fails, **don't add another invariant first**.
Look at the new CEX. Often the new failure reveals what `I` should
really say. Edit `I`, don't pile on.

### 1.6 Bundle vs split

When 5+ invariants are mutually inductive (each one's inductive step
needs the others as premises in the pre-state), put them in a single
`Lemma`. The bundled VC is bigger; if SMT times out, split into
multiple `Lemma`s with a `using` chain, but **proceed in dependency
order**.

**Cycle detection** in PLean's `Proof Safety { ... }` block catches
circular `using`-chains at registration time.

### 1.7 When the invariant is correct but SMT can't prove it

If you're CONFIDENT the invariant is inductive but SMT returns
`unknown`, the issue is SMT complexity (see §3) or it really does
need a manual proof (see §2).

---

## 2. Manual proofs

### 2.1 When to reach for manual

Reach for `@[pverifyProof]` when:

- **SMT says `unknown` for a single obligation**, the rest of the
  Proof block closes, and you've already split the bundle / added
  the obvious strengthening invariants.
- **Lean-auto rejects the goal as "Higher order input?"** — this is
  not solvable by waiting; lean-auto's monomorphizer simply can't
  handle the goal shape (typically `(payload_of e).field` under a
  `∀ e : Sig.Label` quantifier with `markReceived` + `goto` post-state).
- The proof requires **case-splits the solver isn't finding** (e.g.,
  "new label is `x` or it's old" splits combined with kind-bridging).

Don't reach for manual when:
- The invariant is genuinely wrong / not inductive — fix the invariant.
- SMT times out (timeout < 10s) — bump it or split the bundle.

### 2.2 Anatomy of a manual proof

The canonical shape (from LockServer/DistributedLock/ClockBound):

```lean
set_option maxHeartbeats 4000000 in
@[pverifyProof]
theorem <FullObligationName> (this : <M>) (param : <ev>_payload) (lbl : Sig.Label) :
    triple (l := PProp Sig)
      (fun s => (<bundle> s ∧ True) ∧ <dispatcher facts>)
      (do PLean.markReceived (P := Sig) lbl; <M>.<S>.<ev>_handler this param)
      (fun _ s => <bundle> s ∧ True) := by
  unfold <M>.<S>.<ev>_handler
  pverify_step_wp
  intro s hpre
  simp only [<bundle>, <each invariant>, ...] at hpre   -- expose conjuncts
  obtain ⟨h1, h2, ..., _⟩ := hpre                       -- name pre invariants
  intro <dispatcher facts> <choose binders>             -- consume goal prefix
  refine ⟨?case1, ?case2, ..., trivial⟩                 -- split goal bundle
  case case1 => ...
  ...
```

Critical naming: the obligation name `<FullObligationName>` must match
what `#pverify` prints (with `_using_<L1>_<L2>_...` suffix). Copy-paste
from the build output's `── manual-proof skeletons ──` section.

### 2.3 Per-conjunct case-split

Inside each `case`, the recurring pattern for invariants quantifying
over `Sig.Label`:

```lean
case <c> =>
  intro <quantifier binders>     -- ∀ lc, ∀ e, hypotheses ...
  obtain ⟨hSent, hRecv⟩ := hInf  -- unpack inflight to s.sent / s.received
  rw [Bool.or_eq_true, Bool.or_eq_true, decide_eq_true_eq, decide_eq_true_eq] at hSent
  rcases hSent with hNew1 | hNew2 | hOld    -- label origin: new1 / new2 / old
  · subst hNew1; simp only [is_<wrong-ev>] at his<ev>   -- new1 fails kind
  · subst hNew2; simp only [is_<wrong-ev>] at his<ev>   -- new2 fails kind
  · -- old label: recover ¬ received, then case-split on lc.ref = this.ref
    rw [Bool.or_eq_false_iff] at hRecv
    obtain ⟨hOldNeLbl, hRecvPre⟩ := hRecv
    by_cases hLc : lc.ref = this.ref
    · -- lc = this: use pre-state uniqueness/exclusion w.r.t. lbl
      ...
    · -- lc ≠ this: state preserved, use pre invariant directly
      pverify_machine_has_type hLcKindPre : <M> lc.ref from hLcKind
      exact <pre-invariant> lc hLcKindPre <args>
```

### 2.4 Existing tactics — reach for these first

Before writing raw Lean tactics, check what's already in
[`Verify/Tactic.lean`](../PLean/Verify/Tactic.lean):

- **`pverify_step_wp`** — runs `wpgen`, peels Loom's `WithName`/`iInf`
  plumbing, leaves a propositional goal over the post-state.
- **`pverify_smt`** — `pverify_smt_prep` (destruct GlobalState locals,
  simp the `@[pverifySimp]` set, abstract machine lookups) + `loom_smt
  [*]`. Cached.
- **`pverify_split_smt`** — splits every top-level `∧` and calls
  `pverify_smt` per conjunct. Use this when the bundle is too big as
  a single shot but each conjunct is tractable.
- **`pverify_machine_has_type <K> on <r>`** /
  **`pverify_machine_has_type <hPre> : <K> <r> from <hPost>`** — the
  kind-bridge primitive. Closing form discharges `is_<K> <r>` directly.
  Bridging form introduces a pre-state `is_<K> r s` from a post-state
  `is_<K> r post`. Use it everywhere you have `lc.ref ≠ this.ref` →
  "machine kind preserved" reasoning.
- **`pverify_not_inflight <hPre>, <hisE>, <isW>`** — routing-clause
  helper: `¬ inflight e s` carries to `¬ inflight e s'` across a
  `send <newEv>` where `<newEv> ≠ <ev>`.
- **`pverify_inflight_by <h> using <x> => <tac>`** — transport
  `inflight e s'` *back* to `inflight e s`, using a user-supplied
  discriminator that rules out the freshly-sent label.
- **`pverify_carry_after_recv <hPre>`** — carries a clause through
  `markReceived lbl` when the clause is recv-monotone.

The general principle: **find the named helper whose Hoare-triple
shape matches your goal**, and pass it the right hypothesis. Avoid
hand-rolling tactic chains that duplicate these.

### 2.5 Lean-level tactics that come up a lot

- **`rw [Bool.or_eq_true]`** / **`Bool.or_eq_false_iff`** — convert
  the post-state's `sent l = decide (l = new) || ... || s.sent l`
  shape into a propositional disjunction.
- **`decide_eq_true_eq`** / **`decide_eq_false_iff_not`** — strip
  `decide` wrappers around `MachineRef`/label equality.
- **`subst h`** — when `hNew : q = <literal new label>`, substitute
  to make the goal use the literal form.
- **`simp only [is_<wrong-ev>]`** — reduce `is_<wrong-ev>
  <literal-of-different-ev>` to `False`, then `at h<isEv>` produces
  the contradiction.
- **`by_cases hLc : lc.ref = this.ref`** — the standard split for
  "this LC vs another LC" in handlers.

### 2.6 Goal inspection

When a tactic fails with an inscrutable error, `trace_state` is your
friend. Insert it before the failing line; the build output shows the
full goal + hypothesis types. Reading that carefully usually reveals:
- whether your `obtain ⟨...⟩` pattern matches the actual conjunct
  count (off-by-one is the #1 mistake),
- whether `simp only` accidentally over-reduced your bundle to
  something it shouldn't,
- whether a hypothesis you expected to be named differently was
  rolled into the `right✝` placeholder.

### 2.7 Naming conventions

Use suggestive names for case tags: `?inq` (`idle_no_gQuery`), `?inr`
(`idle_no_gResp`), `?urp` (`gResp_unique_per_lc`), etc. Three-letter
abbreviations of the invariant name. Makes the proof scannable.

Avoid `case _ =>` (anonymous case) — when you reorder invariants in
the bundle, anonymous cases silently shift. Named cases stay aligned.

---

## 3. Coping with SMT complexity

### 3.1 The "Higher order input?" rejection

Lean-auto's monomorphizer rejects goals it can't translate. Common
shapes:

- **`s.machines (payload_of e).<field>`** under `∀ e`. Handled
  automatically by `abstract_machine_lookups` (inside
  `pverify_smt_prep`).
- **Quantified payload-of extractors** with `markReceived` /
  `goto` post-state. Often unsolvable by SMT; needs manual.
- **Function-typed record fields** (`s.sent : Sig.Label → Bool`)
  used without first destructuring the state. `sdestruct_state`
  inside `pverify_smt_prep` handles this — but only fires on locals
  of declared type `GlobalState P` (not via abbrevs hidden behind
  `whnf`).

If you hit the rejection, **don't** retry with a larger timeout —
lean-auto fails synchronously. Either restructure the invariant, or
go manual.

### 3.2 Reducing higher-order constructs

Tricks that have worked:

- **State predicates that quantify over `Sig.Label` directly** beat
  predicates that quantify over a payload type and then rely on
  kind-guard injection. Reason: the kind-guard injection rewrites
  `e.<field>` to `(payload_of e).<field>` in the user-stated body,
  but **only on a quantifier of payload-type `<ev>`**. A
  `∀ e : Sig.Label, is_<ev> e → P e` body is *not* rewritten — `e`
  is already a label.

  This matters when you want the literal `e.target` (label's target
  field) instead of the payload's target. Compare ClockBound's
  `gResp_label_payload_target_match`:

  ```lean
  -- WRONG: kind-guard injection turns e.target into (payload_of e).target,
  -- making the equation trivially `(payload_of e).target = (payload_of e).target`.
  invariant gResp_label_payload_target_match :
    ∀ e : eGlobalResponse,
      s.sent e = true →
      e.target = (eGlobalResponse_payload_of e).target

  -- RIGHT: Sig.Label binder; e.target stays the label's target field.
  invariant gResp_label_payload_target_match :
    ∀ e : Sig.Label,
      is_eGlobalResponse e → s.sent e = true →
      e.target = (eGlobalResponse_payload_of e).target
  ```

- **State predicates as `s.sent l = true`** beat `inflight l s` when
  you only care about "was ever sent" (not "still in flight"). One
  fewer conjunct in the SMT query, and `s.sent l = true` is a clean
  Bool atom.

- **Avoid `inflight` in the goal** when you can use `s.sent e = true`
  + a separate clause about `s.received e = false`. Splits the
  reasoning.

- **Tag new helper defs with `@[pverifySimp]`** so the
  `pverify_smt_prep` simp pass reduces them before SMT. Without
  this, your helper's body is opaque to lean-auto.

### 3.3 Managing bundle size

Each `Lemma` produces one obligation per handler. If the bundle has
`N` conjuncts, that's an `N`-clause conjunction in the per-handler
VC. Empirically, **5–7 conjuncts is the soft ceiling**; beyond that,
single-shot SMT often returns `unknown`.

Options:

1. **Split the lemma** into multiple `Lemma`s with `using` chains.
   Order them by dependency; cycles are flagged at parse time.
2. **Keep the bundle but use `pverify_split_smt`** in a manual
   proof — splits the bundle into per-conjunct SMT queries.
3. **Promote a sub-property** to a separate `Lemma` and reference
   it via `using`. Sometimes a small auxiliary fact lifts the SMT's
   ability to close the rest.

### 3.4 `using` chain ordering

In a `Proof Safety { prove A; prove B using A; prove C using A, B; }`
block, the proof obligations for each `prove` directive get the
`using`-cited lemmas as pre-state assumptions. The chain must be
acyclic.

Practical workflow:
- Prove "structural" topology lemmas first (`topology`, `causal`).
- Prove "monotonicity" lemmas next (`global_time`, `linking`).
- Prove "derived bounds" (`local_clock_bounds`) using the above.
- Prove user-facing safety theorems (G1/G2/G3) last, using
  everything.

### 3.5 SMT timeout tuning

Defaults: `loom.solver.smt.timeout = 1s`, `retryOnUnknown = true`
(falls back to z3 if cvc5 says `unknown`).

When iterating:
- Set `loom.solver.smt.retryOnUnknown false` so failures surface
  fast — the cvc5→z3 retry doubles every wait.
- Set `loom.solver.smt.timeout 3` for protocols that are mostly
  tractable. Bump to 10–30s for known-hard bundles (LockServer's
  `system_config` etc.).
- For development, `set_option loom.solver "cvc5"` skips solver
  detection.

### 3.6 Caching

PLean caches solver outcomes by `(lctx, goal target)` hash in
`<project>/.lake/build/pverify_cache/`. Warm rebuilds hit ~14×
faster on SMT-heavy files. `lake clean` invalidates.

Don't rely on the cache during invariant iteration — your
hypothesis context changes constantly, missing the cache. The
cache pays off once your invariants stabilize.

### 3.7 Debugging an unknown — diagnostic chain

When `pverify_smt` says `unknown`, in order:

1. **Read the obligation name + the bundle.** Is this the inductive
   step? Which conjunct can't close?
2. **Bump the timeout** to 30s. If it closes, you had a slow query
   and the cache will save you next time.
3. **Try `pverify_split_smt`** in a manual proof: it splits the
   bundle and runs `pverify_smt` per conjunct. If some conjuncts
   close and others don't, you've localized the issue.
4. **Look at the failing conjunct's structure**. Quantifiers over
   `Sig.Label` + `is_<ev>` + `payload_of` + a complex post-state
   are the usual culprit. Restructure the invariant.
5. **Counter-example?** If the solver actually disproves it (not
   just `unknown`), the invariant isn't inductive. Add a
   strengthening invariant.
6. **Manual proof, last resort**. See §2.

---

## 4. Common anti-patterns

### 4.1 Adding paxioms for protocol-level guarantees

Tempting fix for "I can't prove this inductively": declare it as a
`paxiom`. **Resist this** unless the property is genuinely
external to the protocol (an axiom about an opaque function, like
`btw_3` in RingLeader, NOT a runtime invariant).

If the runtime would guarantee it, the invariant should be
*derivable* from the model + other invariants. Adding it as a
`paxiom` lets a bug in the model slip through unverified.

### 4.2 Stating goals on `inflight` when `sent` would do

`inflight l s = s.sent l ∧ ¬ s.received l`. If your safety property
is "any sent label X satisfies P", state it on `s.sent l`, not on
`inflight l`. Avoids the (often spurious) coupling with `received`.

### 4.3 Stating field-existence invariants instead of structural ones

`∀ lc : LocalClock, lc.maxUncertainty > 0` is fine as an
`init-holds`, but as an inductive invariant it has to be reproven
every handler — wasted work. If a field is set once and never
re-written, state the constraint as `init-holds` only.

### 4.4 Manual proofs that re-prove what SMT could close

If a single conjunct closes by SMT under `pverify_split_smt`, don't
hand-write its proof. Leave it to SMT. Manual proofs are for the
conjuncts SMT genuinely can't handle.

### 4.5 Quantifying over machine kinds when you meant `Sig.Label`

```lean
-- WRONG: kind-guard injection makes e.target → (payload_of e).target.
∀ e : eGlobalResponse, sent e → e.target = lbl.target

-- RIGHT: Sig.Label binder preserves label-level field accesses.
∀ e : Sig.Label, is_eGlobalResponse e → sent e → e.target = lbl.target
```

This is **subtle** — both look correct, but they materialize to
different propositions. Use the `Sig.Label` form whenever you want
label-level (`e.target`, `e.action`, `e.actionCount`) field access
preserved.

---

## 5. Worked example — ClockBound's manual proof

The one manual proof in [`Examples/ClockBound.lean`](../Examples/ClockBound.lean)
illustrates most of the above. The obligation:

`LocalClock.Waiting.eGlobalResponse_correct_Safety_causal_using_topology`

closes the 5-conjunct `causal` bundle for the LC's
`eGlobalResponse` handler. The bundle SMT can't close as a single
shot (event quantifiers + `markReceived` + `goto` produce a
goal large enough that cvc5 gives up).

Proof shape (~110 lines):

1. `unfold` + `pverify_step_wp` + `intro s hpre`.
2. `simp only [causal, idle_no_gQuery, ...] at hpre`, then
   `obtain ⟨hINQ, hINR, hURP, hUQP, hQER, _⟩ := hpre` — names each
   pre-invariant.
3. From `topology` (also in `hpre`), `obtain` the
   `gResp_label_payload_target_match` field. Use it with `hAct` /
   `hTgt` to derive `hLblPayTgt : (eGlobalResponse_payload_of
   lbl).target = this.ref` — the key linking fact.
4. `refine ⟨?inq, ?inr, ?urp, ?uqp, ?qer, trivial⟩`.
5. Each case: three-way `rcases` on label origin + `by_cases lc.ref
   = this.ref`. Within each: invoke the matching pre-invariant
   (`hQER` for `inq`, `hURP` for `inr`, etc.), bridging kind via
   `pverify_machine_has_type`.

Worth reading end-to-end as a template. The structure transfers
directly to other "5-conjunct mutual-induction" lemmas.

---

## 6. References

- [`AUTOMATION.md`](AUTOMATION.md) — discharge pipeline, tactic
  catalog, performance features.
- [`Examples/LockServer.lean`](../Examples/LockServer.lean) — 3
  send-handler manual proofs in the routing-clause + kind-bridge
  shape.
- [`Examples/DistributedLock.lean`](../Examples/DistributedLock.lean)
  — 1 manual proof using payload-extractor case splits.
- [`Examples/RingLeader.lean`](../Examples/RingLeader.lean) — 2
  manual proofs using `pinstance`-axiom-based reasoning
  (`btw_1`..`btw_4`).
- [`Examples/ClockBound.lean`](../Examples/ClockBound.lean) — 1
  manual proof for the 5-conjunct mutual-induction bundle.
- [`Verify/Tactic.lean`](../PLean/Verify/Tactic.lean) — the canonical
  source for what tactics exist; read the Hoare-triple-shape doc
  comments before reinventing.
