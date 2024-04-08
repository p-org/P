// P semantics XYZ: one machine, "push" of the same state; "send" in entry and exit
// This XYZ checks that upon executing "goto" transition, exit function is executed,
// but upon executing "push" transition, exit function is not executed
// Result: semantics error reported by Zing:
// "<Exception> Attempting to enqueue event ____E1 more than max instance of 1"

event E2 assert 1;
event E1 assert 1;

machine Main {
    start state Real1_Init {
        entry {
			send this, E1;
        }
		
        on E2 goto Real1_Init;
		on E1 goto Real1_Init;    //upon goto, "send this, E2;" is executed
        exit {  assert false; }
	}
}

