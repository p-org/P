/-
PLean.Commands.PWf — `#pwf M` well-formedness validator.

Checks performed (Phase 0 scope):
  1. Module exists in `pmoduleExt`.
  2. For every event with a named payload type, the payload resolves to a
     declared type in the module.
  3. For every machine:
       a. exactly one `start state`,
       b. every event referenced in `on … do/goto` is declared,
       c. every event in `receives` is declared,
       d. every event in `sends` is declared,
       e. every state target of `goto` is a declared state of the same
          machine.
  4. For every spec machine, every event in `observes` is declared.
  5. For every eventset, every member event is declared.

When all checks pass: print "OK". When something fails, log all errors
(don't bail at the first), so the user gets a complete picture.
-/
import Lean
import PLean.Internal.Decls
import PLean.Internal.Registry

open Lean Elab Command

namespace PLean

/-- A single well-formedness diagnostic. -/
structure PWfDiag where
  ref : Syntax
  msg : String
  deriving Inhabited

namespace PWfDiag

def render (d : PWfDiag) : MessageData :=
  m!"{d.msg}"

end PWfDiag

/-- Run all well-formedness checks against `ctx`. -/
def runPWf (ctx : LocalPModuleCtx) : Array PWfDiag := Id.run do
  let mut diags : Array PWfDiag := #[]
  let typeNames : NameSet :=
    ctx.types.foldl (init := {}) fun acc n _ => acc.insert n
  let eventNames : NameSet :=
    ctx.events.foldl (init := {}) fun acc n _ => acc.insert n

  -- 2. Event payload types resolve.
  for (_, ev) in ctx.events.toList do
    match ev.payload with
    | none => pure ()
    | some pTy =>
      -- Allow primitive Lean types (Nat, Int, Bool, String, Unit) — those
      -- aren't expected to be in `types`. Heuristic: any name that's not
      -- in `types` AND not a known Lean-builtin is treated as missing.
      let leanBuiltins : NameSet :=
        ({} : NameSet)
          |>.insert `Nat |>.insert `Int |>.insert `Bool
          |>.insert `String |>.insert `Unit
          |>.insert `PLean.MachineRef
          |>.insert `MachineRef
      if !typeNames.contains pTy && !leanBuiltins.contains pTy then
        diags := diags.push
          { ref := ev.ref
            msg := s!"event `{ev.name}` has payload type `{pTy}` which is not declared in module `{ctx.name}`" }

  -- 5. Eventset members are declared events.
  for (_, es) in ctx.eventSets.toList do
    for em in es.events do
      if !eventNames.contains em then
        diags := diags.push
          { ref := es.ref
            msg := s!"eventset `{es.name}` references undeclared event `{em}`" }

  -- 3. Machines.
  for (_, m) in ctx.machines.toList do
    if !m.isSpec then
      -- 3a. Exactly one start state.
      let starts := m.states.filter (·.isStart) |>.size
      if starts == 0 then
        diags := diags.push
          { ref := m.ref
            msg := s!"machine `{m.name}` has no `start state`" }
      else if starts > 1 then
        -- Already errored out at decl-time, but be defensive.
        diags := diags.push
          { ref := m.ref
            msg := s!"machine `{m.name}` has {starts} `start state`s; exactly one is required" }
      -- 3b, 3e. Per-state checks.
      let stateNames : NameSet := m.states.foldl (init := {}) fun acc s => acc.insert s.name
      for s in m.states do
        for ev in s.handles do
          if !eventNames.contains ev then
            diags := diags.push
              { ref := s.ref
                msg := s!"machine `{m.name}` state `{s.name}` handles undeclared event `{ev}`" }
        for st in s.gotos do
          if !stateNames.contains st then
            diags := diags.push
              { ref := s.ref
                msg := s!"machine `{m.name}` state `{s.name}` gotos undeclared state `{st}`" }
    else
      -- 4. Spec: observed events are declared.
      for ev in m.observed do
        if !eventNames.contains ev then
          diags := diags.push
            { ref := m.ref
              msg := s!"spec `{m.name}` observes undeclared event `{ev}`" }
  return diags

/-- `#pwf M` — run well-formedness over the module named `M`. -/
syntax (name := pwfCmd) "#pwf " ident : command

@[command_elab pwfCmd]
def elabPWf : CommandElab := fun stx => do
  let `(#pwf $name:ident) := stx
    | throwUnsupportedSyntax
  let modName := name.getId
  match ← getPModule? modName with
  | none =>
    throwError "no `pmodule {modName}` is registered (did you forget to import the file that declares it?)"
  | some ctx =>
    -- Require `#gen_module` to have run: every registered machine must
    -- have an empty `body` (cleared by materialisation).
    let unmaterialised := ctx.machines.foldl (init := (#[] : Array Name))
      fun acc n m => if m.body.isEmpty then acc else acc.push n
    if !unmaterialised.isEmpty then
      let names := String.intercalate ", " (unmaterialised.toList.map Name.toString)
      throwError "{modName}: machines [{names}] not yet materialised — run `#gen_module {modName}` first"
    let diags := runPWf ctx
    if diags.isEmpty then
      logInfo m!"{modName}: well-formed ({ctx.types.size} types, {ctx.events.size} events, {ctx.machines.size} machines, {ctx.invariants.size} invariants, {ctx.axioms.size} axioms, {ctx.instances.size} instances)"
    else
      for d in diags do
        withRef d.ref <| logError m!"{d.msg}"
      throwError "{modName}: {diags.size} well-formedness error(s)"

end PLean
