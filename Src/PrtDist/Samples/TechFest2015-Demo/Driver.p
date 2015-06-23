include "FailureDetector.p"
include "PrtDistHelp.p"

main machine Driver {
    var fd: machine;
	var nodeseq: seq[machine];
    var nodemap: map[machine, bool];
	var i: int;
	var n: machine;
	var container: machine;
    start state Init {
	    entry {
			new Safety();
			i = 0;
			while (i < 2) {
				container = _CREATECONTAINER(null);
				createMachine_param = (container = container, machineType = 1, param = null);
				push CreateMachine;
				n = createMachine_return;
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

	var createMachine_param: (container: machine, machineType:int, param:any);
	var createMachine_return:machine;
	state CreateMachine {
		entry {
			_SENDRELIABLE(createMachine_param.container, Req_CreateMachine, 
			              (creator = this, machineType = createMachine_param.machineType, param = createMachine_param.param));
		}
        on Resp_CreateMachine do PopState;
	}

	fun PopState() {
		createMachine_return = payload;
		pop;
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
