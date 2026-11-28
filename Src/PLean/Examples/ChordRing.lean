/-
ChordRing: DuoAI's Ivy model `chord_ring_maintenance.ivy`, the ring maintenance of the
Chord distributed hash table.

The nodes sit on a ring ordered by `btw`. An active node keeps a first successor `s1`, a
second successor `s2` (its successor's successor, for when `s1` fails) and a predecessor `p`.
The ring starts as `org ⇄ other`. A node joins with an active successor, provided `org` is not
between them; stabilizes by adopting its successor's predecessor when that one sits between
them; notifies its successor, which may take it as predecessor; inherits its successor's
successor as `s2`; and removes a dead successor by falling back on `s2`. A node other than
`org` may fail unless it is some node's last live successor.

Shape. Each Ivy action changes one node but reads two or three, and `fail`'s requires range
over every node. Split into requests and replies, the actions read stale state and safety
fails (e.g. a node activates on a stale join ack whose successor has since failed);
recovering atomicity would need locks and a snapshot of every node, which Ivy does not have.
So one `Ring` machine holds Ivy's state and applies each action atomically, under Ivy's
requires, when a Node asks for it: on each tick a Node requests one of the nine actions,
naming nodes picked from `nodes`, and the Ring ignores a request whose requires fail. Every
Ivy step can thus be taken whenever it is enabled, and the state changes only by Ivy steps;
what differs is that ring maintenance is centralised.

Mapping: `a(X)` is `X ∈ active`; `s1`, `s2`, `p` are maps (`in_s1`, `in_s2` their domains);
the ghosts `reach` and `error` are Ring fields no protocol guard reads; `ring.btw` and its
axioms are the class `RingBtw` (`pinstance topology`); `org` and `other` are functions with
`paxiom`s saying they are distinct nodes. `nodes` is exactly the set of Node machines, so
requires quantified over it are Ivy's quantifiers over the node sort. Initially the Ring
holds Ivy's `after init`.

Safety is Ivy's `~error(N)` (`no_error`). Call `y` the live next hop of `x` when `s1(x) = y`
is active, or when `s1(x)` is dead and `s2(x) = y` is active. `reach` starts as `{org}`;
the ghost actions `reach_org` / `remove_org` add / remove a node whose live next hop is / is
not in `reach`; `test(x)` sets `error(x)` when `reach` is closed backwards along live next
hops, every active node strictly between `x` and `org` is in `reach`, and `x` is active and
not in `reach`. Over reachable states, `~error` thus says that every active node reaches
`org` along live next hops: if one does not, `reach_org` steps make `reach` the set of nodes
that do, and `test` fires at the one that does not and is closest to `org`. The proof shows a
stronger inductive fact: every active node has a live next hop, and it is `org` or between
the node and `org`.

Deviations from the Ivy model, besides the shape:
- `s1`, `s2`, `p` are maps: every Ivy action keeps the relations functional. `test`'s
  requires `in_s1(x) -> exists Y. s1(x, Y)` and `in_s2(x) -> exists Y. s2(x, Y)` then
  always hold, and are left out.
- There are finitely many nodes. An Ivy run reaching `error` touches finitely many nodes,
  and the same run on those alone reaches it too (the axioms are universal).
- Each firing of an action takes a request, which a Node sends on every tick.
- `RingBtw`'s laws hold for every reference, not only nodes. That restricts nothing: on
  finitely many nodes Ivy's axioms make `btw` a cyclic order, which extends to all
  references. `natRing` (at the end) shows the laws are satisfiable.

Proof: small lemmas with short `using` lists (`routing`, `table`, `no_jump`, `live_next`).
Three steps are proved by hand, since the solver's proofs of them do not replay: `inherit`
for `no_jump` (via `RingBtw.btw_chain`), `fail` for `live_next` (via `fail_key`), and `test`
for the theorem (the live next hop of `x`, found by hand).
-/
import PLean

open PLean

/-- Ivy's `ring_topology`: `btw x y z` says that `y` is on the path from `x` to `z` going
round the ring (`btw x y x` for every `y ≠ x`). The laws are Ivy's four axioms. -/
class RingBtw (t : Type) where
  btw         : t → t → t → Bool
  btw_trans   : ∀ w x y z : t, btw w x y = true → btw w y z = true → btw w x z = true
  btw_acyclic : ∀ w x y : t, btw w x y = true → btw w y x = false
  btw_total   : ∀ w x y : t, btw w x y = true ∨ btw w y x = true ∨ w = x ∨ x = y
  btw_cyclic  : ∀ x y z : t, btw x y z = true → x ≠ z → btw y z x = true


namespace RingBtw
variable {t : Type} [RingBtw t]

theorem btw_irr_right (w x : t) : btw w x x = false := by
  have := btw_acyclic w x x
  cases h : btw w x x <;> simp_all

theorem btw_irr_left (x z : t) : btw x x z = false := by
  cases h : btw x x z
  · rfl
  · exfalso
    by_cases hxz : x = z
    · subst hxz; have := btw_irr_right x x; simp_all
    · have h1 := btw_cyclic x x z h hxz
      have h2 := btw_acyclic x x z h
      simp_all

/-- `y` on the way from `x` to `o`, and `z` on the way from `y` to `o`: then `z` is on the way
from `x` to `o`. (So `inherit`'s `s2(x) = s1(s1(x))` does not jump over `org`.) -/
theorem btw_chain (x y z o : t) (h1 : btw x y o = true) (h2 : btw y z o = true) :
    btw x z o = true := by
  have hzo : z ≠ o := by rintro rfl; simp [btw_irr_right] at h2
  have hyo : y ≠ o := by rintro rfl; simp [btw_irr_right] at h1
  have hyz : y ≠ z := by rintro rfl; simp [btw_irr_left] at h2
  by_cases hxo : x = o
  · subst hxo
    rcases btw_total x z x with h | h | h | h
    · exact h
    · simp [btw_irr_left] at h
    · exact absurd h.symm hzo
    · exact absurd h hzo
  · have hxy : x ≠ y := by rintro rfl; simp [btw_irr_left] at h1
    rcases btw_total x z o with h | h | h | h
    · exact h
    · exfalso
      have hxz : x ≠ z := by
        rintro rfl
        have a1 := btw_cyclic _ _ _ h1 hxo   -- btw y o x
        have := btw_acyclic _ _ _ h2
        simp_all
      have a1 := btw_cyclic _ _ _ (btw_cyclic _ _ _ h1 hxo) (Ne.symm hxy)   -- btw o x y
      have a2 := btw_cyclic _ _ _ (btw_cyclic _ _ _ h2 hyo) (Ne.symm hyz)   -- btw o y z
      have a3 := btw_trans _ _ _ _ a1 a2                                    -- btw o x z
      have a4 := btw_cyclic _ _ _ h hxz                                     -- btw o z x
      have := btw_acyclic _ _ _ a3
      simp_all
    · exfalso; subst h
      have a1 := btw_cyclic _ _ _ h1 hxo   -- btw y o x
      have a2 := btw_trans _ _ _ _ h2 a1   -- btw y x x
      simp [btw_irr_right] at a2
    · exact absurd h hzo

end RingBtw

pmodule ChordRing

  system s

  type tJoin = (x : PLean.MachineRef, y : PLean.MachineRef)
  type tStep = (x : PLean.MachineRef, y : PLean.MachineRef, z : PLean.MachineRef)

  event eTick
  event eJoin      : tJoin
  event eStabilize : tStep
  event eNotify    : tStep
  event eInherit   : tStep
  event eRemove    : tStep
  event eFail      : PLean.MachineRef
  event eReachOrg  : tStep
  event eRemoveOrg : tStep
  event eTest      : PLean.MachineRef

  pinstance topology : RingBtw PLean.MachineRef

  -- Ivy's `node` sort: the Node machines.
  function nodes : set[PLean.MachineRef]
  function org   : PLean.MachineRef
  function other : PLean.MachineRef
  -- The Ring machine the Nodes send their requests to.
  function ring  : PLean.MachineRef

  paxiom org_node     : org ∈ nodes
  paxiom other_node   : other ∈ nodes
  paxiom other_ne_org : other ≠ org

  machine Ring {
    var active : set[PLean.MachineRef]
    var s1     : map[PLean.MachineRef, PLean.MachineRef]
    var s2     : map[PLean.MachineRef, PLean.MachineRef]
    var p      : map[PLean.MachineRef, PLean.MachineRef]
    -- Ghost: written by the ghost requests only, read by no protocol guard.
    var reach  : set[PLean.MachineRef]
    var error  : set[PLean.MachineRef]

    start state Maintain {
      on eJoin (req : tJoin) {
        if ¬ (req.x ∈ active) ∧ req.y ∈ active ∧ RingBtw.btw req.x org req.y = false then do
          active += (req.x)
          s1[req.x] = req.y
          s2 -= (req.x)
          p -= (req.x)
      }

      on eStabilize (req : tStep) {
        if req.x ∈ active ∧ s1[req.x]? = some req.y ∧ req.y ∈ active ∧
            p[req.y]? = some req.z ∧ RingBtw.btw req.x req.z req.y = true then do
          s1[req.x] = req.z
          s2[req.x] = req.y
      }

      on eNotify (req : tStep) {
        if req.x ∈ active ∧ s1[req.x]? = some req.y ∧ req.y ∈ active ∧
            (p[req.y]? = some req.z ∨ ¬ (req.y ∈ p)) ∧
            RingBtw.btw req.z req.x req.y = true then do
          p[req.y] = req.x
      }

      on eInherit (req : tStep) {
        if req.x ∈ active ∧ s1[req.x]? = some req.y ∧ req.y ∈ active ∧
            s1[req.y]? = some req.z then do
          s2[req.x] = req.z
      }

      on eRemove (req : tStep) {
        if req.x ∈ active ∧ s1[req.x]? = some req.y ∧ ¬ (req.y ∈ active) ∧
            s2[req.x]? = some req.z then do
          s1[req.x] = req.z
          s2 -= (req.x)
      }

      on eFail (x : PLean.MachineRef) {
        if x ∈ active ∧ x ≠ org ∧
            (∀ y, y ∈ nodes → s1[y]? = some x → y ∈ s2) ∧
            (∀ y, y ∈ nodes → ∀ z, z ∈ nodes →
              s1[y]? = some x → s2[y]? = some z → z ∈ active) ∧
            (∀ w, w ∈ nodes → ∀ y, y ∈ nodes →
              s1[w]? = some y → s2[w]? = some x → y ≠ x ∧ y ∈ active) then do
          active -= (x)
          p -= (x)
          s1 -= (x)
          s2 -= (x)
      }

      on eReachOrg (req : tStep) {
        if (s1[req.x]? = some req.y ∧ req.y ∈ active ∧ req.y ∈ reach) ∨
            (s1[req.x]? = some req.y ∧ ¬ (req.y ∈ active) ∧ s2[req.x]? = some req.z ∧
              req.z ∈ active ∧ req.z ∈ reach) then do
          reach += (req.x)
      }

      on eRemoveOrg (req : tStep) {
        if req.x ≠ org ∧ s1[req.x]? = some req.y ∧
            (¬ (req.y ∈ active) ∨ ¬ (req.y ∈ reach)) ∧
            (¬ (req.y ∈ active) → s2[req.x]? = some req.z ∨ ¬ (req.x ∈ s2)) ∧
            (¬ (req.y ∈ active) ∧ s2[req.x]? = some req.z →
              ¬ (req.z ∈ active) ∨ ¬ (req.z ∈ reach)) then do
          reach -= (req.x)
      }

      on eTest (x : PLean.MachineRef) {
        if (∀ w, w ∈ nodes → ∀ y, y ∈ nodes →
              s1[w]? = some y ∧ y ∈ active ∧ y ∈ reach → w ∈ reach) ∧
            (∀ w, w ∈ nodes → ∀ y, y ∈ nodes → ∀ z, z ∈ nodes →
              s1[w]? = some y ∧ ¬ (y ∈ active) ∧ s2[w]? = some z ∧ z ∈ active ∧ z ∈ reach →
              w ∈ reach) ∧
            (∀ y, y ∈ nodes → RingBtw.btw x y org = true ∧ y ∈ active → y ∈ reach) ∧
            x ∈ active ∧ ¬ (x ∈ reach) then do
          error += (x)
      }
    }
  }

  machine Node {
    start state Run {
      entry {
        send this.ref, eTick
      }

      on eTick {
        if nodes.card > 0 then do
          let k ← choose (8 : Int)
          let y ← choose nodes
          let z ← choose nodes
          if k = 0 then do
            send ring, eJoin, (x = this.ref, y = y)
          else if k = 1 then do
            send ring, eStabilize, (x = this.ref, y = y, z = z)
          else if k = 2 then do
            send ring, eNotify, (x = this.ref, y = y, z = z)
          else if k = 3 then do
            send ring, eInherit, (x = this.ref, y = y, z = z)
          else if k = 4 then do
            send ring, eRemove, (x = this.ref, y = y, z = z)
          else if k = 5 then do
            send ring, eFail, this.ref
          else if k = 6 then do
            send ring, eReachOrg, (x = this.ref, y = y, z = z)
          else if k = 7 then do
            send ring, eRemoveOrg, (x = this.ref, y = y, z = z)
          else do
            send ring, eTest, this.ref
        send this.ref, eTick
      }
    }
  }

  init-holds ∀ r : PLean.MachineRef, r ∈ nodes ↔ is_Node r s
  init-holds is_Ring ring s
  -- Ivy's `after init`: the ring org ⇄ other; org reaches org.
  init-holds ∀ (r : Ring) (x : PLean.MachineRef), x ∈ r.active ↔ x = org ∨ x = other
  init-holds ∀ (r : Ring) (x y : PLean.MachineRef),
    r.s1[x]? = some y ↔ (x = org ∧ y = other) ∨ (x = other ∧ y = org)
  init-holds ∀ (r : Ring) (x : PLean.MachineRef), x ∈ r.s1 ↔ x = org ∨ x = other
  init-holds ∀ (r : Ring) (x : PLean.MachineRef), ¬ (x ∈ r.s2)
  init-holds ∀ (r : Ring) (x y : PLean.MachineRef),
    r.p[x]? = some y ↔ (x = org ∧ y = other) ∨ (x = other ∧ y = org)
  init-holds ∀ (r : Ring) (x : PLean.MachineRef), x ∈ r.reach ↔ x = org
  init-holds ∀ (r : Ring) (x : PLean.MachineRef), ¬ (x ∈ r.error)

  -- Where each event goes, and that a request names nodes.
  Lemma routing {
    invariant ring_kind : is_Ring ring s
    invariant node_in_nodes : ∀ n : Node, n.ref ∈ nodes
    invariant tick_to_node : ∀ e : eTick, sent e s → is_Node e.target s
    invariant join_to_ring : ∀ e : eJoin, sent e s → e.target = ring
    invariant stabilize_to_ring : ∀ e : eStabilize, sent e s → e.target = ring
    invariant notify_to_ring : ∀ e : eNotify, sent e s → e.target = ring
    invariant inherit_to_ring : ∀ e : eInherit, sent e s → e.target = ring
    invariant remove_to_ring : ∀ e : eRemove, sent e s → e.target = ring
    invariant fail_to_ring : ∀ e : eFail, sent e s → e.target = ring
    invariant reach_org_to_ring : ∀ e : eReachOrg, sent e s → e.target = ring
    invariant remove_org_to_ring : ∀ e : eRemoveOrg, sent e s → e.target = ring
    invariant test_to_ring : ∀ e : eTest, sent e s → e.target = ring
    invariant join_node : ∀ e : eJoin, sent e s → e.x ∈ nodes
    invariant stabilize_node : ∀ e : eStabilize, sent e s → e.z ∈ nodes
  }

  -- `org` stays active and in `reach`; the table holds only nodes; every active node has an `s1`.
  Lemma table {
    invariant org_active : ∀ r : Ring, org ∈ r.active
    invariant org_reach : ∀ r : Ring, org ∈ r.reach
    invariant active_nodes : ∀ (r : Ring) (x : PLean.MachineRef), x ∈ r.active → x ∈ nodes
    invariant s1_nodes :
      ∀ (r : Ring) (x y : PLean.MachineRef), r.s1[x]? = some y → y ∈ nodes
    invariant s2_nodes :
      ∀ (r : Ring) (x y : PLean.MachineRef), r.s2[x]? = some y → y ∈ nodes
    invariant s1_dom : ∀ (r : Ring) (x : PLean.MachineRef), x ∈ r.active → x ∈ r.s1
  }

  -- `s1` never jumps over `org`, and `s2` does not either unless `s1` is `org`.
  Lemma no_jump {
    invariant s1_no_jump :
      ∀ (r : Ring) (x y : PLean.MachineRef), r.s1[x]? = some y → y ≠ org →
        RingBtw.btw x y org = true
    invariant s2_no_jump :
      ∀ (r : Ring) (x y z : PLean.MachineRef), r.s1[x]? = some y → y ≠ org →
        r.s2[x]? = some z → z ≠ org → RingBtw.btw x z org = true
  }

  -- An active node whose `s1` is dead has an `s2`, and it is active.
  Lemma live_next {
    invariant s2_present :
      ∀ (r : Ring) (x y : PLean.MachineRef), x ∈ r.active → r.s1[x]? = some y →
        ¬ (y ∈ r.active) → x ∈ r.s2
    invariant s2_live :
      ∀ (r : Ring) (x y z : PLean.MachineRef), x ∈ r.active → r.s1[x]? = some y →
        ¬ (y ∈ r.active) → r.s2[x]? = some z → z ∈ r.active
  }

  Theorem Safety {
    -- Ivy's `invariant [1000000] ~error(N)`.
    invariant no_error : ∀ (r : Ring) (n : PLean.MachineRef), ¬ (n ∈ r.error)
  }

  Proof {
    prove routing ;
    prove table using routing ;
    prove no_jump using table ;
    prove live_next using table ;
    prove Safety using table, no_jump, live_next ;
  }

end ChordRing

#gen_module ChordRing
#pwf        ChordRing

namespace ChordRing
open PLean Std.WP

/-! Manual proofs: each step gets the facts it needs as hypotheses or a key lemma, and
`pverify_auto` does the rest. -/

-- `inherit(x, y, z)`: `s2(x) := z` with `y = s1(x)` and `z = s1(y)`, neither jumping over `org`.
@[pverifyProof] theorem Ring.Maintain.eInherit_correct_no_jump : ∀ (g : GW) (i : Nat) (src dst : MachineRef) (pl : eInherit_payload),
  Framework g →
    g.error = none →
      no_jump g ∧ table g →
        g.net i = some { src := src, dst := dst, ev := E.eInherit pl } →
          g.log i = some { src := src, dst := dst, ev := E.eInherit pl } →
            i < g.clock →
              (g.machines dst).state = some S.Ring_Maintain →
                (g.machines dst).pending = none →
                  (g.machines dst).waiting = none →
                    (g.machines dst).halted = false →
                      RunPost (fun g' ↦ PostInv "s1_no_jump" (s1_no_jump g') ∧ PostInv "s2_no_jump" (s2_no_jump g'))
                        (GWorld.consume g i) dst (g.machines dst) (Ring.Maintain.eInherit_handler pl) := by
  intro g i src dst pl hF hE hInv hnet hlog hi hst hpend hwait hhalt
  have key : RingBtw.btw pl.x pl.y org = true → RingBtw.btw pl.y pl.z org = true →
      RingBtw.btw pl.x pl.z org = true := RingBtw.btw_chain _ _ _ _
  pverify_auto

/-- When `fail(x)`'s requires hold, a node that stays active and whose first successor is dead
afterwards keeps a second successor, which is active afterwards. -/
theorem fail_key (A : PSet MachineRef) (s1 s2 : PMap MachineRef MachineRef) (x : MachineRef)
    (hAn : ∀ w : MachineRef, w ∈ A → w ∈ nodes)
    (h1n : ∀ w y : MachineRef, s1[w]? = some y → y ∈ nodes)
    (h2n : ∀ w y : MachineRef, s2[w]? = some y → y ∈ nodes)
    (hp2 : ∀ w y : MachineRef, w ∈ A → s1[w]? = some y → ¬ y ∈ A → w ∈ s2)
    (hl2 : ∀ w y z : MachineRef, w ∈ A → s1[w]? = some y → ¬ y ∈ A → s2[w]? = some z → z ∈ A)
    (g1 : ∀ y : MachineRef, y ∈ nodes → s1[y]? = some x → y ∈ s2)
    (g2 : ∀ y : MachineRef, y ∈ nodes → ∀ z : MachineRef, z ∈ nodes → s1[y]? = some x →
      s2[y]? = some z → z ∈ A)
    (g3 : ∀ w : MachineRef, w ∈ nodes → ∀ y : MachineRef, y ∈ nodes → s1[w]? = some y →
      s2[w]? = some x → y ≠ x ∧ y ∈ A)
    (w y : MachineRef) (hw : w ∈ A) (hwx : w ≠ x) (hy : s1[w]? = some y)
    (hyd : y = x ∨ ¬ y ∈ A) :
    w ∈ s2 ∧ ∀ z : MachineRef, s2[w]? = some z → z ∈ A ∧ z ≠ x := by
  have hwn := hAn w hw
  have hyn := h1n w y hy
  rcases hyd with rfl | hyA
  · refine ⟨g1 w hwn hy, fun z hz => ⟨g2 w hwn z (h2n w z hz) hy hz, ?_⟩⟩
    rintro rfl
    exact (g3 w hwn _ hyn hy hz).1 rfl
  · refine ⟨hp2 w y hw hy hyA, fun z hz => ⟨hl2 w y z hw hy hyA hz, ?_⟩⟩
    rintro rfl
    exact hyA (g3 w hwn y hyn hy hz).2

@[pverifyProof] theorem Ring.Maintain.eFail_correct_live_next : ∀ (g : GW) (i : Nat) (src dst : MachineRef) (pl : eFail_payload),
  Framework g →
    g.error = none →
      live_next g ∧ table g →
        g.net i = some { src := src, dst := dst, ev := E.eFail pl } →
          g.log i = some { src := src, dst := dst, ev := E.eFail pl } →
            i < g.clock →
              (g.machines dst).state = some S.Ring_Maintain →
                (g.machines dst).pending = none →
                  (g.machines dst).waiting = none →
                    (g.machines dst).halted = false →
                      RunPost (fun g' ↦ PostInv "s2_present" (s2_present g') ∧ PostInv "s2_live" (s2_live g'))
                        (GWorld.consume g i) dst (g.machines dst) (Ring.Maintain.eFail_handler pl) := by
  intro g i src dst pl hF hE hInv hnet hlog hi hst hpend hwait hhalt
  obtain ⟨⟨hp2, hl2⟩, ⟨-, -, hAn, h1n, h2n, -⟩⟩ := hInv
  by_cases hg : (∀ y : MachineRef, y ∈ nodes → (g.machines dst).fields.Ring_s1[y]? = some pl →
        y ∈ (g.machines dst).fields.Ring_s2) ∧
      (∀ y : MachineRef, y ∈ nodes → ∀ z : MachineRef, z ∈ nodes →
        (g.machines dst).fields.Ring_s1[y]? = some pl → (g.machines dst).fields.Ring_s2[y]? = some z →
        z ∈ (g.machines dst).fields.Ring_active) ∧
      (∀ w : MachineRef, w ∈ nodes → ∀ y : MachineRef, y ∈ nodes →
        (g.machines dst).fields.Ring_s1[w]? = some y → (g.machines dst).fields.Ring_s2[w]? = some pl →
        y ≠ pl ∧ y ∈ (g.machines dst).fields.Ring_active)
  · obtain ⟨g1, g2, g3⟩ := hg
    have key := fail_key _ _ _ pl (hAn ⟨dst⟩ hst) (h1n ⟨dst⟩ hst) (h2n ⟨dst⟩ hst)
      (hp2 ⟨dst⟩ hst) (hl2 ⟨dst⟩ hst) g1 g2 g3
    clear g1 g2 g3 hAn h1n h2n
    pverify_auto
  · -- the requires fail: a node witnesses it, and the handler does nothing
    simp only [Classical.not_and_iff_not_or_not, Classical.not_forall] at hg
    clear hAn h1n h2n
    rcases hg with ⟨y, hyn, hy, hs2⟩ | ⟨y, hyn, z, hzn, hy, hz, hzA⟩ |
      ⟨w, hwn, y, hyn, hy, hz, hbad⟩
    all_goals pverify_auto

-- `test(x)`: `x`'s live next hop is `org` or an active node between `x` and `org`, so test's
-- requires put it, and then `x`, in `reach`. The solver instantiates the requires at the hop.
@[pverifyProof] theorem Ring.Maintain.eTest_correct_Safety : ∀ (g : GW) (i : Nat) (src dst : MachineRef) (pl : eTest_payload),
  Framework g →
    g.error = none →
      Safety g ∧ table g ∧ no_jump g ∧ live_next g →
        g.net i = some { src := src, dst := dst, ev := E.eTest pl } →
          g.log i = some { src := src, dst := dst, ev := E.eTest pl } →
            i < g.clock →
              (g.machines dst).state = some S.Ring_Maintain →
                (g.machines dst).pending = none →
                  (g.machines dst).waiting = none →
                    (g.machines dst).halted = false →
                      RunPost (fun g' ↦ PostInv "no_error" (no_error g')) (GWorld.consume g i) dst (g.machines dst)
                        (Ring.Maintain.eTest_handler pl) := by
  intro g i src dst pl hF hE hInv hnet hlog hi hst hpend hwait hhalt
  obtain ⟨hS, ⟨hoA, hoR, hAn, h1n, h2n, hdom⟩, ⟨hj1, hj2⟩, ⟨hp2, hl2⟩⟩ := hInv
  have hoA' : org ∈ (g.machines dst).fields.Ring_active := hoA ⟨dst⟩ hst
  have hoR' : org ∈ (g.machines dst).fields.Ring_reach := hoR ⟨dst⟩ hst
  by_cases hx : pl ∈ (g.machines dst).fields.Ring_active
  · have hxn : pl ∈ nodes := hAn ⟨dst⟩ hst pl hx
    obtain ⟨y, hy⟩ : ∃ y, (g.machines dst).fields.Ring_s1[pl]? = some y :=
      Option.isSome_iff_exists.mp (hdom ⟨dst⟩ hst pl hx)
    have hyn : y ∈ nodes := h1n ⟨dst⟩ hst pl y hy
    have hyb : y ≠ org → RingBtw.btw pl y org = true := hj1 ⟨dst⟩ hst pl y hy
    by_cases hyA : y ∈ (g.machines dst).fields.Ring_active
    · clear hoA hoR hAn h1n h2n hdom hj1 hj2 hp2 hl2
      pverify_auto
    · obtain ⟨z, hz⟩ : ∃ z, (g.machines dst).fields.Ring_s2[pl]? = some z :=
        Option.isSome_iff_exists.mp (hp2 ⟨dst⟩ hst pl y hx hy hyA)
      have hzA : z ∈ (g.machines dst).fields.Ring_active := hl2 ⟨dst⟩ hst pl y z hx hy hyA hz
      have hzn : z ∈ nodes := h2n ⟨dst⟩ hst pl z hz
      have hzb : y ≠ org → z ≠ org → RingBtw.btw pl z org = true :=
        fun h1 h2 => hj2 ⟨dst⟩ hst pl y z hy h1 hz h2
      clear hoA hoR hAn h1n h2n hdom hj1 hj2 hp2 hl2
      pverify_auto
  · clear hoA hoR hAn h1n h2n hdom hj1 hj2 hp2 hl2
    pverify_auto

end ChordRing

#pverify    ChordRing

/-! A model of `RingBtw`'s laws on all of ℕ: `<` read cyclically, where the path from `x` back to
`x` is the whole ring. -/

/-- `y` is on the path from `x` to `z` going round `<` cyclically. -/
def cyclicBtw (x y z : Nat) : Bool :=
  decide ((x = z ∧ y ≠ x) ∨
    (x ≠ z ∧ ((x < y ∧ y < z) ∨ (y < z ∧ z < x) ∨ (z < x ∧ x < y))))

@[instance_reducible] def natRing : RingBtw Nat where
  btw := cyclicBtw
  btw_trans := by intro w x y z; simp [cyclicBtw]; omega
  btw_acyclic := by intro w x y; simp [cyclicBtw]; omega
  btw_total := by intro w x y; simp [cyclicBtw]; omega
  btw_cyclic := by intro x y z; simp [cyclicBtw]; omega
