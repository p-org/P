// P semantics XYZ: two machines, announce announce instantiation parameter
// This is validation XYZ for announceInvocation.p
event E2 assert 1: bool;

machine Main {
    var XYZ: bool;
	  var ev2: event;
    start state Real1_Init {
        entry {
          ev2 = E2;
			    announce ev2, XYZ;  //"null event" error in Zing
		}
	}
}
spec M observes E2 {
	start state x {
                on E2 do (payload: bool) { assert (payload == false); }
	}
}

test DefaultImpl [main=Main]: assert M in { Main };

