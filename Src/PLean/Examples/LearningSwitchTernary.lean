/-
LearningSwitchTernary: DuoAI's Ivy model `learning_switch_ternary.ivy`, a learning switch
whose pending packets are a ternary relation `pending(p, sw0, sw1)`.

Switches forward packets over symmetric links and learn routes from the packets they see.
A switch `sw1` that receives a packet `p` from `sw0` and has no route toward `src(p)` (and is
not `src(p)`) learns one: its next hop toward `src(p)` is `sw0`. Then, unless it is `dst(p)`,
it forwards `p` along its route toward `dst(p)` if it has one (Ivy's `route`), and otherwise
to every neighbour but `sw0` (`flood`); which one applies is decided before learning, as
Ivy's `require`s read the pre-state. A Host injects a packet at its source switch.

`pending(p, sw0, sw1)` is an `ePacket` in flight to `sw1` with `prev = sw0`; a packet carries
only `src(p)` and `dst(p)`, all Ivy reads of `p`. `link` is the `links` field, which no
handler writes; `route_dom(n, x)` is `n ∈ x.route`.

The closure is kept as ghost rows. Ivy's learning step updates every row of the global
`route_tc`; here each switch keeps its own (`x.route_tc[n]`: the switches beyond `x` on its
route toward `n`; `reaches` reads them). The update is local: no switch routes through the
learning switch (`tc_through_routed`), so only its row changes; and the packet carries a
copy of `prev`'s row (`prev_tc`), still current on delivery (`tc_snapshot`), since a switch
learns only its first route toward a destination. No guard reads the ghosts. `tc_needs_route`,
`tc_strict`, `tc_next` and `route_to_switch` make `route_tc` the closure of `route`, and the
next hop the only `sw2` Ivy's `route` accepts.

Every Ivy step is a step of the model, given a Host for each packet the run injects. The
model has behaviours beyond Ivy's, all readings of message passing:
- Ivy's `pending` keeps the traversal a step reads, so it can be handled again; here the
  handler re-sends the packet to its own switch.
- Traversals that Ivy's `flood` erases stay deliverable, and re-sent copies accumulate. A
  copy never delivered is a lost packet or an erased traversal.
- A Host's `dst` can be any reference; its packets are flooded forever.
- A switch always forwards to its next hop, even where Ivy's `route` would find no
  admissible `sw2`; no reachable world has such a switch.
There are finitely many switches and links: a finite Ivy run touches finitely many nodes,
and restricting `link` to them keeps the axioms. Packet identity is dropped.

Initially no packet is in flight, every `route` and `route_tc` is empty, and `links` are
switches, satisfying Ivy's two axioms (irreflexive, symmetric). A Host's `sw` is a switch.

Safety (`Theorem safety`) is Ivy's `[1000000]`, conjunct by conjunct, over switches, with
`route_tc(N, X, Y)` read as `reaches X.route_tc N X Y`: the rows toward each destination form
an acyclic partial function. `LearningSwitchTernary.safe` proves it, with the lemmas that make
the rows Ivy's `route_tc`, under bag delivery; `safe_fifo` and `safety_holds` cover FIFO
delivery only, which does not follow every Ivy run.

Proof: small lemmas with short `using` lists. The handler's steps for `Theorem safety` and
`Lemma closure` are proved by hand, as the solvers' proofs of them do not replay: the key
lemmas `learn_trans_linear` and `learn_next` give the learned row and next hop from the old
invariants at `prev`. `row_ite` and `state_ite` say that a step leaves the other machines'
rows and states as they were.
-/
import PLean

open PLean

pmodule LearningSwitchTernary

  system s

  -- `prev` is Ivy's `sw0`; `prev_tc` (ghost) is `prev`'s row toward `psrc` when it sent.
  type tPacket = (psrc : PLean.MachineRef, pdst : PLean.MachineRef, prev : PLean.MachineRef,
                  prev_tc : set[PLean.MachineRef])

  event ePacket : tPacket

  -- Ivy's `route_tc(n, x, y)`, for a switch `x` whose ghost rows are `tc`.
  function reaches (tc : map[PLean.MachineRef, set[PLean.MachineRef]])
      (n x y : PLean.MachineRef) : Prop =
    (x = y ∨ y ∈ (tc[n]?).getD ∅)

  machine Switch {
    var links    : set[PLean.MachineRef]
    var route    : map[PLean.MachineRef, PLean.MachineRef]
    -- Ghost: for each `n ∈ route`, the switches beyond this one on the route toward `n`.
    var route_tc : map[PLean.MachineRef, set[PLean.MachineRef]]

    start state Run {
      on ePacket (pkt : tPacket) {
        var routed : Bool
        var hop    : PLean.MachineRef
        var row    : set[PLean.MachineRef]
        -- The traversal stays pending, as in Ivy.
        send this.ref, ePacket, pkt
        -- Ivy's `route` if there is a route toward the destination, else `flood`.
        routed = false
        if pkt.pdst ∈ route then do
          routed = true
          hop = route[pkt.pdst]
        if ¬ (pkt.psrc ∈ route) ∧ pkt.psrc ≠ this.ref then do
          route[pkt.psrc] = pkt.prev
          row = pkt.prev_tc
          row += (pkt.prev)
          route_tc[pkt.psrc] = row
        else do
          -- (ghost) this switch's row toward `psrc`, read as the invariants read it
          row = (route_tc[pkt.psrc]?).getD ∅
        if pkt.pdst ≠ this.ref then do
          if routed then do
            send hop, ePacket, (psrc = pkt.psrc, pdst = pkt.pdst, prev = this.ref, prev_tc = row)
          else do
            foreach (y in links)
              invariant floods :
                new_sends e => e is ePacket ∧ e.target ∈ links ∧ e.target ≠ pkt.prev ∧
                  (ePacket_payload_of e).psrc = pkt.psrc ∧
                  (ePacket_payload_of e).pdst = pkt.pdst ∧
                  (ePacket_payload_of e).prev = this.ref ∧ (ePacket_payload_of e).prev_tc = row ;
            {
              if y ≠ pkt.prev then do
                send y, ePacket, (psrc = pkt.psrc, pdst = pkt.pdst, prev = this.ref, prev_tc = row)
            }
      }
    }
  }

  -- Injects one packet from `sw` toward `dst`: Ivy's `pending(p, src(p), src(p))`.
  machine Host {
    var sw  : PLean.MachineRef
    var dst : PLean.MachineRef

    start state Run {
      entry {
        send sw, ePacket, (psrc = sw, pdst = dst, prev = sw, prev_tc = ∅)
      }
    }
  }

  init-holds ∀ (x : Switch) (n : PLean.MachineRef), ¬ (n ∈ x.route)
  init-holds ∀ (x : Switch) (n : PLean.MachineRef), ¬ (n ∈ x.route_tc)
  init-holds ∀ x : Switch, ¬ (x.ref ∈ x.links)
  init-holds ∀ (x y : Switch), y.ref ∈ x.links → x.ref ∈ y.links
  init-holds ∀ (x : Switch) (r : PLean.MachineRef), r ∈ x.links → is_Switch r s
  init-holds ∀ h : Host, is_Switch h.sw s

  -- Packets go from switch to switch, and next hops are switches.
  Lemma routing {
    invariant links_switch :
      ∀ (x : Switch) (r : PLean.MachineRef), r ∈ x.links → is_Switch r s
    invariant host_switch : ∀ h : Host, is_Switch h.sw s
    invariant route_to_switch :
      ∀ (x : Switch) (n r : PLean.MachineRef), x.route[n]? = some r → is_Switch r s
    invariant packet_to_switch : ∀ e : ePacket, sent e s → is_Switch e.target s
  }

  -- A switch hands a packet on only once it is the packet's source or has a route toward it,
  -- and it never learns a route toward itself.
  Lemma senders {
    invariant prev_switch : ∀ e : ePacket, sent e s → is_Switch e.prev s
    invariant no_self_route : ∀ x : Switch, ¬ (x.ref ∈ x.route)
    invariant sender_routed :
      ∀ (e : ePacket) (w : Switch), sent e s → e.prev = w.ref → e.prev ≠ e.psrc →
        e.psrc ∈ w.route
  }

  -- A packet carries its sender's current row toward its source.
  Lemma snapshot {
    invariant tc_snapshot :
      ∀ (e : ePacket) (w : Switch) (z : PLean.MachineRef), sent e s → e.prev = w.ref →
        (z ∈ e.prev_tc ↔ z ∈ (w.route_tc[e.psrc]?).getD ∅)
  }

  -- The ghost rows: a row is empty until its switch has a route, never holds its own switch,
  -- and holds only switches that are the destination or have a route toward it.
  Lemma rows {
    invariant tc_needs_route :
      ∀ (n x : Switch) (y : PLean.MachineRef),
        y ∈ (x.route_tc[n.ref]?).getD ∅ → n.ref ∈ x.route
    invariant tc_strict :
      ∀ (n x : Switch), ¬ (x.ref ∈ (x.route_tc[n.ref]?).getD ∅)
    invariant tc_through_routed :
      ∀ (n x y : Switch),
        y.ref ∈ (x.route_tc[n.ref]?).getD ∅ → y.ref ≠ n.ref → n.ref ∈ y.route
  }

  -- A switch's row is its next hop and the next hop's row: the rows are the closure of `route`.
  Lemma closure {
    invariant tc_next :
      ∀ (n x y : Switch) (z : PLean.MachineRef), x.route[n.ref]? = some y.ref →
        (z ∈ (x.route_tc[n.ref]?).getD ∅ ↔ z = y.ref ∨ z ∈ (y.route_tc[n.ref]?).getD ∅)
  }

  -- Ivy's invariant [1000000].
  Theorem safety {
    invariant tc_refl :
      ∀ (n x : Switch), reaches x.route_tc n.ref x.ref x.ref
    invariant tc_trans :
      ∀ (n x y z : Switch),
        reaches x.route_tc n.ref x.ref y.ref → reaches y.route_tc n.ref y.ref z.ref →
        reaches x.route_tc n.ref x.ref z.ref
    invariant tc_antisymm :
      ∀ (n x y : Switch),
        reaches x.route_tc n.ref x.ref y.ref → reaches y.route_tc n.ref y.ref x.ref →
        x.ref = y.ref
    invariant tc_linear :
      ∀ (n x y z : Switch),
        reaches x.route_tc n.ref x.ref y.ref → reaches x.route_tc n.ref x.ref z.ref →
        reaches y.route_tc n.ref y.ref z.ref ∨ reaches z.route_tc n.ref z.ref y.ref
  }

  Proof {
    prove routing using senders ;
    prove senders using routing ;
    prove snapshot using senders, rows ;
    prove rows using snapshot, senders ;
    prove closure using rows, snapshot, senders ;
    prove safety using rows, snapshot, senders ;
  }

end LearningSwitchTernary

#gen_module LearningSwitchTernary
#pwf        LearningSwitchTernary

namespace LearningSwitchTernary
open PLean

/-- A step leaves every other machine as it was: normalisation reads machine `x` of the
post-state as `if x = d then … else <x's old state>`, and where `d`'s part is unchanged both
branches agree. -/
theorem row_ite (ms : MachineRef → MachineState S E F G W) (x d n y : MachineRef) :
    (if x = d then (((ms d).fields.Switch_route_tc.get n).getD PSet.empty).mem y
     else (((ms x).fields.Switch_route_tc.get n).getD PSet.empty).mem y) =
    (((ms x).fields.Switch_route_tc.get n).getD PSet.empty).mem y := by
  split <;> simp_all

theorem state_ite (ms : MachineRef → MachineState S E F G W) (x d : MachineRef) :
    (if x = d then (ms d).state else (ms x).state) = (ms x).state := by
  split <;> simp_all

end LearningSwitchTernary

-- Normalisation unfolds `reaches` and reads unchanged machines through a step; the solvers
-- read `Option.getD`.
attribute [local pverifySimp] LearningSwitchTernary.reaches LearningSwitchTernary.row_ite
  LearningSwitchTernary.state_ite
attribute [local crush_unfold] Option.getD

namespace LearningSwitchTernary
open PLean Std.WP

/-- Whether switch `x`'s ghost row toward `n` holds `y`, as normalisation writes it. -/
def inRow (g : GW) (x n y : MachineRef) : Bool :=
  (((g.machines x).fields.Switch_route_tc.get n).getD PSet.empty).mem y

/-- The same after switch `d` learns its first route toward `pl.psrc` from `pl`. -/
def learnRow (g : GW) (d : MachineRef) (pl : ePacket_payload) (x n y : MachineRef) : Bool :=
  if x = d then
    (if n = pl.psrc then decide (y = pl.prev) || pl.prev_tc.mem y else inRow g d n y)
  else inRow g x n y

/-- Switch `x`'s next hop toward `n` after switch `d` learns its route toward `pl.psrc`. -/
def learnRoute (g : GW) (d : MachineRef) (pl : ePacket_payload) (x n : MachineRef) :
    Option MachineRef :=
  if x = d then
    (if n = pl.psrc then some pl.prev else (g.machines d).fields.Switch_route.get n)
  else (g.machines x).fields.Switch_route.get n

/-- Away from `d`'s row toward the source, nothing changed. -/
theorem learnRow_same (g : GW) (d : MachineRef) (pl : ePacket_payload) (x n y : MachineRef)
    (h : ¬ (x = d ∧ n = pl.psrc)) : learnRow g d pl x n y = inRow g x n y := by
  unfold learnRow
  by_cases hx : x = d
  · subst hx; simp [show n ≠ pl.psrc from fun hn => h ⟨rfl, hn⟩]
  · simp [hx]

theorem learnRoute_same (g : GW) (d : MachineRef) (pl : ePacket_payload) (x n : MachineRef)
    (h : ¬ (x = d ∧ n = pl.psrc)) :
    learnRoute g d pl x n = (g.machines x).fields.Switch_route.get n := by
  unfold learnRoute
  by_cases hx : x = d
  · subst hx; simp [show n ≠ pl.psrc from fun hn => h ⟨rfl, hn⟩]
  · simp [hx]

/-- What the learning step needs of the packet `pl` that switch `d` learns from: nobody's
row toward the source holds `d`, `pl.prev` is a switch other than `d`, and `pl.prev_tc` is
`pl.prev`'s row toward the source. -/
theorem learn_facts (g : GW) (i : Nat) (src d : MachineRef) (pl : ePacket_payload)
    (hrows : rows g) (hsnap : snapshot g) (hsend : senders g)
    (hlog : g.log i = some { src := src, dst := d, ev := E.ePacket pl })
    (hd : (g.machines d).state = some S.Switch_Run)
    (hnr : ¬((g.machines d).fields.Switch_route.get pl.psrc).isSome = true)
    (hne : ¬pl.psrc = d) :
    (g.machines pl.prev).state = some S.Switch_Run ∧ pl.prev ≠ d ∧
      (∀ z, pl.prev_tc.mem z = inRow g pl.prev pl.psrc z) ∧
      ((g.machines pl.psrc).state = some S.Switch_Run →
        ∀ x, (g.machines x).state = some S.Switch_Run → inRow g x pl.psrc d = false) := by
  obtain ⟨hprev, -, hrouted⟩ := hsend
  have hpS : (g.machines pl.prev).state = some S.Switch_Run :=
    hprev ⟨i, src, d, E.ePacket pl⟩ ⟨pl, rfl⟩ hlog
  refine ⟨hpS, ?_, ?_, ?_⟩
  · intro h
    have hr := hrouted ⟨i, src, d, E.ePacket pl⟩ ⟨pl, rfl⟩ ⟨pl.prev⟩ hpS hlog rfl
    by_cases hps : pl.prev = pl.psrc
    · exact hne (hps ▸ h)
    · have := hr hps
      rw [h] at this
      exact hnr this
  · intro z
    have := hsnap ⟨i, src, d, E.ePacket pl⟩ ⟨pl, rfl⟩ ⟨pl.prev⟩ hpS z hlog rfl
    exact Bool.eq_iff_iff.mpr this
  · intro hn x hx
    cases h : inRow g x pl.psrc d
    · rfl
    · have := hrows.2.2 ⟨pl.psrc⟩ hn ⟨x⟩ hx ⟨d⟩ hd h (Ne.symm hne)
      exact absurd this hnr

/-- Ivy's transitivity and linearity after the learning step. -/
theorem learn_trans_linear (g : GW) (i : Nat) (src d : MachineRef) (pl : ePacket_payload)
    (hsafe : safety g) (hrows : rows g) (hsnap : snapshot g) (hsend : senders g)
    (hlog : g.log i = some { src := src, dst := d, ev := E.ePacket pl })
    (hd : (g.machines d).state = some S.Switch_Run)
    (hnr : ¬((g.machines d).fields.Switch_route.get pl.psrc).isSome = true)
    (hne : ¬pl.psrc = d) :
    (∀ n x y z : MachineRef, (g.machines n).state = some S.Switch_Run →
      (g.machines x).state = some S.Switch_Run → (g.machines y).state = some S.Switch_Run →
      (g.machines z).state = some S.Switch_Run →
      (x = y ∨ learnRow g d pl x n y = true) → (y = z ∨ learnRow g d pl y n z = true) →
      (x = z ∨ learnRow g d pl x n z = true)) ∧
    (∀ n x y z : MachineRef, (g.machines n).state = some S.Switch_Run →
      (g.machines x).state = some S.Switch_Run → (g.machines y).state = some S.Switch_Run →
      (g.machines z).state = some S.Switch_Run →
      (x = y ∨ learnRow g d pl x n y = true) → (x = z ∨ learnRow g d pl x n z = true) →
      ((y = z ∨ learnRow g d pl y n z = true) ∨ (z = y ∨ learnRow g d pl z n y = true))) := by
  obtain ⟨hpS, hpd, hsn, hnoD⟩ := learn_facts g i src d pl hrows hsnap hsend hlog hd hnr hne
  obtain ⟨-, htrans, -, hlin⟩ := hsafe
  -- the old relation, and the new one where it differs
  have told : ∀ n x y z : MachineRef, (g.machines n).state = some S.Switch_Run →
      (g.machines x).state = some S.Switch_Run → (g.machines y).state = some S.Switch_Run →
      (g.machines z).state = some S.Switch_Run →
      (x = y ∨ inRow g x n y = true) → (y = z ∨ inRow g y n z = true) →
      (x = z ∨ inRow g x n z = true) :=
    fun n x y z hn hx hy hz => htrans ⟨n⟩ hn ⟨x⟩ hx ⟨y⟩ hy ⟨z⟩ hz
  have lold : ∀ n x y z : MachineRef, (g.machines n).state = some S.Switch_Run →
      (g.machines x).state = some S.Switch_Run → (g.machines y).state = some S.Switch_Run →
      (g.machines z).state = some S.Switch_Run →
      (x = y ∨ inRow g x n y = true) → (x = z ∨ inRow g x n z = true) →
      ((y = z ∨ inRow g y n z = true) ∨ (z = y ∨ inRow g z n y = true)) :=
    fun n x y z hn hx hy hz => hlin ⟨n⟩ hn ⟨x⟩ hx ⟨y⟩ hy ⟨z⟩ hz
  have same := learnRow_same g d pl
  have atd : ∀ y,
      learnRow g d pl d pl.psrc y = (decide (y = pl.prev) || inRow g pl.prev pl.psrc y) := by
    intro y; simp [learnRow, hsn]
  constructor
  · intro n x y z hn hx hy hz hxy hyz
    by_cases hN : n = pl.psrc
    · subst hN
      have hD := hnoD hn
      by_cases hxd : x = d
      · subst hxd
        by_cases hyd : y = x
        · subst hyd; exact hyz
        · rw [same y _ z (fun h => hyd h.1)] at hyz
          rw [atd] at hxy ⊢
          have hpy : pl.prev = y ∨ inRow g pl.prev pl.psrc y = true := by
            rcases hxy with h | h
            · exact absurd h.symm hyd
            · simp at h; rcases h with h | h
              · exact Or.inl h.symm
              · exact Or.inr h
          have := told _ _ _ _ hn hpS hy hz hpy hyz
          right; simp; rcases this with h | h
          · exact Or.inl h.symm
          · exact Or.inr h
      · rw [same x _ y (fun h => hxd h.1)] at hxy
        rw [same x _ z (fun h => hxd h.1)]
        have hyd : y ≠ d := by
          rintro rfl
          rcases hxy with h | h
          · exact hxd h
          · rw [hD x hx] at h; exact absurd h (by simp)
        rw [same y _ z (fun h => hyd h.1)] at hyz
        exact told _ _ _ _ hn hx hy hz hxy hyz
    · rw [same x n y (fun h => hN h.2)] at hxy
      rw [same y n z (fun h => hN h.2)] at hyz
      rw [same x n z (fun h => hN h.2)]
      exact told _ _ _ _ hn hx hy hz hxy hyz
  · intro n x y z hn hx hy hz hxy hxz
    by_cases hN : n = pl.psrc
    · subst hN
      have hD := hnoD hn
      by_cases hxd : x = d
      · subst hxd
        by_cases hyd : y = x
        · subst hyd; exact Or.inl hxz
        by_cases hzd : z = x
        · subst hzd; exact Or.inr hxy
        rw [same y _ z (fun h => hyd h.1), same z _ y (fun h => hzd h.1)]
        rw [atd] at hxy hxz
        have hpy : pl.prev = y ∨ inRow g pl.prev pl.psrc y = true := by
          rcases hxy with h | h
          · exact absurd h.symm hyd
          · simp at h; rcases h with h | h
            · exact Or.inl h.symm
            · exact Or.inr h
        have hpz : pl.prev = z ∨ inRow g pl.prev pl.psrc z = true := by
          rcases hxz with h | h
          · exact absurd h.symm hzd
          · simp at h; rcases h with h | h
            · exact Or.inl h.symm
            · exact Or.inr h
        exact lold _ _ _ _ hn hpS hy hz hpy hpz
      · rw [same x _ y (fun h => hxd h.1)] at hxy
        rw [same x _ z (fun h => hxd h.1)] at hxz
        have notd : ∀ w, (x = w ∨ inRow g x pl.psrc w = true) → w ≠ d := by
          rintro w hw rfl
          rcases hw with h | h
          · exact hxd h
          · rw [hD x hx] at h; exact absurd h (by simp)
        rw [same y _ z (fun h => notd y hxy h.1), same z _ y (fun h => notd z hxz h.1)]
        exact lold _ _ _ _ hn hx hy hz hxy hxz
    · rw [same x n y (fun h => hN h.2)] at hxy
      rw [same x n z (fun h => hN h.2)] at hxz
      rw [same y n z (fun h => hN h.2), same z n y (fun h => hN h.2)]
      exact lold _ _ _ _ hn hx hy hz hxy hxz

/-- `tc_next` after the learning step. -/
theorem learn_next (g : GW) (i : Nat) (src d : MachineRef) (pl : ePacket_payload)
    (hnext : closure g) (hrows : rows g) (hsnap : snapshot g) (hsend : senders g)
    (hlog : g.log i = some { src := src, dst := d, ev := E.ePacket pl })
    (hd : (g.machines d).state = some S.Switch_Run)
    (hnr : ¬((g.machines d).fields.Switch_route.get pl.psrc).isSome = true)
    (hne : ¬pl.psrc = d) :
    ∀ n x y z : MachineRef, (g.machines n).state = some S.Switch_Run →
      (g.machines x).state = some S.Switch_Run → (g.machines y).state = some S.Switch_Run →
      learnRoute g d pl x n = some y →
      (learnRow g d pl x n z = true ↔ z = y ∨ learnRow g d pl y n z = true) := by
  obtain ⟨hpS, hpd, hsn, hnoD⟩ := learn_facts g i src d pl hrows hsnap hsend hlog hd hnr hne
  have nold : ∀ n x y z : MachineRef, (g.machines n).state = some S.Switch_Run →
      (g.machines x).state = some S.Switch_Run → (g.machines y).state = some S.Switch_Run →
      (g.machines x).fields.Switch_route.get n = some y →
      (inRow g x n z = true ↔ z = y ∨ inRow g y n z = true) :=
    fun n x y z hn hx hy => hnext ⟨n⟩ hn ⟨x⟩ hx ⟨y⟩ hy z
  intro n x y z hn hx hy hr
  by_cases hN : n = pl.psrc
  · subst hN
    by_cases hxd : x = d
    · subst hxd
      have hr' : pl.prev = y := by simpa [learnRoute] using hr
      subst hr'
      rw [learnRow_same g x pl pl.prev pl.psrc z (fun h => hpd h.1)]
      simp [learnRow, hsn]
    · rw [learnRoute_same g d pl x _ (fun h => hxd h.1)] at hr
      have hyd : y ≠ d := by
        rintro rfl
        have := (nold _ x y y hn hx hy hr).mpr (Or.inl rfl)
        rw [hnoD hn x hx] at this
        exact absurd this (by simp)
      rw [learnRow_same g d pl x _ z (fun h => hxd h.1),
        learnRow_same g d pl y _ z (fun h => hyd h.1)]
      exact nold _ _ _ z hn hx hy hr
  · rw [learnRoute_same g d pl x n (fun h => hN h.2)] at hr
    rw [learnRow_same g d pl x n z (fun h => hN h.2), learnRow_same g d pl y n z (fun h => hN h.2)]
    exact nold _ _ _ z hn hx hy hr

set_option maxHeartbeats 4000000 in
@[pverifyProof] theorem Switch.Run.ePacket_correct_safety :
    ∀ (g : GW) (i : Nat) (src dst : MachineRef) (pl : ePacket_payload),
      Framework g → g.error = none →
        safety g ∧ rows g ∧ snapshot g ∧ senders g →
          g.net i = some { src := src, dst := dst, ev := E.ePacket pl } →
            g.log i = some { src := src, dst := dst, ev := E.ePacket pl } →
              i < g.clock → (g.machines dst).state = some S.Switch_Run →
                (g.machines dst).pending = none → (g.machines dst).waiting = none →
                  (g.machines dst).halted = false →
                    RunPost
                      (fun g' ↦
                        PostInv "tc_refl" (tc_refl g') ∧ PostInv "tc_trans" (tc_trans g') ∧
                          PostInv "tc_antisymm" (tc_antisymm g') ∧
                            PostInv "tc_linear" (tc_linear g'))
                      (GWorld.consume g i) dst (g.machines dst)
                      (Switch.Run.ePacket_handler pl) := by
  intro g i src dst pl hF hE hInv hnet hlog hi hst
  have key := fun hnr hne =>
    learn_trans_linear g i src dst pl hInv.1 hInv.2.1 hInv.2.2.1 hInv.2.2.2 hlog hst hnr hne
  simp only [learnRow, inRow] at key
  pverify_auto

set_option maxHeartbeats 4000000 in
@[pverifyProof] theorem Switch.Run.ePacket_correct_closure :
    ∀ (g : GW) (i : Nat) (src dst : MachineRef) (pl : ePacket_payload),
      Framework g → g.error = none →
        closure g ∧ rows g ∧ snapshot g ∧ senders g →
          g.net i = some { src := src, dst := dst, ev := E.ePacket pl } →
            g.log i = some { src := src, dst := dst, ev := E.ePacket pl } →
              i < g.clock → (g.machines dst).state = some S.Switch_Run →
                (g.machines dst).pending = none → (g.machines dst).waiting = none →
                  (g.machines dst).halted = false →
                    RunPost (fun g' ↦ PostInv "tc_next" (tc_next g'))
                      (GWorld.consume g i) dst (g.machines dst)
                      (Switch.Run.ePacket_handler pl) := by
  intro g i src dst pl hF hE hInv hnet hlog hi hst
  have key := fun hnr hne =>
    learn_next g i src dst pl hInv.1 hInv.2.1 hInv.2.2.1 hInv.2.2.2 hlog hst hnr hne
  simp only [learnRow, learnRoute, inRow] at key
  pverify_auto

end LearningSwitchTernary

#pverify    LearningSwitchTernary
