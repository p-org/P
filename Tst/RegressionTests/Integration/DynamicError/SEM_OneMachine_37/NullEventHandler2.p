// P semantics test, one machine: "null" handler semantics 
// Testing that null handler is enabled in the simplest case

main machine Real1 {
    var test: bool;  //init with "false"
    start state Real1_Init {
        entry { 
        }

        on null goto Real1_S2;   
        exit {   }
	}
	
	state Real1_S2 {
		entry { assert(test == true);}  //reachable
	}
	
}
