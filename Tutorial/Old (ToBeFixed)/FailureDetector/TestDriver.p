//include "FailureDetector.p"

//Main machine:
machine Driver {
    var fd: machine;
    var nodeseq: seq[machine];
    var nodemap: map[machine, bool];
    
    start state Init {
        entry {
            Init(0, null);
            announce M_START, nodemap;
            fd = new FailureDetector(nodeseq);
            send fd, REGISTER_CLIENT, this;
            Fail(0);
        }
        ignore NODE_DOWN;
    }
    
    fun Init(i: int, n: machine) {
        i = 0;
        while (i < 2) {
            n = new Node();
            nodeseq += (i, n);
            nodemap[n] = true;
            i = i + 1;
        }
    }
    
    fun Fail(i: int) {
        i = 0;
        while (i < 2) {
            send nodeseq[i], halt;
            i = i + 1;
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
    
	  state Done { }
}