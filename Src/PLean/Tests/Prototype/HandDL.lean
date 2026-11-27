/-
Hand-written DistributedLock on the transition model (the P1 prototype):
the model, the safety invariant, every obligation of `step_ok` discharged
by `vcgen` + `crush` (checked proofs), and the final theorems.
-/
import PLean.Verify.Norm
import Crush

open PLean Std.WP

set_option experimental.vcgen true
set_option crush.backend "cvc5"
set_option crush.trust "reconstruct"
set_option maxHeartbeats 4000000
-- wall-clock seconds per solver call; the default (10) is too tight on a
-- slower machine (CI)
set_option crush.timeout 60

namespace HandDL

structure tGrant where
  node  : MachineRef
  epoch : Int
  deriving DecidableEq, Repr, Inhabited

structure tAccept where
  epoch  : Int
  source : MachineRef
  deriving DecidableEq, Repr, Inhabited

inductive E where
  | eGrant (p : tGrant)
  | eAccept (p : tAccept)
  deriving DecidableEq, Repr

inductive S where
  | Node_Act
  deriving DecidableEq, Repr

structure F where
  Node_epoch : Int := 0
  Node_held  : Bool := false
  deriving DecidableEq, Repr, Inhabited

abbrev HM := HandlerM S E F Unit Unit
abbrev GW := GWorld S E F Unit Unit

def Node.Act.eGrant (payload : tGrant) : HM Unit := do
  let hs ← get
  if hs.ms.fields.Node_held && decide (payload.epoch > hs.ms.fields.Node_epoch) then
    modify fun hs => { hs with ms := { hs.ms with fields := { hs.ms.fields with Node_held := false } } }
    H.send payload.node (.eAccept ⟨payload.epoch, hs.ref⟩)

def Node.Act.eAccept (payload : tAccept) : HM Unit := do
  let hs ← get
  if decide (payload.epoch > hs.ms.fields.Node_epoch) then
    modify fun hs => { hs with ms := { hs.ms with fields := { hs.ms.fields with Node_held := true } } }
    modify fun hs => { hs with ms := { hs.ms with fields := { hs.ms.fields with Node_epoch := payload.epoch } } }

def prog : Program S E F Unit Unit where
  handler s e := match s, e with
    | .Node_Act, .eGrant p => some (Node.Act.eGrant p)
    | .Node_Act, .eAccept p => some (Node.Act.eAccept p)
  entry _ _ := pure ()
  resume _ _ := none
  deferred _ _ := false

/-! ## Invariants -/

def isNode (g : GW) (r : MachineRef) : Prop := (g.machines r).state = some .Node_Act
def held (g : GW) (r : MachineRef) : Bool := (g.machines r).fields.Node_held
def epochOf (g : GW) (r : MachineRef) : Int := (g.machines r).fields.Node_epoch
def inflightAccept (g : GW) (i : Nat) (pl : tAccept) : Prop :=
  ∃ p, g.net i = some p ∧ p.ev = .eAccept pl

def unique_holder (g : GW) : Prop :=
  ∀ r1 r2, isNode g r1 → isNode g r2 → held g r1 = true → held g r2 = true → r1 = r2
def no_lock_while_transfer (g : GW) : Prop :=
  ∀ r i pl, isNode g r → inflightAccept g i pl → held g r = false
def unique_accept (g : GW) : Prop :=
  ∀ i1 i2 pl1 pl2, inflightAccept g i1 pl1 → inflightAccept g i2 pl2 → i1 = i2
def not_held_after_release (g : GW) : Prop :=
  ∀ r i pl, isNode g r → inflightAccept g i pl → pl.source = r → held g r = false
def transfer_to_higher (g : GW) : Prop :=
  ∀ r i pl, isNode g r → inflightAccept g i pl → pl.source = r → pl.epoch > epochOf g r
/-- An in-flight grant names a machine (a send to `null` fails). -/
def grant_target (g : GW) : Prop :=
  ∀ i p pl, g.net i = some p → p.ev = .eGrant pl → pl.node ≠ MachineRef.null
/-- Auto-invariant: the program never halts a machine. -/
def NoHalt (g : GW) : Prop := ∀ r, (g.machines r).halted = false

def Inv (g : GW) : Prop :=
  unique_holder g ∧ no_lock_while_transfer g ∧ unique_accept g ∧
  not_held_after_release g ∧ transfer_to_higher g ∧ grant_target g ∧ NoHalt g

def InitConditions (g : GW) : Prop :=
  g.clock = 0 ∧ (∀ j, g.net j = none) ∧ (∀ j, g.log j = none) ∧ g.error = none ∧
  (∀ r, g.nextRef ≤ r → (g.machines r).state = none) ∧
  (0 < g.nextRef ∧ (g.machines MachineRef.null).state = none) ∧
  (∀ r, (g.machines r).halted = false) ∧
  (∀ r, r ≠ MachineRef.null → r < g.nextRef → isNode g r) ∧
  (∃ r, isNode g r ∧ held g r = true ∧ epochOf g r > 0 ∧
     ∀ r1, isNode g r1 → r1 ≠ r → held g r1 = false ∧ epochOf g r1 = 0)

@[pverifySimp] theorem F_held_ite {c : Prop} [Decidable c] (a b : F) :
    (if c then a else b).Node_held = if c then a.Node_held else b.Node_held := by split <;> rfl
@[pverifySimp] theorem F_epoch_ite {c : Prop} [Decidable c] (a b : F) :
    (if c then a else b).Node_epoch = if c then a.Node_epoch else b.Node_epoch := by split <;> rfl

/-- The obligation tactic: VCs, normalise, split the invariant hypotheses,
one `crush` per conjunct. -/
macro "dl_close" : tactic => `(tactic| (
  vcgen
  all_goals subst_vars
  all_goals simp only [pverifySimp, Inv, unique_holder, no_lock_while_transfer, unique_accept,
    not_held_after_release, transfer_to_higher, grant_target, NoHalt, isNode, held, epochOf,
    inflightAccept] at *
  all_goals pv_split_ands
  all_goals and_intros
  all_goals crush [*]))

/-! ## Obligations -/

theorem eGrant_ok (pl : tGrant) (g : GW) (i : Nat) (p : Packet E)
    (hF : Framework g) (hInv : Inv g) (hnet : g.net i = some p) (hev : p.ev = .eGrant pl)
    (hst : (g.machines p.dst).state = some .Node_Act) :
    RunOK Inv (g.consume i) p.dst (g.machines p.dst) (Node.Act.eGrant pl) := by
  intro coins; unfold Node.Act.eGrant; dl_close

theorem eAccept_ok (pl : tAccept) (g : GW) (i : Nat) (p : Packet E)
    (hF : Framework g) (hInv : Inv g) (hnet : g.net i = some p) (hev : p.ev = .eAccept pl)
    (hst : (g.machines p.dst).state = some .Node_Act) :
    RunOK Inv (g.consume i) p.dst (g.machines p.dst) (Node.Act.eAccept pl) := by
  intro coins; unfold Node.Act.eAccept; dl_close

theorem entry_ok (g : GW) (m : MachineRef) (arg : Unit) (hF : Framework g) (hInv : Inv g)
    (hst : (g.machines m).state = some .Node_Act) :
    RunOK Inv g m { g.machines m with pending := none } (prog.entry .Node_Act arg) := by
  intro coins; simp only [prog]; dl_close

theorem base_ok (g : GW) (h : InitConditions g) : Framework g ∧ g.error = none ∧ Inv g := by
  obtain ⟨_, hn, hl, he, hu, ⟨hpos, hnull⟩, hh, _, hinit⟩ := h
  refine ⟨⟨fun j _ => hn j, fun j _ => hl j, fun j p h => by simp [hn] at h, hu, hpos, hnull⟩,
    he, ?_⟩
  simp only [Inv, unique_holder, no_lock_while_transfer, unique_accept, not_held_after_release,
    transfer_to_higher, grant_target, NoHalt, inflightAccept, hn] at *
  and_intros <;> crush [*]

/-! ## Assembly -/

theorem step_preserves :
    ∀ g c coins, Framework g → g.error = none → Inv g → prog.enabled .bag g.toWorld c = true →
      (gstep prog g c coins).error = none ∧ Inv (gstep prog g c coins) := by
  apply step_ok prog Inv Inv
  · intro g i p s hF _ hInv hnet hs _ _ _ _
    obtain ⟨src, dst, ev⟩ := p
    cases s; cases ev with
    | eGrant pl => exact ⟨_, rfl, eGrant_ok pl g i _ hF hInv hnet rfl hs⟩
    | eAccept pl => exact ⟨_, rfl, eAccept_ok pl g i _ hF hInv hnet rfl hs⟩
  · intro g i p k _ _ _ _ _ _ _ act hr; simp [prog] at hr
  · intro g m s arg hF _ hInv hs _ _; cases s; exact entry_ok g m arg hF hInv hs
  · intro g i p _ _ hInv _ hh; have := hInv.2.2.2.2.2.2 p.dst; simp [this] at hh

/-- Safety on every reachable world (bag semantics). -/
theorem safe : ∀ g, GReachable prog .bag InitConditions g → Inv g := fun g hg =>
  (safety_of_step prog InitConditions Inv base_ok step_preserves g hg).2.2

/-- …and on every run under P's FIFO semantics. -/
theorem safe_fifo : ∀ g, GReachable prog .fifo InitConditions g → Inv g := fun g hg =>
  safe g hg.fifo_bag

/-- info: 'HandDL.safe_fifo' depends on axioms: [propext, Classical.choice, Quot.sound] -/
#guard_msgs in #print axioms safe_fifo

end HandDL
