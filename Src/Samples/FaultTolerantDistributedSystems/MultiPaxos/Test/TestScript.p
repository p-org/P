module LeaderElectionAbs { LeaderElectionAbsMachine }
module LeaderElection { LeaderElectionMachine, Timer }
module MultiPaxostoLEAbs 
{ MultiPaxosLEAbsMachine }

module LeaderElectionImpClosed = (rename MultiPaxosLEAbsMachine to Main in (compose MultiPaxostoLEAbs, LeaderElection));

module LeaderElectionAbsClosed = (rename MultiPaxosLEAbsMachine to Main in (compose MultiPaxostoLEAbs, LeaderElectionAbs));

test Test0: LeaderElectionImpClosed refines LeaderElectionAbsClosed;

