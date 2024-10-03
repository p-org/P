// hint exact SingleLeader (e1: eBecomeLeader, e2: eBecomeLeader) {
//     num_guards = 1;
//     exists = 0;
//     include_guards = e1.term == e2.term;
//     arity = 2;
// }

// hint exact TryExists (e1: eEntryApplied, e2: eEntryApplied) {
//     config_event = eRaftConfig;
//     exists = 1;
//     arity = 2;
//     num_guards = 1;
// }

// hint exact TestBool (e1: eRequestVoteReply) {
//     arity = 1;
//     num_guards = 0;
//     exists = 0;
// }