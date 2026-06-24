/-
PLean port of [`Tutorial/Advanced/8_LockServer`](../../../Tutorial/Advanced/8_LockServer/PSrc/System.p).

Exercises:
- two machine kinds (`Server`, `Node`) with `m is Server` / `m is Node`,
- multi-lemma `using` chain (`prove safety using system_config`),
- a `pure lock_server() : machine` modelled as an opaque constant.

Both verification blocks port the full invariant set from the P source.
`system_config` carries the topology lemmas (every Server is
`lock_server`, every Node points at it, and where each event may /
may not be in flight); `safety` carries `unique_lock_holder` together
with the mutually-inductive strengthening invariants
(`unique_grant`, `no_lock_while_grant`, `node_server_mutex`, …) that
make it close. All 25 obligations discharge via SMT.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

set_option loom.solver "cvc5"
-- Each obligation now preserves the full invariant bundle (10 in
-- `system_config`, 8 in `safety`), so the per-obligation VC is far
-- larger than the single-invariant skeleton; the 3 s probe budget that
-- the skeleton used returns `unknown` on these. 30 s matches the
-- DistributedLock port.
set_option loom.solver.smt.timeout 30

pmodule LockServer

  type tLockSender   = (sender : PLean.MachineRef)
  type tUnlockSender = (sender : PLean.MachineRef)

  event eLock    : tLockSender
  event eUnlock  : tUnlockSender
  event eGrant
  event eAquire
  event eRelease

  -- P source has `pure lock_server() : machine`; modelled as an
  -- opaque constant (no body).
  function lock_server : Server

  machine Server {
    var has_lock : Bool

    start state Serving {
      on eLock (p : tLockSender) {
        if (has_lock) then do
          has_lock = false;
          send p.sender, eGrant;
      }

      on eUnlock (_p : tUnlockSender) {
        has_lock = true;
      }
    }
  }

  machine Node {
    var has_lock : Bool
    var server   : Server

    start state Working {
      on eAquire {
        send server, eLock, (sender = this.ref)
      }

      on eRelease {
        if (has_lock) then do
          has_lock = false
          send server, eUnlock, (sender = this.ref)
      }

      on eGrant {
        has_lock = true
      }
    }
  }

  init-holds ∀ n : Node, n.server = lock_server
  init-holds ∀ n : Node, n.has_lock = false
  init-holds ∀ sv : Server, sv = lock_server
  init-holds is_Server lock_server s

  -- Topology lemma: every Server is `lock_server`, every Node points at
  -- it, and the routing facts that say where each event may/may not be
  -- in flight. Ported in full from the P source's `system_config`.
  -- `node_send_lock` / `node_send_unlock` are the load-bearing additions
  -- the earlier skeleton dropped: the Server's `eLock` handler forwards a
  -- fresh `eGrant` to the lock's `sender`, so preserving `grant_to_node`
  -- needs to know that sender is a Node (hence not a Server).
  Lemma system_config {
    system s {
      invariant const_server : ∀ n : Node, n.server = lock_server
      invariant serv_is_serv : is_Server lock_server s
      invariant single_server : ∀ sv : Server, sv = lock_server
      invariant unique_server :
        ∀ (m1 m2 : MachineRef),
          is_Server m1 s → is_Server m2 s → m1 = m2

      invariant aquire_to_node :
        ∀ (e : Sig.Label) (mref : MachineRef),
          e is eAquire → (e targets mref) → (is_Server mref s) → ¬ inflight e s

      invariant release_to_node :
        ∀ (e : Sig.Label) (mref : MachineRef),
          e is eRelease → (e targets mref) → (is_Server mref s) → ¬ inflight e s

      invariant grant_to_node :
        ∀ (e : Sig.Label) (mref : MachineRef),
          e is eGrant → (e targets mref) → (is_Server mref s) → ¬ inflight e s

      invariant lock_to_server :
        ∀ (e : Sig.Label) (mref : MachineRef),
          e is eLock → (e targets mref) → (is_Node mref s) → ¬ inflight e s

      invariant unlock_to_server :
        ∀ (e : Sig.Label) (mref : MachineRef),
          e is eUnlock → (e targets mref) → (is_Node mref s) → ¬ inflight e s

      invariant node_send_lock :
        ∀ e : eLock, inflight e s → is_Node (e.sender) s

      invariant node_send_unlock :
        ∀ e : eUnlock, inflight e s → is_Node (e.sender) s
    }
  }
  Proof {
    prove system_config ;
  }

  -- Safety: at most one Node holds the lock. `unique_lock_holder` is not
  -- inductive on its own; it closes only together with the mutually-
  -- inductive strengthening invariants ported from the P source — the
  -- uniqueness of in-flight grants/unlocks, the "no Node/Server holds the
  -- lock while a grant/unlock is in flight" facts, and the Node/Server
  -- mutex. Each is dropped by the earlier skeleton.
  Theorem safety {
    system s {
      invariant unique_lock_holder :
        ∀ n1 n2 : Node,
          n1.has_lock = true →
          n2.has_lock = true →
          n1 = n2

      invariant unique_grant :
        ∀ e1 e2 : eGrant, inflight e1 s → inflight e2 s → e1 = e2

      invariant unique_unlock :
        ∀ e1 e2 : eUnlock, inflight e1 s → inflight e2 s → e1 = e2

      invariant grant_server_unlocked :
        ∀ (e : eGrant) (sv : Server), inflight e s → sv.has_lock = false

      invariant no_lock_while_grant :
        ∀ (e : eGrant) (n : Node), inflight e s → n.has_lock = false

      invariant no_lock_while_unlock :
        ∀ (e : eUnlock) (n : Node) (sv : Server),
          inflight e s → n.has_lock = false ∧ sv.has_lock = false

      invariant grant_not_unlock :
        ∀ (e1 : eGrant) (e2 : eUnlock), ¬ (inflight e1 s ∧ inflight e2 s)

      invariant node_server_mutex :
        ∀ (n : Node) (sv : Server), ¬ (n.has_lock = true ∧ sv.has_lock = true)
    }
  }
  Proof {
    prove safety using system_config ;
    prove default using system_config ;
  }

end LockServer

#gen_module LockServer
#pwf        LockServer

namespace LockServer
open PartialCorrectness DemonicChoice

/-! ## Manual proofs for the three send-handler `system_config` obligations.

The Server `eLock` and Node `eAquire`/`eRelease` handlers each `send` a
fresh event, so preserving the routing invariants (`*_to_*`,
`node_send_*`) requires reasoning about the freshly-built label's payload
and target. SMT returns `unknown` on the full 11-invariant bundle as a
single query (the opaque payload extractor under the `∀ e` binder), so
these three are discharged by hand: split the bundle into its conjuncts,
and for each do a new-vs-old-label case analysis. The fresh label's
payload is computed via the `#gen_module`-emitted `<ev>_payload_of_mk` /
`_spec` characterisations; topology clauses transfer because the
field-only machine update preserves every machine's kind / control
state / `Node_server`. Every other LockServer obligation closes by SMT. -/

-- eAquire: Node sends eLock to its server (= lock_server, a Server).
set_option maxHeartbeats 8000000 in
@[pverifyProof]
theorem Node.Working.eAquire_correct_block0_system_config (this : Node) (lbl : Sig.Label) :
    triple (l := PProp Sig)
      (fun s => (system_config s) ∧ inflight lbl s ∧ lbl.target = this.ref ∧
        is_Node this.ref s ∧ (s.machines this.ref).currentState = Node.Working_st ∧
        lbl.action = .event E.eAquire)
      (do PLean.markReceived (P := Sig) lbl; Node.Working.eAquire_handler this)
      (fun _ s => system_config s) := by
  unfold Node.Working.eAquire_handler
  pverify_step_wp
  intro s hpre
  simp only [system_config, const_server, serv_is_serv, single_server, unique_server,
    aquire_to_node, release_to_node, grant_to_node, lock_to_server,
    unlock_to_server, node_send_lock, node_send_unlock, inflight] at hpre ⊢
  obtain ⟨hConst, hServ, hSingle, hUniq, hAcq, hRel, hGrant, hLock, hUnlock, hNSL, hNSU⟩ := hpre
  intro hInflLbl hTgtThis hThisKind hStThis hActThis
  refine ⟨?cn, ?ss, ?sgl, ?uq, ?aq, ?rel, ?gr, ?lk, ?ulk, ?nsl, ?nsu⟩
  -- Handler only `send`s an eLock: machines unchanged; topology clauses
  -- transfer verbatim; the only new label is an eLock so non-eLock
  -- routing clauses fall to the pre-state.
  case cn | ss | sgl | uq => assumption
  case aq  => intro e m hisE hTgt hm; pverify_not_inflight (hAcq e m hisE hTgt hm), hisE, is_eAquire
  case rel => intro e m hisE hTgt hm; pverify_not_inflight (hRel e m hisE hTgt hm), hisE, is_eRelease
  case gr  => intro e m hisE hTgt hm; pverify_not_inflight (hGrant e m hisE hTgt hm), hisE, is_eGrant
  case ulk => intro e m hisE hTgt hm; pverify_not_inflight (hUnlock e m hisE hTgt hm), hisE, is_eUnlock
  case lk =>
    -- lock_to_server: the new eLock targets lock_server (a Server), so a
    -- Node-target `m` ⟹ old label ⟹ pre-state hLock.
    intro e m hisE hTgt hm
    simp only [is_Node, Node_allocated, Node_kind] at hm
    rw [not_and, Bool.or_eq_true, decide_eq_true_eq]
    intro hsent
    rcases hsent with hNew | hOld
    · subst hNew
      simp only [Label.targets?] at hTgt
      simp only [is_Node, Node_allocated, Node_kind] at hThisKind
      have hcs := hConst this hThisKind
      exfalso; rw [← hTgt, hcs] at hm
      simp only [is_Server, Server_allocated, Server_kind] at hServ; omega
    · rw [Bool.or_eq_false_iff, decide_eq_false_iff_not]
      have hpre7 := hLock e m hisE hTgt
        (by simp only [is_Node, Node_allocated, Node_kind]; exact hm)
      simp only [not_and] at hpre7
      intro hrecv; exact absurd (hpre7 hOld) (by simp [hrecv.2])
  case nsl =>
    -- node_send_lock: new eLock's sender is `this` (a Node).
    intro e hisE hsent
    obtain ⟨hsent1, hrecv⟩ := hsent
    rw [Bool.or_eq_true, decide_eq_true_eq] at hsent1
    simp only [Bool.or_eq_false_iff, decide_eq_false_iff_not] at hrecv
    rcases hsent1 with hNew | hOld
    · subst hNew; rw [eLock_payload_of_mk]
      simp only [is_Node, Node_allocated, Node_kind] at hThisKind ⊢; exact hThisKind
    · have := hNSL e hisE ⟨hOld, hrecv.2⟩; simpa using this
  case nsu =>
    -- node_send_unlock: new label is an eLock, fails is_eUnlock.
    intro e hisE hsent
    obtain ⟨hsent1, hrecv⟩ := hsent
    rw [Bool.or_eq_true, decide_eq_true_eq] at hsent1
    simp only [Bool.or_eq_false_iff, decide_eq_false_iff_not] at hrecv
    rcases hsent1 with hNew | hOld
    · subst hNew; simp only [is_eUnlock] at hisE
    · have := hNSU e hisE ⟨hOld, hrecv.2⟩; simpa using this

-- eLock: Server, `if has_lock then has_lock=false; send p.sender eGrant`.
set_option maxHeartbeats 8000000 in
@[pverifyProof]
theorem Server.Serving.eLock_correct_block0_system_config (this : Server) (param : eLock_payload) (lbl : Sig.Label) :
    triple (l := PProp Sig)
      (fun s => (system_config s) ∧ inflight lbl s ∧ lbl.target = this.ref ∧
        is_Server this.ref s ∧ (s.machines this.ref).currentState = Server.Serving_st ∧
        lbl.action = .event (E.eLock param))
      (do PLean.markReceived (P := Sig) lbl; Server.Serving.eLock_handler this param)
      (fun _ s => system_config s) := by
  unfold Server.Serving.eLock_handler
  pverify_step_wp
  intro s hpre
  simp only [system_config, const_server, serv_is_serv, single_server, unique_server,
    aquire_to_node, release_to_node, grant_to_node, lock_to_server,
    unlock_to_server, node_send_lock, node_send_unlock, inflight] at hpre ⊢
  obtain ⟨hConst, hServ, hSingle, hUniq, hAcq, hRel, hGrant, hLock, hUnlock, hNSL, hNSU⟩ := hpre
  intro hInflLbl hTgtThis hThisKind hStThis hActThis
  -- node_send_lock on the consumed eLock ⟹ its sender (param.sender) is a Node.
  have hIsLock : is_eLock lbl := by simp only [is_eLock, hActThis]
  have hParamNode : is_Node param.sender s := by
    have h := hNSL lbl hIsLock hInflLbl
    rwa [eLock_payload_of_spec lbl param hActThis] at h
  refine ⟨?thenB, ?elseB⟩
  · -- then-branch: state Server_has_lock:=false (field-only; preserves
    -- kind/currentState/Node_server), sends fresh eGrant to param.sender.
    intro _hcond
    refine ⟨?cn, ?ss, ?sgl, ?uq, ?aq, ?rel, ?gr, ?lk, ?ulk, ?nsl, ?nsu⟩
    -- Machine update is field-only (Server_has_lock); kind / currentState
    -- / Node_server are preserved, so a post-state `is_<M> m` guard
    -- bridges to the pre-state form by `by_cases m = this.ref`.
    case cn =>
      intro n hn
      pverify_machine_has_type hnPre : Node n.ref from hn
      have hcn := hConst n hnPre
      by_cases hnt : n.ref = this.ref <;> simp_all
    case ss  => pverify_machine_has_type Server on lock_server.ref
    case sgl => intro sv hsv; apply hSingle
                pverify_machine_has_type Server on sv.ref
    case uq  => intro m1 m2 hm1 hm2; apply hUniq m1 m2
                · pverify_machine_has_type Server on m1
                · pverify_machine_has_type Server on m2
    -- 4 routing clauses with a wrong-event new label: field-only update
    -- + kind-bridge + pre-state hypothesis (see Tactic.lean).
    case aq  => pverify_not_inflight_by Server, hAcq,    is_eAquire
    case rel => pverify_not_inflight_by Server, hRel,    is_eRelease
    case ulk => pverify_not_inflight_by Node,   hUnlock, is_eUnlock
    case lk  => pverify_not_inflight_by Node,   hLock,   is_eLock
    case gr =>
      -- grant_to_node: new eGrant targets param.sender (a Node), so a
      -- Server-target ⟹ old label ⟹ pre hGrant.
      intro e m hisE hTgt hm
      rw [not_and, Bool.or_eq_true, decide_eq_true_eq]
      intro hsent
      rcases hsent with hNew | hOld
      · subst hNew
        simp only [Label.targets?] at hTgt
        exfalso
        rw [← hTgt] at hm
        simp only [is_Server, Server_allocated, Server_kind] at hm
        simp only [is_Node, Node_allocated, Node_kind] at hParamNode
        by_cases hps : param.sender = this.ref <;> simp_all
      · rw [Bool.or_eq_false_iff, decide_eq_false_iff_not]
        pverify_machine_has_type hmPre : Server m from hm
        have := hGrant e m hisE hTgt hmPre
        simp only [not_and] at this
        intro hrecv; exact absurd (this hOld) (by simp [hrecv.2])
    -- node_send_{lock,unlock}: new label is eGrant; fails the event tag
    -- on both clauses, so old labels carry to the pre-state hypothesis.
    case nsl =>
      intro e hisE hsent
      obtain ⟨hsent1, hrecv⟩ := hsent
      rw [Bool.or_eq_true, decide_eq_true_eq] at hsent1
      simp only [Bool.or_eq_false_iff, decide_eq_false_iff_not] at hrecv
      rcases hsent1 with hNew | hOld
      · subst hNew; simp only [is_eLock] at hisE
      · have hpre := hNSL e hisE ⟨hOld, hrecv.2⟩
        simp only [is_Node, Node_allocated, Node_kind] at hpre ⊢
        by_cases hpt : (eLock_payload_of e).sender = this.ref <;> simp_all
    case nsu =>
      intro e hisE hsent
      obtain ⟨hsent1, hrecv⟩ := hsent
      rw [Bool.or_eq_true, decide_eq_true_eq] at hsent1
      simp only [Bool.or_eq_false_iff, decide_eq_false_iff_not] at hrecv
      rcases hsent1 with hNew | hOld
      · subst hNew; simp only [is_eUnlock] at hisE
      · have hpre := hNSU e hisE ⟨hOld, hrecv.2⟩
        simp only [is_Node, Node_allocated, Node_kind] at hpre ⊢
        by_cases hpt : (eUnlock_payload_of e).sender = this.ref <;> simp_all
  · -- else-branch: machines unchanged, only `received[lbl]:=true`.
    intro _hcond
    refine ⟨?cn, ?ss, ?sgl, ?uq, ?aq, ?rel, ?gr, ?lk, ?ulk, ?nsl, ?nsu⟩
    case cn | ss | sgl | uq => assumption
    case aq  => intro e m hisE hTgt hm; pverify_carry_after_recv (hAcq e m hisE hTgt hm)
    case rel => intro e m hisE hTgt hm; pverify_carry_after_recv (hRel e m hisE hTgt hm)
    case gr  => intro e m hisE hTgt hm; pverify_carry_after_recv (hGrant e m hisE hTgt hm)
    case lk  => intro e m hisE hTgt hm; pverify_carry_after_recv (hLock e m hisE hTgt hm)
    case ulk => intro e m hisE hTgt hm; pverify_carry_after_recv (hUnlock e m hisE hTgt hm)
    case nsl => intro e hisE hsent
                exact hNSL e hisE ⟨hsent.1, by simp only [Bool.or_eq_false_iff] at hsent; exact hsent.2.2⟩
    case nsu => intro e hisE hsent
                exact hNSU e hisE ⟨hsent.1, by simp only [Bool.or_eq_false_iff] at hsent; exact hsent.2.2⟩

-- eRelease: Node, `if has_lock then has_lock=false; send server eUnlock`.
set_option maxHeartbeats 8000000 in
@[pverifyProof]
theorem Node.Working.eRelease_correct_block0_system_config (this : Node) (lbl : Sig.Label) :
    triple (l := PProp Sig)
      (fun s => (system_config s) ∧ inflight lbl s ∧ lbl.target = this.ref ∧
        is_Node this.ref s ∧ (s.machines this.ref).currentState = Node.Working_st ∧
        lbl.action = .event E.eRelease)
      (do PLean.markReceived (P := Sig) lbl; Node.Working.eRelease_handler this)
      (fun _ s => system_config s) := by
  unfold Node.Working.eRelease_handler
  pverify_step_wp
  intro s hpre
  simp only [system_config, const_server, serv_is_serv, single_server, unique_server,
    aquire_to_node, release_to_node, grant_to_node, lock_to_server,
    unlock_to_server, node_send_lock, node_send_unlock, inflight] at hpre ⊢
  obtain ⟨hConst, hServ, hSingle, hUniq, hAcq, hRel, hGrant, hLock, hUnlock, hNSL, hNSU⟩ := hpre
  intro hInflLbl hTgtThis hThisKind hStThis hActThis
  -- `this` (a Node) points at lock_server (a Server); the new eUnlock targets it.
  have hThisNodePre : is_Node this.ref s := hThisKind
  have hcs : (s.machines this.ref).fields.Node_server = lock_server := hConst this hThisKind
  refine ⟨?thenB, ?elseB⟩
  · -- then-branch: Node_has_lock:=false (field-only), sends eUnlock to server.
    intro _hcond
    refine ⟨?cn, ?ss, ?sgl, ?uq, ?aq, ?rel, ?gr, ?lk, ?ulk, ?nsl, ?nsu⟩
    case cn =>
      intro n hn
      pverify_machine_has_type hnPre : Node n.ref from hn
      have hcn := hConst n hnPre
      by_cases hnt : n.ref = this.ref <;> simp_all
    case ss  => pverify_machine_has_type Server on lock_server.ref
    case sgl => intro sv hsv; apply hSingle
                pverify_machine_has_type Server on sv.ref
    case uq  => intro m1 m2 hm1 hm2; apply hUniq m1 m2
                · pverify_machine_has_type Server on m1
                · pverify_machine_has_type Server on m2
    -- 4 routing clauses with a wrong-event new label (the new eUnlock
    -- mismatches eAquire/eRelease/eGrant/eLock): field-only + kind-bridge.
    case aq  => pverify_not_inflight_by Server, hAcq,   is_eAquire
    case rel => pverify_not_inflight_by Server, hRel,   is_eRelease
    case gr  => pverify_not_inflight_by Server, hGrant, is_eGrant
    case lk  => pverify_not_inflight_by Node,   hLock,  is_eLock
    case ulk =>
      -- unlock_to_server: new eUnlock targets server = lock_server (a
      -- Server); a Node-target ⟹ old → pre hUnlock.
      intro e m hisE hTgt hm
      pverify_machine_has_type hmPre : Node m from hm
      rw [not_and, Bool.or_eq_true, decide_eq_true_eq]
      intro hsent
      rcases hsent with hNew | hOld
      · subst hNew
        simp only [Label.targets?] at hTgt
        exfalso
        rw [← hTgt, hcs] at hmPre
        simp only [is_Node, Node_allocated, Node_kind] at hmPre
        simp only [is_Server, Server_allocated, Server_kind] at hServ
        omega
      · rw [Bool.or_eq_false_iff, decide_eq_false_iff_not]
        have := hUnlock e m hisE hTgt hmPre
        simp only [not_and] at this
        intro hrecv; exact absurd (this hOld) (by simp [hrecv.2])
    -- node_send_{lock,unlock}: new label is eUnlock. nsl: fails is_eLock
    -- on the new label. nsu: new label's sender is `this` (a Node).
    case nsl =>
      intro e hisE hsent
      obtain ⟨hsent1, hrecv⟩ := hsent
      rw [Bool.or_eq_true, decide_eq_true_eq] at hsent1
      simp only [Bool.or_eq_false_iff, decide_eq_false_iff_not] at hrecv
      rcases hsent1 with hNew | hOld
      · subst hNew; simp only [is_eLock] at hisE
      · have hpre := hNSL e hisE ⟨hOld, hrecv.2⟩
        simp only [is_Node, Node_allocated, Node_kind] at hpre ⊢
        by_cases hpt : (eLock_payload_of e).sender = this.ref <;> simp_all
    case nsu =>
      intro e hisE hsent
      obtain ⟨hsent1, hrecv⟩ := hsent
      rw [Bool.or_eq_true, decide_eq_true_eq] at hsent1
      simp only [Bool.or_eq_false_iff, decide_eq_false_iff_not] at hrecv
      rcases hsent1 with hNew | hOld
      · subst hNew
        rw [eUnlock_payload_of_mk]
        simp only [is_Node, Node_allocated, Node_kind] at hThisKind ⊢
        by_cases hpt : this.ref = this.ref <;> simp_all
      · have hpre := hNSU e hisE ⟨hOld, hrecv.2⟩
        simp only [is_Node, Node_allocated, Node_kind] at hpre ⊢
        by_cases hpt : (eUnlock_payload_of e).sender = this.ref <;> simp_all
  · -- else-branch: machines unchanged, only `received[lbl]:=true`.
    intro _hcond
    refine ⟨?cn, ?ss, ?sgl, ?uq, ?aq, ?rel, ?gr, ?lk, ?ulk, ?nsl, ?nsu⟩
    case cn | ss | sgl | uq => assumption
    case aq  => intro e m hisE hTgt hm; pverify_carry_after_recv (hAcq e m hisE hTgt hm)
    case rel => intro e m hisE hTgt hm; pverify_carry_after_recv (hRel e m hisE hTgt hm)
    case gr  => intro e m hisE hTgt hm; pverify_carry_after_recv (hGrant e m hisE hTgt hm)
    case lk  => intro e m hisE hTgt hm; pverify_carry_after_recv (hLock e m hisE hTgt hm)
    case ulk => intro e m hisE hTgt hm; pverify_carry_after_recv (hUnlock e m hisE hTgt hm)
    case nsl => intro e hisE hsent
                exact hNSL e hisE ⟨hsent.1, by simp only [Bool.or_eq_false_iff] at hsent; exact hsent.2.2⟩
    case nsu => intro e hisE hsent
                exact hNSU e hisE ⟨hsent.1, by simp only [Bool.or_eq_false_iff] at hsent; exact hsent.2.2⟩

end LockServer

set_option maxHeartbeats 4000000 in
#pverify LockServer
