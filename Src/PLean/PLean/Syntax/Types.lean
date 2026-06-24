/-
PLean.Syntax.Types — `type` and `enum` declarations.

Three forms, mirroring P's `type`/`enum` grammar:
  `type N`              — uninterpreted (foreign) sort
  `type N = (f : T, …)` — interpreted named-tuple type (emits `structure`)
  `type N = <term>`     — interpreted alias
  `enum N { c1, c2 }`   — interpreted enum

## Two-phase elaboration

Type aliases may reference *machine* names (e.g. `type ServerInit = (client
: Client)`), which are only materialised at `#gen_module` time. To avoid an
ordering pitfall, we defer ALL type-alias elaboration: `type` / `enum`
register the declaration `Syntax` in the local module ctx and Lean defs
are emitted only when `#gen_module` replays them — alongside machine
bodies, so cross-references resolve.

Foreign sorts could be emitted eagerly (no machine dependency) but we
defer them too for consistency.
-/
import Lean
import PLean.Internal.Decls
import PLean.Internal.Registry

open Lean Elab Command

namespace PLean

/-- `type N` — uninterpreted sort. -/
syntax (name := pTypeForeign) "type " ident : command

/-- One named field in a `type N = (f : T, ...)` named-tuple definition. -/
syntax pNamedField := ident " : " term

/-- `type N = (f1 : T1, f2 : T2, ...)` — interpreted named-tuple type. -/
syntax (name := pTypeNamedTuple) (priority := high)
  "type " ident " = " "(" pNamedField,+ ")" : command

/-- `type N = <term>` — interpreted alias (primitive / reducible). -/
syntax (name := pTypeAlias) "type " ident " = " term : command

/-- `enum N { c1, c2, ... }` — interpreted enum. -/
syntax (name := pEnumDecl) "enum " ident " { " ident,+ " }" : command

@[command_elab pTypeForeign]
def elabPTypeForeign : CommandElab := fun stx => do
  let `(type $id:ident) := stx
    | throwUnsupportedSyntax
  let _ ← requireLocalPModuleCtx "type"
  let ns ← getCurrNamespace
  addType
    { name      := id.getId
      leanName  := ns ++ id.getId
      kind      := PTypeKind.foreign
      defStx    := some stx
      ref       := stx }

@[command_elab pTypeNamedTuple]
def elabPTypeNamedTuple : CommandElab := fun stx => do
  let `(type $id:ident = ($[$flds:pNamedField],*)) := stx
    | throwUnsupportedSyntax
  let _ ← requireLocalPModuleCtx "type"
  if flds.isEmpty then
    throwError "type `{id.getId}` declared as a named-tuple must have at least one field"
  let ns ← getCurrNamespace
  addType
    { name      := id.getId
      leanName  := ns ++ id.getId
      kind      := PTypeKind.alias
      defStx    := some stx
      ref       := stx }

@[command_elab pTypeAlias]
def elabPTypeAlias : CommandElab := fun stx => do
  let `(type $id:ident = $_:term) := stx
    | throwUnsupportedSyntax
  let _ ← requireLocalPModuleCtx "type"
  let ns ← getCurrNamespace
  addType
    { name      := id.getId
      leanName  := ns ++ id.getId
      kind      := PTypeKind.alias
      defStx    := some stx
      ref       := stx }

@[command_elab pEnumDecl]
def elabPEnum : CommandElab := fun stx => do
  let `(enum $id:ident { $cases:ident,* }) := stx
    | throwUnsupportedSyntax
  let _ ← requireLocalPModuleCtx "enum"
  let caseArr := cases.getElems
  if caseArr.isEmpty then
    throwError "enum `{id.getId}` must have at least one case"
  let ns ← getCurrNamespace
  addType
    { name      := id.getId
      leanName  := ns ++ id.getId
      kind      := PTypeKind.enum
      enumCases := caseArr.map (·.getId)
      defStx    := some stx
      ref       := stx }

/-! ## Materialisation

Replay one type's saved `defStx` as a Lean def. Called by `#gen_module`
in `Syntax/Machine.lean` after machine type aliases (`abbrev MName :=
MachineRef`) have been emitted. -/

def materialiseType (d : PTypeDecl) : CommandElabM Unit := do
  match d.defStx with
  | none => pure ()  -- already materialised
  | some stx =>
    match d.kind with
    | .foreign =>
      let `(type $id:ident) := stx
        | throwErrorAt stx "internal error: foreign type defStx malformed"
      let pointedId := mkIdent (id.getId ++ `pointed)
      let neInstId  := mkIdent (id.getId ++ `nonempty)
      elabCommand (← `(opaque $pointedId : NonemptyType.{0}))
      elabCommand (← `(def $id : Type := $pointedId |>.type))
      elabCommand (← `(instance $neInstId:ident : Nonempty $id := $pointedId |>.property))
    | .alias =>
      -- Could be either a named-tuple or a plain alias; redispatch on the
      -- saved syntax shape.
      match stx with
      | `(type $id:ident = ($[$flds:pNamedField],*)) =>
        let fieldStxs ← flds.mapM fun f => do
          let fid : TSyntax `ident := ⟨f.raw[0]⟩
          let fty : TSyntax `term  := ⟨f.raw[2]⟩
          `(Lean.Parser.Command.structSimpleBinder| $fid:ident : $fty)
        -- Derive DecidableEq alongside Inhabited so the per-pmodule
        -- event union can derive DecidableEq when an event has a
        -- named-tuple payload.
        elabCommand (← `(
          structure $id where
            $[$fieldStxs]*
            deriving Inhabited, DecidableEq
        ))
      | `(type $id:ident = $rhs:term) =>
        elabCommand (← `(abbrev $id : Type := $rhs))
      | _ => throwErrorAt stx "internal error: alias type defStx malformed"
    | .enum =>
      let `(enum $id:ident { $cases:ident,* }) := stx
        | throwErrorAt stx "internal error: enum defStx malformed"
      let caseArr := cases.getElems
      let ctors ← caseArr.mapM fun c =>
        `(Lean.Parser.Command.ctor| | $c:ident : $id)
      elabCommand (← `(
        inductive $id where
          $ctors*
          deriving DecidableEq, Inhabited
      ))

end PLean
