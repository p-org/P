// P semantics XYZ: one machine, "send" to itself in entry actions
event E2 assert 1;

machine Main {
    var XYZ: bool;  //init with "false"
    start state Real1_Init {
        entry {
            send this, E2;	
        }
        on E2 do Action2;   // checking "send"
        exit {   }
	}
	fun Action2() {
		assert(false);
    }
}
