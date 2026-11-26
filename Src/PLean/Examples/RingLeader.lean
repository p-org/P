/-
PLean port of [`Tutorial/Advanced/3_RingLeaderVerification`](../../../Tutorial/Advanced/3_RingLeaderVerification/PSrc/System.p).

The two relational fragments are bundled into typeclasses and
instantiated with `pinstance` over `MachineRef`:
- `LeOrder` — total order `le` + 4 axioms.
- `RingTopology` — cyclic betweenness `btw`, successor `right`,
  10 joint axioms.

Each `pinstance` synthesises an axiomatic witness, an anonymous
`instance` for typeclass resolution, and one axiom per Prop-typed class
field; the axioms are handed to the solver as hints.

`x is Won` is a state check (distinct from the event/machine-kind
`is`); ported as `stateOf x s = Server.Won_st`.
-/
import PLean

open PLean Std.WP

class LeOrder (t : Type) where
  le          : t → t → Bool
  le_refl     : ∀ x : t, le x x = true
  le_symm     : ∀ x y : t, le x y = true ∨ le y x = true
  le_trans    : ∀ x y z : t, le x y = true → le y z = true → le x z = true
  le_antisymm : ∀ x y : t, le x y = true → le y x = true → x = y

/-- Ring topology bundle: `btw` (ternary cyclic betweenness), `right`
(clockwise successor), and the 8 joint axioms relating them.

`btw_1`..`btw_4` are the betweenness axioms (transitivity, asymmetry,
totality, rotation). `right_neq_self` / `btw_right` / `btw_Aux1` /
`right_btw` mix both names, so they share the class. -/
class RingTopology (t : Type) where
  btw            : t → t → t → Bool
  right          : t → t
  btw_1          : ∀ w x y z : t, btw w x y = true → btw w y z = true → btw w x z = true
  btw_2          : ∀ x y z : t, btw x y z = true → btw x z y = false
  btw_3          : ∀ x y z : t, btw x y z = true ∨ btw x z y = true ∨ x = y ∨ x = z ∨ y = z
  btw_4          : ∀ x y z : t, btw x y z = true → btw y z x = true
  right_neq_self : ∀ x : t, x ≠ right x
  btw_right      : ∀ x y : t, x ≠ y → y ≠ right x → btw x (right x) y = true
  btw_Aux1       : ∀ x y : t, btw x y (right x) = false
  right_btw      : ∀ x n m : t, m = right n → (btw n m x = true ∨ x = m ∨ x = n)

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

  -- The ring is made of Servers: a Server's successor is a Server (so the
  -- sends to it do not go to `null`).
  init-holds ∀ x : Server, is_Server (RingTopology.right x.ref) s

  Lemma ring_ok {
    invariant right_is_server : ∀ x : Server, is_Server (RingTopology.right x.ref) s
  }

  -- voteFor is the running max.
  Lemma lemmas {
    invariant LeaderMax :
      ∀ x y : MachineRef, stateOf x s = Server.Won_st → LeOrder.le y x = true

    invariant Aux :
      ∀ x y : MachineRef, LeOrder.le x y = true → LeOrder.le y x = true → x = y

    invariant NoBypass :
      ∀ (n m : MachineRef) (e : PLean.Msg E) (p : tNominate),
        inflight e s → (e targets m) → e.ev = E.eNominate p →
        RingTopology.btw p.voteFor n m = true → LeOrder.le n p.voteFor = true

    invariant SelfPendingMax :
      ∀ (n m : MachineRef) (e : PLean.Msg E) (p : tNominate),
        inflight e s → (e targets m) → e.ev = E.eNominate p →
        p.voteFor = m → LeOrder.le n m = true
  }
  Proof {
    prove ring_ok ;
    prove lemmas ;
  }

  -- Main theorem: at most one server reaches the `Won` state.
  Theorem Safety {
    invariant UniqueLeader :
      ∀ x y : MachineRef,
        stateOf x s = Server.Won_st → stateOf y s = Server.Won_st → x = y
  }
  Proof {
    prove Safety using lemmas ;
  }

end RingLeader

#gen_module RingLeader
#pwf        RingLeader

#pverify    RingLeader
