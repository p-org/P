// P semantics test: one machine, "send" to itself in exit function
// E2 is sent upon executing goto; however, 
// E2 is not handled, since state Real1_Init is removed once goto is executed
// Result: semantic error detected by Zing
// Compare this test with SendInExitHandledEvent.p

event E2 assert 1;
event E1 assert 1;

machine Main {
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
    }
	fun Action2() {
		assert(test == false);  //unreachable
    }
}
