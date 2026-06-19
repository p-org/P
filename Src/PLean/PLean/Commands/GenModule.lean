/-
PLean.Commands.GenModule — `#gen_module M` finalisation command.

Walks the registry and emits, in dependency order: machine wrapper
structs; types and enums; per-event payload abbrevs; the per-pmodule
unions (`<Mod>.E` / `G` / `S` / `Fields`) and aliases (`Sig` / `PM'` /
`GS`); `#derive_lifted_wp` for `get`/`set`; per-machine var accessors,
state-tag aliases, and handler defs; finally invariants / axioms /
init-conditions / function / pinstance materialisation.

Macro hygiene: identifiers that must resolve against user-namespace
constants (`<Mod>.E`, `<Mod>.Sig`, `this`, …) are built via `mkIdent`
so they remain unhygienic. Bare names inside `\`(...)` would acquire
hygiene marks and fail to resolve.
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
import PLean.Verify.SimpAttrs

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
  -- WHY flat top-level rather than `namespace <M>`: the wrapper struct
  -- `<Mod>.<M>` claims the namespace slot, making nested defs there
  -- unreliable. WHY `kind ≠ 0`: `0` is the default-initialised value;
  -- without the guard, an unassigned `MachineRef` would satisfy the
  -- first declared machine's `_allocated` predicate.
  --
  -- WHY the `currentState ∈ <M>'s states` conjunct: `MachineState`
  -- flattens `kind : Nat` and `currentState : S` (a union of *every*
  -- machine's states) as independent fields. Without coupling them, a
  -- model can set `kind = <M>_kind` while `currentState` is some *other*
  -- machine's state — an impossible machine PVerifier's typed
  -- per-machine state arrays exclude structurally. Tying state
  -- membership into `<M>_allocated` rules those spurious models out.
  -- This only ever weakens a guard antecedent (`is_<M> m s → …`), so it
  -- cannot make a real obligation harder, and it is preserved because
  -- `goto` only moves a machine within its own states.
  for mn in allKinds do
    let kindNameId := mkIdent (mn.appendAfter "_kind")
    let allocName : Ident := mkIdent (mn.appendAfter "_allocated")
    let stateCtors : Array Ident := Id.run do
      let mut out : Array Ident := #[]
      if let some m := ctx.machines.find? mn then
        for sd in m.states do
          out := out.push (mkIdent (`S ++ Name.mkSimple (mn.toString ++ "_" ++ sd.name.toString)))
      out
    if stateCtors.isEmpty then
      elabCommand (← `(
        @[inline] def $allocName (m : $idMachineRef) (s : $idGS) : Prop :=
          (s.machines m).kind ≠ 0 ∧ (s.machines m).kind = $kindNameId
      ))
    else
      let mut stateMem : TSyntax `term ← `((s.machines m).currentState = $(stateCtors[0]!))
      for i in [1:stateCtors.size] do
        stateMem ← `($stateMem ∨ (s.machines m).currentState = $(stateCtors[i]!))
      elabCommand (← `(
        @[inline] def $allocName (m : $idMachineRef) (s : $idGS) : Prop :=
          (s.machines m).kind ≠ 0 ∧ (s.machines m).kind = $kindNameId ∧ ($stateMem)
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

/-! ## Step 4c: per-event payload extractor `<ev>_payload_of`

Inside invariant bodies, `e : <ev>` (after the `system <s>` materialiser
retypes the binder to `Sig.Label`) projects the payload via
`<ev>_payload_of e`. The body uses an `Inhabited`-default fallback for
the `lbl.action ≠ .event (E.<ev> _)` case — unreachable when the
auto-injected `is_<ev> e` guard holds. The def is left **opaque** to
SMT — lean-auto treats it as an uninterpreted function, which is the
right SMT-level interpretation: `<ev>_payload_of e` becomes an
applied uninterpreted symbol whose return value the user invariant
constrains transitively via the `is_<ev>` predicate (kept in the
goal as another uninterpreted predicate). -/

private def emitEventPayloadAccessors (ctx : LocalPModuleCtx)
    (eventPayloadFields : NameMap NameSet) : CommandElabM Unit := do
  for ename in ctx.eventOrder do
    let some e := ctx.events.find? ename | continue
    let some _ := eventPayloadFields.find? ename | continue
    let some payloadName := e.payload | continue
    let extractor : Ident :=
      mkIdent (Name.mkSimple (e.name.toString ++ "_payload_of"))
    let payloadId : Ident := mkIdent payloadName
    let evCtor : Ident := mkIdent (`E ++ e.name)
    elabCommand (← `(
      def $extractor (lbl : ($idSig).Label) : $payloadId :=
        match lbl.action with
        | .event ($evCtor p) => p
        | _ => default
    ))

/-! ## Step 4d″: per-event payload characterisation `<ev>_payload_of_spec`

`<ev>_payload_of` (step 4d) is meant to be opaque to SMT — lean-auto
treats it as an uninterpreted function. That is right for an in-flight
label `e` whose payload the invariant constrains transitively. But two
things go wrong for a *send*-handler whose post-state routing invariant
quantifies `∀ e, is_<ev> e → … (<ev>_payload_of e) …`:

  1. As a plain `def`, lean-auto tries to look inside `<ev>_payload_of`'s
     `match` body when it appears applied to a *bound* `e`, and aborts
     with `lamTerm2STermAux :: Unexpected head term … lam`. Marking the
     extractor `@[irreducible]` (below) stops that — it then translates
     as a true uninterpreted symbol even under a `∀` binder.

  2. With the extractor uninterpreted, the solver has no way to compute
     its value on the freshly-sent label `Label.mk t (.event (E.<ev> p))
     c`, so it picks a spurious model and reports a counter-example. The
     emitted `<ev>_payload_of_spec` fact — `∀ lbl p, lbl.action = .event
     (E.<ev> p) → <ev>_payload_of lbl = p` — supplies exactly the
     defining equation the solver needs; `emitOneObligation` brings it
     into context (`have`) so `loom_smt [*]` instantiates it at the new
     label.

This generalises the manual `eAccept_payload_of_mk` lemma the
DistributedLock port hand-wrote: the generator now emits the
characterisation for every event with a payload, and the obligation
generator feeds it to SMT automatically. -/

private def emitPayloadCharacterizations (ctx : LocalPModuleCtx)
    (eventPayloadFields : NameMap NameSet) : CommandElabM Unit := do
  for ename in ctx.eventOrder do
    let some e := ctx.events.find? ename | continue
    let some _ := eventPayloadFields.find? ename | continue
    let some payloadName := e.payload | continue
    let extractor : Ident :=
      mkIdent (Name.mkSimple (e.name.toString ++ "_payload_of"))
    let mkThm : Ident :=
      mkIdent (Name.mkSimple (e.name.toString ++ "_payload_of_mk"))
    let specThm : Ident :=
      mkIdent (Name.mkSimple (e.name.toString ++ "_payload_of_spec"))
    let payloadId : Ident := mkIdent payloadName
    let evCtor   : Ident := mkIdent (`E ++ e.name)
    let labelMk  : Ident := mkIdent ``PLean.Label.mk
    -- Defining equation on a literal `Label.mk` (proved before the
    -- extractor is sealed). Kept as a public lemma for manual proofs.
    elabCommand (← `(
      theorem $mkThm (t : PLean.MachineRef) (p : $payloadId) (c : Nat) :
          $extractor ($labelMk t (.event ($evCtor p)) c) = p := by
        unfold $extractor; rfl
    ))
    -- Universal characterisation from any label whose action is known.
    -- Proved via `_mk` after destructuring so it does NOT depend on the
    -- extractor staying reducible.
    elabCommand (← `(
      theorem $specThm (lbl : ($idSig).Label) (p : $payloadId)
          (h : lbl.action = .event ($evCtor p)) : $extractor lbl = p := by
        obtain ⟨t, a, c⟩ := lbl
        simp only at h
        subst h
        exact $mkThm t p c
    ))
    -- Seal the extractor so lean-auto always translates it as an
    -- uninterpreted function (point 1 above). `_spec` supplies its value
    -- to SMT where needed.
    elabCommand (← `(attribute [irreducible] $extractor))

/-! ## Step 4d′: per-event characterisation lemmas `is_<ev>_iff`

Bridge the opaque `is_<ev>` tag predicate to a concrete `Label.action`
constructor equality. `is_<ev>` is emitted (step 4b) as a `match`-on-
`action` def left folded so lean-auto treats it as an uninterpreted
predicate (its `match` body trips the monomorphizer if unfolded). With
no axiom relating `is_<ev> e` to `e.action`, any obligation whose
invariant quantifies `∀ e, e is <ev> → …` reaches SMT with `is_<ev>` as
a free predicate — the solver can neither discharge the guard nor
exclude a spurious label of that tag, so the goal comes back `unknown`.

The `@[pverifySimp]` tag means `pverify_smt_prep`'s `simp only
[pverifySimp]` rewrites every `is_<ev> e` into the action equality
before `loom_smt` runs, so SMT sees a concrete constructor equality.
Pairwise disjointness between events is then *derived* by the solver
from `EventOrGoto` / `E` constructor injectivity — no separate
disjointness lemma is needed. -/

private def emitIsCharacterizations (ctx : LocalPModuleCtx) : CommandElabM Unit := do
  for ename in ctx.eventOrder do
    let some e := ctx.events.find? ename | continue
    let thmName : Ident :=
      mkIdent (Name.mkSimple ("is_" ++ e.name.toString ++ "_iff"))
    let predName : Ident := mkIdent (Name.mkSimple ("is_" ++ e.name.toString))
    let evCtor   : Ident := mkIdent (`E ++ e.name)
    let evOrGoto : Ident := mkIdent ``PLean.EventOrGoto.event
    -- `cases ev <;> simp_all` splits on however many `E` constructors
    -- exist (do NOT hard-code the arity), then closes each arm. The
    -- `goto` action arm closes by `simp_all` directly.
    match e.payload with
    | none =>
      elabCommand (← `(
        @[pverifySimp] theorem $thmName (lbl : ($idSig).Label) :
            $predName lbl ↔ lbl.action = $evOrGoto $evCtor := by
          unfold $predName
          rcases h : lbl.action with ev | g
          · cases ev <;> simp_all
          · simp_all
      ))
    | some _ =>
      elabCommand (← `(
        @[pverifySimp] theorem $thmName (lbl : ($idSig).Label) :
            $predName lbl ↔ ∃ p, lbl.action = $evOrGoto ($evCtor p) := by
          unfold $predName
          rcases h : lbl.action with ev | g
          · cases ev <;> simp_all
          · simp_all
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
    -- Accessors are `@[reducible] def` so the obligation generator's
    -- `unfold <v>_get/<v>_set` step reaches the underlying `get`/`set`
    -- whose `loomSpec` is registered by the per-pmodule
    -- `#derive_lifted_wp` (`emitDerivedWP`).
    --
    -- WHY no type ascription on `← get`: the ascribed
    -- `(get : StateT _ _ _)` form does not match the registered
    -- discr-tree key, so `wpgen` falls back to `WPGen.default`. Letting
    -- Lean pick `get` from the `MonadStateOf` instance produces the
    -- key shape the spec expects.
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
    -- `@[reducible]` so SMT prep's `dsimp only` reduces a goal's
    -- `currentState = <S>_st` to the raw `S.<M>_<S>` constructor —
    -- matching the form the state/kind coupling in `<M>_allocated`
    -- produces, so the solver can relate them.
    elabCommand (← `(
      @[reducible] def $aliasName : ($idSig).S := $sCtorIdent
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

/-! ## Step 7b–d: state-indexed conjunction predicates.

`emitConjPredicate name members applyState` emits

  def <name> : GS → Prop := fun <binder> => m1 ∧ m2 ∧ ... ∧ True

When `applyState = true`, each `mN` is applied to the bound state (so
the binder is an unhygienic `s` that the `mN`s reference); otherwise
the binder is `_` and each `mN` is used verbatim (the user's clauses
are closed propositions, e.g. `init-holds (∀ x, P x)`). Empty `members`
collapses to `fun _ => True`.

WHY a single helper: three earlier callers (`emitInitConditions`,
`emitLemmaBundles`, `emitUserInv`) hand-rolled the same fold. The
2026-06-10 soundness bug was exactly the bundle predicate failing to
apply `s` — having the apply-or-not decision in one place removes the
foot-gun. -/

private def emitConjPredicate (name : Ident) (members : Array (TSyntax `term))
    (applyState : Bool) : CommandElabM Unit := do
  if members.isEmpty then
    elabCommand (← `(
      def $name : ($idGS) → Prop := fun _ => True
    ))
    return
  let sId : Ident := mkIdent `s
  let mut body : TSyntax `term ← `(True)
  for m in members.reverse do
    if applyState then
      body ← `(($m) $sId ∧ $body)
    else
      body ← `(($m) ∧ $body)
  if applyState then
    elabCommand (← `(def $name : ($idGS) → Prop := fun $sId => $body))
  else
    elabCommand (← `(def $name : ($idGS) → Prop := fun _ => $body))

/-- Aggregate every saved `init-holds <prop>` clause **plus the
framework init constraints** into the single predicate
`<Mod>.InitConditions : GS → Prop`. Matches PVerifier's `init {}` block
(`Uclid5CodeGenerator.cs::GenerateInitBlock`):
- buffers `sent` / `received` start empty,
- `actionCount` starts at 0.

(`InStart` / `InEntry` for every `MachineRef` is a PVerifier convention
that PLean does not yet model; this can be added when initialization-
action support lands.) -/
private def emitInitConditions (machineKinds eventKinds : NameSet)
    (machineFields : NameMap NameSet)
    (eventPayloadFields : NameMap NameSet)
    (ctx : LocalPModuleCtx) : CommandElabM Unit := do
  -- Framework init: emit as a state-dependent conjunct via `(applyState
  -- := true)`. We need `s` to appear in the body, so we build the
  -- predicate as a closed `fun s => <framework> ∧ <user clauses>` here
  -- rather than re-using `emitConjPredicate` (which currently can't mix
  -- per-state and closed conjuncts in one call).
  let sId : Ident := mkIdent `s
  let frameworkClauses : Array (TSyntax `term) :=
    #[← `((∀ l : ($idSig).Label, ($sId).sent l = false)),
      ← `((∀ l : ($idSig).Label, ($sId).received l = false)),
      ← `(($sId).actionCount = 0)]
  let mut props : Array (TSyntax `term) := frameworkClauses
  for d in ctx.inits do
    if let some stx := d.defStx then
      -- `init-holds` bodies are materialised under a hardcoded `s` binder
      -- (below), so a `GlobalState`-typed binder in the body would shadow
      -- it and decouple the clause from the actual init state — the same
      -- soundness hole `rejectExplicitStateBinder` guards for invariants.
      PLean.rejectStateShadowIn "`init-holds`" stx[1]
      let rewritten ← liftMacroM <|
        PLean.rewriteFieldProjections machineKinds eventKinds
          machineFields eventPayloadFields `s stx[1]
      let raw ← liftMacroM <|
        PLean.injectKindGuards machineKinds eventKinds `s rewritten
      props := props.push ⟨raw⟩
  -- Build the conjunction by hand (mirrors `emitConjPredicate`'s shape
  -- but doesn't apply `s` to the conjuncts — they're plain props).
  let mut body : TSyntax `term ← `(True)
  for p in props.reverse do
    body ← `(($p) ∧ $body)
  elabCommand (← `(
    def $(mkIdent `InitConditions) : ($idGS) → Prop := fun $sId => $body
  ))

/-- Per-`Lemma X { invariant a; invariant b; }` (and `Theorem`) bundle
predicate `<Mod>.X : GS → Prop := fun s => a s ∧ b s`. Each individual
invariant is itself emitted as `GS → Prop` by `materialiseInvariant`,
so the bundle applies `s` to each conjunct. -/
private def emitLemmaBundles (ctx : LocalPModuleCtx) : CommandElabM Unit := do
  for lname in ctx.lemmaOrder do
    let some l := ctx.lemmas.find? lname | continue
    let conjuncts : Array (TSyntax `term) := l.invariants.map fun n => ⟨mkIdent n⟩
    emitConjPredicate (mkIdent l.name) conjuncts (applyState := true)

/-- `<Mod>.UserInv` — conjunction of every free-standing invariant in
registration order. -/
private def emitUserInv (ctx : LocalPModuleCtx) : CommandElabM Unit := do
  let mut conjuncts : Array (TSyntax `term) := #[]
  for invName in ctx.invariantOrder do
    if ctx.invariants.contains invName then
      conjuncts := conjuncts.push ⟨mkIdent invName⟩
  emitConjPredicate (mkIdent `UserInv) conjuncts (applyState := true)

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
    -- Build the field-projection maps used by the invariant rewriter.
    -- machineFields[M] is the set of `var` names for machine M;
    -- eventPayloadFields[ev] is the set of named-tuple field names for
    -- event ev (empty if its payload isn't a named tuple).
    let machineFields : NameMap NameSet := Id.run do
      let mut out : NameMap NameSet := {}
      for mname in ctx.machineOrder do
        let some vars := machineVars.find? mname | continue
        let s := vars.foldl (init := ({} : NameSet))
          fun s v => s.insert v.name
        out := out.insert mname s
      out
    let eventPayloadFields : NameMap NameSet := Id.run do
      let mut out : NameMap NameSet := {}
      for ename in ctx.eventOrder do
        let some e := ctx.events.find? ename | continue
        let some payloadName := e.payload | continue
        let some pType := ctx.types.find? payloadName | continue
        let some payStx := pType.defStx | continue
        match payStx with
        | `(type $_:ident = ($[$flds:pNamedField],*)) =>
          let mut s : NameSet := {}
          for f in flds do
            let fid := f.raw[0]
            if fid.isIdent then s := s.insert fid.getId
          out := out.insert ename s
        | _ => pure ()
      out
    -- Step 4d: per-event payload extractor `<ev>_payload_of`. Lives
    -- after the union types so it can pattern-match on `E.<ev>`.
    emitEventPayloadAccessors ctx eventPayloadFields
    -- Step 4d″: per-event `<ev>_payload_of_mk` characterisation lemma —
    -- the defining equation of the extractor on a freshly-constructed
    -- event label, used by send-handler manual proofs. Lives after
    -- `emitEventPayloadAccessors` (so `<ev>_payload_of` exists).
    emitPayloadCharacterizations ctx eventPayloadFields
    -- Step 4d′: per-event `is_<ev>_iff` characterisation lemmas. Lives
    -- after `emitIsPredicates` (so `is_<ev>` exists to unfold) and the
    -- union types (so `E.<ev>` resolves).
    emitIsCharacterizations ctx
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
    -- Machine and event kind sets drive auto-injection of kind guards
    -- on `∀ x : <M>, …` / `∀ e : <ev>, …` quantifiers inside
    -- `system <s> { … }` blocks (see
    -- `Surface/Verify.lean::injectKindGuards`). Spec machines are
    -- included for completeness — `is_<M>` is emitted for them too.
    let machineKinds : NameSet :=
      ctx.machineOrder.foldl (init := {}) fun s n => s.insert n
    let eventKinds : NameSet :=
      ctx.eventOrder.foldl (init := {}) fun s n => s.insert n
    -- `function` / `paxiom` / `pinstance` come BEFORE invariants:
    -- invariant and `init-holds` bodies may reference a `function`
    -- (e.g. `n.server = lock_server`), but functions only reference
    -- types / machine fields (already materialised in step 6) and never
    -- reference invariants. Emitting invariants first left `lock_server`
    -- undefined inside a `Lemma`/`Theorem` block.
    for (_, d) in ctx.pures.toList do      materialisePure d
    for (_, d) in ctx.axioms.toList do     materialiseAxiom d
    for (_, d) in ctx.instances.toList do  materialiseInstance d
    for (_, d) in ctx.invariants.toList do
      materialiseInvariant machineKinds eventKinds machineFields
        eventPayloadFields d
    -- Step 7b: aggregate `init-holds` clauses into `<Mod>.InitConditions`
    -- (D21). Available to obligation generation as a global precondition
    -- term that flows into every per-handler triple's pre/post.
    emitInitConditions machineKinds eventKinds machineFields
      eventPayloadFields ctx
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
