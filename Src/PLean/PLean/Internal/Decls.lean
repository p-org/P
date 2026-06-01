/-
PLean.Internal.Decls — lightweight metadata records.

These are NOT a deep AST. They exist only to (1) detect name conflicts during
elaboration, (2) drive `#pwf` resolution checks, and (3) feed the Phase 3
obligation generator. The actual runtime artifacts (state monad updates,
handler bodies) are ordinary Lean defs.

Each record carries:
  - the user-facing P name (`Name`)
  - the fully-qualified Lean def the macro emitted (`Name`, when applicable)
  - source-position info (`Syntax`) for error reporting
  - structural metadata needed by `#pwf` (e.g., which events a machine
    receives/sends, which state is `start`, which events a spec observes)

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
      Phase 0 just records it; semantics arrive later. -/
  temperature : Option Name
  /-- Events handled by this state (used by `#pwf` to check that referenced
      events exist and are consistent with the machine's `receives` set). -/
  handles     : Array Name
  /-- States this state may `goto`. Resolved against the owning machine. -/
  gotos       : Array Name
  ref         : Syntax
  deriving Inhabited

structure PMachineDecl where
  name      : Name
  leanName  : Name
  /-- Events the machine receives — derived from the events handled across
      its states (the union of each state's `handles`). -/
  receives  : Array Name
  /-- Events the machine sends — derived by scanning handler bodies for
      `send` statements naming a bare-identifier event. -/
  sends     : Array Name
  states    : Array PStateDecl
  /-- True iff this is a `spec` machine. Spec machines are flattened into
      globals + handler procedures in Phase 4. -/
  isSpec    : Bool
  /-- For spec machines, the events they observe. Empty for impl machines. -/
  observed  : Array Name
  /-- Raw machine-body syntax saved at registration time. Bodies elaborate
      to Lean defs only at `#gen_module` time, after every machine in the
      module has been registered (so cross-machine type references like
      `var server : Server` resolve). Empty until elaboration completes. -/
  body      : Array Syntax := #[]
  ref       : Syntax
  deriving Inhabited

structure PInvariantDecl where
  name      : Name
  leanName  : Name
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

end PLean
