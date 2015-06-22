include "FailureDetector.p"

main machine Driver {
    var fd: machine;
	var nodeseq: seq[machine];
    var nodemap: map[machine, bool];
	var i: int;
	var n: machine;
    start state Init {
	    entry {
			new Safety();
			i = 0;
			while (i < 2) {
			    n = new Node();
				nodeseq += (i, n);
				nodemap += (n, true);
				i = i + 1;
			}
			new Liveness(nodemap);
			fd = new FailureDetector(nodeseq);
			send fd, REGISTER_CLIENT, this;
			i = 0;
			while (i < 2) {
				send nodeseq[i], halt;
				i = i + 1;
			}
		}
		on NODE_DOWN do {
			monitor Liveness, NODE_DOWN, payload;
		};
	}
}

event M_PING: machine;
event M_PONG: machine;
monitor Safety {
	var pending: map[machine, int];
    start state Init {
	    on M_PING do { 
			if (!(payload in pending))
				pending[payload] = 0;
			pending[payload] = pending[payload] + 1;
			assert (pending[payload] <= 3);
		};
		on M_PONG do { 
			assert (payload in pending);
			assert (0 < pending[payload]);
			pending[payload] = pending[payload] - 1;
		};
	}
}

monitor Liveness {
	var nodes: map[machine, bool];
	start hot state Init {
		entry {
			nodes = payload as map[machine, bool]; 
		}
		on NODE_DOWN do { 
			nodes -= payload;
			if (sizeof(nodes) == 0) 
				raise UNIT;
		};
		on UNIT goto Done;
	}
	state Done {
	}
}
