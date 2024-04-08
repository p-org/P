// P semantics XYZ: one machine, "push", "send" in entry and exit
// This XYZ checks that upon executing "goto" transition, exit function is executed,
// but upon executing "push" transition, exit function is not executed

event E2 assert 1;
event E1 assert 1;

machine Main {
    start state Real1_Init {
        entry {
			send this, E1;
        }
		
        on E2 goto Real1_S1;
		on E1 goto Real1_Init;    //upon goto, "send this, E2;" is executed
        exit {  send this, E2; }
	}
	state Real1_S1 {
		entry {
			assert(false);  //reachable
		}
	}
}

