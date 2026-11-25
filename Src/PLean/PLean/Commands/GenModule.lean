/-
PLean.Commands.GenModule — `#gen_module M`: turn the registered pmodule
`M` into Lean definitions over the transition model.

Emitted inside `namespace M`:

* user types / event payloads / pure functions / axioms / instances;
* `structure <Mach> where ref : MachineRef` per machine (invariant binders);
* the program types: `E` (events), `S` (state tags `<Mach>_<State>`),
  `F` (every machine's vars, `<Mach>_<v>`), `G` (entry arguments), `W`
  (suspended `receive`s), and `HM` / `GW` for the handler monad and the
  ghost world;
* `is_<Mach> r s`, `is_<ev> m`, `<ev>_payload_of m` for invariants;
* per machine: state aliases `<S>_st` / `<S>_garg`, field accessors
  `<v>_set`, handlers `<S>.<ev>_handler`, entries `<S>.entry`;
* `prog : PLean.Program S E F G W`;
* `InitConditions` (every machine in its start state, its start entry
  pending), the invariants and `Lemma` / `Theorem` bundles, and the
  automatic invariants `NoHalt` (when no machine ever halts) and
  `Transient` (a machine is never idle in a state whose entry always ends
  in a `goto`).
-/
import Lean
import PLean.Internal.Decls
import PLean.Internal.Registry
import PLean.Syntax.Types
import PLean.Syntax.Events
import PLean.Syntax.Machine
import PLean.Syntax.Verify
import PLean.Syntax.Stmt
import PLean.Syntax.Loop
import PLean.Syntax.Containers
import PLean.Semantics.Msg
import PLean.Verify.Norm

open Lean Elab Command

namespace PLean

/-- A machine variable. -/
structure VarInfo where
  name : Name
  ty   : TSyntax `term

/-- The `var` declarations of a machine body. -/
def collectVars (body : Array Syntax) : Array VarInfo := Id.run do
  let mut vars : Array VarInfo := #[]
  for it in body do
    match it with
    | `(pMachineBodyItem| var $vname:ident : $vty:term) =>
      vars := vars.push { name := vname.getId, ty := vty }
    | _ => pure ()
  return vars

/-- `<Mach>_<State>`, the state-tag constructor name. -/
def stateTagName (m s : Name) : Name := Name.mkSimple (m.toString ++ "_" ++ s.toString)

/-- `<Mach>_<v>`, the field name in `F`. -/
def fieldName (m v : Name) : Name := Name.mkSimple (m.toString ++ "_" ++ v.toString)

/-- The state items of a machine body: `(name, isStart, items)`. -/
def machineStates (body : Array Syntax) : Array (Name × Bool × Array Syntax) := Id.run do
  let mut out := #[]
  for it in body do
    match it with
    | `(pMachineBodyItem| start state $sid:ident { $items:pStateBodyItem* }) =>
      out := out.push (sid.getId, true, items.map (·.raw))
    | `(pMachineBodyItem| state $sid:ident { $items:pStateBodyItem* }) =>
      out := out.push (sid.getId, false, items.map (·.raw))
    | _ => pure ()
  return out

/-- The typed entry of a state, if any: `(param, type)`. -/
def typedEntry (items : Array Syntax) : Option (Ident × TSyntax `term) := Id.run do
  for it in items do
    match it with
    | `(pStateBodyItem| entry ( $p:ident : $ty:term ) { $_:doSeq }) => return some (p, ty)
    | _ => pure ()
  return none

/-- Events raised (`raise ev`) anywhere in `stx`. -/
partial def raisedEvents (stx : Syntax) : Array Name :=
  let here : Array Name :=
    if stx.getKind == ``pRaiseNoPayload || stx.getKind == ``pRaisePayload then
      if stx[1].isIdent then #[stx[1].getId] else #[]
    else #[]
  stx.getArgs.foldl (fun acc a => acc ++ raisedEvents a) here

/-- Whether any machine body contains `raise halt`. -/
def programHalts (ctx : LocalPModuleCtx) : Bool :=
  ctx.machineOrder.any fun m =>
    match ctx.machines.find? m with
    | some md => md.body.any fun b => (raisedEvents b).contains `halt
    | none => false

/-- The states of `md` whose entry always ends in a `goto` (a machine is
never idle in them). -/
def transientStates (md : PMachineDecl) : Array Name := Id.run do
  let mut out := #[]
  for (sname, _, items) in machineStates md.body do
    for it in items do
      let body? : Option Syntax := match it with
        | `(pStateBodyItem| entry { $b:doSeq }) => some b
        | `(pStateBodyItem| entry ( $_:ident : $_:term ) { $b:doSeq }) => some b
        | _ => none
      let some b := body? | continue
      let seq := if b.getKind == ``Lean.Parser.Term.doSeqBracketed then b[1] else b[0]
      if let some item := seq.getArgs.back? then
        if item[0].getKind == ``pGotoNoPayload || item[0].getKind == ``pGotoPayload then
          out := out.push sname
  return out

/-- The event handled by a state item, with its body (`none` for `goto`-only
and `ignore` items). -/
def itemEvent? (it : Syntax) : Option Name :=
  match it with
  | `(pStateBodyItem| on $ev:ident ( $_:ident : $_:term ) { $_:doSeq }) => some ev.getId
  | `(pStateBodyItem| on $ev:ident { $_:doSeq }) => some ev.getId
  | `(pStateBodyItem| on $ev:ident goto $_:ident) => some ev.getId
  | _ => none

/-- Order a state's handler items so that a handler is emitted after every
handler it raises into (`raise` is inlined as a call). -/
def orderForRaise (items : Array Syntax) : Except String (Array Syntax) := Id.run do
  let handlers := items.filter fun it => (itemEvent? it).isSome
  let mut done : Array Name := #[]
  let mut out : Array Syntax := #[]
  let mut remaining := handlers
  while !remaining.isEmpty do
    let ready := remaining.filter fun it =>
      (raisedEvents it).all fun e => e == `halt || done.contains e ||
        !(handlers.any fun h => itemEvent? h == some e)
    if ready.isEmpty then
      return .error "cyclic `raise` between handlers of one state"
    for r in ready do
      out := out.push r
      if let some e := itemEvent? r then done := done.push e
    remaining := remaining.filter fun it => !(ready.contains it)
  return .ok out

section Emit

private def idE : Ident := mkIdent `E
private def idS : Ident := mkIdent `S
private def idF : Ident := mkIdent `F
private def idG : Ident := mkIdent `G
private def idW : Ident := mkIdent `W
private def idHM : Ident := mkIdent `HM
private def idGW : Ident := mkIdent `GW
private def idRef : Ident := mkIdent ``PLean.MachineRef

private def emitMachineWrappers (ctx : LocalPModuleCtx) : CommandElabM Unit := do
  for mname in ctx.machineOrder do
    let mid := mkIdent mname
    elabCommand (← `(structure $mid where
        ref : $idRef
        deriving Inhabited, DecidableEq, Repr))
    elabCommand (← `(instance : Coe $mid $idRef := ⟨fun s => s.ref⟩))
    let fa := mkIdent (mname ++ `forall_iff)
    let ex := mkIdent (mname ++ `exists_iff)
    elabCommand (← `(@[pverifySimp] theorem $fa (P : $mid → Prop) : (∀ n, P n) ↔ ∀ r, P ⟨r⟩ :=
        ⟨fun h r => h ⟨r⟩, fun h n => h n.ref⟩))
    elabCommand (← `(@[pverifySimp] theorem $ex (P : $mid → Prop) : (∃ n, P n) ↔ ∃ r, P ⟨r⟩ :=
        ⟨fun ⟨n, h⟩ => ⟨n.ref, h⟩, fun ⟨r, h⟩ => ⟨⟨r⟩, h⟩⟩))
    elabCommand (← `(attribute [pverifySimp] $(mkIdent (mname ++ `mk.injEq))))

/-- `E`, `S`, `F`, `G`, `W`, `HM`, `GW`. -/
private def emitProgramTypes (ctx : LocalPModuleCtx) (vars : NameMap (Array VarInfo)) :
    CommandElabM Unit := do
  -- E
  let evCtors ← ctx.eventOrder.mapM fun en => do
    let some e := ctx.events.find? en | throwError "internal: event {en} missing"
    match e.hasPayload with
    | false => `(Lean.Parser.Command.ctor| | $(mkIdent e.name):ident)
    | true =>
      `(Lean.Parser.Command.ctor| | $(mkIdent e.name):ident (payload : $(mkIdent (e.name.appendAfter "_payload"))))
  let evCtors ← if evCtors.isEmpty then pure #[← `(Lean.Parser.Command.ctor| | _none)] else pure evCtors
  elabCommand (← `(inductive $idE where
      $evCtors*
      deriving DecidableEq))
  let (firstName, hasArg) := match ctx.eventOrder[0]? with
    | some en => (en, (ctx.events.find? en).any (fun (d : PEventDecl) => d.hasPayload))
    | none => (`_none, false)
  let firstFull := mkIdent (`E ++ firstName)
  if hasArg then elabCommand (← `(instance : Inhabited $idE := ⟨$firstFull default⟩))
  else elabCommand (← `(instance : Inhabited $idE := ⟨$firstFull⟩))
  -- S
  let mut sCtors : Array (TSyntax ``Lean.Parser.Command.ctor) := #[]
  for mname in ctx.machineOrder do
    let some m := ctx.machines.find? mname | continue
    for (sname, _, _) in machineStates m.body do
      sCtors := sCtors.push (← `(Lean.Parser.Command.ctor| | $(mkIdent (stateTagName mname sname)):ident))
  if sCtors.isEmpty then sCtors := #[← `(Lean.Parser.Command.ctor| | _none)]
  elabCommand (← `(inductive $idS where
      $sCtors*
      deriving DecidableEq, Inhabited, Repr))
  -- F
  let mut fields : Array (TSyntax ``Lean.Parser.Command.structSimpleBinder) := #[]
  for mname in ctx.machineOrder do
    for v in (vars.find? mname).getD #[] do
      fields := fields.push (← `(Lean.Parser.Command.structSimpleBinder|
        $(mkIdent (fieldName mname v.name)):ident : $(v.ty)))
  if fields.isEmpty then
    elabCommand (← `(structure $idF where
        deriving Inhabited))
  else
    elabCommand (← `(structure $idF where
        $[$fields]*
        deriving Inhabited))
  -- G
  let mut gCtors : Array (TSyntax ``Lean.Parser.Command.ctor) := #[← `(Lean.Parser.Command.ctor| | $(mkIdent `none):ident)]
  for mname in ctx.machineOrder do
    let some m := ctx.machines.find? mname | continue
    for (sname, _, items) in machineStates m.body do
      if let some (_, ty) := typedEntry items then
        gCtors := gCtors.push (← `(Lean.Parser.Command.ctor|
          | $(mkIdent (stateTagName mname sname)):ident (arg : $ty)))
  elabCommand (← `(inductive $idG where
      $gCtors*
      deriving Inhabited))
  -- W: suspended receives (none yet).
  elabCommand (← `(abbrev $idW : Type := Unit))
  elabCommand (← `(abbrev $idHM := PLean.HandlerM $idS $idE $idF $idG $idW))
  elabCommand (← `(abbrev $idGW := PLean.GWorld $idS $idE $idF $idG $idW))
  -- `if` through each field projection, for the obligation normaliser.
  for mname in ctx.machineOrder do
    for v in (vars.find? mname).getD #[] do
      let f := mkIdent (fieldName mname v.name)
      let thm := mkIdent (`F ++ (fieldName mname v.name).appendAfter "_ite")
      elabCommand (← `(@[pverifySimp] theorem $thm {c : Prop} [Decidable c] (a b : $idF) :
          (if c then a else b).$f:ident = if c then a.$f:ident else b.$f:ident := by
        split <;> rfl))

/-- `is_<Mach>`, `is_<ev>`, `<ev>_payload_of` (+ its spec lemma). -/
private def emitKindPredicates (ctx : LocalPModuleCtx) : CommandElabM Unit := do
  for mname in ctx.machineOrder do
    let some m := ctx.machines.find? mname | continue
    let states := machineStates m.body
    let r := mkIdent `r; let s := mkIdent `s
    let disj ← match states.toList with
      | [] => `(False)
      | (s0, _, _) :: rest => do
        let mut d ← `(((($s).machines $r).state) = some $(mkIdent (`S ++ stateTagName mname s0)))
        for (si, _, _) in rest do
          d ← `($d ∨ ((($s).machines $r).state = some $(mkIdent (`S ++ stateTagName mname si))))
        pure d
    let isId := mkIdent (Name.mkSimple ("is_" ++ mname.toString))
    elabCommand (← `(@[reducible] def $isId ($r:ident : $idRef) ($s:ident : $idGW) : Prop := $disj))
  for en in ctx.eventOrder do
    let some e := ctx.events.find? en | continue
    let isId := mkIdent (Name.mkSimple ("is_" ++ en.toString))
    let ctor := mkIdent (`E ++ en)
    let m := mkIdent `m
    match e.hasPayload with
    | false =>
      elabCommand (← `(@[reducible] def $isId ($m:ident : PLean.Msg $idE) : Prop := ($m).ev = $ctor))
    | true =>
      let pty := mkIdent (en.appendAfter "_payload")
      elabCommand (← `(@[reducible] def $isId ($m:ident : PLean.Msg $idE) : Prop := ∃ p, ($m).ev = $ctor p))
      let ofId := mkIdent (Name.mkSimple (en.toString ++ "_payload_of"))
      let specId := mkIdent (Name.mkSimple (en.toString ++ "_payload_of_spec"))
      if ctx.eventOrder.size > 1 then
        elabCommand (← `(def $ofId ($m:ident : PLean.Msg $idE) : $pty :=
            match ($m).ev with
            | $ctor p => p
            | _ => default))
      else
        elabCommand (← `(def $ofId ($m:ident : PLean.Msg $idE) : $pty :=
            match ($m).ev with
            | $ctor p => p))
      elabCommand (← `(theorem $specId : ∀ ($m:ident : PLean.Msg $idE) (p : $pty),
          ($m).ev = $ctor p → $ofId $m = p := by
        intro m p h; simp only [$ofId:ident, h]))
      let mkId := mkIdent (Name.mkSimple (en.toString ++ "_payload_of_mk"))
      elabCommand (← `(@[pverifySimp] theorem $mkId (i : Nat) (a b : $idRef) (p : $pty) :
          $ofId ⟨i, a, b, $ctor p⟩ = p := rfl))

/-- Per machine, inside `namespace <Mach>`: state aliases, accessors. -/
private def emitMachineAliases (mname : Name) (states : Array (Name × Bool × Array Syntax))
    (vars : Array VarInfo) : CommandElabM Unit := do
  for (sname, _, items) in states do
    let tag := mkIdent (`S ++ stateTagName mname sname)
    elabCommand (← `(abbrev $(mkIdent (sname.appendAfter "_st")) : $idS := $tag))
    if let some (_, ty) := typedEntry items then
      let gctor := mkIdent (`G ++ stateTagName mname sname)
      elabCommand (← `(abbrev $(mkIdent (sname.appendAfter "_garg")) (x : $ty) : $idG := $gctor x))
  if let some (sname, _, items) := states.find? (·.2.1) then
    let tag := mkIdent (`S ++ stateTagName mname sname)
    elabCommand (← `(abbrev $(mkIdent `start_st) : $idS := $tag))
    if let some (_, ty) := typedEntry items then
      let gctor := mkIdent (`G ++ stateTagName mname sname)
      elabCommand (← `(abbrev $(mkIdent `start_garg) (x : $ty) : $idG := $gctor x))
  for v in vars do
    let f := mkIdent (fieldName mname v.name)
    elabCommand (← `(abbrev $(mkIdent (v.name.appendAfter "_set")) (x : $(v.ty)) : $idHM Unit :=
        modify fun hs => { hs with ms := { hs.ms with fields := { hs.ms.fields with $f:ident := x } } }))

/-- `let this : <Mach> := ⟨(← get).ref⟩` and `let mut v := …` for every var. -/
private def handlerPrelude (mname : Name) (vars : Array VarInfo) :
    CommandElabM (Array (TSyntax ``Lean.Parser.Term.doSeqItem)) := do
  let thisB ← `(Lean.Parser.Term.doSeqItem|
      let $(mkIdent `this):ident : $(mkIdent mname) := ⟨(← get).ref⟩)
  let vs ← vars.mapM fun v => do
    let f := mkIdent (fieldName mname v.name)
    `(Lean.Parser.Term.doSeqItem| let mut $(mkIdent v.name):ident := (← get).ms.fields.$f:ident)
  return #[thisB] ++ vs

/-- The handler / entry definitions of one state. -/
private def emitStateHandlers (mname sname : Name) (items : Array Syntax) (vars : Array VarInfo)
    (payloadTy : Name → Option Name) : CommandElabM Unit := do
  let pre ← handlerPrelude mname vars
  let hName (ev : Name) : Ident := mkIdent (sname ++ ev.appendAfter "_handler")
  let ordered ← match orderForRaise items with
    | .ok o => pure o
    | .error msg => throwError "state `{mname}.{sname}`: {msg}"
  let localize (body : TSyntax ``Lean.Parser.Term.doSeq) :
      CommandElabM (TSyntax ``Lean.Parser.Term.doSeq) :=
    return ⟨← liftMacroM (localizeAssigns (collectLocals body) body)⟩
  for it in ordered do
    match it with
    | `(pStateBodyItem| on $ev:ident ( $p:ident : $ty:term ) { $body:doSeq }) =>
      let body ← localize body
      elabCommand (← `(def $(hName ev.getId) ($p : $ty) : $idHM Unit := do
          $pre*
          do $body))
    | `(pStateBodyItem| on $ev:ident { $body:doSeq }) =>
      let body ← localize body
      match payloadTy ev.getId with
      | some pt =>
        elabCommand (← `(def $(hName ev.getId) (_payload : $(mkIdent pt)) : $idHM Unit := do
            $pre*
            do $body))
      | none =>
        elabCommand (← `(def $(hName ev.getId) : $idHM Unit := do
            $pre*
            do $body))
    | `(pStateBodyItem| on $ev:ident goto $tgt:ident) =>
      let tag := mkIdent (tgt.getId.appendAfter "_st")
      match payloadTy ev.getId with
      | some pt =>
        elabCommand (← `(def $(hName ev.getId) (_payload : $(mkIdent pt)) : $idHM Unit :=
            PLean.H.goto $tag $(mkIdent `G.none)))
      | none =>
        elabCommand (← `(def $(hName ev.getId) : $idHM Unit := PLean.H.goto $tag $(mkIdent `G.none)))
    | _ => pure ()
  -- entry
  let entryName := mkIdent (sname ++ `entry)
  let arg := mkIdent `arg
  let mut emitted := false
  for it in items do
    match it with
    | `(pStateBodyItem| entry { $body:doSeq }) =>
      let body ← localize body
      elabCommand (← `(def $entryName (_ : $idG) : $idHM Unit := do
          $pre*
          do $body))
      emitted := true
    | `(pStateBodyItem| entry ( $p:ident : $ty:term ) { $body:doSeq }) =>
      let body ← localize body
      let gctor := mkIdent (`G ++ stateTagName mname sname)
      elabCommand (← `(def $entryName ($arg : $idG) : $idHM Unit := do
          let $p : $ty := match $arg:ident with
            | $gctor:ident x => x
            | _ => default
          $pre*
          do $body))
      emitted := true
    | _ => pure ()
  unless emitted do
    elabCommand (← `(def $entryName (_ : $idG) : $idHM Unit := pure ()))

/-- `prog : PLean.Program S E F G W`: exhaustive matches over (state, event). -/
private def emitProgram (ctx : LocalPModuleCtx) : CommandElabM Unit := do
  let mut hAlts : Array (TSyntax ``Lean.Parser.Term.matchAlt) := #[]
  let mut eAlts : Array (TSyntax ``Lean.Parser.Term.matchAlt) := #[]
  let mut dAlts : Array (TSyntax ``Lean.Parser.Term.matchAlt) := #[]
  let arg := mkIdent `arg
  let p := mkIdent `p
  let mut nStates := 0
  for mname in ctx.machineOrder do
    let some m := ctx.machines.find? mname | continue
    for (sname, _, items) in machineStates m.body do
      nStates := nStates + 1
      let tag := mkIdent (`S ++ stateTagName mname sname)
      let sd? := m.states.find? (·.name == sname)
      let ign := sd?.map (·.ignoredEvents) |>.getD #[]
      let dfr := sd?.map (·.deferredEvents) |>.getD #[]
      for ev in ctx.eventOrder do
        let hasPayload := (ctx.events.find? ev).any (·.hasPayload)
        let pat ← if hasPayload then `($(mkIdent (`E ++ ev)) $p) else `($(mkIdent (`E ++ ev)))
        let h := mkIdent (mname ++ sname ++ ev.appendAfter "_handler")
        let rhs ←
          if items.any (itemEvent? · == some ev) then
            if hasPayload then `(some ($h $p)) else `(some $h)
          else if ign.contains ev then `(some (pure ()))
          else `(none)
        hAlts := hAlts.push (← `(Lean.Parser.Term.matchAltExpr| | $tag, $pat => $rhs))
        let d ← if dfr.contains ev then `(true) else `(false)
        dAlts := dAlts.push (← `(Lean.Parser.Term.matchAltExpr| | $tag, $pat => $d))
      let entryFn := mkIdent (mname ++ sname ++ `entry)
      eAlts := eAlts.push (← `(Lean.Parser.Term.matchAltExpr| | $tag => $entryFn $arg))
  if nStates == 0 || ctx.eventOrder.isEmpty then
    hAlts := hAlts.push (← `(Lean.Parser.Term.matchAltExpr| | _, _ => none))
    dAlts := dAlts.push (← `(Lean.Parser.Term.matchAltExpr| | _, _ => false))
  if nStates == 0 then
    eAlts := eAlts.push (← `(Lean.Parser.Term.matchAltExpr| | _ => pure ()))
  let s := mkIdent `s; let e := mkIdent `e
  elabCommand (← `(def $(mkIdent `prog) : PLean.Program $idS $idE $idF $idG $idW :=
      ⟨fun $s $e => match $s:ident, $e:ident with $hAlts:matchAlt*,
       fun $s $arg => match $s:ident with $eAlts:matchAlt*,
       fun _ _ => none,
       fun $s $e => match $s:ident, $e:ident with $dAlts:matchAlt*⟩))

/-- `def name : GW → Prop := fun s => c₁ ∧ … ∧ cₙ` (`True` when empty);
`applyState` conjuncts are applied to `s`. -/
private def emitConj (name : Ident) (members : Array (TSyntax `term × Bool)) (sName : Name := `s) :
    CommandElabM Unit := do
  let sId := mkIdent sName
  if members.isEmpty then
    elabCommand (← `(@[reducible] def $name : $idGW → Prop := fun _ => True))
    return
  let render (m : TSyntax `term × Bool) : CommandElabM (TSyntax `term) :=
    if m.2 then `(($(m.1)) $sId) else pure m.1
  let mut body ← render members.back!
  for m in members.pop.reverse do
    body ← `($(← render m) ∧ $body)
  elabCommand (← `(@[reducible] def $name : $idGW → Prop := fun $sId => $body))

/-- `InitConditions`: the framework's initial-world facts plus `init-holds`. -/
private def emitInitConditions (ctx : LocalPModuleCtx) (mk ek : NameSet)
    (machineFields eventFields : NameMap NameSet) : CommandElabM Unit := do
  let sName := ctx.sBinder.getD `s
  let s := mkIdent sName
  let r := mkIdent `r; let j := mkIdent `j
  let mut startDisj : Option (TSyntax `term) := none
  for mname in ctx.machineOrder do
    let some m := ctx.machines.find? mname | continue
    if let some (sname, _, _) := (machineStates m.body).find? (·.2.1) then
      let c ← `((($s).machines $r).state = some $(mkIdent (`S ++ stateTagName mname sname)))
      startDisj ← match startDisj with
        | none => pure (some c)
        | some d => some <$> `($d ∨ $c)
  let startD ← match startDisj with | some d => pure d | none => `(False)
  let mut props : Array (TSyntax `term × Bool) := #[
    (← `(($s).clock = 0), false),
    (← `(∀ $j:ident, ($s).net $j = none), false),
    (← `(∀ $j:ident, ($s).log $j = none), false),
    (← `(($s).error = none), false),
    (← `(∀ $r:ident, ($s).nextRef ≤ $r → (($s).machines $r).state = none), false),
    (← `(∀ $r:ident, $r < ($s).nextRef → $startD ∧ (($s).machines $r).pending ≠ none ∧
          (($s).machines $r).waiting = none), false),
    (← `(∀ $r:ident, (($s).machines $r).halted = false), false)]
  for d in ctx.inits do
    if let some stx := d.defStx then
      PLean.rejectStateShadowIn "`init-holds`" stx[1]
      match d.stateBinder with
      | none => props := props.push (⟨stx[1]⟩, false)
      | some _ =>
        let rw ← liftMacroM <| PLean.rewriteFieldProjections mk ek machineFields {} {} eventFields sName stx[1]
        let g ← liftMacroM <| PLean.injectKindGuards mk ek sName rw
        props := props.push (⟨g⟩, false)
  emitConj (mkIdent `InitConditions) props sName

end Emit

/-! ## The command -/

syntax (name := pGenModule) "#gen_module " ident : command

@[command_elab pGenModule]
def elabPGenModule : CommandElab := fun stx => do
  let `(#gen_module $name:ident) := stx | throwUnsupportedSyntax
  let modName := name.getId
  let some ctx ← getPModule? modName
    | throwError "no `pmodule {modName}` is registered (did you import the file that declares it?)"
  elabCommand (← `(namespace $name))
  elabCommand (← `(open PLean))
  emitMachineWrappers ctx
  for tname in ctx.typeOrder do
    if let some t := ctx.types.find? tname then materialiseType t
  for ename in ctx.eventOrder do
    if let some e := ctx.events.find? ename then materialiseEvent e
  let mut vars : NameMap (Array VarInfo) := {}
  for mname in ctx.machineOrder do
    if let some m := ctx.machines.find? mname then vars := vars.insert mname (collectVars m.body)
  emitProgramTypes ctx vars
  emitKindPredicates ctx
  let machineKinds : NameSet := ctx.machineOrder.foldl (init := {}) fun s n => s.insert n
  let eventKinds : NameSet := ctx.eventOrder.foldl (init := {}) fun s n => s.insert n
  let machineFields : NameMap NameSet := ctx.machineOrder.foldl (init := {}) fun acc m =>
    acc.insert m (((vars.find? m).getD #[]).foldl (init := {}) fun s v => s.insert v.name)
  let eventFields : NameMap NameSet := Id.run do
    let mut out : NameMap NameSet := {}
    for ename in ctx.eventOrder do
      let some e := ctx.events.find? ename | continue
      let some pn := e.payload | continue
      let some pt := ctx.types.find? pn | continue
      let some ps := pt.defStx | continue
      match ps with
      | `(type $_:ident = ($[$flds:pNamedField],*)) =>
        out := out.insert ename (flds.foldl (init := {}) fun s f =>
          if f.raw[0].isIdent then s.insert f.raw[0].getId else s)
      | _ => pure ()
    return out
  for (_, d) in ctx.pures.toList do materialisePure d
  for (_, d) in ctx.axioms.toList do
    materialiseAxiom machineKinds eventKinds machineFields {} {} eventFields d
  for (_, d) in ctx.instances.toList do materialiseInstance d
  -- per-machine aliases, accessors, handlers
  let payloadTy : Name → Option Name := fun ev =>
    match ctx.events.find? ev with
    | some e => if e.hasPayload then some (ev.appendAfter "_payload") else none
    | none => none
  -- Axiomatised instances (`pinstance`) make the program noncomputable.
  let noncomp := !ctx.instances.isEmpty
  if noncomp then elabCommand (← `(noncomputable section))
  for mname in ctx.machineOrder do
    let some m := ctx.machines.find? mname | continue
    let mvars := (vars.find? mname).getD #[]
    let states := machineStates m.body
    elabCommand (← `(namespace $(mkIdent mname)))
    emitMachineAliases mname states mvars
    for (sname, _, items) in states do
      emitStateHandlers mname sname items mvars payloadTy
    elabCommand (← `(end $(mkIdent mname)))
  emitProgram ctx
  if noncomp then elabCommand (← `(end))
  -- specifications
  for (_, d) in ctx.invariants.toList do
    materialiseInvariant machineKinds eventKinds machineFields {} {} eventFields d
  for lname in ctx.lemmaOrder do
    let some l := ctx.lemmas.find? lname | continue
    emitConj (mkIdent l.name) (l.invariants.map fun n => (⟨mkIdent n⟩, true))
  emitInitConditions ctx machineKinds eventKinds machineFields eventFields
  unless programHalts ctx do
    elabCommand (← `(@[reducible] def $(mkIdent `NoHalt) (s : $idGW) : Prop :=
        ∀ r, (s.machines r).halted = false))
  let mut transient : Array (TSyntax `term) := #[]
  for mname in ctx.machineOrder do
    let some md := ctx.machines.find? mname | continue
    for sname in transientStates md do
      transient := transient.push (← `(((s.machines r).state = some $(mkIdent (`S ++ stateTagName mname sname)))))
  if let some last := transient.back? then
    let disj ← transient.pop.foldrM (init := last) fun d acc => `($d ∨ $acc)
    elabCommand (← `(@[reducible] def $(mkIdent `Transient) (s : $idGW) : Prop :=
        ∀ r, $disj → (s.machines r).pending ≠ none))
  -- `pinstance` class fields become named facts, used as solver hints.
  let mut axioms := ctx.axioms
  for (_, d) in ctx.instances.toList do
    for ax in ← synthInstanceFieldAxioms modName d do
      axioms := axioms.insert ax.name ax
  elabCommand (← `(end $name))
  setPModule
    { ctx with
      axioms
      machines := ctx.machines.foldl (init := {}) fun acc n d => acc.insert n { d with materialised := true } }

end PLean
