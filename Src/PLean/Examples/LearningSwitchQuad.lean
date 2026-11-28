/-
LearningSwitchQuad: DuoAI's Ivy model `learning_switch_quad.ivy`, a learning switch with
packets encoded as quadruples.

Every node is a switch. A packet `(ps, pd)` is handed by a Host to its source switch
(`eInject`), which sends it to itself. A switch `sw1` handed a packet by `sw0` (`ePacket`)
learns, if it is not `ps` and has no route toward `ps` yet, that `sw0` is its next hop
toward `ps`; then it floods the packet to every switch. The destination is never read: the
protocol only learns routes. Ivy's `table(T, X, Y)` is the reflexive transitive closure of
the next hops toward `T`; safety says that for every `T` it is a forest order (reflexive,
transitive, antisymmetric, linear on what one switch reaches), so routes never form a cycle.

The table is a ghost kept per switch. Ivy's update of the global `table` is local: when
`sw1` learns its first route toward `ps`, no switch reaches `sw1` toward `ps`, so only
`sw1`'s row changes, to `{sw1} ∪ table(ps, sw0, ·)`; and `sw0`'s row toward `ps` no longer
changes once `sw0` floods. So each Switch keeps a ghost `row` (the switches on its path
toward each destination), `table` reads it, and a packet carries the sender's row as a
ghost `path`, which the receiver records when it learns. The protocol never reads the
ghosts. Its learning test reads `route` where Ivy's reads `table`; by the theorem they agree.

Safety (`Theorem safety`) is Ivy's `[1000000]` over `table` (`table_refl`, `table_trans`,
`table_antisym`, `table_linear`) for every destination `t`, a switch or not. The theorem
also ties `table` to the real next hops (`no_route_alone` … `hop_no_loop`), which then
form no cycle and have `table` as their reflexive transitive closure. That closure is Ivy's
`table`: Ivy's update is the closure update for the one learned edge `sw1 → sw0`.
`LearningSwitchQuad.safe` proves it under bag delivery; `safe_fifo` and `safety_holds`
cover FIFO delivery only, where every route points directly at its source.

Initially `switches` is exactly the set of Switches, their `route` and `row` are empty, and
each Host's `src` is a switch (its `dst` is unconstrained). There are finitely many
switches: a run violating safety involves finitely many nodes and restricts to a finite
instance. There are no `paxiom`s or `pinstance`s (Ivy has no axioms).

Deviations from the Ivy model, which keep its reachable tables:
- Packets are consumed on delivery; Ivy's `forward` may leave one pending. A repeat learns
  nothing, and its flood only duplicates packets `sw1` sent already.
- Ivy's `forward` drops some pending packets; here a lost packet is one never delivered,
  and safety does not depend on delivery.
- New packets come from the Hosts of the initial world, one each; an Ivy run has finitely
  many `new_packet`s.
- The learning test also requires `p.sender ≠ this`: for `sw0 = sw1` Ivy's update adds
  nothing, and the case is unreachable anyway.

Proof: `packets` (a packet's sender is its source or has a route toward it), `paths` (a
packet's ghost path is its sender's current row) and `reach` (nobody reaches a switch that
learns its first route). The two learning steps, of `safety` and of `reach`, are proved by
hand: they need `pkt_path` at the packet being handled (`path_at`), an instance the solvers
do not find.
-/
import PLean

open PLean

pmodule LearningSwitchQuad

  system s

  -- A packet `(ps, pd)` handed on by `sender`. `path` is a ghost: the sender's row of the
  -- table toward `ps`, sender included.
  type tPacket = (ps : PLean.MachineRef, pd : PLean.MachineRef, sender : PLean.MachineRef,
                  path : set[PLean.MachineRef])

  event ePacket : tPacket
  -- A new packet for the switch it is sent to, with this destination.
  event eInject : PLean.MachineRef

  -- The nodes: every Switch, and nothing else.
  function switches : set[PLean.MachineRef]

  -- Ivy's `table(t, x, y)`, read from `x`'s ghost rows `rw`.
  function table (rw : map[PLean.MachineRef, set[PLean.MachineRef]]) (t x y : PLean.MachineRef) :
      Prop =
    (x = y ∨ ((rw.get t).getD ∅).mem y = true)

  machine Switch {
    -- The next hop toward each destination this switch has learned a route to.
    var route : map[PLean.MachineRef, PLean.MachineRef]
    -- Ghost: for each such destination, the switches on the path toward it, this one excluded.
    var row   : map[PLean.MachineRef, set[PLean.MachineRef]]

    start state Run {
      -- Ivy's `new_packet(this, pd)`: the packet is pending from this switch to itself.
      on eInject (pd : PLean.MachineRef) {
        var path : set[PLean.MachineRef]
        path += (this.ref)
        send this.ref, ePacket, (ps = this.ref, pd = pd, sender = this.ref, path = path)
      }

      -- Ivy's `forward(p.ps, p.pd, p.sender, this, _)`.
      on ePacket (p : tPacket) {
        if p.ps ≠ this.ref ∧ ¬ (p.ps ∈ route) ∧ p.sender ≠ this.ref then do
          route[p.ps] = p.sender
          row[p.ps] = p.path
        -- this switch's row toward `p.ps` (`∅` when it has none), itself included (ghost)
        let path := ((row.get p.ps).getD ∅).add this.ref
        foreach (d in switches)
          invariant floods :
            new_sends e => e is ePacket ∧ e.target ∈ switches ∧
              (ePacket_payload_of e).ps = p.ps ∧ (ePacket_payload_of e).sender = this.ref ∧
              (ePacket_payload_of e).path = path ;
        {
          send d, ePacket, (ps = p.ps, pd = p.pd, sender = this.ref, path = path)
        }
      }
    }
  }

  -- The environment: injects the packet `(src, dst)` at switch `src`.
  machine Host {
    var src : PLean.MachineRef
    var dst : PLean.MachineRef

    start state Run {
      entry {
        send src, eInject, dst
      }
    }
  }

  init-holds ∀ r : PLean.MachineRef, r ∈ switches ↔ is_Switch r s
  init-holds ∀ h : Host, h.src ∈ switches
  init-holds ∀ (x : Switch) (t : PLean.MachineRef), ¬ (t ∈ x.route)
  init-holds ∀ (x : Switch) (t : PLean.MachineRef), ¬ (t ∈ x.row)

  -- Where messages go.
  Lemma routing {
    invariant switch_set : ∀ r : PLean.MachineRef, r ∈ switches ↔ is_Switch r s
    invariant host_src : ∀ h : Host, h.src ∈ switches
    invariant packet_to_switch : ∀ e : ePacket, sent e s → is_Switch e.target s
    invariant inject_to_switch : ∀ e : eInject, sent e s → is_Switch e.target s
  }

  Theorem safety {
    -- The Ivy model's invariant [1000000], for every destination `t`.
    invariant table_refl :
      ∀ (t : PLean.MachineRef) (x : Switch), table x.row t x.ref x.ref
    invariant table_trans :
      ∀ (t : PLean.MachineRef) (x y z : Switch),
        table x.row t x.ref y.ref → table y.row t y.ref z.ref → table x.row t x.ref z.ref
    invariant table_antisym :
      ∀ (t : PLean.MachineRef) (x y : Switch),
        table x.row t x.ref y.ref → table y.row t y.ref x.ref → x = y
    invariant table_linear :
      ∀ (t : PLean.MachineRef) (x y z : Switch),
        table x.row t x.ref y.ref → table x.row t x.ref z.ref →
          table y.row t y.ref z.ref ∨ table z.row t z.ref y.ref
    -- `table` is the reflexive transitive closure of the next hops.
    invariant no_route_alone :
      ∀ (t y : PLean.MachineRef) (x : Switch),
        ¬ (t ∈ x.route) → table x.row t x.ref y → y = x.ref
    invariant hop_switch :
      ∀ (t h : PLean.MachineRef) (x : Switch), x.route[t]? = some h → is_Switch h s
    invariant hop_on_path :
      ∀ (t y : PLean.MachineRef) (x h : Switch),
        x.route[t]? = some h.ref → table h.row t h.ref y → table x.row t x.ref y
    invariant path_via_hop :
      ∀ (t y : PLean.MachineRef) (x h : Switch),
        x.route[t]? = some h.ref → table x.row t x.ref y → y ≠ x.ref → table h.row t h.ref y
    invariant hop_no_loop :
      ∀ (t : PLean.MachineRef) (x h : Switch),
        x.route[t]? = some h.ref → ¬ table h.row t h.ref x.ref
  }

  -- What a switch has when it floods a packet: it is the source or has a route toward it.
  -- Toward itself, a switch reaches only itself (Ivy's commented-out
  -- `N1 ~= N2 -> ~table(N1, N1, N2)`).
  Lemma packets {
    invariant pkt_sender : ∀ e : ePacket, sent e s → is_Switch e.sender s
    invariant pkt_ready :
      ∀ (e : ePacket) (x : Switch), sent e s → e.sender = x.ref → x.ref ≠ e.ps → e.ps ∈ x.route
    invariant self_row :
      ∀ (x : Switch) (y : PLean.MachineRef), table x.row x.ref x.ref y → y = x.ref
  }

  -- A packet's ghost path is its sender's current row toward the source, sender included.
  Lemma paths {
    invariant pkt_path :
      ∀ (e : ePacket) (x : Switch) (y : PLean.MachineRef), sent e s → e.sender = x.ref →
        (y ∈ e.path ↔ table x.row e.ps x.ref y)
  }

  -- Every switch on a path toward `t`, other than `t`, has a route toward `t`: nobody reaches
  -- a switch that learns its first route.
  Lemma reach {
    invariant reach_dest :
      ∀ (t : PLean.MachineRef) (x y : Switch), table x.row t x.ref y.ref → y.ref ≠ x.ref →
        y.ref ≠ t → t ∈ y.route
  }

  Proof {
    prove routing ;
    prove packets ;
    prove paths using packets ;
    prove reach using packets, paths ;
    prove safety using packets, paths, reach ;
  }

end LearningSwitchQuad

#gen_module LearningSwitchQuad
#pwf        LearningSwitchQuad

namespace LearningSwitchQuad

/-- A handler's `var` of a set type starts as `default`, which is the empty set. -/
theorem default_set {α : Type} : (default : PSet α) = PSet.empty := rfl

end LearningSwitchQuad

-- Normalisation unfolds `table` and rewrites a handler's `default` local `path` to the empty
-- set, whose lookups the solvers are given; the solvers read `Option.getD`.
attribute [local pverifySimp] LearningSwitchQuad.table LearningSwitchQuad.default_set
attribute [local crush_unfold] Option.getD

namespace LearningSwitchQuad
open PLean Std.WP

/-- `pkt_path` at the packet `pl` being handled: its sender is a switch, and its ghost path is
the sender's row toward `pl.ps`, sender included. -/
theorem path_at (g : GW) (i src dst : Nat) (pl : ePacket_payload)
    (hS : pkt_sender g) (hP : pkt_path g)
    (hlog : g.log i = some { src := src, dst := dst, ev := E.ePacket pl }) :
    (g.machines pl.sender).state = some S.Switch_Run ∧
    ∀ y, pl.path.mem y = true ↔
      (pl.sender = y ∨
        (((g.machines pl.sender).fields.Switch_row.get pl.ps).getD PSet.empty).mem y = true) := by
  have hsent : sent (⟨i, src, dst, E.ePacket pl⟩ : Msg E) g := hlog
  have hsw : (g.machines pl.sender).state = some S.Switch_Run := hS _ ⟨pl, rfl⟩ hsent
  exact ⟨hsw, fun y => hP ⟨i, src, dst, E.ePacket pl⟩ ⟨pl, rfl⟩ ⟨pl.sender⟩ hsw y hsent rfl⟩

/-- The learning step of `reach`: the switches on the new row have a route toward `pl.ps`. -/
@[pverifyProof] theorem Switch.Run.ePacket_correct_reach :
    ∀ (g : GW) (i : Nat) (src dst : MachineRef) (pl : ePacket_payload),
      Framework g →
        g.error = none →
          reach g ∧ packets g ∧ paths g →
            g.net i = some { src := src, dst := dst, ev := E.ePacket pl } →
              g.log i = some { src := src, dst := dst, ev := E.ePacket pl } →
                i < g.clock →
                  (g.machines dst).state = some S.Switch_Run →
                    (g.machines dst).pending = none →
                      (g.machines dst).waiting = none →
                        (g.machines dst).halted = false →
                          RunPost (fun g' ↦ PostInv "reach_dest" (reach_dest g'))
                            (GWorld.consume g i) dst (g.machines dst)
                            (Switch.Run.ePacket_handler pl) := by
  intro g i src dst pl hF hE hInv hnet hlog hi hst hp hw hh
  have ⟨hsw, hpath⟩ := path_at g i src dst pl hInv.2.1.1 hInv.2.2 hlog
  pverify_auto

set_option maxHeartbeats 1000000 in
/-- The learning step of `safety`: the new row is the sender's row plus the learner. -/
@[pverifyProof] theorem Switch.Run.ePacket_correct_safety :
    ∀ (g : GW) (i : Nat) (src dst : MachineRef) (pl : ePacket_payload),
      Framework g →
        g.error = none →
          safety g ∧ packets g ∧ paths g ∧ reach g →
            g.net i = some { src := src, dst := dst, ev := E.ePacket pl } →
              g.log i = some { src := src, dst := dst, ev := E.ePacket pl } →
                i < g.clock →
                  (g.machines dst).state = some S.Switch_Run →
                    (g.machines dst).pending = none →
                      (g.machines dst).waiting = none →
                        (g.machines dst).halted = false →
                          RunPost
                            (fun g' ↦
                              PostInv "table_refl" (table_refl g') ∧
                                PostInv "table_trans" (table_trans g') ∧
                                  PostInv "table_antisym" (table_antisym g') ∧
                                    PostInv "table_linear" (table_linear g') ∧
                                      PostInv "no_route_alone" (no_route_alone g') ∧
                                        PostInv "hop_switch" (hop_switch g') ∧
                                          PostInv "hop_on_path" (hop_on_path g') ∧
                                            PostInv "path_via_hop" (path_via_hop g') ∧
                                              PostInv "hop_no_loop" (hop_no_loop g'))
                            (GWorld.consume g i) dst (g.machines dst)
                            (Switch.Run.ePacket_handler pl) := by
  intro g i src dst pl hF hE hInv hnet hlog hi hst hp hw hh
  have ⟨hsw, hpath⟩ := path_at g i src dst pl hInv.2.1.1 hInv.2.2.1 hlog
  pverify_auto

end LearningSwitchQuad

#pverify LearningSwitchQuad
