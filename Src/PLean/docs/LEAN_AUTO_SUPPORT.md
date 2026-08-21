# Maximizing lean-auto discharge in PLean

Contributor reference. PLean discharges each verification obligation by
handing a first-order VC to `loom_smt`, which translates via
[lean-auto](https://github.com/leanprover-community/lean-auto) into
SMT-LIB and calls cvc5/z3. lean-auto's translator is **first-order**: it
rejects genuinely higher-order inputs with `lamSort2SSortAux ::
Unexpected error. Higher order input?` and, in one confirmed upstream
bug, mis-reifies membership+`Eq` shapes (`reifTermCheckType … is not
type correct`; see [`LEANAUTO_ISSUE_DRAFT.md`](LEANAUTO_ISSUE_DRAFT.md)).

Everything below exists to present each obligation to lean-auto in a
shape it accepts — **applied uninterpreted symbols and concrete
constructors, no function-typed atoms, no struct sorts carrying
function fields**. The two failure messages above are the signal that
one of these techniques is missing or mis-applied.

The single rule of thumb: *a function-typed value may appear only in
applied position (`f x`), never as an atom (`f`), and never as a field
of a datatype lean-auto must translate.* Every method here is a
corollary.

---

## 1. Normalize the VC shape (the `pverifySimp` set)

`Verify/SimpLemmas.lean` holds lemmas/simprocs tagged `@[pverifySimp]`;
`pverify_smt_prep` runs `simp only [pverifySimp] at *` first. Each turns
a higher-order construct into a first-order one:

- **`funextEq` (simproc).** `f = g` between function-typed values →
  `∀ x, f x = g x`. After it fires, function values never appear as SMT
  atoms — only their applications, which translate as uninterpreted
  functions. This is what lets `GlobalState`'s `sent : Label → Bool` /
  `machines : MachineRef → MachineState` fields go to SMT at all.
- **`iff_eq_eq`.** `(p ↔ q) = (p = q)` — lean-auto chokes on `↔`.
- **`tupleEq` / `tupleForall` / `tupleExists`.** Tuples aren't native
  SMT sorts; destruct equalities/quantifiers over `α × β` into
  per-component form.
- **`globalStateForall` / `globalStateExists`.** `∀ s : GlobalState P,
  Q s` → per-field binders `∀ sent received machines containers
  actionCount, Q ⟨…⟩`. lean-auto rejects a `GlobalState`-typed binder
  because of its function-typed fields; splitting places each function
  type in applied position only. Loop iteration VCs (from
  `WPGen.pforeach`) quantify over an intermediate `GlobalState`, so this
  is load-bearing for any loop obligation.
- **State-update unfolds** (`GlobalState.addSent` / `addReceived` /
  `bumpActionCount` / `updateMachine`, plus `inflight`/`sent`/
  `received`): so a post-state lookup like `(s.addSent lbl).sent l`
  β-reduces to `decide (l = lbl) || s.sent l = true` — an applied
  uninterpreted `s.sent` symbol.

**When you add a state-update or container helper, tag it
`@[pverifySimp]`.** An untagged helper reaches lean-auto as an opaque
symbol and the obligation returns `unknown`.

---

## 2. Destructure state out of the goal (`pverify_smt_prep`)

`Verify/Tactic.lean` runs, in order:

1. `simp only [pverifySimp]` — §1.
2. `unfold PLean.stateOf` — so `stateOf x s = <S>_st` surfaces the raw
   `(s.machines _).currentState` projection.
3. **`sdestruct_state`** — destruct every `GlobalState`-typed local into
   its five fields as top-level locals (`gsSent`, `gsMachines`,
   `gsContainers`, …), then destruct `gsContainers` one level further so
   each container var becomes its own free function local. Without this,
   lean-auto rejects the first `s.sent l` / `s.containers.foo`
   projection as higher-order. Multiple `GlobalState` binders (pre-state
   + a loop's post-state) get disjoint subscripted names.
4. **`abstract_machine_lookups`** — generalise a compound lookup
   `gsMachines (<ev>_payload_of e).ref : MachineState` (a `machines`
   application whose argument goes through an opaque payload extractor)
   to a fresh `MachineState` local. lean-auto can't translate the nested
   opaque-extractor-under-`machines` shape under a `∀`.
5. **`destruct_machine_state`** — when `Fields` has a function-typed
   component, destruct each `MachineState` into `(stage, currentState,
   fields, kind)` and `fields` into per-var locals, so a function-typed
   machine `var` appears as a top-level applied symbol. Gated: skipped
   when all vars are first-order (destructuring would erase the equation
   tying the projection to its source, which the solver may need for a
   counter-example).
6. Unfold the default-invariant aliases (`DefaultInvariants`,
   `UniqueActions`, …) so they become plain `∧`-chains.

The discipline: **anything reachable from `GlobalState` that isn't a
plain scalar must be destructured or abstracted into a free
applied-symbol local before lean-auto sees it.**

---

## 3. Keep the *saved types* first-order (`#gen_module`)

lean-auto's monomorphizer reads projection-function types out of the
environment, not just the goal. So the generated `GlobalState` schema
must itself be first-order-friendly:

- **`MachineRef := Nat`; per-machine type is a one-field wrapper.** Kind
  checks go through a `Nat` kind tag; there is no `MachineRef <M>`
  refinement (a dependent refinement would be higher-order).
- **Machine-kind predicates are `@[inline] def`s over projections.**
  `is_<M> m s` / `<M>_allocated m s` reduce to `(s.machines m).kind = …
  ∧ (s.machines m).currentState ∈ …`. The obligation generator unfolds
  them so a guard like `is_<M> n.ref s` becomes bare `machines`
  projections that hit the destructured `gsMachines`.
- **`<ev>_payload_of` is sealed `@[irreducible]`** after emission, with
  `_spec`/`_mk` lemmas as its defining equations. Sealed, lean-auto
  treats it as an uninterpreted function; the spec lemmas let a routing
  invariant `∀ e, is_<ev> e → … payload_of e …` close over a
  freshly-sent label. (Same seal-after-prove trick as `PSet`, §4.)
- **Container `var`s are hoisted out of `Fields` into `Containers`**
  (§5) — a `Fields` struct carrying a function/`PSet` field would make
  `MachineState` untranslatable even when the goal never reads that
  field.

---

## 4. First-order container theory (`PSet`, pattern G)

`Set α = α → Prop` is a transparent function type: `x ∈ s` is *defeq*
the application `s x`, which lean-auto reads as an anonymous function
atom (`Set X is not a ∀`) or trips the membership+`Eq` reifier bug.
`Semantics/PSet.lean` gives sets a **symbol** instead, via the encoding
that satisfies two constraints at once — lean-auto sees an uninterpreted
sort/relation, AND the trusted base gains no axiom:

**Pattern G — seal irreducible after proving specs transparently:**

1. `def PSet (α) : Type := α → Prop` and `def PSet.mem`/`insert`/`empty`/
   `erase` over that model.
2. Prove the algebraic spec lemmas (`mem_insert`, `mem_empty`, …) as
   `Iff.rfl`/`id` THEOREMS while everything is transparent. Tag them
   `@[pverifySimp]`.
3. `attribute [irreducible] PSet PSet.mem …`. After sealing, lean-auto's
   whnf can't peel `PSet.mem x s` back to `s x`, so it translates
   `PSet.mem` as an uninterpreted binary relation and `PSet α` as an
   uninterpreted sort — the shape UCLID5/PVerifier use for sets.

Rejected encodings (both fail on our pin): fully `opaque` (can't prove
the specs → forces axioms, breaks the empty-trusted-base goal); a
`structure` wrapper around `α → Prop` (leaks the function field → HO).
Pattern G is the only one that works.

**Storage rule (critical):** a sealed `PSet` must NEVER be a struct
field reachable from `GlobalState` — translating the enclosing datatype
still chokes. `set[T]` vars therefore hoist into `Containers` as a
whole-value row `MachineRef → PSet T`; after §2's destructure that's a
free `MachineRef → PSet T` function, which translates. Standalone `PSet`
values, function parameters, and hoisted rows are fine; `PSet`-as-field
is not.

**Surface idioms that translate:** `a ∈ s` (needs `PSet.mem_def` in the
simp set), applicative `s a` (via a `CoeFun` instance + `PSet.coe_apply`),
and whole-value predicates `f s` where `f : PSet α → Bool` (a set passed
as a value — the genuine win, e.g. Consensus's `isQuorum` +
`quorum_intersect`).

`map[K,V]` stays `K → Option V`, hoisted uncurried as `MachineRef × K →
Option V` (a Bool/Option-array — already first-order). Its
lookup-after-mutation lemmas (`mapInsert_eq`, …) are `@[pverifySimp]`.

---

## 5. Axioms and premises must reach the local context first-order

`loom_smt [*]` reads only the local context, so any top-level fact SMT
needs must be lifted in:

- **`paxiom` / `pinstance`-field axioms** are lifted per-obligation via
  `have hax_<name> := @<name>`. A top-level Lean `axiom` invoked only by
  a manual `have` is invisible to the auto chain — declare it as a
  `paxiom` to get the free lift.
- **State-independent `paxiom` bodies drop the `∀ σ : GlobalState`
  wrapper.** `materialiseAxiom` checks whether the body mentions the
  `system <σ>` binder; if not, it emits the body unwrapped. This is
  load-bearing with `PSet`/container axioms: otherwise `sdestruct_state`
  turns the bound `σ` into a `containers : Containers` binder — a struct
  carrying a `PSet`/array field — which HO-poisons EVERY obligation the
  axiom is lifted into, even ones unrelated to the axiom's content.
- **`using` premises** are unfolded before the close chain; lemma-bundle
  names are threaded through so a bundle conjunct's body is reachable.

---

## 6. Loops (`foreach` / `pforeach`)

`WPGen.pforeach` threads the *annotated* loop invariant list through the
iteration VC. To make a loop-bearing handler's obligation close by SMT:

- **Annotate the loop with an invariant the loop body preserves and that
  entails the handler's post.** `foreach (x in xs) invariant N : I ; {…}`.
  A first-order `I` (e.g. `DefaultInvariants s`, or a flat
  `∀ p : MachineRef, …`) closes via SMT with no manual proof — see
  `Tests/Verify/LoopInvStrong.lean`, and 2PC's `inv_default`-annotated
  entry/eNo loops.
- **Kind-guard injection + wrapper→`MachineRef` rewrite** fire inside
  loop-invariant bodies (`pLoopInvWrap%`), so `∀ w : <Wrapper>, …`
  retype to `∀ w : MachineRef, is_<M> w s → …[w.ref ↦ w]` — first-order
  under the iteration VC.

### 6.1 The frame walk — when the annotation can't carry the bundle

Annotating helps only when the loop invariant entails the *whole*
target. When it doesn't, `WPGen.pforeach` leaves `invariantSeq inv s' ⇒
Post s'` in the goal, which no solver can close — and if the iteration
VC also quantifies over an intermediate `GlobalState` carrying a
container row, lean-auto rejects it outright as higher-order. Either
way the obligation fails for a reason that has nothing to do with
whether the invariant is true.

`pverify_frame_walk` (`Verify/Tactic.lean`) sidesteps the annotation.
It decomposes the handler along its `>>=` spine and proves, per step,
that the step **preserves the obligation's own precondition**:

- `triple_frame_step` (`Semantics/Loop.lean`) is `triple_bind` at a
  frame cut — the intermediate assertion *is* the pre, so it unifies
  from the goal and the tactic never has to synthesise a cut.
- `triple_pforeach_with` threads that same predicate through the loop
  independently of the annotation.

Each per-step query is one primitive's footprint against a still-folded
bundle, which is small enough to translate. The generator runs the walk
as a fallback after the single-shot chain, so nothing that already
closed changes shape.

The walk applies when every step preserves the whole pre — var-reads, a
broadcast loop, a closing `goto`. A step that breaks the frame (a
`<v>_set`, or a `goto` into a state the pre's guard pins) makes the walk
fail on that step rather than close it, which is why it's a fallback and
not the default.

**Remaining loop limits (still need manual `@[pverifyProof]`):**

- **Event-quantifier invariants** — `∀ e : Label, e is <ev> → …
  payload_of e …` under the `∀ x : GlobalState` post-state binder gives
  a nested-quantifier query the solver returns `unknown` on.
- **`+=`/`set` value-tracking across a loop** — an invariant whose
  preservation needs the *value read* at a `<v>_get` tied to the
  container across a `<v>_set` (e.g. "the new member of `yesVotes`
  prefers YES, from the handled label"). The WP abstracts the read
  value, severing the fact. 2PC's eYes handler is the canonical case.

Closing these two needs a value-tracking `pforeach` WP + first-order
event handling that are **not yet built**; see
[`PLAN_CONTAINERS_AND_LOOPS.md`](PLAN_CONTAINERS_AND_LOOPS.md).

---

## 8. Invariants with no Hoare-step proof at all

Some invariants are true on every reachable state yet have no
per-handler consecution proof, because their argument is a well-founded
induction over a data value (Paxos ballots, Raft terms). No amount of
VC-shaping fixes that — the obligation being asked for is the wrong
obligation.

The surface for these is `prove X from A, B via <thm>;`, which replaces
X's consecution VCs and base case with a single pointwise-implication
VC. See `docs/ProofSkill.md` §1.8; the mechanics and the two soundness
guards are pinned in `Tests/Syntax/DerivedInvariant.lean`.

---

## 7. Diagnosing a failure

| Symptom | Likely cause | Fix |
|---|---|---|
| `Higher order input?` | a function-typed atom / a struct sort with a function field reached lean-auto | destructure it (§2), hoist it out of `Fields` (§3–4), or add the missing `@[pverifySimp]` unfold (§1) |
| `reifTermCheckType … not type correct` | transparent `Set = α → Prop` membership co-occurring with `Eq` | use `PSet` (§4) — this is the upstream bug |
| `unknown` (translated, solver gives up) | nested event/label quantifiers, or an invariant SMT genuinely can't close as annotated | strengthen/split the loop invariant (§6); if intrinsic, manual proof |
| an obligation that "passed" flips to failing after an edit | **stale `pverify` cache** | `rm .lake/build/pverify_cache/*.ok` and rebuild — the cache is keyed by goal-hash and can mask a real regression. ALWAYS measure loop/container work cache-cleared |

**Cache discipline:** the pverify cache (`.lake/build/pverify_cache/`)
records only real `unsat` results, but a byte-identical goal-hash from a
prior (differently-proven) version can produce a false pass. Treat
`lake build Tests Examples` green **with the cache cleared** as the only
ground truth.
