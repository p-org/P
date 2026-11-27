# PLean

Formal verification of [P](https://p-org.github.io/P/) programs in
Lean 4. PLean embeds a large subset of the P language in Lean. You write
machines, states, events and invariants in P's syntax; PLean turns them
into an executable transition system and proves that the invariants hold
in every reachable state, the way P's PVerifier does.

```lean
import PLean

open PLean Std.WP

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
    start state Booting {
      ignore ePong
    }
  }

  Theorem safety {
    -- every pong answers an earlier ping
    invariant pong_after_ping :
      ∀ q : ePong, sent q s → ∃ p : ePing, sent p s ∧ p ≺ q

    -- where each event goes
    invariant ping_routing :
      ∀ e : ePing, sent e s → is_Server e.target s ∧ is_Client (ePing_payload_of e) s
    invariant pong_routing :
      ∀ e : ePong, sent e s → is_Client e.target s
  }

  Proof {
    prove safety ;
  }

end PingPong

#gen_module PingPong
#pverify    PingPong
```

## How it works

- `#gen_module M` builds the program: one handler per state and event,
  run on a model of machines and in-flight messages, in the style of
  [Verdi](https://verdi.uwplse.org/).
- `#pverify M` generates one proof obligation per handler and invariant,
  as PVerifier does. It proves them with `vcgen` and SMT solvers (z3 and
  cvc5, through [lean-crush](https://github.com/AD1024/lean-crush)), and
  checks the solvers' proofs in Lean where it can.
- The result is a Lean theorem, `M.safe`: every reachable state satisfies
  the invariants, and no handler fails (no failed `assert`, no unhandled
  event, no send to `null`).
- An obligation that is not proved is reported with a counterexample: the
  invariant that fails, the handler that breaks it, and a small state in
  which it does.
- Obligations are proved in parallel, and proofs are cached between runs
  (and checked again when reused): after an edit, only the goals it
  affects go to the solver.
- The model is executable, so the same program can be run and, later,
  model checked.

PLean is not a wrapper around PChecker or PVerifier.

## Build

PLean is its own Lake project (Lean v4.36.0). Work from inside
`Src/PLean/`:

```bash
cd Src/PLean
lake build PLean                     # the library
lake build Tests                     # the regression tests
lake build Examples.DistributedLock  # one example
```

The first build downloads z3 and cvc5 into `.lake/build/bin`; put that
directory first on your `PATH`. Verifying an example from scratch takes from
seconds to minutes; a run after an edit proves again only what the edit
affects.

## Examples

[`Examples/`](Examples/) has ports of P's tutorial protocols
(DistributedLock, LockServer, RingLeader, ShardedKV, Consensus,
TwoPhaseCommit, Paxos) and of a clock-bound service (ClockBound), and
models of Flexible Paxos (FlexiblePaxos) and of MultiPaxos (MultiPaxos).
Each one verifies with no manual proof, except Paxos, FlexiblePaxos and
MultiPaxos, where the steps that complete a quorum (and, in MultiPaxos,
the merge of recovered votes) are proved by hand;
[`docs/STATUS.md`](docs/STATUS.md) lists their numbers.

## Documents

| Document | Purpose |
|---|---|
| [`docs/STATUS.md`](docs/STATUS.md) | Current status and results |
| [`docs/ROADMAP.md`](docs/ROADMAP.md) | What is next |
| [`docs/ProofSkill.md`](docs/ProofSkill.md) | Writing invariants and reading `#pverify`'s report |
| [`CLAUDE.md`](CLAUDE.md) | Contributor guide: architecture, conventions, surface syntax |
| [`docs/STYLE.md`](docs/STYLE.md) | Code style |
