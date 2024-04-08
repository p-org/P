// 1.6.6.1.	Multiple handlers for the event: ignore/defer case
//

event E1 assert 2;
event E2 assert 2;
event unit assert 1;

machine Main {
    var XYZ: bool;  //init with "false"
    start state Real1_Init {
        entry {
			raise unit;
			send this, E1;
			send this, E2;
        }
		on unit goto Real1_S1;
		on E2 do Action2;
        //on E1 do Action2;   //Action2 handler for E1 is inherited by Real1_S1	
        exit { send this, E2;  }
	}
	state Real1_S1 {
		entry {
			XYZ = true;
		}
		ignore E1;
		defer E1;
    }
	fun Action2() {
		assert(XYZ == false);  //unreachable
    }
}
