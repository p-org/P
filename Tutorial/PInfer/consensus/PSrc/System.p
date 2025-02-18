type tValue = int;

type tConfig = (quorum: int);
event eConfig: tConfig;

type tStart = (requestVoteFrom: machine);
event eStart: tStart;

type tRequest = (candidate: machine);
event eRequest: tRequest;

type tVote = (voter: machine, votedFor: machine);
event eVote: tVote;

type tDecide = (node: machine, value: tValue);
event eDecide: tDecide;

// ghost events
type tWon = (node: machine);
event eWon: tWon;

type tStateVoted = (voter: machine, votedFor: machine);
event eStateVoted: tStateVoted;


machine Node {
    var voted: bool;
    var value: tValue;
    var votes: set[machine];
    var quorum: int;
    
    start state Voting {
        entry {
            voted = false;
            value = choose(100);
            votes = default(set[machine]);
        }

        on eConfig do (c: tConfig) {
            quorum = c.quorum;
        }

        on eStart do (s: tStart) {
            send s.requestVoteFrom, eRequest, (candidate=this,);
        }

        on eRequest do (req: tRequest) {
            if (!voted) {
                voted = true;
                announce eStateVoted, (voter=this, votedFor=req.candidate);
                send req.candidate, eVote, (voter=this, votedFor=req.candidate);
            }
        }

        on eVote do (v: tVote) {
            votes += (v.voter);
            if (sizeof(votes) >= quorum) {
                announce eDecide, (node=this, value=value);
                announce eWon, (node=this,);
                goto Won;
            }
        }
    }

    state Won {
        ignore eConfig, eStart, eRequest, eVote;
    }
}

spec Safety observes eDecide {
    var decided: bool;
    var value: tValue;

    start state Monitoring {
        entry {
            decided = false;
            value = -1;
        }

        on eDecide do (r: tDecide) {
            if (decided) {
                assert(value == r.value);
            } else {
                decided = true;
                value = r.value;
            }
        }
    }
}

module ConsensusEPR = { Node };