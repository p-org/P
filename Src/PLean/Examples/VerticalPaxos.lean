/-
Vertical Paxos: DuoAI's abstract Ivy model `vertical_paxos.ivy` (from "Paxos Made EPR"),
single-decree Paxos in which an external master assigns a configuration to each round, and
each configuration has its own quorums.

The Master configures a round on request (`eConfigReq`), once, and only a round no lower
than the highest round it knows to be complete (`complete`), with a configuration whose
complete round is exactly that one (`complete_of`); it tells every Proposer (`eConfigure`).
A Proposer leads one round `r`. Once `r` is configured with complete round `cr`, it asks the
nodes to join `r` and report their vote in each round from `cr` to `r - 1` (`eOneA`). A Node
that has not joined a round above `r` joins it, leaving every round below, and reports
(`eJoinAck`). Round by round, once the nodes the Proposer counted for the lowest open round
form a quorum of that round's configuration, it fixes them as the round's quorum (or waits
for more). With every round fixed, it proposes the value reported in the highest round with
a vote; if there is none, it proposes its own value and tells the Master that `r` is
complete (`eComplete`). A Node that has not left `r` votes (`eVote`). Once the voters form a quorum,
the Proposer teaches the decision (`eDecide`) and tells the Master that `r` is complete.

Rounds, values and configurations are `Int`, Ivy's `none` the constant `noneV`; quorums are
sets, with `quorumin`. `left_rnd(N, R)` is `R < N.joined`; `proposal(R, V)` is the Proposer
of `R` in `Vote` or `Done` with `proposed = V`; `vote(N, R, V)` an `eVote`, also recorded in
`N.votes`; `decision(N, R, V)` is `N.decisions[R]? = some V`; `quorum_of_round(R)` is the
proposing Proposer's `acks[R]` when it fixes `R`. Each state and handler names its Ivy action.

Ivy's choices:
- `configure_round`'s arguments are an Admin's `round` and `config`, fixed initially; each
  Admin asks once, and its request takes effect if the Master's guards hold on arrival. An
  Ivy run is matched by initial worlds with one Admin per `configure_round` step.
- A Proposer's round and value are fixed initially, one Proposer per round: Ivy's
  `~proposal(r,V)` is a global check a Proposer can only make of itself.
- Quorums need not be closed under supersets, so a Proposer may decline to fix a round whose
  counted nodes form a quorum (`if ($)`); with the order of reports, every quorum whose
  members reported can be fixed. A decision does not depend on which quorum voted, so the
  Proposer decides at the first quorum of voters. A Proposer that declines with no report
  left, or has nothing to propose, is stuck: liveness only.
- `quorumin` sees a set's update history; the intersection axiom is on membership, so this
  generalises Ivy's quorum systems.

Initial worlds assume Ivy's two axioms (two quorums of a configuration share a node; a
configuration has at most one complete round), the roles of `master`, `nodes` and
`proposers`, one Proposer per round, and Ivy's `after init`: every Node's `joined` is at most the
Master's initial `complete` (Ivy's least round), and the containers are empty. Ivy's
rounds are any total order with a least element; here they are the integers from the
initial `complete` on. No finite Ivy run is lost: the rounds it uses map onto integers in
order, and `propose` needs every round between `cr` and `r` configured.

Safety (Theorem `safety`) is Ivy's: any two decisions agree (`decisions_agree`). Ivy's
`decision` is a relation, a Node's `decisions` a map: by `decision_taught` and
`decide_round_agree`, every decision comes from an `eDecide` of its round, and two of one
round agree, so a Node never needs two values for one round.

Deviations from the Ivy model:
- Messages are consumed; Ivy's persist. Every Ivy action that reads one is idempotent or
  disabled for good once it does nothing (`joined` only grows, a second `join_round` sends
  the same report, voting again changes nothing). A report for a round already fixed is
  dropped (Ivy chooses `quorum_of_round` at `propose`), and a Master request is consumed even
  when its guards fail (another Admin can ask later).
- A Proposer sends its `eOneA`s one round per step; the steps in between act as they could
  after Ivy's `send_1a`. It examines the lowest open round when a report or configuration
  arrives, which includes the last report of any quorum Ivy's `propose` could use.
- Ivy's `propose` spans three Proposer steps (the `Join` entry fixes the last round, the
  `Propose` entry sets `proposed`, the `Vote` entry sends the `eProposal`s), and other machines
  may act in between. That is Ivy's state right after `propose`, since `proposal(r, V)` is the
  Proposer's state, not its messages.
- A Node decides by being taught: the round's Proposer checks the quorum of votes, as it
  alone knows the configuration. Ivy's `decide(n, …)` is the delivery of `eDecide` to `n`.

Proof: the Paxos argument, per Proposer. Once a Proposer has decided, every Proposer of a
higher round that has proposed adopted the decided value (`decided_blocks`); a Proposer that
proposed its own value thus has no decision below it, which is why rounds below a complete
round need not be asked. The two steps that carry the argument, proposing and completing a
quorum of votes, are proved by hand from lemmas about one world (`propose_key`,
`decide_key`), giving `decides_agree` (every `eDecide` carries the same value) and then the
theorem.
-/
import PLean

open PLean

pmodule VerticalPaxos

  system s

  type tConfigReq = (round : Int, config : Int)
  type tConfigure = (round : Int, config : Int, complete : Int)
  type tComplete  = (round : Int)
  type tOneA      = (proposer : PLean.MachineRef, round : Int, rp : Int)
  type tJoinAck   = (node : PLean.MachineRef, round : Int, rp : Int, value : Int)
  type tProposal  = (proposer : PLean.MachineRef, round : Int, value : Int)
  type tVote      = (node : PLean.MachineRef, round : Int, value : Int)
  type tDecide    = (round : Int, value : Int)

  event eConfigReq : tConfigReq
  event eConfigure : tConfigure
  event eComplete  : tComplete
  event eOneA      : tOneA
  event eJoinAck   : tJoinAck
  event eProposal  : tProposal
  event eVote      : tVote
  event eDecide    : tDecide

  -- Ivy's `none`: the value a join ack reports for a round in which the node did not vote.
  function noneV       : Int
  function master      : PLean.MachineRef
  function nodes       : set[PLean.MachineRef]
  function proposers   : set[PLean.MachineRef]
  function quorumin    : set[PLean.MachineRef] → Int → Bool
  function complete_of : Int → Int → Bool

  -- The reconfiguration master.
  machine Master {
    var complete   : Int
    var configured : set[Int]

    start state Serve {
      -- Ivy's `configure_round(r, c)`.
      on eConfigReq (req : tConfigReq) {
        if ¬ (req.round ∈ configured) ∧ complete ≤ req.round ∧
            complete_of req.config complete = true then do
          configured += (req.round)
          foreach (p in proposers)
            invariant configures :
              new_sends e => e is eConfigure ∧ e.target ∈ proposers ∧
                (eConfigure_payload_of e).round = req.round ∧
                (eConfigure_payload_of e).config = req.config ∧
                (eConfigure_payload_of e).complete = complete ;
          {
            send p, eConfigure, (round = req.round, config = req.config, complete = complete)
          }
      }

      -- Ivy's `mark_complete(r)`.
      on eComplete (c : tComplete) {
        if ¬ (c.round ≤ complete) then do
          complete = c.round
      }
    }
  }

  -- The environment: asks the Master to configure `round` with `config`.
  machine Admin {
    var round  : Int
    var config : Int

    start state Request {
      entry {
        send master, eConfigReq, (round = round, config = config)
      }
    }
  }

  machine Node {
    var joined    : Int
    var votes     : map[Int, Int]
    var decisions : map[Int, Int]

    start state Serve {
      -- Ivy's `join_round(n, r, rp, v)`.
      on eOneA (req : tOneA) {
        if joined ≤ req.round then do
          var v : Int
          v = noneV
          if req.rp ∈ votes then do
            v = votes[req.rp]
          send req.proposer, eJoinAck,
            (node = this.ref, round = req.round, rp = req.rp, value = v)
          joined = req.round
      }

      -- Ivy's `cast_vote(n, v, r)`.
      on eProposal (p : tProposal) {
        if p.value ≠ noneV ∧ joined ≤ p.round then do
          votes[p.round] = p.value
          send p.proposer, eVote, (node = this.ref, round = p.round, value = p.value)
      }

      -- Ivy's `decide(n, r, c, v, q)`, the quorum checked by the round's Proposer.
      on eDecide (d : tDecide) {
        if d.round ∈ decisions then do
          assert (decisions[d.round] = d.value)
        else do
          decisions[d.round] = d.value
      }
    }
  }

  machine Proposer {
    var round    : Int
    var value    : Int
    var configs  : map[Int, Int]
    var config   : Int
    var cr       : Int
    var next1a   : Int
    var cur      : Int
    var acks     : map[Int, set[PLean.MachineRef]]
    var reported : map[Int, Int]
    var maxr     : Int
    var maxv     : Int
    var proposed : Int
    var voters   : set[PLean.MachineRef]

    start state Idle {
      on eConfigure (cfg : tConfigure) {
        configs[cfg.round] = cfg.config
        if cfg.round = round then do
          config = cfg.config
          cr = cfg.complete
          next1a = cfg.complete
          cur = cfg.complete
          maxr = cfg.complete - 1
          maxv = noneV
          goto Prepare
      }
    }

    -- Ivy's `send_1a(r, c, cr)`: one round `next1a` per step, from `cr` to `round - 1`.
    state Prepare {
      entry {
        if next1a < round then do
          foreach (n in nodes)
            invariant one_as :
              new_sends e => e is eOneA ∧ e.target ∈ nodes ∧
                (eOneA_payload_of e).proposer = this.ref ∧
                (eOneA_payload_of e).round = round ∧
                (eOneA_payload_of e).rp = next1a ;
          {
            send n, eOneA, (proposer = this.ref, round = round, rp = next1a)
          }
          next1a = next1a + 1
          goto Prepare
        goto Join
      }
    }

    -- The guards of Ivy's `propose`: every round from `cr` to `round - 1` is configured and
    -- has a quorum of its configuration that reported. `cur` is the lowest round whose
    -- quorum is not fixed yet; the entry fixes it, or waits for a report or a configuration.
    state Join {
      entry {
        if round ≤ cur then do
          goto Propose
        if cur ∈ configs ∧ cur ∈ acks then do
          if quorumin acks[cur] configs[cur] = true then do
            if ($) then do
              if cur ∈ reported then do
                maxr = cur
                maxv = reported[cur]
              cur = cur + 1
              goto Join
      }

      on eJoinAck (ack : tJoinAck) {
        if ack.round = round ∧ cur ≤ ack.rp ∧ ack.rp < round then do
          if ¬ (ack.rp ∈ acks) then do
            acks[ack.rp] = ∅
          acks[ack.rp] += (ack.node)
          if ack.value ≠ noneV then do
            reported[ack.rp] = ack.value
          goto Join
      }

      on eConfigure (cfg : tConfigure) {
        configs[cfg.round] = cfg.config
        goto Join
      }
    }

    -- The rest of Ivy's `propose(r, c, cr, maxr, v, w)`.
    state Propose {
      entry {
        if maxv ≠ noneV then do
          proposed = maxv
          goto Vote
        -- Ivy requires `v = none -> w ~= none`.
        if value ≠ noneV then do
          proposed = value
          send master, eComplete, (round = round)
          goto Vote
      }

      ignore eJoinAck, eConfigure
    }

    -- Ivy's `decide`: the voters form a quorum of the round's configuration.
    state Vote {
      entry {
        voters = ∅
        foreach (n in nodes)
          invariant proposals :
            new_sends e => e is eProposal ∧ e.target ∈ nodes ∧
              (eProposal_payload_of e).proposer = this.ref ∧
              (eProposal_payload_of e).round = round ∧
              (eProposal_payload_of e).value = proposed ;
        {
          send n, eProposal, (proposer = this.ref, round = round, value = proposed)
        }
      }

      on eVote (v : tVote) {
        if v.round = round ∧ v.value = proposed then do
          voters += (v.node)
          if quorumin voters config = true then do
            foreach (n in nodes)
              invariant decides :
                new_sends e => e is eDecide ∧ e.target ∈ nodes ∧ e.src = this.ref ∧
                  (eDecide_payload_of e).round = round ∧
                  (eDecide_payload_of e).value = proposed ;
            {
              send n, eDecide, (round = round, value = proposed)
            }
            send master, eComplete, (round = round)
            goto Done
      }

      ignore eJoinAck, eConfigure
    }

    state Done {
      ignore eVote, eJoinAck, eConfigure
    }
  }

  -- Ivy: `quorumin(Q1,C) & quorumin(Q2,C) -> exists N. member(N,Q1) & member(N,Q2)`.
  init-holds ∀ (c : Int) (q1 q2 : set[PLean.MachineRef]),
    quorumin q1 c = true → quorumin q2 c = true → ∃ n, n ∈ q1 ∧ n ∈ q2
  -- Ivy: `complete_of(C,R1) & complete_of(C,R2) -> R1 = R2`.
  init-holds ∀ c r1 r2 : Int, complete_of c r1 = true → complete_of c r2 = true → r1 = r2
  init-holds is_Master master s
  init-holds ∀ r : PLean.MachineRef, r ∈ nodes → is_Node r s
  init-holds ∀ r : PLean.MachineRef, r ∈ proposers → is_Proposer r s
  init-holds ∀ p1 p2 : Proposer, p1.round = p2.round → p1 = p2
  -- Ivy's `after init`.
  init-holds ∀ (n : Node) (m : Master), m.ref = master → n.joined ≤ m.complete
  init-holds ∀ (m : Master) (r : Int), ¬ (r ∈ m.configured)
  init-holds ∀ (n : Node) (r : Int), ¬ (r ∈ n.votes)
  init-holds ∀ (n : Node) (r : Int), ¬ (r ∈ n.decisions)
  init-holds ∀ (p : Proposer) (r : Int), ¬ (r ∈ p.configs)
  init-holds ∀ (p : Proposer) (r : Int), ¬ (r ∈ p.acks)
  init-holds ∀ (p : Proposer) (r : Int), ¬ (r ∈ p.reported)

  -- Where each kind of message goes.
  Lemma system_config {
    invariant master_kind    : is_Master master s
    invariant node_set       : ∀ r : PLean.MachineRef, r ∈ nodes → is_Node r s
    invariant proposer_set   : ∀ r : PLean.MachineRef, r ∈ proposers → is_Proposer r s
    invariant configure_routing  : ∀ e : eConfigure, sent e s → is_Proposer e.target s
    invariant complete_routing   : ∀ e : eComplete, sent e s → e.target = master
    invariant one_a_routing      : ∀ e : eOneA, sent e s → is_Node e.target s
    invariant one_a_from         : ∀ e : eOneA, sent e s → is_Proposer e.proposer s
    invariant join_ack_routing   : ∀ e : eJoinAck, sent e s → is_Proposer e.target s
    invariant proposal_routing   : ∀ e : eProposal, sent e s → is_Node e.target s
    invariant decide_routing     : ∀ e : eDecide, sent e s → is_Node e.target s
  }

  -- A Proposer leaves `Idle` before it asks to join its round.
  Lemma phases {
    invariant one_a_after_idle :
      ∀ e : eOneA, sent e s → stateOf e.proposer s ≠ some Proposer.Idle_st
    invariant join_ack_after_idle :
      ∀ e : eJoinAck, sent e s → stateOf e.target s ≠ some Proposer.Idle_st
  }

  -- One Proposer per round.
  Lemma rounds {
    invariant proposers_distinct : ∀ p1 p2 : Proposer, p1.round = p2.round → p1 = p2
  }

  -- Two quorums of a configuration share a node. This says nothing about the state:
  -- initial worlds assume it.
  Lemma quorum {
    invariant quorum_intersection : ∀ (c : Int) (q1 q2 : set[PLean.MachineRef]),
      quorumin q1 c = true → quorumin q2 c = true → ∃ n, n ∈ q1 ∧ n ∈ q2
  }

  -- The Master configures a round once, with a complete round no higher than it.
  Lemma configured {
    invariant config_req_routing : ∀ e : eConfigReq, sent e s → e.target = master
    invariant configure_below : ∀ e : eConfigure, sent e s → e.complete ≤ e.round
    invariant configure_known :
      ∀ (e : eConfigure) (m : Master), sent e s → m.ref = master →
        e.round ∈ m.configured ∧ e.complete ≤ m.complete
    invariant configure_unique :
      ∀ e1 e2 : eConfigure, sent e1 s → sent e2 s → e1.round = e2.round →
        e1.config = e2.config ∧ e1.complete = e2.complete
    -- A Proposer that left `Idle` holds its round's configuration and complete round.
    invariant config_recorded :
      ∀ p : Proposer, stateOf p.ref s ≠ some Proposer.Idle_st →
        p.configs[p.round]? = some p.config
    invariant cr_recorded :
      ∀ (p : Proposer) (e : eConfigure), stateOf p.ref s ≠ some Proposer.Idle_st → sent e s →
        e.round = p.round → e.complete = p.cr
    -- The configurations a Proposer holds are the Master's.
    invariant configs_known :
      ∀ (p : Proposer) (r c : Int), p.configs[r]? = some c →
        ∃ e : eConfigure, sent e s ∧ e.round = r ∧ e.config = c
    invariant configs_agree :
      ∀ (p : Proposer) (e : eConfigure) (r c : Int), sent e s → p.configs[r]? = some c →
        e.round = r → e.config = c
    invariant configs_configured :
      ∀ (p : Proposer) (m : Master) (r : Int), r ∈ p.configs → m.ref = master →
        r ∈ m.configured
    invariant round_configured :
      ∀ (p : Proposer) (m : Master), stateOf p.ref s ≠ some Proposer.Idle_st →
        m.ref = master → p.round ∈ m.configured
  }

  -- The Master's complete round rises only when a Proposer says a round is complete, and
  -- that round was configured.
  Lemma completes {
    invariant complete_from : ∀ e : eComplete, sent e s → is_Proposer e.src s
    invariant complete_configured :
      ∀ (e : eComplete) (m : Master), sent e s → m.ref = master → e.round ∈ m.configured
    invariant master_raised :
      ∀ (e : eConfigure) (m : Master), sent e s → m.ref = master → e.complete < m.complete →
        ∃ c : eComplete, sent c s ∧ c.round = m.complete
    invariant configure_raised :
      ∀ e1 e2 : eConfigure, sent e1 s → sent e2 s → e1.complete < e2.complete →
        ∃ c : eComplete, sent c s ∧ c.round = e2.complete
    invariant configure_after :
      ∀ e1 e2 : eConfigure, sent e1 s → sent e2 s → e1.complete < e2.complete →
        e2.complete < e2.round
  }

  -- A round is complete when its Proposer proposed its own value (no vote was reported)
  -- or decided.
  Lemma complete_rounds {
    invariant complete_proposer :
      ∀ (c : eComplete) (p : Proposer), sent c s → c.src = p.ref →
        c.round = p.round ∧
        (stateOf p.ref s = some Proposer.Vote_st ∨ stateOf p.ref s = some Proposer.Done_st) ∧
        (stateOf p.ref s ≠ some Proposer.Done_st → p.maxv = noneV)
  }

  -- What a Node's reports (`eJoinAck`) and votes (`eVote`) say about its state.
  Lemma node_state {
    invariant one_a_shape : ∀ e : eOneA, sent e s → e.rp < e.round
    invariant join_ack_shape : ∀ e : eJoinAck, sent e s → e.rp < e.round
    invariant join_recorded :
      ∀ (e : eJoinAck) (n : Node), sent e s → e.node = n.ref → e.round ≤ n.joined
    invariant vote_recorded :
      ∀ (e : eVote) (n : Node), sent e s → e.node = n.ref → n.votes[e.round]? = some e.value
    -- A Node voted for a proposal (Ivy: `vote(N,R,V) -> proposal(R,V)`).
    invariant vote_sent :
      ∀ (n : Node) (r v : Int), n.votes[r]? = some v →
        ∃ e : eProposal, sent e s ∧ e.round = r ∧ e.value = v
  }

  -- Proposals and votes are for the value of the round's Proposer, once it has proposed.
  Lemma proposals {
    invariant proposal_from : ∀ e : eProposal, sent e s → is_Proposer e.proposer s
    invariant vote_routing  : ∀ e : eVote, sent e s → is_Proposer e.target s
    invariant proposal_proposer :
      ∀ (e : eProposal) (p : Proposer), sent e s → e.proposer = p.ref →
        (stateOf p.ref s = some Proposer.Vote_st ∨ stateOf p.ref s = some Proposer.Done_st) ∧
        e.round = p.round ∧ e.value = p.proposed
    invariant vote_proposer :
      ∀ (e : eVote) (p : Proposer), sent e s → e.target = p.ref →
        (stateOf p.ref s = some Proposer.Vote_st ∨ stateOf p.ref s = some Proposer.Done_st) ∧
        e.round = p.round ∧ e.value = p.proposed
    invariant proposed_value :
      ∀ p : Proposer,
        (stateOf p.ref s = some Proposer.Vote_st ∨ stateOf p.ref s = some Proposer.Done_st) →
        p.proposed ≠ noneV
    invariant vote_value : ∀ e : eVote, sent e s → e.value ≠ noneV
  }

  -- A report gives the Node's vote in that round, if it voted there.
  Lemma gap {
    invariant report_gap :
      ∀ (a : eJoinAck) (v : eVote), sent a s → sent v s → a.node = v.node → v.round = a.rp →
        a.value = v.value
    invariant report_voted :
      ∀ a : eJoinAck, sent a s → a.value ≠ noneV →
        ∃ e : eProposal, sent e s ∧ e.round = a.rp ∧ e.value = a.value
  }

  -- The reports a Proposer has counted.
  Lemma counted {
    invariant acks_from :
      ∀ (p : Proposer) (r : Int) (q : set[PLean.MachineRef]) (n : PLean.MachineRef),
        p.acks[r]? = some q → n ∈ q →
        ∃ e : eJoinAck, sent e s ∧ e.target = p.ref ∧ e.node = n ∧ e.round = p.round ∧
          e.rp = r
    invariant acks_below : ∀ (p : Proposer) (r : Int), r ∈ p.acks → r < p.round
    invariant reported_below : ∀ (p : Proposer) (r : Int), r ∈ p.reported → r < p.round
    invariant reported_voted :
      ∀ (p : Proposer) (r w : Int), p.reported[r]? = some w →
        ∃ e : eProposal, sent e s ∧ e.round = r ∧ e.value = w
    invariant reported_value :
      ∀ (p : Proposer) (r w : Int), p.reported[r]? = some w → w ≠ noneV
    -- A Node counted in a round reported its vote there, if any.
    invariant voters_reported :
      ∀ (p : Proposer) (v : eVote) (q : set[PLean.MachineRef]), sent v s →
        p.acks[v.round]? = some q → v.node ∈ q → v.round ∈ p.reported
  }

  -- The rounds a Proposer has fixed, from `cr` to `cur - 1`: each has a quorum of its
  -- configuration, and `maxr` / `maxv` are the highest one with a reported vote.
  Lemma fixed {
    invariant cur_bounds :
      ∀ p : Proposer, stateOf p.ref s ≠ some Proposer.Idle_st →
        p.cr ≤ p.cur ∧ p.maxr < p.cur ∧ p.cr ≤ p.maxr + 1
    invariant fixed_known :
      ∀ (p : Proposer) (r : Int), stateOf p.ref s ≠ some Proposer.Idle_st →
        p.cr ≤ r → r < p.cur → r ∈ p.configs ∧ r ∈ p.acks
    invariant fixed_quorum :
      ∀ (p : Proposer) (r : Int) (q : set[PLean.MachineRef]) (e : eConfigure),
        stateOf p.ref s ≠ some Proposer.Idle_st → p.cr ≤ r → r < p.cur →
        p.acks[r]? = some q → sent e s → e.round = r → quorumin q e.config = true
    invariant fixed_max :
      ∀ (p : Proposer) (r : Int), stateOf p.ref s ≠ some Proposer.Idle_st →
        p.cr ≤ r → r < p.cur → r ∈ p.reported → r ≤ p.maxr
    invariant max_reported :
      ∀ p : Proposer, stateOf p.ref s ≠ some Proposer.Idle_st → p.cr ≤ p.maxr →
        p.maxr ∈ p.reported
    invariant max_value :
      ∀ (p : Proposer) (w : Int), stateOf p.ref s ≠ some Proposer.Idle_st → p.cr ≤ p.maxr →
        p.reported[p.maxr]? = some w → p.maxv = w
    invariant max_none :
      ∀ p : Proposer, stateOf p.ref s ≠ some Proposer.Idle_st → p.maxr < p.cr →
        p.maxv = noneV
  }

  -- A Proposer proposes once every round from `cr` on is fixed, the highest reported value
  -- if there is one.
  Lemma proposing {
    invariant all_fixed :
      ∀ p : Proposer,
        (stateOf p.ref s = some Proposer.Propose_st ∨ stateOf p.ref s = some Proposer.Vote_st ∨
          stateOf p.ref s = some Proposer.Done_st) → p.round ≤ p.cur
    invariant proposed_adopts :
      ∀ p : Proposer,
        (stateOf p.ref s = some Proposer.Vote_st ∨ stateOf p.ref s = some Proposer.Done_st) →
        p.maxv ≠ noneV → p.proposed = p.maxv
  }

  -- The votes a Proposer has counted; it decided once they form a quorum.
  Lemma decided {
    invariant voters_voted :
      ∀ (p : Proposer) (n : PLean.MachineRef),
        ((stateOf p.ref s = some Proposer.Vote_st ∧ (s.machines p.ref).pending = none) ∨
          stateOf p.ref s = some Proposer.Done_st) → n ∈ p.voters →
        ∃ v : eVote, sent v s ∧ v.node = n ∧ v.round = p.round ∧ v.value = p.proposed
    invariant decided_quorum :
      ∀ p : Proposer, stateOf p.ref s = some Proposer.Done_st →
        quorumin p.voters p.config = true
    invariant decide_from : ∀ e : eDecide, sent e s → is_Proposer e.src s
    invariant decide_decided :
      ∀ (e : eDecide) (p : Proposer), sent e s → e.src = p.ref →
        stateOf p.ref s = some Proposer.Done_st ∧ e.round = p.round ∧ e.value = p.proposed
  }

  -- Once a Proposer has decided, every Proposer of a higher round that has proposed
  -- adopted a reported value, the decided one. So Proposers that decided agree.
  Lemma agreement {
    invariant decided_blocks :
      ∀ p1 p2 : Proposer, stateOf p1.ref s = some Proposer.Done_st →
        (stateOf p2.ref s = some Proposer.Vote_st ∨ stateOf p2.ref s = some Proposer.Done_st) →
        p1.round < p2.round → p2.maxv ≠ noneV ∧ p2.proposed = p1.proposed
    invariant decided_agree :
      ∀ p1 p2 : Proposer, stateOf p1.ref s = some Proposer.Done_st →
        stateOf p2.ref s = some Proposer.Done_st → p1.proposed = p2.proposed
    -- Every decision taught is the same.
    invariant decides_agree :
      ∀ e1 e2 : eDecide, sent e1 s → sent e2 s → e1.value = e2.value
  }

  Theorem safety {
    -- A Node's decision was taught.
    invariant decision_taught :
      ∀ (n : Node) (r v : Int), n.decisions[r]? = some v →
        ∃ e : eDecide, sent e s ∧ e.round = r ∧ e.value = v
    -- Two decisions taught for one round agree: a Node never decides a round twice with
    -- different values (Ivy's `decision` is a relation, `decisions` a map).
    invariant decide_round_agree :
      ∀ e1 e2 : eDecide, sent e1 s → sent e2 s → e1.round = e2.round → e1.value = e2.value
    -- The Ivy model's `decision(N1,R1,V1) & decision(N2,R2,V2) -> V1 = V2`.
    invariant decisions_agree :
      ∀ (n1 n2 : Node) (r1 r2 v1 v2 : Int), n1.decisions[r1]? = some v1 →
        n2.decisions[r2]? = some v2 → v1 = v2
  }

  Proof {
    prove system_config ;
    prove phases using system_config ;
    prove rounds ;
    prove quorum ;
    prove configured using system_config ;
    prove completes using system_config, configured ;
    prove complete_rounds ;
    prove node_state using proposals, rounds ;
    prove proposals using system_config ;
    prove gap using node_state ;
    prove counted using node_state, gap, proposals ;
    prove fixed using configured ;
    prove proposing ;
    prove decided ;
    prove agreement using rounds, quorum, configured, completes, complete_rounds, proposals,
                          counted, fixed, proposing, decided ;
    prove safety using decided, agreement, rounds ;
  }

end VerticalPaxos

#gen_module VerticalPaxos
#pwf        VerticalPaxos

namespace VerticalPaxos
open PLean Std.WP

/-! ## The argument

`adopt`: a Proposer that proposes adopts the value of a decided round below its own, given
that the Proposers of the rounds in between agree with it. `propose_key` uses it at the
proposing step (the rounds in between from `decided_blocks`), `decide_key` at the step
that completes a quorum of votes (by induction on the distance between the rounds). -/

/-- A Proposer that left `Idle` has its round's `eConfigure`. -/
abbrev RoundConfig (g : GW) : Prop :=
  ∀ p : Proposer, is_Proposer p.ref g → stateOf p.ref g ≠ some Proposer.Idle_st →
    ∃ e : Msg E, is_eConfigure e ∧ sent e g ∧
      (eConfigure_payload_of e).round = (g.machines p.ref).fields.Proposer_round ∧
      (eConfigure_payload_of e).config = (g.machines p.ref).fields.Proposer_config ∧
      (eConfigure_payload_of e).complete = (g.machines p.ref).fields.Proposer_cr

theorem round_config (g : GW) (hrec : config_recorded g) (hck : configs_known g)
    (hcrr : cr_recorded g) : RoundConfig g := by
  intro p hp hpi
  obtain ⟨e, he, hs, her, hec⟩ := hck p hp _ _ (hrec p hp hpi)
  exact ⟨e, he, hs, her, hec, hcrr p hp e he hpi hs her⟩

theorem map_some_of_mem {V : Type} {m : PMap Int V} {k : Int} (h : k ∈ m) :
    ∃ v, m[k]? = some v :=
  Option.isSome_iff_exists.mp h

theorem mem_add_cases {q : PSet MachineRef} {x a : MachineRef} (h : a ∈ q.add x) :
    a = x ∨ a ∈ q := by
  change (q.add x).mem a = true at h
  rw [PSet.mem_add, Bool.or_eq_true, decide_eq_true_eq] at h
  exact h

/-- A Proposer `p2` that has fixed every round from its `cr` to `round - 1` adopts `v`, when a
quorum `Q` of the configuration of a round `r1` below its own voted `v` there and every
Proposer of a round between them that proposed, proposed `v` after adopting it. -/
theorem adopt (g : GW) (hcr : RoundConfig g)
    (hcb : configure_below g) (hraised : configure_raised g) (hafter : configure_after g)
    (hcfrom : complete_from g) (hcprop : complete_proposer g)
    (hq : quorum_intersection g) (hdist : proposers_distinct g)
    (hpf : proposal_from g) (hpp : proposal_proposer g) (hvr : vote_routing g)
    (hvp : vote_proposer g)
    (hrv : reported_voted g) (hrval : reported_value g) (hrb : reported_below g)
    (hvrep : voters_reported g)
    (hfk : fixed_known g) (hfq : fixed_quorum g) (hfm : fixed_max g)
    (hmr : max_reported g) (hmv : max_value g)
    (hdq : decided_quorum g) (hvv : voters_voted g)
    (v r1 : Int) (Q : PSet MachineRef) (e1 : Msg E) (he1 : is_eConfigure e1)
    (hs1 : sent e1 g) (he1r : (eConfigure_payload_of e1).round = r1)
    (hQ : quorumin Q (eConfigure_payload_of e1).config = true)
    (hQv : ∀ n, n ∈ Q → ∃ w : Msg E, is_eVote w ∧ sent w g ∧ (eVote_payload_of w).node = n ∧
      (eVote_payload_of w).round = r1 ∧ (eVote_payload_of w).value = v)
    (p2 : MachineRef) (hp2 : is_Proposer p2 g) (hp2i : stateOf p2 g ≠ some Proposer.Idle_st)
    (hfix : (g.machines p2).fields.Proposer_round ≤ (g.machines p2).fields.Proposer_cur)
    (hlt : r1 < (g.machines p2).fields.Proposer_round)
    (IH : ∀ p : MachineRef, ((g.machines p).state = some S.Proposer_Vote ∨
        (g.machines p).state = some S.Proposer_Done) →
      r1 < (g.machines p).fields.Proposer_round →
      (g.machines p).fields.Proposer_round < (g.machines p2).fields.Proposer_round →
      (g.machines p).fields.Proposer_maxv ≠ noneV ∧ (g.machines p).fields.Proposer_proposed = v) :
    (g.machines p2).fields.Proposer_maxv ≠ noneV ∧ (g.machines p2).fields.Proposer_maxv = v := by
  -- (A) A round from `cr` on, a quorum of whose configuration voted there, was reported.
  have hA : ∀ (r : Int) (e : Msg E), is_eConfigure e → sent e g →
      (eConfigure_payload_of e).round = r →
      ∀ (Q' : PSet MachineRef), quorumin Q' (eConfigure_payload_of e).config = true →
      (∀ n, n ∈ Q' → ∃ w : Msg E, is_eVote w ∧ sent w g ∧ (eVote_payload_of w).node = n ∧
        (eVote_payload_of w).round = r) →
      (g.machines p2).fields.Proposer_cr ≤ r → r < (g.machines p2).fields.Proposer_round →
      r ≤ (g.machines p2).fields.Proposer_maxr := by
    intro r e he hs her Q' hQ' hQ'v hcr hr
    have hrc : r < (g.machines p2).fields.Proposer_cur := by omega
    have hk : r ∈ (g.machines p2).fields.Proposer_configs ∧
        r ∈ (g.machines p2).fields.Proposer_acks := hfk ⟨p2⟩ hp2 r hp2i hcr hrc
    obtain ⟨q, hqq⟩ := map_some_of_mem hk.2
    have hqc : quorumin q (eConfigure_payload_of e).config = true :=
      hfq ⟨p2⟩ hp2 r q e he hp2i hcr hrc hqq hs her
    obtain ⟨n, hn1, hn2⟩ := hq _ q Q' hqc hQ'
    obtain ⟨w, hw, hsw, hwn, hwr⟩ := hQ'v n hn2
    have hrep : (eVote_payload_of w).round ∈ (g.machines p2).fields.Proposer_reported :=
      hvrep ⟨p2⟩ hp2 w hw q hsw (by rw [hwr]; exact hqq) (by rw [hwn]; exact hn1)
    rw [hwr] at hrep
    exact hfm ⟨p2⟩ hp2 r hp2i hcr hrc hrep
  -- (B) Once the highest round reported is `r1` or above, its value is `v`.
  have hB : r1 ≤ (g.machines p2).fields.Proposer_maxr →
      (g.machines p2).fields.Proposer_cr ≤ (g.machines p2).fields.Proposer_maxr →
      (g.machines p2).fields.Proposer_maxv ≠ noneV ∧
        (g.machines p2).fields.Proposer_maxv = v := by
    intro h1 h2
    have hm : (g.machines p2).fields.Proposer_maxr ∈ (g.machines p2).fields.Proposer_reported :=
      hmr ⟨p2⟩ hp2 hp2i h2
    obtain ⟨w, hw⟩ := map_some_of_mem hm
    have hmw : (g.machines p2).fields.Proposer_maxv = w := hmv ⟨p2⟩ hp2 w hp2i h2 hw
    have hwn : w ≠ noneV := hrval ⟨p2⟩ hp2 _ w hw
    have hbelow : (g.machines p2).fields.Proposer_maxr <
        (g.machines p2).fields.Proposer_round := hrb ⟨p2⟩ hp2 _ hm
    -- `maxv` was proposed at `maxr`, by the Proposer of that round.
    obtain ⟨x, hx, hsx, hxr, hxv⟩ := hrv ⟨p2⟩ hp2 _ w hw
    have hxp := hpf x hx hsx
    obtain ⟨hxst, hxr', hxv'⟩ := hpp x hx ⟨(eProposal_payload_of x).proposer⟩ hxp hsx rfl
    have hxr'' : (eProposal_payload_of x).round =
        (g.machines (eProposal_payload_of x).proposer).fields.Proposer_round := hxr'
    have hxv'' : (eProposal_payload_of x).value =
        (g.machines (eProposal_payload_of x).proposer).fields.Proposer_proposed := hxv'
    refine ⟨by rw [hmw]; exact hwn, ?_⟩
    rw [hmw, ← hxv, hxv'']
    by_cases heq : (g.machines p2).fields.Proposer_maxr = r1
    · -- That proposal is the one of `r1`'s Proposer, for which `Q` voted.
      obtain ⟨n, hn, -⟩ := hq _ Q Q hQ hQ
      obtain ⟨y, hy, hsy, -, hyr, hyv⟩ := hQv n hn
      have hyp := hvr y hy hsy
      obtain ⟨-, hyr', hyv'⟩ := hvp y hy ⟨y.target⟩ hyp hsy rfl
      have hyr'' : (eVote_payload_of y).round =
          (g.machines y.target).fields.Proposer_round := hyr'
      have hyv'' : (eVote_payload_of y).value =
          (g.machines y.target).fields.Proposer_proposed := hyv'
      have hsame := hdist ⟨(eProposal_payload_of x).proposer⟩ hxp ⟨y.target⟩ hyp (by
        show (g.machines (eProposal_payload_of x).proposer).fields.Proposer_round =
          (g.machines y.target).fields.Proposer_round
        omega)
      have ht : (eProposal_payload_of x).proposer = y.target := congrArg Proposer.ref hsame
      rw [ht, ← hyv'', hyv]
    · exact (IH (eProposal_payload_of x).proposer hxst (by omega) (by omega)).2
  obtain ⟨e2, he2, hs2, he2r, -, he2cr⟩ := hcr ⟨p2⟩ hp2 hp2i
  have he2cr' : (eConfigure_payload_of e2).complete = (g.machines p2).fields.Proposer_cr := he2cr
  have he2r' : (eConfigure_payload_of e2).round = (g.machines p2).fields.Proposer_round := he2r
  by_cases hcase : (g.machines p2).fields.Proposer_cr ≤ r1
  · have h1 := hA r1 e1 he1 hs1 he1r Q hQ (fun n hn => by
        obtain ⟨w, hw, hsw, hwn, hwr, -⟩ := hQv n hn
        exact ⟨w, hw, hsw, hwn, hwr⟩) hcase hlt
    exact hB h1 (by omega)
  · -- `r1` is below `cr`: the Proposer of the round `cr` said it is complete, and decided.
    have hcb1 := hcb e1 he1 hs1
    obtain ⟨k, hk, hsk, hkr⟩ := hraised e1 he1 e2 he2 hs1 hs2 (by omega)
    have hkp := hcfrom k hk hsk
    obtain ⟨hk3r, hk3st, hk3none⟩ := hcprop k hk ⟨k.src⟩ hkp hsk rfl
    have hk3r' : (eComplete_payload_of k).round = (g.machines k.src).fields.Proposer_round := hk3r
    have haft := hafter e1 he1 e2 he2 hs1 hs2 (by omega)
    have hih := IH k.src hk3st (by omega) (by omega)
    have hdone : (g.machines k.src).state = some S.Proposer_Done := by
      apply Classical.byContradiction
      intro hnd
      exact hih.1 (hk3none hnd)
    have hvq : quorumin (g.machines k.src).fields.Proposer_voters
        (g.machines k.src).fields.Proposer_config = true := hdq ⟨k.src⟩ hkp hdone
    have hni : stateOf k.src g ≠ some Proposer.Idle_st := by
      intro h
      have h' : (g.machines k.src).state = some S.Proposer_Idle := h
      rw [hdone] at h'
      cases h'
    obtain ⟨e3, he3, hs3, he3r, he3c, -⟩ := hcr ⟨k.src⟩ hkp hni
    have he3r' : (eConfigure_payload_of e3).round = (g.machines k.src).fields.Proposer_round := he3r
    have he3c' : (eConfigure_payload_of e3).config =
        (g.machines k.src).fields.Proposer_config := he3c
    have h3 := hA _ e3 he3 hs3 he3r' (g.machines k.src).fields.Proposer_voters
      (by rw [he3c']; exact hvq) (fun n hn => by
        obtain ⟨w, hw, hsw, hwn, hwr, -⟩ := hvv ⟨k.src⟩ hkp n (Or.inr hdone) hn
        exact ⟨w, hw, hsw, hwn, hwr⟩) (by omega) (by omega)
    exact hB (by omega) (by omega)

/-- When a Proposer in `Propose` proposes, it adopts the value of every Proposer that decided
at a lower round. -/
theorem propose_key (g : GW) (hcr : RoundConfig g)
    (hcb : configure_below g) (hraised : configure_raised g) (hafter : configure_after g)
    (hcfrom : complete_from g) (hcprop : complete_proposer g)
    (hq : quorum_intersection g) (hdist : proposers_distinct g)
    (hpf : proposal_from g) (hpp : proposal_proposer g) (hvr : vote_routing g)
    (hvp : vote_proposer g)
    (hrv : reported_voted g) (hrval : reported_value g) (hrb : reported_below g)
    (hvrep : voters_reported g)
    (hfk : fixed_known g) (hfq : fixed_quorum g) (hfm : fixed_max g)
    (hmr : max_reported g) (hmv : max_value g)
    (hdq : decided_quorum g) (hvv : voters_voted g) (haf : all_fixed g)
    (hdb : decided_blocks g)
    (dst : MachineRef) (hst : (g.machines dst).state = some S.Proposer_Propose)
    (p1 : MachineRef) (hd : (g.machines p1).state = some S.Proposer_Done)
    (hlt : (g.machines p1).fields.Proposer_round < (g.machines dst).fields.Proposer_round) :
    (g.machines dst).fields.Proposer_maxv ≠ noneV ∧
      (g.machines dst).fields.Proposer_maxv = (g.machines p1).fields.Proposer_proposed := by
  have hdst : is_Proposer dst g := Or.inl (Or.inl (Or.inr hst))
  have hp1 : is_Proposer p1 g := Or.inr hd
  have hni : stateOf dst g ≠ some Proposer.Idle_st := by
    intro h
    have h' : (g.machines dst).state = some S.Proposer_Idle := h
    rw [hst] at h'
    cases h'
  have hni1 : stateOf p1 g ≠ some Proposer.Idle_st := by
    intro h
    have h' : (g.machines p1).state = some S.Proposer_Idle := h
    rw [hd] at h'
    cases h'
  obtain ⟨e1, he1, hs1, he1r, he1c, -⟩ := hcr ⟨p1⟩ hp1 hni1
  have he1c' : (eConfigure_payload_of e1).config = (g.machines p1).fields.Proposer_config := he1c
  have hvq : quorumin (g.machines p1).fields.Proposer_voters
      (g.machines p1).fields.Proposer_config = true := hdq ⟨p1⟩ hp1 hd
  have hfix : (g.machines dst).fields.Proposer_round ≤ (g.machines dst).fields.Proposer_cur :=
    haf ⟨dst⟩ hdst (Or.inl hst)
  exact adopt g hcr hcb hraised hafter hcfrom hcprop hq hdist hpf hpp hvr hvp hrv hrval
    hrb hvrep hfk hfq hfm hmr hmv hdq hvv (g.machines p1).fields.Proposer_proposed
    (g.machines p1).fields.Proposer_round (g.machines p1).fields.Proposer_voters e1 he1 hs1 he1r
    (by rw [he1c']; exact hvq)
    (fun n hn => hvv ⟨p1⟩ hp1 n (Or.inr hd) hn)
    dst hdst hni hfix hlt
    (fun p hp h1 _ => hdb ⟨p1⟩ hp1 ⟨p⟩ (hp.elim (fun h => Or.inl (Or.inr h)) Or.inr) hd hp h1)

/-- When a Proposer in `Vote` counts the vote `pl` and completes a quorum, its value is that
of every Proposer that has proposed at a higher round: by induction on that round. -/
theorem decide_key (g : GW) (i : Nat) (src dst : MachineRef) (pl : eVote_payload)
    (hcr : RoundConfig g)
    (hcb : configure_below g) (hraised : configure_raised g) (hafter : configure_after g)
    (hcfrom : complete_from g) (hcprop : complete_proposer g)
    (hq : quorum_intersection g) (hdist : proposers_distinct g)
    (hpf : proposal_from g) (hpp : proposal_proposer g) (hvr : vote_routing g)
    (hvp : vote_proposer g)
    (hrv : reported_voted g) (hrval : reported_value g) (hrb : reported_below g)
    (hvrep : voters_reported g)
    (hfk : fixed_known g) (hfq : fixed_quorum g) (hfm : fixed_max g)
    (hmr : max_reported g) (hmv : max_value g)
    (hdq : decided_quorum g) (hvv : voters_voted g) (haf : all_fixed g)
    (hpa : proposed_adopts g)
    (hlog : g.log i = some { src := src, dst := dst, ev := E.eVote pl })
    (hst : (g.machines dst).state = some S.Proposer_Vote)
    (hpend : (g.machines dst).pending = none)
    (hpr : pl.round = (g.machines dst).fields.Proposer_round)
    (hpv : pl.value = (g.machines dst).fields.Proposer_proposed)
    (hq1 : quorumin ((g.machines dst).fields.Proposer_voters.add pl.node)
      (g.machines dst).fields.Proposer_config = true)
    (p2 : MachineRef)
    (hph : (g.machines p2).state = some S.Proposer_Vote ∨
      (g.machines p2).state = some S.Proposer_Done)
    (hlt : (g.machines dst).fields.Proposer_round < (g.machines p2).fields.Proposer_round) :
    (g.machines p2).fields.Proposer_maxv ≠ noneV ∧
      (g.machines p2).fields.Proposer_proposed = (g.machines dst).fields.Proposer_proposed := by
  have hdst : is_Proposer dst g := Or.inl (Or.inr hst)
  have hni : stateOf dst g ≠ some Proposer.Idle_st := by
    intro h
    have h' : (g.machines dst).state = some S.Proposer_Idle := h
    rw [hst] at h'
    cases h'
  obtain ⟨e1, he1, hs1, he1r, he1c, -⟩ := hcr ⟨dst⟩ hdst hni
  have he1c' : (eConfigure_payload_of e1).config = (g.machines dst).fields.Proposer_config := he1c
  have hQv : ∀ n, n ∈ (g.machines dst).fields.Proposer_voters.add pl.node →
      ∃ w : Msg E, is_eVote w ∧ sent w g ∧ (eVote_payload_of w).node = n ∧
        (eVote_payload_of w).round = (g.machines dst).fields.Proposer_round ∧
        (eVote_payload_of w).value = (g.machines dst).fields.Proposer_proposed := by
    intro n hn
    rcases mem_add_cases hn with rfl | hn
    · exact ⟨⟨i, src, dst, E.eVote pl⟩, ⟨pl, rfl⟩, hlog, rfl, hpr, hpv⟩
    · exact hvv ⟨dst⟩ hdst n (Or.inl ⟨hst, hpend⟩) hn
  suffices step : ∀ (k : Nat) (p2 : MachineRef),
      ((g.machines p2).state = some S.Proposer_Vote ∨
        (g.machines p2).state = some S.Proposer_Done) →
      (g.machines dst).fields.Proposer_round < (g.machines p2).fields.Proposer_round →
      ((g.machines p2).fields.Proposer_round - (g.machines dst).fields.Proposer_round).toNat ≤ k →
      (g.machines p2).fields.Proposer_maxv ≠ noneV ∧
        (g.machines p2).fields.Proposer_proposed = (g.machines dst).fields.Proposer_proposed from
    step _ p2 hph hlt (Nat.le_refl _)
  intro k
  induction k with
  | zero => intro p2 _ hlt hk; omega
  | succ k ih =>
    intro p2 hph hlt hk
    have hp2 : is_Proposer p2 g := hph.elim (fun h => Or.inl (Or.inr h)) Or.inr
    have hni2 : stateOf p2 g ≠ some Proposer.Idle_st := by
      intro h
      have h' : (g.machines p2).state = some S.Proposer_Idle := h
      rcases hph with h2 | h2 <;> rw [h2] at h' <;> cases h'
    have hfix : (g.machines p2).fields.Proposer_round ≤ (g.machines p2).fields.Proposer_cur :=
      haf ⟨p2⟩ hp2 (Or.inr hph)
    have hres := adopt g hcr hcb hraised hafter hcfrom hcprop hq hdist hpf hpp hvr hvp
      hrv hrval hrb hvrep hfk hfq hfm hmr hmv hdq hvv (g.machines dst).fields.Proposer_proposed
      (g.machines dst).fields.Proposer_round _ e1 he1 hs1 he1r
      (by rw [he1c']; exact hq1) hQv p2 hp2 hni2 hfix hlt
      (fun p hp h1 h2 => ih p hp h1 (by omega))
    have hadopt : (g.machines p2).fields.Proposer_proposed =
        (g.machines p2).fields.Proposer_maxv := hpa ⟨p2⟩ hp2 hph hres.1
    exact ⟨hres.1, by rw [hadopt]; exact hres.2⟩


/-! ## The obligations of those steps

Each hands its lemma to `pverify_auto` in place of the invariants the lemma used. -/

set_option maxHeartbeats 1000000 in
@[pverifyProof] theorem Proposer.Propose.entry_correct_agreement :
    ∀ (g : GW) (m : MachineRef) (arg : G),
      Framework g → g.error = none →
      agreement g ∧ rounds g ∧ quorum g ∧ configured g ∧ completes g ∧ complete_rounds g ∧
        proposals g ∧ counted g ∧ fixed g ∧ proposing g ∧ decided g →
      (g.machines m).state = some S.Proposer_Propose →
      (g.machines m).pending = some arg → (g.machines m).halted = false →
      RunPost
        (fun g' =>
          PostInv "decided_blocks" (decided_blocks g') ∧
            PostInv "decided_agree" (decided_agree g') ∧
            PostInv "decides_agree" (decides_agree g'))
        g m { g.machines m with pending := none } (Proposer.Propose.entry arg) := by
  intro g m arg hF hE hInv hst hpend hh
  obtain ⟨⟨hdb, hda, hdag⟩, hdist, hq, ⟨-, hcb, -, -, hcrec, hcrr, hck, -, -, -⟩,
    ⟨hcfrom, -, -, hraised, hafter⟩, hcprop, ⟨hpf, hvr, hpp, hvp, -, -⟩,
    ⟨-, -, hrb, hrv, hrval, hvrep⟩, ⟨-, hfk, hfq, hfm, hmr, hmv, -⟩, ⟨haf, -⟩,
    ⟨hvv, hdq, -, -⟩⟩ := hInv
  have hcr := round_config g hcrec hck hcrr
  have key := propose_key g hcr hcb hraised hafter hcfrom hcprop hq hdist hpf hpp hvr
    hvp hrv hrval hrb hvrep hfk hfq hfm hmr hmv hdq hvv haf hdb m hst
  clear hdist hq hcb hcr hcrec hcrr hck hcfrom hraised hafter hcprop hpf hpp hvr hvp hrb hrv
    hrval hvrep hfk hfq hfm hmr hmv haf hvv hdq
  pverify_auto

set_option maxHeartbeats 1000000 in
@[pverifyProof] theorem Proposer.Vote.eVote_correct_agreement :
    ∀ (g : GW) (i : Nat) (src dst : MachineRef) (pl : eVote_payload),
      Framework g → g.error = none →
      agreement g ∧ rounds g ∧ quorum g ∧ configured g ∧ completes g ∧ complete_rounds g ∧
        proposals g ∧ counted g ∧ fixed g ∧ proposing g ∧ decided g →
      g.net i = some { src := src, dst := dst, ev := E.eVote pl } →
      g.log i = some { src := src, dst := dst, ev := E.eVote pl } →
      i < g.clock → (g.machines dst).state = some S.Proposer_Vote →
      (g.machines dst).pending = none → (g.machines dst).waiting = none →
      (g.machines dst).halted = false →
      RunPost
        (fun g' =>
          PostInv "decided_blocks" (decided_blocks g') ∧
            PostInv "decided_agree" (decided_agree g') ∧
            PostInv "decides_agree" (decides_agree g'))
        (GWorld.consume g i) dst (g.machines dst) (Proposer.Vote.eVote_handler pl) := by
  intro g i src dst pl hF hE hInv hnet hlog hi hst hpend
  obtain ⟨⟨hdb, hda, hdag⟩, hdist, hq, ⟨-, hcb, -, -, hcrec, hcrr, hck, -, -, -⟩,
    ⟨hcfrom, -, -, hraised, hafter⟩, hcprop, ⟨hpf, hvr, hpp, hvp, -, -⟩,
    ⟨-, -, hrb, hrv, hrval, hvrep⟩, ⟨-, hfk, hfq, hfm, hmr, hmv, -⟩, ⟨haf, hpa⟩,
    ⟨hvv, hdq, hdf, hdd⟩⟩ := hInv
  have hcr := round_config g hcrec hck hcrr
  have key := decide_key g i src dst pl hcr hcb hraised hafter hcfrom hcprop hq hdist hpf
    hpp hvr hvp hrv hrval hrb hvrep hfk hfq hfm hmr hmv hdq hvv haf hpa hlog hst hpend
  clear hq hcb hcr hcrec hcrr hck hcfrom hraised hafter hcprop hpf hpp hvr hvp hrb hrv hrval
    hvrep hfk hfq hfm hmr hmv haf hpa hvv hdq
  pverify_auto

end VerticalPaxos

#pverify VerticalPaxos
