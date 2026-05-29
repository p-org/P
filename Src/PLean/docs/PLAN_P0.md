# PLean — Phase 0 (Bootstrap) Plan

This document expands the Phase-0 entry in [`PLAN.md`](PLAN.md). The objective
of Phase 0 is to **let users write P programs in Lean across multiple files**
and have those declarations aggregate into a single named PLean module — with
no verification yet. Verification work begins in Phase 1.

This is the equivalent of velvet/Veil's "scaffolding" phase, modeled directly
on Veil's [`veil module … end`](https://github.com/verse-lab/veil) pattern,
which is the closest existing precedent for a multi-file Lean DSL describing
distributed-system specifications.

## Confirmed design decisions (from conversation)

1. **Fragments, not Veil's `includes`.** A `pmodule M` block may appear in
   many `.lean` files. Importing those files merges the fragments into one
   logical module under the name `M`. Lean's existing `import` mechanism
   handles cross-file delivery of declarations; we do **not** need a separate
   parametric-composition operator like Veil's `includes`. (`includes` in
   Veil is functor-style instantiation with type arguments and aliasing,
   which is overkill for v1.)
2. **Stub `PM` is fine.** Phase 0 elaborates handler bodies into stub Lean
   defs over `PLean.Stub.PM := Id`. Phase 1 replaces the stub with the real
   `PM α := StateT GlobalState (NonDetT DivM) α`.
3. **Two commands, separate concerns.** `#pwf M` runs structural
   well-formedness checks only — it lives forever as a fast, always-available
   subset of the full pipeline. `#pverify M` is the end-to-end command:
   in Phase 0 it implies `#pwf`; in Phase 3+ it additionally generates
   Hoare-triple obligations and dispatches them to `loom_solve`. Other
   subset commands may be added later (e.g., `#pwf_handlers M`,
   `#pwf_invariants M`). The name `#pwf` is chosen to avoid visual
   confusion with P's existing CLI `p check`.
4. **Keyword consistency with P, with a `p` prefix where Lean collides.**
   PLean keywords match P's grammar verbatim wherever possible:
   `event`, `eventset`, `enum`, `type`, `machine`, `spec`, `invariant`,
   `init`, `pure`. Three keywords get a `p` prefix because Lean's
   tokenizer would otherwise eat them:
   - `pmodule` — Lean's `module` is reserved (and P also reserves `module`
     for its own module-system composition).
   - `paxiom` — Lean has a builtin `axiom` command.
   - `pinstance` — Lean has a builtin `instance` command.
   `pmodule M` opens `namespace M`. Events and machines are referenced as
   `M.ePing`, `M.Server`, etc., from outside the module.
5. **Uninterpreted sorts + axioms, alongside interpreted ones.** P's grammar
   already distinguishes the two cases
   ([`PParser.g4:81-83`](../../PCompiler/CompilerCore/Parser/PParser.g4#L81-L83)):
   - `type Round;` — uninterpreted sort (becomes a Lean `opaque`/`axiom`
     constant; SMT sees it as a fresh sort).
   - `type Msg = (sender: MachineRef, payload: Nat);` — interpreted type
     alias (Lean `def Msg := …`).
   - `enum Color { Red, Green, Blue }` — interpreted enum (Lean `inductive`).
   Both `axiom <name> : <prop>;` and `init <prop>;` may quantify over
   uninterpreted sorts.

6. **Axiom bundles via `instance` (Veil pattern, generalized).**
   Single-line `axiom <name> : <prop>;` is fine for one-off facts, but the
   common case — "this sort is a total order", "these nodes form a ring",
   "this set is a queue" — is a *bundle* of related axioms. We crib Veil's
   pattern (Veil names the keyword `instantiate`; PLean uses `instance` so
   the surface reads "this sort *is an instance of* `TotalOrder`"):

   The user writes (or imports) an ordinary Lean `class` whose fields are
   uninterpreted relations/functions plus their algebraic properties stated
   as axioms (cf. [`Veil/Std.lean:3-10`](https://github.com/verse-lab/veil)):

   ```lean
   class TotalOrder (t : Type) where
     le         : t → t → Prop
     le_refl    : ∀ x, le x x
     le_trans   : ∀ x y z, le x y → le y z → le x z
     le_antisym : ∀ x y, le x y → le y x → x = y
     le_total   : ∀ x y, le x y ∨ le y x
   ```

   Then, inside any `pmodule`, the user writes:

   ```lean
   pmodule MyProto
     type N                                  -- uninterpreted sort
     instance tot : TotalOrder N             -- bundle axioms over N
     instance ord : TotalOrder MachineRef    -- bundle axioms over a built-in type
     instance natOrd : TotalOrder Nat        -- works on primitives too
   end MyProto
   ```

   **Implementation.** A one-line `macro_rules` that elaborates
   `instance nm : tp` (in command position, inside an open `pmodule`) into
   `variable [nm : tp]`. Lean then threads the instance through every
   subsequent declaration in scope, and the SMT layer sees the instance's
   fields as fresh constants with their axioms as hypotheses. This mirrors
   Veil's
   [`DSL/Specification/Lang.lean:96-97`](https://github.com/verse-lab/veil):
   ```lean
   macro_rules
     | `(command| instance $nm:ident : $tp:term) =>
         `(variable [$nm : $tp])
   ```

   **Lean keyword collision.** Lean already has an `instance` *command*
   (which takes a body and produces a real typeclass instance). PLean's
   `instance` is a different syntactic shape — bodyless, declaration-only —
   so the parser can disambiguate. The elaborator additionally gates on
   "currently inside `pmodule`" via `localPModuleCtx.isSome`; outside a
   `pmodule`, the user's `instance` falls through to Lean's builtin. Test
   case: a file that uses both Lean's `instance Nat.foo : Foo Nat where …`
   *and* PLean's `instance bar : Bar T` inside a `pmodule` must compile.

   **Uniformity over types.** `instance` works on any type — uninterpreted
   sort, alias, enum, primitive (`Nat`, `Int`), or built-in PLean type like
   `MachineRef`. Veil's examples only show it on uninterpreted sorts, but
   neither Veil's nor PLean's implementation restricts the type. The user
   is responsible for soundness: `instance _ : TotalOrder Nat` is fine
   (`Nat` really is totally ordered); `instance _ : TotalOrder Bool`
   compiles but admits unsound axioms over `Bool` — `#pwf` cannot detect
   that.

   **What `#pwf` checks.**
   - `tp` resolves to a Lean class (catches typos like
     `instance _ : TottalOrder N`),
   - the same `nm` isn't instantiated twice in the same module fragment.
   It does **not** check coherence — Lean's typeclass resolution does that.
   It does **not** check soundness — the user is on the hook for
   instantiating `TotalOrder` only over types that really are totally
   ordered.

## Surface design — what users write

The canonical Phase-0 demo we want to support is a three-file PingPong
program:

```lean
-- File: Examples/PingPong/Events.lean
import PLean

pmodule PingPong
  -- uninterpreted sort (no body): the SMT solver sees a fresh sort
  type ClientId

  -- interpreted enum
  enum Status { Pending, Done }

  -- interpreted type alias
  type PingPayload = { client : MachineRef, id : ClientId }

  event ePing : PingPayload
  event ePong : { id : ClientId }
end PingPong
```

```lean
-- File: Examples/PingPong/Server.lean
import Examples.PingPong.Events       -- carries the `PingPong` fragment

pmodule PingPong                       -- reopens the same module name
  machine Server receives [ePing] sends [ePong] {
    start state Idle {
      on ePing do (req : PingPayload) {
        send req.client, ePong, { id := req.id }
      }
    }
  }
end PingPong
```

```lean
-- File: Examples/PingPong/Top.lean
import Examples.PingPong.Events
import Examples.PingPong.Server
import Examples.PingPong.Client        -- defined similarly

pmodule PingPong
  -- single-proposition axiom over an uninterpreted sort
  axiom client_ids_distinct : ∀ (a b : ClientId), a = b ∨ a ≠ b

  -- bundle of axioms via a Lean class — works on any type, uninterpreted
  -- (ClientId) or built-in (MachineRef)
  instance idOrd : TotalOrder ClientId
  instance refOrd : TotalOrder MachineRef

  -- Bound variables of type `event` are Labels in the encoding; `lbl is ePong`
  -- directly tests the label's event tag (mirrors PVerifier `TestExpr`,
  -- PParser.g4:199). The `≺` operator is the temporal precedence primitive
  -- introduced by PLean; it reduces to `a.actionCount < b.actionCount`. See
  -- PLAN.md → "Open Design Problems → Temporal predicate `≺`". `≺` is *not*
  -- supported by PVerifier today — this invariant cannot be expressed in P
  -- as it currently stands.
  invariant pong_after_ping :
    ∀ (lbl : event), sent lbl ∧ lbl is ePong →
      ∃ (p : event), sent p ∧ p is ePing ∧ p ≺ lbl
end PingPong

#pwf      PingPong                     -- always-available: well-formedness only
#pverify  PingPong                     -- Phase 0: implies #pwf
                                       -- Phase 3+: also generates obligations
```

`#pwf` only checks structural well-formedness:
- every event referenced in `on … do` / `sends` / `receives` is declared,
- every state name used in `goto` resolves,
- every machine referenced in `send` / `new` resolves,
- no duplicate names.

It does **not** generate Hoare triples or call `loom_solve`. That arrives in
Phase 3 via `#pverify`. After Phase 3, `#pwf` continues to exist as a fast
subset check (useful in editing loops where you want to confirm the program
*parses* without paying for SMT).

## Architecture

The module-aggregation mechanism is a two-tier env extension, exactly like
Veil ([`Veil/DSL/Internals/StateExtensions.lean`](https://github.com/verse-lab/veil)
in particular `localSpecCtx` / `globalSpecCtx`):

```lean
-- Per-file scratch space, populated as we elaborate inside `pmodule M ... end M`.
-- Cleared on `end M`; not persistent across imports.
structure LocalPModuleCtx where
  name           : Name              -- module name
  -- types: uninterpreted (no body) and interpreted (alias / enum) live in
  -- the same map; the decl kind discriminates so #pwf can reject illegal
  -- uses (e.g., constructing a value of an uninterpreted sort).
  types          : NameMap PTypeDecl    -- foreign + alias + enum
  instances      : NameMap PInstanceDecl   -- `instance nm : Class T` (axiom bundles)
  events         : NameMap PEventDecl
  eventSets      : NameMap PEventSetDecl
  machines       : NameMap PMachineDecl
  specs          : NameMap PSpecDecl
  invariants     : NameMap PInvariantDecl
  axioms         : NameMap PAxiomDecl   -- single-prop axioms (incl. over uninterpreted sorts)
  inits          : Array PInitDecl      -- `init` clauses (assume-on-start)
  pures          : NameMap PPureDecl
  proofBlocks    : Array PProofBlockDecl
  -- room to grow

-- Persistent across files. Key = module name. Survives `import` automatically.
abbrev GlobalPModuleCtx := Std.HashMap Name LocalPModuleCtx
```

- `localPModuleCtx : SimpleScopedEnvExtension LocalPModuleCtx` — set on
  `pmodule M`, cleared on `end M`. Holds the fragment currently being
  elaborated.
- `globalPModuleCtx : SimplePersistentEnvExtension (Name × LocalPModuleCtx) GlobalPModuleCtx`
  — written on `end M` by *merging* `localPModuleCtx` into the entry for
  `M`. Persistent flavor so `import` carries it across files.

The merge step is the heart of cross-file aggregation:

> When `Server.lean` opens `pmodule PingPong`, the elaborator first
> **restores** `localPModuleCtx` from `globalPModuleCtx[PingPong]` (so events
> declared in `Events.lean` are visible during machine elaboration), then
> accepts new declarations, then merges back on `end PingPong`.
> Re-declaring an existing name (e.g., redefining `ePing`) is an error.

The "decl" types (`PEventDecl`, `PMachineDecl`, …) are intentionally
**lightweight metadata records** — name, types, source position, and a
reference to the elaborated Lean def. They are not a deep AST. Real
machine-handler logic lives as ordinary Lean defs in `PM` (Phase-0 stub:
`PLean.Stub.PM := Id`), consistent with the shallow-embedding decision in
[`PLAN.md`](PLAN.md#design-decisions).

## Module list (Phase 0 deliverables)

```
Src/PLean/
  lakefile.lean                          # require Loom (velvet's pin)
  lean-toolchain                         # match Loom
  PLean.lean                             # facade re-exports

  PLean/
    Internal/
      Decls.lean                         # data records: PEventDecl, PMachineDecl, ...
      Registry.lean                      # local + global env extensions
      Elab.lean                          # shared elaboration helpers (name resolution,
                                         #   reading current pmodule, error helpers)
      Stub.lean                          # PLean.Stub.PM := Id; stub send/raise/goto/new

    Surface/
      Module.lean                        # `pmodule`, `end` commands
      Types.lean                         # `type` (foreign sort + alias), `enum`
      Events.lean                        # `event`, `eventset`
      Machine.lean                       # `machine`, `state`, `entry`,
                                         #   `on _ do/goto`, `spec`
      Stmt.lean                          # statement-position macros
                                         #   (parse + elaborate to Stub combinators)
      Verify.lean                        # `invariant`, `axiom`, `init`, `pure`,
                                         #   `instance` (axiom-bundle instantiation
                                         #   à la Veil's `instantiate`)
                                         #   — parse + register; no proof generation

    Commands/
      PWf.lean                           # `#pwf M`: well-formedness check
                                         #   (always available; lives past Phase 0)
      PVerify.lean                       # `#pverify M`: end-to-end pipeline.
                                         #   Phase 0: implies #pwf.
                                         #   Phase 3+: also generates obligations.
      PrintModule.lean                   # `#print_pmodule M`: dump registered fragment

  Examples/PingPong/                     # the three-file demo above
    Events.lean
    Server.lean
    Client.lean
    Top.lean

  Tests/
    Bootstrap/
      SingleFile.lean                    # one-file pmodule, end-to-end registration
      MultiFile/                         # three-file aggregation test mirroring PingPong
        Events.lean
        Machine.lean
        Top.lean
      Errors.lean                        # `#guard_msgs` for: duplicate events,
                                         #   undeclared event in `on...do`,
                                         #   `end` mismatched name, etc.
```

The `Internal/Decls.lean` records exist purely to (1) detect name conflicts
during elaboration and (2) drive the Phase 3 obligation generator. No
`inductive PProgram`.

## Phase 0 work breakdown (ordered)

1. **Lake bootstrap** — `lakefile.lean`, `lean-toolchain`, empty
   `PLean.lean`, CI job. Confirm `lake build` succeeds with Loom resolved.
2. **Stub PM** — `Internal/Stub.lean`. Define
   `PLean.Stub.PM α := Id α` and stub combinators
   `send`, `raise`, `goto`, `new`, `assign` as no-ops in `Id`. Just enough
   for surface-syntax macros to elaborate.
3. **Internal scaffolding** — `Internal/Decls.lean` + `Internal/Registry.lean`.
   Env extensions only; no commands yet. Add a unit test that manually
   `set`s an extension and reads it back to confirm persistence behavior.
4. **`pmodule` / `end` commands** — `Surface/Module.lean`. Open a Lean
   namespace, set `localPModuleCtx`, restore from `globalPModuleCtx` if the
   module was previously declared. Verify multiple `pmodule M` blocks in one
   file behave correctly (rare but should not crash).
5. **Type declarations** — `Surface/Types.lean`. `type N` (foreign /
   uninterpreted sort: emit `opaque N : Type` + an `Inhabited` axiom),
   `type N = …` (interpreted alias: emit `def N := …`), `enum N { … }`
   (emit `inductive N`). Each path registers metadata in the local context;
   the decl-kind discriminator in `PTypeDecl` lets later passes tell them
   apart (e.g., `#pwf` rejects literal construction of an uninterpreted
   sort).
6. **Event declarations** — `Surface/Events.lean`. `event`, `eventset`.
7. **Machine declarations** — `Surface/Machine.lean`.
   `machine` / `state` / `entry` / `on…do` / `on…goto`. Macros parse and
   register; handler bodies become stub `PM` defs (`do return ()`) until
   Phase 1.
8. **Statement parsing** — `Surface/Stmt.lean`. Recognize
   `send` / `raise` / `goto` / `assign` at statement position. Emit them as
   calls into `PLean.Stub` combinators.
9. **Verification declarations (parse-only)** — `Surface/Verify.lean`.
   `invariant` / `axiom` / `init` / `pure` / `instance`. Register metadata;
   no proofs. Notes on the two assumption-style forms:
   - `axiom <name> : <prop>;` — single proposition, may quantify over
     uninterpreted sorts.
   - `instance <name> : <Class> <T>` — bundle of axioms via a Lean
     typeclass; works over any `T` (uninterpreted, alias, enum, primitive,
     `MachineRef`). Implementation: a one-line `macro_rules` that rewrites
     to `variable [<name> : <Class> <T>]`, gated on
     `localPModuleCtx.isSome` so it doesn't intercept Lean's builtin
     `instance` outside a `pmodule`.
10. **`#pwf` well-formedness validator** — `Commands/PWf.lean`. Walk the
    registered module and emit errors for unresolved names, duplicate
    declarations, or use-before-declare. This is the gating success signal
    for Phase 0 and lives forever as a fast subset check.
11. **`#pverify` shell** — `Commands/PVerify.lean`. In Phase 0, simply
    delegates to `#pwf`. The Phase-3 expansion (obligation generation,
    `loom_solve`) lands as a separate change without touching `#pwf`.
12. **`#print_pmodule`** — `Commands/PrintModule.lean`. Pretty-print the
    registered metadata. Crucial for debugging the registry without running
    anything.
13. **Multi-file demo + tests** — `Examples/PingPong/` and
    `Tests/Bootstrap/`. Three-file aggregation verifies that `import` brings
    event decls into a downstream `pmodule` block. Tests must include:
    - one example exercising an uninterpreted sort + single-proposition
      `axiom`;
    - one example exercising `instance nm : Class T` over an uninterpreted
      sort (Veil-style);
    - one example exercising `instance nm : Class T` over a built-in type
      (e.g., `TotalOrder MachineRef`) — confirms the typeclass mechanism
      doesn't restrict `T`;
    - one regression test confirming Lean's builtin `instance Foo.bar where
      …` still parses outside any `pmodule` block (no keyword collision).

## Exit criterion

The three-file PingPong demo:
- compiles cleanly with `lake build`,
- `#print_pmodule PingPong` from `Top.lean` shows types (both
  uninterpreted and interpreted), events, machines, invariants, axioms,
  and instances all aggregated under one module,
- `#pwf PingPong` reports no errors,
- `#pverify PingPong` succeeds (in Phase 0, this is equivalent to `#pwf`),
- a deliberately broken variant in `Tests/Bootstrap/Errors.lean` (e.g.,
  `on eMissing do …`) produces a localized `#pwf` error pointing to the
  offending line.

No `loom_solve`, no Hoare triples, no real `PM` semantics. Those start in
Phase 1.

## Risks / things to watch

- **Persistent vs. scoped extension semantics.** Veil uses
  `SimpleScopedEnvExtension` for its global registry, which is unusual —
  most cross-file env extensions are `SimplePersistentEnvExtension`. The
  velvet survey confirms that for *transparent* cross-file aggregation,
  persistent is the right flavor (it has an `addImportedFn` callback that
  unions imported maps; see velvet's `velvetObligations`, which lives in
  Loom's `CaseStudies/Extension.lean` —
  https://github.com/verse-lab/velvet,
  https://github.com/verse-lab/loom).
  We follow velvet here, not Veil — Veil works around this with its
  `#gen_spec` finalization step, which we don't need.
- **Re-opening a `pmodule` after `import`.** When `Server.lean` opens
  `pmodule PingPong`, we must restore the local context from the global
  registry **before** any declarations in `Server.lean`'s `pmodule` block
  can see them. Sequence: parse `pmodule M` → look up `globalPModuleCtx[M]`
  → if present, `localPModuleCtx.set` to that → continue elaborating. Get
  this wrong and event references in machines won't resolve.
- **`end M` matching.** Easy to typo `end PingPng`. The command must
  validate that `end <name>` matches the currently-open `pmodule <name>`.
- **Macro hygiene for emitted Lean defs.** When `machine Server` emits
  `def Server` under `namespace PingPong`, we want `PingPong.Server` —
  free with `namespace`, but worth a quick test.
- **Uninterpreted-sort emission.** A `type N` with no body should produce a
  Lean declaration that (a) is well-typed at any use site, (b) prevents
  literal construction (`N.mk`-style), and (c) gives the SMT layer a fresh
  sort. The cleanest scheme is `opaque N : Type := defaultInhabited` plus an
  `axiom N.inhabited : Inhabited N`. Confirm Loom accepts `opaque`-backed
  sorts in goals; if not, fall back to `axiom N : Type` (which Loom does
  accept, judging by velvet's foreign types).
- **`axiom` / `instance`-keyword shadowing.** Lean has built-in `axiom` and
  `instance` commands. Our PLean variants must be custom `command` syntaxes
  matched only inside an open `pmodule` block — outside of one, the user's
  `axiom`/`instance` should still parse as Lean's. Implement by gating on
  `localPModuleCtx.isSome` in the command elaborator. Veil sidesteps this
  for `instance` by using a fresh keyword `instantiate`; we want the more
  natural `instance` reading and accept the gating cost. Test: a file using
  Lean's `instance Foo.bar : Bar Foo where …` *outside* a `pmodule` and
  PLean's `instance bar : Bar T` *inside* one must coexist.

## Hand-off to Phase 1

By end of Phase 0:
- The registry exists and works across files.
- Every macro emits well-typed Lean code (against the stub `PM`).
- `#pwf` walks the registry without crashing and reports clean errors.
- `#pverify` exists as a thin wrapper over `#pwf` (delegates today;
  expanded in Phase 3).

Phase 1 then replaces `PLean.Stub.PM` with the real
`StateT GlobalState (NonDetT DivM)`, fills in the real
`send` / `raise` / `goto` / `new` semantics, and lets the same registry
drive Phase-3 obligation generation. The macros written in Phase 0 do not
need to change.

## References

- Veil module / `#gen_spec` pattern (closest precedent):
  https://github.com/verse-lab/veil — see
  `Veil/DSL/Internals/StateExtensions.lean`,
  `Veil/DSL/Specification/Syntax.lean`, and
  `Examples/Test/Composition.lean`.
- velvet persistent-extension pattern (the merge-on-import callback):
  https://github.com/verse-lab/velvet, with the relevant code in
  Loom's `CaseStudies/Extension.lean`
  (https://github.com/verse-lab/loom).
- P parser grammar (the surface we are mirroring):
  [`PParser.g4`](../../PCompiler/CompilerCore/Parser/PParser.g4)
