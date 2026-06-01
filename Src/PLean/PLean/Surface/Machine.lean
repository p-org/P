/-
PLean.Surface.Machine — `machine`, `spec`, `state`, `entry`, `on...`, `var`.

Surface (close to P's grammar):

  machine MName {
    var server : BankServer
    var balance : Nat

    start state Init {
      entry (input : InitArgs) {
        server = input.server
        balance = input.balance
        goto Active
      }
    }

    state Active {
      on eRequest (req : Req) {
        send server, eResponse, (id = req.id, ok = true)
      }
      on eShutdown goto Done
    }

    state Done { }
  }

  spec SName observes [e1, e2] { ... }

## Two-phase elaboration

PVerifier's encoding makes the *whole* set of machines a single tagged
union (MachineAdt) — so a Lean type alias `MName := MachineRef` cannot
be emitted until every machine in the module is known. Likewise handler
bodies that reference sibling machines (`var server : Server` in `Client`)
need every machine name in scope to typecheck.

We therefore split machine handling into two phases:

  1. **Registration** (`machine M { … }`): we walk the body to extract
     the per-state metadata (`PStateDecl.handles`, `gotos`, etc.) and
     save the entire body as raw `Syntax` in `PMachineDecl.body`. We do
     NOT elaborate handler defs yet.

  2. **Materialisation** (`#gen_module M`): with every machine registered,
     emit `abbrev MName := MachineRef` for each one (so cross-references
     resolve), then replay the saved bodies as Lean defs.

`#pwf` and `#pverify` require finalisation; they error if the module has
machines whose `body` is non-empty (i.e., not yet replayed).
-/
import Lean
import PLean.Internal.Decls
import PLean.Internal.Registry
import PLean.Internal.Stub
import PLean.Surface.Stmt
import PLean.Surface.Types
import PLean.Surface.Events
import PLean.Surface.Verify

open Lean Elab Command

namespace PLean

/-! ## State-body items (parsed; elaborated lazily by `#gen_module`)

  entry { <doSeq> }
  entry ( <ident> : <term> ) { <doSeq> }
  on <ident> { <doSeq> }
  on <ident> ( <ident> : <term> ) { <doSeq> }
  on <ident> goto <ident>
-/

declare_syntax_cat pStateBodyItem

syntax (name := pStateEntry)        "entry " "{" doSeq "}" : pStateBodyItem
syntax (name := pStateEntryTyped)   "entry " "(" ident " : " term ")" "{" doSeq "}" : pStateBodyItem
syntax (name := pStateOnDoTyped)    "on " ident "(" ident " : " term ")" "{" doSeq "}" : pStateBodyItem
syntax (name := pStateOnDoUntyped)  "on " ident "{" doSeq "}" : pStateBodyItem
syntax (name := pStateOnGoto)       "on " ident " goto " ident : pStateBodyItem

/-! ## Machine-body items

  var <ident> : <term>
  start state <ident> { <stateBodyItem>* }
  state       <ident> { <stateBodyItem>* }
-/

declare_syntax_cat pMachineBodyItem

syntax (name := pMachineVar)
  "var " ident " : " term : pMachineBodyItem

syntax (name := pMachineStartState)
  "start " "state " ident "{" pStateBodyItem* "}" : pMachineBodyItem

syntax (name := pMachineState)
  "state " ident "{" pStateBodyItem* "}" : pMachineBodyItem

/-! ## Top-level commands

  machine MName { <machineBodyItem>* }
  spec    SName observes [e1, ...] { <machineBodyItem>* }
-/

syntax (name := pMachineDecl)
  "machine " ident "{" pMachineBodyItem* "}" : command

syntax (name := pSpecDecl)
  "spec " ident "observes" "[" ident,* "]" "{" pMachineBodyItem* "}" : command

/-! ## Registration phase (Phase 1 of two-phase elaboration)

Walk the parsed body to extract the per-state metadata (`handles`,
`gotos`, etc.) — this is what `#pwf` checks against. The raw body
`Syntax` items are saved verbatim for replay at finalisation time.
No Lean defs are produced yet.
-/

private def collectStateMetadata (sname : Name) (isStart : Bool) (ref : Syntax)
    (items : Array Syntax) : CommandElabM PStateDecl := do
  let mut decl : PStateDecl :=
    { name        := sname
      isStart     := isStart
      temperature := none
      handles     := #[]
      gotos       := #[]
      ref         := ref }
  for it in items do
    match it with
    | `(pStateBodyItem| entry { $_:doSeq }) => pure ()
    | `(pStateBodyItem| entry ( $_:ident : $_:term ) { $_:doSeq }) => pure ()
    | `(pStateBodyItem| on $ev:ident ( $_:ident : $_:term ) { $_:doSeq }) =>
      let evN := ev.getId
      if decl.handles.contains evN then
        throwErrorAt it "state `{sname}` already handles event `{evN}`"
      decl := { decl with handles := decl.handles.push evN }
    | `(pStateBodyItem| on $ev:ident { $_:doSeq }) =>
      let evN := ev.getId
      if decl.handles.contains evN then
        throwErrorAt it "state `{sname}` already handles event `{evN}`"
      decl := { decl with handles := decl.handles.push evN }
    | `(pStateBodyItem| on $ev:ident goto $tgt:ident) =>
      let evN := ev.getId
      if decl.handles.contains evN then
        throwErrorAt it "state `{sname}` already handles event `{evN}`"
      decl := { decl with handles := decl.handles.push evN
                          gotos   := decl.gotos.push tgt.getId }
    | _ => throwErrorAt it "unrecognised state body item"
  return decl

/-- Recursively collect the event names appearing in `send` statements
    anywhere within a syntax tree. Used to derive a machine's `sends` set
    from its handler bodies. Only bare-identifier events are collected;
    qualified or computed event expressions are skipped (they're rare and
    `#pwf` validates short names anyway). The event term sits at child
    index 3 of every send node (`send <target> , <ev> ...`). -/
private partial def collectSentEvents (stx : Syntax) : Array Name := Id.run do
  let mut acc : Array Name := #[]
  let k := stx.getKind
  if k == ``pSendNamed || k == ``pSendPayload || k == ``pSendNoPayload then
    let evStx := stx[3]
    if evStx.isIdent then
      acc := acc.push evStx.getId
  for arg in stx.getArgs do
    acc := acc ++ collectSentEvents arg
  return acc

/-- Order-preserving dedup. -/
private def dedupNames (xs : Array Name) : Array Name := Id.run do
  let mut seen : NameSet := {}
  let mut out : Array Name := #[]
  for x in xs do
    if !seen.contains x then
      seen := seen.insert x
      out := out.push x
  return out

/-- Walk the machine body, building per-state metadata and validating
    invariants (no nested machines, exactly one start state, no duplicates).
    Returns the populated states list and the raw body items (for replay). -/
private def collectMachineMetadata (mname : Name) (items : Array Syntax) :
    CommandElabM (Array PStateDecl × Array Syntax) := do
  let mut states : Array PStateDecl := #[]
  let bodySaved : Array Syntax := items
  for it in items do
    match it with
    | `(pMachineBodyItem| var $_:ident : $_:term) => pure ()
    | `(pMachineBodyItem| start state $sid:ident { $sitems:pStateBodyItem* }) => do
      let sname := sid.getId
      if states.any (·.name == sname) then
        throwErrorAt it "duplicate state declaration in machine `{mname}`: `{sname}`"
      if states.any (·.isStart) then
        throwErrorAt it "machine `{mname}` already has a `start` state"
      let decl ← collectStateMetadata sname (isStart := true) it (sitems.map (·.raw))
      states := states.push decl
    | `(pMachineBodyItem| state $sid:ident { $sitems:pStateBodyItem* }) => do
      let sname := sid.getId
      if states.any (·.name == sname) then
        throwErrorAt it "duplicate state declaration in machine `{mname}`: `{sname}`"
      let decl ← collectStateMetadata sname (isStart := false) it (sitems.map (·.raw))
      states := states.push decl
    | _ => throwErrorAt it "unrecognised machine body item"
  return (states, bodySaved)

@[command_elab pMachineDecl]
def elabPMachineDecl : CommandElab := fun stx => do
  let `(machine $name:ident { $items:pMachineBodyItem* }) := stx
    | throwUnsupportedSyntax
  let modCtx ← requireLocalPModuleCtx "machine"
  let mname := name.getId
  let (states, body) ← collectMachineMetadata mname (items.map (·.raw))
  -- Derive `receives` = union of events handled across states;
  -- `sends` = events named in `send` statements anywhere in the body.
  let receives := dedupNames (states.flatMap (·.handles))
  let sends := dedupNames (body.flatMap collectSentEvents)
  let leanName := modCtx.name ++ mname
  addMachine
    { name      := mname
      leanName  := leanName
      receives  := receives
      sends     := sends
      states    := states
      isSpec    := false
      observed  := #[]
      body      := body
      ref       := stx }

@[command_elab pSpecDecl]
def elabPSpecDecl : CommandElab := fun stx => do
  let modCtx ← requireLocalPModuleCtx "spec"
  -- Positions: 0="spec", 1=ident, 2="observes", 3="[", 4=idents,*, 5="]",
  --            6="{", 7=items*, 8="}".
  let nameStx : Ident := ⟨stx[1]⟩
  let mname := nameStx.getId
  let obs := stx[4].getSepArgs.map (·.getId)
  let itemsStx := stx[7].getArgs
  let (states, body) ← collectMachineMetadata mname itemsStx
  let receives := dedupNames (states.flatMap (·.handles))
  let sends := dedupNames (body.flatMap collectSentEvents)
  let leanName := modCtx.name ++ mname
  addMachine
    { name      := mname
      leanName  := leanName
      receives  := receives
      sends     := sends
      states    := states
      isSpec    := true
      observed  := obs
      body      := body
      ref       := stx }

/-! ## Materialisation phase

`#gen_module M` finalises a module by:
  1. Emitting `abbrev <m> := MachineRef` for every registered machine
     under the open `pmodule` namespace, so cross-machine type references
     in `var` and handler bodies resolve.
  2. For each machine, opening a sub-namespace and replaying the saved
     body items as actual Lean defs.
  3. Marking the machine as materialised (`body := #[]`) in the registry
     so `#gen_module M` is idempotent and `#pwf`/`#pverify` can check
     "is this module finalised?" by inspecting `body.isEmpty` for every
     machine.

The module's pmodule namespace must already be CLOSED before
`#gen_module M` runs (the user writes `end M` then `#gen_module M`).
This is so the `abbrev` and `namespace` commands here open the same
namespace cleanly.
-/

/-- Naming scheme for handler defs. The machine namespace is open at
    emission time, so we drop the machine prefix; the resulting Lean
    constant lives at `<pmodule>.<machine>.<state>.<kind>[ _handler]`. -/
private def handlerName (_mname sname : Name) (kind : Name) (suffix : Bool) : Ident :=
  let base := sname ++ kind
  mkIdent (if suffix then base.appendAfter "_handler" else base)

/-- Every handler def takes `this : MachineRef` as an explicit first
    parameter. We make it explicit (rather than a section `variable`)
    because Lean's `variable` doesn't reliably flow through
    `elabCommand`-level command sequences.

    Macro hygiene: writing `(this : ...)` directly inside a quotation
    would generate an *internally-scoped* `this` that doesn't match the
    user's source `this`. We construct the binder using `mkIdent` so the
    name is unhygienic and resolves against the user's references. -/
private def materialiseStateBodyItem (mname sname : Name) (item : Syntax) :
    CommandElabM Unit := do
  let thisIdent := mkIdent `this
  match item with
  | `(pStateBodyItem| entry { $body:doSeq }) =>
    let defName := handlerName mname sname `entry (suffix := false)
    elabCommand (← `(
      def $defName ($thisIdent : PLean.Stub.MachineRef) : PLean.Stub.PM Unit := do $body
    ))
  | `(pStateBodyItem| entry ( $param:ident : $ty:term ) { $body:doSeq }) =>
    let defName := handlerName mname sname `entry (suffix := false)
    elabCommand (← `(
      def $defName ($thisIdent : PLean.Stub.MachineRef) ($param : $ty) : PLean.Stub.PM Unit := do $body
    ))
  | `(pStateBodyItem| on $ev:ident ( $param:ident : $ty:term ) { $body:doSeq }) =>
    let defName := handlerName mname sname ev.getId (suffix := true)
    elabCommand (← `(
      def $defName ($thisIdent : PLean.Stub.MachineRef) ($param : $ty) : PLean.Stub.PM Unit := do $body
    ))
  | `(pStateBodyItem| on $ev:ident { $body:doSeq }) =>
    let defName := handlerName mname sname ev.getId (suffix := true)
    elabCommand (← `(
      def $defName ($thisIdent : PLean.Stub.MachineRef) : PLean.Stub.PM Unit := do $body
    ))
  | `(pStateBodyItem| on $_:ident goto $_:ident) =>
    -- Pure transitions emit no def; they're a registry-only artefact.
    pure ()
  | _ => throwErrorAt item "unrecognised state body item (during materialisation)"

private def materialiseMachineBody (mname : Name) (items : Array Syntax) :
    CommandElabM Unit := do
  for it in items do
    match it with
    | `(pMachineBodyItem| var $vname:ident : $vty:term) =>
      -- Emit `variable (vname : vty)` so subsequent handler defs see it
      -- as a free parameter.
      elabCommand (← `(variable ($vname : $vty)))
    | `(pMachineBodyItem| start state $sid:ident { $sitems:pStateBodyItem* }) =>
      let sname := sid.getId
      for sit in sitems do materialiseStateBodyItem mname sname sit
    | `(pMachineBodyItem| state $sid:ident { $sitems:pStateBodyItem* }) =>
      let sname := sid.getId
      for sit in sitems do materialiseStateBodyItem mname sname sit
    | _ => throwErrorAt it "unrecognised machine body item (during materialisation)"

syntax (name := pGenModule) "#gen_module " ident : command

@[command_elab pGenModule]
def elabPGenModule : CommandElab := fun stx => do
  let `(#gen_module $name:ident) := stx
    | throwUnsupportedSyntax
  let modName := name.getId
  match ← getPModule? modName with
  | none =>
    throwError "no `pmodule {modName}` is registered (did you import the file that declares it?)"
  | some ctx =>
    -- Open the pmodule namespace so all materialised aliases / structures /
    -- handler defs live under <Mod>...
    elabCommand (← `(namespace $name))
    -- Step 1: emit type-alias forwards for every machine (registration
    -- order). This makes `var server : Server` resolve in another
    -- machine, and lets type aliases reference machine names.
    for mname in ctx.machineOrder do
      let some m := ctx.machines.find? mname | continue
      if m.body.isEmpty then continue
      let mid := mkIdent m.name
      elabCommand (← `(abbrev $mid : Type := PLean.Stub.MachineRef))
    -- Step 2: materialise every type/enum in REGISTRATION ORDER.
    -- Crucial for named-tuple types referencing earlier-declared enums:
    -- if `enum E` was registered before `type T = (... : E)`, we want to
    -- materialise E first so that the structure-typed T sees E in scope.
    -- NameMap iteration is alphabetical, which would silently break this.
    for tname in ctx.typeOrder do
      let some t := ctx.types.find? tname | continue
      materialiseType t
    -- Step 3: materialise every event in registration order. Payload-type
    -- abbrevs depend on materialised types from step 2.
    for ename in ctx.eventOrder do
      let some e := ctx.events.find? ename | continue
      materialiseEvent e
    -- Step 4: replay each machine body inside its own sub-namespace.
    -- Each handler def takes `this : MachineRef` as an explicit first
    -- parameter (injected in `materialiseStateBodyItem`). We `open` the
    -- parent module namespace inside each machine so that handler bodies
    -- can reference module-level types/events with their short names.
    for mname in ctx.machineOrder do
      let some m := ctx.machines.find? mname | continue
      if m.body.isEmpty then continue
      let mid := mkIdent m.name
      elabCommand (← `(namespace $mid))
      elabCommand (← `(open $name:ident))
      materialiseMachineBody m.name m.body
      elabCommand (← `(end $mid))
    -- Step 5: materialise verification declarations. They may reference
    -- machine vars and event payloads, so they go after machine bodies.
    for (_, d) in ctx.invariants.toList do materialiseInvariant d
    for (_, d) in ctx.axioms.toList do     materialiseAxiom d
    for d in ctx.inits do                  materialiseInit d
    for (_, d) in ctx.pures.toList do      materialisePure d
    for (_, d) in ctx.instances.toList do  materialiseInstance d
    elabCommand (← `(end $name))
    -- Step 6: mark everything as materialised.
    let machines' := ctx.machines.foldl (init := ({} : NameMap PMachineDecl))
      fun acc n d => acc.insert n { d with body := #[] }
    let types'    := ctx.types.foldl (init := ({} : NameMap PTypeDecl))
      fun acc n d => acc.insert n { d with defStx := none }
    let events'   := ctx.events.foldl (init := ({} : NameMap PEventDecl))
      fun acc n d => acc.insert n { d with defStx := none }
    let invs'     := ctx.invariants.foldl (init := ({} : NameMap PInvariantDecl))
      fun acc n d => acc.insert n { d with defStx := none }
    let axs'      := ctx.axioms.foldl (init := ({} : NameMap PAxiomDecl))
      fun acc n d => acc.insert n { d with defStx := none }
    let pures'    := ctx.pures.foldl (init := ({} : NameMap PPureDecl))
      fun acc n d => acc.insert n { d with defStx := none }
    let insts'    := ctx.instances.foldl (init := ({} : NameMap PInstanceDecl))
      fun acc n d => acc.insert n { d with defStx := none }
    let inits'    := ctx.inits.map fun d => { d with defStx := none }
    setPModule
      { ctx with
        machines := machines', types := types', events := events'
        invariants := invs', axioms := axs', pures := pures'
        instances := insts', inits := inits' }

end PLean
