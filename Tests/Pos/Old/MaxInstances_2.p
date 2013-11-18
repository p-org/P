event E1 assert 1;
event E2:int assert 1;
event E3 assume 1;
event E4;
event unit assert 1;

main machine Real {
    ghost var ghost_machine: mid;
    var test: bool;
    start state Real_Init {
		on E2 do Action1;
        entry {
			ghost_machine = new Ghost(real_machine = this);  
        	raise(unit);   
        }
        
        exit {
			test = true;
        }
		on unit push Real_S1;
		on E4 goto Real_S2;
    }

    state Real_S1 {
	entry {
            send(ghost_machine, E1);
	}
    }

    state Real_S2 {
		entry {
			raise(unit);
		}
		on unit goto Real_S3;
    }

	state Real_S3 {
		entry {}
		on E4 goto Real_S3;
	}
    action Action1 {
		assert(payload == 100);
		send(ghost_machine, E3);
        send(ghost_machine, E3);
    }
 
}

ghost machine Ghost {
    var real_machine: id;
    start state Ghost_Init {
        entry {
        }
        on E1 goto Ghost_S1;
    }

    state Ghost_S1 {
		ignore E1;
        entry {
			send(real_machine, E2, 100);	
        }
        on E3 goto Ghost_S2;
    }

    state Ghost_S2 {
        entry {
	    send(real_machine, E4);
		send(real_machine, E4);
		send(real_machine, E4);
        }
		
		on E3 goto Ghost_Init;
    }
}
