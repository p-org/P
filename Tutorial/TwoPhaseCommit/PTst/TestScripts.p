// checks that all events are handled correctly and also the local assertions in the P machines.
test Test0[main = TestDriverNoFailure]: union TwoPhaseCommit, ClientAndFailureInjector, { TestDriverNoFailure };

// asserts the liveness monitor along with the default properties
test Test1[main = TestDriverNoFailure]: assert AtomicityInvariant, Progress in (union TwoPhaseCommit, ClientAndFailureInjector, { TestDriverNoFailure });

// asserts the liveness monitor along with the default properties
test Test2[main = TestDriverWithFailure]: assert Progress in (union TwoPhaseCommit, ClientAndFailureInjector, { TestDriverWithFailure });