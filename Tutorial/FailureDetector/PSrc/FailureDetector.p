// request from failure detector to node
event ePing: (fd: FailureDetector, trial: int);
// response from node to failure detector
event ePong: (node: Node, trial: int);
// failure notification to client
event eNodeDown: Node;

machine FailureDetector {
    var nodes: set[Node];            // nodes to be monitored
    var clients: set[Client];       // registered clients
    var attempts: int;              // number of ping attempts made
    var alive: set[Node];           // set of alive nodes
    var respInCurrRound: set[machine];  // collected responses in one round
    var timer: Timer;

    start state Init {
        entry (payload: set[Node]) {
            nodes = payload;
            alive = nodes;       // initialize alive to the members of nodes
            timer = CreateTimer(this);
            goto SendPingsToAllNodes;
        }
    }

    state SendPingsToAllNodes {
        entry {
            var notRespondedNodes: set[Node];
            notRespondedNodes = PotentiallyDownNodes();            // send PING events to machines that have not responded
            BroadCast(notRespondedNodes, ePing, (fd =  this, trial = attempts));
            StartTimer(timer); // start timer for intra-round duration
        }
        on ePong do (pong: (node: Node, trial: int)) {
            // collect PONG responses from alive machines
            if (pong.node in alive) {
                respInCurrRound += (pong.node);
                if (sizeof(respInCurrRound) == sizeof(alive)) {
                    // status of alive nodes has not changed
                    CancelTimer(timer);
                }
            }
        }

        on eTimeOut do {
            var nodesDown: set[Node];
            // one attempt is done
            attempts = attempts + 1;
            // maximum number of attempts per round == 2
            if (sizeof(respInCurrRound) < sizeof(alive) && attempts < 2) {
                // try again by re-entering SendPing
				goto SendPingsToAllNodes;
            } else {
                nodesDown = ComputeFailedNodesAndUpdateAliveSet();
                // notify
                if(sizeof(nodesDown) > 0)
                    BroadCast(clients, eNodeDown, nodesDown);
                goto ResetAndStartAgain;
            }
        }
    }

    state ResetAndStartAgain {
        entry {
            // prepare for the next round
            attempts = 0;
            respInCurrRound = default(set[Node]);
            StartTimer(timer);  // start timer for inter-round duration
        }
        on eTimeOut goto SendPingsToAllNodes;
        ignore ePong;
    }



    fun PotentiallyDownNodes() : set[Node] {
        var i: int;
        var nodesNotResponded: set[Node];
        while (i < sizeof(nodes)) {
            if (nodes[i] in alive && !(nodes[i] in respInCurrRound)) {
                nodesNotResponded += (nodes[i]);
            }
            i = i + 1;
        }
        return nodesNotResponded;
    }

    fun ComputeFailedNodesAndUpdateAliveSet() : set[Node] {
        var i: int;
        var nodesDown: set[Node];
        while (i < sizeof(nodes)) {
            if (nodes[i] in alive && !(nodes[i] in respInCurrRound)) {
                alive -= (nodes[i]);
                nodesDown += (nodes[i]);
            }
            i = i + 1;
        }
        return nodesDown;
    }
}



