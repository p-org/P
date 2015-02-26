// Combined tests: "Control Impure" static errors
// Cases covered:
// "push", "pop" and "raise" in "exit" actions,
// in both anon and named functions

event E1 assert 1;
event E2 assert 1;
event E3 assert 1;
event unit assert 1;

main machine Real1 {
    var test: bool;  
    start state Real1_Init {
        entry { 
			raise unit;
        }
		on unit do { send this, E1; 
		             send this, E2; 
		             send this, E3; 
					 push Real1_S1; };   //push stmt; explicit pop is needed
		on E2 goto Real1_S1 with { push Real1_S2;};               //
        on E1 goto Real1_S2 with { raise unit;};  	              //
		on E3 goto Real1_S3 with { pop;};                         //+
        exit {			
			}
	}
	state Real1_S1 {
		entry {
			test = true;
			}
		on E1 goto Real1_S1 with Action2;                        //
	    on E3 goto Real1_S2 with Action3;                        //
		on E3 goto Real1_S3 with Action1;                    //
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
		pop;                                   //
    }
	fun Action2() {
		push Real1_S1;
    }
	fun Action3() {
		raise unit;
    }
	
}
