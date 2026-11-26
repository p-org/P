# CLAUDE.md — PLean

Orientation for working in `Src/PLean/`. PLean is a separate Lake
project from the parent dotnet build (`Src/PCompiler/`); the
parent-repo `CLAUDE.md` (one level up) covers P / PChecker / PeasyAI.

## What PLean is

A port of the P language and its PVerifier verification backend into
Lean 4. The user surface mirrors P's grammar — machines, states, events,
invariants, the temporal `≺` precedence operator — and adds
verification-declaration blocks (`Lemma` / `Theorem` / `Proof`).

Programs elaborate to an executable transition system in the style of
Verdi (Wilcox et al., PLDI'15), designed in
[`docs/TransitionModel.md`](docs/TransitionModel.md):

- a **world** of machine states plus in-flight packets, and a **ghost
  world** that also logs every packet ever sent;
- **local handlers**: each handler is a do-notation program in
  `HandlerM` that sees only its own machine state and appends sends,
  spawned machines and announcements to logs; one generic `commit`
  applies them to the world;
- **verification** by core `Std.WP` / `vcgen` (one Hoare triple per
  handler and invariant), discharged by SMT through
  [lean-crush](https://github.com/AD1024/lean-crush) with checked proof
  reconstruction where possible;
- **execution**: `step` is computable, so model checking can be built
  on the same model (explicit-state BMC is the next phase).

The user-facing pipeline:
1. Author a `pmodule M { … }` (one or more files contribute fragments).
2. `#gen_module M` synthesises the program (types, handlers, `prog`) and
   the specification definitions from the registered metadata.
3. `#pwf M` reports well-formedness (a fast subset check).
4. `#pverify M` emits and discharges the obligations, then assembles
   `M.safe` (every reachable world satisfies `AllInv`) and
   `M.<T>_holds` per proved target.

PLean is **not** a wrapper around PChecker or PVerifier.

## Build

PLean is its own Lake project. Always work from inside `Src/PLean/`:

```bash
cd Src/PLean
lake build PLean                     # the library
lake build Tests                     # all regressions under Tests/
lake build Examples                  # protocol ports
lake build Examples.DistributedLock  # one specific protocol
```

`lake build` from the repo root will fail. The first build downloads
`z3` 4.15.4 and `cvc5` 1.3.4 into `.lake/build/bin/`; crush finds the
solvers on `PATH`, so add `<PLean>/.lake/build/bin` to `PATH`.
Toolchain pinned to **Lean v4.36.0** in `lean-toolchain`; the only
dependency is lean-crush (`bump/4.36.0`).

Examples are expensive to verify (minutes each). Build them one at a
time, and use `pverify.only` / `pverify.profile` (below) to iterate on
single obligations instead of re-running a whole example.

## Architecture

```
PLean/
  Internal/
    Decls.lean       -- metadata records (PTypeDecl, PEventDecl, ...)
    Registry.lean    -- env extension; cross-file pmodule aggregation
  Semantics/
    Log.lean         -- append-only logs (outbox, spawned machines, ...)
    Containers.lean  -- PSet / PMap history datatypes, card, support
    Core.lean        -- MachineRef, PError, MachineState, Packet
    Handler.lean     -- HState, HandlerM, the primitives (H.send, ...),
                        their vcgen specs, the loop frame
    World.lean       -- World / GWorld, Program, commit, step / gstep,
                        Reachable, Framework
    Theorems.lean    -- metatheory: adequacy, RunPost / RunSafe,
                        framework preservation, step_ok,
                        safety_of_step, fifo ⊆ bag, ghost erasure
    Msg.lean         -- invariant vocabulary: Msg, sent / inflight /
                        received / ≺ / targets / is, sentSince
  Syntax/
    Module.lean      -- pmodule M ... end M
    Types.lean       -- type N / enum N / type N = (...)
    Events.lean      -- event ev : T
    Machine.lean     -- machine M { var ...; state S { ... } }
    Stmt.lean        -- statements: send / announce / goto / raise /
                        pnew / assert / assignments / var / choose / if ($)
    Loop.lean        -- foreach / while with invariants
    Containers.lean  -- set[T] / map[K, V] / seq[T] / option[T]
    Verify.lean      -- invariant / Lemma / Theorem / Proof / system /
                        paxiom / init-holds / function / pinstance,
                        field-projection sugar, kind-guard injection
  Commands/
    GenModule.lean   -- #gen_module M
    PWf.lean         -- #pwf M
    PVerify.lean     -- #pverify M
    PrintModule.lean -- #print_pmodule M
  Verify/
    SimpAttrs.lean   -- the pverifySimp attribute
    Norm.lean        -- pverifySimp rules, pv_destruct, pv_cursor_mem
    Tactic.lean      -- pverify_close (vcgen + normalisation + crush)
    Cex.lean         -- counterexamples: a minimised z3 model of a goal
                        that is not proved, decoded into a world
    Progress.lean    -- #pverify's live progress (a widget polling an RPC method)

Examples/            -- protocol ports
Tests/               -- regressions; Tests.** is globbed
```

## Contributor docs

- [`docs/TransitionModel.md`](docs/TransitionModel.md) — the design:
  semantics, obligations, phases.
- [`docs/STATUS.md`](docs/STATUS.md) — current snapshot and closure
  numbers.
- [`docs/ROADMAP.md`](docs/ROADMAP.md) — what is next.
- [`docs/ProofSkill.md`](docs/ProofSkill.md) — writing inductive
  invariants and diagnosing obligations.

## Obligations

Per proved target `T` (`prove T using U₁ … ;`), with premises `T ∧ U₁ ∧ …`:

- `<M>.<S>.<ev>_correct_<T>` per handled / ignored event of each state,
  `<M>.<S>.entry_correct_<T>` per state, and
  `<M>.<S>.recv<k>_<ev>_correct_<T>` per `receive` case: after a handler
  that completes, `T` holds (`RunPost`);
- `base_<T>`: `InitConditions g → T g`.

Assuming every invariant (`AllInv`), once per module:

- `<M>.<S>.<ev>_safe` / `<M>.<S>.entry_safe` / `<M>.<S>.recv<k>_<ev>_safe`:
  the handler never fails (`assert`, `choose` on an empty range, …)
  (`RunSafe`);
- `<M>.<S>.<ev>_never` for each event a state neither handles, ignores
  nor defers: it is never delivered there.

`#gen_module` adds automatic targets: `NoHalt` (no machine ever halts),
when no machine can halt, `Transient` (a machine is never idle in a
state whose entry always ends in a `goto`) and `Waiting` (a machine
suspended at a `receive` is in that `receive`'s state). The framework invariants
(packet ids below the clock, in-flight ⊆ log, unallocated references)
are proved once, generically.

Initial worlds: every machine below `nextRef` is in its start state with
its start entry pending (as in PVerifier), and no machine is halted or
suspended; `init-holds` clauses constrain the rest.

## Surface keywords

PLean tracks P verbatim except where a P keyword collides with a Lean
reserved word or builtin. Mechanical replacements when porting `.p`:

| P keyword       | PLean surface          |
|-----------------|------------------------|
| `module`        | `pmodule`              |
| `axiom`         | `paxiom`               |
| `instance`      | `pinstance`            |
| `pure`          | `function`             |
| `init`          | `init-holds`           |
| `new M(x);` (as a statement) | `pnew M, x`  |
| `do` (in `on`)  | (dropped)              |
| `choose(e)`     | `let x ← choose e` (`e : Int` gives `[0, e]`; sets / maps give a member / key) |
| `if ($)` / `if ($$)` | same               |
| `var x : T;` in a body | same (a handler local, initialised to `default`) |
| `receive { case e: (x: T) { … } … }` | `receive { case e (x : T) => { … } … }` |

`Lemma`, `Theorem`, `Proof`, `prove`, `using`, `system` are PLean's
verification-declaration surface.

In invariants, an event binder `e : eV` ranges over sent messages
(`PLean.Msg E`: id, src, target, ev); `sent e s`, `inflight e s`,
`received e s`, `a ≺ b`, `e targets r`, `e is eV` and `stateOf r s`
(an `Option` of the state tag, e.g. `some Node.Won_st`) are the P
predicates. `n.<v>` is machine `n`'s field and `e.<f>` the payload
field (when `f` is a payload field of the binder's event).

### `receive`

A `receive` must be a top-level statement of a handler or entry body
(not inside `if`, a loop or another `receive` case). `#gen_module` splits
the body there: the code before it suspends the machine at the site,
saving the handler's parameter and the `var` locals declared before it
in the site's `W` constructor (`W.site<k>`), and each case resumes with
its body followed by the rest of the handler. While suspended, a machine
accepts only the cases' events (the others stay queued). Invariants can
talk about a suspended machine through `(s.machines r).waiting = some
(W.site<k> …)`.

### Container types

`set[T]` → `PSet T`, `map[K, V]` → `PMap K V`, `seq[T]` → `List T`,
`option[T]` → `Option T`. `PSet` / `PMap` are histories of updates
(`add` / `del`, `put` / `del`) with recursive lookups (`mem`, `get`),
`support` and `card` (P's `sizeof`). Mutation: `s += (e)`, `s -= (e)`,
`m[k] = v`, `m[k] += (e)`, `m[k] -= (e)`.

### Loops (`foreach` / `while`)

`foreach (x in xs) invariant N : I ; { body }` and `while (cond)
invariant N : I ; [done_with …;] [decreasing …;] { body }` desugar to
core `for … invariant` / `while … invariant … decreasing`. Every loop
invariant automatically includes a frame: what the body cannot change
(the machine's state when the body assigns no field, the outbox when it
sends nothing, …) and that earlier sends are kept. `new_sends e => P`
states that every message the loop has sent so far satisfies `P`; a
loop whose sends matter to an invariant needs one. In a `foreach`, the
loop variable is known to be a member of what is iterated. `while`
loops are proved terminating, so they need a `decreasing` measure.

## Conventions worth knowing

Source comments cover the *why* in detail. The points below are the
load-bearing invariants you need to respect when editing.

- **`system <σ>` declares the state binder for a pmodule.** Every
  subsequent `invariant` / `init-holds` / `paxiom` body may reference
  `σ` as the (ghost) world. Soundness guard (`rejectStateShadowIn`,
  pinned by `Tests/Syntax/SoundnessRegression.lean`) rejects bodies
  that name the world type in any binder.
- **Kind guards are auto-injected on quantifiers.** `∀ n : <M>, body`
  becomes `∀ n : <M>, is_<M> n.ref s → body` (and similarly for `∃` and
  event quantifiers). `is_<M> r s` says `r`'s state belongs to `<M>`.
- **Type parameters, not a signature value.** The model is generic in
  `S E F G W` (state tags, events, fields, entry arguments, suspended
  `receive`s); `#gen_module` emits these per pmodule. Crush rejects
  value-dependent types, so keep it that way.
- **Every executable handler gets an obligation**, and every unhandled
  (state, event) pair a never-receives obligation; the assembly
  (`step_AllInv`, `safe`) is a kernel-checked proof from them.
- **`#pverify` reports, it does not retry.** A failing obligation is
  left as `sorry` (so `<T>_holds` then depends on `sorryAx`); the
  report goes to `.lake/pverify/<Mod>.txt` with the goals crush failed
  on.
- **Tag new state-update helpers `@[pverifySimp]`.** The normalisation
  before crush uses this set.
- **Macro hygiene through `mkIdent`.** Identifiers that resolve against
  user-namespace constants (`E`, `G`, `this`, `<S>_st`, …) or against
  modules a quotation's file does not import must be built via
  `mkIdent` and spliced.

## Diagnosing obligations

- `set_option pverify.only "frag1,frag2" in #pverify M` runs only the
  obligations whose name contains a fragment (and skips the assembly).
- `set_option pverify.profile true` records per solver goal its time,
  the solver that closed it, crush's phase breakdown, and the goal
  itself when it took over 5 s, in the report file.
- Both work from the command line without editing the file:
  `lake env lean -Dweak.pverify.only=… -Dweak.pverify.profile=true Examples/X.lean`.
- `pverify.timeout` (seconds), `pverify.trust` (`reconstructOrTrust` by
  default: obligations whose proof could not be replayed are reported
  `✓ (trusted)`), `pverify.heartbeats`.
- Each obligation line says how its goals were closed (`[norm]`: nothing
  left after `vcgen` and normalisation; `smt n` / `pre n` / `hyp n`: by an
  SMT solver, by crush before SMT translation, as a hypothesis). In the
  editor, `#pverify` shows one progress message, updated in place: the
  obligations done, the one being proved, and the goal, invariant and
  solver it is on (`pverify.progress`, `PLean/Verify/Progress.lean`). The
  command line prints only the final report.
- A goal that is not proved gets a counterexample when z3 finds one
  (`pverify.cex`, on by default): the report names the failing invariant
  and the step, and shows a minimised pre-state (machines by kind with
  their state and fields, packets in flight and delivered). The SMT
  script and model go to `.lake/pverify/<Mod>/<obligation>.smt2`. See
  `docs/ProofSkill.md`.

## Style guide

See [`docs/STYLE.md`](docs/STYLE.md). Soundness rule of thumb: before
adding a tactic that closes more goals, satisfy yourself it can't close
*false* goals.

## Common operations

### Add a new test or example

- **Regression**: drop the file under `Tests/Bootstrap/`,
  `Tests/Semantics/`, `Tests/Syntax/`, or `Tests/Verify/` — the
  `Tests` lean_lib globs `Tests.**`.
- **Protocol port**: drop it under `Examples/` — the `Examples`
  lean_lib globs `Examples.**`.

### Iterate on a failing test

```bash
cd Src/PLean
lake build Tests.Verify.MyTest 2>&1 | grep -E '^error:' -A 5
```

### Run cvc5 / z3 from Lean

```bash
lake build Tests.Semantics.CrushRoundtrip
```
