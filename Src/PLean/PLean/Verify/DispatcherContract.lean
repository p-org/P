/-
PLean.Verify.DispatcherContract — builds the `DispatcherContract`
precondition from registry data (D18, D27).

⚠ **Currently inert** (REVIEW_P3 §4.1).
`Verify/Obligation.lean` constructs the dispatcher clause inline at
[`emitOneObligation`'s `dispatcherClause` block](Obligation.lean) and
does not call `buildDispatcherContractTerm` below. The two emit
*similar but not identical* shapes — this helper wraps the existential
in a `fun (this) [param] (s) => ...` lambda, whereas the inline
version embeds the existential body directly into the obligation's
precondition conjunction. Until a refactor unifies them this file is
kept around so the DispatcherContract docstring lives next to its
intended-shape source-of-truth; the helper is *not* re-exported.

Phase 3 emits per-handler triple obligations whose precondition is

  Inv s ∧ InitConditions s ∧ DispatcherContract this s ev param

where `DispatcherContract` materialises the framework's runtime
guarantee that this handler was only fired because

  - an inflight label exists targeting `this`,
  - the label's action is `event (E.<ev> ?)`,
  - the machine is in state `M.<S>_st` (its `currentState`),
  - the payload extracted from the label equals `param`.

We model this as an existential over the dispatched label, packaging
the four conditions. The handler is parameterised by `(this, param)`
already; we existentially quantify the label.

For an event `ev` with no payload (`event ev`), the contract drops
the payload-equality clause.
-/
import Lean
import PLean.Internal.Decls
import PLean.Internal.Registry

open Lean Elab Command

namespace PLean
namespace Verify

/-- Convenience: identifier for the per-pmodule `Sig`. Constructed
unhygienically because `Sig` lives in the user-facing pmodule
namespace. -/
private def idSig : Ident := mkIdent `Sig

/-- Build the dispatcher-contract syntax for a given (machine, state,
event) triple. Returns:

  fun (this : MName) (param : <ev>_payload) (s : GS) =>
    ∃ lbl : Sig.Label,
      PLean.inflight lbl s ∧
      lbl.target = this.ref ∧
      (s.machines this.ref).currentState = <S>_st ∧
      lbl.action = .event (E.<ev> param)

For payload-less events the param/.event match drops the payload.

The shape is identical to the M1 manual `inflight lbl s ∧ ...`
precondition, but threaded existentially because the surface
handler signature drops the `lbl` parameter (Phase 2's
`#gen_module` doesn't bind it). The Phase-3 wrapper (D27) re-introduces
`lbl`; this contract is for the wrapped form. -/
def buildDispatcherContractTerm (mname sname evname : Name)
    (hasPayload : Bool) :
    MacroM (TSyntax `term) := do
  let mIdent : Ident := mkIdent mname
  let stateAlias : Ident := mkIdent (sname.appendAfter "_st")
  let evCtor : Ident := mkIdent (`E ++ evname)
  let _ := mIdent
  let _ := stateAlias
  if hasPayload then
    let payloadTy := mkIdent (evname.appendAfter "_payload")
    `(fun (this : $mIdent) (param : $payloadTy) (s : ($idSig).GlobalState) =>
        ∃ lbl : ($idSig).Label,
          PLean.inflight lbl s ∧
          lbl.target = this.ref ∧
          (s.machines this.ref).currentState = $stateAlias ∧
          lbl.action = .event ($evCtor param))
  else
    `(fun (this : $mIdent) (s : ($idSig).GlobalState) =>
        ∃ lbl : ($idSig).Label,
          PLean.inflight lbl s ∧
          lbl.target = this.ref ∧
          (s.machines this.ref).currentState = $stateAlias ∧
          lbl.action = .event $evCtor)

end Verify
end PLean
