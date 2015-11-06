// This sample tests basic semantics of actions and goto transitions
event E1 assert 1;
event E2 assert 1;
event E3 assert 1;
event E4 assert 1;
event unit assert 1;

main machine Real {
    var ghost_machine: machine;
    var test: bool;
    start state Real_Init {
        entry {
			ghost_machine = new Ghost(this);  
            send ghost_machine, E1;	   
        }
		//next line can be commented out w/out changing the result:
        on E4 do Action1;   //E4, E3 have no effect on reachability of assert(false) 
        on E2 goto Real_S1; //exit actions are performed before transition to Real_S1
        exit {
	    test = true;
        }
    }

    state Real_S1 {
    
		entry {
			assert(test == true);  //holds
			raise unit;
		}
		on unit goto Real_S2;
    }

    state Real_S2 {
	entry {
        //this assert is reachable: Real -E1-> Ghost -E2-> Real; 
		//then Real_S1 (assert holds), Real_S2 (assert fails)
	    assert(false);  //this assert is reachable 
	}
    }

    fun Action1() {
        send ghost_machine, E3;
    }
 
}

model Ghost {
    var real_machine: machine;
    start state Ghost_Init {
        entry (payload: machine) {
	      real_machine = payload;
        }
        on E1 goto Ghost_S1;
    }

    state Ghost_S1 {
        entry {
			send real_machine, E4;
			send real_machine, E2;
        }
        on E3 goto Ghost_S2;
    }

    state Ghost_S2 {
        entry {
        }
    }
}
