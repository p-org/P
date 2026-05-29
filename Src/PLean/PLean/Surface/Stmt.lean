/-
PLean.Surface.Stmt — statement-level macros.

P statements inside a handler body. Phase 0 supports
the most common five:
  send <target>, <event>, <payload>?
  raise <event>, <payload>?
  goto <stateName>, <payload>?
  new <interfaceName>, <payload>?

We expose macros at Lean's `doElem` level so handler bodies can be written
with Lean's `do`-notation. That lets us reuse Lean's expression parser
(preserving things like `≺`, quantifiers, `lbl is e`) without
reimplementing it.

Each macro elaborates to a call into `PLean.Stub` (Phase 0). The
user-facing surface stays the same when Phase 1 swaps in the real `PM`.
-/
import Lean
import PLean.Internal.Stub

open Lean

namespace PLean.Stub

/-- Convert a state/machine identifier-name to a numeric tag.

Phase 0 stub representation; Phase 1 replaces with per-module indices. -/
@[inline] def tagOf (n : Name) : Nat := n.hash.toNat

end PLean.Stub

namespace PLean

/-! ## Send

P's send syntax is `send <machine>, <event>, (field=value, ...)`
where the third argument is a named-tuple literal with at least one
field. We accept three forms in Phase 0:

  send target, ev, (f1 = v1, ..., fn = vn)   -- named-tuple payload
  send target, ev, <term>                    -- raw-term payload
  send target, ev                            -- no payload

The named-tuple form elaborates to Lean's `{ f1 := v1, ..., fn := vn }`
anonymous-record syntax. The raw-term form is a fallback for Phase 0
flexibility (e.g., when payloads are primitive `Nat`s); P's surface
won't allow it once Phase 1 binds payloads to their declared types.
-/

/-- One `field = value` entry in a send payload. -/
syntax pNamedTupleField := ident " = " term

/-- A non-empty named-tuple payload: `(f = v, …)`. The trailing comma is
    optional; a single-field tuple may be written `(f = v)` (P's grammar
    allows `(f = v,)` but not the bare form, which would be a parenthesized
    expression). -/
syntax pSendNamedTuple := "(" pNamedTupleField,+ ")"

syntax (name := pSendNamed) (priority := high)
  "send " term ", " term ", " pSendNamedTuple : doElem
syntax (name := pSendPayload)   "send " term ", " term ", " term : doElem
syntax (name := pSendNoPayload) "send " term ", " term           : doElem

private def buildAnonRecord (fields : Array (TSyntax `PLean.pNamedTupleField)) :
    MacroM (TSyntax `term) := do
  -- Each field is `ident = term`; positions 0=ident, 2=term.
  let entries ← fields.mapM fun f => do
    let id : Ident := ⟨f.raw[0]⟩
    let v  : TSyntax `term := ⟨f.raw[2]⟩
    `(Lean.Parser.Term.structInstField| $id:ident := $v)
  `({ $entries:structInstField,* })

macro_rules
  | `(doElem| send $target, $ev, ($[$flds:pNamedTupleField],*)) => do
    let rec : TSyntax `term ← buildAnonRecord flds
    -- The named-tuple literal `{ f := v, ... }` needs a type annotation to
    -- elaborate. We synthesize one from `<ev>_payload` (an abbrev emitted
    -- by `event ev : T`). When `ev` is a bare identifier we ascribe;
    -- otherwise (e.g. `ev` is a more complex expression) we fall through
    -- to the unascribed form, which will fail to elaborate with a clearer
    -- "type not known" message.
    if ev.raw.isIdent then
      let evIdent : Ident := ⟨ev.raw⟩
      let payloadTy := mkIdentFrom evIdent (evIdent.getId.appendAfter "_payload")
      `(doElem| PLean.Stub.send $target $ev ($rec : $payloadTy))
    else
      `(doElem| PLean.Stub.send $target $ev $rec)
  | `(doElem| send $target, $ev, $payload:term) =>
    `(doElem| PLean.Stub.send $target $ev $payload)
  | `(doElem| send $target, $ev) =>
    `(doElem| PLean.Stub.send $target $ev ())

/-! ## Raise -/

syntax (name := pRaisePayload) "raise " term ", " term : doElem
syntax (name := pRaiseNoPayload) "raise " term : doElem

macro_rules
  | `(doElem| raise $ev, $payload) =>
    `(doElem| PLean.Stub.raise $ev $payload)
  | `(doElem| raise $ev) =>
    `(doElem| PLean.Stub.raise $ev ())

/-! ## Goto

  `goto stateName` / `goto stateName, payload`

We capture the state name as a string literal at parse time so we don't
have to invent quotation gymnastics for `Name`. The Phase-0 stub then
hashes the string at runtime — same effect, simpler macro. -/

syntax (name := pGotoNoPayload) "goto " ident : doElem
syntax (name := pGotoPayload) "goto " ident ", " term : doElem

macro_rules
  | `(doElem| goto $st:ident) => do
    let lit := Syntax.mkStrLit st.getId.toString
    `(doElem| PLean.Stub.goto ($lit).hash.toNat ())
  | `(doElem| goto $st:ident, $payload) => do
    let lit := Syntax.mkStrLit st.getId.toString
    `(doElem| PLean.Stub.goto ($lit).hash.toNat $payload)

/-! ## New (machine creation) -/

syntax (name := pNewNoArg) "pnew " ident : doElem
syntax (name := pNewArg)   "pnew " ident ", " term : doElem

macro_rules
  | `(doElem| pnew $m:ident) => do
    let lit := Syntax.mkStrLit m.getId.toString
    `(doElem| PLean.Stub.new ($lit).hash.toNat ())
  | `(doElem| pnew $m:ident, $arg) => do
    let lit := Syntax.mkStrLit m.getId.toString
    `(doElem| PLean.Stub.new ($lit).hash.toNat $arg)

/-! ## Assignment

  `<lhs> = <rhs>`

P uses bare `=` for assignment (vs Lean's `:=` or `←`). At Phase 0 the
LHS is just a name; reads of machine `var`s are likewise the bare name.
Real read/write semantics arrive in Phase 1 when GlobalState materialises;
for now this is a no-op that lets handler bodies type-check.

We cannot use `=` directly — Lean parses it as an equality term — so we
match the wider pattern `ident = term` at doElem position with priority. -/

syntax (name := pAssign) (priority := high) ident " = " term : doElem

/-- Phase-0 assignment is a no-op at the value level, but we DO have to
    reference both sides so Lean's section-`variable` auto-inclusion
    picks them up. Otherwise a handler whose body is `client = input.client`
    would leave `client` (the machine-level var) out of the signature,
    and a subsequent reference to `client` from an `on …` handler in the
    same state would fail to resolve. -/
macro_rules
  | `(doElem| $lhs:ident = $rhs:term) =>
    `(doElem| do
        let _ := $lhs
        let _ := $rhs
        pure ())

/-! ## Announce -/

syntax (name := pAnnouncePayload)   "announce " term ", " term : doElem
syntax (name := pAnnounceNoPayload) "announce " term            : doElem

macro_rules
  | `(doElem| announce $ev, $payload) =>
    `(doElem| PLean.Stub.announce $ev $payload)
  | `(doElem| announce $ev) =>
    `(doElem| PLean.Stub.announce $ev ())

end PLean
