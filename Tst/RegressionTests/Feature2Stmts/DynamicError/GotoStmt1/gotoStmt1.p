// P semantics XYZ: one machine, "goto" to the same state; "send" in entry and exit
// This XYZ checks that upon executing "goto" statement, exit function is executed;
// E2 is sent upon executing goto;
// E2 is handled by Action2 after entering Real1_Init upon "goto" transition
// Result: assert on line 21 is raised by Zing

event E2 assert 1;
event E1 assert 1;

machine Main {
    start state Real1_Init {
        entry {
			send this, E1;
        }
		
        on E2 do Action2;
		on E1 do { goto Real1_Init; } //upon goto, "send this, E2;" is executed
        exit {  send this, E2; }
	}
	fun Action2() {
		assert(false);  //reachable
    }
}