// This sample XYZs that payload works correctly with a push transition

event E1 assert 1;
event E2 assert 1 : int ;
event E3 assert 1;
event E4 assert 1;
event unit assert 1;

machine Main {
    var ghost_machine: machine;
    start state Real_Init {
        entry {
			ghost_machine = new Ghost(this);
            raise unit;	
        }
        on E2 do (payload: int) { Action1(payload); }
		on unit goto Real_S1;
        on E4 goto Real_S2;
    }

    state Real_S1 {

	entry {
            send ghost_machine, E1;
	
		}
    }

    state Real_S2 {
	entry {
		//this assert is reachable:
	    assert(false);
	}
    }

    fun Action1(payload: int) {
		assert(payload == 100);  //this assert passes
        send ghost_machine, E3;
    }

}

machine Ghost {
    var real_machine: machine;
    start state Ghost_Init {
        entry (payload: machine) {
	      real_machine = payload;
        }
        on E1 goto Ghost_S1;
    }

    state Ghost_S1 {
        entry {
			send real_machine, E2, 100;
        }
        on E3 goto Ghost_S2;
    }

    state Ghost_S2 {
        entry {
	    send real_machine, E4;
        }
    }
}
