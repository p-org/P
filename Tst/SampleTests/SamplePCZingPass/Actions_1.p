event E1 assert 1;
event E2 assert 1;
event E3 assert 1;
event E4 assert 1;
event unit assert 1;

main machine Real {
    var ghost_machine: model;
    var test: bool;
    start state Real_Init {
        entry {
			ghost_machine = new Ghost(this);  
            send ghost_machine, E1;	     
        }
        on E4 do Action1;
        on E2 goto Real_S1;
        exit {
	    test = true;
        }
    }

    state Real_S1 {
    
		entry {
			assert(true);    
			raise unit;
		}
		on unit goto Real_S2;
    }

    state Real_S2 {
	entry {
        
	    assert(true);
	}
    }

    fun Action1() {
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
