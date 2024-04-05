// P semantics XYZ: one machine, "goto" statement, action is not inherited by the destination state
// This XYZ checks that after "goto" statement, action of the src state is not inherited by the dest state

event E2 assert 1;
event E1 assert 1;
event E3 assert 1;

machine Main {
    var XYZ: bool;
    start state Real1_Init {
        entry {
			send this, E1;
        }
		
        on E1 do { goto Real1_S1; }
		on E3 do { goto Real1_S2; }        //this E3 handler is not inherited by Real1_S1
        exit {
			//send this, E2;
		}
	}
	state Real1_S1 {
		entry {
			XYZ  = true;
			send this, E3;    		
		}
		on E3 do { goto Real1_Init; }
		exit {
			send this, E3;       //this instance of E3 is not handled in Real1_S1, but in Real1_Init
		}
	}
	state Real1_S2 {
		entry {
			assert(XYZ == false);  //reachable
		}
	}
}

