// P semantics XYZ: one machine, XYZing  top  "null" event handler overriding
//inherited (by push transition) handler

event E1 assert 2;
event unit assert 1;
event local;

machine Main {
    var XYZ: bool;  //init with "false"
    start state Real1_Init {
        entry {
			raise unit;
        }
		on unit do {send this, E1; raise local; }
		on local goto Real1_S1;
		on null do Action2;   //Action2 handler for E1 is inherited by Real1_S1
        exit { send this, E1;  }
	}
	state Real1_S1 {
		entry {
		}
		//deferral of E1 overrides inherited handler:
		defer E1;
		on null do Action3; //overrides inherited handler for "null"
    }
	fun Action2() {
		XYZ = true;   //unreachable
    }
	fun Action3() {
		assert(XYZ == true);  //reachable, fails
    }
}
