# PLean

Multi-modal auto-active formal verification of
[P](https://p-org.github.io/P/) models in Lean 4. PLean embeds (a large subset of) P language in Lean 4. It elaborates each
program into per-handler Hoare-triple obligations and discharges them
across three modalities: SMT (via [Loom](https://github.com/verse-lab/loom)
to cvc5 / z3), user-supplied tactic proofs registered through
`@[pverifyProof]`, and a persistent proof cache that re-applies prior
SMT successes on warm rebuilds. 
```lean
import PLean

open PLean PartialCorrectness DemonicChoice

pmodule PingPong
  system s

  event ePing : PLean.MachineRef
  event ePong

  machine Server {
    start state Idle {
      on ePing (replyTo : PLean.MachineRef) {
        send replyTo, ePong
      }
    }
  }

  machine Client {
    start state Booting { ignore ePong }
  }

  Theorem safety {
    invariant pong_after_ping :
      ∀ q : ePong, s.sent q = true →
        ∃ p : ePing, s.sent p = true ∧ p ≺ q
  }
  Proof Safety { prove safety using default ; prove default ; }
end PingPong

#gen_module PingPong
#pwf        PingPong
#pverify    PingPong
```

## Features

- **Auto-interactive proving.** `#pverify M` synthesises one Hoare
  triple per `(machine, state, event)` and discharges it via cvc5 /
  z3 through Loom; when SMT can't close a goal, a theorem tagged
  `@[pverifyProof]` registers a manual proof that type-checks
  against the generator's emitted statement, so soundness can't drift
  between the auto and manual paths.
- **P-style proof scripts.** `Lemma` / `Theorem` bundles of
  `invariant` clauses, `Proof { prove G using L1, L2 ; }` with
  acyclic `using` chains, `prove default ;` for the framework-level
  monotonicity obligations — the verification surface tracks P's
  PVerifier blocks one-to-one.
- **Persistent proof cache.** Successful SMT discharges are hashed
  by `(lctx, goal target)` and re-applied across warm rebuilds via
  Loom's `trust_smt` axiom — ~14× faster on SMT-heavy files.

## Build

PLean is its own Lake project — work from inside `Src/PLean/`:

```bash
cd Src/PLean
lake build PLean                     # the library
lake build Tests                     # all regressions
lake build Examples                  # the verified protocol ports
lake build Examples.DistributedLock  # one specific protocol
```

The first build downloads `z3` (4.15.4) and `cvc5` (1.3.1) into
Loom's build directory so `loom_smt` resolves them; Lean is pinned
to **v4.24.0** in `lean-toolchain`.

## Auto + interactive proving

`#pverify M` is the surface command. For each `(machine, state,
event, prove-directive)` it (a) consults `@[pverifyProof]` for a
user-supplied theorem matching the obligation's generated name,
(b) emits `theorem … := by first | <chain> | sorry` if no manual
proof is registered, (c) inspects the elaborated value for `sorry`
to classify the outcome, and (d) summarises pass / manual / failure
counts in a single report with copy-paste manual-proof skeletons for
the gaps.

### 1. The proof script: `Lemma` / `Theorem` / `Proof`

```lean
Lemma framework {
  invariant inc_count :
    ∀ a : Sig.Label, s.sent a = true → a.actionCount < s.actionCount
}
Proof { prove framework ; }

Theorem safety {
  invariant pong_after_ping :
    ∀ q : ePong, s.sent q = true →
      ∃ p : ePing, s.sent p = true ∧ p ≺ q
}
Proof Safety {
  prove safety using framework ;
  prove default ;
}
```

- A `Lemma` / `Theorem` block bundles one or more `invariant` clauses.
  All clauses in one bundle must be mutually inductive (each one's
  preservation step may assume the others in the pre-state).
- A `Proof` block lists `prove <Bundle> [using <L1>, <L2>, …] ;`
  directives. `using` premises become pre-state assumptions on the
  cited bundle's obligations. The chain must be acyclic; cycles are
  flagged at parse time.
- `prove default ;` emits the three default-invariant obligations
  (`UniqueActions`, `IncreasingCount`, `ReceivedSubsetSent`) per
  handler, capturing the framework-level monotonicity guarantees.
- `system s` declares the pmodule's state binder; every subsequent
  invariant / `init-holds` / `paxiom` body may reference `s` as the
  live state. Field-projection sugar (`n.held`, `e.target`) and
  runtime kind guards (`is_<M> n.ref s`) are injected automatically.

### 2. Automated discharge

Each obligation flows through Loom's `wpgen`-based weakest-
precondition computation, a pre-SMT normalisation chain
(`pverify_smt_prep`: destructure `GlobalState`, simp the
`@[pverifySimp]` lemmas, abstract `(payload_of e).<field>` lookups),
and `loom_smt [*]`. Most invariants in the verified benchmarks close
this way; `#pverify` reports each obligation's outcome and a summary
line:

```
PingPongAuto: 6 obligations from 2 prove-directives
── passed ──
  ✓ PingPongAuto.base_Safety_pong_after_ping  [SMT]
  ✓ PingPongAuto.Server.Idle.ePing_correct_Safety_safety_using_default  [SMT]
  ✓ PingPongAuto.base_Safety_UniqueActions  [SMT]
  ✓ PingPongAuto.base_Safety_IncreasingCount  [SMT]
  ✓ PingPongAuto.base_Safety_ReceivedSubsetSent  [SMT]
  ✓ PingPongAuto.Server.Idle.ePing_correct_Safety_default  [SMT]
PingPongAuto: 6 proved by SMT, 0 user-proved, 0 disproved, 0 unknown,
              0 tactic-error, 0 no-diagnostic, 0 missing-premise
```

When a single-shot SMT query returns `unknown`, `pverify_split_smt`
flattens the bundle's top-level `∧` chain and dispatches per
conjunct. Beyond ~5–7 conjuncts the single-shot path empirically
tips into `unknown`, so split-then-discharge is often the right next
step before reaching for a manual proof.

A genuinely-broken obligation surfaces as a counter-example, with a
copy-paste skeleton attached:

```
BrokenLock: 3 obligations from 1 prove-directives
── failed ──
  ✗ BrokenLock.Node.Act.eGrant_correct_block0_safety_unique_holder  [SMT: counter-example]
      counter-example:
        machines:
          machine[3] = Node@Act(epoch=0, held=true)
          machine[7] = Node@Act(epoch=5, held=true)
        sent (ordered by actionCount):
          []
        witnesses:
          n1 = Node#3, n2 = Node#7
── manual-proof skeletons ──
  @[pverifyProof] theorem BrokenLock.Node.Act.eGrant_correct_block0_safety_unique_holder
      (this : Node) (param : tGrant_payload) (lbl : Sig.Label) : … := by
    sorry
── passed ──
  ✓ BrokenLock.Node.Act.eAccept_correct_block0_safety_unique_holder  [SMT]
  ✓ BrokenLock.base.block0.safety.unique_holder                       [SMT]
BrokenLock: 2 proved by SMT, 0 user-proved, 1 disproved, 0 unknown,
            0 tactic-error, 0 no-diagnostic, 0 missing-premise
1 obligation(s) need a manual proof; fill in the skeletons above.
```

Outcome tags: `[SMT]`, `[manual]`, `[SMT: counter-example]`,
`[SMT: unknown]`, `[tactic error]`, `[no diagnostic]`,
`[missing premise]` (a `using <L>` cites a lemma that no `prove`
directive proves — fill in the missing `prove <L> ;` directive).

### 3. Manual proofs via `@[pverifyProof]`

When the goal genuinely needs Lean-level reasoning (lean-auto's
"Higher order input?" rejection on quantified payload extractors;
case-splits the solver isn't finding), copy the failing obligation's
skeleton from `#pverify`'s output, fill it in, and tag it:

```lean
set_option maxHeartbeats 4000000 in
@[pverifyProof]
theorem LockServer.Server.Idle.eRelease_correct_safety_using_topology
    (this : Server) (param : tRelease_payload) (lbl : Sig.Label) :
    triple (l := PProp Sig)
      (fun s => …)
      (do PLean.markReceived (P := Sig) lbl;
          Server.Idle.eRelease_handler this param)
      (fun _ s => …) := by
  unfold Server.Idle.eRelease_handler
  pverify_step_wp
  intro s hpre
  simp only [safety, unique_holder, …] at hpre
  obtain ⟨hUH, hURP, …⟩ := hpre
  refine ⟨?case1, ?case2, …⟩
  case case1 => pverify_split_smt
  case case2 => pverify_inflight_by hinfe using hNew => …
  …
```

The registry is **type-checked** — a `@[pverifyProof]` theorem whose
statement doesn't match the generator's emitted obligation is
rejected at the attribute site, not silently accepted. This was a
soundness gap in earlier revisions, now pinned by
`Tests/Syntax/SoundnessRegression.lean`.

See [`docs/ProofSkill.md`](docs/ProofSkill.md) for the manual-proof
workflow end-to-end: finding inductive invariants, the
counter-example → invariant iteration, dealing with the
higher-order-rejection cases, and the `using`-chain dependency
ordering.

### 4. Proof caching

Successful SMT discharges write a fingerprint to
`<project>/.lake/build/pverify_cache/<hash>.ok`. The hash is over
the canonical (macro-scope-stripped) pretty-print of every visible
local hypothesis's type plus the goal target, so a hit certifies
that *this* obligation with *these* premises previously cleared SMT.
On a hit, the obligation closes directly via Loom's `trust_smt`
axiom — bypassing prep, the lean-auto translation, and the solver
invocation.

Warm rebuilds save ~11–14% on SMT-heavy files. Reset with
`lake clean`, or `set_option pverify.cache false` to disable.
Soundness is pinned by `Tests/Verify/CacheSoundness.lean`.

A complementary profile mode (`set_option pverify.profile true`)
times each obligation's stages (cache lookup, prep, lean-auto,
solver, assign) and emits a summary table after `#pverify`
completes.

## Tactics

The atomic `pverify_*` tactics in
[`PLean/Verify/Tactic.lean`](PLean/Verify/Tactic.lean) are the
user-facing primitives — `pverify` itself, `pverify_smt`,
`pverify_split_smt`, `pverify_step_wp`, `default_inv`, plus the
manual-proof helpers (`pverify_carry_after_recv`,
`pverify_not_inflight[_by]`, `pverify_inflight_by`,
`pverify_machine_has_type`).

See [TACTICS.md](TACTICS.md) for the full catalogue: each tactic's
Hoare-triple shape, argument conventions, and when to reach for it
vs. its neighbours.

## Examples

The [`Examples/`](Examples/) directory carries end-to-end protocol
ports. Each one verifies under `lake build Examples.<Name>`.

| Protocol | Obligations | Notes |
|---|---|---|
| [`Examples/DistributedLock`](Examples/DistributedLock.lean) | **12 / 12**  (all SMT) | Tutorial port of `6_DistributedLock` |
| [`Examples/LockServer`](Examples/LockServer.lean)           | **37 / 37**  (34 SMT + 3 manual) | `8_LockServer`; manual proofs are send-handler bundles |
| [`Examples/RingLeader`](Examples/RingLeader.lean)           | **17 / 17**  (14 SMT + 3 manual) | `3_RingLeaderVerification`; `pinstance` axiom bundle for `btw_*` |
| [`Examples/ClockBound`](Examples/ClockBound.lean)           | **59 / 59**  (all SMT) | Off-tree AWS clock-bound daemon port; exercises `PLean.choose` |
| [`Examples/ShardedKV`](Examples/ShardedKV.lean)             | **11 / 11**  (all SMT) | `7_ShardedKV`; exercises `map[K, V]` + multi-ref `unique_owner` |
| [`Examples/Consensus`](Examples/Consensus.lean)             | **23 / 23**  (17 SMT + 5 manual + 1 axiom) | Toy Paxos-style election safety under `unique_quorum` |
| [`Examples/PingPongAuto`](Examples/PingPongAuto.lean)       | trivial demo | Minimal `#pverify` auto-discharge |
| [`Examples/PingPongManual`](Examples/PingPongManual.lean)   | trivial demo | Hand-written triple |
| [`Examples/PingPong/`](Examples/PingPong)                   | trivial demo | Multi-file `pmodule` aggregation |

The longer benchmarks are worth reading end-to-end — `ClockBound`
for `PLean.choose` + per-target monotonicity, `ShardedKV` for the
hoisted-container `Containers` pattern, `Consensus` for `foreach`
broadcast and the `goto Won` inductive step. `LockServer` and
`RingLeader` carry the canonical send-handler manual-proof shapes
the `pverify_*` helpers were designed around.

## Contributing

Contributions are welcome. The roadmap to v1 — spec machines, the
remaining surface (`assume`, loop-aware `default_inv`), the
canonical Tutorial ports (`1_ClientServer`, `2_TwoPhaseCommit`) —
is laid out in [`docs/ROADMAP.md`](docs/ROADMAP.md), broken into
independently-pickable workstreams sized S / M / L.

If you're picking up a piece:

1. Read [`CLAUDE.md`](CLAUDE.md) (build cookbook, conventions, style
   guide) and [`docs/STATUS.md`](docs/STATUS.md) (current snapshot).
2. Pick a workstream from [`docs/ROADMAP.md`](docs/ROADMAP.md).
3. For verification work, read
   [`Tests/Syntax/PVerifyManualProof.lean`](Tests/Syntax/PVerifyManualProof.lean)
   to see the manual-proof workflow end-to-end, then
   [`docs/ProofSkill.md`](docs/ProofSkill.md) for the recurring
   patterns and the diagnostic chain for `unknown` results.
4. Build & test:

   ```bash
   cd Src/PLean
   lake build PLean
   lake build Tests Examples
   ```

5. Update [`docs/STATUS.md`](docs/STATUS.md) when a phase checkbox
   flips or a major work item lands.

Good first issues:
- Verifier polish in [`ROADMAP.md` § W5](docs/ROADMAP.md) — each
  item is independent and sized S.
- Porting another `Tutorial/Advanced/` benchmark.
- Adding a new `pverify_*` helper when a recurring proof shape
  shows up in two or more manual proofs.

The two soundness guards (`GlobalState`-shadowed binders rejected;
sorried `@[pverifyProof]` must fail) are load-bearing — don't
regress them. Before adding a tactic that closes more goals,
satisfy yourself it can't close *false* goals.

## Document index

| Document | Purpose |
|---|---|
| [`CLAUDE.md`](CLAUDE.md)                   | Build cookbook, conventions, surface keywords |
| [`TACTICS.md`](TACTICS.md)                 | Catalogue of `pverify_*` tactics and options |
| [`docs/STATUS.md`](docs/STATUS.md)         | One-page status snapshot |
| [`docs/ROADMAP.md`](docs/ROADMAP.md)       | Contributor-facing workstreams, sizing, sprint allocation |
| [`docs/ProofSkill.md`](docs/ProofSkill.md) | Manual-proof workflow + SMT pitfalls |
| [`docs/STYLE.md`](docs/STYLE.md)           | Source / proof / tactic style rules |
