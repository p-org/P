/-
PLean.Semantics.Label — runtime artefacts that flow through the buffer.

- `Label`        — a single buffer entry: `{ target, action, actionCount }`.
- `EventOrGoto`  — the action carried by a label: an event with payload
                   or a goto with payload.
- `MachineState` — per-machine runtime row: stage flag, control state,
                   `var`-block fields, kind tag.

The semantic core is parameterised over the user program's union types
(`E`/`G`/`S`/`F`); `#gen_module` synthesises the concrete unions per
pmodule, and hand-written examples build them directly. Keeping the
core generic means the semantic library compiles once regardless of
what the per-program unions look like.
-/

namespace PLean

/-- Reference to a machine instance, as a flat global numeric handle.

The runtime carrier stays untyped on purpose: `GlobalState.machines :
MachineRef → MachineState` is a single map, the buffer's `Label.target`
is a single field, and primitives (`send`/`goto`/...) take a single
`MachineRef` regardless of the addressee's machine kind.

Per-machine *static* refinements live one level up: `#gen_module` emits
a wrapper struct per machine, e.g.
```
structure Server where ref : MachineRef
  deriving Inhabited, DecidableEq
instance : Coe Server MachineRef := ⟨fun s => s.ref⟩
```
Handlers take `(this : Server)` rather than `(this : MachineRef)`, so
the elaborator distinguishes the kinds (`var c : Client := some_server`
is rejected) while the underlying state map and buffer stay flat.
Invariants quantify `∀ s : Server, …` over the wrapper to restrict the
bound variable to one kind without needing a refined `MachineRef`. -/
abbrev MachineRef : Type := Nat

/-- A label's action is either an event with its payload, or a goto with
its payload. Generic in `E` (the event-payload union) and `G` (the goto
payload union). -/
inductive EventOrGoto (E : Type) (G : Type) where
  /-- A user event with its payload. -/
  | event (e : E)
  /-- A goto transition with its (possibly trivial) payload. -/
  | goto  (g : G)
  deriving Inhabited, DecidableEq, Repr

/-- A label is the buffer entry that machines deliver to each other.
Generic in the action's event/goto unions. The `actionCount` field is
the temporal witness for the `≺` precedence operator (defined in
`Predicates.lean`). -/
structure Label (E : Type) (G : Type) where
  target      : MachineRef
  action      : EventOrGoto E G
  actionCount : Nat
  deriving Inhabited, DecidableEq, Repr

/-- One machine instance's runtime state.

- `stage`        — entry flag: `true` means the entry handler is the
                   next thing to run.
- `currentState` — discrete control state (the program-state-tag enum).
- `fields`       — the machine's `var` block, as a record.
- `kind`         — per-pmodule index identifying which `MKind`
                   constructor this machine belongs to. `0` is reserved
                   for "unset"; real machine kinds are `≥ 1`. The
                   default lets the runtime use a single flat map
                   (`MachineRef → MachineState`) while letting `is_<M>
                   m s` decide statically what kind a ref belongs to. -/
structure MachineState (S : Type) (F : Type) where
  stage        : Bool
  currentState : S
  fields       : F
  kind         : Nat := 0
  deriving Inhabited, Repr

/-! ## Label accessors -/

namespace Label

variable {E G : Type}

/-- True iff the label's action is an event tag. -/
@[inline] def isEvent (lbl : Label E G) : Bool :=
  match lbl.action with
  | .event _ => true
  | .goto  _ => false

/-- True iff the label's action is a goto tag. -/
@[inline] def isGoto (lbl : Label E G) : Bool :=
  match lbl.action with
  | .event _ => false
  | .goto  _ => true

/-- Project the event payload, if this is an event label. -/
@[inline] def eventPayload? (lbl : Label E G) : Option E :=
  match lbl.action with
  | .event e => some e
  | .goto  _ => none

/-- Project the goto payload, if this is a goto label. -/
@[inline] def gotoPayload? (lbl : Label E G) : Option G :=
  match lbl.action with
  | .event _ => none
  | .goto  g => some g

end Label

end PLean
