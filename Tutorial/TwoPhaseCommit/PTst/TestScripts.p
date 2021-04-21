// checks that all events are handled correctly and also the local assertions in the P machines.
test Test0[main = TestDriver0]: { TestDriver0, Coordinator, Participant, Timer, Client };

// asserts the liveness monitor along with the default properties
test Test1[main = TestDriver0]: assert AtomicityInvariant, Progress in { TestDriver0, Coordinator, Participant, Timer, Client };

// asserts the liveness monitor along with the default properties
test Test2[main = TestDriver1]: assert Progress in { TestDriver1, Coordinator, Participant, Timer, Client, FailureInjector };