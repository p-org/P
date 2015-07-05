// P semantics test: two machines, "send", "raise" with payload for non-constant event expressions

event E1 assert 1;
event E2 assert 1: bool;

main machine Real1 {
    var test: bool; 
	var mac: machine;
	var ev1: event;
	var ev2: event;
	var ev3: int;
    start state Real1_Init {
        entry { 
			mac = new Real2(this);
			//monitor M, ev2, test;
			//send M, E1;
			ev1 = E1;			
			raise ev1;  		
        } 	
        on E1 do Action1;   
		on default goto Real1_S1;
        exit {  
			ev2 = E2;
            send mac, ev2, test;			 
		}
	}
	state Real1_S1 {
	}
    fun Action1() {
		test = true;
    }
	
}
machine Real2 {
	var pl: bool;
	start state Real2_Init {
		entry {	
		}
		on E2 do {
			if (trigger == E2) 
			{ 
				Action2(); 
			}
			else 
			{ assert(false);;};  //unreachable
		};
	}
	fun Action2() {
		assert(payload == false);  //fails
    }
}
spec M monitors E1 {
	start state x {
		entry {
			//assert (payload == true);   //fails
		}
	}
}