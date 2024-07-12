enum Vote {YES, NO}
type tVoteResp = (source: machine, vote: Vote);

event eVoteReq;
event eVoteResp: tVoteResp;
event eAbort;
event eCommit;

machine Coordinator
{
    var yesVotes: set[machine];
    
    start state Undecided {
        entry {
            var p: machine;
            foreach (p in participants()) {
                send p, eVoteReq;
            }
        }
    
        on eVoteResp do (resp: tVoteResp) {
            var p: machine;
            
            if (resp.vote == YES) {
                yesVotes += (resp.source);
                
                if (yesVotes == participants()) {
                    foreach (p in participants()) {
                        send p, eCommit;
                    }
                    goto Committed;
                }
                
            } else {
                foreach (p in participants()) {
                    send p, eAbort;
                }
                goto Aborted;
            }
        }
    }
    
    state Committed {ignore eVoteResp;}
    state Aborted {ignore eVoteResp;}
}

machine Participant {
    start state Undecided {
        on eVoteReq do {
            if ($) {
                send coordinator(), eVoteResp, (source = this, vote = YES);
            } else {
                send coordinator(), eVoteResp, (source = this, vote = NO);
            }
        }
        
        on eCommit do {
            goto Committed;
        }
        
        on eAbort do {
            goto Aborted;
        }
    }
    
    state Committed {ignore eVoteReq, eCommit, eAbort;}
    state Aborted {ignore eVoteReq, eCommit, eAbort;}
}


// Using these to avoid initialization
pure participants(): set[machine];
pure coordinator(): machine;
// there is one machine that is a Coordinator and it is coordinator()
axiom forall (m: machine) :: m == coordinator() == m is Coordinator;
// every participant is in the set of participants
axiom forall (m: machine) :: m in participants() == m is Participant;


