// Combined tests: "Control Impure" static errors
// Cases covered:
// "push", "pop" and "raise" in "do" declaration statements,
// used in anonymous functions (allowed) and invoked by (named) function calls (error)

event E1 assert 1;
event E2 assert 1;
event E3 assert 1;
event unit assert 1;

main machine Real1 {
    var i: int;	  
    start state Real1_Init {
        entry { 
			raise unit;
        }
		on unit do { send this, E1; 
		             send this, E2; 
		             send this, E3; 
					 push Real1_S1;                       // no error
					 raise unit;                          // no error
		};   
		//on E2 goto Real1_S1 with { push Real1_S2;};               //+  !!!!causes pc.exe exception
		
		on E2 do {
			Action2();                                    //error
			Action3();                                    //error
		};
		on E3 do {
			if (i == 3) {
				    pop;                                //no error
			}
            else
			    {
					i = i + Action4() +   //error
							Action5() -   //error
							Action6();    //error
			    }
		};
	}
	state Real1_S1 {
		entry {
			}
		on unit do {
			pop;                                   //no error
		};
		on E1 do { Action2(); };                        //error
	    on E2 do { Action3(); };                        //error
		on E3 do { Action1(); };                        //error
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
		push Real1_S1;                          //error
    }
	fun Action3() {
		raise unit;                             //error
    }
	fun Action4() : int {		                          
		pop;                                   
		return 1;
    }
	fun Action5() : int {
		push Real1_S1;                           
		return 1;
    }
	fun Action6() : int {
		raise unit;                                   
		return 1;
    }
}
