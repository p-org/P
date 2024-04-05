// "raise", "send" and announce invocation with non-constant event expression

event E1 assert 1;
event E2 assert 1;

machine Main {
    var XYZ: bool;
	var ev1: event;
	var ev2: event;
	var ev3: int;
    start state Real1_Init {
        entry {
            send this, ev2;	
			raise ev1;
			announce ev1;
			announce E1;
			announce ev3;	  // static error		
        }
		on ev1 do Action1;  // static error
        on E1 do Action1;
		on ev2 do Action2;  // static error
		on E2 do Action2;
        exit {   }
	}
    fun Action1() {
		XYZ = true;
    }
	fun Action2() {
		assert(XYZ == false);  //unreachable
    }
}
spec M observes E1 {
	start state x {
	}
}
