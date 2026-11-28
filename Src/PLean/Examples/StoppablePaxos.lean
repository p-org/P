/-
StoppablePaxos: DuoAI's abstract Ivy model `stoppable_paxos.ivy` (from "Paxos Made EPR"):
MultiPaxos, which decides one value per slot (`inst`), with a special value `stop` that ends
the sequence of slots. No value is proposed above a slot where `stop` may have been chosen,
and `stop` is not proposed below a slot that has a proposal.

The machines and messages are those of `MultiPaxos.lean`. A Proposer leads one round and
asks the acceptors to join it (`ePrepare`). An Acceptor that has neither joined nor voted in
that round or a higher one joins and reports its last vote at every slot (`ePromise`). The
Proposer merges the reports into `recovered`, keeping at each slot the vote of the highest
round (Ivy's `m`). Once the Acceptors that joined form a quorum, it may start its round.
What it proposes is new:
- On starting its round (`Activate`, then `Active`), Ivy's `receive_join_acks` filter: if no
  recovered value is `stop`, every recovered value. Otherwise let `im` be a slot whose
  recovered value is `stop`, of the highest round among those (any of them on a tie). The
  stop at `im` is voided when a slot above `im` has a recovered vote of a higher round. If it
  is voided, every recovered value but the stops is proposed, except at slots above `im`
  whose vote is of a lower round than `im`'s; if not, the recovered values at the slots up
  to `im`, except the stops at slots other than `im`.
- Asked by a Client to propose a value at a slot (`eRequest`, Ivy's `propose`), it does so
  if it has proposed nothing at that slot and `stop` at no lower slot, and, for `stop`,
  nothing at a higher slot. Otherwise it drops the request.
A proposal is recorded in `proposals` and goes to the acceptors (`eAccept`) when the
Proposer handles the `eBroadcast` it sent itself, which also starts the count of its votes
(`accept_acks`). An Acceptor votes unless it has joined or voted in a higher round
(`eAccepted`). Once a quorum has voted for a slot's proposal, the Proposer teaches the value
to the learners (`eLearn`).

Ivy's `node` is an Acceptor (joins, votes) or a Learner (decides). Slots, rounds and values
are `Int`; `none` is the absence of a key in a vote map; `stop` is an arbitrary constant.
Quorums are `isQuorum`, with Ivy's intersection axiom verbatim as an `init-holds`.
`left_round(N, R)` is `R < N.promised`; `active(R)` is the Proposer of `R` in `Activate` or
`Active`; `proposal(I, R, V)` is its `proposals[I] = V`; `decision(N, I, R, V)` is an
`eLearn` of `(I, V)` from `R`'s Proposer to `N`. Initially no packet is in flight, every
Proposer is in `Prepare`, the containers are empty, and `acceptors`, `learners` and
`proposers` hold machines of their kind. Rounds, `promised`, the Clients' requests, `stop`
and the quorums are arbitrary. Rounds need not be distinct: an Acceptor joins a round once
and quorums intersect, so at most one Proposer of a round starts it.

Safety is Ivy's `[1000000]`, `decision(N1,I,R1,V1) & decision(N2,I,R2,V2) -> V1 = V2`: with
`decision` read as above, that is `one_value_per_slot` (Lemma `agreement`). Theorem `safety`
says it of what the Learners hold (`learners_agree`, `learned_was_taught`); a Learner asserts
that every value it is taught at a slot is its first, and `StoppablePaxos.safe` says no
reachable world has an error. Ivy states no property of `stop`, so none is ported.

Deviations from the Ivy model:
- The Acceptor is `MultiPaxos.lean`'s, Lamport's: it joins only above every round it has
  joined or voted in, and votes only in a round at least as high (a vote raises
  `promised`). Ivy's node may still vote, or join, below a round it voted in. Every join and
  vote here meets Ivy's guards, so safety is proved for these acceptors only. Such an
  Acceptor's promise reports its highest vote below the round at every slot, which is what
  Ivy's `receive_join_acks` reads. An arbitrary initial `promised` only removes behaviours.
- `Int` is one model of Ivy's total orders; Ivy's round `none` is used only as "no vote".
- A Proposer may pass over a quorum of promises (`if ($)`) and wait for more, so any quorum
  whose members all joined can start its round. It decides at the first quorum of votes;
  which quorum makes no difference to `decision`.
- Ivy's atomic `receive_join_acks` is three Proposer steps: the `ePromise` completing the
  quorum, the `Activate` entry (computes `im`, with an `if ($)` at each tie, and whether its
  stop is voided, into fields), and the `Active` entry (the proposals). No message is
  delivered to a Proposer while an entry is pending and no other machine reads its fields,
  so for everything else they are one step. A second quorum of promises is ignored. The
  ghost `vslot` (a slot that voids the stop) is read only by invariants.
- `eAccept`s leave when the proposal's `eBroadcast` is handled, which only delays votes.
- Ivy's `propose` takes any slot and value; here Clients ask, and any finite set of
  requests is the Clients of some initial world. A request whose guards fail is dropped:
  proposals are never withdrawn, so it could never be served.
- The round's Proposer counts the votes, decides once and teaches the value; a Learner keeps
  one value per slot.
- Packets are consumed; re-reading Ivy's relations (joining or voting again, deciding again)
  is a no-op.

Proof: `MultiPaxos.lean`'s argument with the stop rules. Within a round nothing is proposed
above a stop (`stop_top`). `activation` describes what `Activate` computed, and that the
`Active` entry proposed every recovered value the filter lets through (`admitted_proposed`).
A slot decided in a round is recovered, with the decided value, by every Proposer of a
higher round whose `Active` entry has run, and the filter lets it through
(`decided_recovered`), because of `stop_top` in the rounds between. Nine obligations are
proved by hand, each handing `pverify_auto` lemmas about one world, or fewer premises:
- merging a promise into `recovered` (`merge_proposed`, `merge_covers`);
- completing a quorum of promises (`promise_quorum_owner`);
- the `Active` entry: a slot decided below passes the filter (`entry_key`, from
  `filter_passes`), nothing above a `stop` (`props_stop_top`), and nothing proposed before
  it runs (for `active_routing`);
- completing a quorum of votes (`vote_quorum_recovered`, by induction on rounds);
- the two steps that start a round, for `agreement`, from its own invariants alone.
-/
import PLean

open PLean

pmodule StoppablePaxos

  system s

  type tVote      = (round : Int, value : Int)
  type tPrepare   = (proposer : PLean.MachineRef, round : Int)
  type tPromise   = (acceptor : PLean.MachineRef, round : Int, votes : map[Int, tVote])
  type tAccept    = (proposer : PLean.MachineRef, round : Int, inst : Int, value : Int)
  type tAccepted  = (acceptor : PLean.MachineRef, round : Int, inst : Int)
  type tLearn     = (inst : Int, value : Int)
  type tRequest   = (inst : Int, value : Int)
  type tBroadcast = (inst : Int)

  event ePrepare   : tPrepare
  event ePromise   : tPromise
  event eAccept    : tAccept
  event eAccepted  : tAccepted
  event eLearn     : tLearn
  event eRequest   : tRequest
  event eBroadcast : tBroadcast

  function acceptors : set[PLean.MachineRef]
  function learners  : set[PLean.MachineRef]
  function proposers : set[PLean.MachineRef]
  function isQuorum  : set[PLean.MachineRef] → Bool
  function stop      : Int

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
    var has_stop    : Bool
    var im          : Int
    var im_round    : Int
    var voided      : Bool
    var vslot       : Int
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
          -- Any quorum whose promises have all arrived may be the one the round starts with.
          if isQuorum promises = true then do
            if ($) then
              goto Activate
      }

      defer eRequest
    }

    -- Ivy's `receive_join_acks`, first part: its `im`, a slot whose recovered value is
    -- `stop`, of the highest round among those, and whether the stop there is voided.
    state Activate {
      entry {
        var cur        : tVote
        var found      : Bool
        var slot       : Int
        var slot_round : Int
        var void       : Bool
        var witness    : Int
        found = false
        void = false
        foreach (i in recovered)
          invariant top_stop :
            found = true → slot ∈ recovered ∧
              ∀ v : tVote, recovered[slot]? = some v → v.value = stop ∧ v.round = slot_round ;
          invariant stops_below :
            ∀ (j : Int) (v : tVote), j ∈ pref → recovered[j]? = some v → v.value = stop →
              found = true ∧ v.round ≤ slot_round ;
        {
          cur = recovered[i]
          if cur.value = stop then do
            if ¬ found ∨ slot_round < cur.round then do
              found = true
              slot = i
              slot_round = cur.round
            else do
              if slot_round = cur.round then do
                if ($) then
                  slot = i
        }
        -- The stop at `im` is voided by a vote of a higher round at a higher slot.
        if found then do
          foreach (i in recovered)
            invariant void_witness :
              void = true → witness ∈ recovered ∧ slot < witness ∧
                ∀ v : tVote, recovered[witness]? = some v → slot_round < v.round ;
            invariant void_all :
              ∀ (j : Int) (v : tVote), j ∈ pref → recovered[j]? = some v → slot < j →
                slot_round < v.round → void = true ;
          {
            cur = recovered[i]
            if slot < i ∧ slot_round < cur.round then do
              void = true
              witness = i
          }
        has_stop = found
        im = slot
        im_round = slot_round
        voided = void
        vslot = witness
        goto Active
      }

      defer eRequest
      ignore ePromise
    }

    state Active {
      -- Ivy's `receive_join_acks`, second part: the proposals of the recovered values.
      entry {
        var cur   : tVote
        var props : map[Int, Int]
        props = ∅
        foreach (i in recovered)
          invariant broadcasts :
            new_sends e => e is eBroadcast ∧ e.target = this.ref ∧
              (eBroadcast_payload_of e).inst ∈ props ;
          invariant props_recovered :
            ∀ (j x : Int), props[j]? = some x → j ∈ recovered ∧
              ∀ v : tVote, recovered[j]? = some v → x = v.value ∧
                (has_stop = false ∨
                  (voided = true ∧ ¬ (v.value = stop ∨ (im < j ∧ v.round < im_round))) ∨
                  (voided = false ∧ ¬ ((j ≠ im ∧ v.value = stop) ∨ im < j))) ;
          invariant props_admitted :
            ∀ (j : Int) (v : tVote), j ∈ pref → recovered[j]? = some v →
              (has_stop = false ∨
                (voided = true ∧ ¬ (v.value = stop ∨ (im < j ∧ v.round < im_round))) ∨
                (voided = false ∧ ¬ ((j ≠ im ∧ v.value = stop) ∨ im < j))) →
              props[j]? = some v.value ;
        {
          cur = recovered[i]
          if ¬ has_stop ∨
              (voided ∧ ¬ (cur.value = stop ∨ (im < i ∧ cur.round < im_round))) ∨
              (¬ voided ∧ ¬ ((i ≠ im ∧ cur.value = stop) ∨ im < i)) then do
            props[i] = cur.value
            send this.ref, eBroadcast, (inst = i)
        }
        proposals = props
      }

      -- Ivy's `propose`.
      on eRequest (req : tRequest) {
        if ¬ (req.inst ∈ proposals) then do
          var allowed : Bool
          allowed = true
          foreach (j in proposals)
            invariant guards :
              allowed = true → ∀ k : Int, k ∈ pref →
                (k < req.inst → proposals[k]? ≠ some stop) ∧ (req.value = stop → k ≤ req.inst) ;
          {
            if j < req.inst ∧ proposals[j] = stop then do
              allowed = false
            if req.value = stop ∧ req.inst < j then do
              allowed = false
          }
          if allowed then do
            proposals[req.inst] = req.value
            send this.ref, eBroadcast, (inst = req.inst)
      }

      -- The 2a messages of the proposal at a slot; the count of its votes starts.
      on eBroadcast (b : tBroadcast) {
        var v : Int
        v = proposals[b.inst]
        if ¬ (b.inst ∈ accept_acks) then do
          accept_acks[b.inst] = ∅
        foreach (a in acceptors)
          invariant accepts :
            new_sends e => e is eAccept ∧ e.target ∈ acceptors ∧
              (eAccept_payload_of e).proposer = this.ref ∧
              (eAccept_payload_of e).round = round ∧
              (eAccept_payload_of e).inst = b.inst ∧
              (eAccept_payload_of e).value = v ;
        {
          send a, eAccept, (proposer = this.ref, round = round, inst = b.inst, value = v)
        }
      }

      on eAccepted (rsp : tAccepted) {
        if rsp.round = round ∧ ¬ (rsp.inst ∈ decided) then do
          accept_acks[rsp.inst] += (rsp.acceptor)
          if isQuorum (accept_acks[rsp.inst]) = true then do
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
  init-holds ∀ q1 q2 : set[PLean.MachineRef],
    isQuorum q1 = true → isQuorum q2 = true → ∃ a, a ∈ q1 ∧ a ∈ q2
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
    invariant learn_routing     : ∀ e : eLearn, sent e s → is_Learner e.target s
    invariant request_routing   : ∀ e : eRequest, sent e s → is_Proposer e.target s
    invariant broadcast_routing : ∀ e : eBroadcast, sent e s → is_Proposer e.target s
  }

  -- `eBroadcast`, `eAccept` and `eAccepted` concern an active Proposer, and a slot it has
  -- proposed at (a key of `proposals`; for `eAccept` and `eAccepted`, also of `accept_acks`).
  Lemma active_routing {
    invariant broadcast_active :
      ∀ (e : eBroadcast) (p : Proposer), sent e s → e.target = p.ref →
        stateOf p.ref s = some Proposer.Active_st ∧ e.inst ∈ p.proposals
    invariant accept_active :
      ∀ (e : eAccept) (p : Proposer), sent e s → e.proposer = p.ref →
        stateOf p.ref s = some Proposer.Active_st ∧ e.inst ∈ p.proposals ∧
        e.inst ∈ p.accept_acks
    invariant accepted_active :
      ∀ (e : eAccepted) (p : Proposer), sent e s → e.target = p.ref →
        stateOf p.ref s = some Proposer.Active_st ∧ e.inst ∈ p.proposals ∧
        e.inst ∈ p.accept_acks
  }

  -- Ivy's axiom: two quorums share a member. It says nothing about the state: initial
  -- worlds assume it.
  Lemma quorum {
    invariant quorum_intersection :
      ∀ q1 q2 : set[PLean.MachineRef],
        isQuorum q1 = true → isQuorum q2 = true → ∃ a, a ∈ q1 ∧ a ∈ q2
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

  -- An activated Proposer has counted promises from a quorum.
  Lemma tally {
    invariant promise_from_acceptor : ∀ e : ePromise, sent e s → e.acceptor ∈ acceptors
    invariant promises_acceptors :
      ∀ (p : Proposer) (a : PLean.MachineRef), a ∈ p.promises → a ∈ acceptors
    invariant active_quorum :
      ∀ p : Proposer,
        (stateOf p.ref s = some Proposer.Activate_st ∨ stateOf p.ref s = some Proposer.Active_st) →
        isQuorum p.promises = true
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

  -- Proposals come from Proposers whose `Active` entry has run, one value per slot. Votes
  -- go to the Proposer of their proposal.
  Lemma proposed {
    invariant accept_from : ∀ e : eAccept, sent e s → is_Proposer e.proposer s
    invariant accept_proposed :
      ∀ (e : eAccept) (p : Proposer), sent e s → e.proposer = p.ref →
        stateOf p.ref s = some Proposer.Active_st ∧ e.round = p.round ∧
        p.proposals[e.inst]? = some e.value
    invariant vote_for_proposed :
      ∀ (e : eAccepted) (p : Proposer), sent e s → e.target = p.ref →
        stateOf p.ref s = some Proposer.Active_st ∧ e.round = p.round ∧ e.inst ∈ p.proposals
    invariant acks_proposed :
      ∀ (p : Proposer) (i : Int), i ∈ p.accept_acks → i ∈ p.proposals
    invariant proposals_active :
      ∀ (p : Proposer) (i : Int), i ∈ p.proposals →
        stateOf p.ref s = some Proposer.Active_st ∧ (s.machines p.ref).pending = none
  }

  -- What the activation computed (`im`, its round, whether its stop is voided, and a slot
  -- that voids it), and that the entry of `Active` proposed every recovered value Ivy's
  -- `receive_join_acks` proposes.
  Lemma activation {
    invariant im_recovered :
      ∀ p : Proposer, stateOf p.ref s = some Proposer.Active_st → p.has_stop = true →
        p.im ∈ p.recovered
    invariant im_stop :
      ∀ (p : Proposer) (v : tVote), stateOf p.ref s = some Proposer.Active_st →
        p.has_stop = true → p.recovered[p.im]? = some v → v.value = stop ∧ v.round = p.im_round
    invariant im_highest :
      ∀ (p : Proposer) (j : Int) (v : tVote), stateOf p.ref s = some Proposer.Active_st →
        p.recovered[j]? = some v → v.value = stop → p.has_stop = true ∧ v.round ≤ p.im_round
    invariant void_slot :
      ∀ p : Proposer, stateOf p.ref s = some Proposer.Active_st → p.voided = true →
        p.vslot ∈ p.recovered ∧ p.im < p.vslot
    invariant void_round :
      ∀ (p : Proposer) (v : tVote), stateOf p.ref s = some Proposer.Active_st →
        p.voided = true → p.recovered[p.vslot]? = some v → p.im_round < v.round
    invariant void_complete :
      ∀ (p : Proposer) (j : Int) (v : tVote), stateOf p.ref s = some Proposer.Active_st →
        p.has_stop = true → p.recovered[j]? = some v → p.im < j → p.im_round < v.round →
        p.voided = true
    invariant admitted_proposed :
      ∀ (p : Proposer) (j : Int) (v : tVote), stateOf p.ref s = some Proposer.Active_st →
        (s.machines p.ref).pending = none → p.recovered[j]? = some v →
        (p.has_stop = false ∨
          (p.voided = true ∧ ¬ (v.value = stop ∨ (p.im < j ∧ v.round < p.im_round))) ∨
          (p.voided = false ∧ ¬ ((j ≠ p.im ∧ v.value = stop) ∨ p.im < j))) →
        p.proposals[j]? = some v.value
  }

  -- A Proposer proposes nothing above a slot where it proposed `stop`.
  Lemma stops {
    invariant stop_top :
      ∀ (p : Proposer) (j k : Int), p.proposals[j]? = some stop → k ∈ p.proposals → k ≤ j
  }

  -- The votes a Proposer has counted at each slot; a decided slot has a quorum of them.
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
    invariant decided_counted :
      ∀ (p : Proposer) (i : Int), i ∈ p.decided → i ∈ p.accept_acks
    invariant decided_quorum :
      ∀ (p : Proposer) (i : Int) (q : set[PLean.MachineRef]), i ∈ p.decided →
        p.accept_acks[i]? = some q → isQuorum q = true
  }

  -- One Proposer is activated per round.
  Lemma rounds {
    invariant round_owner :
      ∀ p1 p2 : Proposer,
        (stateOf p1.ref s = some Proposer.Activate_st ∨
          stateOf p1.ref s = some Proposer.Active_st) →
        (stateOf p2.ref s = some Proposer.Activate_st ∨
          stateOf p2.ref s = some Proposer.Active_st) →
        p1.round = p2.round → p1 = p2
  }

  -- A slot decided in a round is recovered, with the decided value, by every Proposer of a
  -- higher round whose `Active` entry has run, and Ivy's filter let it through: so that
  -- Proposer proposed the decided value there (`admitted_proposed`). Proposers that decided
  -- a slot agree, and so do the values taught for it.
  Lemma agreement {
    invariant decided_recovered_key :
      ∀ (p1 p2 : Proposer) (i : Int), i ∈ p1.decided → p1.round < p2.round →
        stateOf p2.ref s = some Proposer.Active_st → (s.machines p2.ref).pending = none →
        i ∈ p2.recovered
    invariant decided_recovered :
      ∀ (p1 p2 : Proposer) (i x1 : Int) (v : tVote), i ∈ p1.decided → p1.round < p2.round →
        p1.proposals[i]? = some x1 →
        stateOf p2.ref s = some Proposer.Active_st → (s.machines p2.ref).pending = none →
        p2.recovered[i]? = some v →
        v.value = x1 ∧
        (p2.has_stop = false ∨
          (p2.voided = true ∧ ¬ (v.value = stop ∨ (p2.im < i ∧ v.round < p2.im_round))) ∨
          (p2.voided = false ∧ ¬ ((i ≠ p2.im ∧ v.value = stop) ∨ p2.im < i)))
    invariant decided_agree :
      ∀ (p1 p2 : Proposer) (i x1 x2 : Int), i ∈ p1.decided → i ∈ p2.decided →
        p1.proposals[i]? = some x1 → p2.proposals[i]? = some x2 → x1 = x2
    invariant learn_source : ∀ e : eLearn, sent e s → is_Proposer e.src s
    invariant learn_decided :
      ∀ (e : eLearn) (p : Proposer), sent e s → e.src = p.ref →
        e.inst ∈ p.decided ∧ p.proposals[e.inst]? = some e.value
    -- The Ivy model's `[1000000]`, with `decision(N, I, R, V)` read as "an `eLearn` of
    -- `(I, V)` was sent to `N` by the Proposer of round `R`".
    invariant one_value_per_slot :
      ∀ e1 e2 : eLearn, sent e1 s → sent e2 s → e1.inst = e2.inst → e1.value = e2.value
  }

  Theorem safety {
    invariant learned_was_taught :
      ∀ (l : Learner) (i x : Int), l.learned[i]? = some x →
        ∃ e : eLearn, sent e s ∧ e.inst = i ∧ e.value = x
    -- The Ivy model's `[1000000]`, with `decision(N, I, R, V)` read as "Learner `N` holds
    -- `V` at `I`".
    invariant learners_agree :
      ∀ (l1 l2 : Learner) (i x1 x2 : Int), l1.learned[i]? = some x1 →
        l2.learned[i]? = some x2 → x1 = x2
  }

  Proof {
    prove system_config ;
    prove proposed ;
    prove active_routing using system_config, proposed ;
    prove quorum ;
    prove acceptor_state ;
    prove gap using acceptor_state ;
    prove tally using system_config ;
    prove counted using acceptor_state ;
    prove reported ;
    prove recovery using acceptor_state, counted ;
    prove activation ;
    prove stops using activation ;
    prove acks using system_config, proposed ;
    prove rounds using quorum, tally, counted ;
    prove agreement using quorum, tally, counted, recovery, reported, proposed, activation,
                          stops, acks, gap, rounds ;
    prove safety using agreement ;
  }

end StoppablePaxos

#gen_module StoppablePaxos
#pwf        StoppablePaxos

namespace StoppablePaxos
open PLean Std.WP

/-! ## Containers -/

theorem mem_add_cases {q : PSet MachineRef} {x a : MachineRef} (h : a ∈ q.add x) :
    a = x ∨ a ∈ q := by
  change (q.add x).mem a = true at h
  rw [PSet.mem_add, Bool.or_eq_true, decide_eq_true_eq] at h
  exact h

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
  · exact hrec ⟨dst⟩ (Or.inl (Or.inl hst)) k v h
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
  have hdst : is_Proposer dst g := Or.inl (Or.inl hst)
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

/-! ## Rounds -/

/-- A Proposer in `Prepare` that counts the promise `pl` and completes a quorum is the only
one activated in its round. -/
theorem promise_quorum_owner (g : GW) (i : Nat) (src dst : MachineRef) (pl : ePromise_payload)
    (hq : quorum_intersection g) (hquorum : active_quorum g) (hcounted : promise_counted g)
    (howner : promises_owner g)
    (hlog : g.log i = some { src := src, dst := dst, ev := E.ePromise pl })
    (hst : (g.machines dst).state = some S.Proposer_Prepare)
    (hrnd : pl.round = (g.machines dst).fields.Proposer_round)
    (hq1 : isQuorum ((g.machines dst).fields.Proposer_promises.add pl.acceptor) = true)
    (r : MachineRef)
    (hact : (g.machines r).state = some S.Proposer_Activate ∨
      (g.machines r).state = some S.Proposer_Active)
    (hreq : (g.machines r).fields.Proposer_round = (g.machines dst).fields.Proposer_round) :
    False := by
  have hdst : is_Proposer dst g := Or.inl (Or.inl hst)
  have hr : is_Proposer r g := by
    rcases hact with h | h
    · exact Or.inl (Or.inr h)
    · exact Or.inr h
  have hne : dst ≠ r := by
    rintro rfl
    rcases hact with h | h <;> rw [hst] at h <;> cases h
  have hq2 : isQuorum (g.machines r).fields.Proposer_promises = true := hquorum ⟨r⟩ hr hact
  let e : Msg E := ⟨i, src, dst, E.ePromise pl⟩
  have he : is_ePromise e := ⟨pl, rfl⟩
  have hsent : sent e g := hlog
  -- An acceptor in both quorums promised the round to both.
  obtain ⟨a, ha1, ha2⟩ := hq _ _ hq1 hq2
  rcases mem_add_cases ha1 with rfl | ha1
  · exact hne (hcounted e he ⟨r⟩ hr hsent ha2 (by show pl.round = _; rw [hrnd, hreq]))
  · exact hne (congrArg Proposer.ref (howner ⟨dst⟩ hdst ⟨r⟩ hr a ha1 ha2 hreq.symm))

/-! ## Recovered votes -/

/-- A recovered vote was proposed by a Proposer of its round whose `Active` entry has run. -/
theorem proposer_of_recovered (g : GW) (hrep : recovered_proposed g) (hfrom : accept_from g)
    (hacc : accept_proposed g) (hpa : proposals_active g) (hbelow : recovered_below g)
    (p : MachineRef) (hp : is_Proposer p g) (j : Int) (w : tVote)
    (hw : (g.machines p).fields.Proposer_recovered.get j = some w) :
    ∃ p' : MachineRef, (g.machines p').state = some S.Proposer_Active ∧
      (g.machines p').pending = none ∧
      (g.machines p').fields.Proposer_round = w.round ∧
      w.round < (g.machines p).fields.Proposer_round ∧
      (g.machines p').fields.Proposer_proposals.get j = some w.value := by
  have hlt : w.round < (g.machines p).fields.Proposer_round := hbelow ⟨p⟩ hp j w hw
  obtain ⟨e', he', hs', hr', hi', hx'⟩ := hrep ⟨p⟩ hp j w hw
  have hP := hfrom e' he' hs'
  obtain ⟨hact, hround, hprop⟩ := hacc e' he' ⟨(eAccept_payload_of e').proposer⟩ hP hs' rfl
  have hround' : (eAccept_payload_of e').round =
      (g.machines (eAccept_payload_of e').proposer).fields.Proposer_round := hround
  have hprop' : (g.machines (eAccept_payload_of e').proposer).fields.Proposer_proposals.get
      (eAccept_payload_of e').inst = some (eAccept_payload_of e').value := hprop
  have hr'' : (eAccept_payload_of e').round = w.round := hr'
  have hi'' : (eAccept_payload_of e').inst = j := hi'
  have hx'' : (eAccept_payload_of e').value = w.value := hx'
  rw [hi'', hx''] at hprop'
  have hpend : (g.machines (eAccept_payload_of e').proposer).pending = none :=
    (hpa ⟨(eAccept_payload_of e').proposer⟩ hP j (mem_of_value hprop')).2
  exact ⟨(eAccept_payload_of e').proposer, hact, hpend, by omega, hlt, hprop'⟩

/-! ## Ivy's filter

Which recovered values the entry of `Active` proposes: all of them when none is `stop`;
otherwise, with `im` the stop of the highest round, the non-stops outside the slots above
`im` with a lower round when the stop at `im` is voided, and the values up to `im` but the
other stops when it is not. -/

/-- The filter passes at slot `i`, whose recovered vote `v` has the value `x1` and a round
of at least `r1`, when every recovered vote of a round of at least `r1` was proposed by a
Proposer that also has `x1` at `i`, so that (by `stop_top`) a stop at `j` puts `i` at or
below `j`, and `x1 = stop` puts `j` at or below `i` (hypothesis `K2`). -/
theorem filter_passes (m : PMap Int tVote) (hs vd : Bool) (im imr vs i r1 x1 : Int)
    (v : tVote) (hv : m.get i = some v) (hvx : v.value = x1) (hvr : r1 ≤ v.round)
    (F1a : hs = true → (m.get im).isSome = true)
    (F1b : ∀ w : tVote, hs = true → m.get im = some w → w.value = stop ∧ w.round = imr)
    (F2 : ∀ (j : Int) (w : tVote), m.get j = some w → w.value = stop → hs = true ∧ w.round ≤ imr)
    (F3a : vd = true → (m.get vs).isSome = true ∧ im < vs)
    (F3b : ∀ w : tVote, vd = true → m.get vs = some w → imr < w.round)
    (F4 : ∀ (j : Int) (w : tVote), hs = true → m.get j = some w → im < j → imr < w.round →
      vd = true)
    (K2 : ∀ (j : Int) (w : tVote), m.get j = some w → r1 ≤ w.round →
      (w.value = stop → i ≤ j) ∧ (x1 = stop → j ≤ i)) :
    hs = false ∨ (vd = true ∧ ¬ (v.value = stop ∨ (im < i ∧ v.round < imr))) ∨
      (vd = false ∧ ¬ ((i ≠ im ∧ v.value = stop) ∨ im < i)) := by
  cases hs with
  | false => exact Or.inl rfl
  | true =>
    right
    obtain ⟨wim, hwim⟩ := Option.isSome_iff_exists.mp (F1a rfl)
    obtain ⟨hwv, hwr⟩ := F1b wim rfl hwim
    by_cases hx : x1 = stop
    · -- a stop: `im` is its slot, and it is not voided
      have hvs : v.value = stop := hvx.trans hx
      have hle : v.round ≤ imr := (F2 i v hv hvs).2
      have him : im = i := by
        have h := K2 im wim hwim (by omega)
        have h1 := h.1 hwv
        have h2 := h.2 hx
        omega
      cases vd with
      | true =>
        exfalso
        obtain ⟨hk, hlt⟩ := F3a rfl
        obtain ⟨wvs, hwvs⟩ := Option.isSome_iff_exists.mp hk
        have hr := F3b wvs rfl hwvs
        have h := (K2 vs wvs hwvs (by omega)).2 hx
        omega
      | false =>
        right
        refine ⟨rfl, ?_⟩
        rintro (⟨hne, -⟩ | hlt)
        · exact hne him.symm
        · omega
    · have hvs : v.value ≠ stop := fun h => hx (hvx.symm.trans h)
      cases vd with
      | true =>
        left
        refine ⟨rfl, ?_⟩
        rintro (h | ⟨hlt, hlr⟩)
        · exact hvs h
        · have h := (K2 im wim hwim (by omega)).1 hwv
          omega
      | false =>
        right
        refine ⟨rfl, ?_⟩
        rintro (⟨-, h⟩ | hlt)
        · exact hvs h
        · have hle : ¬ imr < v.round := fun h => by
            have := F4 i v rfl hv hlt h
            cases this
          have h := (K2 im wim hwim (by omega)).1 hwv
          omega

/-- For an activated Proposer `r2`: the filter passes at slot `k`, whose recovered vote `v`
has the value `x1` and a round of at least `r1`, when every Proposer whose `Active` entry
has run, of a round from `r1` up to `r2`'s, has `x1` at `k`. -/
theorem admitted_of_higher (g : GW) (hrep : recovered_proposed g) (hfrom : accept_from g)
    (hacc : accept_proposed g) (hpa : proposals_active g) (hbelow : recovered_below g)
    (htop : stop_top g) (hF1a : im_recovered g) (hF1b : im_stop g) (hF2 : im_highest g)
    (hF3a : void_slot g) (hF3b : void_round g) (hF4 : void_complete g)
    (r2 : MachineRef) (hr2 : (g.machines r2).state = some S.Proposer_Active)
    (k r1 x1 : Int) (v : tVote) (hv : (g.machines r2).fields.Proposer_recovered.get k = some v)
    (hvx : v.value = x1) (hvr : r1 ≤ v.round)
    (hhigher : ∀ p : MachineRef, (g.machines p).state = some S.Proposer_Active →
      (g.machines p).pending = none → r1 ≤ (g.machines p).fields.Proposer_round →
      (g.machines p).fields.Proposer_round < (g.machines r2).fields.Proposer_round →
      (g.machines p).fields.Proposer_proposals.get k = some x1) :
    (g.machines r2).fields.Proposer_has_stop = false ∨
      ((g.machines r2).fields.Proposer_voided = true ∧
        ¬ (v.value = stop ∨ ((g.machines r2).fields.Proposer_im < k ∧
          v.round < (g.machines r2).fields.Proposer_im_round))) ∨
      ((g.machines r2).fields.Proposer_voided = false ∧
        ¬ ((k ≠ (g.machines r2).fields.Proposer_im ∧ v.value = stop) ∨
          (g.machines r2).fields.Proposer_im < k)) := by
  have hp2 : is_Proposer r2 g := Or.inr hr2
  refine filter_passes (g.machines r2).fields.Proposer_recovered
    (g.machines r2).fields.Proposer_has_stop (g.machines r2).fields.Proposer_voided
    (g.machines r2).fields.Proposer_im (g.machines r2).fields.Proposer_im_round
    (g.machines r2).fields.Proposer_vslot k r1 x1 v hv hvx hvr
    (fun h => hF1a ⟨r2⟩ hp2 hr2 h)
    (fun w h hw => hF1b ⟨r2⟩ hp2 w hr2 h hw)
    (fun j w hw hs => hF2 ⟨r2⟩ hp2 j w hr2 hw hs)
    (fun h => hF3a ⟨r2⟩ hp2 hr2 h)
    (fun w h hw => hF3b ⟨r2⟩ hp2 w hr2 h hw)
    (fun j w h hw hlt hlr => hF4 ⟨r2⟩ hp2 j w hr2 h hw hlt hlr) ?_
  intro j w hw hrw
  obtain ⟨pj, hact, hpend, hround, hlt, hprop⟩ :=
    proposer_of_recovered g hrep hfrom hacc hpa hbelow r2 hp2 j w hw
  have hk : (g.machines pj).fields.Proposer_proposals.get k = some x1 :=
    hhigher pj hact hpend (by omega) (by omega)
  have hpj : is_Proposer pj g := Or.inr hact
  constructor
  · intro hs
    rw [hs] at hprop
    exact htop ⟨pj⟩ hpj j k hprop (mem_of_value hk)
  · intro hs
    rw [hs] at hk
    exact htop ⟨pj⟩ hpj k j hk (mem_of_value hprop)

/-! ## A decided slot, seen from a higher round -/

/-- Ivy's filter at slot `k`, whose recovered vote is `v`, for the Proposer `r`. -/
abbrev Admits (g : GW) (r : MachineRef) (k : Int) (v : tVote) : Prop :=
  (g.machines r).fields.Proposer_has_stop = false ∨
    ((g.machines r).fields.Proposer_voided = true ∧
      ¬ (v.value = stop ∨ ((g.machines r).fields.Proposer_im < k ∧
        v.round < (g.machines r).fields.Proposer_im_round))) ∨
    ((g.machines r).fields.Proposer_voided = false ∧
      ¬ ((k ≠ (g.machines r).fields.Proposer_im ∧ v.value = stop) ∨
        (g.machines r).fields.Proposer_im < k))

/-- Two slots that the filter of an activated Proposer lets through, the first with the value
`stop`: the second is at or below it. -/
theorem admitted_stop_top (g : GW) (m : MachineRef) (hF2 : im_highest g)
    (hst : (g.machines m).state = some S.Proposer_Active)
    (j k : Int) (vj vk : tVote) (hj : (g.machines m).fields.Proposer_recovered.get j = some vj)
    (hvj : vj.value = stop) (hfj : Admits g m j vj)
    (hfk : Admits g m k vk) : k ≤ j := by
  have hs : (g.machines m).fields.Proposer_has_stop = true := (hF2 ⟨m⟩ (Or.inr hst) j vj hst hj hvj).1
  rcases hfj with h | ⟨-, h⟩ | ⟨hv, h⟩
  · rw [hs] at h; cases h
  · exact absurd (Or.inl hvj) h
  · have him : j = (g.machines m).fields.Proposer_im := 
      Classical.byContradiction fun hne => h (Or.inl ⟨hne, hvj⟩)
    rcases hfk with h' | ⟨hv', -⟩ | ⟨-, h'⟩
    · rw [hs] at h'; cases h'
    · rw [hv] at hv'; cases hv'
    · have : ¬ (g.machines m).fields.Proposer_im < k := fun hlt => h' (Or.inr hlt)
      omega

/-- So in a map of proposals made of slots the filter lets through, with their recovered
values (what the entry of `Active` builds), nothing is above a `stop`. -/
theorem props_stop_top (g : GW) (m : MachineRef) (hF2 : im_highest g)
    (hst : (g.machines m).state = some S.Proposer_Active) (b : PMap Int Int)
    (hb : ∀ (j x : Int), b.get j = some x →
      ((g.machines m).fields.Proposer_recovered.get j).isSome = true ∧
      ∀ v : tVote, (g.machines m).fields.Proposer_recovered.get j = some v →
        x = v.value ∧ Admits g m j v)
    (j k : Int) (hj : b.get j = some stop) (hk : (b.get k).isSome = true) : k ≤ j := by
  obtain ⟨hjr, hjv⟩ := hb j stop hj
  obtain ⟨vj, hvj⟩ := Option.isSome_iff_exists.mp hjr
  obtain ⟨xk, hxk⟩ := Option.isSome_iff_exists.mp hk
  obtain ⟨hkr, hkv⟩ := hb k xk hxk
  obtain ⟨vk, hvk⟩ := Option.isSome_iff_exists.mp hkr
  obtain ⟨hx, hfj⟩ := hjv vj hvj
  obtain ⟨-, hfk⟩ := hkv vk hvk
  exact admitted_stop_top g m hF2 hst j k vj vk hvj hx.symm hfj hfk

/-- A Proposer of a round above that of a Proposer that decided slot `k`, whose `Active`
entry has run, proposed the decided value there. -/
theorem reproposed_of (g : GW) (hkey : decided_recovered_key g) (hdr : decided_recovered g)
    (hadm : admitted_proposed g)
    (r1 r2 : MachineRef) (k x1 : Int) (hr1 : is_Proposer r1 g)
    (hdec : (g.machines r1).fields.Proposer_decided.mem k = true)
    (hlt : (g.machines r1).fields.Proposer_round < (g.machines r2).fields.Proposer_round)
    (hx1 : (g.machines r1).fields.Proposer_proposals.get k = some x1)
    (hr2 : (g.machines r2).state = some S.Proposer_Active)
    (hpend : (g.machines r2).pending = none) :
    (g.machines r2).fields.Proposer_proposals.get k = some x1 := by
  have hp2 : is_Proposer r2 g := Or.inr hr2
  obtain ⟨v, hv⟩ := value_of_mem (hkey ⟨r1⟩ hr1 ⟨r2⟩ hp2 k hdec hlt hr2 hpend)
  obtain ⟨hvx, hadmits⟩ := hdr ⟨r1⟩ hr1 ⟨r2⟩ hp2 k x1 v hdec hlt hx1 hr2 hpend hv
  have h : (g.machines r2).fields.Proposer_proposals.get k = some v.value :=
    hadm ⟨r2⟩ hp2 k v hr2 hpend hv hadmits
  rw [h, hvx]

/-- So two Proposers, one of which decided slot `k` in a lower round, agree on it. -/
theorem blocks_of (g : GW) (hkey : decided_recovered_key g) (hdr : decided_recovered g)
    (hadm : admitted_proposed g) (hpa : proposals_active g)
    (r1 r2 : MachineRef) (k x1 x2 : Int) (hr1 : is_Proposer r1 g) (hr2 : is_Proposer r2 g)
    (hdec : (g.machines r1).fields.Proposer_decided.mem k = true)
    (hlt : (g.machines r1).fields.Proposer_round < (g.machines r2).fields.Proposer_round)
    (hx1 : (g.machines r1).fields.Proposer_proposals.get k = some x1)
    (hx2 : (g.machines r2).fields.Proposer_proposals.get k = some x2) : x1 = x2 := by
  obtain ⟨hact, hpend⟩ := hpa ⟨r2⟩ hr2 k (mem_of_value hx2)
  have h := reproposed_of g hkey hdr hadm r1 r2 k x1 hr1 hdec hlt hx1 hact hpend
  rw [h] at hx2
  exact Option.some.inj hx2

/-- A Proposer activated in a round above that of a Proposer that decided slot `k` recovered
the decision: its vote at `k` has the decided value, and a round at least the decider's. -/
theorem active_recovers_decided (g : GW) (dst : MachineRef) (k : Int)
    (hq : quorum_intersection g) (hquorum : active_quorum g) (hprom : promises_promised g)
    (hcov : promise_covered g) (hdom : promise_dominated g) (hbelow : recovered_below g)
    (hrep : recovered_proposed g) (hfrom : accept_from g) (hacc : accept_proposed g)
    (hpa : proposals_active g) (hcnt : decided_counted g)
    (hvoted : acks_voted g) (hdp : decided_proposed g)
    (hdq : decided_quorum g) (hgk : promise_gap_key g) (hgap : promise_gap g)
    (hown : round_owner g) (hkey : decided_recovered_key g) (hdr : decided_recovered g)
    (hadm : admitted_proposed g)
    (hst : (g.machines dst).state = some S.Proposer_Active)
    (r1 : MachineRef) (x1 : Int) (hr1 : is_Proposer r1 g)
    (hdec : (g.machines r1).fields.Proposer_decided.mem k = true)
    (hlt : (g.machines r1).fields.Proposer_round < (g.machines dst).fields.Proposer_round)
    (hx1 : (g.machines r1).fields.Proposer_proposals.get k = some x1) :
    ∃ v : tVote, (g.machines dst).fields.Proposer_recovered.get k = some v ∧ v.value = x1 ∧
      (g.machines r1).fields.Proposer_round ≤ v.round := by
  have hdst : is_Proposer dst g := Or.inr hst
  have hk1 : k ∈ (g.machines r1).fields.Proposer_proposals := hdp ⟨r1⟩ hr1 k hdec
  have hr1act : (g.machines r1).state = some S.Proposer_Active := (hpa ⟨r1⟩ hr1 k hk1).1
  obtain ⟨q, hq'⟩ := value_of_mem (hcnt ⟨r1⟩ hr1 k hdec)
  have hqq : isQuorum q = true := hdq ⟨r1⟩ hr1 k q hdec hq'
  have hq2 : isQuorum (g.machines dst).fields.Proposer_promises = true :=
    hquorum ⟨dst⟩ hdst (Or.inr hst)
  -- An acceptor that voted for `r1` at `k` promised `dst`'s round.
  obtain ⟨a, ha1, ha2⟩ := hq _ _ hqq hq2
  obtain ⟨e, he, hs, -, hea, her⟩ := hprom ⟨dst⟩ hdst a ha2
  have hea' : (ePromise_payload_of e).acceptor = a := hea
  have her' : (ePromise_payload_of e).round = (g.machines dst).fields.Proposer_round := her
  have hmem : (ePromise_payload_of e).acceptor ∈ (g.machines dst).fields.Proposer_promises := by
    rw [hea']; exact ha2
  -- Its promise reports that vote, or a later one.
  obtain ⟨e2, he2, hs2, -, hea2, her2, hei2⟩ := hvoted ⟨r1⟩ hr1 k q a hq' ha1
  have hea2' : (eAccepted_payload_of e2).acceptor = a := hea2
  have her2' : (eAccepted_payload_of e2).round = (g.machines r1).fields.Proposer_round := her2
  have hei2' : (eAccepted_payload_of e2).inst = k := hei2
  have hacc2 : (ePromise_payload_of e).acceptor = (eAccepted_payload_of e2).acceptor := by
    rw [hea', hea2']
  have hlt2 : (eAccepted_payload_of e2).round < (ePromise_payload_of e).round := by omega
  obtain ⟨v, hv⟩ := value_of_mem (hgk e he e2 he2 hs hs2 hacc2 hlt2)
  have hvr : (eAccepted_payload_of e2).round ≤ v.round := hgap e he e2 he2 v hs hs2 hacc2 hlt2 hv
  rw [hei2'] at hv
  obtain ⟨v', hv'⟩ := value_of_mem (hcov ⟨dst⟩ hdst e he k hs hmem her' (mem_of_value hv))
  have hle : v.round ≤ v'.round := hdom ⟨dst⟩ hdst e he k v v' hs hmem her' hv hv'
  obtain ⟨p', hact', hpend', hround', -, hprop'⟩ :=
    proposer_of_recovered g hrep hfrom hacc hpa hbelow dst hdst k v' hv'
  refine ⟨v', hv', ?_, by omega⟩
  have hp' : is_Proposer p' g := Or.inr hact'
  by_cases heq : v'.round = (g.machines r1).fields.Proposer_round
  · -- proposed in `r1`'s round: by `r1`
    have hsame := hown ⟨p'⟩ hp' ⟨r1⟩ hr1 (Or.inr hact') (Or.inr hr1act)
      (show (g.machines p').fields.Proposer_round = (g.machines r1).fields.Proposer_round by omega)
    have hpr : p' = r1 := congrArg Proposer.ref hsame
    rw [hpr, hx1] at hprop'
    exact (Option.some.inj hprop').symm
  · -- proposed above `r1`'s round, so with `r1`'s decided value
    have h := reproposed_of g hkey hdr hadm r1 p' k x1 hr1 hdec
      (show (g.machines r1).fields.Proposer_round < (g.machines p').fields.Proposer_round by omega)
      hx1 hact' hpend'
    rw [h] at hprop'
    exact (Option.some.inj hprop').symm

/-- What the entry of `Active` needs for a slot decided below its Proposer's round: the
slot was recovered with the decided value, and the filter passes there. -/
theorem entry_key (g : GW) (dst : MachineRef)
    (hq : quorum_intersection g) (hquorum : active_quorum g) (hprom : promises_promised g)
    (hcov : promise_covered g) (hdom : promise_dominated g) (hbelow : recovered_below g)
    (hrep : recovered_proposed g) (hfrom : accept_from g) (hacc : accept_proposed g)
    (hpa : proposals_active g) (hcnt : decided_counted g)
    (hvoted : acks_voted g) (hdp : decided_proposed g)
    (hdq : decided_quorum g) (hgk : promise_gap_key g) (hgap : promise_gap g)
    (hown : round_owner g) (hkey : decided_recovered_key g) (hdr : decided_recovered g)
    (hadm : admitted_proposed g)
    (htop : stop_top g) (hF1a : im_recovered g) (hF1b : im_stop g) (hF2 : im_highest g)
    (hF3a : void_slot g) (hF3b : void_round g) (hF4 : void_complete g)
    (hst : (g.machines dst).state = some S.Proposer_Active)
    (r1 : MachineRef) (k x1 : Int) (hr1 : is_Proposer r1 g)
    (hdec : (g.machines r1).fields.Proposer_decided.mem k = true)
    (hlt : (g.machines r1).fields.Proposer_round < (g.machines dst).fields.Proposer_round)
    (hx1 : (g.machines r1).fields.Proposer_proposals.get k = some x1) :
    ((g.machines dst).fields.Proposer_recovered.get k).isSome = true ∧
      ∀ v : tVote, (g.machines dst).fields.Proposer_recovered.get k = some v →
        v.value = x1 ∧ Admits g dst k v := by
  obtain ⟨v, hv, hvx, hvr⟩ := active_recovers_decided g dst k hq hquorum hprom hcov hdom hbelow
    hrep hfrom hacc hpa hcnt hvoted hdp hdq hgk hgap hown hkey hdr hadm hst r1 x1 hr1 hdec hlt
    hx1
  have hk1 : k ∈ (g.machines r1).fields.Proposer_proposals := hdp ⟨r1⟩ hr1 k hdec
  have hr1act : (g.machines r1).state = some S.Proposer_Active := (hpa ⟨r1⟩ hr1 k hk1).1
  have hhigher : ∀ p : MachineRef, (g.machines p).state = some S.Proposer_Active →
      (g.machines p).pending = none →
      (g.machines r1).fields.Proposer_round ≤ (g.machines p).fields.Proposer_round →
      (g.machines p).fields.Proposer_round < (g.machines dst).fields.Proposer_round →
      (g.machines p).fields.Proposer_proposals.get k = some x1 := by
    intro p hp hpp hge _
    have hp' : is_Proposer p g := Or.inr hp
    by_cases heq : (g.machines p).fields.Proposer_round = (g.machines r1).fields.Proposer_round
    · have hsame := hown ⟨p⟩ hp' ⟨r1⟩ hr1 (Or.inr hp) (Or.inr hr1act) heq
      have hpr : p = r1 := congrArg Proposer.ref hsame
      rw [hpr]; exact hx1
    · exact reproposed_of g hkey hdr hadm r1 p k x1 hr1 hdec
        (show (g.machines r1).fields.Proposer_round < (g.machines p).fields.Proposer_round by
          omega) hx1 hp hpp
  have hfilter := admitted_of_higher g hrep hfrom hacc hpa hbelow htop hF1a hF1b hF2 hF3a hF3b
    hF4 dst hst k (g.machines r1).fields.Proposer_round x1 v hv hvx hvr hhigher
  refine ⟨by rw [hv]; rfl, fun v' hv' => ?_⟩
  rw [hv] at hv'
  cases hv'
  exact ⟨hvx, hfilter⟩

/-! ## Deciding -/

/-- When a Proposer counts the vote `pl` and completes a quorum at a slot, every Proposer of
a higher round whose `Active` entry has run recovered its value there, and its filter lets
it through (so it proposed it, by `admitted_proposed`): by induction on that round, through
the recovered votes of the rounds in between. -/
theorem vote_quorum_recovered (g : GW) (i : Nat) (src dst : MachineRef)
    (pl : eAccepted_payload)
    (hq : quorum_intersection g) (hquorum : active_quorum g) (hprom : promises_promised g)
    (hcov : promise_covered g) (hdom : promise_dominated g) (hbelow : recovered_below g)
    (hrep : recovered_proposed g) (hfrom : accept_from g) (hacc : accept_proposed g)
    (hpa : proposals_active g) (hvoted : acks_voted g)
    (hgk : promise_gap_key g) (hgap : promise_gap g) (hown : round_owner g)
    (htop : stop_top g) (hF1a : im_recovered g) (hF1b : im_stop g) (hF2 : im_highest g)
    (hF3a : void_slot g) (hF3b : void_round g) (hF4 : void_complete g)
    (hadm : admitted_proposed g)
    (hlog : g.log i = some { src := src, dst := dst, ev := E.eAccepted pl })
    (hst : (g.machines dst).state = some S.Proposer_Active)
    (hrnd : pl.round = (g.machines dst).fields.Proposer_round)
    (cur : PSet MachineRef)
    (hcur : (g.machines dst).fields.Proposer_accept_acks.get pl.inst = some cur)
    (hq1 : isQuorum (cur.add pl.acceptor) = true)
    (x1 : Int) (hx1 : (g.machines dst).fields.Proposer_proposals.get pl.inst = some x1)
    (r2 : MachineRef) (hr2 : (g.machines r2).state = some S.Proposer_Active)
    (hpend : (g.machines r2).pending = none)
    (hlt : (g.machines dst).fields.Proposer_round < (g.machines r2).fields.Proposer_round) :
    ((g.machines r2).fields.Proposer_recovered.get pl.inst).isSome = true ∧
      ∀ v : tVote, (g.machines r2).fields.Proposer_recovered.get pl.inst = some v →
        v.value = x1 ∧ Admits g r2 pl.inst v := by
  have hdst : is_Proposer dst g := Or.inr hst
  let em : Msg E := ⟨i, src, dst, E.eAccepted pl⟩
  have hem : is_eAccepted em := ⟨pl, rfl⟩
  have hsent : sent em g := hlog
  suffices step : ∀ (n : Nat) (r2 : MachineRef), (g.machines r2).state = some S.Proposer_Active →
      (g.machines r2).pending = none →
      (g.machines dst).fields.Proposer_round < (g.machines r2).fields.Proposer_round →
      ((g.machines r2).fields.Proposer_round - (g.machines dst).fields.Proposer_round).toNat ≤ n →
      ((g.machines r2).fields.Proposer_recovered.get pl.inst).isSome = true ∧
        ∀ v : tVote, (g.machines r2).fields.Proposer_recovered.get pl.inst = some v →
          v.value = x1 ∧ Admits g r2 pl.inst v from
    step _ r2 hr2 hpend hlt (Nat.le_refl _)
  intro n
  induction n with
  | zero => intro r2 _ _ hlt hn; omega
  | succ n ih =>
    intro r2 hr2 hpend hlt hn
    have hr2' : is_Proposer r2 g := Or.inr hr2
    have hc2 : isQuorum (g.machines r2).fields.Proposer_promises = true :=
      hquorum ⟨r2⟩ hr2' (Or.inr hr2)
    -- An acceptor that voted for `dst` at the slot promised `r2`'s round later, so its
    -- promise reports that vote or a later one.
    have hrep2 : ∃ e : Msg E, ∃ v : tVote, is_ePromise e ∧ sent e g ∧
        (ePromise_payload_of e).acceptor ∈ (g.machines r2).fields.Proposer_promises ∧
        (ePromise_payload_of e).round = (g.machines r2).fields.Proposer_round ∧
        (ePromise_payload_of e).votes.get pl.inst = some v ∧
        (g.machines dst).fields.Proposer_round ≤ v.round := by
      have hvote : ∃ a : MachineRef, a ∈ (g.machines r2).fields.Proposer_promises ∧
          ∃ e2 : Msg E, is_eAccepted e2 ∧ sent e2 g ∧ (eAccepted_payload_of e2).acceptor = a ∧
            (eAccepted_payload_of e2).round = (g.machines dst).fields.Proposer_round ∧
            (eAccepted_payload_of e2).inst = pl.inst := by
        obtain ⟨a, ha1, ha2⟩ := hq _ _ hq1 hc2
        rcases mem_add_cases ha1 with rfl | ha1
        · exact ⟨pl.acceptor, ha2, em, hem, hsent, rfl, hrnd, rfl⟩
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
    obtain ⟨v', hv'⟩ := value_of_mem (hcov ⟨r2⟩ hr2' e he pl.inst hs hmem her (mem_of_value hv))
    have hle : v.round ≤ v'.round := hdom ⟨r2⟩ hr2' e he pl.inst v v' hs hmem her hv hv'
    -- Every Proposer of a round from `dst`'s up to `r2`'s has `x1` at the slot.
    have hhigher : ∀ p : MachineRef, (g.machines p).state = some S.Proposer_Active →
        (g.machines p).pending = none →
        (g.machines dst).fields.Proposer_round ≤ (g.machines p).fields.Proposer_round →
        (g.machines p).fields.Proposer_round < (g.machines r2).fields.Proposer_round →
        (g.machines p).fields.Proposer_proposals.get pl.inst = some x1 := by
      intro p hp hpp hge hlt'
      by_cases heq : (g.machines p).fields.Proposer_round = (g.machines dst).fields.Proposer_round
      · have hsame := hown ⟨p⟩ (Or.inr hp) ⟨dst⟩ hdst (Or.inr hp) (Or.inr hst) heq
        have hpr : p = dst := congrArg Proposer.ref hsame
        rw [hpr]; exact hx1
      · obtain ⟨hk, hall⟩ := ih p hp hpp (by omega) (by omega)
        obtain ⟨w, hw⟩ := Option.isSome_iff_exists.mp hk
        obtain ⟨hwx, hwa⟩ := hall w hw
        have h : (g.machines p).fields.Proposer_proposals.get pl.inst = some w.value :=
          hadm ⟨p⟩ (Or.inr hp) pl.inst w hp hpp hw hwa
        rw [h, hwx]
    -- `r2` recovered a vote of the value `x1`, which its filter admits.
    obtain ⟨p', hact', hpend', hround', hlt', hprop'⟩ :=
      proposer_of_recovered g hrep hfrom hacc hpa hbelow r2 hr2' pl.inst v' hv'
    have hvx : v'.value = x1 := by
      have h := hhigher p' hact' hpend' (by omega) (by omega)
      rw [h] at hprop'
      exact (Option.some.inj hprop').symm
    have hfilter := admitted_of_higher g hrep hfrom hacc hpa hbelow htop hF1a hF1b hF2 hF3a
      hF3b hF4 r2 hr2 pl.inst (g.machines dst).fields.Proposer_round x1 v' hv' hvx (by omega)
      hhigher
    refine ⟨by rw [hv']; rfl, fun w hw => ?_⟩
    rw [hv'] at hw
    cases hw
    exact ⟨hvx, hfilter⟩

/-- When a Proposer counts the vote `pl` and completes a quorum at a slot, its value there is
that of every Proposer that decided the slot. -/
theorem vote_quorum_agrees (g : GW) (i : Nat) (src dst : MachineRef) (pl : eAccepted_payload)
    (hq : quorum_intersection g) (hquorum : active_quorum g) (hprom : promises_promised g)
    (hcov : promise_covered g) (hdom : promise_dominated g) (hbelow : recovered_below g)
    (hrep : recovered_proposed g) (hfrom : accept_from g) (hacc : accept_proposed g)
    (hpa : proposals_active g) (hvoted : acks_voted g)
    (hgk : promise_gap_key g) (hgap : promise_gap g) (hown : round_owner g)
    (htop : stop_top g) (hF1a : im_recovered g) (hF1b : im_stop g) (hF2 : im_highest g)
    (hF3a : void_slot g) (hF3b : void_round g) (hF4 : void_complete g)
    (hadm : admitted_proposed g) (hkey : decided_recovered_key g) (hdr : decided_recovered g)
    (hlog : g.log i = some { src := src, dst := dst, ev := E.eAccepted pl })
    (hst : (g.machines dst).state = some S.Proposer_Active)
    (hrnd : pl.round = (g.machines dst).fields.Proposer_round)
    (hnew : ¬ (g.machines dst).fields.Proposer_decided.mem pl.inst = true)
    (cur : PSet MachineRef)
    (hcur : (g.machines dst).fields.Proposer_accept_acks.get pl.inst = some cur)
    (hq1 : isQuorum (cur.add pl.acceptor) = true)
    (x1 : Int) (hx1 : (g.machines dst).fields.Proposer_proposals.get pl.inst = some x1)
    (r2 : MachineRef) (x2 : Int) (hr2 : is_Proposer r2 g)
    (hdec : (g.machines r2).fields.Proposer_decided.mem pl.inst = true)
    (hx2 : (g.machines r2).fields.Proposer_proposals.get pl.inst = some x2) : x1 = x2 := by
  have hdst : is_Proposer dst g := Or.inr hst
  obtain ⟨hact2, hpend2⟩ := hpa ⟨r2⟩ hr2 pl.inst (mem_of_value hx2)
  have hact2' : (g.machines r2).state = some S.Proposer_Active := hact2
  have hpend2' : (g.machines r2).pending = none := hpend2
  rcases Int.lt_trichotomy (g.machines dst).fields.Proposer_round
      (g.machines r2).fields.Proposer_round with hlt | heq | hgt
  · obtain ⟨hk, hall⟩ := vote_quorum_recovered g i src dst pl hq hquorum hprom hcov hdom hbelow
      hrep hfrom hacc hpa hvoted hgk hgap hown htop hF1a hF1b hF2 hF3a hF3b hF4 hadm hlog hst
      hrnd cur hcur hq1 x1 hx1 r2 hact2' hpend2' hlt
    obtain ⟨w, hw⟩ := Option.isSome_iff_exists.mp hk
    obtain ⟨hwx, hwa⟩ := hall w hw
    have h : (g.machines r2).fields.Proposer_proposals.get pl.inst = some w.value :=
      hadm ⟨r2⟩ hr2 pl.inst w hact2' hpend2' hw hwa
    rw [h] at hx2
    rw [← hwx]
    exact Option.some.inj hx2
  · -- the same round: the same Proposer, which had not decided
    have hsame := hown ⟨dst⟩ hdst ⟨r2⟩ hr2 (Or.inr hst) (Or.inr hact2') heq
    have hpr : dst = r2 := congrArg Proposer.ref hsame
    rw [← hpr] at hdec
    exact absurd hdec hnew
  · exact (blocks_of g hkey hdr hadm hpa r2 dst pl.inst x2 x1 hr2 hdst hdec hgt hx2 hx1).symm

/-- When a Proposer counts the vote `pl` and completes a quorum at a slot, its value there is
that of every `eLearn` sent for the slot. -/
theorem vote_quorum_taught (g : GW) (i : Nat) (src dst : MachineRef) (pl : eAccepted_payload)
    (hq : quorum_intersection g) (hquorum : active_quorum g) (hprom : promises_promised g)
    (hcov : promise_covered g) (hdom : promise_dominated g) (hbelow : recovered_below g)
    (hrep : recovered_proposed g) (hfrom : accept_from g) (hacc : accept_proposed g)
    (hpa : proposals_active g) (hvoted : acks_voted g)
    (hgk : promise_gap_key g) (hgap : promise_gap g) (hown : round_owner g)
    (htop : stop_top g) (hF1a : im_recovered g) (hF1b : im_stop g) (hF2 : im_highest g)
    (hF3a : void_slot g) (hF3b : void_round g) (hF4 : void_complete g)
    (hadm : admitted_proposed g) (hkey : decided_recovered_key g) (hdr : decided_recovered g)
    (hsrc : learn_source g) (hld : learn_decided g)
    (hlog : g.log i = some { src := src, dst := dst, ev := E.eAccepted pl })
    (hst : (g.machines dst).state = some S.Proposer_Active)
    (hrnd : pl.round = (g.machines dst).fields.Proposer_round)
    (hnew : ¬ (g.machines dst).fields.Proposer_decided.mem pl.inst = true)
    (cur : PSet MachineRef)
    (hcur : (g.machines dst).fields.Proposer_accept_acks.get pl.inst = some cur)
    (hq1 : isQuorum (cur.add pl.acceptor) = true)
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
  exact vote_quorum_agrees g i src dst pl hq hquorum hprom hcov hdom hbelow hrep hfrom hacc hpa
    hvoted hgk hgap hown htop hF1a hF1b hF2 hF3a hF3b hF4 hadm hkey hdr hlog hst hrnd hnew cur
    hcur hq1 x1 hx1 s' b.value hP hdec' hprop'

/-! ## The obligations of those steps

Each hands its lemmas to `pverify_auto` in place of the invariants they used. -/

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
      rounds g ∧ quorum g ∧ tally g ∧ counted g →
      g.net i = some { src := src, dst := dst, ev := E.ePromise pl } →
      g.log i = some { src := src, dst := dst, ev := E.ePromise pl } →
      i < g.clock → (g.machines dst).state = some S.Proposer_Prepare →
      (g.machines dst).pending = none → (g.machines dst).waiting = none →
      (g.machines dst).halted = false →
      RunPost (fun g' => PostInv "round_owner" (round_owner g')) (GWorld.consume g i) dst
        (g.machines dst) (Proposer.Prepare.ePromise_handler pl) := by
  intro g i src dst pl hF hE hInv hnet hlog hi hst
  obtain ⟨hown, hq, ⟨-, -, hquorum⟩, ⟨-, hcounted, howner⟩⟩ := hInv
  have key := promise_quorum_owner g i src dst pl hq hquorum hcounted howner hlog hst
  clear hq hquorum hcounted howner
  pverify_auto

set_option maxHeartbeats 4000000 in
@[pverifyProof] theorem Proposer.Active.eAccepted_correct_agreement :
    ∀ (g : GW) (i : Nat) (src dst : MachineRef) (pl : eAccepted_payload),
      Framework g → g.error = none →
      agreement g ∧ quorum g ∧ tally g ∧ counted g ∧ recovery g ∧ reported g ∧ proposed g ∧
        activation g ∧ stops g ∧ acks g ∧ gap g ∧ rounds g →
      g.net i = some { src := src, dst := dst, ev := E.eAccepted pl } →
      g.log i = some { src := src, dst := dst, ev := E.eAccepted pl } →
      i < g.clock → (g.machines dst).state = some S.Proposer_Active →
      (g.machines dst).pending = none → (g.machines dst).waiting = none →
      (g.machines dst).halted = false →
      RunPost
        (fun g' =>
          PostInv "decided_recovered_key" (decided_recovered_key g') ∧
            PostInv "decided_recovered" (decided_recovered g') ∧
            PostInv "decided_agree" (decided_agree g') ∧
            PostInv "learn_source" (learn_source g') ∧ PostInv "learn_decided" (learn_decided g') ∧
            PostInv "one_value_per_slot" (one_value_per_slot g'))
        (GWorld.consume g i) dst (g.machines dst) (Proposer.Active.eAccepted_handler pl) := by
  intro g i src dst pl hF hE hInv hnet hlog hi hst
  obtain ⟨⟨hkey, hdr, hagree, hsrc, hld, hone⟩, hq, ⟨-, -, hquorum⟩, ⟨hprom, -, -⟩,
    ⟨hcov, hdom, hbelow⟩, ⟨-, -, hrep⟩, ⟨hfrom, hacc, -, -, hpa⟩,
    ⟨hF1a, hF1b, hF2, hF3a, hF3b, hF4, hadm⟩, htop, ⟨-, -, hvoted, -, -, -⟩, ⟨hgk, hgap⟩,
    hown⟩ := hInv
  have key_recovered := vote_quorum_recovered g i src dst pl hq hquorum hprom hcov hdom hbelow
    hrep hfrom hacc hpa hvoted hgk hgap hown htop hF1a hF1b hF2 hF3a hF3b hF4 hadm hlog hst
  have key_agrees := vote_quorum_agrees g i src dst pl hq hquorum hprom hcov hdom hbelow hrep
    hfrom hacc hpa hvoted hgk hgap hown htop hF1a hF1b hF2 hF3a hF3b hF4 hadm hkey hdr hlog hst
  have key_taught := vote_quorum_taught g i src dst pl hq hquorum hprom hcov hdom hbelow hrep
    hfrom hacc hpa hvoted hgk hgap hown htop hF1a hF1b hF2 hF3a hF3b hF4 hadm hkey hdr hsrc hld
    hlog hst
  simp only [Admits] at key_recovered
  clear hq hquorum hprom hcov hdom hbelow hrep hfrom hacc hvoted hgk hgap hown htop hF1a hF1b
    hF2 hF3a hF3b hF4 hadm
  pverify_auto

set_option maxHeartbeats 4000000 in
@[pverifyProof] theorem Proposer.Active.entry_correct_agreement :
    ∀ (g : GW) (m : MachineRef) (arg : G),
      Framework g → g.error = none →
      agreement g ∧ quorum g ∧ tally g ∧ counted g ∧ recovery g ∧ reported g ∧ proposed g ∧
        activation g ∧ stops g ∧ acks g ∧ gap g ∧ rounds g →
      (g.machines m).state = some S.Proposer_Active →
      (g.machines m).pending = some arg → (g.machines m).halted = false →
      RunPost
        (fun g' =>
          PostInv "decided_recovered_key" (decided_recovered_key g') ∧
            PostInv "decided_recovered" (decided_recovered g') ∧
            PostInv "decided_agree" (decided_agree g') ∧
            PostInv "learn_source" (learn_source g') ∧ PostInv "learn_decided" (learn_decided g') ∧
            PostInv "one_value_per_slot" (one_value_per_slot g'))
        g m { g.machines m with pending := none } (Proposer.Active.entry arg) := by
  intro g m arg hF hE hInv hst hpend hhalt
  obtain ⟨⟨hkey, hdr, hagree, hsrc, hld, hone⟩, hq, ⟨-, -, hquorum⟩, ⟨hprom, -, -⟩,
    ⟨hcov, hdom, hbelow⟩, ⟨-, -, hrep⟩, ⟨hfrom, hacc, -, -, hpa⟩,
    ⟨hF1a, hF1b, hF2, hF3a, hF3b, hF4, hadm⟩, htop, ⟨-, -, hvoted, hdp, hcnt, hdq⟩, ⟨hgk, hgap⟩,
    hown⟩ := hInv
  have key := entry_key g m hq hquorum hprom hcov hdom hbelow hrep hfrom hacc hpa hcnt hvoted
    hdp hdq hgk hgap hown hkey hdr hadm htop hF1a hF1b hF2 hF3a hF3b hF4 hst
  simp only [Admits] at key
  have key_mem : ∀ (r1 : MachineRef) (k : Int), is_Proposer r1 g →
      (g.machines r1).fields.Proposer_decided.mem k = true →
      (g.machines r1).fields.Proposer_round < (g.machines m).fields.Proposer_round →
      ((g.machines m).fields.Proposer_recovered.get k).isSome = true := by
    intro r1 k hr1 hdec hlt
    obtain ⟨x1, hx1⟩ := value_of_mem (hdp ⟨r1⟩ hr1 k hdec)
    exact (key r1 k x1 hr1 hdec hlt hx1).1
  -- The Proposer has decided nothing yet: its entry has not run.
  have hnodec : ∀ k : Int, ¬ (g.machines m).fields.Proposer_decided.mem k = true := by
    intro k hk
    have h : (g.machines m).pending = none :=
      (hpa ⟨m⟩ (Or.inr hst) k (hdp ⟨m⟩ (Or.inr hst) k hk)).2
    rw [hpend] at h
    cases h
  clear hq hquorum hprom hcov hdom hbelow hrep hfrom hacc hcnt hvoted hdq hgk hgap hown htop
    hF1a hF1b hF2 hF3a hF3b hF4 hadm hpa hdp
  pverify_auto

set_option maxHeartbeats 4000000 in
@[pverifyProof] theorem Proposer.Active.entry_correct_stops :
    ∀ (g : GW) (m : MachineRef) (arg : G),
      Framework g → g.error = none → stops g ∧ activation g →
      (g.machines m).state = some S.Proposer_Active →
      (g.machines m).pending = some arg → (g.machines m).halted = false →
      RunPost (fun g' => PostInv "stop_top" (stop_top g')) g m
        { g.machines m with pending := none } (Proposer.Active.entry arg) := by
  intro g m arg hF hE hInv hst hpend hhalt
  obtain ⟨htop, ⟨-, -, hF2, -, -, -, -⟩⟩ := hInv
  have key := props_stop_top g m hF2 hst
  simp only [Admits] at key
  clear hF2
  pverify_auto

set_option maxHeartbeats 4000000 in
@[pverifyProof] theorem Proposer.Active.entry_correct_active_routing :
    ∀ (g : GW) (m : MachineRef) (arg : G),
      Framework g → g.error = none → active_routing g ∧ system_config g ∧ proposed g →
      (g.machines m).state = some S.Proposer_Active →
      (g.machines m).pending = some arg → (g.machines m).halted = false →
      RunPost
        (fun g' =>
          PostInv "broadcast_active" (broadcast_active g') ∧
            PostInv "accept_active" (accept_active g') ∧
            PostInv "accepted_active" (accepted_active g'))
        g m { g.machines m with pending := none } (Proposer.Active.entry arg) := by
  intro g m arg hF hE hInv hst hpend hhalt
  obtain ⟨hrout, hsys, ⟨-, -, -, -, hpa⟩⟩ := hInv
  -- The Proposer has proposed nothing yet: its entry has not run.
  have hnoprop : ∀ k : Int, ¬ ((g.machines m).fields.Proposer_proposals.get k).isSome = true := by
    intro k hk
    have h : (g.machines m).pending = none := (hpa ⟨m⟩ (Or.inr hst) k hk).2
    rw [hpend] at h
    cases h
  clear hpa
  pverify_auto

-- The two steps that start a round change nothing the agreement speaks of: only the
-- agreement's own invariants are needed.
set_option maxHeartbeats 4000000 in
@[pverifyProof] theorem Proposer.Prepare.ePromise_correct_agreement :
    ∀ (g : GW) (i : Nat) (src dst : MachineRef) (pl : ePromise_payload),
      Framework g → g.error = none →
      agreement g ∧ quorum g ∧ tally g ∧ counted g ∧ recovery g ∧ reported g ∧ proposed g ∧
        activation g ∧ stops g ∧ acks g ∧ gap g ∧ rounds g →
      g.net i = some { src := src, dst := dst, ev := E.ePromise pl } →
      g.log i = some { src := src, dst := dst, ev := E.ePromise pl } →
      i < g.clock → (g.machines dst).state = some S.Proposer_Prepare →
      (g.machines dst).pending = none → (g.machines dst).waiting = none →
      (g.machines dst).halted = false →
      RunPost
        (fun g' =>
          PostInv "decided_recovered_key" (decided_recovered_key g') ∧
            PostInv "decided_recovered" (decided_recovered g') ∧
            PostInv "decided_agree" (decided_agree g') ∧
            PostInv "learn_source" (learn_source g') ∧ PostInv "learn_decided" (learn_decided g') ∧
            PostInv "one_value_per_slot" (one_value_per_slot g'))
        (GWorld.consume g i) dst (g.machines dst) (Proposer.Prepare.ePromise_handler pl) := by
  intro g i src dst pl hF hE hInv hnet hlog hi hst
  obtain ⟨hagr, -, -, -, -, -, -, -, -, -, -, -⟩ := hInv
  pverify_auto

set_option maxHeartbeats 4000000 in
@[pverifyProof] theorem Proposer.Activate.entry_correct_agreement :
    ∀ (g : GW) (m : MachineRef) (arg : G),
      Framework g → g.error = none →
      agreement g ∧ quorum g ∧ tally g ∧ counted g ∧ recovery g ∧ reported g ∧ proposed g ∧
        activation g ∧ stops g ∧ acks g ∧ gap g ∧ rounds g →
      (g.machines m).state = some S.Proposer_Activate →
      (g.machines m).pending = some arg → (g.machines m).halted = false →
      RunPost
        (fun g' =>
          PostInv "decided_recovered_key" (decided_recovered_key g') ∧
            PostInv "decided_recovered" (decided_recovered g') ∧
            PostInv "decided_agree" (decided_agree g') ∧
            PostInv "learn_source" (learn_source g') ∧ PostInv "learn_decided" (learn_decided g') ∧
            PostInv "one_value_per_slot" (one_value_per_slot g'))
        g m { g.machines m with pending := none } (Proposer.Activate.entry arg) := by
  intro g m arg hF hE hInv hst hpend hhalt
  obtain ⟨hagr, -, -, -, -, -, -, -, -, -, -, -⟩ := hInv
  pverify_auto

end StoppablePaxos

#pverify StoppablePaxos
