// Combined XYZs: "Control Impure" static errors
// Cases covered:
// "pop" and "raise" in expressions in functions

event E;
event unit;

machine Main {
		     var i: int;
	start state Init {
			 entry { i = 0; raise E; }
	
		on E push Call;    
	}

	state Call {
		   entry { 
			 if (i == 3) {
				    pop; 
			}
            else
			    {
					i = i + Action1() +   //error
							Action2() -
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

		return 1;
    }
	fun Action3() : int {
		raise unit;
		return 1;
    }
}
