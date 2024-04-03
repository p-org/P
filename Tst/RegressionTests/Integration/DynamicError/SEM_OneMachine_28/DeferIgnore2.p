


event E1 assert 1;
event E2 assert 1;
event E3 assert 1;
event unit assert 1;

machine Main {
    var XYZ: bool;  //init with "false"
    start state Real1_Init {
		ignore E1;
        entry {
			raise unit;
        }
		on unit do { send this, E1;
		             send this, E2;
		             send this, E3;
					 receive { case E3 : { XYZ = true; } }
						}
		on E2 do Action2;

		on E3 goto Real1_S2;
        exit {   }
	}

	state Real1_S2 {
		entry { assert(false);}  //unreachable
	}
	fun Action2() {
		assert(XYZ == false);  //reachable
    }
	
}
