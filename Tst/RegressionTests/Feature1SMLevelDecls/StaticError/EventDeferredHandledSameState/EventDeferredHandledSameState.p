// P semantics XYZ: one machine, event both deferred and handled in the same state:
// handler overrides

event E2 assert 2;
event E1 assert 1;

machine Main {
    var XYZ: bool;  //init with "false"
    start state Real1_Init {
        entry {
			send this, E1;
        }
		on E1 goto Real1_S1;
        on E2 do Action2;
		
        exit {  send this, E2; }
	}
	state Real1_S1 {
		entry {
			XYZ = true;
		}
		//to prevent "unhandled event" exception for E2
		defer E2;
		on E2 do { assert(false); ;}  //unreachable
    }
	fun Action2() {
		assert(XYZ == false);  //unreachable
    }
}
