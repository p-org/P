// Combined tests: "Control Impure" static errors
// Cases covered:
//  "push", "pop" and "raise" in entry action,
// used in anonymous functions (allowed) and  
// invoked by (named) function calls (error)

event E;
event unit;

main machine Program {
		     var i: int;
	start state Init {
			 entry { i = 0; raise E; }
	
		on E push Call;    
	}

	state Call {
		   entry { 
			 if (i == 3) {
				    pop;           //no error
					Action4();     //error
					raise E;       //no error
					Action5();     //error
					push Init;     //no error
					Actions6();    //error
			}
            else
			    {
					i = i + Action1() +   //error
							Action2() -   //error
							Action3();    //error
			    }
			     raise E; 
			 }
	}
	fun Action1() : int {		                          
		pop;   
		return 1;
    }
	fun Action2() : int {
		push Init;
		return 1;
    }
	fun Action3() : int {
		raise unit;
		return 1;
    }
	fun Action4() {		                          
		pop;   
		
    }
	fun Action5() {
		raise unit;
    }
	fun Action6() {
		push Init;
    }
}
