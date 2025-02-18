type tRound = int;
type tValue = int;
type tConfig = (view: Orchestrator, round: tRound, completed: tRound, acceptors: seq[machine], learners: seq[machine]);
event eConfig: tConfig;

type tP1A = (proposer: Proposer, round: tRound, rp: tRound);
event eP1A: tP1A;

type tP1B = (acceptor: machine, round: tRound, maxr: tRound, v: tValue);
event eP1B: tP1B;
type tP2A = (proposer: machine, round: tRound, completed: tRound, value: tValue);
event eP2A: tP2A;
type tP2B = (acceptor: machine, round: tRound, value: tValue);
event eP2B: tP2B;
type tDecided = (round: tRound, value: tValue);
event eDecided: tDecided;

type tRoundComplete = (round: tRound);
event eRoundCompleteOnDecide: tRoundComplete;
event eRoundCompleteOnPropose: tRoundComplete;
event eReconfig;

type tPaxosConfig = (quorum: int);
event ePaxosConfig: tPaxosConfig;

type tPromised = (prev_round: tRound);
event ePromised: tPromised;

// forall e0: ePropose. exists e1: ePromise. e0.promise = e1.promise && e0.completed <= e1.joined < e0.round