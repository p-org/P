// P semantics XYZ: one machine, "send" to itself and "raise" in entry actions
event E1 assert 1;
event E2 assert 1;

machine Main {
    var XYZ: bool;  //init with "false"
    start state Real1_Init {
        entry {
            send this, E2;	
			raise E1;
        }
		
        on E1 do Action1;   // checking "raise"
        on E2 do Action2;   // checking "send"
        exit {   }
	}
    fun Action1() {
		XYZ = true;
    }
	fun Action2() {
		assert(XYZ == false);  //fails here
    }
}
