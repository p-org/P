/-
PLean.Surface.Stmt — statement-level macros.

P statements inside a handler body. Phase 2 supports
the most common five:
  send <target>, <event>, <payload>?
  raise <event>, <payload>?
  goto <stateName>, <payload>?
  pnew <interfaceName>, <payload>?
  announce <event>, <payload>?

We expose macros at Lean's `doElem` level so handler bodies can be written
with Lean's `do`-notation. That lets us reuse Lean's expression parser
(preserving things like `≺`, quantifiers, `lbl is e`) without
reimplementing it.

Each macro elaborates to a call into `PLean.send` / `PLean.raise` /
`PLean.goto` / `PLean.announce` / `PLean.newMachine` (the real PM
primitives over the synthesised per-pmodule `Sig`). The `(P := Sig)`
ascription resolves `Sig` against the surrounding namespace — handler
defs are emitted inside `<Mod>.<MachineName>` after `open <Mod>`, so
`Sig` finds `<Mod>.Sig`.

Decision D11 (Phase 2): macros target the real PM. The Phase-0 `Stub`
indirection is gone; `PLean.Internal.Stub` is retired (D15).

## A note on macro hygiene

Several macros below reference user-namespace constants emitted by
`#gen_module`:

  - the handler's `this` binder (made unhygienic in `#gen_module` via
    `mkIdent \`this`),
  - the per-pmodule `Sig` constant (`<Mod>.Sig`),
  - the per-pmodule `E` / `G` constructors and `<S>_st` aliases.

Bare names written inside macro quotations would acquire hygiene marks
and fail to resolve against those user-namespace constants. We build
each via `mkIdent` and splice it in. Lean handles `.field` projections
and qualified names like `PLean.send` correctly without further
intervention.
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

/-! ## Goto — D13: real transition. -/

syntax (name := pGotoNoPayload) "goto " ident : doElem
syntax (name := pGotoPayload) "goto " ident ", " term : doElem

macro_rules
  | `(doElem| goto $st:ident) => do
    let tref ← thisRef
    let stTag := mkIdentFrom st (st.getId.appendAfter "_st")
    `(doElem| PLean.goto (P := $idSig) $tref $stTag $idGUnit)
  | `(doElem| goto $st:ident, $_payload) => do
    let tref ← thisRef
    let stTag := mkIdentFrom st (st.getId.appendAfter "_st")
    `(doElem| PLean.goto (P := $idSig) $tref $stTag $idGUnit)

/-! ## New (machine creation) -/

syntax (name := pNewNoArg) "pnew " ident : doElem
syntax (name := pNewArg)   "pnew " ident ", " term : doElem

macro_rules
  | `(doElem| pnew $m:ident) => do
    let tref ← thisRef
    let lit := Syntax.mkStrLit m.getId.toString
    `(doElem| PLean.newMachine (P := $idSig) $tref ($lit).hash.toNat)
  | `(doElem| pnew $m:ident, $_arg) => do
    let tref ← thisRef
    let lit := Syntax.mkStrLit m.getId.toString
    `(doElem| PLean.newMachine (P := $idSig) $tref ($lit).hash.toNat)

/-! ## Assignment (D12). -/

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
