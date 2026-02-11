// the two phase commit module
module TwoPhaseCommit = union { Coordinator, Participant }, Timer;