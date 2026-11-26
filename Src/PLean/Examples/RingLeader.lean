/-
PLean port of [`Tutorial/Advanced/3_RingLeaderVerification`](../../../Tutorial/Advanced/3_RingLeaderVerification/PSrc/System.p).

The order and the ring are typeclasses, instantiated with `pinstance` over
`MachineRef`; their laws are handed to the solver as hints:
- `LeOrder`: a total order `le`.
- `RingTopology`: the ring's members `mem`, a cyclic betweenness `btw`, and
  the successor `right` of a member.

P's `forall (x: machine)` ranges over the program's machines, which are all
Servers. `MachineRef` is all of ℕ, and no finite set of references is
closed under a successor that is immediate in a cyclic order of all of ℕ.
So the successor laws are stated for the ring's members, the Servers are
exactly the members (`servers_are_ring`), and the invariants quantify over
members. A finite ring of two or more Servers satisfies the laws.

`x is Won` is a state check, ported as `stateOf x s = Server.Won_st`.
-/
import PLean

open PLean Std.WP

class LeOrder (t : Type) where
  le          : t → t → Bool
  le_refl     : ∀ x : t, le x x = true
  le_symm     : ∀ x y : t, le x y = true ∨ le y x = true
  le_trans    : ∀ x y z : t, le x y = true → le y z = true → le x z = true
  le_antisymm : ∀ x y : t, le x y = true → le y x = true → x = y

/-- The ring: its members `mem`, the cyclic betweenness `btw` (transitivity,
asymmetry, totality, rotation), and the successor `right` of a member (the
next member in the cyclic order). -/
class RingTopology (t : Type) where
  mem            : t → Bool
  btw            : t → t → t → Bool
  right          : t → t
  btw_1          : ∀ w x y z : t, btw w x y = true → btw w y z = true → btw w x z = true
  btw_2          : ∀ x y z : t, btw x y z = true → btw x z y = false
  btw_3          : ∀ x y z : t, btw x y z = true ∨ btw x z y = true ∨ x = y ∨ x = z ∨ y = z
  btw_4          : ∀ x y z : t, btw x y z = true → btw y z x = true
  right_mem      : ∀ x : t, mem x = true → mem (right x) = true
  right_neq_self : ∀ x : t, mem x = true → x ≠ right x
  btw_right      : ∀ x y : t, mem x = true → mem y = true → x ≠ y → y ≠ right x →
                     btw x (right x) y = true
  btw_Aux1       : ∀ x y : t, mem x = true → mem y = true → btw x y (right x) = false
  right_btw      : ∀ x n m : t, mem x = true → mem n = true → m = right n →
                     (btw n m x = true ∨ x = m ∨ x = n)

pmodule RingLeader

  system s

  type tNominate = (voteFor : PLean.MachineRef)

  event eNominate : tNominate

  -- Total order on machine ids (the running-max comparison).
  pinstance order : LeOrder PLean.MachineRef

  -- Ring topology: cyclic betweenness + clockwise successor.
  pinstance ring : RingTopology PLean.MachineRef

  machine Server {
    start state Proposing {
      entry {
        send (RingTopology.right this.ref), eNominate, (voteFor = this.ref)
      }

      on eNominate (n : tNominate) {
        if n.voteFor = this.ref then do
          goto Won
        else if LeOrder.le this.ref n.voteFor then do
          send (RingTopology.right this.ref), eNominate, (voteFor = n.voteFor)
        else do
          send (RingTopology.right this.ref), eNominate, (voteFor = this.ref)
      }
    }

    state Won {
      ignore eNominate
    }
  }

  -- The Servers are the ring's members.
  init-holds ∀ r : PLean.MachineRef, is_Server r s ↔ RingTopology.mem r = true

  Lemma ring_ok {
    invariant servers_are_ring :
      ∀ r : PLean.MachineRef, is_Server r s ↔ RingTopology.mem r = true

    -- Votes go round the ring, and are for members.
    invariant nominate_in_ring :
      ∀ e : eNominate, inflight e s →
        RingTopology.mem e.target = true ∧ RingTopology.mem e.voteFor = true
  }

  -- voteFor is the running max.
  Lemma lemmas {
    invariant LeaderMax :
      ∀ x y : MachineRef, stateOf x s = Server.Won_st → RingTopology.mem y = true →
        LeOrder.le y x = true

    invariant Aux :
      ∀ x y : MachineRef, LeOrder.le x y = true → LeOrder.le y x = true → x = y

    invariant NoBypass :
      ∀ (n m : MachineRef) (e : PLean.Msg E) (p : tNominate),
        inflight e s → (e targets m) → e.ev = E.eNominate p → RingTopology.mem n = true →
        RingTopology.btw p.voteFor n m = true → LeOrder.le n p.voteFor = true

    invariant SelfPendingMax :
      ∀ (n m : MachineRef) (e : PLean.Msg E) (p : tNominate),
        inflight e s → (e targets m) → e.ev = E.eNominate p → RingTopology.mem n = true →
        p.voteFor = m → LeOrder.le n m = true
  }
  Proof {
    prove ring_ok ;
    prove lemmas using ring_ok ;
  }

  -- Main theorem: at most one server reaches the `Won` state.
  Theorem Safety {
    invariant UniqueLeader :
      ∀ x y : MachineRef,
        stateOf x s = Server.Won_st → stateOf y s = Server.Won_st → x = y
  }
  Proof {
    prove Safety using lemmas, ring_ok ;
  }

end RingLeader

#gen_module RingLeader
#pwf        RingLeader

#pverify    RingLeader

/-! The laws have a model with a ring of three Servers, `1 → 2 → 3 → 1`:
`<` on ℕ read cyclically, and the next member. So the theorems are not
only about the empty system. -/

/-- `y` is strictly between `x` and `z` going round `<` cyclically. -/
def cyclicBtw (x y z : Nat) : Bool :=
  decide ((x < y ∧ y < z) ∨ (y < z ∧ z < x) ∨ (z < x ∧ x < y))

example : LeOrder Nat where
  le x y := decide (x ≤ y)
  le_refl := by intro x; simp
  le_symm := by intro x y; simp; omega
  le_trans := by intro x y z; simp; omega
  le_antisymm := by intro x y; simp; omega

example : RingTopology Nat where
  mem x := decide (1 ≤ x ∧ x ≤ 3)
  btw := cyclicBtw
  right x := if x = 3 then 1 else x + 1
  btw_1 := by intro w x y z; simp [cyclicBtw]; omega
  btw_2 := by intro x y z; simp [cyclicBtw]; omega
  btw_3 := by intro x y z; simp [cyclicBtw]; omega
  btw_4 := by intro x y z; simp [cyclicBtw]; omega
  right_mem := by intro x; simp; split <;> omega
  right_neq_self := by intro x; simp; split <;> omega
  btw_right := by
    intro x y hx hy; simp at hx hy
    rcases (by omega : x = 1 ∨ x = 2 ∨ x = 3) with rfl | rfl | rfl <;>
      rcases (by omega : y = 1 ∨ y = 2 ∨ y = 3) with rfl | rfl | rfl <;> decide
  btw_Aux1 := by
    intro x y hx hy; simp at hx hy
    rcases (by omega : x = 1 ∨ x = 2 ∨ x = 3) with rfl | rfl | rfl <;>
      rcases (by omega : y = 1 ∨ y = 2 ∨ y = 3) with rfl | rfl | rfl <;> decide
  right_btw := by
    intro x n m hx hn hm; simp at hx hn; subst hm
    rcases (by omega : x = 1 ∨ x = 2 ∨ x = 3) with rfl | rfl | rfl <;>
      rcases (by omega : n = 1 ∨ n = 2 ∨ n = 3) with rfl | rfl | rfl <;> decide
