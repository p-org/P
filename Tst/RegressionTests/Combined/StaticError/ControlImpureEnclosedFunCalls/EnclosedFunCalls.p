// Combined tests: "Control Impure" static errors
// Cases covered:
// enclosed function calls used in "do" and "goto"

event E1 assert 1;
event E2 assert 1;
event E3 assert 1;
event E4 assert 1;
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
					 send this, E4;
					 push Real1_S1;  
					 Action4();                          //error: Action4 changes current state
					 raise unit;                          
		};   
		//on E1 goto Real1_S1 with { push Real1_S2;};               //+  !!!!causes pc.exe exception
		on E2 goto Real1_S1 with { Action1();};           //no error: Action1 does not change current state
		on E3 goto Real1_S1 with { Action4();};           //error: Action4 changes current state
		
		on E4 do {
			Action2();                                    //error
			Action3();                                    //error
		};
	}
	state Real1_S1 {
		entry {
			}
		on unit do {
			push Real1_S2;                                   
		};
		on E1 do { Action7(); };                        //no error - OK
	    on E2 do { Action8(); };                        //no error - OK
		on E3 do { Action9(); };                        //no error - OK
    }
	state Real1_S2 {
		entry {
			}
		on E1 do { Action7(); };                        //error
	    on E2 do { Action8(); };                        //no error - OK
		on E3 do { Action9(); };                        //no error - OK
	}
	fun Action1() {		                          
		//pop; 
		i = i + 1;
    }
	fun Action2() {
		push Real1_S1;                        
    }
	fun Action3() {
		raise unit;                             
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
	fun Action7() : int {  
		push Real1_S1;                         //no error on line 37, when this line is commented out - OK
		return Action5();                     //error
    }
	fun Action8() : int {
		if (i == 1) {
			i = Action5();                      //error
		}
		return 1;
    }
	fun Action9() : int {
		if (i == 1) {
			i = Action9();                     
		}
		return 1;
    }
}
