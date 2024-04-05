// P semantics XYZ, one machine: "null" handler semantics
// XYZing that null handler is enabled in the simplest case

machine Main {
    var XYZ: bool;  //init with "false"
    start state Real1_Init {
        entry {
        }

        on null goto Real1_S2;
        exit {   }
	}
	
	state Real1_S2 {
		entry { assert(XYZ == true);}  //reachable
	}
	
}
