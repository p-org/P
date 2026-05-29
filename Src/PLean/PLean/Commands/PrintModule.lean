/-
PLean.Commands.PrintModule — `#print_pmodule M`.

Dumps the registered fragment for diagnostic / debugging use. Intended for
both human inspection and #guard_msgs-based regression tests.
-/
import Lean
import PLean.Internal.Decls
import PLean.Internal.Registry

open Lean Elab Command

namespace PLean

private def renderType (d : PTypeDecl) : String :=
  match d.kind with
  | .foreign => "  type " ++ d.name.toString ++ "                              -- foreign"
  | .alias   => "  type " ++ d.name.toString ++ " = ...                          -- alias"
  | .enum    =>
    let cases := String.intercalate ", " (d.enumCases.toList.map Name.toString)
    "  enum " ++ d.name.toString ++ " { " ++ cases ++ " }"

private def renderEvent (d : PEventDecl) : String :=
  match d.payload with
  | none    => "  event " ++ d.name.toString
  | some pt => "  event " ++ d.name.toString ++ " : " ++ pt.toString

private def renderEventSet (d : PEventSetDecl) : String :=
  let evs := String.intercalate ", " (d.events.toList.map Name.toString)
  "  eventset " ++ d.name.toString ++ " = { " ++ evs ++ " }"

private def renderState (d : PStateDecl) : String :=
  let kw := if d.isStart then "start state" else "state"
  let h :=
    if d.handles.isEmpty then ""
    else " handles=[" ++ String.intercalate "," (d.handles.toList.map Name.toString) ++ "]"
  let g :=
    if d.gotos.isEmpty then ""
    else " gotos=[" ++ String.intercalate "," (d.gotos.toList.map Name.toString) ++ "]"
  "    " ++ kw ++ " " ++ d.name.toString ++ h ++ g

private def renderMachine (d : PMachineDecl) : String :=
  let kw := if d.isSpec then "spec" else "machine"
  let header :=
    if d.isSpec then
      let obs := String.intercalate "," (d.observed.toList.map Name.toString)
      "  " ++ kw ++ " " ++ d.name.toString ++ " observes [" ++ obs ++ "] {"
    else
      let r := String.intercalate "," (d.receives.toList.map Name.toString)
      let s := String.intercalate "," (d.sends.toList.map Name.toString)
      "  " ++ kw ++ " " ++ d.name.toString ++ " receives [" ++ r ++ "] sends [" ++ s ++ "] {"
  let stateLines := d.states.toList.map renderState
  String.intercalate "\n" (header :: stateLines ++ ["  }"])

private def renderInvariant (d : PInvariantDecl) : String :=
  "  invariant " ++ d.name.toString

private def renderAxiom (d : PAxiomDecl) : String :=
  "  axiom " ++ d.name.toString

private def renderInstance (d : PInstanceDecl) : String :=
  "  instance " ++ d.name.toString ++ " : " ++ d.typeRepr

private def renderPure (d : PPureDecl) : String :=
  let kw := if d.hasBody then "pure" else "pure (foreign)"
  "  " ++ kw ++ " " ++ d.name.toString

syntax (name := printPModuleCmd) "#print_pmodule " ident : command

@[command_elab printPModuleCmd]
def elabPrintPModule : CommandElab := fun stx => do
  let `(#print_pmodule $name:ident) := stx
    | throwUnsupportedSyntax
  let modName := name.getId
  match ← getPModule? modName with
  | none =>
    throwError "no `pmodule {modName}` is registered"
  | some ctx =>
    let mut lines : Array String := #[]
    lines := lines.push ("pmodule " ++ ctx.name.toString)
    for (_, d) in ctx.types.toList do      lines := lines.push (renderType d)
    for (_, d) in ctx.events.toList do     lines := lines.push (renderEvent d)
    for (_, d) in ctx.eventSets.toList do  lines := lines.push (renderEventSet d)
    for (_, d) in ctx.machines.toList do   lines := lines.push (renderMachine d)
    for (_, d) in ctx.invariants.toList do lines := lines.push (renderInvariant d)
    for (_, d) in ctx.axioms.toList do     lines := lines.push (renderAxiom d)
    for (_, d) in ctx.instances.toList do  lines := lines.push (renderInstance d)
    for _ in ctx.inits do                  lines := lines.push "  init ..."
    for (_, d) in ctx.pures.toList do      lines := lines.push (renderPure d)
    lines := lines.push ("end " ++ ctx.name.toString)
    let body := String.intercalate "\n" lines.toList
    logInfo body

end PLean
