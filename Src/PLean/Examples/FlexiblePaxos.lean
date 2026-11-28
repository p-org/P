/-
Flexible Paxos: single-decree Paxos in which the quorums of the two phases
are drawn from two quorum systems, and only a phase-1 quorum and a phase-2
quorum need to share an Acceptor. Two phase-1 quorums may be disjoint, and
so may two phase-2 quorums.

A Proposer asks the acceptors to promise its ballot (`ePrepareReq`). Once
the Acceptors that promised form a phase-1 quorum, it proposes the value of
the highest ballot they reported having accepted, or else its own
(`eAcceptReq`). Once the Acceptors that accepted form a phase-2 quorum, it
teaches the value to the learners (`eLearn`).

The quorum systems are the predicates `isPhase1Quorum` and
`isPhase2Quorum`, of which initial worlds assume only that a phase-1 and a
phase-2 quorum intersect. Since two phase-1 quorums need not intersect, two
Proposers of one ballot could both complete phase 1 and propose different
values: ballots are distinct, one per Proposer.

Safety is the Paxos tutorial's `OneValueTaught`: every `eLearn` carries the
same value, which is not `-1`. So the Learners' assertions hold, and Learners
that learned agree.

The invariants are in small lemmas with short `using` lists. The two steps in
which a Proposer completes a quorum are proved by hand, after `#gen_module`,
as lemmas about one world (`promise_quorum_agrees`, `vote_quorum_agrees`)
that the obligations of those steps hand to `pverify_auto`.
-/
import PLean

open PLean

pmodule FlexiblePaxos

  system s

  type tPrepareReq = (proposer : PLean.MachineRef, ballot_n : Int, v : Int)
  type tPrepareRsp = (acceptor : PLean.MachineRef, promised : Int, v_accepted : Int,
                      n_accepted : Int)
  type tAcceptReq  = (proposer : PLean.MachineRef, ballot_n : Int, v : Int)
  type tAcceptRsp  = (acceptor : PLean.MachineRef, accepted : Int)
  type tLearn      = (ballot : Int, v : Int)

  event ePrepareReq : tPrepareReq
  event ePrepareRsp : tPrepareRsp
  event eAcceptReq  : tAcceptReq
  event eAcceptRsp  : tAcceptRsp
  event eLearn      : tLearn

  function acceptors      : set[PLean.MachineRef]
  function learners       : set[PLean.MachineRef]
  function isPhase1Quorum : set[PLean.MachineRef] → Bool
  function isPhase2Quorum : set[PLean.MachineRef] → Bool

  machine Acceptor {
    var n_prepared : Int
    var v_accepted : Int
    var n_accepted : Int

    start state Init {
      entry {
        n_prepared = -1
        v_accepted = -1
        n_accepted = -1
        goto Accept
      }
    }

    state Accept {
      on ePrepareReq (req : tPrepareReq) {
        if req.ballot_n > n_prepared then do
          send req.proposer, ePrepareRsp,
            (acceptor = this.ref, promised = req.ballot_n, v_accepted = v_accepted,
             n_accepted = n_accepted)
          n_prepared = req.ballot_n
      }

      on eAcceptReq (req : tAcceptReq) {
        if req.ballot_n ≥ n_prepared then do
          v_accepted = req.v
          n_accepted = req.ballot_n
          n_prepared = req.ballot_n
          send req.proposer, eAcceptRsp, (acceptor = this.ref, accepted = req.ballot_n)
      }
    }
  }

  machine Learner {
    var learned_value : Int

    start state Init {
      entry {
        learned_value = -1
        goto Learn
      }
    }

    state Learn {
      on eLearn (payload : tLearn) {
        assert (payload.v ≠ -1)
        assert (learned_value = -1 ∨ learned_value = payload.v)
        learned_value = payload.v
      }
    }
  }

  machine Proposer {
    var ballot_n           : Int
    var highest_proposal_n : Int
    var value_to_propose   : Int
    var prepare_acks       : set[PLean.MachineRef]
    var accept_acks        : set[PLean.MachineRef]

    start state Prepare {
      entry {
        highest_proposal_n = -1
        foreach (acceptor in acceptors)
          invariant prepare_reqs :
            new_sends e => e is ePrepareReq ∧ e.target ∈ acceptors ∧
              (ePrepareReq_payload_of e).proposer = this.ref ;
        {
          send acceptor, ePrepareReq,
            (proposer = this.ref, ballot_n = ballot_n, v = value_to_propose)
        }
      }

      on ePrepareRsp (rsp : tPrepareRsp) {
        if rsp.promised = ballot_n then do
          if rsp.n_accepted > highest_proposal_n then do
            highest_proposal_n = rsp.n_accepted
            value_to_propose = rsp.v_accepted
          prepare_acks += (rsp.acceptor)
          if isPhase1Quorum prepare_acks = true then do
            goto Accept
      }
    }

    state Accept {
      entry {
        foreach (acceptor in acceptors)
          invariant accept_reqs :
            new_sends e => e is eAcceptReq ∧ e.target ∈ acceptors ∧
              (eAcceptReq_payload_of e).proposer = this.ref ∧
              (eAcceptReq_payload_of e).ballot_n = ballot_n ∧
              (eAcceptReq_payload_of e).v = value_to_propose ;
        {
          send acceptor, eAcceptReq,
            (proposer = this.ref, ballot_n = ballot_n, v = value_to_propose)
        }
      }

      on eAcceptRsp (rsp : tAcceptRsp) {
        if rsp.accepted = ballot_n then do
          accept_acks += (rsp.acceptor)
          if isPhase2Quorum accept_acks = true then do
            goto Teach
      }

      ignore ePrepareRsp
    }

    state Teach {
      entry {
        foreach (learner in learners)
          invariant learns :
            new_sends e => e is eLearn ∧ e.target ∈ learners ∧ e.src = this.ref ∧
              (eLearn_payload_of e).ballot = ballot_n ∧
              (eLearn_payload_of e).v = value_to_propose ;
        {
          send learner, eLearn, (ballot = ballot_n, v = value_to_propose)
        }
      }

      ignore eAcceptRsp, ePrepareRsp
    }
  }

  init-holds ∀ r : PLean.MachineRef, r ∈ acceptors → is_Acceptor r s
  init-holds ∀ r : PLean.MachineRef, r ∈ learners → is_Learner r s
  init-holds ∀ (p : Proposer) (a : PLean.MachineRef), ¬ (a ∈ p.prepare_acks)
  init-holds ∀ (p : Proposer) (a : PLean.MachineRef), ¬ (a ∈ p.accept_acks)
  init-holds ∀ p : Proposer, p.value_to_propose ≠ -1
  init-holds ∀ p1 p2 : Proposer, p1.ballot_n = p2.ballot_n → p1 = p2
  init-holds ∀ q1 q2 : set[PLean.MachineRef],
    isPhase1Quorum q1 = true → isPhase2Quorum q2 = true → ∃ a, a ∈ q1 ∧ a ∈ q2

  -- The acceptors and the learners, and where each kind of message goes.
  Lemma system_config {
    invariant acceptor_set : ∀ r : PLean.MachineRef, r ∈ acceptors → is_Acceptor r s
    invariant learner_set  : ∀ r : PLean.MachineRef, r ∈ learners → is_Learner r s
    invariant prepare_req_routing :
      ∀ e : ePrepareReq, sent e s →
        e.target ∈ acceptors ∧ is_Acceptor e.target s ∧ is_Proposer e.proposer s
    invariant prepare_rsp_routing :
      ∀ e : ePrepareRsp, sent e s → is_Proposer e.target s ∧ is_Acceptor e.acceptor s
    invariant accept_req_routing :
      ∀ e : eAcceptReq, sent e s →
        e.target ∈ acceptors ∧ is_Acceptor e.target s ∧ is_Proposer e.proposer s
    invariant accept_rsp_routing :
      ∀ e : eAcceptRsp, sent e s → is_Proposer e.target s ∧ is_Acceptor e.acceptor s
    invariant learn_routing : ∀ e : eLearn, sent e s → is_Learner e.target s
    invariant learn_source  : ∀ e : eLearn, sent e s → is_Proposer e.src s
  }

  -- What an Acceptor's promises (`ePrepareRsp`) and votes (`eAcceptRsp`) say
  -- about its state.
  Lemma acceptor_state {
    invariant acceptor_bounds :
      ∀ a : Acceptor, stateOf a.ref s = some Acceptor.Accept_st →
        -1 ≤ a.n_accepted ∧ a.n_accepted ≤ a.n_prepared
    invariant promise_recorded :
      ∀ (e : ePrepareRsp) (a : Acceptor), sent e s → e.acceptor = a.ref →
        stateOf a.ref s = some Acceptor.Accept_st ∧ e.promised ≤ a.n_prepared
    invariant promise_shape :
      ∀ e : ePrepareRsp, sent e s → -1 ≤ e.n_accepted ∧ e.n_accepted < e.promised
    invariant vote_recorded :
      ∀ (e : eAcceptRsp) (a : Acceptor), sent e s → e.acceptor = a.ref →
        stateOf a.ref s = some Acceptor.Accept_st ∧ e.accepted ≤ a.n_accepted
  }

  -- A promise reports the Acceptor's last vote below the promised ballot.
  Lemma gap {
    invariant promise_gap :
      ∀ (e1 : ePrepareRsp) (e2 : eAcceptRsp), sent e1 s → sent e2 s →
        e1.acceptor = e2.acceptor → e2.accepted < e1.promised → e2.accepted ≤ e1.n_accepted
  }

  -- One Proposer per ballot.
  Lemma ballots {
    invariant ballots_distinct : ∀ p1 p2 : Proposer, p1.ballot_n = p2.ballot_n → p1 = p2
  }

  -- A Proposer has proposed once it is in `Accept` or `Teach`: the Acceptors
  -- that promised its ballot form a phase-1 quorum.
  Lemma tally {
    invariant proposed_quorum :
      ∀ p : Proposer,
        (stateOf p.ref s = some Proposer.Accept_st ∨ stateOf p.ref s = some Proposer.Teach_st) →
        isPhase1Quorum p.prepare_acks = true
  }

  -- The promises a Proposer has counted.
  Lemma counted {
    -- None is counted before `Prepare`'s entry has run.
    invariant acks_fresh :
      ∀ (p : Proposer) (a : PLean.MachineRef), (s.machines p.ref).pending ≠ none →
        stateOf p.ref s = some Proposer.Prepare_st → ¬ (a ∈ p.prepare_acks)
    invariant prepare_acks_promised :
      ∀ (p : Proposer) (a : PLean.MachineRef), a ∈ p.prepare_acks →
        ∃ e : ePrepareRsp, sent e s ∧ e.target = p.ref ∧ e.acceptor = a ∧
          e.promised = p.ballot_n ∧ e.n_accepted ≤ p.highest_proposal_n
    invariant highest_bound :
      ∀ p : Proposer,
        ((stateOf p.ref s = some Proposer.Prepare_st ∧ (s.machines p.ref).pending = none) ∨
          stateOf p.ref s = some Proposer.Accept_st ∨ stateOf p.ref s = some Proposer.Teach_st) →
        -1 ≤ p.highest_proposal_n
    -- The highest ballot reported is below the Proposer's own.
    invariant highest_below :
      ∀ p : Proposer,
        ((stateOf p.ref s = some Proposer.Prepare_st ∧ (s.machines p.ref).pending = none) ∨
          stateOf p.ref s = some Proposer.Accept_st ∨ stateOf p.ref s = some Proposer.Teach_st) →
        p.highest_proposal_n ≠ -1 → p.highest_proposal_n < p.ballot_n
  }

  -- Once a Proposer has proposed, its ballot and value are fixed; requests
  -- and votes carry them.
  Lemma proposed {
    invariant proposed_ballot :
      ∀ p : Proposer,
        (stateOf p.ref s = some Proposer.Accept_st ∨ stateOf p.ref s = some Proposer.Teach_st) →
        0 ≤ p.ballot_n
    invariant accept_req_from : ∀ e : eAcceptReq, sent e s → is_Proposer e.proposer s
    invariant accept_req_proposed :
      ∀ (e : eAcceptReq) (p : Proposer), sent e s → e.proposer = p.ref →
        (stateOf p.ref s = some Proposer.Accept_st ∨ stateOf p.ref s = some Proposer.Teach_st) ∧
        e.ballot_n = p.ballot_n ∧ e.v = p.value_to_propose
    -- A vote goes to the Proposer of its ballot.
    invariant vote_for_proposed :
      ∀ (e : eAcceptRsp) (p : Proposer), sent e s → e.target = p.ref →
        (stateOf p.ref s = some Proposer.Accept_st ∨ stateOf p.ref s = some Proposer.Teach_st) ∧
        e.accepted = p.ballot_n
  }

  -- The votes a Proposer has counted.
  Lemma votes {
    invariant accept_acks_voted :
      ∀ (p : Proposer) (a : PLean.MachineRef), a ∈ p.accept_acks →
        ∃ e : eAcceptRsp, sent e s ∧ e.target = p.ref ∧ e.acceptor = a ∧
          e.accepted = p.ballot_n
    invariant teach_quorum :
      ∀ p : Proposer, stateOf p.ref s = some Proposer.Teach_st →
        isPhase2Quorum p.accept_acks = true
  }

  -- An accepted value, a reported one, and the one a Proposer adopted were
  -- proposed (in an `eAcceptReq`) at that ballot.
  Lemma reported {
    invariant accepted_proposed :
      ∀ a : Acceptor, stateOf a.ref s = some Acceptor.Accept_st → 0 ≤ a.n_accepted →
        ∃ e : eAcceptReq, sent e s ∧ e.ballot_n = a.n_accepted ∧ e.v = a.v_accepted
    invariant promise_reports_proposed :
      ∀ e : ePrepareRsp, sent e s → 0 ≤ e.n_accepted →
        ∃ e' : eAcceptReq, sent e' s ∧ e'.ballot_n = e.n_accepted ∧ e'.v = e.v_accepted
    invariant highest_proposed :
      ∀ p : Proposer,
        ((stateOf p.ref s = some Proposer.Prepare_st ∧ (s.machines p.ref).pending = none) ∨
          stateOf p.ref s = some Proposer.Accept_st ∨ stateOf p.ref s = some Proposer.Teach_st) →
        0 ≤ p.highest_proposal_n →
        ∃ e : eAcceptReq, sent e s ∧ e.ballot_n = p.highest_proposal_n ∧
          e.v = p.value_to_propose
  }

  -- `-1` stands for no value.
  Lemma values {
    invariant value_set : ∀ p : Proposer, p.value_to_propose ≠ -1
  }

  -- A phase-1 quorum and a phase-2 quorum share an Acceptor. This says
  -- nothing about the state: initial worlds assume it.
  Lemma quorum {
    invariant quorum_intersection :
      ∀ q1 q2 : set[PLean.MachineRef],
        isPhase1Quorum q1 = true → isPhase2Quorum q2 = true → ∃ a, a ∈ q1 ∧ a ∈ q2
  }

  -- An Acceptor that promised a ballot above a Proposer's, reporting no vote
  -- from that Proposer's ballot on, is not among those it counts as voters.
  Lemma blocking {
    invariant no_skip :
      ∀ (e : ePrepareRsp) (p : Proposer), sent e s →
        e.n_accepted < p.ballot_n → p.ballot_n < e.promised → ¬ (e.acceptor ∈ p.accept_acks)
  }

  -- A Proposer whose value a phase-2 quorum accepted has decided. Every
  -- proposal at a higher ballot is for its value, so Proposers that decided
  -- agree.
  Lemma agreement {
    invariant decided_blocks :
      ∀ p1 p2 : Proposer, stateOf p1.ref s = some Proposer.Teach_st →
        (stateOf p2.ref s = some Proposer.Accept_st ∨ stateOf p2.ref s = some Proposer.Teach_st) →
        p1.ballot_n < p2.ballot_n → p1.value_to_propose = p2.value_to_propose
    invariant decided_agree :
      ∀ p1 p2 : Proposer,
        stateOf p1.ref s = some Proposer.Teach_st → stateOf p2.ref s = some Proposer.Teach_st →
        p1.value_to_propose = p2.value_to_propose
  }

  Theorem safety {
    invariant learn_decided :
      ∀ (e : eLearn) (p : Proposer), sent e s → e.src = p.ref →
        stateOf p.ref s = some Proposer.Teach_st ∧ e.v = p.value_to_propose
    -- P's `OneValueTaught`: a value is taught, and only one.
    invariant taught_value_set : ∀ e : eLearn, sent e s → e.v ≠ -1
    invariant one_value_taught :
      ∀ e1 e2 : eLearn, sent e1 s → sent e2 s → e1.v = e2.v
    invariant learned_was_taught :
      ∀ l : Learner, stateOf l.ref s = some Learner.Learn_st → l.learned_value ≠ -1 →
        ∃ e : eLearn, sent e s ∧ e.v = l.learned_value
    invariant learners_agree :
      ∀ l1 l2 : Learner,
        stateOf l1.ref s = some Learner.Learn_st → stateOf l2.ref s = some Learner.Learn_st →
        l1.learned_value ≠ -1 → l2.learned_value ≠ -1 → l1.learned_value = l2.learned_value
  }

  Proof {
    prove system_config ;
    prove acceptor_state ;
    prove gap using acceptor_state ;
    prove ballots ;
    prove tally ;
    prove counted using acceptor_state ;
    prove proposed using acceptor_state ;
    prove votes using system_config ;
    prove reported ;
    prove values using reported, counted, proposed ;
    prove quorum ;
    prove blocking using acceptor_state, gap, votes ;
    prove agreement using tally, counted, votes, gap, blocking, reported, proposed, quorum,
                          ballots ;
    prove safety using system_config, values, agreement ;
  }

end FlexiblePaxos

#gen_module FlexiblePaxos
#pwf        FlexiblePaxos

namespace FlexiblePaxos
open PLean Std.WP

/-! ## The argument

The two lemmas below are the steps that carry it: a Proposer completes a
phase-1 quorum (and so proposes), or a phase-2 quorum (and so decides). Each
uses that the quorum it completes shares an Acceptor with a quorum of the
other phase. They are about one world; the obligations of those steps
follow. -/

theorem mem_add_cases {q : PSet MachineRef} {x a : MachineRef} (h : a ∈ q.add x) :
    a = x ∨ a ∈ q := by
  change (q.add x).mem a = true at h
  rw [PSet.mem_add, Bool.or_eq_true, decide_eq_true_eq] at h
  exact h

/-- When a Proposer in `Prepare` counts the promise `pl` and completes a
phase-1 quorum, its value `v'` (that of the highest ballot `h'` reported to it,
if any) is the value of every Proposer that decided at a lower ballot. -/
theorem promise_quorum_agrees (g : GW) (i : Nat) (src dst : MachineRef)
    (pl : ePrepareRsp_payload)
    (hprom : prepare_acks_promised g) (hteach : teach_quorum g) (hskip : no_skip g)
    (hpb : proposed_ballot g) (hfrom : accept_req_from g) (hreq : accept_req_proposed g)
    (hq : quorum_intersection g) (hbal : ballots_distinct g) (hdb : decided_blocks g)
    (hlog : g.log i = some { src := src, dst := dst, ev := E.ePrepareRsp pl })
    (hst : (g.machines dst).state = some S.Proposer_Prepare)
    (hprm : pl.promised = (g.machines dst).fields.Proposer_ballot_n)
    (hq1 : isPhase1Quorum ((g.machines dst).fields.Proposer_prepare_acks.add pl.acceptor) = true)
    (h' v' : Int) (hh1 : pl.n_accepted ≤ h')
    (hh2 : (g.machines dst).fields.Proposer_highest_proposal_n ≤ h')
    (hv : 0 ≤ h' → ∃ r : Msg E, is_eAcceptReq r ∧ sent r g ∧
      (eAcceptReq_payload_of r).ballot_n = h' ∧ (eAcceptReq_payload_of r).v = v')
    (r1 : MachineRef) (ht : (g.machines r1).state = some S.Proposer_Teach)
    (hlt : (g.machines r1).fields.Proposer_ballot_n < (g.machines dst).fields.Proposer_ballot_n) :
    (g.machines r1).fields.Proposer_value_to_propose = v' := by
  have hdst : is_Proposer dst g := Or.inl (Or.inl hst)
  have hr1 : is_Proposer r1 g := Or.inr ht
  have hq2 : isPhase2Quorum (g.machines r1).fields.Proposer_accept_acks = true :=
    hteach ⟨r1⟩ hr1 ht
  have hb0 : 0 ≤ (g.machines r1).fields.Proposer_ballot_n := hpb ⟨r1⟩ hr1 (Or.inr ht)
  let e : Msg E := ⟨i, src, dst, E.ePrepareRsp pl⟩
  have he : is_ePrepareRsp e := ⟨pl, rfl⟩
  have hsent : sent e g := hlog
  by_cases h1 : h' < (g.machines r1).fields.Proposer_ballot_n
  · -- An Acceptor in both quorums promised above `r1`'s ballot before voting in it.
    exfalso
    obtain ⟨a, ha1, ha2⟩ := hq _ _ hq1 hq2
    rcases mem_add_cases ha1 with rfl | ha1
    · exact hskip e he ⟨r1⟩ hr1 hsent
        (show pl.n_accepted < (g.machines r1).fields.Proposer_ballot_n by omega)
        (show (g.machines r1).fields.Proposer_ballot_n < pl.promised by omega) ha2
    · obtain ⟨e2, he2, hs2, -, hacc, hp2, hn2⟩ := hprom ⟨dst⟩ hdst a ha1
      have hp2' : (ePrepareRsp_payload_of e2).promised =
          (g.machines dst).fields.Proposer_ballot_n := hp2
      have hn2' : (ePrepareRsp_payload_of e2).n_accepted ≤
          (g.machines dst).fields.Proposer_highest_proposal_n := hn2
      exact hskip e2 he2 ⟨r1⟩ hr1 hs2
        (show (ePrepareRsp_payload_of e2).n_accepted < (g.machines r1).fields.Proposer_ballot_n by
          omega)
        (show (g.machines r1).fields.Proposer_ballot_n < (ePrepareRsp_payload_of e2).promised by
          omega)
        (by rw [hacc]; exact ha2)
  · -- The value was proposed at `h'`: by `r1` itself, or above its ballot.
    obtain ⟨r, hr, hsr, hbr, hvr⟩ := hv (by omega)
    have hqP := hfrom r hr hsr
    obtain ⟨hph, hqb, hqv⟩ := hreq r hr ⟨(eAcceptReq_payload_of r).proposer⟩ hqP hsr rfl
    have hqb' : (eAcceptReq_payload_of r).ballot_n =
        (g.machines (eAcceptReq_payload_of r).proposer).fields.Proposer_ballot_n := hqb
    have hqv' : (eAcceptReq_payload_of r).v =
        (g.machines (eAcceptReq_payload_of r).proposer).fields.Proposer_value_to_propose := hqv
    by_cases h2 : (g.machines r1).fields.Proposer_ballot_n = h'
    · have hown := hbal ⟨(eAcceptReq_payload_of r).proposer⟩ hqP ⟨r1⟩ hr1
        (show (g.machines (eAcceptReq_payload_of r).proposer).fields.Proposer_ballot_n =
          (g.machines r1).fields.Proposer_ballot_n by omega)
      have hq1 : (eAcceptReq_payload_of r).proposer = r1 := congrArg Proposer.ref hown
      rw [hq1] at hqv'
      omega
    · have hval : (g.machines r1).fields.Proposer_value_to_propose =
          (g.machines (eAcceptReq_payload_of r).proposer).fields.Proposer_value_to_propose :=
        hdb ⟨r1⟩ hr1 ⟨(eAcceptReq_payload_of r).proposer⟩ hqP ht hph
          (show (g.machines r1).fields.Proposer_ballot_n <
            (g.machines (eAcceptReq_payload_of r).proposer).fields.Proposer_ballot_n by omega)
      omega

/-- When a Proposer in `Accept` counts the vote `pl` and completes a phase-2
quorum, its value is that of every Proposer that has proposed at a higher
ballot: by induction on that ballot, through the proposals whose value was
adopted. -/
theorem vote_quorum_agrees (g : GW) (i : Nat) (src dst : MachineRef)
    (pl : eAcceptRsp_payload)
    (hpq : proposed_quorum g) (hprom : prepare_acks_promised g) (hbelow : highest_below g)
    (hgap : promise_gap g) (hskip : no_skip g) (hhigh : highest_proposed g)
    (hpb : proposed_ballot g) (hfrom : accept_req_from g) (hreq : accept_req_proposed g)
    (hq : quorum_intersection g) (hbal : ballots_distinct g)
    (hlog : g.log i = some { src := src, dst := dst, ev := E.eAcceptRsp pl })
    (hst : (g.machines dst).state = some S.Proposer_Accept)
    (hacc : pl.accepted = (g.machines dst).fields.Proposer_ballot_n)
    (hq2 : isPhase2Quorum ((g.machines dst).fields.Proposer_accept_acks.add pl.acceptor) = true)
    (r2 : MachineRef)
    (hph : (g.machines r2).state = some S.Proposer_Accept ∨
      (g.machines r2).state = some S.Proposer_Teach)
    (hlt : (g.machines dst).fields.Proposer_ballot_n < (g.machines r2).fields.Proposer_ballot_n) :
    (g.machines dst).fields.Proposer_value_to_propose =
      (g.machines r2).fields.Proposer_value_to_propose := by
  have hdst : is_Proposer dst g := Or.inl (Or.inr hst)
  have hb0 : 0 ≤ (g.machines dst).fields.Proposer_ballot_n := hpb ⟨dst⟩ hdst (Or.inl hst)
  let e : Msg E := ⟨i, src, dst, E.eAcceptRsp pl⟩
  have he : is_eAcceptRsp e := ⟨pl, rfl⟩
  have hsent : sent e g := hlog
  suffices step : ∀ (n : Nat) (r2 : MachineRef),
      ((g.machines r2).state = some S.Proposer_Accept ∨
        (g.machines r2).state = some S.Proposer_Teach) →
      (g.machines dst).fields.Proposer_ballot_n < (g.machines r2).fields.Proposer_ballot_n →
      ((g.machines r2).fields.Proposer_ballot_n -
        (g.machines dst).fields.Proposer_ballot_n).toNat ≤ n →
      (g.machines dst).fields.Proposer_value_to_propose =
        (g.machines r2).fields.Proposer_value_to_propose from
    step _ r2 hph hlt (Nat.le_refl _)
  intro n
  induction n with
  | zero => intro r2 _ hlt hn; omega
  | succ n ih =>
    intro r2 hph hlt hn
    have hr2 : is_Proposer r2 g := hph.elim (fun h => Or.inl (Or.inr h)) Or.inr
    have hq1 : isPhase1Quorum (g.machines r2).fields.Proposer_prepare_acks = true :=
      hpq ⟨r2⟩ hr2 hph
    -- An Acceptor in both quorums voted at `dst`'s ballot before it promised
    -- `r2`'s, so it reported that vote or a later one.
    have hle : (g.machines dst).fields.Proposer_ballot_n ≤
        (g.machines r2).fields.Proposer_highest_proposal_n := by
      apply Classical.byContradiction
      intro hnle
      obtain ⟨a, ha1, ha2⟩ := hq _ _ hq1 hq2
      obtain ⟨e2, he2, hs2, -, hacc2, hp2, hn2⟩ := hprom ⟨r2⟩ hr2 a ha1
      have hp2' : (ePrepareRsp_payload_of e2).promised =
          (g.machines r2).fields.Proposer_ballot_n := hp2
      have hn2' : (ePrepareRsp_payload_of e2).n_accepted ≤
          (g.machines r2).fields.Proposer_highest_proposal_n := hn2
      rcases mem_add_cases ha2 with rfl | ha2
      · have hle' : pl.accepted ≤ (ePrepareRsp_payload_of e2).n_accepted :=
          hgap e2 he2 e he hs2 hsent hacc2
            (show pl.accepted < (ePrepareRsp_payload_of e2).promised by omega)
        omega
      · exact hskip e2 he2 ⟨dst⟩ hdst hs2
          (show (ePrepareRsp_payload_of e2).n_accepted <
            (g.machines dst).fields.Proposer_ballot_n by omega)
          (show (g.machines dst).fields.Proposer_ballot_n <
            (ePrepareRsp_payload_of e2).promised by omega)
          (by rw [hacc2]; exact ha2)
    -- `r2` adopted its value from a proposal at that ballot: `dst`'s, or a higher one.
    obtain ⟨r, hr, hsr, hbr, hvr⟩ := hhigh ⟨r2⟩ hr2 (Or.inr hph)
      (show 0 ≤ (g.machines r2).fields.Proposer_highest_proposal_n by omega)
    have hbr' : (eAcceptReq_payload_of r).ballot_n =
        (g.machines r2).fields.Proposer_highest_proposal_n := hbr
    have hvr' : (eAcceptReq_payload_of r).v =
        (g.machines r2).fields.Proposer_value_to_propose := hvr
    have hqP := hfrom r hr hsr
    obtain ⟨hphq, hqb, hqv⟩ := hreq r hr ⟨(eAcceptReq_payload_of r).proposer⟩ hqP hsr rfl
    have hqb' : (eAcceptReq_payload_of r).ballot_n =
        (g.machines (eAcceptReq_payload_of r).proposer).fields.Proposer_ballot_n := hqb
    have hqv' : (eAcceptReq_payload_of r).v =
        (g.machines (eAcceptReq_payload_of r).proposer).fields.Proposer_value_to_propose := hqv
    by_cases h2 : (g.machines r2).fields.Proposer_highest_proposal_n =
        (g.machines dst).fields.Proposer_ballot_n
    · have hown := hbal ⟨(eAcceptReq_payload_of r).proposer⟩ hqP ⟨dst⟩ hdst
        (show (g.machines (eAcceptReq_payload_of r).proposer).fields.Proposer_ballot_n =
          (g.machines dst).fields.Proposer_ballot_n by omega)
      have hq0 : (eAcceptReq_payload_of r).proposer = dst := congrArg Proposer.ref hown
      rw [hq0] at hqv'
      omega
    · have hb2 : (g.machines r2).fields.Proposer_highest_proposal_n ≠ -1 →
          (g.machines r2).fields.Proposer_highest_proposal_n <
            (g.machines r2).fields.Proposer_ballot_n := hbelow ⟨r2⟩ hr2 (Or.inr hph)
      have hstep := ih (eAcceptReq_payload_of r).proposer hphq (by omega) (by omega)
      omega

/-! ## The obligations of those steps

Each hands its lemma to `pverify_auto` in place of the invariants the lemma
used, which leaves the solver the bookkeeping of the step. -/

set_option maxHeartbeats 1000000 in
@[pverifyProof] theorem Proposer.Prepare.ePrepareRsp_correct_agreement :
    ∀ (g : GW) (i : Nat) (src dst : MachineRef) (pl : ePrepareRsp_payload),
      Framework g → g.error = none →
      agreement g ∧ tally g ∧ counted g ∧ votes g ∧ gap g ∧ blocking g ∧ reported g ∧
        proposed g ∧ quorum g ∧ ballots g →
      g.net i = some { src := src, dst := dst, ev := E.ePrepareRsp pl } →
      g.log i = some { src := src, dst := dst, ev := E.ePrepareRsp pl } →
      i < g.clock → (g.machines dst).state = some S.Proposer_Prepare →
      (g.machines dst).pending = none → (g.machines dst).waiting = none →
      (g.machines dst).halted = false →
      RunPost
        (fun g' =>
          PostInv "decided_blocks" (decided_blocks g') ∧ PostInv "decided_agree" (decided_agree g'))
        (GWorld.consume g i) dst (g.machines dst) (Proposer.Prepare.ePrepareRsp_handler pl) := by
  intro g i src dst pl hF hE hInv hnet hlog hi hst hpend
  obtain ⟨hagr, -, ⟨-, hprom, -, -⟩, ⟨-, hteach⟩, -, hskip, ⟨-, hrep, hhigh⟩,
    ⟨hpb, hfrom, hreq, -⟩, hq, hbal⟩ := hInv
  have key := promise_quorum_agrees g i src dst pl hprom hteach hskip hpb hfrom hreq hq hbal
    hagr.1 hlog hst
  -- The highest ballot reported and its value: those of `pl`, or the ones held.
  have key_new := fun hprm hq1 hh2 => key hprm hq1 pl.n_accepted pl.v_accepted
    (Int.le_refl _) hh2 (hrep ⟨i, src, dst, E.ePrepareRsp pl⟩ ⟨pl, rfl⟩ hlog)
  have key_old := fun hprm hq1 hh1 => key hprm hq1
    (g.machines dst).fields.Proposer_highest_proposal_n
    (g.machines dst).fields.Proposer_value_to_propose hh1 (Int.le_refl _)
    (hhigh ⟨dst⟩ (Or.inl (Or.inl hst)) (Or.inl ⟨hst, hpend⟩))
  clear key hprom hteach hskip hrep hhigh hpb hfrom hreq hq hbal
  pverify_auto

set_option maxHeartbeats 1000000 in
@[pverifyProof] theorem Proposer.Accept.eAcceptRsp_correct_agreement :
    ∀ (g : GW) (i : Nat) (src dst : MachineRef) (pl : eAcceptRsp_payload),
      Framework g → g.error = none →
      agreement g ∧ tally g ∧ counted g ∧ votes g ∧ gap g ∧ blocking g ∧ reported g ∧
        proposed g ∧ quorum g ∧ ballots g →
      g.net i = some { src := src, dst := dst, ev := E.eAcceptRsp pl } →
      g.log i = some { src := src, dst := dst, ev := E.eAcceptRsp pl } →
      i < g.clock → (g.machines dst).state = some S.Proposer_Accept →
      (g.machines dst).pending = none → (g.machines dst).waiting = none →
      (g.machines dst).halted = false →
      RunPost
        (fun g' =>
          PostInv "decided_blocks" (decided_blocks g') ∧ PostInv "decided_agree" (decided_agree g'))
        (GWorld.consume g i) dst (g.machines dst) (Proposer.Accept.eAcceptRsp_handler pl) := by
  intro g i src dst pl hF hE hInv hnet hlog hi hst
  obtain ⟨hagr, hpq, ⟨-, hprom, -, hbelow⟩, -, hgap, hskip, ⟨-, -, hhigh⟩,
    ⟨hpb, hfrom, hreq, -⟩, hq, hbal⟩ := hInv
  have key := vote_quorum_agrees g i src dst pl hpq hprom hbelow hgap hskip hhigh hpb hfrom hreq
    hq hbal hlog hst
  clear hpq hprom hbelow hgap hskip hhigh hpb hfrom hreq hq
  pverify_auto

end FlexiblePaxos

#pverify FlexiblePaxos
