// P semantics XYZ: two machines, "send", "raise", announce invocation for non-constant event expressions
//This XYZ found null ptr deref bug in Zing
event E1 assert 1;
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
			announce ev2, true;  //zing error (ev2 is nulll)
			ev1 = E1;			
			raise ev1;  		
        } 	
        on E1 do Action1;
		on null goto Real1_S1;
		//on E2 do Action2;
        exit {
			ev2 = E2;
            send mac, ev2, XYZ;			
		}
	}
	state Real1_S1 {
	}
    fun Action1() {
		XYZ = true;
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
		assert(payload == false);  //fails in runtime
    }
}
