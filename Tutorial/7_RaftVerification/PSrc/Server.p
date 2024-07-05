// Based on "In Search of an Understandable Consensus Algorithm" by Diego Ongaro and John Ousterhout

// RequestVoteRPC: Invoked by candidates to gather votes (§5.2).
type tRequestVoteArguments = (term: int, candidateId: machine, lastLogIndex: int, lastLogTerm: int);
    // term             candidate’s term
    // candidateId      candidate requesting vote
    // lastLogIndex     index of candidate’s last log entry (§5.4)
    // lastLogTerm      term of candidate’s last log entry (§5.4)
type tRequestVoteResults = (term: int, voteGranted: bool);
    // term             currentTerm, for candidate to update itself
    // voteGranted      true means candidate received vote
event eInvokeRequestVote: tRequestVoteArguments;
event eRespondRequestVote: tRequestVoteResults;

// AppendEntry RPC: Invoked by leader to replicate log entries (§5.3); also used as heartbeat (§5.2).
type tAppendEntryArguments = (term: int, leaderId: machine , prevLogIndex: int, prevLogTerm: int, item: tLogEntry, leaderCommit: int);
    // term             leader’s term
    // leaderId         so follower can redirect clients
    // prevLogIndex     index of log entry immediately preceding new ones
    // prevLogTerm      term of prevLogIndex entry
    // item             log entry to store (-1 term for heartbeat)
    // leaderCommit     leader’s commitIndex
type tAppendEntryResults = (term: int, success: bool);
    // term             currentTerm, for leader to update itself
    // success          true if follower contained entry matching prevLogIndex and prevLogTerm
event eInvokeAppendEntry: tAppendEntryArguments;
event eRespondAppendEntry: tAppendEntryResults;

type tLogEntry = (term: int, command: int);

machine Server
{
    // Persistent state on all servers: (Updated on stable storage before responding to RPCs)
    var currentTerm: int; // latest term server has seen (initialized to 0 on first boot, increases monotonically)
    var votedFor: map[int, machine]; // candidateId that received vote per term
    var log: map[int, tLogEntry]; // log entries; each entry contains command for state machine, and term when entry was received by leader (first index is 1)
    
    // Volatile state on all servers:
    var commitIndex: int; // index of highest log entry known to be committed (initialized to 0, increases monotonically)
    var lastApplied: int; // index of highest log entry applied to state machine (initialized to 0, increases monotonically)
    
    // Volatile state on leaders: (Reinitialized after election)
    var nextIndex: map[machine, int]; // for each server, index of the next log entry to send to that server (initialized to leader last log index + 1)
    var matchIndex: map[machine, int]; // for each server, index of highest log entry known to be replicated on server (initialized to 0, increases monotonically)
    
    start state Init {
        entry {
            currentTerm = 0;
            commitIndex = 0;
            lastApplied = 0;
            // TODO: how to set the maps to empty?
            goto Follower;
        }
    }

    state Follower {
        entry {
            // instead of waiting for a timer, maybe call an election
            if ($) {
                goto Candidate;
            }
        }
        
        on eInvokeAppendEntry do (args: tAppendEntryArguments) {
            // 1. Reply false if term < currentTerm (§5.1)
            if (args.term < currentTerm) {
                send args.leaderId, eRespondAppendEntry, (term = currentTerm, success = false);
            }
            // 2. Reply false if log doesn’t contain an entry at prevLogIndex whose term matches prevLogTerm (§5.3)
            if (!(args.prevLogIndex in log) || log[args.prevLogIndex].term != args.prevLogTerm) {
                send args.leaderId, eRespondAppendEntry, (term = currentTerm, success = false);
            }
            // 3. If an existing entry conflicts with a new one (same index but different terms), delete the existing entry and all that follow it (§5.3)
            if () {
                
            }
            // 4. Append any new entries not already in the log
            log += (args.leaderCommit, args.item);
            // 5. If leaderCommit > commitIndex, set commitIndex = min(leaderCommit, index of last new entry)
            if (args.leaderCommit > commitIndex) {
                commitIndex = args.leaderCommit;
            }
        }
    }
    
    state Candidate {
    
    }
    
    state Leader {
        entry {
            // todo set the leader maps
        }
    
    }
}