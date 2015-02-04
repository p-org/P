// P semantics test: one machine, "halt" is raised and unhandled

event E2 assert 1;
event E1 assert 1;

main machine Real1 {
    var test: bool;  //init with "false"
    start state Real1_Init {
        entry { 
		    send this, E1;
			raise halt;
        }
		on E1 goto Real1_S1;
        on E2 do Action2; 
        exit {  send this, E2; }   //machine Real1 is halted after sending E2
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
