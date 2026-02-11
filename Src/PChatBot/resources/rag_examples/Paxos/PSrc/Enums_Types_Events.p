// Enums
enum tProposalStatus { PENDING, PROMISED, ACCEPTED, REJECTED }

// Types
type tProposal = (proposer: machine, proposalNumber: int, value: int);
type tPromise = (proposer: machine, highestProposalNumber: int, acceptedValue: int, hasAcceptedValue: bool);
type tAcceptRequest = (proposer: machine, proposalNumber: int, value: int);
type tAccepted = (learner: machine, proposalNumber: int, value: int);
type tLearn = (allComponents: machine, finalValue: int);
type tProposeRequest = (client: machine, value: int);

// Events
event ePropose: tProposal;
event ePromise: tPromise;
event eAcceptRequest: tAcceptRequest;
event eAccepted: tAccepted;
event eLearn: tLearn;
event eProposeRequest: tProposeRequest;
event eClientRequest: int;