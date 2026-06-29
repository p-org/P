/-
PLean.Syntax.Machine — `machine`, `spec`, `state`, `entry`, `on...`, `var`.

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

  2. **Materialisation** (`#gen_module M`, in `Commands/GenModule.lean`):
     with every machine registered, synthesise per-pmodule union types
     and machine wrappers, then replay the saved bodies as Lean defs over
     the real `PM`.

`#pwf` and `#pverify` require finalisation; they error if the module has
machines whose `body` is non-empty (i.e., not yet replayed).
-/
import Lean
import PLean.Internal.Decls
import PLean.Internal.Registry
import PLean.Syntax.Stmt

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
/-- `ignore <e1>, <e2>, …;` — declare a no-op handler for each listed
event without generating a body. The verifier skips ignored events
entirely (no per-handler triple, no auto-default obligation): an
ignored event is semantically equivalent to `on <e> { pure () }` whose
VC is vacuously satisfied because the state and buffers don't change.
Saves the SMT pass on shells like `state Won { ignore eNominate; }`. -/
syntax (name := pStateIgnore)       "ignore " ident,+ : pStateBodyItem

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

/-! ## Registration phase

Walk the parsed body to extract per-state metadata (`handles`,
`gotos`, …) — this is what `#pwf` checks against. The raw body
`Syntax` items are saved verbatim for replay at `#gen_module` time.
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
    | `(pStateBodyItem| ignore $evs:ident,*) =>
      for evStx in evs.getElems do
        let evN := evStx.getId
        if decl.handles.contains evN then
          throwErrorAt evStx "state `{sname}` already handles event `{evN}`"
        decl := { decl with
                    handles       := decl.handles.push evN
                    ignoredEvents := decl.ignoredEvents.push evN }
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

/-- Derive the events a machine receives: the union of every state's
`handles` set, dedup'd. Computed on demand because the body Syntax is
retained and may evolve in later phases (e.g. spec-machine emission). -/
def machineReceives (m : PMachineDecl) : Array Name :=
  dedupNames (m.states.flatMap (·.handles))

/-- Derive the events a machine sends: every bare-identifier event
named in a `send` statement anywhere in the body. -/
def machineSends (m : PMachineDecl) : Array Name :=
  dedupNames (m.body.flatMap collectSentEvents)

@[command_elab pMachineDecl]
def elabPMachineDecl : CommandElab := fun stx => do
  let `(machine $name:ident { $items:pMachineBodyItem* }) := stx
    | throwUnsupportedSyntax
  let modCtx ← requireLocalPModuleCtx "machine"
  let mname := name.getId
  let (states, body) ← collectMachineMetadata mname (items.map (·.raw))
  let leanName := modCtx.name ++ mname
  addMachine
    { name      := mname
      leanName  := leanName
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
  let leanName := modCtx.name ++ mname
  addMachine
    { name      := mname
      leanName  := leanName
      states    := states
      isSpec    := true
      observed  := obs
      body      := body
      ref       := stx }

end PLean
