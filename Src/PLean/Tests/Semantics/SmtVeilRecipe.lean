/-
Pin: `pverify_smt` closes a default-invariant obligation on PLean's
real `GlobalState` shape. The packaged recipe — destructure the
state-struct hypothesis, intros, then bare simp — is what lets
lean-auto translate `GlobalState`-shaped goals; a regression that
reverts any step in `pverify_smt_prep` is caught here.
-/
import PLean
import PLean.Verify.Tactic

set_option loom.solver "cvc5"
set_option loom.solver.smt.timeout 8

open PLean PartialCorrectness DemonicChoice

/-! ## A tiny `Sig` for the probe -/

inductive Ev | ePing | ePong
  deriving DecidableEq, Inhabited

inductive G | unit
  deriving DecidableEq, Inhabited

inductive S | onlyState
  deriving DecidableEq, Inhabited

structure F where
  deriving Inhabited

abbrev Sig : ProgramSig := { E := Ev, G := G, S := S, F := F }
abbrev GS := PLean.GlobalState Sig

/-! ## THE PROBE

`UniqueActions` is preserved across one `addSent` + `bumpActionCount`,
under the precondition that the new label's `actionCount` equals the
pre-state's `actionCount` and the pre-state satisfies UA / IC.

This is *exactly* the shape a `prove default;` obligation produces
for a single-`send` handler. -/

example (s : GS) (newLbl : Sig.Label)
    (hUA : UniqueActions s) (hIC : IncreasingCount s)
    (hNew : newLbl.actionCount = s.actionCount) :
    UniqueActions ((s.addSent newLbl).bumpActionCount) := by
  unfold UniqueActions IncreasingCount at *
  pverify_smt

/-- The same shape, but for `IncreasingCount`. -/
example (s : GS) (newLbl : Sig.Label)
    (hIC : IncreasingCount s)
    (hNew : newLbl.actionCount = s.actionCount) :
    IncreasingCount ((s.addSent newLbl).bumpActionCount) := by
  unfold IncreasingCount at *
  pverify_smt

/-- And `ReceivedSubsetSent`. -/
example (s : GS) (newLbl : Sig.Label)
    (hRS : ReceivedSubsetSent s) :
    ReceivedSubsetSent ((s.addSent newLbl).bumpActionCount) := by
  unfold ReceivedSubsetSent at *
  pverify_smt

/-! ## The full bundle: `DefaultInvariants` preservation -/

example (s : GS) (newLbl : Sig.Label)
    (hDI : DefaultInvariants s)
    (hNew : newLbl.actionCount = s.actionCount) :
    DefaultInvariants ((s.addSent newLbl).bumpActionCount) := by
  unfold DefaultInvariants UniqueActions IncreasingCount
    ReceivedSubsetSent at *
  pverify_smt
