/-
PLean.Internal.Decls — lightweight metadata records.

These are NOT a deep AST. They exist only to (1) detect name conflicts during
elaboration, (2) drive `#pwf` resolution checks, and (3) feed the obligation
generator. The actual runtime artifacts (state monad updates, handler
bodies) are ordinary Lean defs.

Each record carries:
  - the user-facing P name (`Name`)
  - the fully-qualified Lean def the macro emitted (`Name`, when applicable)
  - source-position info (`Syntax`) for error reporting
  - structural metadata needed by `#pwf` (e.g., which state is `start`,
    which events a spec observes; events received / sent are derived
    from the states list and body Syntax on demand — see
    `Syntax/Machine.lean::{machineReceives, machineSends}`)

The records are stored in `LocalPModuleCtx` (see Registry.lean). They do not
need to be `inhabited` or `serializable` — env-extension persistence handles
serialization of the wrapping `Std.HashMap`.
-/
import Lean
open Lean

namespace PLean

/-- Discriminator for a `type` declaration. -/
inductive PTypeKind where
  /-- `type N` — uninterpreted sort. Emitted as `opaque N : Type` plus an
      `Inhabited` axiom so values exist for typing but are opaque to Lean. -/
  | foreign
  /-- `type N = …` — interpreted alias. Emitted as `def N := …`. -/
  | alias
  /-- `enum N { … }` — interpreted enum. Emitted as `inductive N`. -/
  | enum
  deriving Inhabited, BEq, Repr

structure PTypeDecl where
  /-- Unqualified user-facing name (e.g., `ClientId`). -/
  name      : Name
  /-- Fully-qualified Lean def the macro emitted (e.g., `PingPong.ClientId`). -/
  leanName  : Name
  kind      : PTypeKind
  /-- For `enum`, the list of constructor names (unqualified). Empty otherwise. -/
  enumCases : Array Name := #[]
  /-- The whole declaration `Syntax` saved for replay at `#gen_module`
      time. We defer type elaboration just like machine bodies because
      named-tuple aliases may reference machine names that don't exist
      until the materialisation phase. -/
  defStx    : Option Syntax := none
  /-- Source range of the declaration, for error messages. -/
  ref       : Syntax
  deriving Inhabited

structure PEventDecl where
  name      : Name
  leanName  : Name
  /-- Name of the payload type (resolves to a `PTypeDecl` in the module).
      `none` means the event has no payload. -/
  payload   : Option Name
  /-- Saved declaration syntax for replay at `#gen_module` time.
      Deferred for the same reason as types: an event's payload may
      reference a type that is itself deferred. -/
  defStx    : Option Syntax := none
  ref       : Syntax
  deriving Inhabited

structure PEventSetDecl where
  name      : Name
  /-- Names of the events in the set; resolved against `events` at `#pwf`. -/
  events    : Array Name
  ref       : Syntax
  deriving Inhabited

/-- A `state` inside a `machine`. We keep this nested because state names are
    only meaningful within their owning machine. -/
structure PStateDecl where
  name        : Name
  /-- True iff this state was declared with `start state …`. Exactly one
      state per machine should have this set; `#pwf` enforces it. -/
  isStart     : Bool
  /-- Optional temperature: `hot`/`cold` for liveness; `none` for safety-only.
      Recorded for `#pwf` only — there is no liveness semantics yet. -/
  temperature : Option Name
  /-- Events handled by this state (used by `#pwf` to check that
      referenced events exist). -/
  handles     : Array Name
  /-- States this state may `goto`. Resolved against the owning machine. -/
  gotos       : Array Name
  ref         : Syntax
  deriving Inhabited

structure PMachineDecl where
  name      : Name
  leanName  : Name
  states    : Array PStateDecl
  /-- True iff this is a `spec` machine. Spec-machine handlers are not
      yet covered by `#pverify` (see CLAUDE.md's "Phase status"). -/
  isSpec    : Bool
  /-- For spec machines, the events they observe. Empty for impl machines. -/
  observed  : Array Name
  /-- Raw machine-body syntax saved at registration time. Bodies elaborate
      to Lean defs only at `#gen_module` time, after every machine in the
      module has been registered (so cross-machine type references like
      `var server : Server` resolve). The body is RETAINED after
      materialisation so the obligation generator can extract `var`
      declarations and re-walk for entry / goto-only clauses. -/
  body      : Array Syntax := #[]
  /-- True after `#gen_module M` has elaborated the machine into Lean
      defs. `#pwf` / `#pverify` check this to ensure materialisation
      precedes them. -/
  materialised : Bool := false
  ref       : Syntax
  deriving Inhabited

structure PInvariantDecl where
  name      : Name
  leanName  : Name
  /-- The state binder name introduced by the enclosing `system <s> { … }`
      block. `none` for bare top-level / Lemma-internal invariants whose
      bodies don't reference state. The materialiser uses this to choose
      the lambda binder: `none` → `fun _ => <body>` (state-independent);
      `some n` → `fun n => <body>` (state-implicit, using the user's
      chosen name). -/
  stateBinder : Option Name := none
  defStx    : Option Syntax := none
  ref       : Syntax
  deriving Inhabited

/-- Single-proposition `paxiom` (vs. `pinstance` axiom bundles). -/
structure PAxiomDecl where
  name      : Name
  leanName  : Name
  defStx    : Option Syntax := none
  ref       : Syntax
  deriving Inhabited

/-- `pinstance nm : Class T` — bundle of axioms via a Lean typeclass. -/
structure PInstanceDecl where
  name      : Name
  /-- The class expression as written (rendered for `#print_pmodule`). -/
  classRepr : String
  /-- The argument type as written (rendered for `#print_pmodule`). -/
  typeRepr  : String
  defStx    : Option Syntax := none
  ref       : Syntax
  deriving Inhabited

/-- `init <prop>;` — assume-on-start clause. There may be many; we don't
    name them, hence an Array (not NameMap) in the module ctx. -/
structure PInitDecl where
  defStx    : Option Syntax := none
  /-- Position of the `init` keyword for diagnostics. -/
  ref       : Syntax
  deriving Inhabited

structure PPureDecl where
  name      : Name
  leanName  : Name
  /-- True iff the user gave a body (`pure foo (x : T) : R = expr;`). False
      means uninterpreted (foreign). -/
  hasBody   : Bool
  defStx    : Option Syntax := none
  ref       : Syntax
  deriving Inhabited

/-- A `Lemma` or `Theorem` block — a named bundle of invariants. The
    distinction between `Lemma` and `Theorem` is purely declarative; both
    materialise to the same registry record (with `isTheorem` discriminating
    for diagnostics). -/
structure PLemmaDecl where
  name       : Name
  /-- True iff declared with `Theorem`, false for `Lemma`. -/
  isTheorem  : Bool
  /-- Names of the per-invariant declarations this lemma owns, in the
      order they were declared. Each name is *also* registered as a
      free-standing `PInvariantDecl` so cross-references via
      `prove ... using ...` can resolve to the individual prop. -/
  invariants : Array Name
  /-- Saved declaration `Syntax` for replay at `#gen_module` time. -/
  defStx     : Option Syntax := none
  ref        : Syntax
  deriving Inhabited

/-- One `prove` directive inside a `Proof` block. Either targets a named
    lemma (`prove <name>`) or the special `default` sanity invariants
    (`prove default`). -/
structure PProveDirective where
  /-- Target lemma name, or `default` (a sentinel). -/
  target : Name
  /-- True iff `target == default` (the sanity-invariants sentinel). -/
  isDefault : Bool
  /-- Names of lemmas to assume (`using <l1>, <l2>`). Each must itself be
      a previously-`prove`d lemma name. -/
  usingLemmas : Array Name := #[]
  ref   : Syntax
  deriving Inhabited

/-- A `Proof <name>?` block — list of `prove` directives. The optional
    name is just a tag for diagnostics; multiple `Proof` blocks accumulate. -/
structure PProofDecl where
  /-- Optional tag (debug only). Empty Name (`Name.anonymous`) when the
      `Proof` block was anonymous. -/
  name       : Name
  directives : Array PProveDirective
  ref        : Syntax
  deriving Inhabited

end PLean
