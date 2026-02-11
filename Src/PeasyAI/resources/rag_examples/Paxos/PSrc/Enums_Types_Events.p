// Enums
enum tProposalStatus { PENDING, PROMISED, ACCEPTED, REJECTED }

// Types
type tProposal = (proposer: machine, proposalNumber: int, value: int);
type tPromise = (proposer: machine, highestProposalNumber: int, acceptedValue: int, hasAcceptedValue: bool);
type tAcceptRequest = (proposer: machine, proposalNumber: int, value: int);
type tAccepted = (learner: machine, proposalNumber: int, value: int);
type tLearn = (allComponents: machine, finalValue: int);
type tProposeRequest = (client: machine, value: int);

// Protocol events
event ePropose: tProposal;
event ePromise: tPromise;
event eAcceptRequest: tAcceptRequest;
event eAccepted: tAccepted;
event eLearn: tLearn;
event eProposeRequest: tProposeRequest;
event eClientRequest: int;

// BEST PRACTICE: Use dedicated setup/configuration events for post-creation
// initialization. This solves circular dependency problems where machine A
// needs machine B's reference at creation time, but B also needs A.
//
// Pattern: Create machine A -> Create machine B(A) -> send A, eSetupEvent, B
//
// This is especially useful for:
// 1. Broadcast lists (e.g., Learner's allComponents)
// 2. Back-references (e.g., Timer needing its owner)
// 3. Ring topologies (each node needs its neighbor)
event eSetupLearnerComponents: seq[machine];
