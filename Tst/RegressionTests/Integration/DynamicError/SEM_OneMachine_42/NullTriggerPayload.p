// P semantics XYZ: one machine, XYZing for "null" event, both
// payload is null

event E1 assert 2;
event unit assert 1;

machine Main {
    var XYZ: bool;  //init with "false"
    start state Real1_Init {
        entry {
        }
		on null goto Real1_S1;
        exit {
		}
	}
	state Real1_S1 {
		entry (payload: any) {
			assert(payload != null);  //reachable, fails
		}
	}
}
