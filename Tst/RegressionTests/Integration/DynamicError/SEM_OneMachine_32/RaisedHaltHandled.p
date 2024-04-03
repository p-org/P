// P semantics XYZ: one machine, "halt" is raised and handled

event E1 assert 1;

machine Main {
    var XYZ: bool;  //init with "false"
    start state Real1_Init {
        entry {
		    send this, E1;
			raise halt;
        }
		on halt goto Real1_S1;
        on E1 do Action2;   //inherited by Real1_S1
        exit { }
	}
	state Real1_S1 {
		entry (payload: any) {
			XYZ = true;
		}
    }
	fun Action2() {
		assert(XYZ == false);  //reachable
    }
}
