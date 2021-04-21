//include "Timer.p"

// request from failure detector to node 
event ePing: (fd: FailureDetector, trial: int);
// response from node to failure detector
event ePong: (node: Node, trial: int);
// failure notification to client
event eNodeDown: Node;

machine FailureDetector {
    var nodes: seq[Node];            // nodes to be monitored
    var clients: seq[Client];       // registered clients
    var attempts: int;                  // number of PING attempts made
    var alive: set[Node];      // set of alive nodes
    var responsesInRound: set[machine];  // collected responses in one round
    var timer: Timer;

    start state Init {
        entry (payload: seq[Node]) {
            nodes = payload;
            InitializeAliveSet();       // initialize alive to the members of nodes 
            timer = CreateTimer(this);
            goto SendPings;
        }
    }

    state SendPings {
        entry {
            BroadCastPings();            // send PING events to machines that have not responded
            StartTimer(timer, 100); // start timer for intra-round duration
        }
        on ePong do (pong: (node: Node, trial: int)) {
            // collect PONG responses from alive machines
            if (pong.node in alive) {
                responsesInRound += (pong.node);
                if (sizeof(responsesInRound) == sizeof(alive)) {
                    // status of alive nodes has not changed
                    CancelTimer(timer);
                }
            }
        }

        on eTimeOut do {
            // one attempt is done
            attempts = attempts + 1;
            // maximum number of attempts per round == 2
            if (sizeof(responsesInRound) < sizeof(alive) && attempts < 2) {
                //raise UNIT;     // try again by re-entering SendPing
				goto SendPings;
            } else {
                Notify();       // send any failure notifications
                goto Reset;
            }
        }
    }

    state Reset {
        entry {
            // prepare for the next round
            attempts = 0;
            responsesInRound = default(set[Node]);
            StartTimer(timer, 1000);  // start timer for inter-round duration
        }
        on eTimeOut goto SendPings;
        ignore ePong;
    }

    fun InitializeAliveSet() {
        var i: int;
        while (i < sizeof(nodes)) {
            alive += (nodes[i]);
            i = i + 1;
        }
    }

    fun BroadCastPings() {
        var i: int;
        while (i < sizeof(nodes)) {
            if (nodes[i] in alive && !(nodes[i] in responsesInRound)) {
                send nodes[i], ePing, (fd = this, trial = attempts);
            }
            i = i + 1;
        }
    }

    fun Notify() {
        var i, j: int;
        i = 0;
        while (i < sizeof(nodes)) {
            if (nodes[i] in alive && !(nodes[i] in responsesInRound)) {
                alive -= nodes[i];
                j = 0;
                while (j < sizeof(clients)) {
                    send clients[j], eNodeDown, nodes[i];
                    j = j + 1;
                }
            }
            i = i + 1;
        }
    }
}



