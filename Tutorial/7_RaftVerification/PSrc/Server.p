type tRequestLeaderVote = (term: int);
type tRespondLeaderVote = (term: int, voter: machine);
type tAppendEntry       = (term: int, key: int, value: int);

event eRequestLeaderVote : tRequestLeaderVote;
event eRespondLeaderVote : tRespondLeaderVote;
event eAppendEntry       : tAppendEntry;

machine Server
{
    var currentTerm: int;
    var voteCount : set[machine];
    var log: map[int, int];
    var currentPosition: int;

    start state Follower {
        entry {
            // instead of waiting for a timer, maybe call an election
            if ($) {
                goto Candidate;
            }
        }
        
        on eAppendEntry do (msg: tAppendEntry) {
            log += (msg.key, msg.value);
        }
    }
    
    state Candidate {
        entry {
            currentTerm = currentTerm + 1;
            BroadcastRequestLeaderVote(currentTerm);
            voteCount = ZeroVotes();
        }
        
        on eRespondLeaderVote do (vote: tRespondLeaderVote) {
            var majority: bool;
            if (vote.term == currentTerm) {
                voteCount += (vote.voter);
            }
            majority = CheckMajority(voteCount);
            if (majority) {
                goto Leader;
            }
        }
        
        on eAppendEntry do (msg: tAppendEntry) {
            if (msg.term >= currentTerm) {
                currentTerm = msg.term;
                goto Follower;
            }
        }
    }
    
    state Leader {
        entry {
            BroadcastHeartBeat(currentTerm);
        }
        
        on eClientRequest do (req: tClientRequest) {
            log[currentPosition] = req.action;
            BroadcastAppendEntry(currentTerm, currentPosition, req.action);
            currentPosition = currentPosition + 1;
        }
    }
}

fun BroadcastRequestLeaderVote(term: int);
fun BroadcastAppendEntry(term: int, pos: int, act: int);
fun BroadcastHeartBeat(term: int);
fun CheckMajority(count: set[machine]): bool;
fun ZeroVotes(): set[machine];