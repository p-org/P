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
import PLean.Syntax.Types
import PLean.Syntax.Events
import PLean.Syntax.Machine
import PLean.Syntax.Verify
import PLean.Syntax.Stmt
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

/-! ## Step 1: per-machine wrapper structs -/

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

/-! ## Step 1b: machine-kind machinery

For each pmodule, emit:
  inductive <Mod>.MKind | <M1> | <M2> | ... deriving DecidableEq, Inhabited
  def <Mod>.<M>_kind : Nat := <i>     -- index ≥ 1 (0 reserved for "unset")

Plus a per-machine `<M>.allocated (m : MachineRef) (s : GS) : Prop`
predicate that asserts a machine ref's `kind` matches its expected
ctor. The `is` macro extends to dispatch on machine names by
expanding `m is <M>` to `<M>.allocated m`.

Spec machines participate in the kind tagging too so their kind is
recognisable in invariants. -/

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
  -- `<Mod>.G`: goto payload union. The surface doesn't expose goto
  -- payloads; one trivial constructor suffices. The constructor MUST be
  -- emitted unhygienically (via `mkIdent`): the `goto` doElem macro in
  -- `Syntax/Stmt.lean` references it as the clean name `G.unit`, so a
  -- hygienic `| unit` ctor (which Lean would name `G.unit._@…._hyg.N`)
  -- would make `goto <state>` fail to resolve `G.unit` inside a handler.
  let gUnitCtor ← `(Lean.Parser.Command.ctor| | $(mkIdent `unit):ident)
  elabCommand (← `(
    inductive $idG where
      $gUnitCtor:ctor
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

The `is` notation in `Syntax/Notation.lean` rewrites `lbl is <ev>` to
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

`<ev>_payload_of` is sealed `@[irreducible]` so SMT treats it as an
uninterpreted symbol — that's what lets a routing invariant
`∀ e, is_<ev> e → … (<ev>_payload_of e) …` translate cleanly. The
defining equation
  `∀ lbl p, lbl.action = .event (E.<ev> p) → <ev>_payload_of lbl = p`
is emitted as a separate theorem `<ev>_payload_of_spec`; the obligation
generator brings it into the local context as a `have`, so SMT can
compute the extractor's value on a freshly-sent label. -/

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
    -- `@[reducible] def` (equivalently, `abbrev`) so SMT prep's
    -- `dsimp only` reduces a goal's `currentState = <S>_st` to the raw
    -- `S.<M>_<S>` constructor. Without this the solver leaves `<S>_st`
    -- uninterpreted and returns `unknown` on base cases that need to
    -- distinguish e.g. `Won_st ≠ Proposing_st`. Also matches the
    -- coupling form the state/kind check in `<M>_allocated` produces.
    elabCommand (← `(
      @[reducible] def $aliasName : ($idSig).S := $sCtorIdent
    ))

/-! ## Handler emission

For each state body item, emit a Lean def. Handler defs take
`(this : <MName>)` (the wrapper struct) as the explicit first
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
    (eventPayloadTy : Name → Option Name)
    (item : Syntax) : CommandElabM Unit := do
  let mIdent := mkIdent mname
  match item with
  | `(pStateBodyItem| entry { $body:doSeq }) =>
    let defName := handlerName sname `entry (suffix := false)
    let bindings ← liftMacroM <| buildVarBindings vars
    let bodyItem : TSyntax `Lean.Parser.Term.doSeqItem ←
      liftMacroM <| `(Lean.Parser.Term.doSeqItem| do $body)
    -- `noncomputable` so a handler that references an axiomatised
    -- `pinstance` projection (e.g. `LeOrder.le this.ref n.voteFor`)
    -- doesn't fail Lean's code-generator check. Handler bodies are
    -- never compiled — they're only inspected by WP / SMT — so making
    -- them noncomputable has no downstream effect.
    elabCommand (← `(
      noncomputable def $defName ($idThis : $mIdent) : $idPM Unit := do
        $bindings*
        $bodyItem
    ))
  | `(pStateBodyItem| entry ( $param:ident : $ty:term ) { $body:doSeq }) =>
    let defName := handlerName sname `entry (suffix := false)
    let bindings ← liftMacroM <| buildVarBindings vars
    let bodyItem : TSyntax `Lean.Parser.Term.doSeqItem ←
      liftMacroM <| `(Lean.Parser.Term.doSeqItem| do $body)
    -- `noncomputable` so a handler that references an axiomatised
    -- `pinstance` projection (e.g. `LeOrder.le this.ref n.voteFor`)
    -- doesn't fail Lean's code-generator check. Handler bodies are
    -- never compiled — they're only inspected by WP / SMT — so making
    -- them noncomputable has no downstream effect.
    elabCommand (← `(
      noncomputable def $defName ($idThis : $mIdent) ($param : $ty) : $idPM Unit := do
        $bindings*
        $bodyItem
    ))
  | `(pStateBodyItem| on $ev:ident ( $param:ident : $ty:term ) { $body:doSeq }) =>
    let defName := handlerName sname ev.getId (suffix := true)
    let bindings ← liftMacroM <| buildVarBindings vars
    let bodyItem : TSyntax `Lean.Parser.Term.doSeqItem ←
      liftMacroM <| `(Lean.Parser.Term.doSeqItem| do $body)
    -- `noncomputable` so a handler that references an axiomatised
    -- `pinstance` projection (e.g. `LeOrder.le this.ref n.voteFor`)
    -- doesn't fail Lean's code-generator check. Handler bodies are
    -- never compiled — they're only inspected by WP / SMT — so making
    -- them noncomputable has no downstream effect.
    elabCommand (← `(
      noncomputable def $defName ($idThis : $mIdent) ($param : $ty) : $idPM Unit := do
        $bindings*
        $bodyItem
    ))
  | `(pStateBodyItem| on $ev:ident { $body:doSeq }) =>
    let defName := handlerName sname ev.getId (suffix := true)
    let bindings ← liftMacroM <| buildVarBindings vars
    let bodyItem : TSyntax `Lean.Parser.Term.doSeqItem ←
      liftMacroM <| `(Lean.Parser.Term.doSeqItem| do $body)
    -- `noncomputable` so a handler that references an axiomatised
    -- `pinstance` projection (e.g. `LeOrder.le this.ref n.voteFor`)
    -- doesn't fail Lean's code-generator check. Handler bodies are
    -- never compiled — they're only inspected by WP / SMT — so making
    -- them noncomputable has no downstream effect.
    elabCommand (← `(
      noncomputable def $defName ($idThis : $mIdent) : $idPM Unit := do
        $bindings*
        $bodyItem
    ))
  | `(pStateBodyItem| on $ev:ident goto $tgt:ident) =>
    -- Emit a synthetic `_handler` whose body is just the `goto`. The
    -- per-handler obligation needs a Lean def to apply `wpgen` to; without
    -- one, the (state, event) pair was silently skipped and the goto's
    -- `currentState` / `sent` / `actionCount` mutations went unverified.
    -- The binder list matches the obligation generator's call shape:
    -- payload events get a `param : <ev>_payload`, payload-less ones don't.
    let defName := handlerName sname ev.getId (suffix := true)
    let bindings ← liftMacroM <| buildVarBindings vars
    let tgtAlias : Ident := mkIdent (tgt.getId.appendAfter "_st")
    let gUnit : Ident := mkIdent (`G ++ `unit)
    let bodyItem : TSyntax `Lean.Parser.Term.doSeqItem ←
      liftMacroM <| `(Lean.Parser.Term.doSeqItem|
        do PLean.goto ($idThis |>.ref) $tgtAlias $gUnit)
    match eventPayloadTy ev.getId with
    | some payloadTy =>
      let payloadId : Ident := mkIdent payloadTy
      elabCommand (← `(
        noncomputable def $defName ($idThis : $mIdent) (_param : $payloadId) :
            $idPM Unit := do
          $bindings*
          $bodyItem
      ))
    | none =>
      elabCommand (← `(
        noncomputable def $defName ($idThis : $mIdent) : $idPM Unit := do
          $bindings*
          $bodyItem
      ))
  | _ => throwErrorAt item "unrecognised state body item (during materialisation)"

private def materialiseMachineBody (mname : Name) (items : Array Syntax)
    (vars : Array VarInfo)
    (eventPayloadTy : Name → Option Name) : CommandElabM Unit := do
  for it in items do
    match it with
    | `(pMachineBodyItem| var $_:ident : $_:term) =>
      pure ()
    | `(pMachineBodyItem| start state $sid:ident { $sitems:pStateBodyItem* }) =>
      let sname := sid.getId
      for sit in sitems do
        materialiseStateBodyItem mname sname vars eventPayloadTy sit
    | `(pMachineBodyItem| state $sid:ident { $sitems:pStateBodyItem* }) =>
      let sname := sid.getId
      for sit in sitems do
        materialiseStateBodyItem mname sname vars eventPayloadTy sit
    | _ => throwErrorAt it "unrecognised machine body item (during materialisation)"

/-! ## Step 7b–d: state-indexed conjunction predicates.

`emitConjPredicate name members` emits

  def <name> : GS → Prop := fun s => c₁ ∧ c₂ ∧ ... ∧ cₙ

The binder is always the unhygienic `s` (the soundness fix from
2026-06-10: a bundle that elided the binder while the body still named
`s` via macro capture silently decoupled the predicate from per-handler
state). Each member is a `(term, applyState)` pair: when `applyState` is
true the conjunct is rendered as `(m) s`; when false the conjunct is
`m` verbatim — the user's clause is already a closed `Prop` that may
reference `s` directly (e.g. a framework init clause like
`s.actionCount = 0`, or an `init-holds (∀ x, P x)` body).

The per-member flag lets `emitInitConditions` mix `s`-dependent
framework clauses with closed user-supplied props in a single call.
Empty `members` collapses to `fun _ => True`. -/

private def emitConjPredicate (name : Ident)
    (members : Array (TSyntax `term × Bool)) : CommandElabM Unit := do
  if members.isEmpty then
    elabCommand (← `(
      def $name : ($idGS) → Prop := fun _ => True
    ))
    return
  let sId : Ident := mkIdent `s
  let renderOne (m : TSyntax `term × Bool) : CommandElabM (TSyntax `term) :=
    liftMacroM <|
      if m.2 then `(($(m.1)) $sId) else `($(m.1))
  let last := members[members.size - 1]!
  let init : TSyntax `term ← renderOne last
  let mut body : TSyntax `term := init
  for m in members.pop.reverse do
    let head ← renderOne m
    body ← liftMacroM <| `($head ∧ $body)
  elabCommand (← `(def $name : ($idGS) → Prop := fun $sId => $body))

/-- Convenience: every member uses the same `applyState` flag. -/
private def emitConjPredicateUniform (name : Ident)
    (members : Array (TSyntax `term)) (applyState : Bool) :
    CommandElabM Unit :=
  emitConjPredicate name (members.map (·, applyState))

/-- Aggregate every saved `init-holds <prop>` clause **plus the
framework init constraints** into the single predicate
`<Mod>.InitConditions : GS → Prop`:
- buffers `sent` / `received` start empty,
- `actionCount` starts at 0,
- every `MachineRef` starts in some machine's `start state`.

The conjuncts are all closed `Prop`s that name `s` directly via macro
capture (via the unhygienic `s` binder the helper introduces), so they
all pass `applyState := false` through `emitConjPredicate`. -/
private def emitInitConditions (machineKinds eventKinds : NameSet)
    (machineFields : NameMap NameSet)
    (eventPayloadFields : NameMap NameSet)
    (ctx : LocalPModuleCtx) : CommandElabM Unit := do
  let sId : Ident := mkIdent `s
  let mut props : Array (TSyntax `term) :=
    #[← `((∀ l : ($idSig).Label, ($sId).sent l = false)),
      ← `((∀ l : ($idSig).Label, ($sId).received l = false)),
      ← `(($sId).actionCount = 0)]
  -- InStart: every machine ref starts in a start state. Allocated
  -- machines are in their kind's declared `start state`; unallocated
  -- refs hold the default `MachineState`, whose `currentState` is the
  -- first `S` constructor — so include that ctor too. This lets
  -- base-case VCs for state-dependent invariants (e.g. `∀ x, stateOf x
  -- s = Won → …`) discharge: no machine is in a non-start state
  -- initially. Sound because P machines begin in their start state.
  let mut startCtors : Array Name := #[]
  if let some m0 := ctx.machineOrder[0]? then
    if let some md0 := ctx.machines.find? m0 then
      if let some sd0 := md0.states[0]? then
        startCtors := startCtors.push
          (Name.mkSimple (m0.toString ++ "_" ++ sd0.name.toString))
  for mname in ctx.machineOrder do
    if let some md := ctx.machines.find? mname then
      for sd in md.states do
        if sd.isStart then
          let c := Name.mkSimple (mname.toString ++ "_" ++ sd.name.toString)
          unless startCtors.contains c do startCtors := startCtors.push c
  if let some c0 := startCtors[0]? then
    let mId : Ident := mkIdent `m
    let csRead : TSyntax `term ← `((($sId).machines $mId).currentState)
    let mut disj : TSyntax `term ← `($csRead = $(mkIdent (`S ++ c0)))
    for i in [1:startCtors.size] do
      let ci := startCtors[i]!
      disj ← `($disj ∨ $csRead = $(mkIdent (`S ++ ci)))
    props := props.push (← `(∀ $mId : PLean.MachineRef, $disj))
  for d in ctx.inits do
    if let some stx := d.defStx then
      -- An `init-holds` body that names `GlobalState` as a binder type
      -- would shadow the outer `s` and silently decouple the clause
      -- from the actual init state — the same soundness hole
      -- `rejectExplicitStateBinder` guards for invariants.
      PLean.rejectStateShadowIn "`init-holds`" stx[1]
      let rewritten ← liftMacroM <|
        PLean.rewriteFieldProjections machineKinds eventKinds
          machineFields eventPayloadFields `s stx[1]
      let raw ← liftMacroM <|
        PLean.injectKindGuards machineKinds eventKinds `s rewritten
      props := props.push ⟨raw⟩
  emitConjPredicateUniform (mkIdent `InitConditions)
    props (applyState := false)

/-- Per-`Lemma X { invariant a; invariant b; }` (and `Theorem`) bundle
predicate `<Mod>.X : GS → Prop := fun s => a s ∧ b s`. Each individual
invariant is itself emitted as `GS → Prop` by `materialiseInvariant`,
so the bundle applies `s` to each conjunct. -/
private def emitLemmaBundles (ctx : LocalPModuleCtx) : CommandElabM Unit := do
  for lname in ctx.lemmaOrder do
    let some l := ctx.lemmas.find? lname | continue
    let conjuncts : Array (TSyntax `term) := l.invariants.map fun n => ⟨mkIdent n⟩
    emitConjPredicateUniform (mkIdent l.name) conjuncts (applyState := true)

/-- `<Mod>.UserInv` — conjunction of every free-standing invariant in
registration order. -/
private def emitUserInv (ctx : LocalPModuleCtx) : CommandElabM Unit := do
  let mut conjuncts : Array (TSyntax `term) := #[]
  for invName in ctx.invariantOrder do
    if ctx.invariants.contains invName then
      conjuncts := conjuncts.push ⟨mkIdent invName⟩
  emitConjPredicateUniform (mkIdent `UserInv) conjuncts (applyState := true)

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
    -- Step 1: per-machine wrapper structs.
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
    -- `is` notation from `Syntax/Notation.lean`).
    emitIsPredicates ctx
    -- Step 4c: machine-kind inductive + `<M>.allocated` predicates.
    -- Lives between the `Sig`/`GS` aliases and the per-machine
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
    -- Step 5: derive lifted WP for `get`/`set`.
    emitDerivedWP
    -- Step 5b: materialise `pure` functions (foreign + defined),
    -- `paxiom`s, and `pinstance`s *before* the per-machine handler
    -- bodies (Step 6) and the verification declarations (Step 7).
    -- Handler bodies and invariants may reference any of: a `function`
    -- (`if le this.ref x then …`), an axiom (`have := f_total x`), or
    -- a typeclass projection (`LeOrder.le this.ref x`, found via the
    -- anonymous instance `pinstance` emits). The pinstance bundle has
    -- to land *before* Step 6 specifically so typeclass resolution for
    -- `LeOrder.le` inside a handler body succeeds; otherwise the
    -- elaborator reports `failed to synthesize LeOrder MachineRef`.
    for (_, d) in ctx.pures.toList do      materialisePure d
    for (_, d) in ctx.axioms.toList do     materialiseAxiom d
    for (_, d) in ctx.instances.toList do  materialiseInstance d
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
      -- Synthetic `on ev goto tgt` handler bodies need the event's
      -- payload type identifier so the def's `param : <ev>_payload`
      -- binder matches what the obligation generator expects when
      -- `eventHasPayload ctx ev` is true.
      let eventPayloadTy : Name → Option Name := fun ev =>
        match ctx.events.find? ev with
        | some e => if e.payload.isSome
                    then some (ev.appendAfter "_payload")
                    else none
        | none => none
      if !m.body.isEmpty then
        materialiseMachineBody mname m.body vars eventPayloadTy
      elabCommand (← `(end $mid))
    -- Step 7: verification declarations.
    -- Machine and event kind sets drive auto-injection of kind guards
    -- on `∀ x : <M>, …` / `∀ e : <ev>, …` quantifiers inside
    -- `system <s> { … }` blocks (see
    -- `Syntax/Verify.lean::injectKindGuards`). Spec machines are
    -- included for completeness — `is_<M>` is emitted for them too.
    let machineKinds : NameSet :=
      ctx.machineOrder.foldl (init := {}) fun s n => s.insert n
    let eventKinds : NameSet :=
      ctx.eventOrder.foldl (init := {}) fun s n => s.insert n
    -- `pure`s / `paxiom`s / `pinstance`s were already emitted in
    -- Step 5b above — handler bodies need them in scope.
    -- Per-pinstance: synthesise one top-level def per Prop-typed class
    -- field, and accumulate them into `synthAxioms` so they flow into
    -- the persistent `ctx.axioms` map (and thus into the obligation
    -- generator's `have hax_<name>` injection — the same SMT bridge
    -- hand-written `paxiom`s use). Done here rather than in
    -- `materialiseInstance` because adding to `ctx.axioms` requires the
    -- enclosing pmodule's context, which we have here.
    let mut synthAxioms : NameMap PAxiomDecl := {}
    for (_, d) in ctx.instances.toList do
      let decls ← synthInstanceFieldAxioms modName d
      for ax in decls do
        synthAxioms := synthAxioms.insert ax.name ax
    for (_, d) in ctx.invariants.toList do
      materialiseInvariant machineKinds eventKinds machineFields
        eventPayloadFields d
    -- Step 7b: aggregate `init-holds` clauses into `<Mod>.InitConditions`.
    -- The obligation generator consumes this as the base-case
    -- precondition for each invariant.
    emitInitConditions machineKinds eventKinds machineFields
      eventPayloadFields ctx
    -- Step 7c: emit per-Lemma/Theorem bundle predicates. For each
    -- registered Lemma/Theorem `X` whose body lists invariants
    -- `[a, b, c]`, emit `def X : PProp Sig := fun s => a s ∧ b s ∧ c s`
    -- so `Proof { prove X using Y }` can refer to the named lemma.
    emitLemmaBundles ctx
    -- Step 7d: aggregate every free-standing invariant into
    -- `<Mod>.UserInv` (empty → `True`).
    emitUserInv ctx
    elabCommand (← `(end $name))
    -- Step 8: mark types/events/invariants/etc. as materialised by
    -- clearing their `defStx` (so re-elaboration in another file's
    -- import won't fire them again). We KEEP the machine body Syntax
    -- so the obligation generator can extract `var` declarations and
    -- re-walk for entry / goto-only clauses; the `materialised` flag
    -- is set so `#pwf` knows the machine has been emitted.
    let machines' := ctx.machines.foldl (init := ({} : NameMap PMachineDecl))
      fun acc n d => acc.insert n { d with materialised := true }
    let types'    := ctx.types.foldl (init := ({} : NameMap PTypeDecl))
      fun acc n d => acc.insert n { d with defStx := none }
    let events'   := ctx.events.foldl (init := ({} : NameMap PEventDecl))
      fun acc n d => acc.insert n { d with defStx := none }
    let invs'     := ctx.invariants.foldl (init := ({} : NameMap PInvariantDecl))
      fun acc n d => acc.insert n { d with defStx := none }
    let axs'      := ctx.axioms.foldl
      (init := synthAxioms)  -- start with the pinstance-synthesised axioms
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
