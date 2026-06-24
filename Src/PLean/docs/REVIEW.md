# PLean Code Review — Soundness & Code Style

Scope: the PLean library (`PLean/**`, ~7.2k LOC), the four protocol
benchmarks under `Examples/**`, and the regression tests under `Tests/**`.
Reviewed at HEAD on 2026-06-24 (122/122 obligations closing, suite green).

Two foci, in order of importance:

1. **Soundness** — whether `#pverify` can falsely declare a P program safe.
   This is the *one* property PLean is delivering, and it dominates everything
   else.
2. **Code style** — module decomposition, comment hygiene, and structural
   duplication that increases the risk of soundness regressions when the
   verifier is extended.

A correctness-bug list and a milestones list (the previous review's I/IV
sections) are out of scope of this revision; the previous review's most
important items have either landed (I.1 `default_inv_guard`, I.2 `pnew`
kind, I.3 `goto` payload, I.7 dead `DispatcherContract.lean`, I.8 deep
`GlobalState` walk, I.12 stale `PWf.lean` comment) or are addressed below.

---

## I. Soundness

The verifier ships **two** trusted layers and **one** unchecked extension
mechanism. Soundness rests on:

- **(T1)** the `Loom.SMT.trust_smt` axiom and its in-tree counterpart on
  cache hits — i.e., that every `unsat` we accept is a *real* `unsat` and
  that every `define-fun` translation accurately reflects the goal;
- **(T2)** the `@[pverifyProof]` lookup correctly type-checks each manual
  proof against the obligation the generator *would* have emitted;
- **(E)** every user-facing `paxiom` / `pinstance` / `function` (foreign)
  is the user's responsibility — adding an inconsistent axiom makes any
  invariant trivially provable.

What's *not* in the trust base, and what would constitute a soundness bug:

- **(VC-completeness)** the obligation generator must emit a Hoare-triple
  obligation for *every* code path that can change the global state — every
  handler, every base case, every primitive footprint. A handler whose
  obligation is silently skipped is unsound.
- **(VC-shape)** the per-handler triple's pre and post must accurately
  characterise the dispatcher contract (what facts are guaranteed about
  the in-flight label) and the post-state predicate to preserve. A pre that
  is too strong, or a post that is too weak, lets a real violation slip
  through.
- **(handler binding)** the obligation must run `markReceived lbl >>= handler`
  — handler bodies must not see the consumed event still in `inflight`.
- **(quantifier well-formedness)** machine / event quantifiers over a wrapper
  struct must be coupled to runtime kind tags; otherwise the solver fabricates
  values whose `kind` doesn't match their `currentState`.
- **(predicate stability)** invariant materialisation must not introduce a
  binder that shadows the per-handler state.
- **(SMT prep)** the `pverify_smt_prep` rewrite chain (lean-auto preparation)
  must not turn a *valid* (= `unsat`-of-negation) obligation into an
  *invalid* one nor weaken a hypothesis to make the obligation trivially
  follow — sound rewrites only.
- **(escape hatches)** registered manual proofs must match the obligation
  shape exactly, and `sorry`-backed proofs must not appear as passes.

This section walks the codebase against each of these.

### I.1 VC completeness — every handler gets an obligation

**Coverage map.** [`Verify/Obligation.lean::synthesise`](../PLean/Verify/Obligation.lean#L896)
walks every `Proof { prove X using Y; }` directive and for each emits

1. one **base-case VC** per individual invariant in `X`'s bundle
   (`InitConditions s → i s`), and
2. one **inductive-step VC** per `(machine, state, on-handler event)` triple
   in the *whole* pmodule.

After the user-directive pass, a **`block_auto_default` synthetic pass**
([`:984-1000`](../PLean/Verify/Obligation.lean#L984-L1000)) emits a `prove
default;` obligation for every `(M, S, ev)` *not* already covered, so the
three default invariants (`UniqueActions` / `IncreasingCount` /
`ReceivedSubsetSent`) are checked on every reachable handler regardless of
whether the user remembered to write `prove default`.

This coverage is sound for the handler set the generator actually iterates
— but the iteration has three gaps worth flagging.

#### S.1.1 — `entry { … }` handler bodies are NOT verified  (latent soundness hole)

`PStateDecl.handles` ([`Surface/Machine.lean::collectStateMetadata:128-129`](../PLean/Surface/Machine.lean#L128-L129))
records only events listed in `on <ev> …` clauses; `entry { … }` blocks
are pattern-matched but the entry events (if any) are silently skipped:

```lean
| `(pStateBodyItem| entry { $_:doSeq }) => pure ()
| `(pStateBodyItem| entry ( $_:ident : $_:term ) { $_:doSeq }) => pure ()
```

The synthesise loop walks `sd.handles`
([`Verify/Obligation.lean:962`](../PLean/Verify/Obligation.lean#L962)),
so an entry handler that mutates state (writes a `var`, calls `send`,
calls `goto`) is materialised as a Lean def but *no Hoare-triple obligation
is generated for it*. A user could write

```lean
machine Bad {
  var x : Nat
  start state Init {
    entry { x = 42 }     -- breaks the invariant `x = 0`
  }
}
Theorem broken { invariant : ∀ b : Bad, b.x = 0 }
Proof { prove broken; }
```

and `#pverify` would report 0 failures (the base case would still fire,
but the entry-handler step would not). The Tutorial port [`RingLeader`](../Examples/RingLeader.lean)
exercises `entry { send … }` in `Server.Proposing` — its safety is currently
established via the `Lemma using` chain over `eNominate`, which closes
*despite* the entry handler being unchecked, because `eNominate` re-establishes
the invariants. This is a latent miss, not yet an exploited soundness bug,
but it lets a buggy `entry { … }` slip through silently.

**Fix path.** Either (a) thread synthetic event names (`__entry_<S>`) into
`sd.handles` at registration so `synthesise` picks them up uniformly, or
(b) add a second emission loop in `synthesise` that walks the body Syntax
for `pStateEntry` / `pStateEntryTyped` items and emits one obligation per
entry handler. PVerifier's path is (b) — it emits one `InEntry` flag and
an `entry`-procedure per machine. Match that.

#### S.1.2 — `InEntry` / `stage` is not modelled  (acknowledged completeness gap)

PVerifier tracks a per-machine `InEntry`/`InStart` pair and asserts at
init that every machine starts in entry. PLean has the `stage : Bool`
field on `MachineState` ([`Semantics/Label.lean:85`](../PLean/Semantics/Label.lean#L85))
but no obligation references it, and `markReceived` doesn't flip it.
[`Commands/GenModule.lean:703-705`](../PLean/Commands/GenModule.lean#L703-L705)
explicitly notes this:

```
(`InStart` / `InEntry` for every `MachineRef` is a PVerifier convention
that PLean does not yet model; this can be added when initialization-
action support lands.)
```

Combined with S.1.1, the `stage` flag is currently a stale field. This
is sound *as long as* no invariant references it; user code that does
reference `.stage` would get a model where the solver picks an unconstrained
value. The cleanest fix is to remove the field until entry handlers are
modelled (or model both together).

#### S.1.3 — `on ev goto tgt` handlers skip both the user obligation AND the auto-default

[`Verify/Obligation.lean:963`](../PLean/Verify/Obligation.lean#L963) and
`:993` skip events whose state body uses `on ev goto tgt`:

```lean
if gotoHandlers.contains (sd.name, ev) then continue
```

The skip is necessary — `#gen_module` doesn't emit a `_handler` def for
those clauses, so referencing one would be an `Unknown constant` error.
But this means a `goto`-only handler that violates an invariant on the
state transition (a `goto` *does* mutate state — `currentState`, `stage`,
`actionCount`, `sent`) is **not verified**. The transition's safety should
follow from `markReceived lbl >>= goto …`, and the `goto` primitive's
semantics are well-defined; the generator just doesn't emit the triple.

**Fix path.** Synthesise an inline handler body `do markReceived lbl;
goto <tgt>` at obligation-emit time for `on ev goto tgt` clauses, rather
than skipping the (state, event) pair entirely. The fix is small and
soundness-load-bearing.

#### S.1.4 — Spec machine handlers are skipped wholesale

[`Verify/Obligation.lean:956-958`](../PLean/Verify/Obligation.lean#L956-L958):

```lean
if m.isSpec then
  logInfo m!"spec machine `{mname}` skipped — spec obligations are not yet supported"
  continue
```

This is the documented Phase-4 gap. It's harmless **until** Phase 4 lands
spec handlers; if it lingers after Phase 4, a spec-violating program would
verify cleanly. Add a `failOnSpec` option that errors instead of
`logInfo`-ing once spec machines have a real story, so the message-log
notice doesn't get drowned.

### I.2 VC shape — pre, post, and the dispatcher contract

The per-handler obligation has the shape
([`Verify/Obligation.lean::emitOneObligation:253-260`](../PLean/Verify/Obligation.lean#L253-L260)):

```
triple
  (fun s => bundle s ∧ ⋯using⋯ s ∧ DispatcherContract this lbl param s)
  (markReceived lbl >>= handler this [param])
  (fun _ s => bundle s)
```

The dispatcher contract is the conjunction of:

```
inflight lbl s ∧ lbl.target = this.ref ∧
is_<M> this.ref s ∧ (s.machines this.ref).currentState = <S>_st ∧
lbl.action = .event (E.<ev> [param])
```

Each conjunct is load-bearing for soundness:

- `inflight lbl s` — ensures the handler isn't running on a fresh / arbitrary
  label.
- `lbl.target = this.ref` — ties the label to the running machine.
- `is_<M> this.ref s` — pins `this`'s kind. Without this, the solver could
  fabricate a `this` with a `currentState` of the right state but a `kind`
  field of some other machine, since `MachineState` flattens `kind` and
  `currentState` independently. This was the fix landed 2026-06-18 (commit
  `9a8645a2d`).
- `(s.machines this.ref).currentState = <S>_st` — pins the control state.
- `lbl.action = .event (E.<ev> param)` — pins the dispatched event tag.

The shape is correct. Three observations:

#### S.2.1 — `DefaultInvariants` is intentionally absent from user-invariant obligations

[`:217-222`](../PLean/Verify/Obligation.lean#L217-L222):

> "`DefaultInvariants` (UA/IC/RS) is a well-formedness bundle, proven once
> by `prove default` obligations and not assumed in user-invariant
> obligations."

This decoupling is *sound* — strictly fewer hypotheses make the obligation
strictly harder, never easier. It is the right call (PVerifier does the
same). The risk is *completeness*: a user invariant that genuinely needs
`a.actionCount < b.actionCount` (a fact only `IncreasingCount` provides)
cannot reach for it without writing a redundant `using` clause — except
that `default` cannot currently appear as a `using` target ([`Surface/Verify.lean:283-285`](../PLean/Surface/Verify.lean#L283-L285)
rejects unknown lemma names, and `default` isn't a lemma). Either:

- allow `prove X using default;` syntactically and inject the three
  defaults into the `using` chain, or
- accept that benchmarks needing `IncreasingCount` must re-state it as a
  `Lemma`.

Worth deciding before more ports go in.

#### S.2.2 — Post checks only the target bundle, not the dispatcher contract

The post is `fun _ s => bundle s` — the dispatcher contract is **not**
reasserted on the post-state. This is correct: a handler that consumes a
label cannot be required to keep that label in-flight in the post. But it
means the contract is *only* a pre-state hypothesis; if a future change
moves any of the contract conjuncts into the post (e.g., "`is_<M> this.ref`
is preserved"), the soundness argument changes. Today this is fine.

#### S.2.3 — `markReceived` does not bump `stage`

`markReceived` only updates the `received` set. PVerifier's runtime also
flips the machine's `stage` to false (indicating the entry handler has
run). PLean's coupling here is consistent — `stage` is unused throughout —
but see S.1.2: this is a co-symptom of the entry-handler gap.

### I.3 Quantifier well-formedness — kind guards and field-projection sugar

Two rewrite passes in [`Surface/Verify.lean`](../PLean/Surface/Verify.lean)
make machine/event quantifiers usable. Both are soundness-load-bearing.

#### S.3.1 — Kind-guard injection

`injectKindGuards` ([`:460-517`](../PLean/Surface/Verify.lean#L460-L517))
walks an invariant body and adds `is_<M> n.ref s →` (or `is_<ev> e →`,
with the event binder retyped to `Sig.Label`) to every quantifier over a
registered kind. Multi-binder forms (`∀ x y : T`, `∀ (x : T) (y : U)`) are
normalised to nested singles via `expandMultiBinder` first.

**Soundness analysis.**

- Adding a guard `is_<M> n.ref s → P` is a strict *weakening* — it turns
  `∀ n : <M>, P` into a property that needs to hold only for kind-matching
  refs. The injection makes a user-supplied invariant *easier* to prove,
  not harder. But the invariant the user *wrote* would, without the guard,
  range over every wrapper-typed value — including impossible ones whose
  state slot disagrees with the wrapper. So the guard is what makes the
  invariant *actually express* what the user meant. Sound, and necessary.

- For events: the rewrite changes the binder's static type from `<ev>` (a
  wrapper that doesn't exist in PLean) to `Sig.Label` and adds `is_<ev> e`.
  This collapses *two* surface notations into one quantifier. The is-predicate
  is then either rewritten to its action-equality form (via the `is_<ev>_iff`
  `@[pverifySimp]` lemma) or left opaque. Sound.

- The injection runs *after* field-projection sugar (`rewriteFieldProjections`)
  so the sugar sees the original quantifier types. Order matters; getting
  it wrong breaks the sugar (kind-guard injection retypes event binders to
  `Sig.Label`, defeating the lookup). Comment at [`Surface/Verify.lean:534-537`](../PLean/Surface/Verify.lean#L534-L537)
  documents this; the regression in [`Tests/Surface/FieldProjectionSugar.lean`](../Tests/Surface/FieldProjectionSugar.lean)
  pins it.

**Caveat — non-bracketed mathlib quantifiers.** `quantNotationKinds`
([`:564-567`](../PLean/Surface/Verify.lean#L564-L567)) recognises mathlib's
`«term∀_,_»` / `«term∃_,_»` notation for the field-projection pass; the
kind-guard injection uses Lean's primitive `Term.forall` / `Term.exists`
matchers, so a mathlib-notation quantifier might *not* get kind-guarded.
Multi-binder normalisation also uses the primitive shapes only. This is
unlikely to bite because invariant bodies parse through Lean's standard
elaborator, but worth a regression test exercising the mathlib shapes.

#### S.3.2 — `GlobalState`-shadow rejection

`rejectExplicitStateBinder` ([`:339-350`](../PLean/Surface/Verify.lean#L339-L350))
uses a *deep* walk (`containsExplicitStateBinder`, [`:324-337`](../PLean/Surface/Verify.lean#L324-L337))
that rejects *any* mention of the `GlobalState` type identifier inside a
`system <s> { … }` invariant body, regardless of binder form (`∀` / `∃` /
`let` / `have` / `fun` / `λ`). This is the right defence — the previous
∀-only matcher was the original soundness hole (fixed 2026-06-10, then
re-broken 2026-06-19 by the `let s` evasion and re-fixed). [`Tests/Surface/SoundnessRegression.lean`](../Tests/Surface/SoundnessRegression.lean)
probes 1–5 pin every binder shape.

**Caveat.** The check matches the *name* `GlobalState` / `PLean.GlobalState`.
A user who aliases the state type (`abbrev MyState := GlobalState Sig`)
and then writes `let s : MyState := default` evades the guard. The fix is
to resolve the type at elaboration time and reject any binder whose
*resolved* type is `GlobalState _`. This requires running the check in
the term elaborator rather than at the Syntax level, which is more
expensive but eliminates the name-based bypass.

Same caveat for `rejectStateShadowIn` on `init-holds` bodies.

#### S.3.3 — Spec machine kind labels included in the kind set

[`Commands/GenModule.lean:917-918`](../PLean/Commands/GenModule.lean#L917-L918):

```lean
let machineKinds : NameSet :=
  ctx.machineOrder.foldl (init := {}) fun s n => s.insert n
```

The set does not filter out `isSpec` machines, so a quantifier over a
spec machine (`∀ c : Correctness, …`) would get the kind guard. With S.1.4
in place (spec handlers skipped), this is harmless — there are no handler
obligations referencing it. When spec handlers land, ensure spec
quantifiers have well-defined semantics, otherwise the guard becomes
load-bearing for a feature that hasn't been validated.

### I.4 SMT preparation — rewrite chain soundness

`pverify_smt_prep` ([`Verify/Tactic.lean:233-248`](../PLean/Verify/Tactic.lean#L233-L248))
runs a sequence of soundness-load-bearing rewrites:

```
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
```

Walking each:

- **`simp only [pverifySimp]`** — every lemma in the set (`funextEq'`,
  `tupleEq`, `tupleForall`, `tupleExists`, `iff_eq_eq`, the `is_<ev>_iff`
  family, `addSent` / `addReceived` / `bumpActionCount` / `updateMachine`,
  `inflight` / `sent` / `received`) is *proved* in [`Verify/SimpLemmas.lean`](../PLean/Verify/SimpLemmas.lean)
  or *emitted as a `theorem` with a real proof* by `#gen_module`. None are
  axioms. Each rewrite preserves provability. Sound.

- **`sdestruct_state`** — destructures every `GlobalState`-typed local
  via `obtain ⟨gsSent, gsReceived, gsMachines, gsActionCount⟩`. Replaces
  one hypothesis with four projections. Sound (a record is canonically
  isomorphic to its field tuple).

- **`abstract_machine_lookups`** — `generalize (s.machines ((<ev>_payload_of
  e).f)) = ms`. Names a subterm with a fresh local. Sound: generalising
  is *weakening*. The gating heuristic (the argument mentions a
  `…_payload_of` constant) is a name-string check; if a user's program
  has a function named `foo_payload_of` it could over-fire, but
  over-abstraction only weakens, so the worst case is a spurious
  `unknown`.

- **`unfold DefaultInvariants` etc.** — unfolds proved-equal defs. Sound.

- **`dsimp only`** — definitional reduction. Sound.

The whole chain is wrapped in `try`, so a stage that fails-to-rewrite is
silently skipped rather than propagating an error. This is the correct
discipline for a *pre-processor* — failure to simplify can't make the
post-condition false — but it does mean the chain is **deeply forgiving**
of mismatches. If a future change adds a stage that is *not* sound on its
own (e.g., a normaliser that loses information), it would silently break
soundness rather than aborting. Worth a comment near the macro_rule.

#### S.4.1 — `funextEq` is a simproc, not a plain rewrite

[`Verify/SimpLemmas.lean:39-52`](../PLean/Verify/SimpLemmas.lean#L39-L52)
registers `funextEq` as a `simproc ↓ funextEq (_ = _)` that fires when
both sides of `_=_` have arrow type. The simproc constructs the rewrite
goal `∀ x, f x = g x` and applies `PLean.funextEq'` (a theorem) as the
proof. Sound — the proof witness is `PLean.funextEq'`, not a fabricated
term.

#### S.4.2 — `is_<ev>_iff` lemmas tagged `@[pverifySimp]`

For every event, `#gen_module` emits

```
@[pverifySimp] theorem is_<ev>_iff (lbl) : is_<ev> lbl ↔ lbl.action = …
```

with a proved body. The simp pass rewrites every `is_<ev> lbl` into its
action equality. Sound: the theorem proves the equivalence.

### I.5 Trusted-axiom layer — `Loom.SMT.trust_smt` and the obligation cache

#### S.5.1 — `trust_smt` on `unsat`

The default discharge path is `pverify_smt`, which calls `loom_smt [*]`,
which on `unsat` closes the goal with `Loom.SMT.trust_smt` — a Loom-side
axiom. This is **the** soundness anchor; PLean does not add to it. Two
PLean-side concerns:

1. **`pverify_smt_prep` runs *before* `loom_smt`**, so what the solver
   sees is *not* the user-stated obligation but its prepared form. The
   solver's `unsat` certifies the prepared form, and `trust_smt` accepts
   the *original* goal as proved. The argument is that every rewrite step
   preserves provability (above), so prepared-unsat implies original-unsat.
   This is true *if and only if* the simp set's lemmas are all sound (no
   axioms, real proofs). They are. But if a future contributor adds a
   `@[pverifySimp]`-tagged `axiom`, the chain becomes unsound. **Add a
   build-time assertion** (e.g., an `#eval` that walks the `pverifySimp`
   set and rejects any `Lean.ConstantInfo.axiomInfo` entry) so new
   contributors can't introduce one accidentally.

2. **`solver.smt.retryOnUnknown`** ([`pverify_smt_prep`-adjacent option](../PLean/Verify/Tactic.lean#L487-L489)) —
   when the chosen solver returns `unknown`, Loom may retry with another.
   This is a Loom concern, not PLean's, but worth noting that PLean does
   not currently *disable* retry. A retry that produces `unsat` is still
   `trust_smt`-anchored, so still sound.

#### S.5.2 — The obligation cache uses the same `trust_smt` axiom

[`Verify/Tactic.lean::pverifySmtCloseDefault:416-417`](../PLean/Verify/Tactic.lean#L416-L417):

```lean
mv.assign (mkApp (mkConst ``Loom.SMT.trust_smt) goalType)
```

A cache hit closes the goal via the same axiom — no additional trust
introduced. The cache key is the (local context, goal target) pair after
`instantiateMVars + consumeMData`, hashed via `String.hash` on the raw
`Expr.toString`. Two soundness questions:

1. **Hash collision.** 64-bit `String.hash` has a birthday bound of ~2^32
   for a 50% collision probability. At 10⁴ cache entries (the order of a
   single benchmark), collision probability is ~10⁻¹¹. At 10⁶ entries
   (everything PLean will ever cache), still ~10⁻⁷. Acceptable, but not
   *zero*. PVerifier sidesteps this by checksumming the *full* generated
   UCLID source; PLean could match by writing the goal text into the
   cache file and checking it on lookup (the cache already writes the
   text — just gate hit-acceptance on text match too). Worth the marginal
   cost; the soundness improvement is qualitative ("never wrong" vs "very
   probably right").

2. **`Expr.toString` stability across Lean releases.** The cache persists
   across `lake clean`. If a Lean upgrade changes the `Expr` printer
   (e.g., bound-variable indices), every cache entry becomes ambiguous —
   a hit could pair a current goal with a stale text from a different
   Lean version. Mitigate by writing the Lean toolchain version into the
   cache filename or directory. Today, `lake clean` invalidates, so this
   is a low-risk gap.

3. **The cache write occurs *after* `Loom.SMT.trust_smt` accepts** —
   [`:421-424`](../PLean/Verify/Tactic.lean#L421-L424). So a cache entry
   only exists if the *real* solver previously returned `unsat`. There is
   no path to inject a bogus cache entry from inside PLean. (The file
   path is writable by the user, of course; an attacker with shell access
   could plant entries. This is the same trust model as `lake clean`.)

### I.6 Manual-proof escape hatch (`@[pverifyProof]`)

[`Verify/Obligation.lean::emitOneObligation:194-378`](../PLean/Verify/Obligation.lean#L194-L378):
when a user-registered theorem name matches, the generator builds the
obligation's *expected* type and discharges via `exact @<userThm> this
[param] lbl` inside a `<name>_check` shim. If the user theorem's
signature doesn't match, `exact` fails and the obligation reports as a
tactic error.

This is the **fix** to the 2026-06-18 soundness hole (commit `555641970`,
recorded in STATUS) where the registry was keyed on name only and a
mismatched user theorem (`True := trivial`) was silently accepted.

#### S.6.1 — `sorry`-backed manual proofs are caught

[`:158-166`](../PLean/Verify/Obligation.lean#L158-L166): the classifier
inspects *both* `<name>_check`'s value AND the user theorem's value for
`sorry`. If either contains a `sorry`, the obligation is reported as
`unfinished`. [`Tests/Surface/SoundnessRegression.lean` probe 6](../Tests/Surface/SoundnessRegression.lean#L187-L240)
pins this.

#### S.6.2 — `sorry`-containing theorems still live in the environment

The `pverify_log_failure_else_sorry` wrapper ([`Verify/Tactic.lean:96-115`](../PLean/Verify/Tactic.lean#L96-L115))
catches any tactic-chain failure, stashes the diagnostic in the per-key
diag map, and closes the goal by `sorry`. The classifier reads the diag
map and flags the obligation. `pverify.failOnIncomplete` makes the
command throw at the end.

**But the `theorem <thmName> := by sorry` is already committed to the
environment** at the point `elabCommand` returned. If `failOnIncomplete`
is `false` (or another file `import`s a partially-verified pmodule), the
sorried theorem is accessible by name. A downstream consumer who writes

```lean
example : <obligationType> := <Mod>.<thmName>
```

gets a `sorry`-backed proof of an unverified property. Lean's standard
`declaration uses 'sorry'` warning fires, but only for the declaration
that uses the theorem — not for the importing module wholesale.

**Mitigations.**

- The right discipline is the existing `pverify.failOnIncomplete = true`
  default, which makes the command throw. Programs that use the
  `false` setting are doing so as a debugging affordance, not for
  production. Make this explicit by adding an `axiom`-style attribute on
  the emitted theorem (e.g., `@[pverifyAxiomatic]`) that flags it as
  unverified, surfaceable by a checker over the env.
- Alternatively, on failure, instead of `:= by sorry`, emit `:= by
  Loom.SMT.trust_smt _` *and* record the obligation in a separate
  "needs verification" extension that `#pverify` reads at command end.
  This is more invasive but removes the silent `sorry`.

Today's state is sound *as currently used* (the suite never imports an
unverified obligation), but the assumption isn't enforced.

### I.7 Other rewrite passes

#### S.7.1 — `default_inv` ladder

[`Verify/Tactic.lean::default_inv:643-692`](../PLean/Verify/Tactic.lean#L643-L692).
The macro starts with `default_inv_guard`, which (after recent fixes)
*actually* fails unless the goal head is one of `DefaultInvariants` /
`UniqueActions` / `IncreasingCount` / `ReceivedSubsetSent`. With the
guard active, the rest of the macro splits the 3-way conjunction, intros
per-conjunct, simps, and closes each leaf via `solve_by_elim` /
`Nat.lt_irrefl` / `grind` / `omega`. Each closing tactic is sound; the
chain is a *closure*, not a rewrite. Sound.

The guard could be sharpened by also requiring the goal to be *quantified
over labels* (or to match the exact UA/IC/RS shape) — today, a hand-rolled
goal that names `UniqueActions` but isn't in canonical form trips the
ladder. Not unsound; just imprecise.

#### S.7.2 — `pverify` and `pverify_default` close-chains

[`:1025-1041`](../PLean/Verify/Tactic.lean#L1025-L1041) — three branches:
trivial-handler (`pverify_step_wp; done`), post-equals-pre (`assumption`),
and the full chain. The `assumption` branch's interaction with the
trivial-`True` post is a usability nit (see REVIEW's earlier I.4) but not
unsound — `assumption` only succeeds on a *real* type match.

### I.8 Soundness regressions pinned by tests

[`Tests/Surface/SoundnessRegression.lean`](../Tests/Surface/SoundnessRegression.lean)
pins six probes:

1. False invariant must FAIL to verify (state-application).
2. Invariants are emitted as `GS → Prop` (not closed `Prop`).
3. Non-leading `∀ s : GlobalState Sig, …` is rejected.
4. `let`/`have` `GlobalState` shadow is rejected.
5. `∃ s : GlobalState Sig, …` shadow is rejected.
6. `sorry`-backed `@[pverifyProof]` is reported as unfinished.

This is the minimum bar. The **gaps** I'd add immediately:

- A probe that exercises `goto`-only handlers (S.1.3) — author a state with
  `on eGo goto Won` and an invariant that the transition *would* violate;
  pin that `#pverify` either fails or doesn't silently pass.
- A probe that exercises `entry { … }` handler mutation (S.1.1) — same
  shape as the `let` shadow probe but with a state-mutating entry.
- A probe that exercises a `pverifySimp`-tagged axiom (S.5.1) — assert at
  command time that the set contains no `axiomInfo` entries.

### I.9 Summary

The architecture is sound by construction at the points where the trusted
axiom (`trust_smt`) and the type-checked manual-proof bridge meet the
solver. The two real soundness concerns to act on are:

1. **VC-completeness gaps** for `entry` handlers (S.1.1), `goto`-only
   transitions (S.1.3), and (latent) spec handlers (S.1.4). Of these,
   S.1.1 and S.1.3 are exploitable today with hand-crafted P programs.
2. **`pverify_log_failure_else_sorry` leaves sorried theorems in the
   environment** (S.6.2). Today's discipline (`failOnIncomplete = true`)
   keeps this safe, but the safety isn't enforced by the type system.

Mitigations for both are mechanical (≤1 day each). They should ship
before any further benchmark ports — every new port that exercises an
entry or goto-only transition compounds the latent-bug surface.

---

## II. Code Style

The codebase is ~7.2k LOC across four directories. Three modules dominate:
[`Verify/Obligation.lean`](../PLean/Verify/Obligation.lean) (1009),
[`Verify/Tactic.lean`](../PLean/Verify/Tactic.lean) (1057),
[`Commands/GenModule.lean`](../PLean/Commands/GenModule.lean) (981).

Style follows CLAUDE.md's house rules in most places; the remaining drift
is concentrated in three categories.

### II.1 Module size and decomposition

Two splits are overdue.

#### Sx.1.1 — `Verify/Tactic.lean` (1057 LOC)

The file mixes four cohesive units:

- **Diag plumbing** (`pverifyDiagMap`, `pverifyHere!`, `pverifyCache*`,
  options) — ~300 LOC.
- **Pre-SMT preparation tactics** (`sdestruct_state`,
  `abstract_machine_lookups`, `pverify_smt_prep`) — ~120 LOC.
- **SMT discharge** (`pverifySmtCloseDefault`, `pverifySmtCloseProfiled`,
  `pverify_smt`, `pverify_split_smt`, `pverify_grind`) — ~250 LOC.
- **Manual-proof helpers** (`pverify_carry_after_recv`,
  `pverify_not_inflight[_by]`, `pverify_inflight_by`,
  `pverify_machine_has_type`) — ~300 LOC, mostly docstrings.
- **`default_inv` + `pverify` + `pverify_default` macros** — ~80 LOC.

Suggested split, mirroring CLAUDE.md's "match the decomposition to the
existing axes" advice:

```
Verify/
  DiagAndCache.lean   -- diag refs, cache hashing, pverify.* options
  SmtPrep.lean        -- sdestruct_state, abstract_machine_lookups, pverify_smt_prep
  Smt.lean            -- pverify_smt, pverify_split_smt, pverify_grind, profiled path
  ManualProof.lean    -- the helper tactics (mostly docstrings)
  Tactic.lean         -- default_inv, pverify, pverify_default top-level macros
```

Saves ~5 round-trips for a reader who only cares about one of these.
None of the splits introduce new import cycles (verified by walking the
dependency graph in-file).

#### Sx.1.2 — `Commands/GenModule.lean` (981 LOC)

`#gen_module` has *eight* steps, each ~50–100 LOC. They could move into
co-located files:

```
Commands/GenModule/
  Wrappers.lean       -- Step 1: machine wrappers
  Kinds.lean          -- Step 1b: MKind, is_<M>, <M>_allocated
  Unions.lean         -- Step 4: E/G/S/Fields/Sig/PM'/GS
  EventBridge.lean    -- Step 4b–4d″: is_<ev>, payload_of, _spec, _iff
  Wp.lean             -- Step 5: emitDerivedWP
  Accessors.lean      -- Step 6: var accessors, state aliases, handler defs
  Verify.lean         -- Step 7: invariants, lemma bundles, UserInv, init
GenModule.lean        -- the dispatch loop calling all of them
```

A reader looking up "what does Step 4d emit" today has to scroll through
800 lines of unrelated emission code. The split is mechanical (each
function is already private) and adds zero net imports if the
sub-modules re-export their step.

#### Sx.1.3 — Other size hotspots

- [`Surface/Verify.lean`](../PLean/Surface/Verify.lean) (859) is big but
  cohesive — every declaration is a surface command for a verification
  decl + its materialiser. Could split into `SurfaceDecls.lean` (the
  command grammar) and `Materialise.lean` (the materialisers), each ~400.
  Lower priority than II.1.1 / II.1.2.

- [`Verify/CexModel.lean`](../PLean/Verify/CexModel.lean) (629) is
  cleanly the counter-example decoder. Internally consistent; keep
  unsplit.

### II.2 Structural duplication that risks soundness regressions

#### Sx.2.1 — Bundle-conjunction emitters are NOT fully consolidated

[`Commands/GenModule.lean::emitConjPredicate:674-694`](../PLean/Commands/GenModule.lean#L674-L694)
exists to centralise the `def <name> : GS → Prop := fun s => p1 s ∧ … ∧
pn s` pattern. `emitLemmaBundles` and `emitUserInv` use it. But
`emitInitConditions` ([`:706-780`](../PLean/Commands/GenModule.lean#L706-L780))
inlines its own fold because it mixes state-dependent (framework clauses)
and closed (user `init-holds`) conjuncts. The duplication is exactly the
shape that caused the 2026-06-10 soundness bug (one of three callers
forgot to apply `s`). The risk is recurring: if a future variant of
init-holds adds a third clause shape, the inline fold is where the
mistake happens.

**Fix.** Generalise `emitConjPredicate` to accept a `members : Array (TSyntax
'term × Bool)` where the bool says whether to apply `s`. The two existing
callers pass `applyState`-uniform arrays; `emitInitConditions` passes a
mixed array. One emitter, three call sites, zero drift surface.

#### Sx.2.2 — Handler-emission cartesian product

[`Verify/Obligation.lean::emitOneObligation`](../PLean/Verify/Obligation.lean#L185-L380)
is ~200 LOC, of which the inner `match` (~100 LOC) hand-enumerates the
`hasPayload × hasAccessors × isDefault` cartesian product. Today the
generation is via an `Array (TSyntax 'tactic)` accumulator (lines 324–344)
already — the only remaining if-else is `if hasPayload then`/`else` on
the *signature* (lines 365–378). Two arms is fine; the refactor that
previous reviews called for is largely done.

What's still split is `materialiseStateBodyItem` ([`Commands/GenModule.lean:573-640`](../PLean/Commands/GenModule.lean#L573-L640))
with four arms differing only in (a) whether there's a `param` and (b)
whether the def gets an `entry`/`<ev>` name. Each arm has an identical
`buildVarBindings + bodyItem + elabCommand` body. Factor the body into a
helper taking `(defName, params, doSeq) → CommandElabM Unit`; the four
arms reduce to four `match` clauses that destructure and call the
helper. ~25 LOC saved, plus the four identical `noncomputable` comments
collapse to one.

#### Sx.2.3 — `idE`, `idG`, `idS`, … unhygienic identifier helpers

[`Commands/GenModule.lean:57-71`](../PLean/Commands/GenModule.lean#L57-L71)
has 13 private one-liners building `mkIdent` for a fixed set of names.
Verify/Obligation.lean repeats this pattern with `idSig` / `idThis` /
`idParam` / `idLbl`. The pattern is fine — see the macro-hygiene
discussion in CLAUDE.md — but it would be cleaner as a single
`unhygienicIdent (name : Name) : Ident := mkIdent name` with call sites
using the literal name. Performance is identical; readability improves.

### II.3 Comment hygiene

The user's stored memory `[feedback_plean_comment_style]` (PLean comment
style) says: no phase numbers / decision IDs in source, no
thought-process narration, no paths or repo URLs, WHY-only. There are
specific drift sites worth cleaning.

#### Sx.3.1 — `Uclid5CodeGenerator.cs:NNNN` references in semantic-layer comments

The user's stored memory `[feedback_no_paths_in_comments]` forbids paths
or repo URLs in source comments. Hits to fix (each is a file:line cite
to PVerifier's C# source):

- [`Semantics/GlobalState.lean:7-13`](../PLean/Semantics/GlobalState.lean#L7-L13)
  — "Mirrors PVerifier's `StateAdt` at `Uclid5CodeGenerator.cs:594-606`".
- [`Semantics/GlobalState.lean:56`](../PLean/Semantics/GlobalState.lean#L56)
  — same.
- [`Semantics/Default.lean:6, :18-21, :32-35, :42-45`](../PLean/Semantics/Default.lean)
  — four UCLID5 source citations.
- [`Semantics/Label.lean:29, :49-50, :62-66, :82`](../PLean/Semantics/Label.lean)
  — four more.
- [`Semantics/Primitives.lean:5-13`](../PLean/Semantics/Primitives.lean#L5-L13)
  — six more.
- [`Semantics/Predicates.lean:5-8, :23-24, :40-44, :49-51, :55`](../PLean/Semantics/Predicates.lean)
  — six more.

The pattern across the semantic layer is "mirrors PVerifier's <thing> at
<file:line>". The fact that the C# is the reference is true, but per
project policy these citations live in plan docs, not source. Replace
with a one-line statement of *intent* ("the UCLID5 `[Label]boolean`
encoding") and drop the file:line. The git log already records the
correspondence (commits cite PVerifier when the fix is from a read of it).

#### Sx.3.2 — PLAN_P* decision-ID references in source

Comments still embed `D8` / `D10` / `D13` / `D17` / `D20` / `R15` / `R20`
/ `PLAN_P3 D19` / etc. Per `[feedback_plean_comment_style]`, these
should describe the design directly. Specific sites:

- [`Commands/GenModule.lean:74, :88, :103, :153-180, :217-220, :225-229, :231, :470, :505-513, :522-528, :533-540, :609-617, :626-634, :784-790, :940-945, :949-955`](../PLean/Commands/GenModule.lean):
  many "D10", "D14", "D18", "D20", "D21", "R20", "(PLAN_P3 D18)",
  "(PLAN_P3 D19)" references.
- [`Surface/Notation.lean:9, :29`](../PLean/Surface/Notation.lean#L9):
  "Decision D16: notation lands in Phase 2…" — pure history, drop.
- [`Surface/Machine.lean:35-52, :84, :91-95, :109-112`](../PLean/Surface/Machine.lean):
  "Phase 0", "D10", "Phase 1", "Phase 2".
- [`Surface/Stmt.lean:130-132, :154-160`](../PLean/Surface/Stmt.lean):
  "REVIEW.md I.2", "REVIEW.md I.11" — fine *only because* the previous
  review's section numbers are stable; better as a comment about the
  invariant.
- [`Internal/Decls.lean:79-94, :107-112, :122-129, :169, :173, :194-198`](../PLean/Internal/Decls.lean):
  "PLAN_P3 D19", "PLAN_P3 D20 / R20".
- [`Semantics/Monad.lean:6, :13`](../PLean/Semantics/Monad.lean#L6):
  "decision D1", "decision D5".
- [`Semantics/Primitives.lean:36`](../PLean/Semantics/Primitives.lean#L36):
  "NOTE (Phase 3, R15 / R-P3.2)".

These are forensic, not load-bearing. Rewriting each to a one-line WHY
note (or deleting if the WHY is obvious from the code) is mechanical and
shrinks the file headers noticeably.

#### Sx.3.3 — Historical narrative in inline comments

The clearest examples (per memory `[feedback_plean_comment_style]`):

- [`Semantics/Monad.lean:13-19`](../PLean/Semantics/Monad.lean#L13-L19):
  "The `MAlgOrdered` instances … are `scoped` inside `PartialCorrectness
  DemonicChoice` (decision D5 — the v1 verification mode). The `open` is
  *not* baked into this file because that would force every importer to
  use partial-correctness + demonic choice — instead we re-export the
  namespaces …" — narrative. Replace with a one-liner: "Instances are
  scoped; importers re-export via `open PLean PartialCorrectness
  DemonicChoice`."

- [`Commands/GenModule.lean:355-380`](../PLean/Commands/GenModule.lean#L355-L380):
  the `emitPayloadCharacterizations` preamble walks through "two things
  go wrong" with paragraph-style prose. The *invariant* the code
  maintains ("`<ev>_payload_of` is opaque to lean-auto; `_spec` supplies
  its defining equation") is two lines; the rest is debugging
  archaeology.

- [`Verify/Obligation.lean:36-41`](../PLean/Verify/Obligation.lean#L36-L41):
  "Unhygienic binders … Without these, the bare `this` / `param` … acquire
  macro scopes and the rendered signature carries ✝ marks that break the
  copy-paste manual-proof skeleton." — one-line WHY: "Unhygienic binders
  so the rendered signature shows the user-facing names."

- [`Surface/Verify.lean:300-322`](../PLean/Surface/Verify.lean#L300-L322):
  the 22-line preamble on `containsExplicitStateBinder` re-tells the
  2026-06-10 soundness incident. The WHY is one paragraph; the rest is
  STATUS.md territory.

- [`Verify/Tactic.lean:264-269`](../PLean/Verify/Tactic.lean#L264-L269):
  the cache "Soundness/Stability" preamble has both — soundness (one
  line) and stability (paragraph mentioning macro-scope drift). The
  stability paragraph is a development-history note.

These are not load-bearing; they make the files harder to navigate
without conveying invariants the code doesn't already express.

#### Sx.3.4 — Stale doc that's drifted from the code

- [`Surface/Stmt.lean:43-46`](../PLean/Surface/Stmt.lean#L43-L46): the
  `pSendNamed` syntax has `priority := high`, but the comment doesn't say
  why. Likewise for `pAssign` ([`:164`](../PLean/Surface/Stmt.lean#L164)).
- [`Internal/Decls.lean:107-110`](../PLean/Internal/Decls.lean#L107-L110):
  "The body is RETAINED after materialisation so the Phase-3 obligation
  generator (`#pverify`) can extract `var` declarations …" — "Phase-3" is
  history; the *invariant* ("the body is retained for accessor
  extraction") is one line.

### II.4 Suggested ordering

If picking these up:

1. **Soundness fixes** (I.1 / I.3 / I.6) — these are *code* changes, not
   style. Day 1.
2. **`emitConjPredicate` unification** (II.2.1) — closes the recurring
   foot-gun. Half a day.
3. **`Verify/Tactic.lean` split** (II.1.1) — straightforward; opens
   future contributions. Half a day.
4. **Comment-style sweep** (II.3.1, II.3.2, II.3.3) — mechanical, can
   batch with each file's next substantive edit. Spread out, not bulk.

The order is "soundness first, structural second, cosmetic last." That
mirrors CLAUDE.md's "Correctness first" and is the ordering that lowers
the *next* contributor's bug-introduction surface fastest.
