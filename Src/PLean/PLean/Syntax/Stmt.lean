/-
PLean.Syntax.Stmt — P statements inside handler bodies, as `doElem`
macros over the handler primitives (`PLean.H.*`).

Handler bodies are elaborated inside the machine's namespace, after
`#gen_module` has emitted the per-pmodule names they refer to: the event
type `E`, the entry-argument type `G`, the per-machine field accessors
`<v>_set`, the state aliases `<S>_st` / `<S>_garg`, and the handlers
`<ev>_handler` (for an inlined `raise`). Those names are built with
`mkIdent` so they resolve against the user's namespaces rather than
picking up macro hygiene.

Every machine variable `v` is a `let mut` local of the handler, kept in
sync with the machine's field: each write updates both.
-/
import Lean
import PLean.Semantics.Handler
import PLean.Semantics.Containers

namespace PLean

open Lean

private def evCtorIdent (ev : Ident) : Ident := mkIdentFrom ev (`E ++ ev.getId)
private def setterOf (v : Ident) : Ident := mkIdentFrom v (v.getId.appendAfter "_set")

/-! ## `send` / `announce` -/

syntax pNamedTupleField := ident " = " term
syntax pSendNamedTuple := "(" pNamedTupleField,+ ")"

syntax (name := pSendNamed) (priority := high)
  "send " term ", " term ", " pSendNamedTuple : doElem
syntax (name := pSendPayload)   "send " term ", " term ", " term : doElem
syntax (name := pSendNoPayload) "send " term ", " term           : doElem

private def buildAnonRecord (fields : Array (TSyntax `PLean.pNamedTupleField)) :
    MacroM (TSyntax `term) := do
  let entries ← fields.mapM fun f => do
    let id : Ident := ⟨f.raw[0]⟩
    let v  : TSyntax `term := ⟨f.raw[2]⟩
    `(Lean.Parser.Term.structInstField| $id:ident := $v)
  `({ $entries:structInstField,* })

/-- The event value `E.<ev> payload` (or `E.<ev>`) for an event name. -/
private def eventValue (ev : TSyntax `term) (payload : Option (TSyntax `term)) :
    MacroM (TSyntax `term) := do
  if ev.raw.isIdent then
    let c := evCtorIdent ⟨ev.raw⟩
    match payload with
    | some p => `($c $p)
    | none => `($c)
  else pure ev

macro_rules
  | `(doElem| send $target, $ev, ($[$flds:pNamedTupleField],*)) => do
    let rec_ ← buildAnonRecord flds
    let payloadTy := mkIdentFrom ev (ev.raw.getId.appendAfter "_payload")
    let v ← eventValue ev (some (← `(($rec_ : $payloadTy))))
    `(doElem| PLean.H.send ($target : PLean.MachineRef) $v)
  | `(doElem| send $target, $ev, $payload:term) => do
    `(doElem| PLean.H.send ($target : PLean.MachineRef) $(← eventValue ev (some payload)))
  | `(doElem| send $target, $ev) => do
    `(doElem| PLean.H.send ($target : PLean.MachineRef) $(← eventValue ev none))

syntax (name := pAnnouncePayload)   "announce " term ", " term : doElem
syntax (name := pAnnounceNoPayload) "announce " term            : doElem

macro_rules
  | `(doElem| announce $ev, $payload) => do
    `(doElem| PLean.H.announce $(← eventValue ev (some payload)))
  | `(doElem| announce $ev) => do
    `(doElem| PLean.H.announce $(← eventValue ev none))

/-! ## Control: `goto`, `raise`, `halt` -/

syntax (name := pGotoNoPayload) "goto " ident : doElem
syntax (name := pGotoPayload) "goto " ident ", " term : doElem

macro_rules
  | `(doElem| goto $st:ident) => do
    let tag := mkIdentFrom st (st.getId.appendAfter "_st")
    `(doElem| do PLean.H.goto $tag $(mkIdent `G.none); return)
  | `(doElem| goto $st:ident, $arg) => do
    let tag := mkIdentFrom st (st.getId.appendAfter "_st")
    let garg := mkIdentFrom st (st.getId.appendAfter "_garg")
    `(doElem| do PLean.H.goto $tag ($garg $arg); return)

/-- `raise ev[, payload]`: run the current state's handler for `ev` right
away (P semantics: the raised event is handled before any dequeue), then
end this handler. `raise halt` halts the machine. -/
syntax (name := pRaisePayload) "raise " ident ", " term : doElem
syntax (name := pRaiseNoPayload) "raise " ident : doElem

macro_rules
  | `(doElem| raise $ev:ident) => do
    if ev.getId == `halt then
      `(doElem| do PLean.H.halt; return)
    else
      let h := mkIdentFrom ev (ev.getId.appendAfter "_handler")
      `(doElem| do $h:ident; return)
  | `(doElem| raise $ev:ident, $payload) => do
    let h := mkIdentFrom ev (ev.getId.appendAfter "_handler")
    `(doElem| do $h:ident $payload; return)

/-! ## `new` -/

syntax (name := pNewNoArg) "pnew " ident : doElem
syntax (name := pNewArg)   "pnew " ident ", " term : doElem

macro_rules
  | `(doElem| pnew $m:ident) => do
    let start := mkIdentFrom m (m.getId ++ `start_st)
    `(doElem| discard <| PLean.H.new (PLean.MachineState.fresh $start $(mkIdent `G.none)))
  | `(doElem| pnew $m:ident, $arg) => do
    let start := mkIdentFrom m (m.getId ++ `start_st)
    let garg := mkIdentFrom m (m.getId ++ `start_garg)
    `(doElem| discard <| PLean.H.new (PLean.MachineState.fresh $start ($garg $arg)))

/-! ## `assert` -/

syntax (name := pAssert) "assert " term : doElem

macro_rules
  | `(doElem| assert $c) => `(doElem| PLean.H.passert (decide ($c)) 0)

/-! ## Assignments and container updates

Each writes the local `v` and the machine field `v`. -/

syntax (name := pSetAdd) (priority := high) ident " += " "(" term ")" : doElem
syntax (name := pSetRemove) (priority := high) ident " -= " "(" term ")" : doElem
syntax (name := pMapAssign) (priority := high) ident "[" term "]" " = " term : doElem
syntax (name := pMapAdd) (priority := high) ident "[" term "]" " += " "(" term ")" : doElem
syntax (name := pMapRemove) (priority := high) ident "[" term "]" " -= " "(" term ")" : doElem
syntax (name := pAssign) (priority := high) ident " = " term : doElem

macro_rules
  | `(doElem| $v:ident += ($e:term)) =>
    `(doElem| do $v:ident := PLean.PContainerAdd.add $v $e; $(setterOf v):ident $v)
  | `(doElem| $v:ident -= ($e:term)) =>
    `(doElem| do $v:ident := PLean.PContainerErase.erase $v $e; $(setterOf v):ident $v)
  | `(doElem| $v:ident [ $k:term ] = $x:term) =>
    `(doElem| do $v:ident := PLean.PMap.put $v $k $x; $(setterOf v):ident $v)
  | `(doElem| $v:ident [ $k:term ] += ($e:term)) =>
    `(doElem| do
        $v:ident := PLean.PMap.put $v $k (PLean.PContainerAdd.add (($v).get $k |>.getD default) $e)
        $(setterOf v):ident $v)
  | `(doElem| $v:ident [ $k:term ] -= ($e:term)) =>
    `(doElem| do
        $v:ident := PLean.PMap.put $v $k (PLean.PContainerErase.erase (($v).get $k |>.getD default) $e)
        $(setterOf v):ident $v)
  | `(doElem| $v:ident = $rhs:term) =>
    `(doElem| do $v:ident := $rhs; $(setterOf v):ident $v)

/-! ## `receive`

`receive { case e1 (x : T) => { … } case e2 => { … } }` suspends the
handler until one of the cases' events arrives, then runs that case and
the rest of the handler. `#gen_module` splits handlers at their
top-level `receive`s; anywhere else a `receive` is rejected. -/

syntax pRecvCase := "case " ident ("(" ident " : " term ")")? " => " "{" doSeq "}"
syntax (name := pReceive) "receive " "{" pRecvCase* "}" : doElem

macro_rules
  | `(doElem| receive { $_* }) =>
    Macro.throwError "`receive` must be a top-level statement of a handler or entry body \
      (not inside `if`, a loop or a `receive` case)"

/-! ## Local variables

`var x : T` declares a handler local, initialised to `default`. Writes to
a local do not touch the machine's fields: `#gen_module` rewrites the
assignments to declared locals with `localizeAssigns`. -/

syntax (name := pLocalVar) "var " ident " : " term : doElem

macro_rules
  | `(doElem| var $x:ident : $t:term) => `(doElem| let mut $x:ident : $t := default)

/-- The locals a handler body declares with `var`. -/
partial def collectLocals (stx : Syntax) : NameSet :=
  let here := if stx.getKind == ``pLocalVar then ({} : NameSet).insert stx[1].getId else {}
  stx.getArgs.foldl (fun acc a => (collectLocals a).foldl NameSet.insert acc) here

/-- Turn assignments to the given locals into plain reassignments. -/
def localizeAssigns (locals : NameSet) (stx : Syntax) : MacroM Syntax :=
  if locals.isEmpty then pure stx else
  stx.replaceM fun s => do
    let isLocal (v : Ident) := locals.contains v.getId
    match s with
    | `(doElem| $v:ident = $rhs:term) =>
      if isLocal v then some <$> `(doElem| $v:ident := $rhs) else pure none
    | `(doElem| $v:ident += ($e:term)) =>
      if isLocal v then some <$> `(doElem| $v:ident := PLean.PContainerAdd.add $v $e) else pure none
    | `(doElem| $v:ident -= ($e:term)) =>
      if isLocal v then some <$> `(doElem| $v:ident := PLean.PContainerErase.erase $v $e) else pure none
    | `(doElem| $v:ident [ $k:term ] = $x:term) =>
      if isLocal v then some <$> `(doElem| $v:ident := PLean.PMap.put $v $k $x) else pure none
    | `(doElem| $v:ident [ $k:term ] += ($e:term)) =>
      if isLocal v then
        some <$> `(doElem| $v:ident :=
          PLean.PMap.put $v $k (PLean.PContainerAdd.add (($v).get $k |>.getD default) $e))
      else pure none
    | `(doElem| $v:ident [ $k:term ] -= ($e:term)) =>
      if isLocal v then
        some <$> `(doElem| $v:ident :=
          PLean.PMap.put $v $k (PLean.PContainerErase.erase (($v).get $k |>.getD default) $e))
      else pure none
    | _ => pure none

/-! ## Nondeterminism -/

/-- `if ($) then … else …` (and `$$`): a nondeterministic branch. -/
syntax (name := pIfNondet) "if " "($)" " then " doSeq (" else " doSeq)? : doElem
syntax (name := pIfNondet2) "if " "($$)" " then " doSeq (" else " doSeq)? : doElem

macro_rules
  | `(doElem| if ($) then $a:doSeq) =>
    `(doElem| do let c ← PLean.H.flip; if c then do $a)
  | `(doElem| if ($) then $a:doSeq else $b:doSeq) =>
    `(doElem| do let c ← PLean.H.flip; if c then do $a else do $b)
  | `(doElem| if ($$) then $a:doSeq) =>
    `(doElem| do let c ← PLean.H.flip; if c then do $a)
  | `(doElem| if ($$) then $a:doSeq else $b:doSeq) =>
    `(doElem| do let c ← PLean.H.flip; if c then do $a else do $b)

/-- What `choose(c)` picks from. -/
class PChoose (C : Type) (R : outParam Type) where
  choose {S E F G W : Type} : C → HandlerM S E F G W R

instance : PChoose Int Int := ⟨H.chooseInt⟩
instance {T : Type} [DecidableEq T] : PChoose (PSet T) T := ⟨H.chooseSet⟩
instance {K V : Type} [DecidableEq K] : PChoose (PMap K V) K := ⟨fun m => H.chooseSet m.keySet⟩

/-- P's `choose(c)`: an integer in `[0, c]`, a member of a set, or a key of
a map (`let x ← choose c`). -/
@[inline] def choose {S E F G W C R : Type} [PChoose C R] (c : C) : HandlerM S E F G W R :=
  PChoose.choose c

section
open Std.WP
variable {S E F G W : Type} {epost : PError → Prop}

@[spec] theorem choose_int_spec {n : Int} {post : Int → HState S E F G W → Prop} :
    Triple (choose (S := S) (E := E) (F := F) (G := G) (W := W) n)
      (fun hs => (n < 0 → epost .badChoose) ∧
                 (0 ≤ n → ∀ i : Int, 0 ≤ i → i ≤ n → post i hs.tick)) post epost :=
  H.chooseInt_spec

@[spec] theorem choose_set_spec {T : Type} [DecidableEq T] {s : PSet T}
    {post : T → HState S E F G W → Prop} :
    Triple (choose (S := S) (E := E) (F := F) (G := G) (W := W) s)
      (fun hs => (s.card = 0 → epost .emptyChoose) ∧
                 (∀ x, s.mem x = true → post x hs.tick)) post epost :=
  H.chooseSet_spec

@[spec] theorem choose_map_spec {K V : Type} [DecidableEq K] {m : PMap K V}
    {post : K → HState S E F G W → Prop} :
    Triple (choose (S := S) (E := E) (F := F) (G := G) (W := W) m)
      (fun hs => (m.keySet.card = 0 → epost .emptyChoose) ∧
                 (∀ x, m.keySet.mem x = true → post x hs.tick)) post epost :=
  H.chooseSet_spec
end

end PLean
