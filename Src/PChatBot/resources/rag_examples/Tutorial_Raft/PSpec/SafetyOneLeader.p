type tBecomeLeader = (term:TermId, leader:Server, log: seq[tServerLog], commitIndex: LogIndex);
event eBecomeLeader: tBecomeLeader;

spec SafetyOneLeader observes eBecomeLeader{
    var termToLeader: map[int, Server];
    start state Init {
        entry {
            termToLeader = default(map[int, Server]);
        }
        on eBecomeLeader do (payload: (term:int, leader:Server, log: seq[tServerLog], commitIndex: int)) {
            if (payload.term in keys(termToLeader)) {
                assert termToLeader[payload.term] == payload.leader, format("At term {0} there are multiple leaders: {1} and {2}.", payload.term, termToLeader[payload.term], payload.leader);
            }
            termToLeader += (payload.term, payload.leader);
        }
    }
}