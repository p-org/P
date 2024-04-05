// P semantics XYZ: one machine, "send" to itself in exit function
// exit function is never executed, since control never leaves the Real1_Init state
event E2 assert 1;

machine Main {
    start state Real1_Init {
        entry {
        }
        on E2 do Action2;
        exit {  send this, E2; }
	}
	fun Action2() {
		assert(false);  //unreachable
    }
}
