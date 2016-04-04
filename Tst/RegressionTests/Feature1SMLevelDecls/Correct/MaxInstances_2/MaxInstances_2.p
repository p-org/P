// This sample tests assert/assume annotations on events
event E1 assert 1;
event E2 assert 1 :int;
event E3 assume 1;
event E4;
event unit assert 1;

main machine Real {
    var ghost_machine: machine;
    start state Real_Init {
		on E2 do (payload: int) { Action1(payload); }
        entry {
			ghost_machine = new Ghost(this);  
        	raise unit;   
        }
        
		on unit push Real_S1;
		on E4 goto Real_S2;
    }

    state Real_S1 {
	entry {
            send ghost_machine, E1;
	}
    }

    state Real_S2 {
		entry {
			raise unit;
		}
		on unit goto Real_S3;
    }

	state Real_S3 {
		entry {}
		on E4 goto Real_S3;
	}
   fun Action1(payload: int) {
		assert(payload == 100);
		send ghost_machine, E3;
        send ghost_machine, E3;
    }
 
}

model Ghost {
    var real_machine: machine;
    start state _Init {
	entry (payload: machine) { real_machine = payload; raise unit; }
        on unit goto Ghost_Init;
    }

    state Ghost_Init {
        entry {
        }
        on E1 goto Ghost_S1;
    }

    state Ghost_S1 {
		ignore E1;
        entry {
			send real_machine, E2, 100;	
        }
        on E3 goto Ghost_S2;
    }

    state Ghost_S2 {
        entry {
	    send real_machine, E4;
		send real_machine, E4;
		send real_machine, E4;
        }
		on E3 goto Ghost_Init;
    }
}
