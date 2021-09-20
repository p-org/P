// event: ping nodes (from failure detector to nodes)
event ePing: (fd: FailureDetector, trial: int);
// event: pong (response to ping) (from node to failure detector)
event ePong: (node: Node, trial: int);
// failure notification to the client (from failure detector to client)
event eNotifyNodesDown: set[Node];

/***************************************************
FailureDetector machine monitors whether a set of nodes in the system are alive (responsive), it
periodically sends ping message to each node and waits for a pong message from the nodes.
The nodes that do not send a pong message after multiple attempts are marked as down and notified to
the client nodes.
***************************************************/
machine FailureDetector {
    // set of nodes to be monitored
    var nodes: set[Node];
    // set of registered clients
    var clients: set[Client];
    // num of ping attempts made
    var attempts: int;
    // set of alive nodes
    var alive: set[Node];
    // nodes that have responded in the current round
    var respInCurrRound: set[machine];
    // timer to wait
    var timer: Timer;

    start state Init {
        entry (config: (nodes: set[Node], clients: set[Client])) {
            nodes = config.nodes;
            alive = config.nodes;
            clients = config.clients;
            timer = CreateTimer(this);
            goto SendPingsToAllNodes;
        }
    }

    state SendPingsToAllNodes {
        entry {
            var notRespondedNodes: set[Node];

            if(sizeof(alive) == 0)
                raise halt; // stop myself, there are no alive nodes!

            notRespondedNodes = PotentiallyDownNodes();
            // send ping events to machines that have not responded in the previous attempt
            UnReliableBroadCast(notRespondedNodes, ePing, (fd =  this, trial = attempts));
            // start wait timer
            StartTimer(timer);
        }

        on ePong do (pong: (node: Node, trial: int)) {
            // collect pong responses from alive machines
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
            // one more attempt is done
            attempts = attempts + 1;
            print format ("RespInCurr: {0} :: alive: {1}", respInCurrRound, alive);
            if (sizeof(respInCurrRound) < sizeof(alive) ) {
                // maximum number of attempts == 3
                if(attempts < 3) {
                    // try again by re-pinging the machines that have not responded
                    goto SendPingsToAllNodes;
				}
				else
				{
				    // inform clients about the nodes down
                    nodesDown = ComputeNodesDownAndUpdateAliveSet();
                    ReliableBroadCast(clients, eNotifyNodesDown, nodesDown);
				}
            }

            goto ResetAndStartAgain;
        }
    }

    state ResetAndStartAgain {
        entry {
            // prepare for the next round
            attempts = 0;
            respInCurrRound = default(set[Node]);
            // start timer for inter-round waiting
            StartTimer(timer);
        }
        on eTimeOut goto SendPingsToAllNodes;
        ignore ePong;
    }

    // compute the potentially down nodes
    // i.e., nodes that have not responded Pong in this round
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

    // compute the nodes that might be down and also update the alive set accordingly
    fun ComputeNodesDownAndUpdateAliveSet() : set[Node] {
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



