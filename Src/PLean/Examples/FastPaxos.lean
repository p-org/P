/-
FastPaxos: DuoAI's abstract Ivy model `fast_paxos.ivy` (from "Paxos Made EPR"),
single-decree Paxos in which some rounds are fast: in a fast round that need not respect an
earlier vote, the acceptors may vote for any value of `clientValues` (an arbitrary finite
set), and a fast quorum of votes decides.

A Proposer owns one round and asks every acceptor to join it (`e1a`). An Acceptor that has not
joined a higher round joins and reports its highest vote below the round, if any (`e1b`).
Once a classic quorum has joined, the Proposer chooses from the reports:
- no vote reported: "any" (`eAny`) in a fast round, its own `value` (`e2a`) in a classic one;
- otherwise, with `maxr` the highest round reported: a value `v2` that, for some fast quorum,
  each of its members in the classic quorum reports at `maxr`; if there is none, any value
  reported at `maxr`.
An Acceptor votes once per round, unless it has joined a higher one: for the round's
proposal, or after "any" for a value of `clientValues`, and tells the learners (`e2b`). A
Learner decides `(r, v)` once the votes for `v` at `r` it received cover a classic quorum
(`r` classic) or a fast quorum (`r` fast).

Rounds and values are `Int`; Ivy's `none` is an `e1b` with `has_vote = false`. Quorums are
ids in `cquorums` / `fquorums` with members `cmembers` / `fmembers`; `left_rnd(N, R)` is
`R < N.promised`; `vote(N, R, V)` is `N.votes[R]? = some V`; `decision(N, R, V)` is
`(round = R, value = V) ∈ N.decided` for a
Learner `N`.

`propose`: at every `e1b` the Proposer picks (`choose`) a classic quorum `q` and a report
`x`, and proposes if Ivy's requirements hold of them; otherwise it waits. Ivy reads the votes
of `q`'s members below the round, the Proposer their reports; these agree, since a member
has joined the round, its votes below it are frozen, it reported the highest, and it votes
once per round. Choosing `x` with `x.maxv = v2` covers Ivy's choices of `v` and `v2`. The
ghost fields `quorum` and `report` record `q` and `x` for the invariants. `propose`'s last
requirement (`fastOK`) and the decision conditions (`fcovered`, `ccovered`) are named
functions, so that invariants can record them and the solvers take them as atoms.

Initial worlds assume: the configuration of `acceptors` and `learners`; empty containers
(Ivy's `after init`); distinct Proposer rounds (Ivy lets a round be proposed in once); and
Ivy's two quorum axioms. Rounds, values, `isFast`, `clientValues` and an Acceptor's
`promised` are otherwise arbitrary: a `promised` above some round only makes Acceptors refuse
more. A quorum member need not be an Acceptor, and `cquorums` / `fquorums` may be empty; both
only add configurations.

Safety (`decisions_agree`) is Ivy's `decision(N1,R1,V1) & decision(N2,R2,V2) -> V1 = V2`.

Deviations from the Ivy model:
- `node` has two roles, Acceptor and Learner; safety covers every decision of every Learner.
- One Proposer per round does `send_1a` and `propose`. A wrong choice at the last `e1b`
  leaves it waiting, which affects liveness only.
- Messages are consumed; re-reading Ivy's relations adds nothing. Votes below a joined round
  are frozen, `cast_vote` fires once per node and round, `propose` once per round, and a
  Learner keeps the votes it received (`tally`). A message refused by its guard could never
  be processed in Ivy either, as `left_rnd` and `vote` only grow.
- A Learner decides when a vote arrives; Ivy may decide later, which changes nothing else.
- After "any" the value is one of `clientValues`; a run uses finitely many values, so some
  `clientValues` gives every choice Ivy's `cast_vote` has.

Proof: small lemmas, one kind of fact each. The protocol's argument is proved by hand, as
lemmas about one world: `proposal_blocks`, by induction on rounds, says that a Proposer that
proposed above a round at which a quorum voted for one value proposed that value;
`decide_agrees` applies it to the Learner's step, whose obligation
(`Learner.Learn.e2b_correct_safety`) is proved by hand. The quorum axioms (`quorum_axioms`) and
the monotonicity of covering (`covered_mono`, base case by hand) are invariants that do not
mention the state. `Proposer.Prepare.e1b_correct_held_reports` is proved by hand as well: the
solvers' proof of one step of it (`acceptor_after_step`) does not replay.
-/
import PLean

open PLean

/-- A bounded `∃ x, x ∈ s ∧ P x` over a set ranges over its support, so a handler can test
it. -/
instance FastPaxosSupport.decidableBEx {T : Type} [DecidableEq T] (s : PSet T) (P : T → Prop)
    [DecidablePred P] : Decidable (∃ x, x ∈ s ∧ P x) :=
  decidable_of_iff (∃ x, x ∈ s.support ∧ P x)
    ⟨fun ⟨x, hx, hp⟩ => ⟨x, (PSet.mem_support s x).mp hx, hp⟩,
     fun ⟨x, hx, hp⟩ => ⟨x, (PSet.mem_support s x).mpr hx, hp⟩⟩

pmodule FastPaxos

  system s

  type t1a      = (proposer : PLean.MachineRef, round : Int)
  type t1b      = (acceptor : PLean.MachineRef, round : Int, has_vote : Bool, maxr : Int,
                   maxv : Int)
  type t2a      = (round : Int, value : Int)
  type tAny     = (round : Int)
  type t2b      = (acceptor : PLean.MachineRef, round : Int, value : Int)
  type tDecision = (round : Int, value : Int)

  event e1a  : t1a
  event e1b  : t1b
  event e2a  : t2a
  event eAny : tAny
  event e2b  : t2b

  function acceptors    : set[PLean.MachineRef]
  function learners     : set[PLean.MachineRef]
  function isFast       : Int → Bool
  function cquorums     : set[Int]
  function cmembers     : Int → set[PLean.MachineRef]
  function fquorums     : set[Int]
  function fmembers     : Int → set[PLean.MachineRef]
  function clientValues : set[Int]

  -- `propose`'s last requirement, for the classic quorum `q`, the reports `ob`, the highest
  -- round reported `mr` and the value `v` reported at it: some fast quorum's members in `q`
  -- all report `v` at `mr`, or, for every fast quorum, `F ∩ q` is not empty and no value
  -- reported is reported at `mr` by all of `F ∩ q`.
  function fastOK (q : Int) (ob : set[t1b]) (mr v : Int) : Bool =
    decide ((∃ f, f ∈ fquorums ∧ ∀ n, n ∈ fmembers f → n ∈ cmembers q →
               ∃ y, y ∈ ob ∧ y.acceptor = n ∧ y.has_vote = true ∧ y.maxr = mr ∧ y.maxv = v) ∨
            (∀ f, f ∈ fquorums →
               (∃ n, n ∈ fmembers f ∧ n ∈ cmembers q) ∧
               ∀ z, z ∈ ob → ∃ n, n ∈ fmembers f ∧ n ∈ cmembers q ∧
                 ¬ ∃ y, y ∈ ob ∧ y.acceptor = n ∧ y.has_vote = true ∧ y.maxr = mr ∧
                   y.maxv = z.maxv))

  -- The votes `t` received cover a fast (`fcovered`) or a classic (`ccovered`) quorum of
  -- votes for `v` at `r`.
  function fcovered (t : set[t2b]) (r v : Int) : Bool =
    decide (∃ f, f ∈ fquorums ∧ ∀ n, n ∈ fmembers f →
      ({ acceptor := n, round := r, value := v } : t2b) ∈ t)
  function ccovered (t : set[t2b]) (r v : Int) : Bool =
    decide (∃ q, q ∈ cquorums ∧ ∀ n, n ∈ cmembers q →
      ({ acceptor := n, round := r, value := v } : t2b) ∈ t)

  machine Acceptor {
    var promised : Int
    var votes    : map[Int, Int]

    start state Serve {
      on e1a (m : t1a) {
        var has : Bool
        var mr  : Int
        var mv  : Int
        has = false
        if m.round ≥ promised then do
          foreach (k in votes)
            invariant max_vote : has = true → mr < m.round ∧ votes[mr]? = some mv ;
            invariant max_above : ∀ j : Int, j ∈ pref → j < m.round → has = true ∧ j ≤ mr ;
          {
            if k < m.round ∧ (has = false ∨ mr < k) then do
              has = true
              mr = k
              mv = votes[k]
          }
          promised = m.round
          send m.proposer, e1b,
            (acceptor = this.ref, round = m.round, has_vote = has, maxr = mr, maxv = mv)
      }

      on e2a (m : t2a) {
        if m.round ≥ promised ∧ ¬ (m.round ∈ votes) then do
          votes[m.round] = m.value
          foreach (l in learners)
            invariant votes_cast :
              new_sends e => e is e2b ∧ e.target ∈ learners ∧
                (e2b_payload_of e).acceptor = this.ref ∧
                (e2b_payload_of e).round = m.round ∧ (e2b_payload_of e).value = m.value ;
          {
            send l, e2b, (acceptor = this.ref, round = m.round, value = m.value)
          }
      }

      on eAny (m : tAny) {
        if m.round ≥ promised ∧ ¬ (m.round ∈ votes) ∧ 0 < clientValues.card then do
          let v ← choose clientValues
          votes[m.round] = v
          foreach (l in learners)
            invariant any_votes_cast :
              new_sends e => e is e2b ∧ e.target ∈ learners ∧
                (e2b_payload_of e).acceptor = this.ref ∧
                (e2b_payload_of e).round = m.round ∧ (e2b_payload_of e).value = v ;
          {
            send l, e2b, (acceptor = this.ref, round = m.round, value = v)
          }
      }
    }
  }

  machine Proposer {
    var round    : Int
    var value    : Int
    var joined   : set[PLean.MachineRef]
    var oneb     : set[t1b]
    var quorum   : Int
    var report   : t1b
    var proposed : Int

    start state Prepare {
      entry {
        foreach (a in acceptors)
          invariant prepares :
            new_sends e => e is e1a ∧ e.target ∈ acceptors ∧
              (e1a_payload_of e).proposer = this.ref ∧ (e1a_payload_of e).round = round ;
        {
          send a, e1a, (proposer = this.ref, round = round)
        }
      }

      on e1b (m : t1b) {
        if m.round = round then do
          joined += (m.acceptor)
          oneb += (m)
          -- `choose` fails on an empty set, and `card` is opaque to the solvers.
          if 0 < cquorums.card ∧ 0 < oneb.card then do
            let q ← choose cquorums
            let x ← choose oneb
            if ∀ n, n ∈ cmembers q → n ∈ joined then do
              if ∀ y, y ∈ oneb → y.acceptor ∈ cmembers q → y.has_vote = false then do
                quorum = q
                if isFast round = true then do
                  goto SendAny
                else do
                  proposed = value
                  goto SendProposal
              else do
                if x.acceptor ∈ cmembers q ∧ x.has_vote = true ∧
                    (∀ y, y ∈ oneb → y.acceptor ∈ cmembers q → y.has_vote = true →
                      y.maxr ≤ x.maxr) ∧
                    fastOK q oneb x.maxr x.maxv = true then do
                  quorum = q
                  report = x
                  proposed = x.maxv
                  goto SendProposal
      }
    }

    state SendProposal {
      entry {
        foreach (a in acceptors)
          invariant proposals :
            new_sends e => e is e2a ∧ e.target ∈ acceptors ∧ e.src = this.ref ∧
              (e2a_payload_of e).round = round ∧ (e2a_payload_of e).value = proposed ;
        {
          send a, e2a, (round = round, value = proposed)
        }
      }

      ignore e1b
    }

    state SendAny {
      entry {
        foreach (a in acceptors)
          invariant anys :
            new_sends e => e is eAny ∧ e.target ∈ acceptors ∧ e.src = this.ref ∧
              (eAny_payload_of e).round = round ;
        {
          send a, eAny, (round = round)
        }
      }

      ignore e1b
    }
  }

  machine Learner {
    var tally   : set[t2b]
    var decided : set[tDecision]

    start state Learn {
      on e2b (m : t2b) {
        tally += (m)
        if isFast m.round = true then do
          if fcovered tally m.round m.value = true then do
            decided += (({ round := m.round, value := m.value } : tDecision))
        else do
          if ccovered tally m.round m.value = true then do
            decided += (({ round := m.round, value := m.value } : tDecision))
      }
    }
  }

  init-holds ∀ r : PLean.MachineRef, r ∈ acceptors → is_Acceptor r s
  init-holds ∀ r : PLean.MachineRef, r ∈ learners → is_Learner r s
  init-holds ∀ (a : Acceptor) (r : Int), ¬ (r ∈ a.votes)
  init-holds ∀ (p : Proposer) (n : PLean.MachineRef), ¬ (n ∈ p.joined)
  init-holds ∀ (p : Proposer) (y : t1b), ¬ (y ∈ p.oneb)
  init-holds ∀ (l : Learner) (y : t2b), ¬ (y ∈ l.tally)
  init-holds ∀ (l : Learner) (d : tDecision), ¬ (d ∈ l.decided)
  init-holds ∀ p1 p2 : Proposer, p1.round = p2.round → p1 = p2
  init-holds ∀ q1 q2 : Int, q1 ∈ cquorums → q2 ∈ cquorums →
    ∃ n : PLean.MachineRef, n ∈ cmembers q1 ∧ n ∈ cmembers q2
  init-holds ∀ q f1 f2 : Int, q ∈ cquorums → f1 ∈ fquorums → f2 ∈ fquorums →
    ∃ n : PLean.MachineRef, n ∈ cmembers q ∧ n ∈ fmembers f1 ∧ n ∈ fmembers f2

  -- The acceptors and the learners, and where each kind of message goes.
  Lemma system_config {
    invariant acceptor_set : ∀ r : PLean.MachineRef, r ∈ acceptors → is_Acceptor r s
    invariant learner_set  : ∀ r : PLean.MachineRef, r ∈ learners → is_Learner r s
    invariant e1a_to_acceptor : ∀ e : e1a, sent e s → e.target ∈ acceptors
    invariant e1a_from_proposer : ∀ e : e1a, sent e s → is_Proposer e.proposer s
    invariant e1b_to_proposer : ∀ e : e1b, sent e s → is_Proposer e.target s
    invariant e2a_to_acceptor : ∀ e : e2a, sent e s → e.target ∈ acceptors
    invariant eAny_to_acceptor : ∀ e : eAny, sent e s → e.target ∈ acceptors
    invariant e2b_to_learner : ∀ e : e2b, sent e s → e.target ∈ learners
  }

  -- Who sends what: reports and votes come from Acceptors, proposals and "any" from
  -- Proposers.
  Lemma senders {
    invariant e1b_from_acceptor : ∀ e : e1b, sent e s → is_Acceptor e.acceptor s
    invariant e2b_from_acceptor : ∀ e : e2b, sent e s → is_Acceptor e.acceptor s
    invariant e2a_from_proposer : ∀ e : e2a, sent e s → is_Proposer e.src s
    invariant eAny_from_proposer : ∀ e : eAny, sent e s → is_Proposer e.src s
  }

  -- A report (`e1b`) is the Acceptor's highest vote below the round, and the Acceptor has
  -- joined the round, so its votes below it are frozen.
  Lemma reports {
    invariant report_joined :
      ∀ (e : e1b) (a : Acceptor), sent e s → e.acceptor = a.ref → e.round ≤ a.promised
    invariant report_vote :
      ∀ (e : e1b) (a : Acceptor), sent e s → e.acceptor = a.ref → e.has_vote = true →
        e.maxr < e.round ∧ a.votes[e.maxr]? = some e.maxv
    invariant report_max :
      ∀ (e : e1b) (a : Acceptor) (r : Int), sent e s → e.acceptor = a.ref → r < e.round →
        r ∈ a.votes → e.has_vote = true ∧ r ≤ e.maxr
  }

  -- A vote announced (`e2b`) or received (`tally`) is held by its Acceptor.
  Lemma votes_held {
    invariant e2b_held :
      ∀ (e : e2b) (a : Acceptor), sent e s → e.acceptor = a.ref →
        a.votes[e.round]? = some e.value
    invariant tally_from_acceptor :
      ∀ (l : Learner) (y : t2b), y ∈ l.tally → is_Acceptor y.acceptor s
    invariant tally_held :
      ∀ (l : Learner) (a : Acceptor) (y : t2b), y ∈ l.tally → y.acceptor = a.ref →
        a.votes[y.round]? = some y.value
  }

  -- A vote answers a proposal or an "any" of its round.
  Lemma vote_origin {
    invariant vote_proposed :
      ∀ (a : Acceptor) (r v : Int), a.votes[r]? = some v →
        (∃ e : e2a, sent e s ∧ e.round = r ∧ e.value = v) ∨ (∃ e : eAny, sent e s ∧ e.round = r)
  }

  -- A proposal or an "any" comes from the Proposer of its round, which has proposed. One
  -- Proposer per round.
  Lemma proposals {
    invariant e2a_proposed :
      ∀ (e : e2a) (p : Proposer), sent e s → e.src = p.ref →
        stateOf p.ref s = some Proposer.SendProposal_st ∧ e.round = p.round ∧
          e.value = p.proposed
    invariant eAny_proposed :
      ∀ (e : eAny) (p : Proposer), sent e s → e.src = p.ref →
        stateOf p.ref s = some Proposer.SendAny_st ∧ e.round = p.round
    invariant rounds_distinct : ∀ p1 p2 : Proposer, p1.round = p2.round → p1 = p2
  }

  -- The reports a Proposer has received, and what one that proposed chose from them.
  Lemma proposer_state {
    invariant oneb_round : ∀ (p : Proposer) (y : t1b), y ∈ p.oneb → y.round = p.round
    invariant joined_reported :
      ∀ (p : Proposer) (n : PLean.MachineRef), n ∈ p.joined →
        ∃ e : e1b, sent e s ∧ e.acceptor = n ∧ (e1b_payload_of e) ∈ p.oneb
    invariant proposed_quorum :
      ∀ p : Proposer,
        (stateOf p.ref s = some Proposer.SendProposal_st ∨
          stateOf p.ref s = some Proposer.SendAny_st) →
        p.quorum ∈ cquorums
    invariant quorum_joined :
      ∀ (p : Proposer) (n : PLean.MachineRef),
        (stateOf p.ref s = some Proposer.SendProposal_st ∨
          stateOf p.ref s = some Proposer.SendAny_st) →
        n ∈ cmembers p.quorum → n ∈ p.joined
    invariant any_fast :
      ∀ p : Proposer, stateOf p.ref s = some Proposer.SendAny_st → isFast p.round = true
    invariant any_no_vote :
      ∀ (p : Proposer) (y : t1b), stateOf p.ref s = some Proposer.SendAny_st →
        y ∈ p.oneb → y.acceptor ∈ cmembers p.quorum → y.has_vote = false
    invariant proposal_report :
      ∀ (p : Proposer) (y : t1b), stateOf p.ref s = some Proposer.SendProposal_st →
        y ∈ p.oneb → y.acceptor ∈ cmembers p.quorum → y.has_vote = true →
        p.report ∈ p.oneb ∧ (p.report).has_vote = true ∧ p.proposed = (p.report).maxv ∧
          fastOK p.quorum p.oneb (p.report).maxr (p.report).maxv = true
    invariant proposal_max :
      ∀ (p : Proposer) (y : t1b), stateOf p.ref s = some Proposer.SendProposal_st →
        y ∈ p.oneb → y.acceptor ∈ cmembers p.quorum → y.has_vote = true →
        y.maxr ≤ (p.report).maxr
  }

  -- What a report a Proposer holds says about its Acceptor, as `reports` says of the
  -- message.
  Lemma held_reports {
    invariant oneb_from_acceptor :
      ∀ (p : Proposer) (y : t1b), y ∈ p.oneb → is_Acceptor y.acceptor s
    invariant oneb_joined :
      ∀ (p : Proposer) (a : Acceptor) (y : t1b), y ∈ p.oneb → y.acceptor = a.ref →
        y.round ≤ a.promised
    invariant oneb_vote :
      ∀ (p : Proposer) (a : Acceptor) (y : t1b), y ∈ p.oneb → y.acceptor = a.ref →
        y.has_vote = true → y.maxr < y.round ∧ a.votes[y.maxr]? = some y.maxv
    invariant oneb_max :
      ∀ (p : Proposer) (a : Acceptor) (y : t1b) (r : Int), y ∈ p.oneb → y.acceptor = a.ref →
        r < y.round → r ∈ a.votes → y.has_vote = true ∧ r ≤ y.maxr
  }

  -- Receiving a vote keeps a quorum covered. This says nothing about the state; its base
  -- case is proved below.
  Lemma covered_mono {
    invariant fcovered_add :
      ∀ (t : set[t2b]) (y : t2b) (r v : Int), fcovered t r v = true →
        fcovered (t.add y) r v = true
    invariant ccovered_add :
      ∀ (t : set[t2b]) (y : t2b) (r v : Int), ccovered t r v = true →
        ccovered (t.add y) r v = true
  }

  -- The votes a Learner received for a decision cover a quorum of its round's kind.
  Lemma decisions {
    invariant decided_fast :
      ∀ (l : Learner) (d : tDecision), d ∈ l.decided → isFast d.round = true →
        fcovered l.tally d.round d.value = true
    invariant decided_classic :
      ∀ (l : Learner) (d : tDecision), d ∈ l.decided → isFast d.round = false →
        ccovered l.tally d.round d.value = true
  }

  -- The quorum axioms. This says nothing about the state: initial worlds assume it.
  Lemma quorum_axioms {
    invariant classic_intersect :
      ∀ q1 q2 : Int, q1 ∈ cquorums → q2 ∈ cquorums →
        ∃ n : PLean.MachineRef, n ∈ cmembers q1 ∧ n ∈ cmembers q2
    invariant fast_intersect :
      ∀ q f1 f2 : Int, q ∈ cquorums → f1 ∈ fquorums → f2 ∈ fquorums →
        ∃ n : PLean.MachineRef, n ∈ cmembers q ∧ n ∈ fmembers f1 ∧ n ∈ fmembers f2
  }

  Theorem safety {
    invariant decisions_agree :
      ∀ (l1 l2 : Learner) (d1 d2 : tDecision), d1 ∈ l1.decided → d2 ∈ l2.decided →
        d1.value = d2.value
  }

  Proof {
    prove system_config ;
    prove senders ;
    prove reports ;
    prove votes_held using senders ;
    prove vote_origin ;
    prove proposals ;
    prove proposer_state ;
    prove held_reports using senders, reports ;
    prove covered_mono ;
    prove decisions using covered_mono ;
    prove quorum_axioms ;
    prove safety using senders, votes_held, vote_origin, proposals, proposer_state,
                       held_reports, decisions, quorum_axioms ;
  }

end FastPaxos

#gen_module FastPaxos
#pwf        FastPaxos

namespace FastPaxos
open PLean Std.WP

/-! ## The argument

`proposal_blocks`, by induction on the distance between the rounds, through the proposal
whose value the Proposer adopted; `decide_agrees` applies it to the Learner's step. -/

/-- The votes of the Acceptor at `a`. -/
abbrev votesOf (g : GW) (a : MachineRef) : PMap Int Int := (g.machines a).fields.Acceptor_votes

/-- `Q` is a quorum of `r`'s kind, and each of its members voted `v` at `r`. -/
def Full (g : GW) (r v Q : Int) : Prop :=
  (isFast r = true → Q ∈ fquorums ∧ ∀ a, a ∈ fmembers Q → (votesOf g a).get r = some v) ∧
  (isFast r = false → Q ∈ cquorums ∧ ∀ a, a ∈ cmembers Q → (votesOf g a).get r = some v)

theorem mem_add_cases {T : Type} [DecidableEq T] {q : PSet T} {x a : T} (h : a ∈ q.add x) :
    a = x ∨ a ∈ q := by
  change (q.add x).mem a = true at h
  rw [PSet.mem_add, Bool.or_eq_true, decide_eq_true_eq] at h
  exact h

theorem mem_add_of_mem {T : Type} [DecidableEq T] {q : PSet T} {a : T} (x : T) (h : a ∈ q) :
    a ∈ q.add x := by
  change (q.add x).mem a = true
  rw [PSet.mem_add, Bool.or_eq_true]
  exact Or.inr h

/-- Receiving a vote keeps a quorum covered. -/
@[pverifyProof] theorem base_covered_mono :
    ∀ (g : GW), InitConditions g →
      PostInv "fcovered_add" (fcovered_add g) ∧ PostInv "ccovered_add" (ccovered_add g) := by
  intro g _
  refine ⟨fun t y r v h => ?_, fun t y r v h => ?_⟩
  · unfold fcovered at h ⊢
    rw [decide_eq_true_eq] at h ⊢
    obtain ⟨f, hf, hall⟩ := h
    exact ⟨f, hf, fun n hn => mem_add_of_mem y (hall n hn)⟩
  · unfold ccovered at h ⊢
    rw [decide_eq_true_eq] at h ⊢
    obtain ⟨q, hq, hall⟩ := h
    exact ⟨q, hq, fun n hn => mem_add_of_mem y (hall n hn)⟩

/-- Covered votes, each held by its Acceptor, make a quorum of voters. -/
theorem fcovered_full (g : GW) (t : PSet t2b) (r v : Int)
    (ht : ∀ y, y ∈ t → is_Acceptor y.acceptor g ∧
      (votesOf g y.acceptor).get y.round = some y.value)
    (h : fcovered t r v = true) :
    ∃ f, f ∈ fquorums ∧ ∀ a, a ∈ fmembers f →
      is_Acceptor a g ∧ (votesOf g a).get r = some v := by
  unfold fcovered at h
  rw [decide_eq_true_eq] at h
  obtain ⟨f, hf, hall⟩ := h
  exact ⟨f, hf, fun a ha => ht _ (hall a ha)⟩

theorem ccovered_full (g : GW) (t : PSet t2b) (r v : Int)
    (ht : ∀ y, y ∈ t → is_Acceptor y.acceptor g ∧
      (votesOf g y.acceptor).get y.round = some y.value)
    (h : ccovered t r v = true) :
    ∃ q, q ∈ cquorums ∧ ∀ a, a ∈ cmembers q →
      is_Acceptor a g ∧ (votesOf g a).get r = some v := by
  unfold ccovered at h
  rw [decide_eq_true_eq] at h
  obtain ⟨q, hq, hall⟩ := h
  exact ⟨q, hq, fun a ha => ht _ (hall a ha)⟩

/-- A vote answers the proposal or the "any" of the Proposer of its round. -/
theorem vote_source (g : GW) (hvp : vote_proposed g) (h2aP : e2a_from_proposer g)
    (hAnyP : eAny_from_proposer g) (h2a : e2a_proposed g) (hAny : eAny_proposed g)
    (a : MachineRef) (ha : is_Acceptor a g) (r v : Int) (hv : (votesOf g a).get r = some v) :
    ∃ p : MachineRef, is_Proposer p g ∧ (g.machines p).fields.Proposer_round = r ∧
      (((g.machines p).state = some S.Proposer_SendProposal ∧
          (g.machines p).fields.Proposer_proposed = v) ∨
        (g.machines p).state = some S.Proposer_SendAny) := by
  rcases hvp ⟨a⟩ ha r v hv with ⟨e, he, hs, hr, hval⟩ | ⟨e, he, hs, hr⟩
  · have hp : is_Proposer e.src g := h2aP e he hs
    obtain ⟨hst, hround, hvalue⟩ := h2a e he ⟨e.src⟩ hp hs rfl
    have hst' : (g.machines e.src).state = some S.Proposer_SendProposal := hst
    have hround' : (e2a_payload_of e).round = (g.machines e.src).fields.Proposer_round := hround
    have hvalue' : (e2a_payload_of e).value = (g.machines e.src).fields.Proposer_proposed :=
      hvalue
    exact ⟨e.src, hp, by omega, Or.inl ⟨hst', by omega⟩⟩
  · have hp : is_Proposer e.src g := hAnyP e he hs
    obtain ⟨hst, hround⟩ := hAny e he ⟨e.src⟩ hp hs rfl
    have hst' : (g.machines e.src).state = some S.Proposer_SendAny := hst
    have hround' : (eAny_payload_of e).round = (g.machines e.src).fields.Proposer_round := hround
    exact ⟨e.src, hp, by omega, Or.inr hst'⟩

/-- A report a Proposer holds is for its round, from an Acceptor, and is that Acceptor's
highest vote below the round; the Acceptor has joined the round, so its votes below it are
the ones it had. -/
theorem report_facts (g : GW) (hor : oneb_round g) (hoA : oneb_from_acceptor g)
    (hov : oneb_vote g) (hom : oneb_max g)
    (p : MachineRef) (hp : is_Proposer p g) (y : t1b)
    (hy : y ∈ (g.machines p).fields.Proposer_oneb) :
    y.round = (g.machines p).fields.Proposer_round ∧ is_Acceptor y.acceptor g ∧
      (y.has_vote = true →
        y.maxr < y.round ∧ (votesOf g y.acceptor).get y.maxr = some y.maxv) ∧
      (∀ r v, r < y.round → (votesOf g y.acceptor).get r = some v →
        y.has_vote = true ∧ r ≤ y.maxr) := by
  have hA : is_Acceptor y.acceptor g := hoA ⟨p⟩ hp y hy
  refine ⟨hor ⟨p⟩ hp y hy, hA, fun hv => hov ⟨p⟩ hp ⟨y.acceptor⟩ hA y hy rfl hv,
    fun r v hr hv => ?_⟩
  exact hom ⟨p⟩ hp ⟨y.acceptor⟩ hA y r hy rfl hr
    (by show ((votesOf g _).get r).isSome = true; rw [hv]; rfl)

/-- The report of a member `n` of a Proposer's classic quorum, once it has proposed. -/
theorem member_report (g : GW) (hjr : joined_reported g) (hqj : quorum_joined g)
    (p : MachineRef) (hp : is_Proposer p g)
    (hprop : (g.machines p).state = some S.Proposer_SendProposal ∨
      (g.machines p).state = some S.Proposer_SendAny)
    (n : MachineRef) (hn : n ∈ cmembers (g.machines p).fields.Proposer_quorum) :
    ∃ y : t1b, y ∈ (g.machines p).fields.Proposer_oneb ∧ y.acceptor = n := by
  have hj : n ∈ (g.machines p).fields.Proposer_joined := hqj ⟨p⟩ hp n hprop hn
  obtain ⟨e, -, -, hacc, hin⟩ := hjr ⟨p⟩ hp n hj
  exact ⟨_, hin, hacc⟩

/-- A Proposer that proposed above a round `r` at which a quorum `Q` of `r`'s kind voted
for `v` proposed `v` (and not "any"). -/
theorem proposal_blocks (g : GW)
    (hor : oneb_round g) (hjr : joined_reported g)
    (hpq : proposed_quorum g) (hqj : quorum_joined g) (hanyf : any_fast g)
    (hanv : any_no_vote g) (hrep : proposal_report g) (hmax : proposal_max g)
    (hoA : oneb_from_acceptor g) (hov : oneb_vote g) (hom : oneb_max g)
    (hvp : vote_proposed g) (h2aP : e2a_from_proposer g) (hAnyP : eAny_from_proposer g)
    (h2a : e2a_proposed g) (hAny : eAny_proposed g) (hrd : rounds_distinct g)
    (hci : classic_intersect g) (hfi : fast_intersect g) :
    ∀ (k : Nat) (p : MachineRef) (r v Q : Int), is_Proposer p g →
      ((g.machines p).state = some S.Proposer_SendProposal ∨
        (g.machines p).state = some S.Proposer_SendAny) →
      r < (g.machines p).fields.Proposer_round →
      ((g.machines p).fields.Proposer_round - r).toNat ≤ k →
      Full g r v Q →
      (g.machines p).state = some S.Proposer_SendProposal ∧
        (g.machines p).fields.Proposer_proposed = v := by
  intro k
  induction k with
  | zero => intro p r v Q _ _ hlt hk _; omega
  | succ k ih =>
    intro p r v Q hp hprop hlt hk hfull
    have hq : (g.machines p).fields.Proposer_quorum ∈ cquorums := hpq ⟨p⟩ hp hprop
    -- A member `N` of the Proposer's classic quorum is in `Q`: it voted `v` at `r`.
    obtain ⟨N, hNq, hNv, hNQ⟩ : ∃ N, N ∈ cmembers (g.machines p).fields.Proposer_quorum ∧
        (votesOf g N).get r = some v ∧ (isFast r = true → N ∈ fmembers Q) := by
      cases hfr : isFast r
      · obtain ⟨hQ, hall⟩ := hfull.2 hfr
        obtain ⟨N, h1, h2⟩ := hci Q _ hQ hq
        exact ⟨N, h2, hall N h1, fun h => by cases h⟩
      · obtain ⟨hQ, hall⟩ := hfull.1 hfr
        obtain ⟨N, h1, h2, -⟩ := hfi _ Q Q hq hQ hQ
        exact ⟨N, h1, hall N h2, fun _ => h2⟩
    -- `N` joined the Proposer's round, and reported a vote at `r` or above.
    obtain ⟨y, hyin, hyN⟩ := member_report g hjr hqj p hp hprop N hNq
    obtain ⟨hyr, -, hyv, hym⟩ := report_facts g hor hoA hov hom p hp y hyin
    rw [hyN] at hyv hym
    obtain ⟨hyhas, hyle⟩ := hym r v (by omega) hNv
    have hyq : y.acceptor ∈ cmembers (g.machines p).fields.Proposer_quorum := by
      rw [hyN]; exact hNq
    rcases hprop with hsp | hsa
    · -- It proposed the value `x.maxv` of the report `x` it chose.
      have hrep' := hrep ⟨p⟩ hp y hsp hyin hyq hyhas
      have hxin : (g.machines p).fields.Proposer_report ∈ (g.machines p).fields.Proposer_oneb :=
        hrep'.1
      have hxhas : (g.machines p).fields.Proposer_report.has_vote = true := hrep'.2.1
      have hpropx : (g.machines p).fields.Proposer_proposed =
          (g.machines p).fields.Proposer_report.maxv := hrep'.2.2.1
      have hfast : fastOK (g.machines p).fields.Proposer_quorum
          (g.machines p).fields.Proposer_oneb (g.machines p).fields.Proposer_report.maxr
          (g.machines p).fields.Proposer_report.maxv = true := hrep'.2.2.2
      have hymax : y.maxr ≤ (g.machines p).fields.Proposer_report.maxr :=
        hmax ⟨p⟩ hp y hsp hyin hyq hyhas
      clear hrep'
      refine ⟨hsp, ?_⟩
      rw [hpropx]
      generalize hx : (g.machines p).fields.Proposer_report = x at hxin hxhas hfast hymax ⊢
      obtain ⟨hxr, hxA, hxv, -⟩ := report_facts g hor hoA hov hom p hp x hxin
      obtain ⟨hxlt, hxvote⟩ := hxv hxhas
      rcases Int.lt_or_eq_of_le (show r ≤ x.maxr by omega) with hrM | hrM
      · -- `r` is below the round of `x`'s vote: the Proposer of that round proposed
        -- `x.maxv`, and `v` by induction.
        obtain ⟨p', hp', hround', hcase⟩ :=
          vote_source g hvp h2aP hAnyP h2a hAny _ hxA _ _ hxvote
        have hind := ih p' r v Q hp' (hcase.imp (·.1) id) (by omega) (by omega) hfull
        rcases hcase with ⟨-, hval⟩ | hsa'
        · rw [← hval, hind.2]
        · rw [hind.1] at hsa'; cases hsa'
      · -- `r` is the round of `x`'s vote.
        subst hrM
        cases hfr : isFast x.maxr
        · -- A classic round has one proposal, which both votes answer.
          have hNA : is_Acceptor N g := by
            rw [← hyN]; exact (report_facts g hor hoA hov hom p hp y hyin).2.1
          obtain ⟨p1, hp1, hr1, hc1⟩ := vote_source g hvp h2aP hAnyP h2a hAny N hNA _ v hNv
          obtain ⟨p2, hp2, hr2, hc2⟩ := vote_source g hvp h2aP hAnyP h2a hAny _ hxA _ _ hxvote
          have h12 : (⟨p1⟩ : Proposer) = ⟨p2⟩ :=
            hrd ⟨p1⟩ hp1 ⟨p2⟩ hp2 (show (g.machines p1).fields.Proposer_round =
              (g.machines p2).fields.Proposer_round by omega)
          have h12' : p1 = p2 := congrArg Proposer.ref h12
          subst h12'
          rcases hc1 with ⟨hs1, hv1⟩ | hs1
          · rcases hc2 with ⟨-, hv2⟩ | hs2
            · rw [← hv2, hv1]
            · rw [hs1] at hs2; cases hs2
          · have hf : isFast (g.machines p1).fields.Proposer_round = true := hanyf ⟨p1⟩ hp1 hs1
            rw [hr1, hfr] at hf; cases hf
        · -- A fast round: `fastOK`.
          obtain ⟨hQ, hQall⟩ := hfull.1 hfr
          unfold fastOK at hfast
          rw [decide_eq_true_eq] at hfast
          rcases hfast with ⟨f, hf, hfall⟩ | hb
          · -- The members of a fast quorum `f` in `q` report `x.maxv`, and one of them is
            -- in `Q`.
            obtain ⟨n0, hn0q, hn0f, hn0Q⟩ := hfi _ f Q hq hf hQ
            obtain ⟨y', hy'in, hy'acc, hy'has, hy'maxr, hy'maxv⟩ := hfall n0 hn0f hn0q
            obtain ⟨-, -, hy'v, -⟩ := report_facts g hor hoA hov hom p hp y' hy'in
            have h1 : (votesOf g y'.acceptor).get y'.maxr = some y'.maxv := (hy'v hy'has).2
            rw [hy'acc, hy'maxr, hy'maxv] at h1
            have h2 : (votesOf g n0).get x.maxr = some v := hQall n0 hn0Q
            rw [h1] at h2
            exact Option.some.inj h2
          · -- Otherwise the members of `Q` in `q` do not all report `y`'s value, `v`; but
            -- they all voted `v` at `x.maxr`, the highest round reported.
            exfalso
            obtain ⟨-, hz⟩ := hb Q hQ
            obtain ⟨n1, hn1Q, hn1q, hn1no⟩ := hz y hyin
            apply hn1no
            obtain ⟨y1, hin1, hacc1⟩ := member_report g hjr hqj p hp (Or.inl hsp) n1 hn1q
            obtain ⟨hr1, -, hv1, hm1⟩ := report_facts g hor hoA hov hom p hp y1 hin1
            rw [hacc1] at hv1 hm1
            have hn1v : (votesOf g n1).get x.maxr = some v := hQall n1 hn1Q
            obtain ⟨hhas1, hle1⟩ := hm1 x.maxr v (by omega) hn1v
            have hq1 : y1.acceptor ∈ cmembers (g.machines p).fields.Proposer_quorum := by
              rw [hacc1]; exact hn1q
            have hmax1 : y1.maxr ≤ (g.machines p).fields.Proposer_report.maxr :=
              hmax ⟨p⟩ hp y1 hsp hin1 hq1 hhas1
            rw [hx] at hmax1
            have heq1 : y1.maxr = x.maxr := by omega
            have heqy : y.maxr = x.maxr := by omega
            have hvote1 : (votesOf g n1).get y1.maxr = some y1.maxv := (hv1 hhas1).2
            rw [heq1, hn1v] at hvote1
            have hvotey : (votesOf g N).get y.maxr = some y.maxv := (hyv hyhas).2
            rw [heqy, hNv] at hvotey
            refine ⟨y1, hin1, hacc1, hhas1, heq1, ?_⟩
            rw [← Option.some.inj hvote1, ← Option.some.inj hvotey]
    · -- "any" needs no report of a vote in `q`, and `N` reported one.
      have := hanv ⟨p⟩ hp y hsa hyin hyq
      rw [hyhas] at this; cases this

/-- When a Learner receives the vote `pl` and decides `(pl.round, pl.value)`, every decision
already made is for `pl.value`. -/
theorem decide_agrees (g : GW) (i : Nat) (src dst : MachineRef) (pl : e2b_payload)
    (hor : oneb_round g) (hjr : joined_reported g)
    (hpq : proposed_quorum g) (hqj : quorum_joined g) (hanyf : any_fast g)
    (hanv : any_no_vote g) (hrep : proposal_report g) (hmax : proposal_max g)
    (hoA : oneb_from_acceptor g) (hov : oneb_vote g) (hom : oneb_max g)
    (h2bA : e2b_from_acceptor g) (h2bh : e2b_held g) (htA : tally_from_acceptor g)
    (hth : tally_held g)
    (hvp : vote_proposed g) (h2aP : e2a_from_proposer g) (hAnyP : eAny_from_proposer g)
    (h2a : e2a_proposed g) (hAny : eAny_proposed g) (hrd : rounds_distinct g)
    (hci : classic_intersect g) (hfi : fast_intersect g)
    (hdf : decided_fast g) (hdc : decided_classic g)
    (hlog : g.log i = some { src := src, dst := dst, ev := E.e2b pl })
    (hst : (g.machines dst).state = some S.Learner_Learn)
    (l : MachineRef) (d : tDecision) (hl : (g.machines l).state = some S.Learner_Learn)
    (hd : (g.machines l).fields.Learner_decided.mem d = true) :
    (isFast pl.round = true →
      fcovered ((g.machines dst).fields.Learner_tally.add pl) pl.round pl.value = true →
      d.value = pl.value) ∧
    (¬ isFast pl.round = true →
      ccovered ((g.machines dst).fields.Learner_tally.add pl) pl.round pl.value = true →
      d.value = pl.value) := by
  have blocks := proposal_blocks g hor hjr hpq hqj hanyf hanv hrep hmax hoA hov hom hvp
    h2aP hAnyP h2a hAny hrd hci hfi
  have hsrc := vote_source g hvp h2aP hAnyP h2a hAny
  -- The vote `pl`, and the Proposer `P` of its round.
  have he : is_e2b (⟨i, src, dst, E.e2b pl⟩ : Msg E) := ⟨pl, rfl⟩
  have hs : sent (⟨i, src, dst, E.e2b pl⟩ : Msg E) g := hlog
  have hA : is_Acceptor pl.acceptor g := h2bA _ he hs
  have hv : (votesOf g pl.acceptor).get pl.round = some pl.value :=
    h2bh _ he ⟨pl.acceptor⟩ hA hs rfl
  obtain ⟨P, hP, hPr, hPc⟩ := hsrc pl.acceptor hA pl.round pl.value hv
  have hq : (g.machines P).fields.Proposer_quorum ∈ cquorums := hpq ⟨P⟩ hP (hPc.imp (·.1) id)
  -- The votes the deciding Learner holds, with `pl`.
  have hdst : is_Learner dst g := hst
  have ht : ∀ y, y ∈ (g.machines dst).fields.Learner_tally.add pl →
      is_Acceptor y.acceptor g ∧ (votesOf g y.acceptor).get y.round = some y.value := by
    intro y hy
    rcases mem_add_cases hy with rfl | hy
    · exact ⟨hA, hv⟩
    · have hyA := htA ⟨dst⟩ hdst y hy
      exact ⟨hyA, hth ⟨dst⟩ hdst ⟨y.acceptor⟩ hyA y hy rfl⟩
  -- The decision `d`: its quorum `QD`, a member of it, which voted for `d`, and the
  -- Proposer `P'` of its round. (A fast quorum is not empty: it shares a member with
  -- `P`'s classic quorum.)
  have hl' : is_Learner l g := hl
  have hd' : d ∈ (g.machines l).fields.Learner_decided := hd
  have htl : ∀ y, y ∈ (g.machines l).fields.Learner_tally →
      is_Acceptor y.acceptor g ∧ (votesOf g y.acceptor).get y.round = some y.value := by
    intro y hy
    have hyA := htA ⟨l⟩ hl' y hy
    exact ⟨hyA, hth ⟨l⟩ hl' ⟨y.acceptor⟩ hyA y hy rfl⟩
  obtain ⟨QD, hQD, nD, hnDA, hnDv⟩ : ∃ QD, Full g d.round d.value QD ∧
      ∃ n, is_Acceptor n g ∧ (votesOf g n).get d.round = some d.value := by
    cases hfd : isFast d.round
    · obtain ⟨Q, hQ, hall⟩ := ccovered_full g _ _ _ htl (hdc ⟨l⟩ hl' d hd' hfd)
      obtain ⟨n, hn, -⟩ := hci Q Q hQ hQ
      exact ⟨Q, ⟨fun h => absurd (hfd.symm.trans h) (by decide),
        fun _ => ⟨hQ, fun a ha => (hall a ha).2⟩⟩, n, hall n hn⟩
    · obtain ⟨Q, hQ, hall⟩ := fcovered_full g _ _ _ htl (hdf ⟨l⟩ hl' d hd' hfd)
      obtain ⟨n, -, hn, -⟩ := hfi _ Q Q hq hQ hQ
      exact ⟨Q, ⟨fun _ => ⟨hQ, fun a ha => (hall a ha).2⟩,
        fun h => absurd (hfd.symm.trans h) (by decide)⟩, n, hall n hn⟩
  obtain ⟨P', hP', hP'r, hP'c⟩ := hsrc nD hnDA d.round d.value hnDv
  -- The new decision, with a quorum `QN` of its round's kind.
  have main : ∀ QN, Full g pl.round pl.value QN → d.value = pl.value := by
    intro QN hQN
    rcases Int.lt_trichotomy d.round pl.round with hlt | heq | hgt
    · -- `P` proposed above `d`'s round: it proposed `d.value`, so `pl` voted for it.
      have hb := blocks _ P d.round d.value QD hP (hPc.imp (·.1) id) (by omega) (Nat.le_refl _)
        hQD
      rcases hPc with ⟨-, hPv⟩ | hPa
      · rw [← hPv, hb.2]
      · rw [hb.1] at hPa; cases hPa
    · -- The same round, and so the same Proposer.
      have h12 : (⟨P⟩ : Proposer) = ⟨P'⟩ :=
        hrd ⟨P⟩ hP ⟨P'⟩ hP' (show (g.machines P).fields.Proposer_round =
          (g.machines P').fields.Proposer_round by omega)
      have h12' : P = P' := congrArg Proposer.ref h12
      subst h12'
      rcases hPc with ⟨hPs, hPv⟩ | hPa
      · rcases hP'c with ⟨-, hP'v⟩ | hP'a
        · rw [← hP'v, hPv]
        · rw [hPs] at hP'a; cases hP'a
      · -- "any": a fast round, and the two fast quorums share a member of `P`'s classic
        -- quorum.
        have hf : isFast (g.machines P).fields.Proposer_round = true := hanyf ⟨P⟩ hP hPa
        rw [hPr] at hf
        have hfd : isFast d.round = true := by rw [heq]; exact hf
        obtain ⟨hQN1, hQN2⟩ := hQN.1 hf
        obtain ⟨hQD1, hQD2⟩ := hQD.1 hfd
        obtain ⟨n, -, hn1, hn2⟩ := hfi _ QN QD hq hQN1 hQD1
        have h1 := hQN2 n hn1
        have h2 := hQD2 n hn2
        rw [heq, h1] at h2
        exact (Option.some.inj h2).symm
    · -- `P'` proposed above `pl`'s round: it proposed `pl.value`, so `d` was voted for it.
      have hb := blocks _ P' pl.round pl.value QN hP' (hP'c.imp (·.1) id) (by omega)
        (Nat.le_refl _) hQN
      rcases hP'c with ⟨-, hP'v⟩ | hP'a
      · rw [← hP'v, hb.2]
      · rw [hb.1] at hP'a; cases hP'a
  refine ⟨fun hf hcov => ?_, fun hf hcov => ?_⟩
  · obtain ⟨QN, hQN, hall⟩ := fcovered_full g _ _ _ ht hcov
    exact main QN ⟨fun _ => ⟨hQN, fun a ha => (hall a ha).2⟩,
      fun h => absurd (hf.symm.trans h) (by decide)⟩
  · obtain ⟨QN, hQN, hall⟩ := ccovered_full g _ _ _ ht hcov
    exact main QN ⟨fun h => absurd h hf, fun _ => ⟨hQN, fun a ha => (hall a ha).2⟩⟩

/-- The decisions after the Learner `dst` adds `x` agree, when they did before and every
decision already made is for `x.value`: the post-state of the Learner's step. -/
theorem agree_after_add (g : GW) (dst : MachineRef) (x : tDecision)
    (hagr : ∀ r, (g.machines r).state = some S.Learner_Learn →
      ∀ r', (g.machines r').state = some S.Learner_Learn → ∀ d1 d2 : tDecision,
        (g.machines r).fields.Learner_decided.mem d1 = true →
        (g.machines r').fields.Learner_decided.mem d2 = true → d1.value = d2.value)
    (hkey : ∀ (l : MachineRef) (d : tDecision), (g.machines l).state = some S.Learner_Learn →
      (g.machines l).fields.Learner_decided.mem d = true → d.value = x.value)
    {r1 r2 : MachineRef} {d1 d2 : tDecision}
    (m1 : (if r1 = dst then decide (d1 = x) || (g.machines dst).fields.Learner_decided.mem d1
      else (g.machines r1).fields.Learner_decided.mem d1) = true)
    (h1 : (if r1 = dst then (g.machines dst).state else (g.machines r1).state) =
      some S.Learner_Learn)
    (m2 : (if r2 = dst then decide (d2 = x) || (g.machines dst).fields.Learner_decided.mem d2
      else (g.machines r2).fields.Learner_decided.mem d2) = true)
    (h2 : (if r2 = dst then (g.machines dst).state else (g.machines r2).state) =
      some S.Learner_Learn) :
    d1.value = d2.value := by
  have side : ∀ (r : MachineRef) (d : tDecision),
      (if r = dst then (g.machines dst).state else (g.machines r).state) =
        some S.Learner_Learn →
      (if r = dst then decide (d = x) || (g.machines dst).fields.Learner_decided.mem d
        else (g.machines r).fields.Learner_decided.mem d) = true →
      d = x ∨ ((g.machines r).state = some S.Learner_Learn ∧
        (g.machines r).fields.Learner_decided.mem d = true) := by
    intro r d hs hm
    by_cases hr : r = dst
    · subst hr
      simp only [↓reduceIte] at hs hm
      rw [Bool.or_eq_true, decide_eq_true_eq] at hm
      exact hm.imp id (fun h => ⟨hs, h⟩)
    · simp only [hr, ↓reduceIte] at hs hm
      exact Or.inr ⟨hs, hm⟩
  rcases side r1 d1 h1 m1 with rfl | ⟨hs1, hm1⟩ <;>
    rcases side r2 d2 h2 m2 with rfl | ⟨hs2, hm2⟩
  · rfl
  · exact (hkey r2 d2 hs2 hm2).symm
  · exact hkey r1 d1 hs1 hm1
  · exact hagr r1 hs1 r2 hs2 d1 d2 hm1 hm2

set_option maxHeartbeats 1000000 in
@[pverifyProof] theorem Learner.Learn.e2b_correct_safety :
    ∀ (g : GW) (i : Nat) (src dst : MachineRef) (pl : e2b_payload),
      Framework g →
        g.error = none →
          safety g ∧
              senders g ∧
                votes_held g ∧ vote_origin g ∧ proposals g ∧ proposer_state g ∧ held_reports g ∧
                  decisions g ∧ quorum_axioms g →
            g.net i = some { src := src, dst := dst, ev := E.e2b pl } →
              g.log i = some { src := src, dst := dst, ev := E.e2b pl } →
                i < g.clock →
                  (g.machines dst).state = some S.Learner_Learn →
                    (g.machines dst).pending = none →
                      (g.machines dst).waiting = none →
                        (g.machines dst).halted = false →
                          RunPost (fun g' ↦ PostInv "decisions_agree" (decisions_agree g'))
                            (GWorld.consume g i) dst (g.machines dst)
                            (Learner.Learn.e2b_handler pl) := by
  intro g i src dst pl hF hE hInv hnet hlog hi hst
  obtain ⟨hagr, ⟨-, h2bA, h2aP, hAnyP⟩, ⟨h2bh, htA, hth⟩, hvp, ⟨h2a, hAny, hrd⟩,
    ⟨hor, hjr, hpq, hqj, hanyf, hanv, hrep, hmax⟩, ⟨hoA, -, hov, hom⟩, ⟨hdf, hdc⟩,
    ⟨hci, hfi⟩⟩ := hInv
  have key := decide_agrees g i src dst pl hor hjr hpq hqj hanyf hanv hrep hmax hoA hov hom
    h2bA h2bh htA hth hvp h2aP hAnyP h2a hAny hrd hci hfi hdf hdc hlog hst
  clear h2bA h2aP hAnyP h2bh htA hth hvp h2a hAny hrd hor hjr hpq hqj hanyf hanv hrep hmax hoA
    hov hom hdf hdc hci hfi
  -- What is left is the post-state: the Learner adds the decision to its own.
  set_option pverify.trust "reconstruct" in pverify_auto
  · apply agree_after_add g dst { round := pl.round, value := pl.value } hagr
      (fun l d hl hd => (key l d hl hd).1 ‹_› ‹_›) <;> assumption
  · apply agree_after_add g dst { round := pl.round, value := pl.value } hagr
      (fun l d hl hd => (key l d hl hd).2 ‹_› ‹_›) <;> assumption

/-! ## The Proposer's step, for `held_reports`

When the Proposer goes to `SendProposal` or `SendAny`, the Acceptor of each report it holds
is still an Acceptor; the solvers prove it, but their proof does not replay. -/

theorem acceptor_after_step (g : GW) (dst : MachineRef) (pl : t1b) (s' : S)
    (hdst : (g.machines dst).state = some S.Proposer_Prepare)
    (hob : ∀ r : MachineRef, is_Proposer r g → ∀ y : t1b,
      (g.machines r).fields.Proposer_oneb.mem y = true →
      (g.machines y.acceptor).state = some S.Acceptor_Serve)
    (hpl : (g.machines pl.acceptor).state = some S.Acceptor_Serve)
    {ref : MachineRef} {y : t1b}
    (hm : (if ref = dst then decide (y = pl) || (g.machines dst).fields.Proposer_oneb.mem y
      else (g.machines ref).fields.Proposer_oneb.mem y) = true)
    (hs : ((if ref = dst then some s' else (g.machines ref).state) = some S.Proposer_Prepare ∨
        (if ref = dst then some s' else (g.machines ref).state) = some S.Proposer_SendProposal) ∨
      (if ref = dst then some s' else (g.machines ref).state) = some S.Proposer_SendAny) :
    (if y.acceptor = dst then some s' else (g.machines y.acceptor).state) =
      some S.Acceptor_Serve := by
  have hy : (g.machines y.acceptor).state = some S.Acceptor_Serve := by
    by_cases hr : ref = dst
    · subst hr
      simp only [↓reduceIte, Bool.or_eq_true, decide_eq_true_eq] at hm
      rcases hm with rfl | hm
      · exact hpl
      · exact hob ref (Or.inl (Or.inl hdst)) y hm
    · simp only [hr, ↓reduceIte] at hm hs
      exact hob ref hs y hm
  have hne : y.acceptor ≠ dst := fun h => by rw [h, hdst] at hy; cases hy
  simp only [hne, ↓reduceIte]
  exact hy

set_option maxHeartbeats 1000000 in
@[pverifyProof] theorem Proposer.Prepare.e1b_correct_held_reports :
    ∀ (g : GW) (i : Nat) (src dst : MachineRef) (pl : e1b_payload),
      Framework g →
        g.error = none →
          held_reports g ∧ senders g ∧ reports g →
            g.net i = some { src := src, dst := dst, ev := E.e1b pl } →
              g.log i = some { src := src, dst := dst, ev := E.e1b pl } →
                i < g.clock →
                  (g.machines dst).state = some S.Proposer_Prepare →
                    (g.machines dst).pending = none →
                      (g.machines dst).waiting = none →
                        (g.machines dst).halted = false →
                          RunPost
                            (fun g' ↦
                              PostInv "oneb_from_acceptor" (oneb_from_acceptor g') ∧
                                PostInv "oneb_joined" (oneb_joined g') ∧
                                  PostInv "oneb_vote" (oneb_vote g') ∧
                                    PostInv "oneb_max" (oneb_max g'))
                            (GWorld.consume g i) dst (g.machines dst)
                            (Proposer.Prepare.e1b_handler pl) := by
  intro g i src dst pl hF hE hInv hnet hlog hi hst
  have hob : ∀ r : MachineRef, is_Proposer r g → ∀ y : t1b,
      (g.machines r).fields.Proposer_oneb.mem y = true →
      (g.machines y.acceptor).state = some S.Acceptor_Serve :=
    fun r hr y hy => hInv.1.1 ⟨r⟩ hr y hy
  have hpl : (g.machines pl.acceptor).state = some S.Acceptor_Serve :=
    hInv.2.1.1 ⟨i, src, dst, E.e1b pl⟩ ⟨pl, rfl⟩ hlog
  set_option pverify.trust "reconstruct" in pverify_auto
  all_goals (apply acceptor_after_step g dst pl _ hst hob hpl <;> assumption)

end FastPaxos

#pverify FastPaxos
