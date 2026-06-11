/-
PLean.Commands.GenModule — `#gen_module M` finalisation command.

Phase 2: walks the registry and emits, in dependency order:

  1. Per-machine wrapper structs (D10):
        structure <Mod>.<MName> where ref : MachineRef
          deriving DecidableEq, Inhabited
        instance : Coe <Mod>.<MName> MachineRef := ⟨<Mod>.<MName>.ref⟩
  2. Types and enums (foreign / alias / named-tuple).
  3. Events: per-event payload abbrev `<ev>_payload`.
  4. The per-pmodule unions:
        inductive <Mod>.E ...                       -- one ctor per event
        inductive <Mod>.G | unit                    -- goto payload (Unit-like)
        inductive <Mod>.S ...                       -- one ctor per (machine, state)
        structure <Mod>.Fields ...                  -- one field per var across all machines
        abbrev <Mod>.Sig : ProgramSig := ...
        abbrev <Mod>.PM' (α : Type) := PM Sig α
        abbrev <Mod>.GS := GlobalState Sig
  5. `#derive_lifted_wp` for `get`/`set` on `GS` (D14).
  6. Per-machine var accessors `<MName>.<vname>_get` / `_set`,
     state-tag aliases `<MName>.<sname>_st`, and replayed handler defs.
  7. Verification declarations (invariant, paxiom, init, function, pinstance).

Decisions: D8 (one Sig per pmodule), D10 (typed machine wrappers),
D11 (handlers over real PM with typed `this`), D12 (real var fields),
D13 (real `goto`), D14 (`#derive_lifted_wp`), D15 (Stub retirement).

## A note on macro hygiene

This file constructs syntax for command emission. Bare identifiers
inside `\`(...)` quotations (e.g., `E`, `Sig`, `this`) get hygiene
marks during macro expansion. The user-facing names we emit must NOT
be hygienic — `<Mod>.E`, `<Mod>.Sig`, etc. live in the user's
namespace and are referenced by handler bodies (which were macro-
expanded earlier and saw the bare names without hygiene marks). To
avoid mismatch, every binder/identifier we want resolved against
those user-namespace names is constructed via `mkIdent` and spliced
in. Lean handles `.field` projections and qualified names like
`PLean.send` correctly without further intervention.
-/
import Lean
import PLean.Internal.Decls
import PLean.Internal.Registry
import PLean.Surface.Types
import PLean.Surface.Events
import PLean.Surface.Machine
import PLean.Surface.Verify
import PLean.Surface.Stmt
import PLean.Semantics.Monad
import PLean.Semantics.Primitives
import PLean.Semantics.Predicates
import PLean.Semantics.Default
import Loom.Meta

open Lean Elab Command

namespace PLean

/-! ## Var extraction

Walk a machine's saved body items and pull out each `var <name> : <ty>`
declaration. The result feeds (a) the per-machine `Fields` struct,
(b) the union `<Mod>.Fields`, and (c) the `<vname>_get` / `<vname>_set`
accessors. -/

structure VarInfo where
  name : Name
  ty   : TSyntax `term

private def collectVars (body : Array Syntax) : CommandElabM (Array VarInfo) := do
  let mut vars : Array VarInfo := #[]
  for it in body do
    match it with
    | `(pMachineBodyItem| var $vname:ident : $vty:term) =>
      vars := vars.push { name := vname.getId, ty := vty }
    | _ => pure ()
  return vars

/-! ## Unhygienic identifier helpers (see file-level note on hygiene). -/

private def idE        : Ident := mkIdent `E
private def idG        : Ident := mkIdent `G
private def idS        : Ident := mkIdent `S
private def idFields   : Ident := mkIdent `Fields
private def idSig      : Ident := mkIdent `Sig
private def idPM       : Ident := mkIdent `PM'
private def idGS       : Ident := mkIdent `GS
private def idThis     : Ident := mkIdent `this
private def idGet      : Ident := mkIdent `get
private def idSet      : Ident := mkIdent `set
private def idDivM     : Ident := mkIdent `DivM
private def idMachineRef : Ident := mkIdent `PLean.MachineRef
private def idProgramSig : Ident := mkIdent `PLean.ProgramSig
private def idPM_PLean   : Ident := mkIdent `PLean.PM
private def idGlobalState : Ident := mkIdent `PLean.GlobalState

/-! ## Step 1: per-machine wrapper structs (D10) -/

private def emitMachineWrappers (ctx : LocalPModuleCtx) : CommandElabM Unit := do
  for mname in ctx.machineOrder do
    let some _ := ctx.machines.find? mname | continue
    let mid := mkIdent mname
    elabCommand (← `(
      structure $mid where
        ref : $idMachineRef
        deriving Inhabited, DecidableEq
    ))
    elabCommand (← `(
      instance : Coe $mid $idMachineRef := ⟨fun s => s.ref⟩
    ))

/-! ## Step 1b: machine-kind machinery (D20)

For each pmodule, emit:
  inductive <Mod>.MKind | <M1> | <M2> | ... deriving DecidableEq, Inhabited
  def <Mod>.<M>_kind : Nat := <i>     -- index ≥ 1 (0 reserved per R20)

Plus a per-machine `<M>.allocated (m : MachineRef) (s : GS) : Prop`
predicate that asserts a machine ref's `kind` matches its expected
ctor. The `is` macro extends to dispatch on machine names by
expanding `m is <M>` to `<M>.allocated m`.

Spec machines participate in the kind tagging too (decision: their kind
is recognisable in invariants even though Phase 4 runs spec handlers
specially). This costs nothing now and avoids re-emitting MKind later. -/

private def idMKind  : Ident := mkIdent `MKind

private def emitMachineKinds (ctx : LocalPModuleCtx) : CommandElabM Unit := do
  -- Filter to "real" (impl + spec) machines in the module.
  let mut allKinds : Array Name := #[]
  for mname in ctx.machineOrder do
    let some _ := ctx.machines.find? mname | continue
    allKinds := allKinds.push mname
  -- Emit `<Mod>.MKind`. Always include a `_none` placeholder so the
  -- inductive is non-empty even in the (degenerate) zero-machine case.
  let mut kCtors : Array (TSyntax ``Lean.Parser.Command.ctor) := #[]
  let noneCtor ← `(Lean.Parser.Command.ctor| | _none)
  kCtors := kCtors.push noneCtor
  for mn in allKinds do
    let cId := mkIdent mn
    let ctor ← `(Lean.Parser.Command.ctor| | $cId:ident)
    kCtors := kCtors.push ctor
  elabCommand (← `(
    inductive $idMKind where
      $kCtors*
      deriving DecidableEq, Inhabited
  ))
  -- Emit `<Mod>.<M>_kind : Nat := i` (i ≥ 1; 0 reserved for unset).
  let mut idx : Nat := 1
  for mn in allKinds do
    let kindNameId := mkIdent (mn.appendAfter "_kind")
    let lit := Syntax.mkNatLit idx
    elabCommand (← `(
      def $kindNameId : Nat := $lit
    ))
    idx := idx + 1
  -- Per-machine `<M>_allocated` and top-level `is_<M>` predicates.
  --
  -- The wrapper struct `<Mod>.<M>` is *both* a structure and a
  -- target namespace; emitting `def allocated` inside `namespace M`
  -- proved unreliable (the structure declaration claims the slot
  -- first). We instead emit a flat top-level `<Mod>.<M>_allocated`
  -- and an `is_<M>` alias used by the surface `is` macro.
  --
  -- `is_<M>` is `MachineRef → GS → Prop` (Curry of `<M>_allocated`),
  -- mirroring the `is_<ev>` shape so the surface `is` macro can
  -- dispatch uniformly: `m is <M>` expands to `is_<M> m` (a
  -- `GS → Prop`).
  for mn in allKinds do
    let kindNameId := mkIdent (mn.appendAfter "_kind")
    let allocName : Ident := mkIdent (mn.appendAfter "_allocated")
    -- PLAN_P3 R20 mitigation: `kind ≠ 0` excludes default-initialised
    -- (uninitialised) machines from any `<M>_allocated` predicate.
    -- Without this, a `MachineRef` whose state was never explicitly
    -- assigned a kind would satisfy `<M0>_allocated` for the first
    -- declared machine M0 (since both have kind 0), violating the
    -- "real machine kinds are ≥ 1" invariant.
    elabCommand (← `(
      @[inline] def $allocName (m : $idMachineRef) (s : $idGS) : Prop :=
        (s.machines m).kind ≠ 0 ∧ (s.machines m).kind = $kindNameId
    ))
  for mn in allKinds do
    let predName : Ident := mkIdent (Name.mkSimple ("is_" ++ mn.toString))
    let allocName : Ident := mkIdent (mn.appendAfter "_allocated")
    elabCommand (← `(
      @[inline] def $predName (m : $idMachineRef) : $idGS → Prop :=
        $allocName m
    ))

/-! ## Step 4: union types `<Mod>.E`, `<Mod>.G`, `<Mod>.S`, `<Mod>.Fields` -/

private def emitProgramUnions (ctx : LocalPModuleCtx)
    (machineVars : NameMap (Array VarInfo)) : CommandElabM Unit := do
  -- `<Mod>.E`: one ctor per event.
  let evCtors ← ctx.eventOrder.mapM fun en => do
    let some e := ctx.events.find? en
      | throwError "internal: event {en} missing"
    let cId := mkIdent e.name
    match e.payload with
    | none =>
      `(Lean.Parser.Command.ctor| | $cId:ident)
    | some _ =>
      let pty := mkIdent (e.name.appendAfter "_payload")
      `(Lean.Parser.Command.ctor| | $cId:ident (payload : $pty))
  if evCtors.isEmpty then
    elabCommand (← `(
      inductive $idE where
        | _none
        deriving DecidableEq, Inhabited
    ))
  else
    elabCommand (← `(
      inductive $idE where
        $evCtors*
        deriving DecidableEq
    ))
    -- An Inhabited instance referencing the first event constructor.
    let firstName := ctx.eventOrder[0]!
    let some firstE := ctx.events.find? firstName
      | throwError "internal: event {firstName} missing"
    let firstId := mkIdent firstE.name
    let firstFull := mkIdent (`E ++ firstE.name)
    match firstE.payload with
    | none =>
      let _ := firstId
      elabCommand (← `(
        instance : Inhabited $idE := ⟨$firstFull⟩
      ))
    | some _ =>
      elabCommand (← `(
        instance : Inhabited $idE := ⟨$firstFull default⟩
      ))
  -- `<Mod>.G`: goto payload union. Phase-2 surface doesn't expose goto
  -- payloads; one trivial constructor suffices.
  elabCommand (← `(
    inductive $idG where
      | unit
      deriving DecidableEq, Inhabited
  ))
  -- `<Mod>.S`: one ctor per (machine, state).
  let mut sCtors : Array (TSyntax ``Lean.Parser.Command.ctor) := #[]
  for mname in ctx.machineOrder do
    let some m := ctx.machines.find? mname | continue
    for sd in m.states do
      let cName := (mname.toString ++ "_" ++ sd.name.toString)
      let cId : Ident := mkIdent (Name.mkSimple cName)
      let ctor ← `(Lean.Parser.Command.ctor| | $cId:ident)
      sCtors := sCtors.push ctor
  if sCtors.isEmpty then
    elabCommand (← `(
      inductive $idS where
        | _none
        deriving DecidableEq, Inhabited
    ))
  else
    elabCommand (← `(
      inductive $idS where
        $sCtors*
        deriving DecidableEq, Inhabited
    ))
  -- `<Mod>.Fields`: one field per var across all machines, prefixed by
  -- machine name.
  let mut fieldStxs : Array (TSyntax ``Lean.Parser.Command.structSimpleBinder) := #[]
  for mname in ctx.machineOrder do
    let some _ := ctx.machines.find? mname | continue
    let some vars := machineVars.find? mname | continue
    for v in vars do
      let fldName := (mname.toString ++ "_" ++ v.name.toString)
      let fldId : Ident := mkIdent (Name.mkSimple fldName)
      let s ← `(Lean.Parser.Command.structSimpleBinder| $fldId:ident : $(v.ty))
      fieldStxs := fieldStxs.push s
  if fieldStxs.isEmpty then
    elabCommand (← `(
      structure $idFields where
        deriving Inhabited
    ))
  else
    elabCommand (← `(
      structure $idFields where
        $[$fieldStxs]*
        deriving Inhabited
    ))
  -- `<Mod>.Sig`, `<Mod>.PM'`, `<Mod>.GS` aliases.
  elabCommand (← `(
    abbrev $idSig : $idProgramSig :=
      { E := $idE, G := $idG, S := $idS, F := $idFields }
  ))
  elabCommand (← `(
    abbrev $idPM (α : Type) := $idPM_PLean $idSig α
  ))
  elabCommand (← `(
    abbrev $idGS : Type := $idGlobalState $idSig
  ))

/-! ## Step 4b: per-event tag-check predicates `is_<ev>`

The `is` notation in `Surface/Notation.lean` rewrites `lbl is <ev>` to
`is_<ev> lbl` — a *tag-only* check (matching P's surface semantics).
Emitting one per event side-steps the awkwardness of "constructor as
first-class value" in Lean: the user never has to write a payload to
do a tag check. -/

private def emitIsPredicates (ctx : LocalPModuleCtx) : CommandElabM Unit := do
  for ename in ctx.eventOrder do
    let some e := ctx.events.find? ename | continue
    let predName : Ident := mkIdent (Name.mkSimple ("is_" ++ e.name.toString))
    let evCtor : Ident := mkIdent (`E ++ e.name)
    -- The predicate inspects `lbl.action`: True iff the action is an
    -- event whose ctor is `<ev>`. Payload (if any) is ignored.
    match e.payload with
    | none =>
      elabCommand (← `(
        @[inline] def $predName (lbl : ($idSig).Label) : Prop :=
          match lbl.action with
          | .event $evCtor => True
          | _ => False
      ))
    | some _ =>
      elabCommand (← `(
        @[inline] def $predName (lbl : ($idSig).Label) : Prop :=
          match lbl.action with
          | .event ($evCtor _) => True
          | _ => False
      ))

/-! ## Step 5: `#derive_lifted_wp` for the per-pmodule `get`/`set`

These register `loomSpec` lemmas that teach `wpgen` how to step through
state reads and writes. Without them, surface-emitted handler triples
stall at the first `← get`. -/

private def emitDerivedWP : CommandElabM Unit := do
  -- The `open` is needed so `MAlgOrdered` and the lifted WP machinery
  -- find the scoped instances. We *don't* bake it into the surrounding
  -- file (that would force every importer into partial-correctness +
  -- demonic-choice mode); we open it just for the duration of the
  -- `#derive_lifted_wp` call.
  let pcId : Ident := mkIdent `PartialCorrectness
  let dcId : Ident := mkIdent `DemonicChoice
  elabCommand (← `(open $pcId:ident $dcId:ident in
    #derive_lifted_wp for
      ($idGet : StateT $idGS $idDivM $idGS)
      as $idPM $idGS
  ))
  elabCommand (← `(open $pcId:ident $dcId:ident in
    #derive_lifted_wp (s : $idGS) for
      ($idSet s : StateT $idGS $idDivM PUnit)
      as $idPM PUnit
  ))

/-! ## Step 6: per-machine var accessors and state-tag aliases -/

private def emitVarAccessors (mname : Name) (vars : Array VarInfo) :
    CommandElabM Unit := do
  for v in vars do
    let fldName := (mname.toString ++ "_" ++ v.name.toString)
    let fldId : Ident := mkIdent (Name.mkSimple fldName)
    let getName : Ident := mkIdent (v.name.appendAfter "_get")
    let setName : Ident := mkIdent (v.name.appendAfter "_set")
    let ty := v.ty
    -- Accessors are emitted as `@[reducible] def` so the obligation
    -- generator's `unfold <v>_get; unfold <v>_set` step (or alternatively
    -- a `simp [<v>_get, <v>_set]` step) reaches the underlying `get`/
    -- `set`. The per-pmodule `#derive_lifted_wp` for `get`/`set`
    -- (`emitDerivedWP`) registers `loomSpec` lemmas, so once the
    -- accessor unfolds, `wpgen` walks through state reads/writes
    -- natively (PLAN_P3 R15 / R-P3.1).
    --
    -- IMPORTANT: do NOT ascribe `(get : StateT $idGS $idDivM $idGS)`
    -- explicitly inside the accessor body. The ascription makes the
    -- elaborated `liftM (get : StateT _ _ _) : PM' _` term not match
    -- the discr-tree key that `#derive_lifted_wp` registered (probed
    -- empirically — see `Tests/Surface/WpgenAccessorProbe.lean`).
    -- Letting Lean's `do`-elaborator pick `get` from the
    -- `MonadStateOf` instance produces a `liftM get` whose head form
    -- matches the registered `loomSpec`, so `wpgen` walks the body
    -- without falling back to `WPGen.default`.
    elabCommand (← `(
      @[reducible] def $getName ($idThis : $idMachineRef) : $idPM $ty := do
        let s ← get
        pure (s.machines $idThis).fields.$fldId
    ))
    elabCommand (← `(
      @[reducible] def $setName ($idThis : $idMachineRef) (v : $ty) : $idPM Unit := do
        let s ← get
        let curr := s.machines $idThis
        let newFields : $idFields := { curr.fields with $fldId:ident := v }
        let newMS : ($idSig).MachineState :=
          { stage := curr.stage, currentState := curr.currentState
            fields := newFields, kind := curr.kind }
        set (s.updateMachine $idThis newMS)
    ))

private def emitStateAliases (mname : Name) (states : Array PStateDecl) :
    CommandElabM Unit := do
  for sd in states do
    let cName := (mname.toString ++ "_" ++ sd.name.toString)
    let aliasName : Ident := mkIdent (sd.name.appendAfter "_st")
    let sCtorIdent : Ident := mkIdent (`S ++ Name.mkSimple cName)
    elabCommand (← `(
      def $aliasName : ($idSig).S := $sCtorIdent
    ))

/-! ## Handler emission

For each state body item, emit a Lean def. Handler defs take
`(this : <MName>)` (the wrapper struct, D11/D10) as the explicit first
parameter.

Each handler body is wrapped with leading `let v ← v_get this.ref` for
every machine var, so the body can reference `v` as a Lean local.

The `_handler` suffix avoids shadowing the event name itself when the
handler body references the event (`send target, ePing, ...`). -/

private def handlerName (sname : Name) (kind : Name) (suffix : Bool) : Ident :=
  let base := sname ++ kind
  mkIdent (if suffix then base.appendAfter "_handler" else base)

/-- Build a sequence of `let <v> ← <v>_get this.ref` doSeqItems for every
machine var, so handler bodies can use bare `v` for reads. -/
private def buildVarBindings (vars : Array VarInfo) :
    MacroM (Array (TSyntax `Lean.Parser.Term.doSeqItem)) := do
  let thisI : Ident := mkIdent `this
  vars.mapM fun v => do
    let getName : Ident := mkIdent (v.name.appendAfter "_get")
    let vId : Ident := mkIdent v.name
    `(Lean.Parser.Term.doSeqItem|
        let $vId:ident ← $getName:ident ($thisI |>.ref))

private def materialiseStateBodyItem (mname sname : Name) (vars : Array VarInfo)
    (item : Syntax) : CommandElabM Unit := do
  let mIdent := mkIdent mname
  match item with
  | `(pStateBodyItem| entry { $body:doSeq }) =>
    let defName := handlerName sname `entry (suffix := false)
    let bindings ← liftMacroM <| buildVarBindings vars
    let bodyItem : TSyntax `Lean.Parser.Term.doSeqItem ←
      liftMacroM <| `(Lean.Parser.Term.doSeqItem| do $body)
    elabCommand (← `(
      def $defName ($idThis : $mIdent) : $idPM Unit := do
        $bindings*
        $bodyItem
    ))
  | `(pStateBodyItem| entry ( $param:ident : $ty:term ) { $body:doSeq }) =>
    let defName := handlerName sname `entry (suffix := false)
    let bindings ← liftMacroM <| buildVarBindings vars
    let bodyItem : TSyntax `Lean.Parser.Term.doSeqItem ←
      liftMacroM <| `(Lean.Parser.Term.doSeqItem| do $body)
    elabCommand (← `(
      def $defName ($idThis : $mIdent) ($param : $ty) : $idPM Unit := do
        $bindings*
        $bodyItem
    ))
  | `(pStateBodyItem| on $ev:ident ( $param:ident : $ty:term ) { $body:doSeq }) =>
    let defName := handlerName sname ev.getId (suffix := true)
    let bindings ← liftMacroM <| buildVarBindings vars
    let bodyItem : TSyntax `Lean.Parser.Term.doSeqItem ←
      liftMacroM <| `(Lean.Parser.Term.doSeqItem| do $body)
    elabCommand (← `(
      def $defName ($idThis : $mIdent) ($param : $ty) : $idPM Unit := do
        $bindings*
        $bodyItem
    ))
  | `(pStateBodyItem| on $ev:ident { $body:doSeq }) =>
    let defName := handlerName sname ev.getId (suffix := true)
    let bindings ← liftMacroM <| buildVarBindings vars
    let bodyItem : TSyntax `Lean.Parser.Term.doSeqItem ←
      liftMacroM <| `(Lean.Parser.Term.doSeqItem| do $body)
    elabCommand (← `(
      def $defName ($idThis : $mIdent) : $idPM Unit := do
        $bindings*
        $bodyItem
    ))
  | `(pStateBodyItem| on $_:ident goto $_:ident) =>
    -- Pure transition: no handler def emitted (registry-only).
    pure ()
  | _ => throwErrorAt item "unrecognised state body item (during materialisation)"

private def materialiseMachineBody (mname : Name) (items : Array Syntax)
    (vars : Array VarInfo) : CommandElabM Unit := do
  for it in items do
    match it with
    | `(pMachineBodyItem| var $_:ident : $_:term) =>
      pure ()
    | `(pMachineBodyItem| start state $sid:ident { $sitems:pStateBodyItem* }) =>
      let sname := sid.getId
      for sit in sitems do materialiseStateBodyItem mname sname vars sit
    | `(pMachineBodyItem| state $sid:ident { $sitems:pStateBodyItem* }) =>
      let sname := sid.getId
      for sit in sitems do materialiseStateBodyItem mname sname vars sit
    | _ => throwErrorAt it "unrecognised machine body item (during materialisation)"

/-! ## Step 7b: `<Mod>.InitConditions` aggregation (D21).

Walk the saved `init-holds <prop>` clauses and emit a single
`<Mod>.InitConditions : PProp Sig` predicate that is the conjunction
of all `<prop>`s. The Phase-3 obligation generator threads this into
every per-handler triple's pre- and post-condition. -/

private def emitInitConditions (ctx : LocalPModuleCtx) : CommandElabM Unit := do
  let initsId : Ident := mkIdent `InitConditions
  if ctx.inits.isEmpty then
    elabCommand (← `(
      def $initsId : ($idGS) → Prop := fun _ => True
    ))
    return
  -- Build a chain of `∧` of the clause predicates. Each clause was saved
  -- as a syntax `(init-holds <prop>)`; we extract the `<prop>` term from
  -- index 1 of the saved syntax.
  let mut props : Array (TSyntax `term) := #[]
  for d in ctx.inits do
    match d.defStx with
    | none => continue
    | some stx =>
      -- `init-holds <term>` — child index 1 is the term.
      let propStx : TSyntax `term := ⟨stx[1]⟩
      props := props.push propStx
  if props.isEmpty then
    elabCommand (← `(
      def $initsId : ($idGS) → Prop := fun _ => True
    ))
    return
  -- Fold right: `p1 ∧ p2 ∧ ... ∧ True`. The user's clauses are closed
  -- props (forall ...), so they are `Prop` values — InitConditions
  -- `s` ignores `s` for unconditional clauses but must remain
  -- `GS → Prop`-shaped for the obligation generator. We wrap the
  -- conjunction in `fun _ => ...`.
  let mut body : TSyntax `term ← `(True)
  for p in props.reverse do
    body ← `(($p) ∧ $body)
  elabCommand (← `(
    def $initsId : ($idGS) → Prop := fun _ => $body
  ))

/-! ## Step 7c: per-Lemma/Theorem bundle predicates (D19).

For each registered `Lemma X { invariant a; invariant b; }`, emit
`def X : PProp Sig := fun s => a s ∧ b s`. The free-standing
invariants `a`, `b`, ... are emitted by `materialiseInvariant`
(in step 7); the bundle composes them so `Proof { prove ... using X }`
can pull the whole group as a single hypothesis. -/

private def emitLemmaBundles (ctx : LocalPModuleCtx) : CommandElabM Unit := do
  for lname in ctx.lemmaOrder do
    let some l := ctx.lemmas.find? lname | continue
    let lid : Ident := mkIdent l.name
    if l.invariants.isEmpty then
      elabCommand (← `(
        def $lid : ($idGS) → Prop := fun _ => True
      ))
      continue
    -- Build `fun s => i1 s ∧ i2 s ∧ ... ∧ True`. Each `iN` is itself a
    -- `GS → Prop` (per `materialiseInvariant`) so we APPLY it to `s` —
    -- otherwise the bundle would reference the closed proposition
    -- `iN` and ignore its state argument (the soundness bug fixed
    -- 2026-06-10 final-final).
    let sId : Ident := mkIdent `s
    let mut body : TSyntax `term ← `(True)
    for ivName in l.invariants.reverse do
      let ivIdent : Ident := mkIdent ivName
      body ← `(($ivIdent) $sId ∧ $body)
    elabCommand (← `(
      def $lid : ($idGS) → Prop := fun $sId => $body
    ))

/-! ## Step 7d: `<Mod>.UserInv` (D18).

Conjunction of every registered free-standing invariant in registration
order, plus every Lemma/Theorem bundle name (so `prove X` lemmas join
the global state-invariant lattice when no `Proof` directive
references them). When no invariants exist, emit `True`. -/

private def emitUserInv (ctx : LocalPModuleCtx) : CommandElabM Unit := do
  let userInvId : Ident := mkIdent `UserInv
  let mut conjuncts : Array (TSyntax `term) := #[]
  for invName in ctx.invariantOrder do
    let some _ := ctx.invariants.find? invName | continue
    let ivIdent : Ident := mkIdent invName
    conjuncts := conjuncts.push (⟨ivIdent⟩)
  if conjuncts.isEmpty then
    elabCommand (← `(
      def $userInvId : ($idGS) → Prop := fun _ => True
    ))
    return
  -- Apply each invariant to the bound `s`, mirroring `emitLemmaBundles`.
  let sId : Ident := mkIdent `s
  let mut body : TSyntax `term ← `(True)
  for c in conjuncts.reverse do
    body ← `(($c) $sId ∧ $body)
  elabCommand (← `(
    def $userInvId : ($idGS) → Prop := fun $sId => $body
  ))

/-! ## The `#gen_module` command -/

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
    -- handler defs live under <Mod>.* and so cross-references resolve.
    elabCommand (← `(namespace $name))
    -- Step 1: per-machine wrapper structs (D10).
    emitMachineWrappers ctx
    -- Step 2: types and enums in registration order.
    for tname in ctx.typeOrder do
      let some t := ctx.types.find? tname | continue
      materialiseType t
    -- Step 3: events — emits `<ev>_payload` abbrevs.
    for ename in ctx.eventOrder do
      let some e := ctx.events.find? ename | continue
      materialiseEvent e
    -- Pull out the var info for every machine before emitting `Fields`
    -- and the per-machine accessors.
    let mut machineVars : NameMap (Array VarInfo) := {}
    for mname in ctx.machineOrder do
      let some m := ctx.machines.find? mname | continue
      let vars ← collectVars m.body
      machineVars := machineVars.insert mname vars
    -- Step 4: emit `Sig`/`PM'`/`GS` and the union types.
    emitProgramUnions ctx machineVars
    -- Step 4b: per-event `is_<ev>` tag-check predicates (used by the
    -- `is` notation from `Surface/Notation.lean`).
    emitIsPredicates ctx
    -- Step 4c: machine-kind inductive + `<M>.allocated` predicates
    -- (D20). Lives between the `Sig`/`GS` aliases and the per-machine
    -- accessors so invariants and obligations can reference them.
    emitMachineKinds ctx
    -- Step 5: derive lifted WP for `get`/`set` (D14).
    emitDerivedWP
    -- Step 6: per-machine var accessors + state-tag aliases, plus
    -- handler defs replayed inside each machine namespace.
    for mname in ctx.machineOrder do
      let some m := ctx.machines.find? mname | continue
      let vars := (machineVars.find? mname).getD #[]
      let mid := mkIdent m.name
      elabCommand (← `(namespace $mid))
      elabCommand (← `(open $name:ident))
      emitVarAccessors mname vars
      emitStateAliases mname m.states
      if !m.body.isEmpty then
        materialiseMachineBody mname m.body vars
      elabCommand (← `(end $mid))
    -- Step 7: verification declarations.
    for (_, d) in ctx.invariants.toList do materialiseInvariant d
    for (_, d) in ctx.axioms.toList do     materialiseAxiom d
    for (_, d) in ctx.pures.toList do      materialisePure d
    for (_, d) in ctx.instances.toList do  materialiseInstance d
    -- Step 7b: aggregate `init-holds` clauses into `<Mod>.InitConditions`
    -- (D21). Available to obligation generation as a global precondition
    -- term that flows into every per-handler triple's pre/post.
    emitInitConditions ctx
    -- Step 7c: emit per-Lemma/Theorem bundle predicates (D19): for each
    -- registered Lemma/Theorem `X` whose body lists invariants
    -- `[a, b, c]`, emit `def X : PProp Sig := fun s => a s ∧ b s ∧ c s`
    -- so `Proof { prove X using Y }` can later refer to the named lemma.
    emitLemmaBundles ctx
    -- Step 7d: aggregate the union of every registered free-standing
    -- invariant into `<Mod>.UserInv` (PLAN_P3 D18). Empty -> True.
    emitUserInv ctx
    elabCommand (← `(end $name))
    -- Step 8: mark types/events/invariants/etc. as materialised by
    -- clearing their `defStx` (so re-elaboration in another file's
    -- import won't fire them again). We KEEP the machine body Syntax
    -- so the Phase-3 obligation generator (`#pverify`) can extract
    -- `var` declarations to build accessor-unfold lists; the
    -- `materialised` flag is set so `#pwf` knows the machine has been
    -- emitted.
    let machines' := ctx.machines.foldl (init := ({} : NameMap PMachineDecl))
      fun acc n d => acc.insert n { d with materialised := true }
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
