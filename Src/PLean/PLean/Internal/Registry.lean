/-
PLean.Internal.Registry — env extensions for module aggregation.

Two tiers:

1. `localPModuleCtx` (scoped, per-file scratch) — the fragment of a `pmodule M`
   currently being elaborated. Set by the `pmodule M` command, mutated by
   declarations inside the block, cleared on Lean's builtin `end M`.

2. `pmoduleExt` (persistent, cross-file) — the merged registry of all
   completed `pmodule M` fragments, keyed on module name. Persists across
   `import` boundaries via `addImportedFn`. Each add-helper writes to BOTH
   tiers so cross-file consumers see partial work-in-progress modules
   correctly even before this file finishes elaborating.

Re-opening a `pmodule M` in a downstream file:
  parse `pmodule M`
  → look up `pmoduleExt[M]`
  → if present, restore that into `localPModuleCtx`
  → continue elaborating new declarations into both tiers
  → Lean's builtin `end M` closes the namespace; we don't intercept it,
    but we do clear `localPModuleCtx` via a secondary command if needed.

Re-declaring an existing name (e.g., redefining `ePing` after `import`) is
an error, surfaced by the per-decl helpers below.
-/
import Lean
import PLean.Internal.Decls

open Lean Elab Command

namespace PLean

/-- Per-file scratch: the `pmodule M` fragment currently open.

The `*Order` arrays preserve registration order for declarations whose
materialisation depends on it — for example, an enum referenced by a
named-tuple type must materialise *before* the type. NameMap iteration
is not insertion-ordered, so we keep a parallel array. -/
structure LocalPModuleCtx where
  name       : Name
  types      : NameMap PTypeDecl     := {}
  typeOrder  : Array Name            := #[]
  events     : NameMap PEventDecl    := {}
  eventOrder : Array Name            := #[]
  eventSets  : NameMap PEventSetDecl := {}
  machines   : NameMap PMachineDecl  := {}
  machineOrder : Array Name          := #[]
  invariants : NameMap PInvariantDecl := {}
  axioms     : NameMap PAxiomDecl    := {}
  instances  : NameMap PInstanceDecl := {}
  inits      : Array PInitDecl       := #[]
  pures      : NameMap PPureDecl     := {}
  deriving Inhabited

/-- Cross-file registry: merged fragments keyed on module name. -/
abbrev PModuleMap := NameMap LocalPModuleCtx

/-- The "currently open" pmodule, if any. -/
initialize localPModuleCtx : EnvExtension (Option LocalPModuleCtx) ←
  registerEnvExtension (pure none)

/-- Merge two fragments of the same module name (field-wise union). Used
    when two imports each contribute partial declarations for `M`. The
    `errIfDuplicate` checks at decl-registration time rule out *intra-file*
    name collisions, but two unrelated files could legitimately declare
    different events/machines under the same `pmodule`; we union them. If
    a name shows up in both fragments (e.g., a re-import cycle inserted
    the same decl twice), the second wins.

    NameMap.foldl iterates the second map and inserts each entry into the
    first; insert overwrites on collision. -/
private def mergeCtx (a b : LocalPModuleCtx) : LocalPModuleCtx :=
  -- For order arrays, prefer the imported (b) order over the prior (a) one
  -- only for entries that don't already appear in a. This preserves the
  -- registration order seen by the importing file while still picking up
  -- new declarations from the imported fragment.
  let mergeOrder (aOrder bOrder : Array Name) : Array Name := Id.run do
    let mut out := aOrder
    let aSet : NameSet := aOrder.foldl (init := {}) fun s n => s.insert n
    for n in bOrder do
      if !aSet.contains n then out := out.push n
    return out
  { name         := a.name
    types        := b.types.foldl      (fun m k v => m.insert k v) a.types
    typeOrder    := mergeOrder a.typeOrder b.typeOrder
    events       := b.events.foldl     (fun m k v => m.insert k v) a.events
    eventOrder   := mergeOrder a.eventOrder b.eventOrder
    eventSets    := b.eventSets.foldl  (fun m k v => m.insert k v) a.eventSets
    machines     := b.machines.foldl   (fun m k v => m.insert k v) a.machines
    machineOrder := mergeOrder a.machineOrder b.machineOrder
    invariants   := b.invariants.foldl (fun m k v => m.insert k v) a.invariants
    axioms       := b.axioms.foldl     (fun m k v => m.insert k v) a.axioms
    instances    := b.instances.foldl  (fun m k v => m.insert k v) a.instances
    inits        := a.inits ++ b.inits
    pures        := b.pures.foldl      (fun m k v => m.insert k v) a.pures
  }

/-- Persistent registry of completed module fragments. Imports from
    different files contributing to the same `pmodule M` are merged
    field-wise. -/
initialize pmoduleExt :
    PersistentEnvExtension (Name × LocalPModuleCtx) (Name × LocalPModuleCtx) PModuleMap ←
  registerPersistentEnvExtension {
    name             := `PLean.pmoduleExt
    mkInitial        := pure {}
    addImportedFn    := fun ass => do
      let mut m : PModuleMap := {}
      for arr in ass do
        for (n, ctx) in arr do
          match m.find? n with
          | none      => m := m.insert n ctx
          | some prev => m := m.insert n (mergeCtx prev ctx)
      return m
    -- In-file additions accumulate via `commit` (which calls setPModule on
    -- every decl); each call gives us the *full current* local ctx, so a
    -- straight insert is correct here — we are not trying to merge
    -- against any imported state at this point.
    addEntryFn       := fun m (n, ctx) => m.insert n ctx
    exportEntriesFn  := fun m =>
      m.foldl (init := #[]) fun acc n ctx => acc.push (n, ctx)
  }

/-- Read the currently-open module fragment. -/
def getLocalPModuleCtx? : CommandElabM (Option LocalPModuleCtx) := do
  return localPModuleCtx.getState (← getEnv)

/-- Read the currently-open module fragment, throwing if no `pmodule` is open. -/
def requireLocalPModuleCtx (declKind : String) : CommandElabM LocalPModuleCtx := do
  match ← getLocalPModuleCtx? with
  | some c => return c
  | none =>
    throwError "`{declKind}` declaration must appear inside a `pmodule … end` block"

/-- Replace the local module fragment. -/
def setLocalPModuleCtx (ctx : LocalPModuleCtx) : CommandElabM Unit := do
  modifyEnv fun env => localPModuleCtx.setState env (some ctx)

/-- Clear the local module fragment. -/
def clearLocalPModuleCtx : CommandElabM Unit := do
  modifyEnv fun env => localPModuleCtx.setState env none

/-- Look up a module fragment in the persistent registry. -/
def getPModule? (name : Name) : CommandElabM (Option LocalPModuleCtx) := do
  return (pmoduleExt.getState (← getEnv)).find? name

/-- Insert/update a module fragment in the persistent registry. -/
def setPModule (ctx : LocalPModuleCtx) : CommandElabM Unit := do
  modifyEnv fun env => pmoduleExt.addEntry env (ctx.name, ctx)

/-- All registered modules (used by `#print_pmodule` and tests). -/
def allPModules : CommandElabM (Array (Name × LocalPModuleCtx)) := do
  let m := pmoduleExt.getState (← getEnv)
  return m.foldl (init := #[]) fun acc n ctx => acc.push (n, ctx)

/-! ## Per-decl insertion helpers

Each helper:
  - reads the current local ctx (errors if no `pmodule` is open),
  - errors if `name` is already declared in this fragment,
  - writes the updated ctx back to BOTH the local and persistent tiers
    so cross-file readers always see the latest state.
-/

private def errIfDuplicate (kind : String) {α : Type}
    (m : NameMap α) (name : Name) (ref : Syntax) : CommandElabM Unit := do
  if m.contains name then
    withRef ref <| throwError "duplicate {kind} declaration: `{name}`"

/-- Atomically commit a new local ctx to both tiers. -/
private def commit (ctx : LocalPModuleCtx) : CommandElabM Unit := do
  setLocalPModuleCtx ctx
  setPModule ctx

def addType (d : PTypeDecl) : CommandElabM Unit := do
  let ctx ← requireLocalPModuleCtx "type"
  errIfDuplicate "type" ctx.types d.name d.ref
  commit { ctx with
    types     := ctx.types.insert d.name d
    typeOrder := ctx.typeOrder.push d.name }

def addEvent (d : PEventDecl) : CommandElabM Unit := do
  let ctx ← requireLocalPModuleCtx "event"
  errIfDuplicate "event" ctx.events d.name d.ref
  commit { ctx with
    events     := ctx.events.insert d.name d
    eventOrder := ctx.eventOrder.push d.name }

def addEventSet (d : PEventSetDecl) : CommandElabM Unit := do
  let ctx ← requireLocalPModuleCtx "eventset"
  errIfDuplicate "eventset" ctx.eventSets d.name d.ref
  commit { ctx with eventSets := ctx.eventSets.insert d.name d }

def addMachine (d : PMachineDecl) : CommandElabM Unit := do
  let kind := if d.isSpec then "spec" else "machine"
  let ctx ← requireLocalPModuleCtx kind
  errIfDuplicate kind ctx.machines d.name d.ref
  commit { ctx with
    machines     := ctx.machines.insert d.name d
    machineOrder := ctx.machineOrder.push d.name }

def addInvariant (d : PInvariantDecl) : CommandElabM Unit := do
  let ctx ← requireLocalPModuleCtx "invariant"
  errIfDuplicate "invariant" ctx.invariants d.name d.ref
  commit { ctx with invariants := ctx.invariants.insert d.name d }

def addAxiom (d : PAxiomDecl) : CommandElabM Unit := do
  let ctx ← requireLocalPModuleCtx "axiom"
  errIfDuplicate "axiom" ctx.axioms d.name d.ref
  commit { ctx with axioms := ctx.axioms.insert d.name d }

def addInstance (d : PInstanceDecl) : CommandElabM Unit := do
  let ctx ← requireLocalPModuleCtx "instance"
  errIfDuplicate "instance" ctx.instances d.name d.ref
  commit { ctx with instances := ctx.instances.insert d.name d }

def addInit (d : PInitDecl) : CommandElabM Unit := do
  let ctx ← requireLocalPModuleCtx "init"
  commit { ctx with inits := ctx.inits.push d }

def addPure (d : PPureDecl) : CommandElabM Unit := do
  let ctx ← requireLocalPModuleCtx "pure"
  errIfDuplicate "pure" ctx.pures d.name d.ref
  commit { ctx with pures := ctx.pures.insert d.name d }

end PLean
