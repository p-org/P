// Combined XYZs: "Control Impure" static errors
// Cases covered:
// "pop" and "raise" in entry action,
// used in anonymous functions (allowed) and
// invoked by (named) function calls (error)

event E;
event unit;

machine Main {
		     var i: int;
	start state Init {
			 entry { i = 0; raise E; }
	
		on E goto Call;
	}

	state Call {
		   entry {
			 if (i == 3) {
//				    pop;           //no error
					Action4();
					raise E;       //no error
					Action5();

					Actions6();    //error
			}
            else
			    {
					i = i + Action2() +
							Action2() -
							Action2();
			    }
			     raise E;
			 }
	}
	fun Action1() : int {		
//		pop;
		return 1;
    }
	fun Action2() : int {

		return 1;
    }
	fun Action3() : int {
		raise unit;
		return 1;
    }
	fun Action4() {		
//		pop;
		
    }
	fun Action5() {
		raise unit;
    }
	fun Action6() {

    }
}
