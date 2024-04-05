// P semantics XYZ: one machine, the XYZ demonstrates that "null" event handler
// is executed in a loop
//

event E1 assert 2;
event unit assert 1;
event local;

machine Main {
    var XYZ: bool;  //init with "false"
	var i: int;
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
			XYZ = true;
			i = 0;
		}
		//deferral of E1 overrides inherited handler:
		defer E1;
		on null do Action1; //overrides inherited handler for "null"
    }
	fun Action2() {
		XYZ = true;   //unreachable
    }
	fun Action1() {
		if (i < 10)
		{			
			i = i + 1;
		}
		else {assert (i != 10);}
	}
}
