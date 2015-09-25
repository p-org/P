// This sample tests assert annotations on events
// Error: number of instances greater than asserted
event E1 assert 1;
event E2 assert 1 :int;
event E3 assume 1;
event E4;
event unit assert 1;

interface I_Real E2, E4;

main machine Real implements I_Real {
    var ghost_machine: I_Ghost;
    start state Real_Init {
		on E2 do Action1;
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
			//error:
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
   fun Action1() {
		assert(payload == 100);
		send ghost_machine, E3;
        send ghost_machine, E3;
    }
 
}

interface I_Ghost E1, E2, E3;

model Ghost implements I_Ghost {
    var real_machine: I_Real;
    start state _Init {
	entry { real_machine = payload as I_Real; raise unit; }
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
