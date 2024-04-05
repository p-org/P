// P semantics XYZ: two machines, announce invocation with non-constant event expression with payload
// This XYZ found a bug (?) in pc.exe
event E1 assert 1;
event E2 assert 1: bool;

machine Main {
    var XYZ: bool;
	var ev2: event;
    start state Real1_Init {
        entry {
			ev2 = E2;
			announce ev2, XYZ;
		}
	}
}
spec M observes E2 {
	start state x {
		entry {
		}
		on E2 do (payload: bool) { assert (payload == true); }  //fails in Zinger
	}
}

test DefaultImpl [main=Main]: assert M in { Main };