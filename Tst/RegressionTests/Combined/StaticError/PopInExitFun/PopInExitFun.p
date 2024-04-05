// (15,4): "function may cause a change in current state; this is not allowed here"
// case: "pop" in exit function of the state:
// PurityError(c, called) :- c is StateDecl(_, owner, _, called, _), called = AnonFunDecl(owner, _), ControlImpure(called).

event E1 assert 1;

machine Main {
    var XYZ: bool;  //init with "false"
    start state Real1_Init {
        entry {			
        }
		
        on E1 do Action1;   // checking "raise"
        exit {
			pop;
		}
	}
	state Real1_S1 {
		entry {
			assert(XYZ == true); //unreachable
		}
    }
    fun Action1() {
		XYZ = true;
    }
}
