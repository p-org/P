/-
PLean.Surface.Verify — `invariant`, `paxiom`, `init-holds`, `function`, `pinstance`.

  invariant <name> : <prop>
  paxiom    <name> : <prop>
  init-holds <prop>          -- assume-on-start
  function  <name> (params) : <retT> = <expr>     -- defined helper
  function  <name> (params) : <retT>              -- foreign helper
  pinstance <name> : <Class> <T>                  -- axiom bundle

`init-holds` is hyphenated to avoid colliding with Lean's `init :=` named
argument syntax. `function` matches P's existing `fun` decl spelling
(without the keyword collision Lean's `pure` would trigger).

## Two-phase elaboration

All verification declarations defer to `#gen_module` time, just like
machines/types/events. An invariant body may reference any module-level
name (machine vars, event payloads, types), so eager elaboration would
fail in any but the most trivial cases.
-/
import Lean
import PLean.Internal.Decls
import PLean.Internal.Registry

open Lean Elab Command

namespace PLean

/-! ## Invariant -/

syntax (name := pInvariant) "invariant " ident " : " term : command

@[command_elab pInvariant]
def elabPInvariant : CommandElab := fun stx => do
  let `(invariant $id:ident : $_:term) := stx
    | throwUnsupportedSyntax
  let _ ← requireLocalPModuleCtx "invariant"
  let ns ← getCurrNamespace
  addInvariant
    { name := id.getId, leanName := ns ++ id.getId, defStx := some stx, ref := stx }

/-! ## Axiom (single-prop) -/

syntax (name := pAxiom) "paxiom " ident " : " term : command

@[command_elab pAxiom]
def elabPAxiom : CommandElab := fun stx => do
  let `(paxiom $id:ident : $_:term) := stx
    | throwUnsupportedSyntax
  let _ ← requireLocalPModuleCtx "paxiom"
  let ns ← getCurrNamespace
  addAxiom
    { name := id.getId, leanName := ns ++ id.getId, defStx := some stx, ref := stx }

/-! ## Init (assume-on-start) -/

syntax (name := pInit) "init-holds " term : command

@[command_elab pInit]
def elabPInit : CommandElab := fun stx => do
  let _ ← requireLocalPModuleCtx "init-holds"
  addInit { defStx := some stx, ref := stx }

/-! ## Pure (defined or foreign) -/

syntax (name := pPureDefined)
  "function " ident bracketedBinder* " : " term " = " term : command

syntax (name := pPureForeign)
  "function " ident bracketedBinder* " : " term : command

@[command_elab pPureDefined]
def elabPPureDefined : CommandElab := fun stx => do
  let `(function $id:ident $_:bracketedBinder* : $_:term = $_:term) := stx
    | throwUnsupportedSyntax
  let _ ← requireLocalPModuleCtx "function"
  let ns ← getCurrNamespace
  addPure
    { name := id.getId, leanName := ns ++ id.getId
      hasBody := true, defStx := some stx, ref := stx }

@[command_elab pPureForeign]
def elabPPureForeign : CommandElab := fun stx => do
  let `(function $id:ident $_:bracketedBinder* : $_:term) := stx
    | throwUnsupportedSyntax
  let _ ← requireLocalPModuleCtx "function"
  let ns ← getCurrNamespace
  addPure
    { name := id.getId, leanName := ns ++ id.getId
      hasBody := false, defStx := some stx, ref := stx }

/-! ## Pinstance (axiom bundle, Veil-style)

  `pinstance nm : Class T`  ↝  `variable [nm : Class T]`
-/

syntax (name := pInstance) "pinstance " ident " : " term : command

@[command_elab pInstance]
def elabPInstance : CommandElab := fun stx => do
  let `(pinstance $id:ident : $tp:term) := stx
    | throwUnsupportedSyntax
  let _ ← requireLocalPModuleCtx "pinstance"
  let typeRepr := tp.raw.prettyPrint.pretty
  addInstance
    { name := id.getId, classRepr := typeRepr, typeRepr := typeRepr
      defStx := some stx, ref := stx }

/-! ## Materialisation

Replay each verification declaration as a Lean def. Called by
`#gen_module` after machines have been materialised so invariant /
axiom bodies can reference machine fields and event payloads. -/

def materialiseInvariant (d : PInvariantDecl) : CommandElabM Unit := do
  match d.defStx with
  | none => pure ()
  | some stx =>
    let `(invariant $id:ident : $prop:term) := stx
      | throwErrorAt stx "internal error: invariant defStx malformed"
    elabCommand (← `(def $id : Prop := $prop))

def materialiseAxiom (d : PAxiomDecl) : CommandElabM Unit := do
  match d.defStx with
  | none => pure ()
  | some stx =>
    let `(paxiom $id:ident : $prop:term) := stx
      | throwErrorAt stx "internal error: paxiom defStx malformed"
    elabCommand (← `(axiom $id : $prop))

def materialiseInit (d : PInitDecl) : CommandElabM Unit := do
  -- Phase 0: `init` registers the prop only; no Lean def emitted. Phase 1
  -- will bind it to the initial-state precondition.
  match d.defStx with
  | none => pure ()
  | some _ => pure ()

def materialisePure (d : PPureDecl) : CommandElabM Unit := do
  match d.defStx with
  | none => pure ()
  | some stx =>
    match stx with
    | `(function $id:ident $binders:bracketedBinder* : $ret:term = $body:term) =>
      elabCommand (← `(def $id $binders* : $ret := $body))
    | `(function $id:ident $binders:bracketedBinder* : $ret:term) =>
      elabCommand (← `(opaque $id $binders* : $ret))
    | _ => throwErrorAt stx "internal error: function defStx malformed"

def materialiseInstance (d : PInstanceDecl) : CommandElabM Unit := do
  match d.defStx with
  | none => pure ()
  | some stx =>
    let `(pinstance $id:ident : $tp:term) := stx
      | throwErrorAt stx "internal error: pinstance defStx malformed"
    elabCommand (← `(variable [$id : $tp]))

end PLean
