// P semantics test: one machine, "halt" is raised and handled

event E1 assert 1;

main machine Real1 {
    var test: bool;  //init with "false"
    start state Real1_Init {
        entry { 
		    send this, E1;
			raise halt;
        }
		on halt push Real1_S1;
        on E1 do Action2;   //inherited by Real1_S1
        exit { }   
	}
	state Real1_S1 {
		entry (payload: any) {
			test = true;
		}
    }
	fun Action2() {
		assert(test == false);  //reachable
    }
}
