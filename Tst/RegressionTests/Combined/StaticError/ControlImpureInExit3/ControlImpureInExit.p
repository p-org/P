// P semantics XYZ, one machine: "Control Impure" static errors
// Cases covered:
// "pop" and "raise" in "exit" function,
// in both anon and named functions

event E1 assert 1;
event E2 assert 1;
event E3 assert 1;
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
					 }
		on E2 do Action1;
        on E1 do { }
	}
	state Real1_S1 {
		entry {}
		ignore E1;
	    defer E2;
		on E3 do { pop; }
		exit Action1;                        //error
    }
	state Real1_S2 {
		entry { }
	}
	state Real1_S3 {
		entry { }
		exit Action2;
	}
	state Real1_S4 {
		entry { }
	}
	state Real1_S5 {
		entry { }
	}
	fun Action1() {		
		pop;
    }
	fun Action2() {

    }
	fun Action3() {
		raise unit;
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
}
