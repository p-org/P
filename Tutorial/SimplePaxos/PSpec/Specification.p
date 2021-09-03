/******************************************
If mulitple proposers are proposing values simultaneously
then some value eventually gets accepted

Note: Paxos does not satisfy this property by default.
*******************************************/

spec LivenessMonitor observes prepare, accepted {
  start state Init {
    on prepare goto ProposedState;
  }

  hot state ProposedState {
    ignore prepare;
    on accepted goto AcceptedState;
  }

  cold state AcceptedState {
    ignore accepted;
    on prepare goto ProposedState;
  }
}