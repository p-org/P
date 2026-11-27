# PLean — Roadmap

Contributor-facing entry point. [`STATUS.md`](STATUS.md) carries the
current snapshot.

## Phases

| Phase | Status | Deliverable |
|---|---|---|
| P0 Toolchain | ☑ | Lean v4.36.0, lean-crush, bundled z3 / cvc5 |
| P1 Prototype | ☑ | Hand-written model and obligations (`Tests/Prototype/HandDL`) |
| P2 Semantics library | ☑ | `Semantics/*`: logs, containers, handler monad, worlds, steps, `Framework`, `step_ok`, `safety_of_step`, `fifo ⊆ bag`, ghost erasure |
| P3 Code generation | ☑ | `#gen_module` over the new model: handlers, entries, `prog`, `defer`, loops, locals, `choose` / `$`, `receive` |
| P4 Obligations | ☑ | Per-handler `RunPost` obligations, error freedom (`RunSafe`), never-receives, base cases, generic assembly into `M.safe` / `M.<T>_holds`; automatic `NoHalt` / `Transient` / `Waiting`; counterexamples |
| P5 Ports | ☑ | All examples verify (see `STATUS.md`) |
| P6 Explicit-state BMC | ☐ backlog | `#prun`, BFS with a visited set, coin enumeration, counterexample replay |

## Next

1. **Spec monitors.** P's `spec X observes …` as world state updated
   at send time; `assert` failures inside a monitor become error
   freedom obligations.
2. **Benchmarks.** `Tutorial/Advanced/1_ChainReplicationVerification`
   (needs spec monitors), `4_Paxos`.
3. **P6: explicit-state BMC** on the executable model.

## Smaller items

- `new M(x)` as an expression (the surface only has the statement
  `pnew M, x`).
- `receive` inside `if`, loops and functions (the continuation then
  re-enters the enclosing blocks).
- A solver-portfolio that runs z3 and cvc5 in parallel.
