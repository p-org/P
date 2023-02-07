// original
// checks that all events are handled correctly and also the local assertions in the P machines.
test tcSingleClientNoFailure [main = SingleClientNoFailure]:
  union TwoPhaseCommit, TwoPCClient, FailureInjector, { SingleClientNoFailure };

// asserts the liveness monitor along with the default properties
test tcMultipleClientsNoFailure [main = MultipleClientsNoFailure]:
  assert AtomicityInvariant, Progress in
    (union TwoPhaseCommit, TwoPCClient, FailureInjector, { MultipleClientsNoFailure });

//// asserts the liveness monitor along with the default properties
//test tcMultipleClientsWithFailure [main = MultipleClientsWithFailure]:
//  assert Progress in (union TwoPhaseCommit, TwoPCClient, FailureInjector, { MultipleClientsWithFailure });


// more data
// checks that all events are handled correctly and also the local assertions in the P machines.
test tcSingleClientNoFailureMoreData [main = SingleClientNoFailureMoreData]:
  union TwoPhaseCommit, TwoPCClient, FailureInjector, { SingleClientNoFailureMoreData };

// asserts the liveness monitor along with the default properties
test tcMultipleClientsNoFailureMoreData [main = MultipleClientsNoFailureMoreData]:
  assert AtomicityInvariant, Progress in
    (union TwoPhaseCommit, TwoPCClient, FailureInjector, { MultipleClientsNoFailureMoreData });

//// asserts the liveness monitor along with the default properties
//test tcMultipleClientsWithFailureMoreData [main = MultipleClientsWithFailureMoreData]:
//  assert Progress in (union TwoPhaseCommit, TwoPCClient, FailureInjector, { MultipleClientsWithFailureMoreData });


// more participants
// checks that all events are handled correctly and also the local assertions in the P machines.
test tcSingleClientNoFailureMoreParticipants [main = SingleClientNoFailureMoreParticipants]:
  union TwoPhaseCommit, TwoPCClient, FailureInjector, { SingleClientNoFailureMoreParticipants };

// asserts the liveness monitor along with the default properties
test tcMultipleClientsNoFailureMoreParticipants [main = MultipleClientsNoFailureMoreParticipants]:
  assert AtomicityInvariant, Progress in
    (union TwoPhaseCommit, TwoPCClient, FailureInjector, { MultipleClientsNoFailureMoreParticipants });

//// asserts the liveness monitor along with the default properties
//test tcMultipleClientsWithFailureMoreParticipants [main = MultipleClientsWithFailureMoreParticipants]:
//  assert Progress in (union TwoPhaseCommit, TwoPCClient, FailureInjector, { MultipleClientsWithFailureMoreParticipants });
