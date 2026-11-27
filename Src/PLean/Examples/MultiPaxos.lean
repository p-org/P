/-
MultiPaxos: an imperative P model of the abstract Ivy model
[`multi_paxos.ivy`](https://github.com/VeriGu/DuoAI/blob/master/protocols/multi_paxos/multi_paxos.ivy)
(from "Paxos Made EPR"), which decides one value per slot (`inst`).

A Proposer leads one round. It asks the acceptors to join the round (`ePrepare`). An
Acceptor that has not joined that round or a higher one joins it, and reports its last vote
at every slot (`ePromise`). The Proposer merges the reports, keeping for each slot the vote
of the highest round; once a majority has joined, it is active. From then on, asked to
propose at a slot (`eRequest`), it proposes the recovered value if the slot has one and the
requested value otherwise (`eAccept`); on becoming active it sends itself a request for
every recovered slot. An Acceptor votes for a proposal unless it has joined a higher round
(`eAccepted`). Once a majority has voted for a slot's proposal the slot is decided, and the
Proposer teaches its value to the learners (`eLearn`).

| Ivy                    | here                                               |
|------------------------|----------------------------------------------------|
| `one_a(R)`             | an `ePrepare` of round `R`                         |
| `one_b_msg(N, R, M)`   | an `ePromise` of `N` for `R`; `M` is its `votes`   |
| `left_round(N, R)`     | `R < N.promised`                                   |
| `active(R)`            | the Proposer of round `R` is in `Active`           |
| `proposal(I, R, V)`    | an `eAccept`                                       |
| `vote(N, I, R, V)`     | an `eAccepted` of `N`, answering that `eAccept`    |
| `decision(N, I, R, V)` | Learner `N` holds `V` at `I` (`learned`)           |

`acceptors`, `learners` and `proposers` are the sets the P test driver would hand out. A
Proposer's round is its `round` in the initial world. Rounds need not be distinct: an
Acceptor joins a round once, so at most one Proposer of a round gets a majority. A Client
asks every Proposer to propose its `value` at its `inst`; both are arbitrary in the initial
world, and so is an Acceptor's `promised`. The containers start empty, as in P.

Safety is the Ivy model's: decisions on a slot agree. `learners_agree` states it for the
Learners and `one_value_per_slot` for the `eLearn` messages, and the Learner's assertion
never fails.

The invariants are in small lemmas with short `using` lists. Five obligations are proved by
hand, after `#gen_module`, each from lemmas about one world that it hands to `pverify_auto`:
- the merge of a promise's votes into `recovered`, for the two lemmas about recovered votes
  (`merge_proposed`, `merge_covers`);
- a Proposer completes a majority of promises (`promise_majority_owner`);
- it proposes at a slot (`active_recovers_decided`);
- it completes a majority of votes (`vote_majority_blocks` and its two corollaries).

Two majorities share an Acceptor by `PSet.quorum_intersect`.
-/
import PLean

open PLean

pmodule MultiPaxos

  system s

  type tVote     = (round : Int, value : Int)
  type tPrepare  = (proposer : PLean.MachineRef, round : Int)
  type tPromise  = (acceptor : PLean.MachineRef, round : Int, votes : map[Int, tVote])
  type tAccept   = (proposer : PLean.MachineRef, round : Int, inst : Int, value : Int)
  type tAccepted = (acceptor : PLean.MachineRef, round : Int, inst : Int)
  type tLearn    = (inst : Int, value : Int)
  type tRequest  = (inst : Int, value : Int)

  event ePrepare  : tPrepare
  event ePromise  : tPromise
  event eAccept   : tAccept
  event eAccepted : tAccepted
  event eLearn    : tLearn
  event eRequest  : tRequest

  function acceptors : set[PLean.MachineRef]
  function learners  : set[PLean.MachineRef]
  function proposers : set[PLean.MachineRef]

  machine Acceptor {
    var promised : Int
    var votes    : map[Int, tVote]

    start state Accept {
      on ePrepare (req : tPrepare) {
        if req.round > promised then do
          promised = req.round
          send req.proposer, ePromise, (acceptor = this.ref, round = req.round, votes = votes)
      }

      on eAccept (req : tAccept) {
        if req.round ≥ promised then do
          promised = req.round
          votes[req.inst] = { round := req.round, value := req.value }
          send req.proposer, eAccepted,
            (acceptor = this.ref, round = req.round, inst = req.inst)
      }
    }
  }

  machine Learner {
    var learned : map[Int, Int]

    start state Learn {
      on eLearn (d : tLearn) {
        if d.inst ∈ learned then do
          assert (learned[d.inst] = d.value)
        else do
          learned[d.inst] = d.value
      }
    }
  }

  machine Proposer {
    var round       : Int
    var promises    : set[PLean.MachineRef]
    var recovered   : map[Int, tVote]
    var proposals   : map[Int, Int]
    var accept_acks : map[Int, set[PLean.MachineRef]]
    var decided     : set[Int]

    start state Prepare {
      entry {
        foreach (a in acceptors)
          invariant prepares :
            new_sends e => e is ePrepare ∧ e.target ∈ acceptors ∧
              (ePrepare_payload_of e).proposer = this.ref ;
        {
          send a, ePrepare, (proposer = this.ref, round = round)
        }
      }

      on ePromise (rsp : tPromise) {
        if rsp.round = round then do
          -- For each slot, keep the vote of the highest round. The merge goes through a
          -- local, so that the loop's invariant can compare it with `recovered`.
          var merged : map[Int, tVote]
          merged = recovered
          foreach (i in rsp.votes)
            invariant kept_keys : ∀ j : Int, j ∈ recovered → j ∈ merged ;
            invariant kept_rounds :
              ∀ (j : Int) (v v' : tVote), recovered[j]? = some v → merged[j]? = some v' →
                v.round ≤ v'.round ;
            invariant seen_keys : ∀ j : Int, j ∈ pref → j ∈ merged ;
            invariant seen_rounds :
              ∀ (j : Int) (v v' : tVote), j ∈ pref → rsp.votes[j]? = some v →
                merged[j]? = some v' → v.round ≤ v'.round ;
            invariant origin :
              ∀ (j : Int) (v' : tVote), merged[j]? = some v' → recovered[j]? ≠ some v' →
                rsp.votes[j]? = some v' ;
          {
            if i ∈ merged then do
              if (merged[i]).round < (rsp.votes[i]).round then do
                merged[i] = rsp.votes[i]
            else do
              merged[i] = rsp.votes[i]
          }
          recovered = merged
          promises += (rsp.acceptor)
          if (promises.card : Int) ≥ (acceptors.card : Int) / 2 + 1 then do
            goto Active
      }

      defer eRequest
    }

    state Active {
      entry {
        foreach (i in recovered)
          invariant resubmits : new_sends e => e is eRequest ∧ e.target = this.ref ;
        {
          send this.ref, eRequest, (inst = i, value = (recovered[i]).value)
        }
      }

      on eRequest (req : tRequest) {
        if ¬ (req.inst ∈ proposals) then do
          var v : Int
          v = req.value
          if req.inst ∈ recovered then do
            v = (recovered[req.inst]).value
          proposals[req.inst] = v
          accept_acks[req.inst] = ∅
          foreach (a in acceptors)
            invariant accepts :
              new_sends e => e is eAccept ∧ e.target ∈ acceptors ∧
                (eAccept_payload_of e).proposer = this.ref ∧
                (eAccept_payload_of e).round = round ∧
                (eAccept_payload_of e).inst = req.inst ∧
                (eAccept_payload_of e).value = v ;
          {
            send a, eAccept, (proposer = this.ref, round = round, inst = req.inst, value = v)
          }
      }

      on eAccepted (rsp : tAccepted) {
        if rsp.round = round ∧ ¬ (rsp.inst ∈ decided) then do
          accept_acks[rsp.inst] += (rsp.acceptor)
          if ((accept_acks[rsp.inst]).card : Int) ≥ (acceptors.card : Int) / 2 + 1 then do
            var chosen : Int
            chosen = proposals[rsp.inst]
            decided += (rsp.inst)
            foreach (l in learners)
              invariant learns :
                new_sends e => e is eLearn ∧ e.target ∈ learners ∧ e.src = this.ref ∧
                  (eLearn_payload_of e).inst = rsp.inst ∧
                  (eLearn_payload_of e).value = chosen ;
            {
              send l, eLearn, (inst = rsp.inst, value = chosen)
            }
      }

      ignore ePromise
    }
  }

  machine Client {
    var inst  : Int
    var value : Int

    start state Run {
      entry {
        foreach (p in proposers)
          invariant requests : new_sends e => e is eRequest ∧ e.target ∈ proposers ;
        {
          send p, eRequest, (inst = inst, value = value)
        }
      }
    }
  }

  init-holds ∀ r : PLean.MachineRef, r ∈ acceptors → is_Acceptor r s
  init-holds ∀ r : PLean.MachineRef, r ∈ learners → is_Learner r s
  init-holds ∀ r : PLean.MachineRef, r ∈ proposers → is_Proposer r s
  init-holds ∀ (a : Acceptor) (i : Int), ¬ (i ∈ a.votes)
  init-holds ∀ (l : Learner) (i : Int), ¬ (i ∈ l.learned)
  init-holds ∀ (p : Proposer) (a : PLean.MachineRef), ¬ (a ∈ p.promises)
  init-holds ∀ (p : Proposer) (i : Int), ¬ (i ∈ p.recovered)
  init-holds ∀ (p : Proposer) (i : Int), ¬ (i ∈ p.proposals)
  init-holds ∀ (p : Proposer) (i : Int), ¬ (i ∈ p.accept_acks)
  init-holds ∀ (p : Proposer) (i : Int), ¬ (i ∈ p.decided)

  -- The three sets, and where each kind of message goes.
  Lemma system_config {
    invariant acceptor_set : ∀ r : PLean.MachineRef, r ∈ acceptors → is_Acceptor r s
    invariant learner_set  : ∀ r : PLean.MachineRef, r ∈ learners → is_Learner r s
    invariant proposer_set : ∀ r : PLean.MachineRef, r ∈ proposers → is_Proposer r s
    invariant prepare_routing :
      ∀ e : ePrepare, sent e s →
        e.target ∈ acceptors ∧ is_Acceptor e.target s ∧ is_Proposer e.proposer s
    invariant promise_routing :
      ∀ e : ePromise, sent e s →
        is_Proposer e.target s ∧ e.acceptor ∈ acceptors ∧ is_Acceptor e.acceptor s
    invariant accept_routing :
      ∀ e : eAccept, sent e s →
        e.target ∈ acceptors ∧ is_Acceptor e.target s ∧ is_Proposer e.proposer s
    invariant accepted_routing :
      ∀ e : eAccepted, sent e s →
        is_Proposer e.target s ∧ e.acceptor ∈ acceptors ∧ is_Acceptor e.acceptor s
    invariant learn_routing   : ∀ e : eLearn, sent e s → is_Learner e.target s
    invariant request_routing : ∀ e : eRequest, sent e s → is_Proposer e.target s
  }

  -- What an Acceptor's promises (`ePromise`) and votes (`eAccepted`) say about its state.
  -- A fact about the vote at a slot comes in two parts, that there is one and what holds of
  -- it: a conclusion `∃ v : tVote, …` would send the solver's front end searching for `v`.
  Lemma acceptor_state {
    invariant vote_bound :
      ∀ (a : Acceptor) (i : Int) (v : tVote), a.votes[i]? = some v → v.round ≤ a.promised
    invariant promise_recorded :
      ∀ (e : ePromise) (a : Acceptor), sent e s → e.acceptor = a.ref → e.round ≤ a.promised
    invariant promise_shape :
      ∀ (e : ePromise) (i : Int) (v : tVote), sent e s → e.votes[i]? = some v →
        v.round < e.round
    invariant vote_promised :
      ∀ (e : eAccepted) (a : Acceptor), sent e s → e.acceptor = a.ref → e.round ≤ a.promised
    invariant vote_recorded_key :
      ∀ (e : eAccepted) (a : Acceptor), sent e s → e.acceptor = a.ref → e.inst ∈ a.votes
    invariant vote_recorded :
      ∀ (e : eAccepted) (a : Acceptor) (v : tVote), sent e s → e.acceptor = a.ref →
        a.votes[e.inst]? = some v → e.round ≤ v.round
    -- An Acceptor joins a round once.
    invariant promise_unique :
      ∀ e1 e2 : ePromise, sent e1 s → sent e2 s →
        e1.acceptor = e2.acceptor → e1.round = e2.round → e1 = e2
  }

  -- A promise reports the Acceptor's last vote below its round, at every slot.
  Lemma gap {
    invariant promise_gap_key :
      ∀ (e1 : ePromise) (e2 : eAccepted), sent e1 s → sent e2 s →
        e1.acceptor = e2.acceptor → e2.round < e1.round → e2.inst ∈ e1.votes
    invariant promise_gap :
      ∀ (e1 : ePromise) (e2 : eAccepted) (v : tVote), sent e1 s → sent e2 s →
        e1.acceptor = e2.acceptor → e2.round < e1.round → e1.votes[e2.inst]? = some v →
        e2.round ≤ v.round
  }

  -- An active Proposer has counted promises from a majority of the acceptors.
  Lemma tally {
    invariant promise_from_acceptor : ∀ e : ePromise, sent e s → e.acceptor ∈ acceptors
    invariant promises_acceptors :
      ∀ (p : Proposer) (a : PLean.MachineRef), a ∈ p.promises → a ∈ acceptors
    invariant active_quorum :
      ∀ p : Proposer, stateOf p.ref s = some Proposer.Active_st →
        ((p.promises).card : Int) ≥ (acceptors.card : Int) / 2 + 1
  }

  -- The promises a Proposer has counted.
  Lemma counted {
    invariant promises_promised :
      ∀ (p : Proposer) (a : PLean.MachineRef), a ∈ p.promises →
        ∃ e : ePromise, sent e s ∧ e.target = p.ref ∧ e.acceptor = a ∧ e.round = p.round
    -- A promise is counted by the Proposer it was sent to, so no Acceptor is counted by
    -- two Proposers of one round.
    invariant promise_counted :
      ∀ (e : ePromise) (p : Proposer), sent e s → e.acceptor ∈ p.promises →
        e.round = p.round → e.target = p.ref
    invariant promises_owner :
      ∀ (p1 p2 : Proposer) (a : PLean.MachineRef), a ∈ p1.promises → a ∈ p2.promises →
        p1.round = p2.round → p1 = p2
  }

  -- A vote that is held, reported or recovered was proposed (in an `eAccept`).
  Lemma reported {
    invariant accepted_proposed :
      ∀ (a : Acceptor) (i : Int) (v : tVote), a.votes[i]? = some v →
        ∃ e : eAccept, sent e s ∧ e.round = v.round ∧ e.inst = i ∧ e.value = v.value
    invariant promise_reports_proposed :
      ∀ (e : ePromise) (i : Int) (v : tVote), sent e s → e.votes[i]? = some v →
        ∃ e' : eAccept, sent e' s ∧ e'.round = v.round ∧ e'.inst = i ∧ e'.value = v.value
    invariant recovered_proposed :
      ∀ (p : Proposer) (i : Int) (v : tVote), p.recovered[i]? = some v →
        ∃ e : eAccept, sent e s ∧ e.round = v.round ∧ e.inst = i ∧ e.value = v.value
  }

  -- What a Proposer recovered: at every slot, a vote at least as high as each one reported
  -- in a promise it counted, and below its own round.
  Lemma recovery {
    invariant promise_covered :
      ∀ (p : Proposer) (e : ePromise) (i : Int), sent e s → e.acceptor ∈ p.promises →
        e.round = p.round → i ∈ e.votes → i ∈ p.recovered
    invariant promise_dominated :
      ∀ (p : Proposer) (e : ePromise) (i : Int) (v v' : tVote), sent e s →
        e.acceptor ∈ p.promises → e.round = p.round → e.votes[i]? = some v →
        p.recovered[i]? = some v' → v.round ≤ v'.round
    invariant recovered_below :
      ∀ (p : Proposer) (i : Int) (v : tVote), p.recovered[i]? = some v → v.round < p.round
  }

  -- Proposals come from active Proposers, one value per slot: the recovered one, if any.
  -- Votes go to the Proposer of their proposal.
  Lemma proposed {
    invariant accept_from : ∀ e : eAccept, sent e s → is_Proposer e.proposer s
    invariant accept_proposed :
      ∀ (e : eAccept) (p : Proposer), sent e s → e.proposer = p.ref →
        stateOf p.ref s = some Proposer.Active_st ∧ e.round = p.round ∧
        p.proposals[e.inst]? = some e.value
    invariant vote_for_proposed :
      ∀ (e : eAccepted) (p : Proposer), sent e s → e.target = p.ref →
        stateOf p.ref s = some Proposer.Active_st ∧ e.round = p.round ∧ e.inst ∈ p.proposals
    invariant acks_iff_proposed :
      ∀ (p : Proposer) (i : Int), i ∈ p.accept_acks ↔ i ∈ p.proposals
    invariant no_early_proposals :
      ∀ (p : Proposer) (i : Int), stateOf p.ref s = some Proposer.Prepare_st →
        ¬ (i ∈ p.proposals)
    invariant proposal_adopts :
      ∀ (p : Proposer) (i : Int) (v : tVote) (x : Int), p.recovered[i]? = some v →
        p.proposals[i]? = some x → x = v.value
  }

  -- The votes a Proposer has counted at each slot; a decided slot has a majority of them.
  Lemma acks {
    invariant vote_from_acceptor : ∀ e : eAccepted, sent e s → e.acceptor ∈ acceptors
    invariant acks_acceptors :
      ∀ (p : Proposer) (i : Int) (q : set[PLean.MachineRef]) (a : PLean.MachineRef),
        p.accept_acks[i]? = some q → a ∈ q → a ∈ acceptors
    invariant acks_voted :
      ∀ (p : Proposer) (i : Int) (q : set[PLean.MachineRef]) (a : PLean.MachineRef),
        p.accept_acks[i]? = some q → a ∈ q →
        ∃ e : eAccepted, sent e s ∧ e.target = p.ref ∧ e.acceptor = a ∧ e.round = p.round ∧
          e.inst = i
    invariant decided_proposed :
      ∀ (p : Proposer) (i : Int), i ∈ p.decided → i ∈ p.proposals
    invariant decided_quorum :
      ∀ (p : Proposer) (i : Int) (q : set[PLean.MachineRef]), i ∈ p.decided →
        p.accept_acks[i]? = some q → (q.card : Int) ≥ (acceptors.card : Int) / 2 + 1
  }

  -- One Proposer is active in a round.
  Lemma rounds {
    invariant round_owner :
      ∀ p1 p2 : Proposer,
        stateOf p1.ref s = some Proposer.Active_st → stateOf p2.ref s = some Proposer.Active_st →
        p1.round = p2.round → p1 = p2
  }

  -- Every proposal for a decided slot in a higher round is for the decided value, so
  -- Proposers that decided a slot agree, and so do the values taught for it.
  Lemma agreement {
    invariant decided_blocks :
      ∀ (p1 p2 : Proposer) (i x1 x2 : Int), i ∈ p1.decided → p1.round < p2.round →
        p1.proposals[i]? = some x1 → p2.proposals[i]? = some x2 → x1 = x2
    invariant decided_agree :
      ∀ (p1 p2 : Proposer) (i x1 x2 : Int), i ∈ p1.decided → i ∈ p2.decided →
        p1.proposals[i]? = some x1 → p2.proposals[i]? = some x2 → x1 = x2
    invariant learn_source : ∀ e : eLearn, sent e s → is_Proposer e.src s
    invariant learn_decided :
      ∀ (e : eLearn) (p : Proposer), sent e s → e.src = p.ref →
        e.inst ∈ p.decided ∧ p.proposals[e.inst]? = some e.value
    invariant one_value_per_slot :
      ∀ e1 e2 : eLearn, sent e1 s → sent e2 s → e1.inst = e2.inst → e1.value = e2.value
  }

  Theorem safety {
    invariant learned_was_taught :
      ∀ (l : Learner) (i x : Int), l.learned[i]? = some x →
        ∃ e : eLearn, sent e s ∧ e.inst = i ∧ e.value = x
    -- The Ivy model's `decision(N1,I,R1,V1) & decision(N2,I,R2,V2) -> V1 = V2`.
    invariant learners_agree :
      ∀ (l1 l2 : Learner) (i x1 x2 : Int), l1.learned[i]? = some x1 →
        l2.learned[i]? = some x2 → x1 = x2
  }

  Proof {
    prove system_config ;
    prove acceptor_state ;
    prove gap using acceptor_state ;
    prove tally using system_config ;
    prove counted using acceptor_state ;
    prove reported ;
    prove recovery using acceptor_state, counted ;
    prove proposed ;
    prove acks using system_config, proposed ;
    prove rounds using tally, counted ;
    prove agreement using tally, counted, recovery, reported, proposed, acks, gap, rounds ;
    prove safety using agreement ;
  }

end MultiPaxos

#gen_module MultiPaxos
#pwf        MultiPaxos

namespace MultiPaxos
open PLean Std.WP

/-! ## Majorities -/

/-- Two majorities of the acceptors share one. -/
theorem majority_intersect (q1 q2 : PSet MachineRef)
    (h1 : ∀ a, a ∈ q1 → a ∈ acceptors) (h2 : ∀ a, a ∈ q2 → a ∈ acceptors)
    (hc1 : (q1.card : Int) ≥ (acceptors.card : Int) / 2 + 1)
    (hc2 : (q2.card : Int) ≥ (acceptors.card : Int) / 2 + 1) :
    ∃ a, a ∈ q1 ∧ a ∈ q2 :=
  PSet.quorum_intersect h1 h2 (by omega)

/-- The same, when the first majority is a set and the acceptor a handler adds to it. -/
theorem majority_add_intersect (q1 q2 : PSet MachineRef) (x : MachineRef)
    (h1 : ∀ a, a ∈ q1 → a ∈ acceptors) (hx : x ∈ acceptors) (h2 : ∀ a, a ∈ q2 → a ∈ acceptors)
    (hc1 : ((q1.add x).card : Int) ≥ (acceptors.card : Int) / 2 + 1)
    (hc2 : (q2.card : Int) ≥ (acceptors.card : Int) / 2 + 1) :
    x ∈ q2 ∨ ∃ a, a ∈ q1 ∧ a ∈ q2 := by
  have mem_add : ∀ y, y ∈ q1.add x → y = x ∨ y ∈ q1 := by
    intro y h
    have h' : (q1.add x).mem y = true := h
    rw [PSet.mem_add, Bool.or_eq_true, decide_eq_true_eq] at h'
    exact h'
  have sub : ∀ y, y ∈ q1.add x → y ∈ acceptors := by
    intro y hy
    rcases mem_add y hy with rfl | h
    · exact hx
    · exact h1 y h
  obtain ⟨y, hy1, hy2⟩ := majority_intersect (q1.add x) q2 sub h2 hc1 hc2
  rcases mem_add y hy1 with rfl | h
  · exact Or.inl hy2
  · exact Or.inr ⟨y, h, hy2⟩

/-- A key of a map has a value, and conversely. -/
theorem value_of_mem {V : Type} {m : PMap Int V} {k : Int} (h : k ∈ m) :
    ∃ v, m.get k = some v :=
  Option.isSome_iff_exists.mp h

theorem mem_of_value {V : Type} {m : PMap Int V} {k : Int} {v : V} (h : m.get k = some v) :
    k ∈ m := by
  show (m.get k).isSome = true
  rw [h]; rfl

/-! ## The merge

What `Proposer.Prepare`'s `ePromise` handler leaves in `recovered`, from the invariant of
its loop: a map `m` made of the votes recovered so far and those the promise `pl` reports,
keeping the higher round at each slot. -/

/-- Every vote in such a map was proposed. -/
theorem merge_proposed (g : GW) (i : Nat) (src dst : MachineRef) (pl : ePromise_payload)
    (hrp : promise_reports_proposed g) (hrec : recovered_proposed g)
    (hlog : g.log i = some { src := src, dst := dst, ev := E.ePromise pl })
    (hst : (g.machines dst).state = some S.Proposer_Prepare)
    (m : PMap Int tVote)
    (horigin : ∀ (j : Int) (v' : tVote), m.get j = some v' →
      ¬ (g.machines dst).fields.Proposer_recovered.get j = some v' → pl.votes.get j = some v')
    (k : Int) (v : tVote) (hv : m.get k = some v) :
    ∃ e : Msg E, is_eAccept e ∧ sent e g ∧ (eAccept_payload_of e).round = v.round ∧
      (eAccept_payload_of e).inst = k ∧ (eAccept_payload_of e).value = v.value := by
  by_cases h : (g.machines dst).fields.Proposer_recovered.get k = some v
  · exact hrec ⟨dst⟩ (Or.inl hst) k v h
  · exact hrp ⟨i, src, dst, E.ePromise pl⟩ ⟨pl, rfl⟩ k v hlog (horigin k v hv h)

/-- Such a map has, at every slot, a vote at least as high as the one reported by each
promise the Proposer counts once it counts `pl`. -/
theorem merge_covers (g : GW) (i : Nat) (src dst : MachineRef) (pl : ePromise_payload)
    (huniq : promise_unique g) (hcov : promise_covered g) (hdom : promise_dominated g)
    (hlog : g.log i = some { src := src, dst := dst, ev := E.ePromise pl })
    (hst : (g.machines dst).state = some S.Proposer_Prepare)
    (hrnd : pl.round = (g.machines dst).fields.Proposer_round)
    (m : PMap Int tVote)
    (hkk : ∀ j : Int, ((g.machines dst).fields.Proposer_recovered.get j).isSome = true →
      (m.get j).isSome = true)
    (hkr : ∀ (j : Int) (v v' : tVote), (g.machines dst).fields.Proposer_recovered.get j = some v →
      m.get j = some v' → v.round ≤ v'.round)
    (hsk : ∀ j : Int, (pl.votes.get j).isSome = true → (m.get j).isSome = true)
    (hsr : ∀ (j : Int) (v v' : tVote), (pl.votes.get j).isSome = true → pl.votes.get j = some v →
      m.get j = some v' → v.round ≤ v'.round)
    (id : Nat) (s' t' : MachineRef) (b : ePromise_payload)
    (hb : g.log id = some { src := s', dst := t', ev := E.ePromise b })
    (hmem : b.acceptor = pl.acceptor ∨
      (g.machines dst).fields.Proposer_promises.mem b.acceptor = true)
    (hbr : b.round = (g.machines dst).fields.Proposer_round) (k : Int) :
    ((b.votes.get k).isSome = true → (m.get k).isSome = true) ∧
      ∀ v v' : tVote, b.votes.get k = some v → m.get k = some v' → v.round ≤ v'.round := by
  have hdst : is_Proposer dst g := Or.inl hst
  let e : Msg E := ⟨id, s', t', E.ePromise b⟩
  have he : is_ePromise e := ⟨b, rfl⟩
  have hs : sent e g := hb
  rcases hmem with hacc | hold
  · -- the promise being counted
    let em : Msg E := ⟨i, src, dst, E.ePromise pl⟩
    have hem : is_ePromise em := ⟨pl, rfl⟩
    have heq : e = em := huniq e he em hem hs hlog hacc (hbr.trans hrnd.symm)
    have hbpl : b = pl := by
      have h1 : E.ePromise b = E.ePromise pl := congrArg Msg.ev heq
      exact E.ePromise.inj h1
    subst hbpl
    exact ⟨hsk k, fun v v' h1 h2 => hsr k v v' (by rw [h1]; rfl) h1 h2⟩
  · -- a promise counted before
    have hin : (b.votes.get k).isSome = true →
        ((g.machines dst).fields.Proposer_recovered.get k).isSome = true :=
      hcov ⟨dst⟩ hdst e he k hs hold hbr
    refine ⟨fun h => hkk k (hin h), fun v v' h1 h2 => ?_⟩
    obtain ⟨v0, hv0⟩ := Option.isSome_iff_exists.mp (hin (by rw [h1]; rfl))
    have hle : v.round ≤ v0.round := hdom ⟨dst⟩ hdst e he k v v0 hs hold hbr h1 hv0
    have := hkr k v0 v' hv0 h2
    omega

/-! ## The argument -/

/-- A Proposer in `Prepare` that counts the promise `pl` and reaches a majority is the only
one active in its round. -/
theorem promise_majority_owner (g : GW) (i : Nat) (src dst : MachineRef) (pl : ePromise_payload)
    (htally : tally g) (hcounted : promise_counted g) (howner : promises_owner g)
    (hlog : g.log i = some { src := src, dst := dst, ev := E.ePromise pl })
    (hst : (g.machines dst).state = some S.Proposer_Prepare)
    (hrnd : pl.round = (g.machines dst).fields.Proposer_round)
    (hcard : (acceptors.card : Int) / 2 + 1 ≤
      (((g.machines dst).fields.Proposer_promises.add pl.acceptor).card : Int))
    (r : MachineRef) (hact : (g.machines r).state = some S.Proposer_Active)
    (hreq : (g.machines r).fields.Proposer_round = (g.machines dst).fields.Proposer_round) :
    False := by
  obtain ⟨hfrom, hpacc, hquorum⟩ := htally
  have hdst : is_Proposer dst g := Or.inl hst
  have hr : is_Proposer r g := Or.inr hact
  have hne : dst ≠ r := by
    rintro rfl
    rw [hst] at hact
    cases hact
  have hc2 : ((g.machines r).fields.Proposer_promises.card : Int) ≥
      (acceptors.card : Int) / 2 + 1 := hquorum ⟨r⟩ hr hact
  let e : Msg E := ⟨i, src, dst, E.ePromise pl⟩
  have he : is_ePromise e := ⟨pl, rfl⟩
  have hsent : sent e g := hlog
  -- An acceptor both count promised the round to one of them.
  rcases majority_add_intersect (g.machines dst).fields.Proposer_promises
      (g.machines r).fields.Proposer_promises pl.acceptor (hpacc ⟨dst⟩ hdst)
      (hfrom e he hsent) (hpacc ⟨r⟩ hr) (by omega) hc2 with hmem | ⟨a, ha1, ha2⟩
  · exact hne (hcounted e he ⟨r⟩ hr hsent hmem (by show pl.round = _; rw [hrnd, hreq]))
  · exact hne (congrArg Proposer.ref (howner ⟨dst⟩ hdst ⟨r⟩ hr a ha1 ha2 hreq.symm))

/-- What a promise that the Proposer `p` counted, reporting the vote `v` at slot `k`, gives:
`p` recovered a vote at `k`, not below `v`, that a Proposer active in its round proposed. -/
theorem recovered_of_promise (g : GW) (p : MachineRef) (k : Int)
    (hcov : promise_covered g) (hdom : promise_dominated g) (hbelow : recovered_below g)
    (hrep : recovered_proposed g) (hfrom : accept_from g) (hacc : accept_proposed g)
    (hp : is_Proposer p g) (e : Msg E) (he : is_ePromise e) (hs : sent e g)
    (hmem : (ePromise_payload_of e).acceptor ∈ (g.machines p).fields.Proposer_promises)
    (her : (ePromise_payload_of e).round = (g.machines p).fields.Proposer_round)
    (v : tVote) (hv : (ePromise_payload_of e).votes.get k = some v) :
    ∃ v' : tVote, ∃ p' : MachineRef,
      (g.machines p).fields.Proposer_recovered.get k = some v' ∧ v.round ≤ v'.round ∧
      v'.round < (g.machines p).fields.Proposer_round ∧
      (g.machines p').state = some S.Proposer_Active ∧
      (g.machines p').fields.Proposer_round = v'.round ∧
      (g.machines p').fields.Proposer_proposals.get k = some v'.value := by
  obtain ⟨v', hv'⟩ := value_of_mem (hcov ⟨p⟩ hp e he k hs hmem her (mem_of_value hv))
  have hle : v.round ≤ v'.round := hdom ⟨p⟩ hp e he k v v' hs hmem her hv hv'
  have hlt : v'.round < (g.machines p).fields.Proposer_round := hbelow ⟨p⟩ hp k v' hv'
  obtain ⟨e', he', hs', hr', hi', hx'⟩ := hrep ⟨p⟩ hp k v' hv'
  have hP := hfrom e' he' hs'
  obtain ⟨hact, hround, hprop⟩ := hacc e' he' ⟨(eAccept_payload_of e').proposer⟩ hP hs' rfl
  have hround' : (eAccept_payload_of e').round =
      (g.machines (eAccept_payload_of e').proposer).fields.Proposer_round := hround
  have hprop' : (g.machines (eAccept_payload_of e').proposer).fields.Proposer_proposals.get
      (eAccept_payload_of e').inst = some (eAccept_payload_of e').value := hprop
  have hr'' : (eAccept_payload_of e').round = v'.round := hr'
  have hi'' : (eAccept_payload_of e').inst = k := hi'
  have hx'' : (eAccept_payload_of e').value = v'.value := hx'
  refine ⟨v', (eAccept_payload_of e').proposer, hv', hle, hlt, hact, by omega, ?_⟩
  rw [← hi'', ← hx'']
  exact hprop'

/-- A Proposer active in a round above that of a Proposer that decided slot `k` recovered
the decision: its highest reported vote at `k` has the decided value. -/
theorem active_recovers_decided (g : GW) (dst : MachineRef) (k : Int)
    (htally : tally g) (hprom : promises_promised g)
    (hcov : promise_covered g) (hdom : promise_dominated g) (hbelow : recovered_below g)
    (hrep : recovered_proposed g) (hfrom : accept_from g) (hacc : accept_proposed g)
    (hkeys : acks_iff_proposed g) (hearly : no_early_proposals g)
    (hacks : acks_acceptors g) (hvoted : acks_voted g) (hdp : decided_proposed g)
    (hdq : decided_quorum g) (hgk : promise_gap_key g) (hgap : promise_gap g)
    (hown : round_owner g) (hblocks : decided_blocks g)
    (hst : (g.machines dst).state = some S.Proposer_Active)
    (r1 : MachineRef) (x1 : Int)
    (hr1 : (g.machines r1).state = some S.Proposer_Prepare ∨
      (g.machines r1).state = some S.Proposer_Active)
    (hdec : (g.machines r1).fields.Proposer_decided.mem k = true)
    (hlt : (g.machines r1).fields.Proposer_round < (g.machines dst).fields.Proposer_round)
    (hx1 : (g.machines r1).fields.Proposer_proposals.get k = some x1) :
    ∃ v : tVote, (g.machines dst).fields.Proposer_recovered.get k = some v ∧ v.value = x1 := by
  obtain ⟨-, hpacc, hquorum⟩ := htally
  have hdst : is_Proposer dst g := Or.inr hst
  have hr1' : is_Proposer r1 g := hr1
  have hk1 : k ∈ (g.machines r1).fields.Proposer_proposals := hdp ⟨r1⟩ hr1' k hdec
  obtain ⟨q, hq⟩ := value_of_mem ((hkeys ⟨r1⟩ hr1' k).mpr hk1)
  have hqc : (q.card : Int) ≥ (acceptors.card : Int) / 2 + 1 := hdq ⟨r1⟩ hr1' k q hdec hq
  have hc2 : ((g.machines dst).fields.Proposer_promises.card : Int) ≥
      (acceptors.card : Int) / 2 + 1 := hquorum ⟨dst⟩ hdst hst
  -- An acceptor that voted for `r1` at `k` promised `dst`'s round.
  obtain ⟨a, ha1, ha2⟩ := majority_intersect q (g.machines dst).fields.Proposer_promises
    (fun a ha => hacks ⟨r1⟩ hr1' k q a hq ha) (hpacc ⟨dst⟩ hdst) hqc hc2
  obtain ⟨e, he, hs, -, hea, her⟩ := hprom ⟨dst⟩ hdst a ha2
  have hea' : (ePromise_payload_of e).acceptor = a := hea
  have her' : (ePromise_payload_of e).round = (g.machines dst).fields.Proposer_round := her
  have hmem : (ePromise_payload_of e).acceptor ∈ (g.machines dst).fields.Proposer_promises := by
    rw [hea']; exact ha2
  -- Its promise reports that vote, or a later one.
  obtain ⟨e2, he2, hs2, -, hea2, her2, hei2⟩ := hvoted ⟨r1⟩ hr1' k q a hq ha1
  have hea2' : (eAccepted_payload_of e2).acceptor = a := hea2
  have her2' : (eAccepted_payload_of e2).round = (g.machines r1).fields.Proposer_round := her2
  have hei2' : (eAccepted_payload_of e2).inst = k := hei2
  have hacc2 : (ePromise_payload_of e).acceptor = (eAccepted_payload_of e2).acceptor := by
    rw [hea', hea2']
  have hlt2 : (eAccepted_payload_of e2).round < (ePromise_payload_of e).round := by omega
  obtain ⟨v, hv⟩ := value_of_mem (hgk e he e2 he2 hs hs2 hacc2 hlt2)
  have hvr : (eAccepted_payload_of e2).round ≤ v.round := hgap e he e2 he2 v hs hs2 hacc2 hlt2 hv
  rw [hei2'] at hv
  have hrec :=
      recovered_of_promise g dst k hcov hdom hbelow hrep hfrom hacc hdst e he hs hmem her' v hv
  obtain ⟨v', p', hv', hle, -, hact', hround', hprop'⟩ := hrec
  refine ⟨v', hv', ?_⟩
  have hp' : is_Proposer p' g := Or.inr hact'
  by_cases heq : v'.round = (g.machines r1).fields.Proposer_round
  · -- proposed in `r1`'s round: by `r1`
    have hr1act : (g.machines r1).state = some S.Proposer_Active := by
      rcases hr1 with h | h
      · exact absurd hk1 (hearly ⟨r1⟩ hr1' k h)
      · exact h
    have hsame := hown ⟨p'⟩ hp' ⟨r1⟩ hr1' hact' hr1act
      (show (g.machines p').fields.Proposer_round = (g.machines r1).fields.Proposer_round by omega)
    have hpr : p' = r1 := congrArg Proposer.ref hsame
    rw [hpr, hx1] at hprop'
    exact (Option.some.inj hprop').symm
  · -- proposed above `r1`'s round: `r1`'s decision blocks it
    exact (hblocks ⟨r1⟩ hr1' ⟨p'⟩ hp' k x1 v'.value hdec
      (show (g.machines r1).fields.Proposer_round < (g.machines p').fields.Proposer_round by omega)
      hx1 hprop').symm

/-- When a Proposer counts the vote `pl` and reaches a majority at a slot, its value there is
that of every proposal for the slot in a higher round: by induction on that round, through
the proposals whose value was recovered. -/
theorem vote_majority_blocks (g : GW) (i : Nat) (src dst : MachineRef) (pl : eAccepted_payload)
    (htally : tally g) (hprom : promises_promised g)
    (hcov : promise_covered g) (hdom : promise_dominated g) (hbelow : recovered_below g)
    (hrep : recovered_proposed g) (hfrom : accept_from g) (hacc : accept_proposed g)
    (hearly : no_early_proposals g) (hadopt : proposal_adopts g)
    (hvacc : vote_from_acceptor g) (hacks : acks_acceptors g) (hvoted : acks_voted g)
    (hgk : promise_gap_key g) (hgap : promise_gap g) (hown : round_owner g)
    (hlog : g.log i = some { src := src, dst := dst, ev := E.eAccepted pl })
    (hst : (g.machines dst).state = some S.Proposer_Active)
    (hrnd : pl.round = (g.machines dst).fields.Proposer_round)
    (cur : PSet MachineRef)
    (hcur : (g.machines dst).fields.Proposer_accept_acks.get pl.inst = some cur)
    (hcard : (acceptors.card : Int) / 2 + 1 ≤ ((cur.add pl.acceptor).card : Int))
    (x1 : Int) (hx1 : (g.machines dst).fields.Proposer_proposals.get pl.inst = some x1)
    (r2 : MachineRef) (x2 : Int)
    (hr2 : (g.machines r2).state = some S.Proposer_Prepare ∨
      (g.machines r2).state = some S.Proposer_Active)
    (hlt : (g.machines dst).fields.Proposer_round < (g.machines r2).fields.Proposer_round)
    (hx2 : (g.machines r2).fields.Proposer_proposals.get pl.inst = some x2) : x1 = x2 := by
  obtain ⟨-, hpacc, hquorum⟩ := htally
  have hdst : is_Proposer dst g := Or.inr hst
  let em : Msg E := ⟨i, src, dst, E.eAccepted pl⟩
  have hem : is_eAccepted em := ⟨pl, rfl⟩
  have hsent : sent em g := hlog
  suffices step : ∀ (n : Nat) (r2 : MachineRef) (x2 : Int),
      ((g.machines r2).state = some S.Proposer_Prepare ∨
        (g.machines r2).state = some S.Proposer_Active) →
      (g.machines dst).fields.Proposer_round < (g.machines r2).fields.Proposer_round →
      (g.machines r2).fields.Proposer_proposals.get pl.inst = some x2 →
      ((g.machines r2).fields.Proposer_round - (g.machines dst).fields.Proposer_round).toNat ≤ n →
      x1 = x2 from
    step _ r2 x2 hr2 hlt hx2 (Nat.le_refl _)
  intro n
  induction n with
  | zero => intro r2 x2 _ hlt _ hn; omega
  | succ n ih =>
    intro r2 x2 hr2 hlt hx2 hn
    have hr2' : is_Proposer r2 g := hr2
    have hact2 : (g.machines r2).state = some S.Proposer_Active := by
      rcases hr2 with h | h
      · exact absurd (mem_of_value hx2) (hearly ⟨r2⟩ hr2' pl.inst h)
      · exact h
    have hc2 : ((g.machines r2).fields.Proposer_promises.card : Int) ≥
        (acceptors.card : Int) / 2 + 1 := hquorum ⟨r2⟩ hr2' hact2
    -- An acceptor that voted for `dst` at the slot promised `r2`'s round later, so its
    -- promise reports that vote or a later one.
    have hrep2 : ∃ e : Msg E, ∃ v : tVote, is_ePromise e ∧ sent e g ∧
        (ePromise_payload_of e).acceptor ∈ (g.machines r2).fields.Proposer_promises ∧
        (ePromise_payload_of e).round = (g.machines r2).fields.Proposer_round ∧
        (ePromise_payload_of e).votes.get pl.inst = some v ∧
        (g.machines dst).fields.Proposer_round ≤ v.round := by
      -- the vote: `pl` itself, or the one behind a counted acceptor
      have hvote : ∃ a : MachineRef, a ∈ (g.machines r2).fields.Proposer_promises ∧
          ∃ e2 : Msg E, is_eAccepted e2 ∧ sent e2 g ∧ (eAccepted_payload_of e2).acceptor = a ∧
            (eAccepted_payload_of e2).round = (g.machines dst).fields.Proposer_round ∧
            (eAccepted_payload_of e2).inst = pl.inst := by
        rcases majority_add_intersect cur (g.machines r2).fields.Proposer_promises pl.acceptor
            (fun a ha => hacks ⟨dst⟩ hdst pl.inst cur a hcur ha) (hvacc em hem hsent)
            (hpacc ⟨r2⟩ hr2') (by omega) hc2 with hmem | ⟨a, ha1, ha2⟩
        · exact ⟨pl.acceptor, hmem, em, hem, hsent, rfl, hrnd, rfl⟩
        · obtain ⟨e2, he2, hs2, -, hea2, her2, hei2⟩ := hvoted ⟨dst⟩ hdst pl.inst cur a hcur ha1
          exact ⟨a, ha2, e2, he2, hs2, hea2, her2, hei2⟩
      obtain ⟨a, ha, e2, he2, hs2, hea2, her2, hei2⟩ := hvote
      obtain ⟨e, he, hs, -, hea, her⟩ := hprom ⟨r2⟩ hr2' a ha
      have hea' : (ePromise_payload_of e).acceptor = a := hea
      have her' : (ePromise_payload_of e).round = (g.machines r2).fields.Proposer_round := her
      have hacc2 : (ePromise_payload_of e).acceptor = (eAccepted_payload_of e2).acceptor := by
        rw [hea', hea2]
      have hlt2 : (eAccepted_payload_of e2).round < (ePromise_payload_of e).round := by omega
      obtain ⟨v, hv⟩ := value_of_mem (hgk e he e2 he2 hs hs2 hacc2 hlt2)
      have hvr : (eAccepted_payload_of e2).round ≤ v.round :=
        hgap e he e2 he2 v hs hs2 hacc2 hlt2 hv
      rw [hei2] at hv
      exact ⟨e, v, he, hs, by rw [hea']; exact ha, her', hv, by omega⟩
    obtain ⟨e, v, he, hs, hmem, her, hv, hvr⟩ := hrep2
    -- `r2` recovered a vote at least as high, and proposes its value.
    obtain ⟨v', p', hv', hle, hlt', hact', hround', hprop'⟩ :=
      recovered_of_promise g r2 pl.inst hcov hdom hbelow hrep hfrom hacc hr2' e he hs hmem her v hv
    have hx2' : x2 = v'.value := hadopt ⟨r2⟩ hr2' pl.inst v' x2 hv' hx2
    have hp' : is_Proposer p' g := Or.inr hact'
    by_cases heq : v'.round = (g.machines dst).fields.Proposer_round
    · have hsame := hown ⟨p'⟩ hp' ⟨dst⟩ hdst hact' hst
        (show (g.machines p').fields.Proposer_round = (g.machines dst).fields.Proposer_round by
          omega)
      have hpr : p' = dst := congrArg Proposer.ref hsame
      rw [hpr, hx1] at hprop'
      rw [hx2']
      exact Option.some.inj hprop'
    · have := ih p' v'.value (Or.inr hact') (by omega) hprop' (by omega)
      rw [hx2']
      exact this

/-- When a Proposer counts the vote `pl` and reaches a majority at a slot, its value there is
that of every Proposer that decided the slot. -/
theorem vote_majority_agrees (g : GW) (i : Nat) (src dst : MachineRef) (pl : eAccepted_payload)
    (htally : tally g) (hprom : promises_promised g)
    (hcov : promise_covered g) (hdom : promise_dominated g) (hbelow : recovered_below g)
    (hrep : recovered_proposed g) (hfrom : accept_from g) (hacc : accept_proposed g)
    (hearly : no_early_proposals g) (hadopt : proposal_adopts g)
    (hvacc : vote_from_acceptor g) (hacks : acks_acceptors g) (hvoted : acks_voted g)
    (hgk : promise_gap_key g) (hgap : promise_gap g) (hown : round_owner g)
    (hblocks : decided_blocks g)
    (hlog : g.log i = some { src := src, dst := dst, ev := E.eAccepted pl })
    (hst : (g.machines dst).state = some S.Proposer_Active)
    (hrnd : pl.round = (g.machines dst).fields.Proposer_round)
    (hnew : ¬ (g.machines dst).fields.Proposer_decided.mem pl.inst = true)
    (cur : PSet MachineRef)
    (hcur : (g.machines dst).fields.Proposer_accept_acks.get pl.inst = some cur)
    (hcard : (acceptors.card : Int) / 2 + 1 ≤ ((cur.add pl.acceptor).card : Int))
    (x1 : Int) (hx1 : (g.machines dst).fields.Proposer_proposals.get pl.inst = some x1)
    (r2 : MachineRef) (x2 : Int)
    (hr2 : (g.machines r2).state = some S.Proposer_Prepare ∨
      (g.machines r2).state = some S.Proposer_Active)
    (hdec : (g.machines r2).fields.Proposer_decided.mem pl.inst = true)
    (hx2 : (g.machines r2).fields.Proposer_proposals.get pl.inst = some x2) : x1 = x2 := by
  have hdst : is_Proposer dst g := Or.inr hst
  have hr2' : is_Proposer r2 g := hr2
  rcases Int.lt_trichotomy (g.machines dst).fields.Proposer_round
      (g.machines r2).fields.Proposer_round with hlt | heq | hgt
  · exact vote_majority_blocks g i src dst pl htally hprom hcov hdom hbelow hrep hfrom hacc hearly
      hadopt hvacc hacks hvoted hgk hgap hown hlog hst hrnd cur hcur hcard x1 hx1 r2 x2 hr2 hlt
      hx2
  · -- the same round: the same Proposer, which had not decided
    have hact2 : (g.machines r2).state = some S.Proposer_Active := by
      rcases hr2 with h | h
      · exact absurd (mem_of_value hx2) (hearly ⟨r2⟩ hr2' pl.inst h)
      · exact h
    have hsame := hown ⟨dst⟩ hdst ⟨r2⟩ hr2' hst hact2 heq
    have hpr : dst = r2 := congrArg Proposer.ref hsame
    rw [← hpr] at hdec
    exact absurd hdec hnew
  · exact (hblocks ⟨r2⟩ hr2' ⟨dst⟩ hdst pl.inst x2 x1 hdec hgt hx2 hx1).symm


/-- When a Proposer counts the vote `pl` and reaches a majority at a slot, its value there is
that of every `eLearn` sent for the slot. -/
theorem vote_majority_taught (g : GW) (i : Nat) (src dst : MachineRef) (pl : eAccepted_payload)
    (htally : tally g) (hprom : promises_promised g)
    (hcov : promise_covered g) (hdom : promise_dominated g) (hbelow : recovered_below g)
    (hrep : recovered_proposed g) (hfrom : accept_from g) (hacc : accept_proposed g)
    (hearly : no_early_proposals g) (hadopt : proposal_adopts g)
    (hvacc : vote_from_acceptor g) (hacks : acks_acceptors g) (hvoted : acks_voted g)
    (hgk : promise_gap_key g) (hgap : promise_gap g) (hown : round_owner g)
    (hblocks : decided_blocks g) (hsrc : learn_source g) (hld : learn_decided g)
    (hlog : g.log i = some { src := src, dst := dst, ev := E.eAccepted pl })
    (hst : (g.machines dst).state = some S.Proposer_Active)
    (hrnd : pl.round = (g.machines dst).fields.Proposer_round)
    (hnew : ¬ (g.machines dst).fields.Proposer_decided.mem pl.inst = true)
    (cur : PSet MachineRef)
    (hcur : (g.machines dst).fields.Proposer_accept_acks.get pl.inst = some cur)
    (hcard : (acceptors.card : Int) / 2 + 1 ≤ ((cur.add pl.acceptor).card : Int))
    (x1 : Int) (hx1 : (g.machines dst).fields.Proposer_proposals.get pl.inst = some x1)
    (j : Nat) (s' t' : MachineRef) (b : eLearn_payload)
    (hj : g.log j = some { src := s', dst := t', ev := E.eLearn b })
    (hb : b.inst = pl.inst) : x1 = b.value := by
  let e : Msg E := ⟨j, s', t', E.eLearn b⟩
  have he : is_eLearn e := ⟨b, rfl⟩
  have hs : sent e g := hj
  have hP : is_Proposer s' g := hsrc e he hs
  obtain ⟨hdec, hprop⟩ := hld e he ⟨s'⟩ hP hs rfl
  have hdec' : (g.machines s').fields.Proposer_decided.mem pl.inst = true := by
    rw [← hb]; exact hdec
  have hprop' : (g.machines s').fields.Proposer_proposals.get pl.inst = some b.value := by
    rw [← hb]; exact hprop
  exact vote_majority_agrees g i src dst pl htally hprom hcov hdom hbelow hrep hfrom hacc hearly
    hadopt hvacc hacks hvoted hgk hgap hown hblocks hlog hst hrnd hnew cur hcur hcard x1 hx1 s'
    b.value hP hdec' hprop'

/-! ## The obligations of those steps

Each hands its lemmas to `pverify_auto` in place of the invariants they used, which leaves
the solver the bookkeeping of the step. -/

set_option maxHeartbeats 4000000 in
@[pverifyProof] theorem Proposer.Prepare.ePromise_correct_reported :
    ∀ (g : GW) (i : Nat) (src dst : MachineRef) (pl : ePromise_payload),
      Framework g → g.error = none →
      reported g →
      g.net i = some { src := src, dst := dst, ev := E.ePromise pl } →
      g.log i = some { src := src, dst := dst, ev := E.ePromise pl } →
      i < g.clock → (g.machines dst).state = some S.Proposer_Prepare →
      (g.machines dst).pending = none → (g.machines dst).waiting = none →
      (g.machines dst).halted = false →
      RunPost
        (fun g' =>
          PostInv "accepted_proposed" (accepted_proposed g') ∧
            PostInv "promise_reports_proposed" (promise_reports_proposed g') ∧
            PostInv "recovered_proposed" (recovered_proposed g'))
        (GWorld.consume g i) dst (g.machines dst) (Proposer.Prepare.ePromise_handler pl) := by
  intro g i src dst pl hF hE hInv hnet hlog hi hst
  have key := merge_proposed g i src dst pl hInv.2.1 hInv.2.2 hlog hst
  pverify_auto

set_option maxHeartbeats 4000000 in
@[pverifyProof] theorem Proposer.Prepare.ePromise_correct_recovery :
    ∀ (g : GW) (i : Nat) (src dst : MachineRef) (pl : ePromise_payload),
      Framework g → g.error = none →
      recovery g ∧ acceptor_state g ∧ counted g →
      g.net i = some { src := src, dst := dst, ev := E.ePromise pl } →
      g.log i = some { src := src, dst := dst, ev := E.ePromise pl } →
      i < g.clock → (g.machines dst).state = some S.Proposer_Prepare →
      (g.machines dst).pending = none → (g.machines dst).waiting = none →
      (g.machines dst).halted = false →
      RunPost
        (fun g' =>
          PostInv "promise_covered" (promise_covered g') ∧
            PostInv "promise_dominated" (promise_dominated g') ∧
            PostInv "recovered_below" (recovered_below g'))
        (GWorld.consume g i) dst (g.machines dst) (Proposer.Prepare.ePromise_handler pl) := by
  intro g i src dst pl hF hE hInv hnet hlog hi hst
  obtain ⟨hrecv, ⟨-, -, hshape, -, -, -, huniq⟩, -⟩ := hInv
  have key := merge_covers g i src dst pl huniq hrecv.1 hrecv.2.1 hlog hst
  clear huniq
  pverify_auto

set_option maxHeartbeats 4000000 in
@[pverifyProof] theorem Proposer.Prepare.ePromise_correct_rounds :
    ∀ (g : GW) (i : Nat) (src dst : MachineRef) (pl : ePromise_payload),
      Framework g → g.error = none →
      rounds g ∧ tally g ∧ counted g →
      g.net i = some { src := src, dst := dst, ev := E.ePromise pl } →
      g.log i = some { src := src, dst := dst, ev := E.ePromise pl } →
      i < g.clock → (g.machines dst).state = some S.Proposer_Prepare →
      (g.machines dst).pending = none → (g.machines dst).waiting = none →
      (g.machines dst).halted = false →
      RunPost (fun g' => PostInv "round_owner" (round_owner g')) (GWorld.consume g i) dst
        (g.machines dst) (Proposer.Prepare.ePromise_handler pl) := by
  intro g i src dst pl hF hE hInv hnet hlog hi hst
  obtain ⟨hown, htally, ⟨-, hcounted, howner⟩⟩ := hInv
  have key := promise_majority_owner g i src dst pl htally hcounted howner hlog hst
  clear htally hcounted howner
  pverify_auto

set_option maxHeartbeats 4000000 in
@[pverifyProof] theorem Proposer.Active.eRequest_correct_agreement :
    ∀ (g : GW) (i : Nat) (src dst : MachineRef) (pl : eRequest_payload),
      Framework g → g.error = none →
      agreement g ∧ tally g ∧ counted g ∧ recovery g ∧ reported g ∧ proposed g ∧ acks g ∧ gap g ∧
        rounds g →
      g.net i = some { src := src, dst := dst, ev := E.eRequest pl } →
      g.log i = some { src := src, dst := dst, ev := E.eRequest pl } →
      i < g.clock → (g.machines dst).state = some S.Proposer_Active →
      (g.machines dst).pending = none → (g.machines dst).waiting = none →
      (g.machines dst).halted = false →
      RunPost
        (fun g' =>
          PostInv "decided_blocks" (decided_blocks g') ∧
            PostInv "decided_agree" (decided_agree g') ∧
            PostInv "learn_source" (learn_source g') ∧ PostInv "learn_decided" (learn_decided g') ∧
            PostInv "one_value_per_slot" (one_value_per_slot g'))
        (GWorld.consume g i) dst (g.machines dst) (Proposer.Active.eRequest_handler pl) := by
  intro g i src dst pl hF hE hInv hnet hlog hi hst
  obtain ⟨hagr, htally, ⟨hprom, -, -⟩, ⟨hcov, hdom, hbelow⟩, ⟨-, -, hrep⟩,
    ⟨hfrom, hacc, -, hkeys, hearly, -⟩, ⟨-, hacks, hvoted, hdp, hdq⟩, ⟨hgk, hgap⟩, hown⟩ := hInv
  have key := active_recovers_decided g dst pl.inst htally hprom hcov hdom hbelow hrep hfrom hacc
    hkeys hearly hacks hvoted hdp hdq hgk hgap hown hagr.1 hst
  clear htally hprom hcov hdom hbelow hrep hfrom hacc hkeys hearly hacks hvoted hdq hgk hgap hown
  pverify_auto

set_option maxHeartbeats 4000000 in
@[pverifyProof] theorem Proposer.Active.eAccepted_correct_agreement :
    ∀ (g : GW) (i : Nat) (src dst : MachineRef) (pl : eAccepted_payload),
      Framework g → g.error = none →
      agreement g ∧ tally g ∧ counted g ∧ recovery g ∧ reported g ∧ proposed g ∧ acks g ∧ gap g ∧
        rounds g →
      g.net i = some { src := src, dst := dst, ev := E.eAccepted pl } →
      g.log i = some { src := src, dst := dst, ev := E.eAccepted pl } →
      i < g.clock → (g.machines dst).state = some S.Proposer_Active →
      (g.machines dst).pending = none → (g.machines dst).waiting = none →
      (g.machines dst).halted = false →
      RunPost
        (fun g' =>
          PostInv "decided_blocks" (decided_blocks g') ∧
            PostInv "decided_agree" (decided_agree g') ∧
            PostInv "learn_source" (learn_source g') ∧ PostInv "learn_decided" (learn_decided g') ∧
            PostInv "one_value_per_slot" (one_value_per_slot g'))
        (GWorld.consume g i) dst (g.machines dst) (Proposer.Active.eAccepted_handler pl) := by
  intro g i src dst pl hF hE hInv hnet hlog hi hst
  obtain ⟨hagr, htally, ⟨hprom, -, -⟩, ⟨hcov, hdom, hbelow⟩, ⟨-, -, hrep⟩,
    ⟨hfrom, hacc, -, -, hearly, hadopt⟩, ⟨hvacc, hacks, hvoted, -, -⟩, ⟨hgk, hgap⟩, hown⟩ := hInv
  have key_blocks := vote_majority_blocks g i src dst pl htally hprom hcov hdom hbelow hrep hfrom
    hacc hearly hadopt hvacc hacks hvoted hgk hgap hown hlog hst
  have key_agrees := vote_majority_agrees g i src dst pl htally hprom hcov hdom hbelow hrep hfrom
    hacc hearly hadopt hvacc hacks hvoted hgk hgap hown hagr.1 hlog hst
  have key_taught := vote_majority_taught g i src dst pl htally hprom hcov hdom hbelow hrep hfrom
    hacc hearly hadopt hvacc hacks hvoted hgk hgap hown hagr.1 hagr.2.2.1 hagr.2.2.2.1 hlog hst
  clear htally hprom hcov hdom hbelow hrep hfrom hacc hearly hadopt hvacc hacks hvoted hgk hgap
    hown
  pverify_auto

end MultiPaxos

#pverify MultiPaxos
