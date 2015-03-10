// P semantics test: one machine, testing for "null" event, both
// payload and trigger are null

event E1 assert 2;
event unit assert 1;

main machine Real1 {
    var test: bool;  //init with "false"
    start state Real1_Init {
        entry { 
        }
		on null goto Real1_S1;
        exit {  
		}
	}
	state Real1_S1 {
		entry {
			assert(payload != null || trigger != null);  //reachable, fails
		}
	}
}
