# PLean — Counter-Example Rendering Plan

Plan for turning `#pverify`'s raw SMT model dump into a
human-and-agent-readable counter-example. Companion to
[`STATUS.md`](STATUS.md); update STATUS when a checkbox here flips.

## Problem

When an obligation is disproved, `loom_smt` throws
`"<solver>: the goal is false:<MODEL>"` where `<MODEL>` is the solver's
`(get-model)` reply re-stringified from a parsed S-expression. PLean
caps it at 12 lines / 1500 chars and prints it verbatim under
`counter-example:`. The user sees a single-line wall of
`(define-fun valid_fact_N () Bool …)` boilerplate (re-printed negated
hypotheses, not assignments) interleaved with lean-auto-mangled symbol
names (`_machines.116_`, `_sent.1546_`, `_Label.actionCount`). The
clause that was actually violated, the per-machine state, and the send
order are all unreadable.

## What a CEX should show

For a Hoare-triple obligation the model is a single `GlobalState s`
(plus, for the inductive step, the post-state `s'` the handler
produces). The useful content, in priority order:

1. **State of each machine** — for every `MachineRef` the model pins,
   render `<Machine>@<State>(field=value, …)` (e.g.
   `Node@Act(epoch=9, held=false)`): the owning machine and control
   state from the `MachineState`'s `currentState`, the `var` fields
   sliced from `Fields.mk` by that machine.
2. **Event trace** — the `sent : Label → Bool` set, serialized into a
   list ordered ascending by `Label.actionCount` (the order the `≺`
   operator reasons over), each rendered `<event>(field=value, …)` with
   a `[delivered]` marker from `received`. Empty set prints `[]`.
3. **Global counter** — `actionCount`.
4. **Witnesses** — the obligation's `this` / payload / skolem bindings,
   minus uninterpreted-sort universe noise.
5. **Machine-type alert** — when the model places a *typed* machine
   reference (declared `: <M>`, e.g. `function lock_server : Server`,
   `reshard_to : Node`) in a slot whose runtime kind differs, append the
   concrete type constraints to add — `init-holds (is_<M> <ref>.ref)`
   and `invariant <ref>_is_<M> : is_<M> <ref>.ref s`. A machine-typed
   reference is kind-erased in the VC (matching PVerifier:
   `PermissionType{Origin: Machine}` → flat `MachineRefT`), so its kind
   must be pinned by a user invariant seeded at init — P's
   `const_server` pattern. Detection keys on the wrapper constructor
   `(<M>.mk r)` in the model, so a raw `MachineRef` reference (P's
   untyped `machine`) never triggers it, and a kind-guarded
   `∀ n : <M>` binder cannot mismatch.

`MachineRef`-typed values (event-payload ref fields, ref-typed `var`s,
label targets, `this`) render as `<Kind>#<ref>` machine labels so a
reader can follow which machine sends/holds what. `MachineRef` is a
reducible `Nat` alias — the model can't tell a ref from any other `Nat`
— so two registry facts combine: the ref-typed field names (from
projection-function return types) and the ref → kind map (from the
`machines` table). A ref absent from `machines` renders bare (`#24`).

The base case has one state; the inductive step has pre/post — v1
renders the model's state(s) as they decode; pre/post diffing is v2.

## Architecture

The model already reaches PLean as a string in `pverifySmtDiagRef`
(set by `pverify_smt_close`, read by `classifyFailure`). v1 parses that
string — no changes to Loom or lean-auto.

```
loom_smt throws "<solver>: the goal is false:<MODEL>"
   │
   ▼ pverify_smt_close stashes full message in pverifySmtDiagRef
   │
   ▼ Verify/Obligation.lean::classifyFailure
   │     extractModel  : drop the prose prefix, isolate the sexp
   │     CexParse.parseModel : sexp → Array ModelDef (drop boilerplate)
   │     CexModel.decode     : ModelDefs → CexModel (best-effort)
   │     CexModel.render     : CexModel → readable String
   │
   ▼ .disproved (rendered : String)   -- signature unchanged
   │
   ▼ Commands/PVerify.lean::renderDiagnostic prints it
```

New files:
- `PLean/Verify/CexParse.lean` — `ModelDef` record; `parseModel`
  (sexp-string → entries, drops `valid_fact_*`/internal boilerplate);
  `demangle` (`_base.NNN_` → `base`, strip leading `_`).
- `PLean/Verify/CexModel.lean` — `CexNameCtx` (registry-derived names);
  `CexModel` (machines + sorted sent trace + counter + witnesses);
  `decode` (best-effort walk of `MachineState.mk` / `Label.mk`
  constructors and `ite`-chains); `render`.

`CexNameCtx` is built in `Obligation.lean::buildCexNameCtx` from the
registry and stashed in `cexNameCtxRef` before `synthesise` walks the
obligations. It carries: state-ctor → (machine, state), the global
`Fields` order as `(machine, var)`, each event's payload field names,
and the set of `MachineRef`-typed field/var names. All of this reads the
materialised structures via the environment (`getStructureInfo?` for
field names, projection-function return types for ref detection) —
`#gen_module` clears the registry `defStx` before `synthesise` runs, so
re-parsing it is not an option.

Changed files:
- `PLean/Verify/Obligation.lean` — `classifyFailure` builds and renders
  the structured CEX into the existing `.disproved (cex : String)`
  payload. Truncation moves to a fallback path used only when the
  structured decode fails.
- `PLean/Commands/PVerify.lean` — `renderDiagnostic` unchanged in shape;
  it now prints the rendered structured text.

`ObligationOutcome.disproved` keeps its `String` payload — `cex` is now
the rendered text, so the ripple stays inside `classifyFailure`.

## Name de-mangling (v1, heuristic)

lean-auto names atoms `"_" ++ delab(originalExpr)`, with a `.NNN_`
gensym suffix on fields freed by `sdestruct_state`. Recoverable by
string ops, no symbol-table bridge needed:

| Model symbol            | Demangled       |
|-------------------------|-----------------|
| `_sent.1546_`           | `sent`          |
| `_machines.116_`        | `machines`      |
| `_Label.actionCount`    | `Label.actionCount` |
| `_Fields.Bad_x`         | `Fields.Bad_x`  |
| `_Bad.ref`              | `Bad.ref`       |

The GlobalState fields are matched on the demangled base
(`sent`/`received`/`machines`/`actionCount`); machine/label
projections on the `<Type>.<member>` shape.

## Best-effort decode + graceful fallback

`decode` handles the shapes the solvers actually emit (confirmed
against live cvc5 runs on DistributedLock / LockServer):
- `machines` as an `ite`-equality chain `(ite (= x K) V …)` over
  `MachineRef`, each `V` a `MachineState.mk stage state fields kind`
  constructor decoded positionally;
- `sent` / `received` as arbitrary boolean skeletons (`or`-of-equalities,
  `ite`-chains) over `Label.mk target action actionCount` values, with
  the labels often `let`-bound — decoded by inlining `let`s and
  collecting every `Label.mk` subterm;
- nullary witnesses (`this`, payload, skolems) as `define-fun n () T V`.

When `CexNameCtx` lacks a name (or a value has an unexpected shape) the
relevant cell degrades to the de-mangled raw value, so the output is
never worse than the verbatim dump.

## Solver choice

v1 runs on whatever Loom invokes (Z3 default; its cardinality
constraints already enumerate finite uninterpreted universes, enough to
decode). v1.5 adds an opt-in CVC5 `--finite-model-find` re-query for
cleaner finite universes — that needs a Loom-side flag change and is
deferred so v1 stays self-contained.

## Phase plan

- **v1** (done)
  - `CexParse` + `CexModel`; wire through `classifyFailure`.
  - Machines table + sent-trace sorted by `actionCount`; raw fallback.
  - Registry-aware rendering: `Machine@State(field=val)` and
    `event(field=val)` via `CexNameCtx`; witnesses section; `[]` for
    empty sent; `-n` for negative ints.
  - Golden test pinning the rendered shape on a captured model;
    end-to-end test on a deliberately-falsifiable obligation.
  - Raise the disproved-path truncation cap.
- **v1.5**
  - CVC5 `--finite-model-find` re-query path (Loom flag change).
  - Pre/post state diff for inductive-step obligations.
- **v2**
  - Expose lean-auto's `h2lMap` / `l2hMap` from the query builder so
    symbol→`Lean.Expr` recovery is exact instead of heuristic.
  - `(get-abduct …)` "missing strengthening invariant" suggestion.
  - Optional JSON variant for the MCP/agent consumer.
