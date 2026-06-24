/-
PLean.Syntax.Stmt — `doElem`-level macros for statements inside a
handler body: `send`, `raise`, `goto`, `pnew`, `announce`, plus the
machine-var assignment `<v> = <expr>`. Each rewrites to a call into
the corresponding `PLean.*` primitive over the per-pmodule `Sig`.

Macro hygiene: identifiers that must resolve against user-namespace
constants emitted by `#gen_module` (`Sig`, `E`/`G` constructors,
`<S>_st` aliases, the handler's `this`) are built via `mkIdent` and
spliced in. Bare names inside macro quotations pick up hygiene marks
and fail to resolve.
-/
import Lean
import PLean.Semantics.Primitives

open Lean

namespace PLean

/-! ## Unhygienic identifier helpers (see file-level note on hygiene). -/

private def idThis : Ident := mkIdent `this
private def idSig  : Ident := mkIdent `Sig
private def idG    : Ident := mkIdent `G
private def idGUnit : Ident := mkIdent (`G ++ `unit)

private def thisRef : MacroM (TSyntax `term) := do
  let thisI := idThis
  `($thisI |>.ref)

private def evCtorIdent (evIdent : Ident) : Ident :=
  -- `<evname>` ↦ `E.<evname>` (the per-pmodule event-union constructor).
  mkIdentFrom evIdent (`E ++ evIdent.getId)

/-! ## Send -/

/-- One `field = value` entry in a send payload. -/
syntax pNamedTupleField := ident " = " term

/-- A non-empty named-tuple payload: `(f = v, …)`. -/
syntax pSendNamedTuple := "(" pNamedTupleField,+ ")"

-- `priority := high` so the named-tuple form wins over the generic
-- `pSendPayload` (a `term` would otherwise consume the parenthesised
-- tuple as an anonymous expression).
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

macro_rules
  | `(doElem| send $target, $ev, ($[$flds:pNamedTupleField],*)) => do
    let rec : TSyntax `term ← buildAnonRecord flds
    if ev.raw.isIdent then
      let evIdent : Ident := ⟨ev.raw⟩
      let payloadTy := mkIdentFrom evIdent (evIdent.getId.appendAfter "_payload")
      let evCtor := evCtorIdent evIdent
      `(doElem| PLean.send (P := $idSig) ($target : PLean.MachineRef)
                  ($evCtor ($rec : $payloadTy)))
    else
      `(doElem| PLean.send (P := $idSig) ($target : PLean.MachineRef) $ev)
  | `(doElem| send $target, $ev, $payload:term) => do
    if ev.raw.isIdent then
      let evIdent : Ident := ⟨ev.raw⟩
      let evCtor := evCtorIdent evIdent
      `(doElem| PLean.send (P := $idSig) ($target : PLean.MachineRef) ($evCtor $payload))
    else
      `(doElem| PLean.send (P := $idSig) ($target : PLean.MachineRef) $ev)
  | `(doElem| send $target, $ev) => do
    if ev.raw.isIdent then
      let evIdent : Ident := ⟨ev.raw⟩
      let evCtor := evCtorIdent evIdent
      `(doElem| PLean.send (P := $idSig) ($target : PLean.MachineRef) $evCtor)
    else
      `(doElem| PLean.send (P := $idSig) ($target : PLean.MachineRef) $ev)

/-! ## Raise — intra-machine `send`. -/

syntax (name := pRaisePayload) "raise " term ", " term : doElem
syntax (name := pRaiseNoPayload) "raise " term : doElem

macro_rules
  | `(doElem| raise $ev, $payload) => do
    let tref ← thisRef
    if ev.raw.isIdent then
      let evIdent : Ident := ⟨ev.raw⟩
      let evCtor := evCtorIdent evIdent
      `(doElem| PLean.raise (P := $idSig) $tref ($evCtor $payload))
    else
      `(doElem| PLean.raise (P := $idSig) $tref $ev)
  | `(doElem| raise $ev) => do
    let tref ← thisRef
    if ev.raw.isIdent then
      let evIdent : Ident := ⟨ev.raw⟩
      let evCtor := evCtorIdent evIdent
      `(doElem| PLean.raise (P := $idSig) $tref $evCtor)
    else
      `(doElem| PLean.raise (P := $idSig) $tref $ev)

/-! ## Goto — real state transition. -/

syntax (name := pGotoNoPayload) "goto " ident : doElem
syntax (name := pGotoPayload) "goto " ident ", " term : doElem

macro_rules
  | `(doElem| goto $st:ident) => do
    let tref ← thisRef
    let stTag := mkIdentFrom st (st.getId.appendAfter "_st")
    `(doElem| PLean.goto (P := $idSig) $tref $stTag $idGUnit)
  | `(doElem| goto $_st:ident, $payload) => do
    -- Payload-bearing `goto S, p` is parsed but not yet supported: the
    -- per-pmodule `Sig.G` union is `Unit`-only. Erroring is safer than
    -- silently discarding the payload.
    Macro.throwErrorAt payload
      "`goto <state>, <payload>` is not yet supported (the per-pmodule \
       goto-payload union `Sig.G` is `Unit`-only). Use `goto <state>` \
       and pass arguments via a separate `send`/`raise`."

/-! ## New (machine creation) -/

syntax (name := pNewNoArg) "pnew " ident : doElem
syntax (name := pNewArg)   "pnew " ident ", " term : doElem

-- `pnew M` resolves the kind tag through the per-pmodule registered
-- `<M>_kind : Nat` def (emitted by `Commands/GenModule.lean::emitMachineKinds`).
-- Lean's namespace search picks `<Mod>.<M>_kind` because handler defs
-- elaborate inside `<Mod>.<MachineName>` after `open <Mod>` — the same
-- mechanism that resolves `Sig` and `is_<M>`. Using a hash of `m`'s
-- string form here would let the freshly-created machine violate
-- `<M>_allocated` (the registered tag and the hash needn't match).
private def kindIdentFor (m : Ident) : Ident :=
  mkIdentFrom m (m.getId.appendAfter "_kind")

macro_rules
  | `(doElem| pnew $m:ident) => do
    let tref ← thisRef
    let kindId := kindIdentFor m
    `(doElem| PLean.newMachine (P := $idSig) $tref $kindId)
  | `(doElem| pnew $m:ident, $_arg) => do
    let tref ← thisRef
    let kindId := kindIdentFor m
    `(doElem| PLean.newMachine (P := $idSig) $tref $kindId)

/-! ## Assignment

`pAssign` parses `<ident> = <term>` at high priority (to win over Lean's
default `=` parses inside a `do` block) and rewrites it into
`<lhs>_set this.ref <rhs>; let <lhs> ← <lhs>_get this.ref`.

Limitation: the macro fires on any identifier-followed-by-`=` form,
including ones the user did not intend as a machine-var write. If
`<lhs>` is not a machine var, the user sees a "unknown identifier
`x_set`" error rather than a bespoke "var `x` is not declared in this
machine". A registry-aware version of this macro requires access to the
local pmodule context at expansion time (only reachable from
`CommandElabM`, not `MacroM`). -/

syntax (name := pAssign) (priority := high) ident " = " term : doElem

macro_rules
  | `(doElem| $lhs:ident = $rhs:term) => do
    let tref ← thisRef
    let setIdent := mkIdentFrom lhs (lhs.getId.appendAfter "_set")
    let getIdent := mkIdentFrom lhs (lhs.getId.appendAfter "_get")
    `(doElem| do
        $setIdent:ident $tref $rhs
        let $lhs:ident ← $getIdent:ident $tref)

/-! ## Announce -/

syntax (name := pAnnouncePayload)   "announce " term ", " term : doElem
syntax (name := pAnnounceNoPayload) "announce " term            : doElem

macro_rules
  | `(doElem| announce $ev, $payload) => do
    let tref ← thisRef
    if ev.raw.isIdent then
      let evIdent : Ident := ⟨ev.raw⟩
      let evCtor := evCtorIdent evIdent
      `(doElem| PLean.announce (P := $idSig) $tref ($evCtor $payload))
    else
      `(doElem| PLean.announce (P := $idSig) $tref $ev)
  | `(doElem| announce $ev) => do
    let tref ← thisRef
    if ev.raw.isIdent then
      let evIdent : Ident := ⟨ev.raw⟩
      let evCtor := evCtorIdent evIdent
      `(doElem| PLean.announce (P := $idSig) $tref $evCtor)
    else
      `(doElem| PLean.announce (P := $idSig) $tref $ev)

end PLean
