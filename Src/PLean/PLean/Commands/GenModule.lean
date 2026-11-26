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
  automatic invariants `NoHalt` (when no machine ever halts),
  `Transient` (a machine is never idle in a state whose entry always ends
  in a `goto`) and `Waiting` (a machine suspended at a `receive` is in
  that `receive`'s state).
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

/-- Whether `stx` contains a `receive`. -/
partial def hasReceive (stx : Syntax) : Bool :=
  stx.getKind == ``pReceive || stx.getArgs.any hasReceive

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
      if hasReceive b then continue
      let seq := if b.getKind == ``Lean.Parser.Term.doSeqBracketed then b[1] else b[0]
      if let some item := seq.getArgs.back? then
        if item[0].getKind == ``pGotoNoPayload || item[0].getKind == ``pGotoPayload then
          out := out.push sname
  return out

/-! ## `receive` sites

A handler or entry body is split at each top-level `receive`: the code
before it ends by suspending at the site (`H.suspend (W.site<k> …)`,
saving the locals), and each case resumes with its body followed by the
code after the `receive`. -/

/-- A `receive` case: its event, payload binder and body. -/
structure RecvCase where
  ev     : Name
  binder : Option (Ident × TSyntax `term)
  body   : TSyntax ``Lean.Parser.Term.doSeq

/-- A local a suspended handler keeps: a `var` or a handler / entry parameter. -/
structure Saved where
  name  : Ident
  ty    : TSyntax `term
  isVar : Bool

/-- A `receive` site. `W.site<idx>` holds `saved`; a case resumes with its
body, then `cont`, which ends by suspending at `next`, if any. -/
structure RecvSite where
  idx   : Nat
  mname : Name
  sname : Name
  saved : Array Saved
  cases : Array RecvCase := #[]
  cont  : Array Syntax := #[]
  next  : Option Nat := none
  /-- The names that shadow machine variables in the handler. -/
  shadow : NameSet := {}

/-- A handler or entry body (assignments to its locals localised), up to its
first top-level `receive`. -/
structure BodyPlan where
  pre   : Array Syntax
  first : Option Nat
  /-- The names that shadow machine variables in the body: its parameter
  and its `var` locals. -/
  shadow : NameSet := {}

/-- The `W` constructor of site `k`. -/
def siteName (k : Nat) : Name := Name.mkSimple s!"site{k}"

/-- The resumer of site `k` for event `ev`. -/
def resumerName (sname : Name) (k : Nat) (ev : Name) : Name :=
  sname ++ Name.mkSimple s!"recv{k}_{ev}"

/-- The items of a `do` sequence. -/
def doSeqItemsOf (b : Syntax) : Array Syntax :=
  (if b.getKind == ``Lean.Parser.Term.doSeqBracketed then b[1] else b[0]).getArgs

private def varsOf (items : Array Syntax) : Array Saved :=
  items.filterMap fun it =>
    if it[0].getKind == ``pLocalVar then some { name := ⟨it[0][1]⟩, ty := ⟨it[0][3]⟩, isVar := true }
    else none

private partial def planItems (mname sname : Name) (saved : Array Saved) (items : Array Syntax) :
    StateM (Array RecvSite) (Array Syntax × Option Nat) := do
  match items.findIdx? (·[0].getKind == ``pReceive) with
  | none => return (items, none)
  | some i =>
    let saved := saved ++ varsOf (items.extract 0 i)
    let idx := (← get).size
    modify (·.push { idx, mname, sname, saved })
    let (cont, next) ← planItems mname sname saved (items.extract (i + 1) items.size)
    let recv := items[i]![0]
    let cases := recv[2].getArgs.map fun c =>
      { ev := c[1].getId
        binder := if c[2].getNumArgs == 5 then some (⟨c[2][1]⟩, ⟨c[2][3]⟩) else none
        body := ⟨c[5]⟩ : RecvCase }
    modify fun a => a.set! idx { idx, mname, sname, saved, cases, cont, next }
    return (items.extract 0 i, some idx)

/-- Localise the assignments to a `receive` case's binder in its case. -/
partial def localizeCaseBinders (stx : Syntax) : MacroM Syntax := do
  let stx := stx.setArgs (← stx.getArgs.mapM localizeCaseBinders)
  if stx.getKind == ``pRecvCase && stx[2].getNumArgs == 5 && stx[2][1].isIdent then
    return stx.setArg 5 (← localizeAssigns (({} : NameSet).insert stx[2][1].getId) stx[5])
  return stx

/-- `stx` with the identifier `x` (and the head of `x.f`) renamed to `x'`. -/
partial def renameIdent (x x' : Name) (stx : Syntax) : Syntax :=
  if stx.isIdent then
    match stx.getId.components with
    | h :: rest => if h == x then mkIdentFrom stx (rest.foldl (· ++ ·) x') else stx
    | [] => stx
  else stx.setArgs (stx.getArgs.map (renameIdent x x'))

/-- Whether `stx` mentions `n` (as `n`, or as the head of `n.f`). -/
private partial def mentionsName (n : Name) (stx : Syntax) : Bool :=
  if stx.isIdent then
    let id := stx.getId
    id == n || (match id with
      | .str (.str .anonymous h) _ => Name.mkSimple h == n
      | _ => false)
  else stx.getArgs.any (mentionsName n)

/-- The identifiers of a `let` / `have` before its `:=` / `←` (and before a
type ascription): the names it binds, and the constructors of a pattern. -/
private partial def letHeadIdents (stx : Syntax) : StateM Bool (Array Name) := do
  if ← get then return #[]
  if stx.isAtom then
    if [":=", "←", ":", "|"].contains stx.getAtomVal then set true
    return #[]
  if stx.isIdent then return #[stx.getId]
  let mut out := #[]
  for c in stx.getArgs do
    out := out ++ (← letHeadIdents c)
  return out

/-- A `let` before a top-level `receive` is not kept while the machine waits
(only `var` locals and parameters are), so it cannot be used after it. -/
def checkLetsAcrossReceive (items : Array Syntax) : CommandElabM Unit := do
  for i in [0:items.size] do
    unless items[i]![0].getKind == ``pReceive do continue
    for j in [0:i] do
      let e := items[j]![0]
      let k := e.getKind
      unless k == ``Lean.Parser.Term.doLet || k == ``Lean.Parser.Term.doLetArrow ||
          k == ``Lean.Parser.Term.doLetElse || k == ``Lean.Parser.Term.doHave do continue
      for n in Id.run ((letHeadIdents e).run' false) do
        -- a constructor in a pattern binds nothing
        let env ← getEnv
        if (← resolveGlobalName n).any fun (c, _) => (env.find? c).any (·.isCtor) then continue
        if (items.extract i items.size).any (mentionsName n) then
          throwErrorAt e m!"`{n}` is bound by `let` before a `receive` and used after it, but \
            only `var` locals and parameters are kept while the machine waits; declare it with \
            `var {n} : T`"

/-- Every handler / entry body's plan, keyed by (machine, state, event or
`entry`), and every `receive` site. -/
def planReceives (ctx : LocalPModuleCtx) :
    CommandElabM (Array ((Name × Name × Name) × BodyPlan) × Array RecvSite) := do
  let mut plans := #[]
  let mut sites : Array RecvSite := #[]
  for mname in ctx.machineOrder do
    let some m := ctx.machines.find? mname | continue
    for (sname, _, items) in machineStates m.body do
      for it in items do
        let (owner, params, body?) : Name × Array Saved × Option Syntax := match it with
          | `(pStateBodyItem| on $ev:ident ( $p:ident : $ty:term ) { $b:doSeq }) =>
            (ev.getId, #[{ name := p, ty, isVar := false }], some b.raw)
          | `(pStateBodyItem| on $ev:ident { $b:doSeq }) => (ev.getId, #[], some b.raw)
          | `(pStateBodyItem| entry { $b:doSeq }) => (`entry, #[], some b.raw)
          | `(pStateBodyItem| entry ( $p:ident : $ty:term ) { $b:doSeq }) =>
            (`entry, #[{ name := p, ty, isVar := false }], some b.raw)
          | _ => (`none, #[], none)
        let some b := body? | continue
        -- Parameters, like `var` locals, shadow machine variables of the
        -- same name (as in P); a `receive` case's binder does in its case.
        let b ← liftMacroM (localizeCaseBinders b)
        let locals := params.foldl (fun s p => s.insert p.name.getId) (collectLocals b)
        let b ← liftMacroM (localizeAssigns locals b)
        let b ← liftMacroM (rewriteIndexReads b)
        checkLetsAcrossReceive (doSeqItemsOf b)
        let shadow := locals
        let n0 := sites.size
        let ((pre, first), sites') := (planItems mname sname params (doSeqItemsOf b)).run sites
        sites := sites'.mapIdx fun j st => if j < n0 then st else { st with shadow }
        plans := plans.push ((mname, sname, owner), { pre, first, shadow })
  return (plans, sites)

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
private def emitProgramTypes (ctx : LocalPModuleCtx) (vars : NameMap (Array VarInfo))
    (sites : Array RecvSite) : CommandElabM Unit := do
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
  -- W: one constructor per `receive` site, holding the saved locals.
  if sites.isEmpty then
    elabCommand (← `(abbrev $idW : Type := Unit))
  else
    let ctors ← sites.mapM fun s => do
      let bs ← s.saved.mapM fun v =>
        `(Lean.Parser.Term.bracketedBinderF| ($(v.name):ident : $(v.ty)))
      `(Lean.Parser.Command.ctor| | $(mkIdent (siteName s.idx)):ident $bs:bracketedBinder*)
    elabCommand (← `(inductive $idW where
        $ctors*))
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

/-- `let this : <Mach> := ⟨(← get).ref⟩` and `let mut v := …` for every var
not in `shadow` (a parameter or local of the same name hides the var, as
in P). -/
private def handlerPrelude (mname : Name) (vars : Array VarInfo) (shadow : NameSet := {}) :
    CommandElabM (Array (TSyntax ``Lean.Parser.Term.doSeqItem)) := do
  let thisB ← `(Lean.Parser.Term.doSeqItem|
      let $(mkIdent `this):ident : $(mkIdent mname) := ⟨(← get).ref⟩)
  let vs ← (vars.filter (!shadow.contains ·.name)).mapM fun v => do
    let f := mkIdent (fieldName mname v.name)
    `(Lean.Parser.Term.doSeqItem| let mut $(mkIdent v.name):ident := (← get).ms.fields.$f:ident)
  return #[thisB] ++ vs

/-- The events a state ignores. -/
def ignoredOf (items : Array Syntax) : Array Name :=
  items.foldl (init := #[]) fun acc it =>
    match it with
    | `(pStateBodyItem| ignore $evs:ident,*) => acc ++ evs.getElems.map (·.getId)
    | _ => acc

/-- The nodes of `stx`, in pre-order. -/
private partial def nodesOf (stx : Syntax) : Array Syntax :=
  stx.getArgs.foldl (init := #[stx]) fun acc c => acc ++ nodesOf c

/-- Reject what P's type checker rejects and the model would otherwise read
differently: `goto S` / `pnew M` without the argument a typed entry takes
(or with one an untyped entry does not take), and `raise e` in a state that
neither handles nor ignores `e`. -/
def checkStatements (ctx : LocalPModuleCtx) : CommandElabM Unit := do
  let startTyped (m : Name) : Option Bool := do
    let md ← ctx.machines.find? m
    let (_, _, items) ← (machineStates md.body).find? (·.2.1)
    return (typedEntry items).isSome
  for mname in ctx.machineOrder do
    let some md := ctx.machines.find? mname | continue
    let states := machineStates md.body
    let typed (st : Name) : Option Bool :=
      (states.find? (·.1 == st)).map fun (_, _, items) => (typedEntry items).isSome
    for (sname, _, items) in states do
      let handled := items.filterMap itemEvent?
      let ignored := ignoredOf items
      for it in items do
        for n in nodesOf it do
          let k := n.getKind
          if k == ``pGotoNoPayload && typed n[1].getId == some true then
            throwErrorAt n m!"`goto {n[1].getId}`: the entry of state `{n[1].getId}` takes an \
              argument; pass it with `goto {n[1].getId}, e`"
          if k == ``pGotoPayload && typed n[1].getId == some false then
            throwErrorAt n m!"`goto {n[1].getId}, …`: the entry of state `{n[1].getId}` takes no \
              argument"
          if k == ``pNewNoArg && startTyped n[1].getId == some true then
            throwErrorAt n m!"`pnew {n[1].getId}`: the entry of `{n[1].getId}`'s start state takes \
              an argument; pass it with `pnew {n[1].getId}, e`"
          if k == ``pNewArg && startTyped n[1].getId == some false then
            throwErrorAt n m!"`pnew {n[1].getId}, …`: the entry of `{n[1].getId}`'s start state \
              takes no argument"
          if (k == ``pRaiseNoPayload || k == ``pRaisePayload) && n[1].isIdent then
            let ev := n[1].getId
            unless ev == `halt || handled.contains ev || ignored.contains ev do
              throwErrorAt n m!"`raise {ev}`: state `{mname}.{sname}` neither handles nor \
                ignores `{ev}` (in P, an unhandled-event error)"

/-- The handler / entry definitions of one state. -/
private def emitStateHandlers (mname sname : Name) (items : Array Syntax) (vars : Array VarInfo)
    (payloadTy : Name → Option Name) (plans : Array ((Name × Name × Name) × BodyPlan))
    (sites : Array RecvSite) (typedStates : NameSet) : CommandElabM Unit := do
  let hName (ev : Name) : Ident := mkIdent (sname ++ ev.appendAfter "_handler")
  -- A definition, then the check that it compares no containers with `=`.
  let emit (n : Ident) (cmd : TSyntax `command) (ref : Syntax) : CommandElabM Unit := do
    elabCommand cmd
    rejectContainerDecideEq ((← getCurrNamespace) ++ n.getId) ref
  -- Parameters are bound after the prelude (which binds the machine
  -- variables), so a parameter shadows a variable of the same name.
  let argId := mkIdent `__arg
  -- An ignored event's handler does nothing (what `raise` of it runs).
  for ev in ignoredOf items do
    match payloadTy ev with
    | some pt =>
      elabCommand (← `(def $(hName ev) (_ : $(mkIdent pt)) : $idHM Unit := pure ()))
    | none => elabCommand (← `(def $(hName ev) : $idHM Unit := pure ()))
  let ordered ← match orderForRaise items with
    | .ok o => pure o
    | .error msg => throwError "state `{mname}.{sname}`: {msg}"
  let suspendAt (k : Nat) : CommandElabM (TSyntax ``Lean.Parser.Term.doSeqItem) := do
    let some s := sites[k]? | throwError "internal: no receive site {k}"
    let args := s.saved.map (·.name)
    `(Lean.Parser.Term.doSeqItem| PLean.H.suspend ($(mkIdent (`W ++ siteName k)) $args*))
  let itemsOf (body : Array Syntax) (next : Option Nat) :
      CommandElabM (Array (TSyntax ``Lean.Parser.Term.doSeqItem)) := do
    let mut out : Array (TSyntax ``Lean.Parser.Term.doSeqItem) := body.map (⟨·⟩)
    if let some k := next then out := out.push (← suspendAt k)
    if out.isEmpty then out := out.push (← `(Lean.Parser.Term.doSeqItem| pure ()))
    return out
  let bodyOf (owner : Name) :
      CommandElabM (Array (TSyntax ``Lean.Parser.Term.doSeqItem) × Array (TSyntax ``Lean.Parser.Term.doSeqItem)) := do
    let some (_, pl) := plans.find? (·.1 == (mname, sname, owner))
      | throwError "internal: no body for `{mname}.{sname}.{owner}`"
    return (← handlerPrelude mname vars pl.shadow, ← itemsOf pl.pre pl.first)
  for it in ordered do
    match it with
    | `(pStateBodyItem| on $ev:ident ( $p:ident : $ty:term ) { $_:doSeq }) =>
      let (pre, body) ← bodyOf ev.getId
      emit (hName ev.getId) (← `(def $(hName ev.getId) ($argId : $ty) : $idHM Unit := do
          $pre*
          let mut $p:ident : $ty := $argId
          $body*)) it
    | `(pStateBodyItem| on $ev:ident { $_:doSeq }) =>
      let (pre, body) ← bodyOf ev.getId
      match payloadTy ev.getId with
      | some pt =>
        emit (hName ev.getId) (← `(def $(hName ev.getId) (_ : $(mkIdent pt)) : $idHM Unit := do
            $pre*
            $body*)) it
      | none =>
        emit (hName ev.getId) (← `(def $(hName ev.getId) : $idHM Unit := do
            $pre*
            $body*)) it
    | `(pStateBodyItem| on $ev:ident goto $tgt:ident) =>
      -- As in P, the event's payload is the argument of the target's entry.
      let tag := mkIdent (tgt.getId.appendAfter "_st")
      let typed := typedStates.contains tgt.getId
      match payloadTy ev.getId, typed with
      | some pt, true =>
        let garg := mkIdent (tgt.getId.appendAfter "_garg")
        elabCommand (← `(def $(hName ev.getId) ($argId : $(mkIdent pt)) : $idHM Unit :=
            PLean.H.goto $tag ($garg $argId)))
      | some pt, false =>
        elabCommand (← `(def $(hName ev.getId) (_ : $(mkIdent pt)) : $idHM Unit :=
            PLean.H.goto $tag $(mkIdent `G.none)))
      | none, true =>
        throwErrorAt it m!"`on {ev.getId} goto {tgt.getId}`: the entry of state \
          `{tgt.getId}` takes an argument, but `{ev.getId}` has no payload"
      | none, false =>
        elabCommand (← `(def $(hName ev.getId) : $idHM Unit := PLean.H.goto $tag $(mkIdent `G.none)))
    | _ => pure ()
  -- entry
  let entryName := mkIdent (sname ++ `entry)
  let mut emitted := false
  for it in items do
    match it with
    | `(pStateBodyItem| entry { $_:doSeq }) =>
      let (pre, body) ← bodyOf `entry
      emit entryName (← `(def $entryName (_ : $idG) : $idHM Unit := do
          $pre*
          $body*)) it
      emitted := true
    | `(pStateBodyItem| entry ( $p:ident : $ty:term ) { $_:doSeq }) =>
      let (pre, body) ← bodyOf `entry
      let gctor := mkIdent (`G ++ stateTagName mname sname)
      emit entryName (← `(def $entryName ($argId : $idG) : $idHM Unit := do
          $pre*
          let mut $p:ident : $ty := match $argId:ident with
            | $gctor:ident x => x
            | _ => default
          $body*)) it
      emitted := true
    | _ => pure ()
  unless emitted do
    elabCommand (← `(def $entryName (_ : $idG) : $idHM Unit := pure ()))
  -- resumers of this state's `receive` sites
  for s in sites do
    unless s.mname == mname && s.sname == sname do continue
    let savedIds := (List.range s.saved.size).toArray.map fun j => mkIdent (Name.mkSimple s!"__s{j}")
    let savedBs ← (s.saved.zip savedIds).mapM fun (v, a) =>
      `(Lean.Parser.Term.bracketedBinderF| ($a:ident : $(v.ty)))
    let restore ← (s.saved.zip savedIds).mapM fun (v, a) =>
      `(Lean.Parser.Term.doSeqItem| let mut $(v.name):ident : $(v.ty) := $a:ident)
    let rest ← itemsOf s.cont s.next
    let pre ← handlerPrelude mname vars s.shadow
    for c in s.cases do
      -- The case's binder is bound in the case's own block, so its scope
      -- ends with the case. Lean forbids shadowing a mutable variable, so a
      -- binder named like a machine variable or a saved local is renamed in
      -- its case.
      let (plB, caseItem) ← match c.binder, payloadTy c.ev with
        | some (x, ty), _ =>
          let collides := vars.any (·.name == x.getId) || s.saved.any (·.name.getId == x.getId)
          let x' : Ident := if collides then mkIdent (x.getId.appendBefore "__case_") else x
          let body := if collides then renameIdent x.getId x'.getId c.body else c.body
          let items : Array (TSyntax ``Lean.Parser.Term.doSeqItem) := (doSeqItemsOf body).map (⟨·⟩)
          pure (#[← `(Lean.Parser.Term.bracketedBinderF| ($argId : $ty))],
            ← `(Lean.Parser.Term.doSeqItem| do
                let mut $x':ident : $ty := $argId
                $items*))
        | none, some pt =>
          pure (#[← `(Lean.Parser.Term.bracketedBinderF| (_ : $(mkIdent pt)))],
            ← `(Lean.Parser.Term.doSeqItem| do $(c.body)))
        | none, none => pure (#[], ← `(Lean.Parser.Term.doSeqItem| do $(c.body)))
      let rn := mkIdent (resumerName sname s.idx c.ev)
      emit rn (← `(def $rn $savedBs:bracketedBinder* $plB:bracketedBinder* : $idHM Unit := do
          $pre*
          $restore*
          $caseItem
          $rest*)) c.body

/-- `prog : PLean.Program S E F G W`: exhaustive matches over (state, event). -/
private def emitProgram (ctx : LocalPModuleCtx) (sites : Array RecvSite) : CommandElabM Unit := do
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
  -- resumers, per (site, case)
  let mut rAlts : Array (TSyntax ``Lean.Parser.Term.matchAlt) := #[]
  for s in sites do
    let args := (List.range s.saved.size).toArray.map fun j => mkIdent (Name.mkSimple s!"a{j}")
    for c in s.cases do
      let hasPayload := (ctx.events.find? c.ev).any (·.hasPayload)
      let pat ← if hasPayload then `($(mkIdent (`E ++ c.ev)) $p) else `($(mkIdent (`E ++ c.ev)))
      let fn := mkIdent (s.mname ++ resumerName s.sname s.idx c.ev)
      let rhs ← if hasPayload then `(some ($fn $args* $p)) else `(some ($fn $args*))
      rAlts := rAlts.push (← `(Lean.Parser.Term.matchAltExpr|
        | $(mkIdent (`W ++ siteName s.idx)) $args*, $pat => $rhs))
  let allCovered := !sites.isEmpty &&
    sites.all fun s => ctx.eventOrder.all fun ev => s.cases.any (·.ev == ev)
  unless allCovered do
    rAlts := rAlts.push (← `(Lean.Parser.Term.matchAltExpr| | _, _ => none))
  let s := mkIdent `s; let e := mkIdent `e; let k := mkIdent `k
  elabCommand (← `(def $(mkIdent `prog) : PLean.Program $idS $idE $idF $idG $idW :=
      ⟨fun $s $e => match $s:ident, $e:ident with $hAlts:matchAlt*,
       fun $s $arg => match $s:ident with $eAlts:matchAlt*,
       fun $k $e => match $k:ident, $e:ident with $rAlts:matchAlt*,
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
    (← `(0 < ($s).nextRef ∧ (($s).machines PLean.MachineRef.null).state = none), false),
    (← `(∀ $r:ident, $r ≠ PLean.MachineRef.null → $r < ($s).nextRef →
        $startD ∧ (($s).machines $r).pending ≠ none), false),
    (← `(∀ $r:ident, (($s).machines $r).halted = false ∧ (($s).machines $r).waiting = none), false)]
  for d in ctx.inits do
    if let some stx := d.defStx then
      PLean.rejectStateShadowIn "`init-holds`" stx[1]
      match d.stateBinder with
      | none => props := props.push (← `(pv_ext% $(⟨stx[1]⟩)), false)
      | some _ =>
        let rw ← liftMacroM <| PLean.rewriteFieldProjections mk ek machineFields {} {} eventFields sName stx[1]
        let g ← liftMacroM <| PLean.injectKindGuards mk ek sName rw
        props := props.push (← `(pv_ext% $(⟨g⟩)), false)
  emitConj (mkIdent `InitConditions) props sName

end Emit

/-! ## The command -/

syntax (name := pGenModule) "#gen_module " ident : command

/-- `#gen_module`'s emission, after the checks. -/
private def genModuleBody (ctx : LocalPModuleCtx) (modName : Name) (name : Ident)
    (plans : Array ((Name × Name × Name) × BodyPlan)) (sites : Array RecvSite) :
    CommandElabM Unit := do
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
  emitProgramTypes ctx vars sites
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
  -- In source order: instances first (functions and axioms may use them),
  -- then functions, then axioms.
  for n in ctx.instanceOrder do
    if let some d := ctx.instances.find? n then materialiseInstance d
  for n in ctx.pureOrder do
    if let some d := ctx.pures.find? n then materialisePure d
  for n in ctx.axiomOrder do
    if let some d := ctx.axioms.find? n then
      materialiseAxiom machineKinds eventKinds machineFields {} {} eventFields d
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
    let typedStates : NameSet := states.foldl (init := {}) fun acc (sn, _, items) =>
      if (typedEntry items).isSome then acc.insert sn else acc
    for (sname, _, items) in states do
      emitStateHandlers mname sname items mvars payloadTy plans sites typedStates
    elabCommand (← `(end $(mkIdent mname)))
  emitProgram ctx sites
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
  -- A machine suspended at a `receive` site is in the site's state.
  unless sites.isEmpty do
    let conjs ← sites.mapM fun st => do
      let args := (List.range st.saved.size).toArray.map fun j => mkIdent (Name.mkSimple s!"a{j}")
      let tag := mkIdent (`S ++ stateTagName st.mname st.sname)
      `(∀ $args*, (s.machines r).waiting = some ($(mkIdent (`W ++ siteName st.idx)) $args*) →
          (s.machines r).state = some $tag)
    let body ← conjs.pop.foldrM (init := conjs.back!) fun c acc => `($c ∧ $acc)
    elabCommand (← `(@[reducible] def $(mkIdent `Waiting) (s : $idGW) : Prop := ∀ r, $body))
  -- `pinstance` class fields become named facts, used as solver hints.
  let mut axioms := ctx.axioms
  for n in ctx.instanceOrder do
    let some d := ctx.instances.find? n | continue
    for ax in ← synthInstanceFieldAxioms modName d do
      axioms := axioms.insert ax.name ax
  elabCommand (← `(end $name))
  setPModule
    { ctx with
      axioms
      machines := ctx.machines.foldl (init := {}) fun acc n d => acc.insert n { d with materialised := true } }

@[command_elab pGenModule]
def elabPGenModule : CommandElab := fun stx => do
  let `(#gen_module $name:ident) := stx | throwUnsupportedSyntax
  let modName := name.getId
  let some ctx ← getPModule? modName
    | throwError "no `pmodule {modName}` is registered (did you import the file that declares it?)"
  unless ctx.conflicts.isEmpty do
    throwError "pmodule `{modName}`: its fragments disagree:\n{"\n".intercalate ctx.conflicts.toList}"
  checkStatements ctx
  let (plans, sites) ← planReceives ctx
  -- On an error, close the namespaces opened below.
  let scopes := (← get).scopes
  try genModuleBody ctx modName name plans sites
  catch e =>
    modify fun st => { st with scopes }
    throw e

end PLean
