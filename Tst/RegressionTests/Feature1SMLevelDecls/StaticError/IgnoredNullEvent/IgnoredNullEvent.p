// 1.7.7. "null" event cannot be "ignored"
// (parsing error)

event E1 assert 2;
event unit assert 1;

machine Main {
    var XYZ: bool;  //init with "false"
    start state Real1_Init {
        entry {
			raise unit;
        }
		on unit do {send this, E1; }
		on null do Action2;   //Action2 handler for E1 is inherited by Real1_S1
        exit { send this, E1;  }
	}
	state Real1_S1 {
		entry {
		}
		//deferral of E1 overrides inherited handler:
		defer E1;
		//on null do Action3; //overrides inherited handler for "null"
		ignore null;
    }
	fun Action2() {
		XYZ = true;   //unreachable
    }
	fun Action3() {
		assert(XYZ == true);  //unreachable
    }
}
