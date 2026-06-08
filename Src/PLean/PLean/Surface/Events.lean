/-
PLean.Surface.Events — `event` and `eventset` declarations.

Two forms:
  `event eName`                 — payload-less event
  `event eName : <PayloadType>` — typed event
  `eventset esName = { e1, e2, ... }` — named event set

## Two-phase elaboration

Like `type` and `machine`, events defer elaboration to `#gen_module`
time. An event's payload type may itself be a deferred type alias
(e.g. a named-tuple referencing a machine), so eager event emission
would fail with an "unknown identifier" against a not-yet-materialised
type. Eventsets are pure metadata — they need no Lean def either way.
-/
import Lean
import PLean.Internal.Decls
import PLean.Internal.Registry

open Lean Elab Command

namespace PLean

/-- `event eName` — payload-less event. -/
syntax (name := pEventNoPayload) "event " ident : command

/-- `event eName : <PayloadType>` — typed event. -/
syntax (name := pEventTyped) "event " ident " : " term : command

/-- `eventset esName = { e1, e2, ... }` — named set of events. -/
syntax (name := pEventSet) "eventset " ident " = " "{" ident,* "}" : command

@[command_elab pEventNoPayload]
def elabPEventNoPayload : CommandElab := fun stx => do
  let `(event $id:ident) := stx
    | throwUnsupportedSyntax
  let _ ← requireLocalPModuleCtx "event"
  let ns ← getCurrNamespace
  addEvent
    { name      := id.getId
      leanName  := ns ++ id.getId
      payload   := none
      defStx    := some stx
      ref       := stx }

@[command_elab pEventTyped]
def elabPEventTyped : CommandElab := fun stx => do
  let `(event $id:ident : $payloadTy:term) := stx
    | throwUnsupportedSyntax
  let _ ← requireLocalPModuleCtx "event"
  let ns ← getCurrNamespace
  let payloadName : Option Name :=
    if payloadTy.raw.isIdent then some payloadTy.raw.getId else none
  addEvent
    { name      := id.getId
      leanName  := ns ++ id.getId
      payload   := payloadName
      defStx    := some stx
      ref       := stx }

@[command_elab pEventSet]
def elabPEventSet : CommandElab := fun stx => do
  let `(eventset $id:ident = { $events:ident,* }) := stx
    | throwUnsupportedSyntax
  let _ ← requireLocalPModuleCtx "eventset"
  let evs := events.getElems.map (·.getId)
  -- Eventsets are metadata-only; no Lean def.
  addEventSet
    { name   := id.getId
      events := evs
      ref    := stx }

/-! ## Materialisation

Replay one event's saved `defStx`. Called by `#gen_module` after types
have been materialised. -/

def materialiseEvent (d : PEventDecl) : CommandElabM Unit := do
  match d.defStx with
  | none => pure ()
  | some stx =>
    -- For typed events, emit the payload-type abbrev that the `send`
    -- macro reads to ascribe named-tuple literals. The abbrev name uses
    -- an underscore (`<ev>_payload`) rather than a dot
    -- (`<ev>.payload`) so it doesn't shadow / get confused with field
    -- accessors. Phase 2 no longer emits a numeric event-tag def: the
    -- per-pmodule event union `<Mod>.E` is the source of truth, and
    -- `<ev>` resolves to the union constructor `<Mod>.E.<ev>` in
    -- handler bodies.
    match stx with
    | `(event $_:ident : $payloadTy:term) =>
      let payloadAbbrev := mkIdent (d.name.appendAfter "_payload")
      elabCommand (← `(abbrev $payloadAbbrev : Type := $payloadTy))
    | `(event $_:ident) => pure ()
    | _ => pure ()

end PLean
