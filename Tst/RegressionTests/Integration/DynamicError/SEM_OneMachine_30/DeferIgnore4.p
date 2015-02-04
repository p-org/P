// P semantics test, one machine: "defer" semantics and the state stack
// Testing that if an event is explicitly deferred in the pushed state,
// and there's no handler for the event in the top state (after "pop").
// "unhandled event" exception will be thrown

event E1 assert 1;
event E2 assert 1;
event E3 assert 1;
event unit assert 1;

main machine Real1 {
    var test: bool;  //init with "false"
    start state Real1_Init {
        entry { 
			raise unit;
        }
		on unit do { send this, E1; 
		             send this, E2; 
		             send this, E3; 
					 push Real1_S1; };   //push stmt; explicit pop is needed in Real1_S1
		//on E2 do Action2;      //No handler for E2 deferred in Real1_S1
        on E1 do Action2;     	
		on E3 goto Real1_S2;   
        exit {   }
	}
	state Real1_S1 {
		entry {
			test = true;
			}
		ignore E1;
	    defer E2;    
		on E3 do { pop; };
    }
	state Real1_S2 {
		entry { assert(false);}  //unreachable
	}
	fun Action2() {
		assert(test == false);  //unreachable
    }
	
}
