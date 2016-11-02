// P semantics test_: one machine, deferral of an unhandled event
// Compare this test_ to SendInExitUnhandledEvent.p

event E2 assert 2;
event E1 assert 1;

machine Main {
    var test_: bool;  //init with "false"
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
			test_ = true;
		}
		//to prevent "unhandled event" exception for E2
		defer E2;
    }
	fun Action2() {
		assert(test_ == false);  //unreachable
    }
}
