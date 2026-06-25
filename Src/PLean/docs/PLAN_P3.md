# PLean — Phase 3 (Verification declarations) Plan

This document expands the Phase-3 entry in [`PLAN.md`](PLAN.md). The
objective of Phase 3 is to remove the user from the per-handler-triple
business — Phase 2 made handlers verifiable, but the user still
hand-writes `theorem ..._correct := by wpgen; ...` per handler.
Phase 3 makes `#pverify M` *generate* and *discharge* those triples
from the registry alone.

Phase 3 ends with **M3**: from one `pmodule M` declaration, every
handler triple — both user-stated and default — is generated and
proved without the user writing a `theorem` line. The flagship test
is the surface PingPong from M2 plus a few of the easier Tutorial/Advanced
benchmarks.

> **Read this first.** Phase 2 produced `<Mod>.Sig` / `<Mod>.PM'` /
> `<Mod>.GS` and per-handler defs (see [`Commands/GenModule.lean`](../PLean/Commands/GenModule.lean#L403-L406)); the user must hand-write every
> triple lemma. Phase 3 collapses that user-facing line: `#pverify
> M` walks the registry, synthesises one obligation per (machine,
> state, event), and discharges each via a PLean-owned `pverify`
> tactic that wraps `wpgen` + a configurable solver. Phase 3 owns
> the *whole* "user invariants → discharged Hoare triples" pipeline.

---

## What Phase 2 left in place

### Surface and emission (used)
- [`pmodule M ... end M`](../PLean/Surface/Module.lean#L31-L36) — multi-file aggregation works, registry
  carries types/events/machines/invariants.
- [`#gen_module M`](../PLean/Commands/GenModule.lean#L403-L406) synthesises the per-pmodule type machinery and
  every handler def at the real `PM`.
- [`Surface/Stmt.lean`](../PLean/Surface/Stmt.lean) macros target real primitives.
- [`≺`, `is`, `targets`](../PLean/Surface/Notation.lean#L31-L58) notations.
- `Internal/Stub.lean` retired.

### Verification (gaps Phase 3 fills)
- [`#pverify M`](../PLean/Commands/PVerify.lean#L54-L73) is a thin wrapper over [`#pwf M`](../PLean/Commands/PWf.lean#L113-L116) plus a handler-def
  existence check (D17). **No obligation generation. No proof.**
- Users hand-write `theorem ..._correct (this : <MName>) ... :
  triple ... := by ... wpgen ...` per handler — see
  [`Tests/Surface/Phase2PingPong.lean`](../Tests/Surface/Phase2PingPong.lean). The proofs are mechanical and
  identical modulo names.
- The `Lemma` / `Theorem` / `Proof` blocks from
  [`Tutorial/Advanced/`](../../../Tutorial/Advanced/) are not yet
  parsed at all — Phase 0/2 only know about `invariant <name>` (a
  single proposition).
- The `default` proof obligation (the three sanity invariants) is
  not auto-generated; Phase 1's [`DefaultInvariants`](../PLean/Semantics/Default.lean#L53-L54) is referenced
  by hand inside hand-written triples.
- The `m is <MachineKind>` predicate doesn't exist yet (Phase 2's `is`
  is event-tag only). The benchmarks need it everywhere.

### The gap (concretely)

Take [`Tutorial/Advanced/8_LockServer/PSrc/System.p`](../../../Tutorial/Advanced/8_LockServer/PSrc/System.p).
It declares a `Lemma system_config { invariant ... }` block, a
`Theorem safety { invariant unique_lock_holder: ... }` block, and a
`Proof { prove system_config; prove safety using system_config; prove
default; }` block. After Phase 3:

```lean
#gen_module LockServer
#pverify    LockServer
-- → "LockServer: 13 invariants, 5 handlers; obligations
--    generated and discharged. system_config: ✓ (3 VCs).
--    safety: ✓ (4 VCs). default: ✓ (15 VCs)."
```

No hand-written triples. The user wrote only the `Lemma`/`Theorem`/`Proof`
groupings; PLean did the rest.

---

## Tutorial benchmark inventory

Phase 3 must handle the **structural verification machinery** used by
these benchmarks. Items the surface needs but Phase 3 *defers* are
flagged with their target phase. Sizes are line counts; complexity in
parens is rough VC count for `prove default`.

| Benchmark | Lines | Features needed | Phase-3 scope |
|---|---|---|---|
| [`6_DistributedLock`](../../../Tutorial/Advanced/6_DistributedLock/PSrc/System.p) | 40 (~8) | basic invariants, single `Theorem`/`Proof`, `inflight`, `e.source`, `e targets m`, `is`, `m1.held`, `prove default` | **in scope** — simplest non-trivial benchmark |
| [`5_Consensus`](../../../Tutorial/Advanced/5_Consensus/PSrc/System.p) | 68 (~15) | quorum axioms via `pure`, `Lemma`/`Proof`/`using`, `m is <State>` (state, not kind), `foreach` w/ `invariant forall new` | **partially** — `foreach` is Phase 5; rest in scope |
| [`8_LockServer`](../../../Tutorial/Advanced/8_LockServer/PSrc/System.p) | 96 (~20) | `m is Server`/`m is Node` (machine-kind check), `pure lock_server(): machine`, multiple `Lemma`s, `prove ... using` chains | **in scope** — exercises machine-kind quantification |
| [`3_RingLeaderVerification`](../../../Tutorial/Advanced/3_RingLeaderVerification/PSrc/System.p) | 92 (~25) | many `init-condition` axioms, multiple `Lemma`s w/ `using` deps, `prove default`, `Lemma using` chain | **in scope** — exercises lemma graph |
| [`1_ChainReplicationVerification`](../../../Tutorial/Advanced/1_ChainReplicationVerification/PSrc/System.p) | 128 (~30) | spec machine, `assert`, `map[int,int]`, `next_(m): machine`, `default(map[K,V])`, conditional `if (k in kv)` | **partial** — spec machine = Phase 4; conditional & maps = Phase 5 |
| [`2_TwoPhaseCommitVerification/Single`](../../../Tutorial/Advanced/2_TwoPhaseCommitVerification/Single/PSrc/System.p) | 128 (~22) | `set[machine]`, `foreach` w/ `forall new`, multi-state state machines, multiple `Lemma`s | **partial** — `foreach` = Phase 5 |
| [`7_ShardedKV`](../../../Tutorial/Advanced/7_ShardedKV/PSrc/System.p) | 47 | sharding via `pure shard(k): machine`, conditional sends | **in scope** if conditionals land |
| [`4_Paxos`](../../../Tutorial/Advanced/4_Paxos/PSrc/System.p) | 394 (~80) | full Paxos protocol, ~10 lemmas, `assume`, `set[machine]` operations, multi-stage proofs | **stretch / Phase 6** |

The Phase-3 acceptance set: **6_DistributedLock + 8_LockServer +
3_RingLeaderVerification verify end-to-end** through `#pverify`.
Anything that doesn't need `foreach` / spec machines / collection ops
beyond simple membership is fair game.

---

## Confirmed design decisions (Phase 3)

These extend [`PLAN_P2.md` § "Confirmed design decisions"](PLAN_P2.md).
Numbering continues so `D18` here is `D18` in any cross-reference.

1. **D18 — Per-handler obligation shape.** For each `(machine M, state
   S, event ev)` triple in the registry, synthesise:

   ```lean
   theorem M.S.<ev>_handler_correct
       (this : M) (param : <ev>_payload) :
       triple (l := PProp Sig)
         (fun s =>
           Inv s ∧                       -- conjunction of all user invariants + DefaultInvariants
           InitConditions s ∧            -- conjunction of all init-condition axioms (held forever)
           DispatcherContract this s ev param)
                                         -- inflight ev label of right shape targeting this
         (M.S.<ev>_handler this param)
         (fun _ => Inv ∧ InitConditions) := by
     pverify
   ```

   The asymmetric pre/post (more in pre, just `Inv ∧ InitConditions`
   in post) is the same shape PVerifier emits per-handler ([`Uclid5CodeGenerator.cs:1189-1201`](../../../Src/PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs#L1189-L1201))
   and the same shape M2's hand-written [`Server.Idle.ePing_correct`](../Tests/Surface/Phase2PingPong.lean#L98-L106) has.

   `DispatcherContract` is computed from the registry per (M, S, ev):
   `∃ lbl, sent lbl ∧ lbl is ev ∧ lbl targets this ∧ stateOf this = M.S_st`,
   plus payload-extraction equating `param` to the inflight label's
   payload.

2. **D19 — `Lemma`/`Theorem`/`Proof` blocks.** Surface gains three new
   commands paralleling P:

   ```p
   Lemma <name> { invariant a: ...; invariant b: ...; }
   Theorem <name> { invariant safety: ...; }
   Proof <name>? { prove <lemma> [using <l1>, ...]; prove default; }
   ```

   - `Lemma X` and `Theorem Y` register a *named group* of invariants
     in the registry (a new `PLemmaDecl` record).
   - `Proof <name>?` registers a list of `prove …` directives. The
     optional `<name>` is a tag for debug output; it doesn't affect
     verification.
   - `prove X using Y, Z` — when discharging X's obligations, conjoin
     Y and Z's invariants into the precondition (assumed; their own
     obligations are discharged separately).
   - `prove default` — discharge the per-handler default-invariant
     obligations (`UniqueActions`/`IncreasingCount`/`ReceivedSubsetSent`
     hold in the post-state for every handler).

   Expansion to Lean: `Lemma X { invariant a: P1; invariant b: P2; }`
   becomes one `def X : PProp Sig := fun s => P1 s ∧ P2 s` *plus* the
   individual `invariant a` / `invariant b` keep their existing
   per-prop emission for fine-grained `using` references. `Proof`
   blocks emit `theorem X_correct : ... := by pverify` per
   `(handler, lemma)` pair plus a top-level `theorem X_holds : Inv →
   X` if the lemma needs to be discharged at startup.

3. **D20 — `m is <MachineKind>` predicate.** Phase 2's `is` is
   event-tag only. Benchmarks (LockServer, Paxos, ChainReplication)
   need machine-kind checks: `forall (m: machine) :: m is Server ==>
   ...`.

   Encoding is **kind-tracked**: extend [`MachineState`](../PLean/Semantics/Label.lean#L80-L87) with an
   `Option MachineKindTag` field, and `<Mod>.MKind` is a synthesised
   inductive (`| Server | Client | ...`) in `#gen_module`. The `is`
   notation gains a second rule: when `<rhs>` is a registered machine
   name rather than an event name, expand to
   `(s.machines m.ref).kind = some MKind.<rhs>`.

   The `is` macro disambiguates by checking the registry at expansion
   time. If `<rhs>` is an event → `is_<ev>` (Phase 2). If `<rhs>` is a
   machine → `kind = some MKind.<m>` (Phase 3). If neither, error
   "`<rhs>` is not a registered event or machine kind".

   Resolves R14 from PLAN_P2: `forall (s : Server) :: <prop>` uses the
   wrapper struct (Phase 2) and the underlying `validRef` predicate
   (`m is Server` here) to restrict quantification to allocated refs.

   > **Phase 3 implementation deviation** (REVIEW_P3 §2.6 / §5.2).
   > The `is` macro shipped in Phase 3 is *not* registry-aware. It
   > emits `is_<rhs> $lbl` blindly and lets Lean's name resolution pick
   > whichever `is_<rhs>` is in scope (the per-event `is_<ev>` from
   > [`Commands/GenModule.lean::emitIsPredicates`](../PLean/Commands/GenModule.lean#L228) or the per-machine
   > `is_<M>` alias from [`emitMachineKinds`](../PLean/Commands/GenModule.lean#L182)).
   > Consequences: (a) the bespoke "not a registered event or machine
   > kind" error from this decision is not produced — typos surface as
   > generic `unknown identifier`; (b) M3 benchmarks reach past the
   > macro and call `<M>_allocated` directly. A registry-aware
   > rewrite remains tracked under R20-followup; STATUS records the
   > deviation under "Deferred from REVIEW_P3".

   Also: `<Mod>.MachineState.kind` is encoded as a flat `Nat` (not
   `Option MachineKindTag`) with `0` reserved for "unset" and real
   kinds ≥ 1; `<M>_allocated` checks `kind ≠ 0 ∧ kind = <M>_kind`
   per the R20 mitigation (REVIEW_P3 §5.3).

4. **D21 — `init-condition` becomes the global precondition lattice.**
   Phase 2 emits each `init-holds <prop>` as a registry entry but
   doesn't propagate it to handler triples. Phase 3 collects all
   init-conditions into `InitConditions : PProp Sig := fun s =>
   <conj>`, and *every* synthesised triple takes `InitConditions s` in
   its precondition. The frame conjoins it into the postcondition
   too — init-conditions are *invariants assumed at start*, but
   PVerifier treats them as global axioms (they hold for all time
   because P promises the user gives a *truthful* init-condition
   set). PLean does the same: `InitConditions` flows through every
   triple.

   Special case: `axiom` blocks (PVerifier's `axiom`s, PLean's
   `paxiom`) are even stronger — they're *unconditionally* assumed
   universally quantified facts, not state-indexed. Phase 3 emits
   them as Lean `axiom` decls and keeps them in scope; they're
   automatically available to `pverify`.

5. **D22 — `pverify` tactic shape.** Owns the Loom-equivalent of
   `loom_solve`. Modeled on `CaseStudies/Tactic.lean` minus the
   Cashmere-specific scaffolding (decision D3 from PLAN_P1):

   ```lean
   syntax "pverify" : tactic
   syntax "pverify" "using" ident,+ : tactic       -- assume named lemmas
   syntax "pverify!" : tactic                      -- report unsolved as errors
   syntax "pverify?" : tactic                      -- suggest the proof script
   ```

   Pipeline:
   1. `unfold` the handler def, all referenced invariants, the
      default invariants, and the relevant predicates (`is_<ev>`,
      `precedes`, etc.).
   2. `wpgen <;> first | apply WPGen.default | skip` to step through
      the handler body.
   3. `loom_logic_simp`/`loom_intro`/`loom_split` chain to break the
      goal into bare propositional VCs (one per invariant in the
      conjuncted post).
   4. Per-VC, in order:
      - If the VC's target is a `DefaultInvariants` conjunct, try
        `default_inv` (D28) first — deterministic, fast.
      - Else (or on `default_inv` failure), try `grind` (the
        configured `loom.solver`).
      - Else fall back to `loom_smt` for goals that don't decide
        arithmetically.
   5. `pverify?` prints a `Try this:` suggestion with the explicit
      script (debug aid).

   `pverify` is configurable via `loom.solver` option (already a
   thing in Loom); default `grind`.

   Lives in `Verify/Tactic.lean`.

   > **Phase 3 implementation deviation** (REVIEW_P3 §1 / §2.5 / §5.2).
   > The shipped tactic is `pverify` plus a sibling `pverify_default`
   > (used by the obligation generator for `prove default;`); the
   > `pverify using L1, L2, ...` / `pverify!` / `pverify?` variants
   > are *not* implemented at the tactic-level surface. The
   > obligation generator handles `using`-clause unfolding by
   > splicing `try unfold $[$usingUnfolds:ident]*` into the proof
   > preamble before `pverify` runs.
   >
   > `default_inv` *is* now wired into `pverify` (REVIEW_P3 §1
   > sharpened) via a head-symbol guard — see D28 below.
   >
   > `loom_smt` SMT fallback (step 4 last bullet) is **not yet
   > wired** — `pverify_solve` ends at `tauto`. M3 benchmarks that
   > need SMT will exercise R15 follow-up work, not this MVP tactic.

6. **D23 — Obligation generator entry point.** `Verify/Obligation.lean`
   has the registry-walk that synthesises every triple. Pseudocode:

   ```lean
   def synthesisePerHandlerObligations (mod : LocalPModuleCtx) :
       CommandElabM (Array Theorem) := do
     for m in mod.machines, s in m.states, ev in s.handles do
       let t ← buildTriple m s ev mod.invariants mod.lemmas mod.inits
       elabCommand (← `(theorem $t._name : $t._stmt := by pverify))
     -- Plus: the `prove default` obligation per (m, s, ev) in mod.proofs.
   ```

   The discharger is just `by pverify` — the *interesting* code is
   the obligation-statement builder, which threads in the right
   precondition shape (`Inv`, `InitConditions`, `DispatcherContract`)
   from the registry.

7. **D24 — `prove default` semantics.** P's `prove default` discharges
   the three default invariants for every handler. PLean's
   [`DefaultInvariants`](../PLean/Semantics/Default.lean#L53-L54) ([`UniqueActions`](../PLean/Semantics/Default.lean#L23-L26)/[`IncreasingCount`](../PLean/Semantics/Default.lean#L36-L37)/[`ReceivedSubsetSent`](../PLean/Semantics/Default.lean#L46-L47))
   ships in [`Semantics/Default.lean`](../PLean/Semantics/Default.lean). `prove default` becomes:

   ```lean
   theorem M.S.<ev>_handler_default :
       triple (l := PProp Sig)
         (fun s => DefaultInvariants s ∧ InitConditions s ∧ DispatcherContract ...)
         (M.S.<ev>_handler this param)
         (fun _ => DefaultInvariants ∧ InitConditions) := by
     pverify           -- internally uses `default_inv` (D28) for the three default conjuncts
   ```

   Emitted once per (M, S, ev) in addition to the user lemma
   obligations. Auto-generated, no `Lemma default { ... }` decl
   needed on the user side. The discharger is `pverify`, which (per
   D22's pipeline) routes default-invariant goals through the
   focused `default_inv` automation rather than `grind`.

8. **D25 — `using` clauses preserve precondition strength.** When
   `Proof { prove A using B, C; }` is processed, the obligations for
   A's invariants gain `B s ∧ C s` in their precondition. This matches
   PVerifier ([`Uclid5CodeGenerator.cs`](../../../Src/PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs))
   exactly. Each `using` reference must itself be either a previously-
   `prove`d lemma or `default`. Cycle detection: if A `using` B and B
   `using` A, error.

9. **D26 — `Spec` machines deferred.** `spec X observes [evs] { ... }`
   appears in `1_ChainReplicationVerification` and elsewhere. Phase-2
   registers spec machines but doesn't generate spec-handler
   obligations. Phase 3 leaves spec verification to Phase 4 — the
   spec handlers fire on observed events but don't change `Sig`'s
   shape, and they need a different obligation form (specs hold
   *across* `send`s of observed events, not at handler boundaries).
   Tracked separately.

10. **D27 — `markReceived` injected by obligation generator.** Phase
    2 elided `markReceived` from surface emission (intentional gap).
    Phase 3's obligation generator wraps each handler's body in the
    "framework prologue":

    ```lean
    def M.S.<ev>_handler_wrapped (this : M) (lbl : Sig.Label)
        (param : <ev>_payload) (hMatches : payloadOf lbl = param) :
        PM Sig Unit := do
      markReceived (P := Sig) lbl
      M.S.<ev>_handler this param
    ```

    The triple obligation is over `_handler_wrapped`, not `_handler`.
    `_handler` (the user-visible def from Phase 2) stays as-is; the
    wrapper is generated alongside the obligations. M2's manual
    `Inv ∧ ∃ p, ...` precondition becomes the *wrapper*'s precondition,
    materially identical to M1's `triple` shape.

    > **Phase 3 implementation deviation** (REVIEW_P3 §2.1 / §5.2).
    > The `_handler_wrapped` form is **not implemented**.
    > `Verify/Wrapper.lean` does not exist; the obligation generator
    > targets `<M>.<S>.<ev>_handler` directly (the user-visible Phase-2
    > def). The dispatcher contract is carried by the existential
    > `∃ lbl, inflight lbl ∧ ...` clause in the obligation's
    > precondition (matching M2's hand-written shape), not by the
    > wrapper's explicit `(lbl : Sig.Label) (hMatches : ...)`
    > parameters. This deviation is recorded under STATUS's "Deferred
    > from REVIEW_P3" — the wrapper either lands in a Phase-3b pass,
    > or this decision is rewritten to make the existential the
    > chosen design.

11. **D28 — Default-invariant automation: `default_inv` tactic.** The
    three default invariants (`UniqueActions`, `IncreasingCount`,
    `ReceivedSubsetSent`) are preserved by every well-formed handler
    by an essentially mechanical argument that depends *only* on
    which primitives the handler called — not on what the user
    invariant says. Hand-written M1 and M2 ([`HandPingPong.lean:188-214`](../Tests/Semantics/HandPingPong.lean#L188-L214))
    show the same `simp [`[`addSent`](../PLean/Semantics/GlobalState.lean#L84)`, `[`bumpActionCount`](../PLean/Semantics/GlobalState.lean#L94)`] at *; rcases ...
    with rfl | hPrev; ...` shape repeating per handler.

    > **Phase 3 implementation deviation** (REVIEW_P3 §2.2 / §5.2).
    > The shipped `default_inv` is a head-symbol-gated `simp only +
    > omega` fallback — it works for M1 / M2 / DistributedLock-style
    > "no-`send` or one-`send`" handlers, but it does *not*
    > implement the bounded `mini-tactic + rcases` case-table this
    > decision prescribes. Specifically: no per-conjunct named
    > mini-tactics (`unique_actions_step` / `increasing_count_step`
    > / `received_subset_sent_step`); no `rcases ha with rfl |
    > hPrev` chain; no `default_inv?` companion; no exhaustiveness
    > error on R21 case-table miss. The head-symbol guard
    > (`default_inv_guard`) does land — it's what makes wiring
    > `default_inv` into `pverify` safe (REVIEW_P3 §1).
    >
    > The drift-risk PLAN warned about (`simp` without `only`
    > picking up new mathlib `[simp]` lemmas) is mitigated: the
    > current macro uses `simp only [GlobalState.addSent,
    > GlobalState.addReceived, GlobalState.bumpActionCount,
    > GlobalState.updateMachine]` per the §2.6 fix.

    `pverify` (D22) is general-purpose; pulling out a focused
    `default_inv` tactic for the default-invariant goals lets us
    automate them deterministically without grind/SMT overhead.

    **The regular pattern.** For a goal of the form
    `<DefaultInv> (<post-state>)` where the post-state is a
    composition of [`addSent`](../PLean/Semantics/GlobalState.lean#L84) / [`addReceived`](../PLean/Semantics/GlobalState.lean#L89) / [`bumpActionCount`](../PLean/Semantics/GlobalState.lean#L94) /
    [`updateMachine`](../PLean/Semantics/GlobalState.lean#L98) applications, the proof shape is:

    ```lean
    intro a [b]?               -- universally quantified labels
    [intro hne]?               -- a ≠ b for UniqueActions
    intro ha [hb]?             -- s'.sent a = true, s'.sent b = true (or .received)
    simp only [GlobalState.addSent, GlobalState.addReceived,
               GlobalState.bumpActionCount,
               GlobalState.updateMachine] at ha [hb]? ⊢
    rcases ha with rfl | hAprev
    [rcases hb with rfl | hBprev]?
    -- For each (newLabel?, oldLabel?) case:
    --   if both new (UniqueActions only): hne contradiction
    --   if one new (count = s.actionCount), one old (count < s.actionCount by hIC):
    --     `intro hEq; exact absurd (hEq ▸ hIC ...) (Nat.lt_irrefl _)`
    --   if both old: appeal to the pre-state default holding (hUA / hIC / hRS)
    --   for IncreasingCount new case: `Nat.lt_succ_self _`
    --   for IncreasingCount old case: `Nat.lt_succ_of_lt (hIC ...)`
    ```

    All choices are determined by the handler's primitive footprint:
    - **No [`send`](../PLean/Semantics/Primitives.lean#L39)/`raise`/`goto`** (e.g., M1's `Server.Idle.entry`,
      M2's `Client.Booting.ePong_handler`): `sent`/`actionCount`
      unchanged → all three goals reduce to "appeal to pre-state".
    - **One `send`** (M2's `Server.Idle.ePing_handler`): the
      single-new-label form above.
    - **`goto`**: same as `send` plus a `machines` update — the
      machines update doesn't touch sent/received/count, so the
      `simp` deletes it.
    - **[`markReceived`](../PLean/Semantics/Primitives.lean#L67-L69) + `send`**: pairs of `addReceived` and
      `addSent` updates; the `rcases` chain has at most 4 leaves
      (new-recv vs old, new-sent vs old).

    **Tactic shape.**

    ```lean
    syntax "default_inv" : tactic       -- discharges all three default-inv goals at the leaf level
    syntax "default_inv?" : tactic      -- prints `Try this:` with the explicit script
    ```

    `default_inv` runs:
    1. `unfold DefaultInvariants UniqueActions IncreasingCount
       ReceivedSubsetSent at *` (so the goal reduces to the three
       conjuncts).
    2. Splits the conjunction with `refine ⟨?_, ?_, ?_⟩`.
    3. For each conjunct, runs the appropriate mini-tactic
       (`unique_actions_step`, `increasing_count_step`,
       `received_subset_sent_step`) — each is just the regular
       `intro / simp / rcases / per-case-by-name` chain above.
    4. Each mini-tactic *only* fires when the goal shape matches; on
       any mismatch it leaves the goal unsolved with a clear case-tag
       so the user can fall back to `pverify` or hand-finish.

    The `simp only [...]` set is fixed — only the four `GlobalState`
    update functions plus `decide` for the boolean equalities. No
    `simp` on user-defined invariants.

    `default_inv` is what `pverify` calls *first* on each VC whose
    target is a `DefaultInvariants` conjunct. If `default_inv` closes
    it, great; otherwise fall through to the general `pverify`
    pipeline (which will likely also fail, and the user gets a
    `pverify!` report).

    **Where it lives.** `Verify/Tactic.lean`
    alongside `pverify`. Implementation is small (~150 lines
    estimated) because the rcases chain is bounded — at most 2 levels
    deep × 3 conjuncts × 4 case combinations = 24 leaf cases, each
    one or two lines. We can ship it as a flat `match-on-goal-shape`
    tactic rather than a recursive tactic.

    **Why a separate tactic, not just `pverify`?** Three reasons:
    (a) `default_inv` is *deterministic* — no `grind` or SMT, so
    proofs are stable across solver-version changes; (b) it's *fast*
    — the `simp only` + `rcases` chain takes <50ms vs grind's
    ~500ms; (c) it gives `pverify`'s failure reports a cleaner case
    tag (`UniqueActions/<handler>` vs a generic
    `default_invariants/<handler>`).

    *Tracked risk:* `R21` — handlers that call primitives we haven't
    yet wired into `default_inv`'s case table (e.g., a future
    `broadcast` primitive that sends *N* labels at once) will fall
    through to `pverify` until the table is extended. Mitigation:
    exhaustiveness check at compile time — `default_inv`
    enumerates the supported primitive list, anything else triggers
    a tactic-level error directing the user to add a case.

---

## Module list (Phase 3 deliverables)

```
Src/PLean/PLean/
  Surface/
    Verify.lean                  # MODIFIED: add Lemma / Theorem / Proof blocks
                                 # (D19), `init-condition` collection (D21)
    Notation.lean                # MODIFIED: extend `is` to handle machine kinds
                                 # (D20). Same syntax, registry-aware expansion.

  Semantics/
    Label.lean                   # MODIFIED: MachineState.kind : Option MachineKindTag
                                 # (D20). Backward-compatible default to none.

  Verify/                        # NEW DIRECTORY
    Obligation.lean              # NEW: per-handler triple synthesis (D18, D23)
    Tactic.lean                  # NEW: `pverify` tactic (D22) + `default_inv`
                                 # automation (D28); the latter discharges
                                 # default-invariant goals deterministically
                                 # without grind.
    Sanity.lean                  # NEW: default-invariant obligation emission
                                 # (D24)
    DispatcherContract.lean      # NEW: builds the dispatcher precondition
                                 # from (machine, state, event) registry data
    Wrapper.lean                 # NEW: handler `_wrapped` form with markReceived
                                 # injected (D27)

  Commands/
    GenModule.lean               # MODIFIED: emit MKind inductive + per-MachineState
                                 # kind tagging (D20); emit InitConditions
                                 # conjunction (D21); call into Verify/Obligation
                                 # at the end if `Proof` blocks exist
    PVerify.lean                 # MODIFIED: do real obligation generation
                                 # & discharge (M3). #pwf stays as fast subset.

Src/PLean/Tests/Surface/
  Phase3DistributedLock.lean     # NEW: M3 — port of Tutorial/Advanced/6_DistributedLock,
                                 # verifies clean via `#pverify`
  Phase3LockServer.lean          # NEW: port of Tutorial/Advanced/8_LockServer
                                 # (exercises `m is Server` machine-kind check, D20)
  Phase3RingLeader.lean          # NEW: port of Tutorial/Advanced/3_RingLeaderVerification
                                 # (exercises multi-Lemma `using` chain)
  PVerifyTactic.lean             # NEW: unit tests for `pverify` against
                                 # synthetic small triples
  ObligationShape.lean           # NEW: `#guard_msgs`-pinned tests verifying
                                 # the obligation generator produces the
                                 # right theorem statements (no proof, just
                                 # statement shape)
```

---

## Phase 3 work breakdown (ordered)

### 1. **`MachineState.kind` + `MKind` inductive** — small, foundational
*([`Semantics/Label.lean`](../PLean/Semantics/Label.lean), [`Commands/GenModule.lean`](../PLean/Commands/GenModule.lean), ~½ day)*

Backward-compatible extension to [`MachineState`](../PLean/Semantics/Label.lean#L80-L87):

```lean
structure MachineState (S : Type) (F : Type) where
  stage        : Bool
  currentState : S
  fields       : F
  kind         : Nat := 0   -- index into the per-pmodule MKind inductive;
                            -- 0 reserved for "unset" so existing code that
                            -- builds a MachineState without kind still
                            -- compiles. #gen_module emits the proper
                            -- mapping.
```

`#gen_module` emits:
```lean
inductive <Mod>.MKind | Server | Client | ... deriving DecidableEq, Inhabited
def <Mod>.<MachineName>_kind : Nat := <index>
```

Then the per-machine wrapper struct's `Coe` instance is augmented
with a `kind`-aware constructor: `M.mk' (r : MachineRef) : M := ⟨r⟩`,
plus a `M.allocated (m : M) (s : GS) : Prop := (s.machines m.ref).kind
= <Mod>.<M>_kind`.

Exit: `#check @<Mod>.Server_kind`, `#check @<Mod>.Server.allocated`
both resolve. Existing tests (M1, M2) still build (the field has a
default).

### 2. **`is` extended for machine kinds** — D20
*([`Surface/Notation.lean`](../PLean/Surface/Notation.lean), ~½ day)*

The [`is` term-macro](../PLean/Surface/Notation.lean#L43-L54) currently rewrites `lbl is <ev>` to
`is_<ev> lbl`. Extend the macro to:
1. Look up `<rhs>` in the local pmodule registry (it's available via
   [`getLocalPModuleCtx?`](../PLean/Internal/Registry.lean#L125-L127)).
2. If `<rhs>` is a registered event → emit `is_<rhs>` (Phase 2).
3. If `<rhs>` is a registered machine → emit `<rhs>.allocated`
   (D20).
4. Otherwise → error "<rhs> is not a registered event or machine kind".

The macro becomes a `command_elab`-aware term elaborator; we may need
to hold the registry reference at expansion time via a TermElabM
wrapper, since macro expansion happens in TermElabM and
`getLocalPModuleCtx?` lives in CommandElabM. Workaround: the
expanded form uses bare `is_<rhs>` *or* `<rhs>.allocated` — Lean's
namespace search picks whichever exists. If both exist (very rare,
event and machine sharing a name → name conflict, error), warn.

Exit: a test that uses `m is Server` inside an invariant body and
`lbl is ePing` inside another, both elaborate.

### 3. **`Lemma`/`Theorem`/`Proof` blocks** — D19
*([`Surface/Verify.lean`](../PLean/Surface/Verify.lean), [`Internal/Decls.lean`](../PLean/Internal/Decls.lean), ~1 day)*

Three new top-level commands:

```lean
syntax (name := pLemmaDecl) "Lemma " ident "{" pLemmaBodyItem* "}" : command
syntax (name := pTheoremDecl) "Theorem " ident "{" pLemmaBodyItem* "}" : command
syntax (name := pProofDecl) "Proof " (ident)? "{" pProofItem* "}" : command

declare_syntax_cat pLemmaBodyItem
syntax (name := pLemmaInvariant) "invariant " ident " : " term : pLemmaBodyItem

declare_syntax_cat pProofItem
syntax (name := pProofProve)
  "prove " ident (" using " ident,+)? ";" : pProofItem
syntax (name := pProofDefault) "prove " "default" (" using " ident,+)? ";" : pProofItem
```

New registry records: `PLemmaDecl`, `PTheoremDecl`, `PProofDecl`.
Each `prove X` directive becomes a `(target : Name, using : Array
Name)` pair. `prove default` is a sentinel.

Materialisation (in `Commands/GenModule.lean` step pipeline):
- After per-handler defs and per-machine accessors land, emit
  `def <Lemma>.bundle : PProp Sig := fun s => <prop1> ∧ <prop2> ∧ ...`
  for each Lemma/Theorem.
- Each individual `invariant a : P` keeps its existing emission so
  `using` references resolve to the named prop.

Exit: parse-only test (no proof yet) where a `Lemma`/`Proof` block
type-checks and shows up in `#print_pmodule`.

### 4. **`InitConditions` aggregation** — D21
*([`Commands/GenModule.lean`](../PLean/Commands/GenModule.lean), ~½ day)*

After all `init-holds` directives are registered, emit
`def <Mod>.InitConditions : PProp Sig := fun s => <conj>` (or `True`
if no inits were declared). Available to obligation generation in
step 7.

`paxiom` decls keep their current materialisation (Lean `axiom`
declarations, unconditionally available).

Exit: `#check @<Mod>.InitConditions` resolves; manual triple test
verifies init-conditions hold across a handler.

### 5. **`pverify` tactic** — D22
*(`Verify/Tactic.lean`, ~1.5 days — the meat)*

The PLean `loom_solve`-equivalent. Composition:
1. `pverify_unfold` — unfold the handler def, all referenced
   invariants, `Inv`/`InitConditions`/`DispatcherContract` aliases,
   `is_<ev>` predicates, `<MName>.allocated`, `precedes`,
   `inflight`/`sent`/`received`/`stateOf`, plus the three
   default-invariant defs. Driven by an `@[pverifyUnfold]` attribute
   so user invariants can opt in.
2. `pverify_wp` — `wpgen <;> first | apply WPGen.default | skip`,
   then `simp only [GlobalState.addSent, GlobalState.bumpActionCount,
   GlobalState.addReceived, GlobalState.updateMachine,
   GlobalState.initial]` to compute the post-state.
3. `pverify_intro` — `loom_logic_simp`/`loom_intro`/`loom_split` chain
   borrowed shape-wise from `CaseStudies/Tactic.lean` but without the
   `WithName` registration.
4. `pverify_solve` — per-VC: try `grind`, fall back to `loom_smt
   [<hints>]` if `loom.solver` is set to `cvc5`/`z3`.
5. `pverify_report` — for `pverify!`, log unsolved goals with the
   originating `prove X` directive's name as case tag.

The `pverify using L1, L2` form passes `L1`/`L2`'s invariants as
`have` hypotheses before step 4.

Exit: `pverify` reproduces M2's manual proof tail on a per-handler
basis, replacing the M2 hand-written tactic block with a single
`pverify`. M2 still verifies.

### 5b. **`default_inv` automation** — D28
*(`Verify/Tactic.lean` alongside `pverify`, ~1 day)*

Companion tactic to `pverify` for the three default-invariant goals
(`UniqueActions` / `IncreasingCount` / `ReceivedSubsetSent`). The
proof shape is mechanical and depends only on the handler's
primitive footprint (which `addSent`/`addReceived`/`bumpActionCount`/
`updateMachine` calls compose the post-state). Hand-written M1 and M2
([`HandPingPong.lean:188-214`](../Tests/Semantics/HandPingPong.lean#L188-L214))
show the same `simp + rcases + Nat.lt_succ_*` pattern repeating per
handler.

Implementation order:
1. **Three mini-tactics**, one per default conjunct:
   - `unique_actions_step` — handles the `(a, b)`-pair case split.
   - `increasing_count_step` — handles the single-label new-vs-old
     case split.
   - `received_subset_sent_step` — handles the received-vs-sent
     containment.
   Each is a flat `match-on-goal` tactic with the per-primitive case
   table (no `send` / one `send` / one `goto` / `markReceived` +
   `send`).
2. **`default_inv` driver** — unfolds `DefaultInvariants` and the
   three conjuncts, splits with `refine ⟨?_, ?_, ?_⟩`, runs the
   three mini-tactics in sequence. On any mini-tactic failure, leave
   the residual goal with a `default_inv/<conjunct>/<handler>` case
   tag so `pverify!` can attribute it.
3. **Wire into `pverify`** — `pverify`'s per-VC step (D22, step 4)
   tries `default_inv` *first* whenever the goal's target is
   syntactically a default-invariant conjunct (detected by
   `head_symbol_is UniqueActions / IncreasingCount /
   ReceivedSubsetSent`). On failure, fall through to grind.

The implementation is small (~150 lines) and stable across solver
versions because it doesn't depend on `grind` or SMT — the rcases
chain is bounded (24 leaf cases max).

Exit: M1's hand-written 30-line per-handler default-invariant proof
block becomes `default_inv` (one line). M2's reductive cases collapse
similarly. A regression test in `Tests/Surface/PVerifyTactic.lean`
runs `default_inv` on every (handler, default-conjunct) pair from
M1 / M2 and checks `<50ms` per goal.

### 6. **Handler `_wrapped` form with markReceived** — D27
*(`Verify/Wrapper.lean`, ~½ day)*

For each (machine, state, event) handler, generate:
```lean
def M.S.<ev>_handler_wrapped (this : M) (lbl : Sig.Label)
    (hMatches : ∃ p, lbl.action = .event (E.<ev> p)) :
    PM Sig Unit := do
  markReceived (P := Sig) lbl
  -- extract payload from hMatches via Classical.choose
  M.S.<ev>_handler this (Classical.choose hMatches)
```

The `Classical.choose` is fine — we're not extracting computational
content, just feeding the user-handler the right value.

Exit: the wrapped handler exists; its triple has the M1-shape pre-
condition (with `inflight lbl` etc.), no existential-witness gymnastics.

### 7. **Obligation generator** — D18, D23, D24, D25
*(`Verify/Obligation.lean`, `Verify/Sanity.lean`,
`Verify/DispatcherContract.lean`, ~2 days)*

The big piece. For each `Proof <name>? { prove X using Y, Z; ... }`
directive:

For each `prove X using Y, Z;`:
- For each `(M, S, ev)` handler:
  - Build pre = `X.bundle s ∧ Y.bundle s ∧ Z.bundle s ∧
    InitConditions s ∧ DispatcherContract M S ev this lbl`.
  - Build post = `fun _ => X.bundle ∧ InitConditions`.
  - Emit `theorem M.S.<ev>_correct_X : triple <pre> M.S.<ev>_handler_wrapped
    <post> := by pverify using Y, Z`.
- Plus, separately, for each top-level `invariant a : P` not in any
  Lemma/Theorem (Phase-0/2 still allows these): one obligation per
  handler with that invariant in the post.

For `prove default;`:
- For each `(M, S, ev)`: `theorem M.S.<ev>_default :
  triple (fun s => DefaultInvariants s ∧ InitConditions s ∧
  DispatcherContract ...) M.S.<ev>_handler_wrapped
  (fun _ => DefaultInvariants ∧ InitConditions) := by pverify`.

`DispatcherContract` builder (`Verify/DispatcherContract.lean`)
takes the handler's signature (machine wrapper type, payload type,
event ctor) and produces the precondition existential.

`using` cycle detection: build a graph of `prove X using Y` edges,
DFS, error on back-edges.

Exit: M3 — `Tests/Surface/Phase3DistributedLock.lean` verifies via
`#pverify` only, no hand-written `theorem ..._correct`.

### 8. **`#pverify` rewires to obligation gen** — D17 graduates
*([`Commands/PVerify.lean`](../PLean/Commands/PVerify.lean), ~½ day)*

[`#pverify M`](../PLean/Commands/PVerify.lean#L54-L73):
1. Run [`#pwf`](../PLean/Commands/PWf.lean#L113-L116).
2. Run handler-def existence check (Phase 2).
3. **NEW**: walk `Proof` directives, hand off to
   `Verify.Obligation.synthesise`, count successes/failures.
4. Report: `"M: 13 invariants × 5 handlers = 65 VCs (62 ✓, 3 ✗ —
   prove safety using kondo failed at Coordinator.WaitForResponses.eYes)"`.
5. If any failed, throwError (so CI fails).

The "report" output drives `#guard_msgs`-based regression tests.

Exit: `#pverify` on M3's DistributedLock prints success; on a
deliberately broken invariant prints which (handler, lemma, VC)
failed.

### 9. **M3 benchmarks** — the exit
*(`Tests/Surface/Phase3*.lean`, ~2 days)*

Port three Tutorial/Advanced benchmarks to PLean surface:
1. [`6_DistributedLock`](../../../Tutorial/Advanced/6_DistributedLock/PSrc/System.p) —
   smallest. Exercises `inflight`, `e.source`, `e targets m`, basic
   `Theorem`/`Proof`, `prove default`. **40-line input → expect ~50-line
   PLean port.**
2. [`8_LockServer`](../../../Tutorial/Advanced/8_LockServer/PSrc/System.p) —
   exercises **machine-kind `is`** (D20), `pure lock_server(): machine`
   (already supported), multiple `Lemma`s. **96 → ~110 lines.**
3. [`3_RingLeaderVerification`](../../../Tutorial/Advanced/3_RingLeaderVerification/PSrc/System.p) —
   exercises **`Lemma using` chain** with three lemmas (`less_than`,
   `between_rel`, `right_rel`) referenced from a fourth (`lemmas`)
   referenced from the `Theorem Safety`. Tests the obligation
   generator's `using`-chain correctness. **92 → ~110 lines.**

Each port is a parallel file in `Tests/Surface/Phase3*.lean`; the
P source lives untouched in `Tutorial/Advanced/`. Each port `#pverify`s
clean, no hand-written triples.

If 3_RingLeaderVerification doesn't make it (likely culprit:
`btw`/`right` axiomatization needs `paxiom`/`pinstance` over functions,
which we have but haven't stressed), drop it down to bonus and ship
1 + 2.

Exit: M3 ☑ in STATUS.md.

### 10. **Stretch — `prove default` for the default invariants only**
*(~½ day, can be parallel with #9)*

Some benchmarks use `prove default;` standalone (just the default
invariants). Make sure the path that emits *only* default-invariant
obligations (no user lemma involved) works and is cheap. This is the
common case in `8_LockServer` (`Proof Safety { prove safety; prove
default; }`) — `prove default` adds no `using` clause but must still
emit one obligation per handler.

---

## Exit criterion (M3)

From [`PLAN.md` § Phase 3](PLAN.md#phase-3--verification-declarations-4-days):

> Walk registry → synthesize per-handler `@[loomSpec]` lemmas; ship
> `pverify` tactic; default obligations.

Concretely, after Phase 3:

- **M3-DistributedLock**: `Tests/Surface/Phase3DistributedLock.lean` contains
  the `pmodule DistributedLock { ... }` translation of
  [`Tutorial/Advanced/6_DistributedLock/PSrc/System.p`](../../../Tutorial/Advanced/6_DistributedLock/PSrc/System.p),
  followed by `#gen_module DistributedLock; #pverify DistributedLock`.
  No hand-written `theorem`. The file builds clean, `#pverify` reports
  "X invariants × Y handlers = Z VCs, all ✓".
- **M3-LockServer**: same, for
  [`Tutorial/Advanced/8_LockServer/PSrc/System.p`](../../../Tutorial/Advanced/8_LockServer/PSrc/System.p).
  Exercises `m is Server`/`m is Node` machine-kind checks (D20) and
  multi-Lemma `using` chains. Build + `#pverify` clean.
- **M3-RingLeader** (stretch): same, for
  [`Tutorial/Advanced/3_RingLeaderVerification/PSrc/System.p`](../../../Tutorial/Advanced/3_RingLeaderVerification/PSrc/System.p).
  Heavy axioms (`paxiom` over uninterpreted `pure le`/`btw`/`right`)
  + four-lemma `using` graph.
- The `pverify` tactic stands alone: a unit-test file
  `Tests/Surface/PVerifyTactic.lean` runs it on synthetic small triples
  (no obligation gen) and confirms the same proofs M2 wrote by hand
  now go through with `by pverify`.
- M2's [`Tests/Surface/Phase2PingPong.lean`](../Tests/Surface/Phase2PingPong.lean) is **rewritten** to use
  `#pverify` instead of hand-written triples. The hand-written form
  is moved to `Tests/Surface/Phase2PingPong_manual.lean` as a
  regression for the manual proof shape. Both still build.
- `#pverify` on a deliberately-broken invariant in any of the M3
  files reports the failing (handler, lemma, VC) triple via a clear
  diagnostic.
- STATUS.md: Phase 3 → ☑, M3 ☑, decision-log entries D18–D27.

Phase 4 then layers spec machines on top: parse `spec X observes
[evs] { ... }`, flatten to global vars + handler procedures, generate
spec-correctness obligations whenever an observed event is sent.

---

## Risks / things to watch

Inherits PLAN_P2's residual list (R8–R14). New risks specific to
Phase 3:

- **R15 — `pverify` stalls on benchmarks beyond DistributedLock.**
  `grind` is fast for arithmetic + boolean reasoning but can fail on
  benchmarks involving set/quorum reasoning (Paxos, Consensus). When
  `pverify`'s grind tail can't close a goal, `pverify!` reports it and
  the user falls back to `pverify using L1, L2 with cvc5` (an SMT
  fallback). Not a Phase-3 blocker for M3 — M3 picks benchmarks that
  don't need quorum reasoning. *Mitigation*: keep the `loom_smt`
  fallback in the `pverify` pipeline (off by default) so power users
  can engage it; Phase 6 stresses it on Paxos.

- **R16 — Obligation count blowup.** Per-handler × per-invariant ×
  per-Proof-directive can produce dozens or hundreds of obligations.
  `8_LockServer` has 13 invariants × 5 handlers = 65 VCs. Paxos has
  ~80 invariants × ~15 handlers = 1200 VCs. Each VC requires
  `wpgen` + simp + grind. *Mitigation*: emit obligations as Lean
  `theorem`s but cache the unfolded WP form across handlers
  (handlers within the same machine share their post-state shape).
  Phase 3 ships without this caching — if M3 build times exceed 30s,
  cache as a follow-up.

- **R17 — `using` resolution timing.** `Proof { prove X using Y; }`
  references Y by name; Y must already be `prove`d (and its theorem
  exists) for `pverify using Y` to find the lemma in the local
  context. The `Proof` directive list must be processed in order,
  and within a `Proof` block the `using` references look only
  *backwards*. Cross-`Proof`-block references look across the whole
  module. *Mitigation*: two-pass — first pass collects all `prove`
  directives, second pass topologically sorts and emits.

- **R18 — `paxiom` over `pure` functions doesn't auto-flow into
  `pverify`.** RingLeader axioms (`init-condition forall (x: machine)
  :: le(x, x)`) are `paxiom`s over the uninterpreted `pure le(...)`
  function. They live as Lean `axiom` decls; `grind`/`loom_smt` need
  them as hints. *Mitigation*: `pverify` automatically pulls every
  `paxiom` and `init-condition` into the goal context as a hypothesis
  (via `have`-binding). This is the same pattern Loom uses
  internally.

- **R19 — Macro-emitted `theorem`s can't be re-elaborated.** If
  `#pverify` runs `Verify.Obligation.synthesise` and a single
  obligation fails, Lean's `theorem ... := by pverify` emits the
  failure as a *theorem-level* error rather than letting `#pverify`
  catch it. *Mitigation*: synthesise as a `noncomputable def proof :
  triple ... := ?_` shape and use `Lean.Elab.Term.elabTermAndSynthesize`
  with a `MessageLog` capture so failures are individually attributable.
  Or simpler: emit each obligation in a `try ... catch` and accumulate
  a failure report.

- **R20 — `MachineState.kind` field default = 0 collides with
  uninitialised refs.** A handler running on a machine that
  `#gen_module` emitted with `<M>_kind = 0` would also satisfy
  `m is <M0>` for the *first declared* machine after a default-
  initialised allocation. *Mitigation*: reserve `<M>_kind = 0` for
  "uninitialised", make all real machine kinds `≥ 1`. The
  `<M>.allocated` predicate explicitly checks `kind ≠ 0 ∧ kind =
  <M>_kind`. Tests for this go in `Tests/Surface/Phase3*` and
  `ObligationShape.lean`.

- **R21 — `default_inv` doesn't cover a primitive's footprint.**
  The tactic's case table enumerates the supported handler primitive
  shapes (`pure ()` / one `send` / one `goto` / one `markReceived`
  pair / etc.). A handler that calls a primitive combination outside
  that table (e.g., a future `broadcast` that adds *N* labels at
  once, or a `goto` inside a conditional) leaves
  `default_inv` unable to match and falls through to `pverify`'s
  grind tail, which may also fail. *Mitigation*: enumerate the
  supported primitives explicitly in `Verify/Tactic.lean`'s case
  table; on a no-match case, emit a tactic-level error directing
  the user to extend the table. Phase 5 (`foreach`/`while`) will
  introduce loops; the case table will need a "loop body composes
  with `addSent` *N* times" generalisation that's beyond Phase 3's
  scope.

---

## Hand-off to Phase 4 and beyond

> **Phase 4 plan** — see [`PLAN_P4.md`](PLAN_P4.md). Phase 4 owns spec
> machines (D26 deferred to it) *and* the residual P3 items collected
> under "Deferred from REVIEW_P3" — most importantly R15
> (per-accessor `#derive_lifted_wp` + per-primitive `loomSpec`
> lemmas), which gates every M3 benchmark beyond
> Phase3PingPong/trivial.


Phase 4 (Spec machines) needs:
- The handler-wrapper pattern from D27 — spec handlers fire on
  observed events, so `markReceived` injection still applies.
- `<Mod>.MKind` from D20 — spec machines are also kinds, just with
  no constructors that the user can `new`.
- The obligation generator from D23 — extended with a "spec-handler
  fires alongside every `send` of an observed event" obligation.

Phase 5 (Remaining surface) — partial; status as of 2026-06-26:
- `foreach (x in S) invariant <inv>; { body }` — **shipped 2026-06-26**.
  Desugars to `PLean.pforeach xs invList (fun x => do body)` with a
  PLean-local `@[loomSpec] WPGen.pforeach` (proven by reduction to
  Loom's `triple_forIn_list`). User-supplied invariants are
  state-implicit `PProp Sig` predicates; `forall new (e: event)`-style
  frame conditions are not yet auto-derived (user writes them
  explicitly).
- `while` inside handler bodies — **shipped 2026-06-26**. Surface form
  `while (cond) invariant N : I; [done_with …;] [decreasing …;] {
  body }` lowers to a `Lean.Loop.mk`-driven `for`-block matching
  Loom's `@[loomSpec] WPGen.forWithInvariantLoop`. `pverify_step_wp`
  carries the `Pi.inf_apply` / `inf_Prop_eq` simp set that reduces
  the post-`wpgen` lattice meet to a `Prop`-level conjunction SMT
  decides. `if` inside loop bodies still falls through to
  `WPGen.default` (no `WPGen.if` for `DivM`-backed `PM`).
- Auto-default under loops. Loop-bearing handlers report `[SMT:
  counter-example]` on the auto-emitted `prove default;` obligation
  unless the user pins `DefaultInvariants`-strength clauses as part
  of the loop invariant. Future work: a loop-aware `default_inv`
  recognising the post-`forWithInvariantLoop` shape.
- `assume <prop>` — a Lean `Classical.byContradiction` plus assumption,
  registered to flow into `pverify`'s context.
- `assert <prop>` — only legal inside spec machines (Phase 4); errors
  otherwise.
- Map / set / seq operations beyond plain membership — **shipped
  2026-06-25** (see [`STATUS.md`](STATUS.md)'s 2026-06-25 session and
  [`Examples/ShardedKV`](../Examples/ShardedKV.lean)). The
  ChainReplication / Paxos spec-level uses remain gated on Phase-4
  spec machines.

Phase 6 (Tutorial port):
- Port [`Tutorial/1_ClientServer`](../../../Tutorial/1_ClientServer/) (M4)
  and [`Tutorial/2_TwoPhaseCommit`](../../../Tutorial/2_TwoPhaseCommit/) (M5).

- These exercise the *user-facing* tutorials, not the verified
  benchmarks under `Advanced/`. They're shorter and ship with
  P-checker tests that need PLean equivalents.
- Stretch: port `4_Paxos` from `Tutorial/Advanced/`, the flagship
  proof.

---

## References

**In-repo** (clickable):

- [`PLAN.md`](PLAN.md) — overall plan
- [`PLAN_P1.md`](PLAN_P1.md) — Phase 1 detailed plan (semantic core)
- [`PLAN_P2.md`](PLAN_P2.md) — Phase 2 detailed plan (registry +
  surface), incl. R14 (machine-kind quantification, resolved here as D20)
- [`STATUS.md`](STATUS.md) — living tracker
- [`HandPingPong.lean`](../Tests/Semantics/HandPingPong.lean) — M1
  hand-written triples; M3 obligations should match this shape
- [`Phase2PingPong.lean`](../Tests/Surface/Phase2PingPong.lean) — M2
  surface triples, hand-written; M3 replaces these with `#pverify`
- [`Surface/Verify.lean`](../PLean/Surface/Verify.lean) — extend with
  `Lemma`/`Theorem`/`Proof`
- [`Commands/PVerify.lean`](../PLean/Commands/PVerify.lean) — rewire
  to obligation gen

**Tutorial benchmarks** (target verification set):

- [`Tutorial/Advanced/6_DistributedLock`](../../../Tutorial/Advanced/6_DistributedLock/) — M3 minimum
- [`Tutorial/Advanced/8_LockServer`](../../../Tutorial/Advanced/8_LockServer/) — M3 machine-kind exercise
- [`Tutorial/Advanced/3_RingLeaderVerification`](../../../Tutorial/Advanced/3_RingLeaderVerification/) — M3 lemma-chain exercise
- [`Tutorial/Advanced/5_Consensus`](../../../Tutorial/Advanced/5_Consensus/) — Phase 5 (foreach)
- [`Tutorial/Advanced/2_TwoPhaseCommitVerification/Single`](../../../Tutorial/Advanced/2_TwoPhaseCommitVerification/Single/) — Phase 5 (foreach)
- [`Tutorial/Advanced/1_ChainReplicationVerification`](../../../Tutorial/Advanced/1_ChainReplicationVerification/) — Phase 4 (spec) + Phase 5 (maps)
- [`Tutorial/Advanced/4_Paxos`](../../../Tutorial/Advanced/4_Paxos/) — Phase 6 stretch
- [`Tutorial/Advanced/7_ShardedKV`](../../../Tutorial/Advanced/7_ShardedKV/) — Phase 5 (sharding via maps)

**Loom dependency** (vendored under the build tree; cited by module
path + def name):

- `Loom.MonadAlgebras.WP.Tactic.wpgen` — main proof-stepping tactic
- `Loom.MonadAlgebras.WP.Basic.WPGen` — fallback for un-derived ops
- `Loom.SMT.loom_smt` — SMT-backed solver tail
- `CaseStudies.Tactic.loom_solve` — *reference only*; PLean re-implements
  the equivalent in `Verify/Tactic.lean` without the
  `WithName`/`bdef`/`prove_correct` Cashmere scaffolding (decision D3
  from PLAN_P1). This was confirmed empirically at end of Phase 2 —
  `loom_solve` requires Cashmere's assertion-name registry which
  PLean doesn't produce.
