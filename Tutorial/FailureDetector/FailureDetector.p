//include "Timer.p"

// request from failure detector to node 
event PING: machine;
// response from node to failure detector
event PONG: machine;
// register a client for failure notification 
event REGISTER_CLIENT: machine;
// unregister a client from failure notification
event UNREGISTER_CLIENT: machine;
// failure notification to client
event NODE_DOWN: machine;
// local events for control transfer within failure detector
event UNIT;
event ROUND_DONE;
event TIMER_CANCELED;

machine FailureDetector {
    var nodes: seq[machine];            // nodes to be monitored
    var clients: map[machine, bool];    // registered clients
    var attempts: int;                  // number of PING attempts made
    var alive: map[machine, bool];      // set of alive nodes
    var responses: map[machine, bool];  // collected responses in one round
    var timer: machine;

    start state Init {
        entry (payload: seq[machine]) {
            nodes = payload;
            InitializeAliveSet();       // initialize alive to the members of nodes 
            timer = new Timer(this);
            raise UNIT;
        }
        on REGISTER_CLIENT do (payload: machine) { clients[payload] = true; }
        on UNREGISTER_CLIENT do (payload: machine) { if (payload in clients) clients -= payload; }
        on UNIT push SendPing;
    }

    state SendPing {
        entry {
            SendPings();            // send PING events to machines that have not responded
            send timer, START, 100; // start timer for intra-round duration
        }
        on PONG do (payload: machine) {
            // collect PONG responses from alive machines
            if (payload in alive) {
                responses[payload] = true;
                if (sizeof(responses) == sizeof(alive)) {
                    // status of alive nodes has not changed
                    send timer, CANCEL;
                    raise TIMER_CANCELED;
                }
            }
        }
        on TIMER_CANCELED push WaitForCancelResponse;
        on TIMEOUT do {
            // one attempt is done
            attempts = attempts + 1;
            // maximum number of attempts per round == 2
            if (sizeof(responses) < sizeof(alive) && attempts < 2) {
                //raise UNIT;     // try again by re-entering SendPing
				goto SendPing;
            } else {
                Notify();       // send any failure notifications
                raise ROUND_DONE;
            }
        }
        //on UNIT goto SendPing;
        on ROUND_DONE goto Reset;
    }

    state WaitForCancelResponse {
        defer TIMEOUT, PONG;
        on CANCEL_SUCCESS do { raise ROUND_DONE; }
        on CANCEL_FAILURE do { pop; }
    }

    state Reset {
        entry {
            // prepare for the next round
            attempts = 0;
            responses = default(map[machine, bool]);
            send timer, START, 1000;  // start timer for inter-round duration
        }
        on TIMEOUT goto SendPing;
        ignore PONG;
    }

    fun InitializeAliveSet() {
        var i: int;
        i = 0;
        while (i < sizeof(nodes)) {
            //alive += (nodes[i], true);
			alive[nodes[i]] = true;
            i = i + 1;
        }
    }

    fun SendPings() {
        var i: int;
        i = 0;
        while (i < sizeof(nodes)) {
            if (nodes[i] in alive && !(nodes[i] in responses)) {
                announce M_PING, nodes[i];
                send nodes[i], PING, this;
            }
            i = i + 1;
        }
    }

    fun Notify() {
        var i, j: int;
        i = 0;
        while (i < sizeof(nodes)) {
            if (nodes[i] in alive && !(nodes[i] in responses)) {
                alive -= nodes[i];
                j = 0;
                while (j < sizeof(clients)) {
                    send keys(clients)[j], NODE_DOWN, nodes[i];
                    j = j + 1;
                }
            }
            i = i + 1;
        }
    }
}

machine Node {
    start state WaitPing {
        on PING do (payload: machine) {
            send payload, PONG, this;
        }
    }
}

event M_PING: machine;

spec Safety observes M_PING, PONG {
    var pending: map[machine, int];
    
    start state Init {
        on M_PING do (payload: machine) {
            if (!(payload in pending))
                pending[payload] = 0;
            pending[payload] = pending[payload] + 1;
            assert (pending[payload] <= 3);
        }
        on PONG do (payload: machine) {
            assert (payload in pending);
            assert (0 < pending[payload]);
            pending[payload] = pending[payload] - 1;
        }
    }
}