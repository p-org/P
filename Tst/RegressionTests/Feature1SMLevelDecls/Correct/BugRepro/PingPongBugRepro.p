// This test found a bug in treatment of function parameters
// in the case when there is a scheduling point inside the function (such as a send),
// and the mutation performed before the scheduling point is used after it


event PING: machine;
event PONG: machine;

machine Main {
    var fd: machine;
	var nodeseq: seq[machine];
    var nodemap: map[machine, bool];
	var i: int;
	var n: machine;
    start state Init {
	    entry {
			i = 0;
			while (i < 2) {
			    n = new Node();
				nodeseq += (i, n);
				nodemap[n] = true;
				i = i + 1;
			}
			announce M_START, nodemap;
			fd = new FailureDetector(nodeseq);
			send fd, REGISTER_CLIENT, this;
			i = 0;
			while (i < 2) {
				send nodeseq[i], halt;
				i = i + 1;
			}
		}
		ignore NODE_DOWN;
	}
}

event ROUND_DONE;
event REGISTER_CLIENT: machine;
event UNREGISTER_CLIENT: machine;
event NODE_DOWN: machine;
event TIMER_CANCELED;

machine FailureDetector {
	var nodes: seq[machine];
    var clients: map[machine, bool];
	var attempts: int;
	var alive: map[machine, bool];
	var responses: map[machine, bool];
    var timer: machine;
	
    start state Init {
        entry (payload: seq[machine]) {
  	        nodes = payload;
			InitializeAliveSet(0);
			timer = new Timer(this);
	        goto SendPing;   	
        }
		on REGISTER_CLIENT do (payload: machine) { clients[payload] = true; }
		on UNREGISTER_CLIENT do  (payload: machine) { if (payload in clients) clients -= payload; }
    }
    state SendPing {
        entry {
		    SendPingsToAliveSet(0);
			send timer, START, 100;
	    }
	    on REGISTER_CLIENT do (payload: machine) { clients[payload] = true; }
		on UNREGISTER_CLIENT do  (payload: machine) { if (payload in clients) clients -= payload; }
    	on PONG do (payload: machine) {
		    if (payload in alive) {
				 responses[payload] = true;
				 if (sizeof(responses) == sizeof(alive)) {
			         send timer, CANCEL;
					 raise TIMER_CANCELED;
			     }
			}
		}
		on TIMEOUT do (payload: machine) {
			attempts = attempts + 1;
		    if (sizeof(responses) < sizeof(alive) && attempts < 2) {
				raise UNIT;
			}
			Notify(1, 0);
			raise ROUND_DONE;
		}
		on ROUND_DONE goto Reset;
		on UNIT goto SendPing;
		on TIMER_CANCELED goto WaitForCancelResponse;
     }
	 state WaitForCancelResponse {
	     defer TIMEOUT, PONG;
	     on CANCEL_SUCCESS, CANCEL_FAILURE do (payload: machine) { goto Reset; }
	     on REGISTER_CLIENT do (payload: machine) { clients[payload] = true; }
		on UNREGISTER_CLIENT do  (payload: machine) { if (payload in clients) clients -= payload; }
	 }
	 state Reset {
         entry {
			 attempts = 0;
			 responses = default(map[machine, bool]);
			 send timer, START, 1000;
		 }
		 on TIMEOUT goto SendPing;
		 on REGISTER_CLIENT do (payload: machine) { clients[payload] = true; }
		on UNREGISTER_CLIENT do  (payload: machine) { if (payload in clients) clients -= payload; }
		 ignore PONG;
	 }

	 fun InitializeAliveSet(i: int) {
		i = 0;
		while (i < sizeof(nodes)) {
			alive[nodes[i]] = true;
			i = i + 1;
		}
	 }
	 fun SendPingsToAliveSet(i: int) {
		i = 0;
		while (i < sizeof(nodes)) {
		    if (nodes[i] in alive && !(nodes[i] in responses)) {
				announce M_PING, nodes[i];
				send nodes[i], PING, this;
			}
		    i = i + 1;
		}
	 }
	fun Notify(i: int, j: int) {
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
			announce M_PONG, this;
		    send payload as machine, PONG, this;
		}
    }
}

event M_PING: machine;
event M_PONG: machine;
spec Safety observes M_PING, M_PONG {
	var pending: map[machine, int];
    start state Init {
	    on M_PING do (payload: machine) {
			if (!(payload in pending))
				pending[payload] = 0;
			pending[payload] = pending[payload] + 1;
			assert (pending[payload] <= 3);   //fails
		}
		on M_PONG do (payload: machine) {
			assert (payload in pending);
			assert (0 < pending[payload]);
			pending[payload] = pending[payload] - 1;
		}
	}
}

event M_START: map[machine, bool];
spec Liveness observes M_START, NODE_DOWN {
	var nodes: map[machine, bool];
	start state Init {
		on M_START goto Wait;
	}
	hot state Wait {
		entry (payload: map[machine, bool]) {
			nodes = payload;
		}
		on NODE_DOWN do (payload: machine) {
			nodes -= payload;
			if (sizeof(nodes) == 0)
				raise UNIT;
		}
		on UNIT goto Done;
	}
	state Done {
	}
}
