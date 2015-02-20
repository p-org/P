// Combined tests: "Control Impure" static errors
// Cases covered:
// "push", "pop" and "raise" in "goto" function,
// used in anonymous functions (error) and invoked by (named) function calls (error)

event E1 assert 1;

main machine Real1 {
    var i: int;	  
    start state Real1_Init {
        entry { 
			send this, E1;
        }
		on E1 goto Real1_S1 with {
			i = i + Action7();                // no error??
			};
	}
	state Real1_S1 {
		entry {
			}
    }
	fun Action5() : int {
		push Real1_S1;                           
		return 1;
    }
	fun Action7() : int {  
		//push Real1_S1;                      //no error on line 15, when this line is commented out
		return Action5();                     //error
    }
}
