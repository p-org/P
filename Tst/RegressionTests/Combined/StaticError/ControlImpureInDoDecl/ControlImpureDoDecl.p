// Combined XYZs: "Control Impure" static errors
// Cases covered:
// "pop" and "raise" in "do" declaration statements,
// used in anonymous functions (allowed) and invoked by (named) function calls (error)

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

					 raise unit;                          // no error
		}   

		
		on E2 do {
			Action2();
			Action3();
		}
		on E3 do {
			if (i == 3) {
				    pop;                                //no error
			}
            else
			    {
					i = i + Action4() +   //error
							Action5() - 
							Action6();    //error
			    }
		}
	}
	state Real1_S1 {
		entry {
			}
		on unit do {
			pop;                                   //no error
		}
		on E1 do { Action2(); }
	    on E2 do { Action3(); }
		on E3 do { Action1(); }
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
