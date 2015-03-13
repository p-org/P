// P semantics test: two machines, monitor instantiation parameter

event E2 assert 1: bool;

main machine Real1 {
    var test: bool; 
	var ev2: event;
    start state Real1_Init {
        entry { 
			new M();
			//ev2 is null, but action below is not reachable -
			//hence, no error from Zinger
			monitor M, ev2, test;  
		}
	}
}
monitor M {
	start state x {
		entry {
			// executed upon "new":
			assert (payload == true);   //fails: payload is "new" parameter here (null)
		}
	}
}