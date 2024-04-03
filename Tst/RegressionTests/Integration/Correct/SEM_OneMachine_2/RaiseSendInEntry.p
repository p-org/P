// P semantics XYZ: two machines, machine is halted with "raise halt" (unhandled)
// Action2 is never executed after raising E1; XYZ passes
event E1 assert 1;
event E2 assert 1;

machine Main {
    var XYZ: bool;  //init with "false"
    start state Real1_Init {
        entry { 	
			raise E1;
			send this, E2;
        }
		
        on E1 do Action1;   // checking "raise"
        on E2 do Action2;   // checking "send"
        exit {   }
	}
    fun Action1() {
		XYZ = true;
    }
	fun Action2() {
		assert(XYZ == false); //unreachable
    }
}
