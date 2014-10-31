// This sample tests the deferred-by-default semantics of push statement
// and inheritance of actions by push statement

event E1 assert 1;
event E2 assert 1;
event E3 assert 1;
event E4 assert 1;
event unit assert 1;

main machine Real {
    var ghost_machine: machine;
    start state Real_Init {
        entry {
		ghost_machine = new Ghost(this);  
			//everything is deferred by default:
        	push Real_S1;   
        }
		//actions are inherited:
        on E2 do Action1;
		//transitions are not inherited:
        on E4 goto Real_S2;
    }

    state Real_S1 {
    // In this state, E2 may be dequeued but not E4
	entry {
            send ghost_machine, E1;
			//when there's only E4 in the queue, deadlock happens,
			//since there's no handler for E4, and no "pop" -
			//compare to Actions_5.p (push transition case)
	}
    }

    state Real_S2 {
	entry {
		//this state is unreachable:
	    assert(false);
	}
    }

    fun Action1() {
		//this state is reachable:
		//assert(false);
        send ghost_machine, E3;
    }
 
}

model Ghost {
    var real_machine: machine;
    start state Ghost_Init {
        entry {
	      real_machine = payload as machine;
        }
        on E1 goto Ghost_S1;
    }

    state Ghost_S1 {
        entry {
			send real_machine, E2;
        }
        on E3 goto Ghost_S2;
    }

    state Ghost_S2 {
        entry {
	    send real_machine, E4;
		//pop;
        }
    }
}
