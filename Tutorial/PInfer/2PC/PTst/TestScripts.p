// checks that all events are handled correctly and also the local assertions in the P machines.
test tcSingleClientNoFailure [main = SingleClientNoFailure]:
  union TwoPhaseCommit, TwoPCClient, FailureInjector, { SingleClientNoFailure };

// asserts the liveness monitor along with the default properties
test tcMultipleClientsNoFailure [main = MultipleClientsNoFailure]:
  assert AtomicityInvariant, Progress, Atomicity_1 in
    (union TwoPhaseCommit, TwoPCClient, FailureInjector, { MultipleClientsNoFailure });

// asserts the liveness monitor along with the default properties
test tcMultipleClientsWithFailure [main = MultipleClientsWithFailure]:
  assert Progress in (union TwoPhaseCommit, TwoPCClient, FailureInjector, { MultipleClientsWithFailure });

test tcC2P3T3 [main = C2P3T3]:
  assert AtomicityInvariant, Progress in
    (union TwoPhaseCommit, TwoPCClient, FailureInjector, { C2P3T3 });

test tcC3P4T3 [main = C3P4T3]:
  assert AtomicityInvariant, Progress in
    (union TwoPhaseCommit, TwoPCClient, FailureInjector, { C3P4T3 });

test tcC3P5T3 [main = C3P5T3]:
  assert AtomicityInvariant, Progress in
    (union TwoPhaseCommit, TwoPCClient, FailureInjector, { C3P5T3 });