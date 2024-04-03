// Combined XYZs: "Control Impure" static errors
// Cases covered:
// "pop" and "raise" in "goto" function,
// used in anonymous functions (error) and invoked by (named) function calls (error)

event E1 assert 1;
event E2 assert 1;
event E3 assert 1;
event E4 assert 1;
event unit assert 1;

machine Main {
    var i: int;	
    start state Real1_Init {
        entry {
			raise unit;
        }
		on unit do { send this, E1;
		             send this, E2;
		             send this, E3;
					 send this, E4;
					 }
		on E2 goto Real1_S1 with { }
	}
	state Real1_S1 {
		entry {
			}
		on E1 goto Real1_S1 with Action2;
		on E3 goto Real1_S3 with Action1;                        //error
    }
	state Real1_S2 {
		entry { }
	}
	state Real1_S3 {
		entry { }
	}
	state Real1_S4 {
		entry { }
	}
	state Real1_S5 {
		entry { }
	}
	fun Action1() {		
		pop;                                   //error
    }
	fun Action2() {

    }
	fun Action3() {
		raise unit;                             //error
    }
	fun Action4() : int {		
		pop;
		return 1;
    }
	fun Action5() : int {

		return 1;
    }
	fun Action6() : int {
		raise unit;
		return 1;
    }
	fun Action7() : int {
		return Action5();
    }
}
