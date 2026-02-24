// Types for machine configurations
type tProposerConfig = (acceptors: seq[machine], learners: seq[machine], proposerId: int, valueToPropose: int);
type tAcceptorConfig = (learners: seq[machine]);
type tLearnerConfig = (majoritySize: int);
type tClientConfig = (proposer: machine, valueToPropose: int);

// Types for event payloads
type tProposePayload = (proposer: machine, proposalNumber: int, proposedValue: int);
type tPromisePayload = (proposer: machine, highestProposalSeen: int, acceptedValue: int, acceptedProposal: int);
type tAcceptRequestPayload = (proposer: machine, proposalNumber: int, proposedValue: int);
type tAcceptedPayload = (proposalNumber: int, acceptedValue: int);
type tLearnPayload = (learnedValue: int);

// Events
event ePropose: tProposePayload;
event ePromise: tPromisePayload;
event eAcceptRequest: tAcceptRequestPayload;
event eAccepted: tAcceptedPayload;
event eLearn: tLearnPayload;
event eStartConsensus: int;