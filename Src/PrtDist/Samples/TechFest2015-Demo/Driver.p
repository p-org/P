include "FailureDetector.p"
include "PrtDistHelp.p"

static fun CreateNode(container: machine) : machine
[container = container]
{
	var newMachine: machine;
	newMachine = new Node();
	return newMachine;
}

main machine Driver {
    var fd: machine;
	var nodeseq: seq[machine];
    var nodemap: map[machine, bool];
	var container: machine;
	var i: int;
	var n: machine;
    start state Init {
	    entry {
			new Safety();
			i = 0;
			while (i < 2) {
				container = _CREATECONTAINER();
				n = CreateNode(container);
				nodeseq += (i, n);
				nodemap += (n, true);
				i = i + 1;
			}
			new Liveness(nodemap);
			fd = new FailureDetector(nodeseq);
			send fd, REGISTER_CLIENT, this;
			i = 0;
			while (i < 2) {
				_SEND(nodeseq[i], halt, null);
				i = i + 1;
			}
		}
		ignore NODE_DOWN;
	}
}

event M_PING: machine;
event M_PONG: machine;
spec Safety monitors M_PING, M_PONG {
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

spec Liveness monitors NODE_DOWN {
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
