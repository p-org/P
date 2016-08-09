// P semantics test, one machine: "null" handler semantics 
// Testing that null handler is enabled in the simplest case

event E1 assert 1;
event unit assert 1;

machine Main {
    var test: bool;  //init with "false"
    start state Real1_Init {
        entry { 
			raise unit;
        }
		on unit do { send this, E1; }
        on E1 do Action2;   
        on null goto Real1_S2;   
        exit {   }
	}
	
	state Real1_S2 {
		entry { assert(test == false);}  //reachable
	}
	fun Action2() {
		test = true;
		assert(test == true);  //reachable
    }
	
}
