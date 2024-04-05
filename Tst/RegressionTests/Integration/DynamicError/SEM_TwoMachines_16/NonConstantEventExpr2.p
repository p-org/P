// P semantics XYZ: two machines, "send", "raise" with non-constant event expressions
// "raise" with non-constant event expression has non-null payload
event E1 assert 1: int;
event E2 assert 1: bool;

machine Main {
    var XYZ: bool;
	var mac: machine;
	var ev1: event;
	var ev2: event;
	var ev3: int;
    start state Real1_Init {
        entry {
			mac = new Real2();
			ev2 = E2;
			announce ev2, true;
			ev1 = E1;			
			raise ev1, 100;  		
        } 	
        on E1 do (payload: int) { Action1(payload); }
		on null goto Real1_S1;  //unreachable
		//on E2 do Action2;
        exit {
			ev2 = E2;
            send mac, ev2, XYZ;			
		}
	}
	state Real1_S1 {
	}
    fun Action1(payload: int) {
		assert (payload != 100);   //fails (both in Zing and runtime)	
    }
	
}
machine Real2 {
	var pl: bool;
	start state Real2_Init {
		entry {	
		}
		on E2 do (payload: bool) {
			
			
				Action2(payload);
			
			
			
		}
	}
	fun Action2(payload: bool) {
		assert(payload == false);  //unreachable
    }
}
