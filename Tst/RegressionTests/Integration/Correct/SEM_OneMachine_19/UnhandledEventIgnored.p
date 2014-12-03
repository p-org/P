// P semantics test: one machine, "ignore" of an unhandled event
// Compare this test to SendInExitUnhandledEvent.p

event E2 assert 2;
event E1 assert 1;

main machine Real1 {
    var test: bool;  //init with "false"
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
			test = true;
		}
		//to prevent "unhandled event" exception for E2
		ignore E2;
    }
	fun Action2() {
		assert(test == false);  //unreachable
    }
}
