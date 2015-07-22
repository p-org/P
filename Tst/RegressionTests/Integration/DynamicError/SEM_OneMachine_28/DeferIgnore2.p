// P semantics test, one machine: "defer" semantics and the state stack
// Testing that if an event is explicitly deferred in the pushed state,
// it will be handled after the state is popped
event E1 assert 1;
event E2 assert 1;
event E3 assert 1;
event unit assert 1;

main machine Real1 {
    var test: bool;  //init with "false"
    start state Real1_Init {
		ignore E1;
        entry { 
			raise unit;
        }
		on unit do { send this, E1; 
		             send this, E2; 
		             send this, E3; 
					 receive { case E3 : { test = true; }}
						};   //push stmt; explicit pop is needed
		on E2 do Action2;   //Action2 handler for E1, E2 is inherited by Real1_S1
        on E1 do Action2;   	
		on E3 goto Real1_S2;  //handler for E3 is not inherited by Real1_S1 
        exit {   }
	}

	state Real1_S2 {
		entry { assert(false);}  //unreachable
	}
	fun Action2() {
		assert(test == false);  //reachable
    }
	
}
