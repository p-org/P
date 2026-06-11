/-
Helper that builds the per-handler dispatcher contract.

⚠ **Currently inert.** `Verify/Obligation.lean` builds an equivalent
existential inline in `emitOneObligation`'s `dispatcherClause` block.
The two shapes differ in lambda packaging; this file is kept so the
canonical `DispatcherContract` shape is documented in one place.

The contract witnesses the framework's runtime guarantee that a
handler is only fired when an inflight label exists targeting `this`,
the label's action is `event (E.<ev> ?)`, the machine is in state
`<S>_st`, and (when present) the label's payload equals `param`.
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
event) triple. For payload-less events, the constructor pattern drops
the payload argument. -/
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
