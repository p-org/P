// This sample XYZs the semantics of push transitions: not deferral-by-default
// (as for push statements) and inheritance of actions
// compare to Actions_2.p (push statement case)

event E1 assert 1;
event E2 assert 1;
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
		//actions are inherited by the pushed state:
        on E2 do Action1;
		on unit goto Real_S1;
		//transitions are not inherited:
        on E4 goto Real_S2;
    }

    state Real_S1 {

	entry {
            send ghost_machine, E1;	
			//we wait in this state until E2 comes from Ghost,
			//then handle E2 using the inherited handler Action1
			//installed by Real_Init
			//then wait until E4 comes from Ghost, and since
			//there's no handler for E4 in this pushed state,
			//this state is popped, and E4 "goto" handler from Real_Init
			//is invoked
		}
    }

    state Real_S2 {
	entry {
		//this assert is reachable:
	    assert(false);
	}
    }

    fun Action1() {
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
			send real_machine, E2;
        }
        on E3 goto Ghost_S2;
    }

    state Ghost_S2 {
        entry {
	    send real_machine, E4;
        }
    }
}
