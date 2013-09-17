event Packet:(to:mid, ev:eid, pl:any);
event NextBallot:(n:int);
event LastVote:(n:int, v:any);
event Unit;

machine Network {
    start state init {
        entry { raise(Unit); }
        on Unit goto WaitForPacket;
    }

    state WaitForPacket {
        on Packet goto Forward;
    }

    state Forward {
        entry {
            send(payload.to, payload.ev, payload.pl);
        }

        on Unit goto WaitForPacket;
    }
}

machine Proposer {
    var proposedVal:any;
    var quorum:(mid, mid, mid);
    var network:mid;

    var n,i,j, lastN, numResp:int;
    var v:any;
    var lastVoteMsg:(n:int, v:any);

    start state init { }

    foreign fun NetSend(t:mid, e:eid, p:any) {
        send(network, Packet, (to=t, ev=e, pl=p));
    }

    state SendNextBallot {
        entry {
            numResp = 0;
            lastN = 0;
            NetSend(quorum[0], NextBallot, n + 1);
            NetSend(quorum[1], NextBallot, n + 1);
            NetSend(quorum[2], NextBallot, n + 1);
            raise(Unit);
        }
        on Unit goto WaitForLastVotes;
    }

    state WaitForLastVotes {
        entry {
            if (trigger == WaitForLastVotes) {
                lastVoteMsg = (n:int, v:any)payload;
                i = lastVoteMsg.n;
                v = lastVoteMsg.v;
                if (lastN < i) {
                    
                }
                // Finish this test using the numResp counter
                // to account for who voted. This should be suseptible to message duplication.
            }
        }

        on LastVote goto WaitForLastVotes;
    }

    state SendBeginBallot {
    }

    state WaitForVotes {
    }

    state Success {
    }

    state Retry {
    }
}

machine Acceptor {
    start state init { }
}

machine Learner {
    start state init { }
}

main ghost machine Env {
    start state init { }
}
