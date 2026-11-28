/-
HybridReliableBroadcast: DuoAI's Ivy model `hybrid_reliable_broadcast.ivy`, reliable
broadcast among nodes with mixed faults.

A source sends an init message to some nodes; a node that got it echoes (`eMsg`) to every
node. A node records the senders of the echoes it receives (`rcvd`): once they contain an
A-quorum (Ivy's `quorum_a`, think `t + 1` nodes) it echoes too, and once they contain a
B-quorum (`quorum_b`, think `n - t`) it accepts. Each node has a fault class (`fault`):
- `correct` nodes follow the protocol;
- `fc` (symmetric omission) may skip an echo it owes, but sends it to all or none;
- `fi` (arbitrary omission) sends each echo it owes to any subset of the nodes;
- `fs` / `fa` (symmetric / arbitrary Byzantine) may echo to all / any subset at any time,
  and their state is arbitrary.
Channels are authenticated: a node, faulty or not, echoes only as itself.

Safety (`accept_needs_init`) is Ivy's: if a node that is neither `fs` nor `fa` accepts, some
node that is neither received the init message. `HybridReliableBroadcast.safe` proves it
under bag delivery; `safe_fifo` and `safety_holds` cover FIFO delivery only.

Ivy's actions are handler branches (each handler names its actions). A node triggers its
spontaneous actions by sending itself events: its start entry sends `eInit` when `rcv_init`
holds, and `eFaultySend` / `eFaultyState` when it is `fs` or `fa`; each faulty event sends
itself again, so a Byzantine node acts any number of times. Ivy's choices (a subset of
destinations, an `fc` node's skip, a Byzantine node's new state) are `if ($)`. As in Ivy,
any node may run `receive_init` and `receive_msg`. `rcv_msg(S, N)` is `S ∈ N.rcvd`. The
fault classes are a function, so Ivy's disjointness axioms hold by construction; classes
and quorum systems are arbitrary and fixed. `rcv_init` is arbitrary initially and never
changes; initially no node accepts or has counted an echo. There are finitely many nodes.

The quorum paxioms state Ivy's axioms through `hasQuorumA q` / `hasQuorumB q` ("`q` contains
an A- / B-quorum") and the witnesses `honestIn q` / `coreIn q`. They have models in which a
correct node accepts: four nodes, one `fa`, an A-quorum any 2 nodes, a B-quorum any 3.

Deviations from the Ivy model:
- Ivy's axioms imply the paxioms. Upward closure of the quorum predicates and Ivy's
  liveness axiom (some B-quorum is correct) are not assumed, and the predicates read a
  set's update history, not its members: more quorum systems are covered.
- A packet is consumed on delivery; Ivy can re-read `sent_msg`. A repeated Ivy action
  changes nothing, or sends echoes the node could have sent at the earlier step (delivered
  later), or does what `eFaultySend` / `eFaultyState` can do. So every Ivy run has a run
  here with the same `rcv_init`, `accept` and `rcv_msg`, which is all safety reads.
- A node echoes on every echo it handles with an A-quorum, not once. Ivy's
  `sent_msg(n, D) := true` is idempotent, and a duplicate echo is handled as a repeated
  `receive_msg`.

Proof: `chain` traces echoes and counted senders back to an init message, and safety
follows from `chain` and `routing`. The two obligations of the step that counts an echo
are proved by hand, from lemmas about one world: the solvers prove them without the
lemmas, but those proofs do not replay.
-/
import PLean

open PLean

pmodule HybridReliableBroadcast

  system s

  enum tFault { correct, fc, fi, fs, fa }

  type tMsg = (sender : PLean.MachineRef)

  event eInit
  -- An echo: Ivy's `sent_msg(sender, target)`.
  event eMsg    : tMsg
  event eFaultySend
  event eFaultyState

  -- The destinations of a broadcast: every node.
  function nodes : set[PLean.MachineRef]
  -- Ivy's `member_fc/fi/fs/fa`.
  function fault : PLean.MachineRef → tFault
  -- The set contains a quorum of the first / second system.
  function hasQuorumA : set[PLean.MachineRef] → Bool
  function hasQuorumB : set[PLean.MachineRef] → Bool
  -- Witnesses of Ivy's two forall-exists quorum axioms.
  function honestIn : set[PLean.MachineRef] → PLean.MachineRef
  function coreIn   : set[PLean.MachineRef] → set[PLean.MachineRef]

  -- An A-quorum holds a node that is neither `fs` nor `fa`.
  paxiom quorum_a_honest :
    ∀ q : set[PLean.MachineRef], hasQuorumA q = true →
      honestIn q ∈ q ∧ fault (honestIn q) ≠ tFault.fs ∧ fault (honestIn q) ≠ tFault.fa

  -- A B-quorum holds an A-quorum of nodes that are neither `fa` nor `fi`.
  paxiom quorum_b_core_quorum :
    ∀ q : set[PLean.MachineRef], hasQuorumB q = true → hasQuorumA (coreIn q) = true

  paxiom quorum_b_core_members :
    ∀ (q : set[PLean.MachineRef]) (r : PLean.MachineRef), hasQuorumB q = true →
      r ∈ coreIn q → r ∈ q ∧ fault r ≠ tFault.fa ∧ fault r ≠ tFault.fi

  machine Node {
    var rcv_init : Bool
    var accept   : Bool
    var rcvd     : set[PLean.MachineRef]

    start state Run {
      entry {
        if rcv_init = true then do
          send this.ref, eInit
        if fault this.ref = tFault.fs ∨ fault this.ref = tFault.fa then do
          send this.ref, eFaultySend
          send this.ref, eFaultyState
      }

      -- `receive_init`, and `receive_init_i` for an `fi` node.
      on eInit {
        if rcv_init = true then do
          foreach (d in nodes)
            invariant init_echoes :
              new_sends e => e is eMsg ∧ (eMsg_payload_of e).sender = this.ref ;
          {
            if fault this.ref ≠ tFault.fi then do
              send d, eMsg, (sender = this.ref)
            else do
              if ($) then
                send d, eMsg, (sender = this.ref)
          }
      }

      -- `receive_msg`, `receive_msg_c_1` / `_c_2` for an `fc` node, `receive_msg_i` for an
      -- `fi` node.
      on eMsg (m : tMsg) {
        rcvd += (m.sender)
        if hasQuorumB rcvd = true then do
          accept = true
        if hasQuorumA rcvd = true then do
          var skip : Bool
          if fault this.ref = tFault.fc then do
            if ($) then
              skip = true
          if skip = false then do
            foreach (d in nodes)
              invariant msg_echoes :
                new_sends e => e is eMsg ∧ (eMsg_payload_of e).sender = this.ref ;
            {
              if fault this.ref ≠ tFault.fi then do
                send d, eMsg, (sender = this.ref)
              else do
                if ($) then
                  send d, eMsg, (sender = this.ref)
            }
      }

      -- `faulty_send_s` for an `fs` node, `faulty_send_a` for an `fa` node.
      on eFaultySend {
        if fault this.ref = tFault.fs ∨ fault this.ref = tFault.fa then do
          foreach (d in nodes)
            invariant faulty_echoes :
              new_sends e => e is eMsg ∧ (eMsg_payload_of e).sender = this.ref ;
          {
            if fault this.ref ≠ tFault.fa then do
              send d, eMsg, (sender = this.ref)
            else do
              if ($) then
                send d, eMsg, (sender = this.ref)
          }
          send this.ref, eFaultySend
      }

      -- `faulty_state_sa`. `rcvd` is built in a local: a loop that assigns a field loses
      -- the machine's state from its frame.
      on eFaultyState {
        if fault this.ref = tFault.fs ∨ fault this.ref = tFault.fa then do
          var kept : set[PLean.MachineRef]
          foreach (r in nodes) {
            if ($) then
              kept += (r)
          }
          rcvd = kept
          if ($) then
            accept = true
          else
            accept = false
          send this.ref, eFaultyState
      }
    }
  }

  init-holds ∀ r : PLean.MachineRef, r ∈ nodes ↔ is_Node r s
  init-holds ∀ n : Node, n.accept = false
  init-holds ∀ (n : Node) (r : PLean.MachineRef), ¬ (r ∈ n.rcvd)

  -- Every broadcast goes to a Node.
  Lemma config {
    invariant nodes_are_nodes : ∀ r : PLean.MachineRef, r ∈ nodes → is_Node r s
  }

  -- A Node sends `eInit` only to itself, and only when it received the init message.
  Lemma routing {
    invariant init_to_node : ∀ e : eInit, sent e s → is_Node e.target s
    invariant init_received :
      ∀ (e : eInit) (n : Node), sent e s → e.target = n.ref → n.rcv_init = true
  }

  -- An echo from a node that is neither `fs` nor `fa`, and a sender such a node counted,
  -- trace back to an init message received by a node that is neither.
  Lemma chain {
    invariant echo_init :
      ∀ e : eMsg, sent e s → fault e.sender ≠ tFault.fs → fault e.sender ≠ tFault.fa →
        ∃ i : eInit, sent i s ∧ fault i.target ≠ tFault.fs ∧ fault i.target ≠ tFault.fa
    invariant rcvd_init :
      ∀ (n : Node) (r : PLean.MachineRef), fault n.ref ≠ tFault.fs → fault n.ref ≠ tFault.fa →
        r ∈ n.rcvd → fault r ≠ tFault.fs → fault r ≠ tFault.fa →
        ∃ i : eInit, sent i s ∧ fault i.target ≠ tFault.fs ∧ fault i.target ≠ tFault.fa
  }

  Theorem safety {
    -- Ivy's `(exists N. ~member_fs(N) & ~member_fa(N) & accept(N)) ->
    --   (exists M. ~member_fs(M) & ~member_fa(M) & rcv_init(M))`.
    invariant accept_needs_init :
      ∀ n : Node, fault n.ref ≠ tFault.fs → fault n.ref ≠ tFault.fa → n.accept = true →
        ∃ m : Node, fault m.ref ≠ tFault.fs ∧ fault m.ref ≠ tFault.fa ∧ m.rcv_init = true
  }

  Proof {
    prove config ;
    prove routing ;
    prove chain ;
    prove safety using chain, routing ;
  }

end HybridReliableBroadcast

#gen_module HybridReliableBroadcast
#pwf        HybridReliableBroadcast

namespace HybridReliableBroadcast
open PLean Std.WP

/-! ## Hand proofs of the step that counts an echo -/

/-- A member of the new `rcvd`, neither `fs` nor `fa`, at a node that is neither, traces back
to an init message. -/
theorem counted_init (g : GW) (i : Nat) (src dst : MachineRef) (pl : eMsg_payload)
    (hchain : chain g)
    (hlog : g.log i = some { src := src, dst := dst, ev := E.eMsg pl })
    (hst : (g.machines dst).state = some S.Node_Run)
    (hdfs : fault dst ≠ tFault.fs) (hdfa : fault dst ≠ tFault.fa)
    (r : MachineRef) (hr : r ∈ ((g.machines dst).fields.Node_rcvd).add pl.sender)
    (hrfs : fault r ≠ tFault.fs) (hrfa : fault r ≠ tFault.fa) :
    ∃ e : Msg E, is_eInit e ∧ sent e g ∧ fault e.target ≠ tFault.fs ∧
      fault e.target ≠ tFault.fa := by
  have h' : (((g.machines dst).fields.Node_rcvd).add pl.sender).mem r = true := hr
  rw [PSet.mem_add, Bool.or_eq_true, decide_eq_true_eq] at h'
  rcases h' with rfl | h
  · exact hchain.1 ⟨i, src, dst, E.eMsg pl⟩ ⟨pl, rfl⟩ hlog hrfs hrfa
  · exact hchain.2 ⟨dst⟩ hst r hdfs hdfa h hrfs hrfa

/-- An A-quorum in the new `rcvd` traces back to an init message (axiom 2 gives a member that
is neither `fs` nor `fa`). -/
theorem quorumA_init (g : GW) (i : Nat) (src dst : MachineRef) (pl : eMsg_payload)
    (hchain : chain g)
    (hlog : g.log i = some { src := src, dst := dst, ev := E.eMsg pl })
    (hst : (g.machines dst).state = some S.Node_Run)
    (hdfs : fault dst ≠ tFault.fs) (hdfa : fault dst ≠ tFault.fa)
    (hA : hasQuorumA (((g.machines dst).fields.Node_rcvd).add pl.sender) = true) :
    ∃ e : Msg E, is_eInit e ∧ sent e g ∧ fault e.target ≠ tFault.fs ∧
      fault e.target ≠ tFault.fa := by
  obtain ⟨hmem, hfs, hfa⟩ := quorum_a_honest _ hA
  exact counted_init g i src dst pl hchain hlog hst hdfs hdfa _ hmem hfs hfa

/-- A B-quorum in the new `rcvd` holds an A-quorum of nodes that are not `fa` (axiom 3), so a
node that is neither `fs` nor `fa` (axiom 2), whose echo traces back to an init message
received by a Node. -/
theorem quorumB_node (g : GW) (i : Nat) (src dst : MachineRef) (pl : eMsg_payload)
    (hchain : chain g) (hrouting : routing g)
    (hlog : g.log i = some { src := src, dst := dst, ev := E.eMsg pl })
    (hst : (g.machines dst).state = some S.Node_Run)
    (hdfs : fault dst ≠ tFault.fs) (hdfa : fault dst ≠ tFault.fa)
    (hB : hasQuorumB (((g.machines dst).fields.Node_rcvd).add pl.sender) = true) :
    ∃ m : MachineRef, (g.machines m).state = some S.Node_Run ∧ fault m ≠ tFault.fs ∧
      fault m ≠ tFault.fa ∧ (g.machines m).fields.Node_rcv_init = true := by
  obtain ⟨hmem, hfs, hfa⟩ := quorum_a_honest _ (quorum_b_core_quorum _ hB)
  obtain ⟨hin, -, -⟩ := quorum_b_core_members _ _ hB hmem
  obtain ⟨e, he, hs, hefs, hefa⟩ :=
    counted_init g i src dst pl hchain hlog hst hdfs hdfa _ hin hfs hfa
  have hn := hrouting.1 e he hs
  exact ⟨e.target, hn, hefs, hefa, hrouting.2 e he ⟨e.target⟩ hn hs rfl⟩

/-! ## The obligations of that step -/

set_option maxHeartbeats 4000000 in
@[pverifyProof] theorem Node.Run.eMsg_correct_chain :
    ∀ (g : GW) (i : Nat) (src dst : MachineRef) (pl : eMsg_payload),
      Framework g → g.error = none →
      chain g →
      g.net i = some { src := src, dst := dst, ev := E.eMsg pl } →
      g.log i = some { src := src, dst := dst, ev := E.eMsg pl } →
      i < g.clock → (g.machines dst).state = some S.Node_Run →
      (g.machines dst).pending = none → (g.machines dst).waiting = none →
      (g.machines dst).halted = false →
      RunPost
        (fun g' => PostInv "echo_init" (echo_init g') ∧ PostInv "rcvd_init" (rcvd_init g'))
        (GWorld.consume g i) dst (g.machines dst) (Node.Run.eMsg_handler pl) := by
  intro g i src dst pl hF hE hInv hnet hlog hi hst
  have key_counted := counted_init g i src dst pl hInv hlog hst
  have key_echo := quorumA_init g i src dst pl hInv hlog hst
  pverify_auto

set_option maxHeartbeats 4000000 in
@[pverifyProof] theorem Node.Run.eMsg_correct_safety :
    ∀ (g : GW) (i : Nat) (src dst : MachineRef) (pl : eMsg_payload),
      Framework g → g.error = none →
      safety g ∧ chain g ∧ routing g →
      g.net i = some { src := src, dst := dst, ev := E.eMsg pl } →
      g.log i = some { src := src, dst := dst, ev := E.eMsg pl } →
      i < g.clock → (g.machines dst).state = some S.Node_Run →
      (g.machines dst).pending = none → (g.machines dst).waiting = none →
      (g.machines dst).halted = false →
      RunPost (fun g' => PostInv "accept_needs_init" (accept_needs_init g'))
        (GWorld.consume g i) dst (g.machines dst) (Node.Run.eMsg_handler pl) := by
  intro g i src dst pl hF hE hInv hnet hlog hi hst
  obtain ⟨hS, hC, hR⟩ := hInv
  have key := quorumB_node g i src dst pl hC hR hlog hst
  clear hC hR
  pverify_auto

end HybridReliableBroadcast

#pverify HybridReliableBroadcast
