// 1.7.7. "default" event cannot be deferred
// (parsing error)

event E1 assert 2;
event unit assert 1;

main machine Real1 {
    var test: bool;  //init with "false"
    start state Real1_Init {
        entry { 
			raise unit;
        }
		on unit do {send this, E1; push Real1_S1; };  
		on default do Action2;   //Action2 handler for E1 is inherited by Real1_S1
        exit { send this, E1;  }
	}
	state Real1_S1 {
		entry {
		}
		//deferral of E1 overrides inherited handler:
		defer E1;
		//on default do Action3; //overrides inherited handler for "default"
		defer default;
    }
	fun Action2() {
		test = true;   //unreachable
    }
	fun Action3() {
		assert(test == true);  //reachable, fails
    }
}
