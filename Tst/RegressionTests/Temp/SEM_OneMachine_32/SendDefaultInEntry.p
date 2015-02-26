// P semantics test: one machine, no actions

main machine Real1 {
    var test: bool;  //init with "false"
    start state Real1_Init {
        entry {
	   
        }
		exit {   }
		//In both cases, assert(false) is not reached. Why?
		//on default do Action2;
		on default do {assert(false);};      
	}
	fun Action2() {
		assert(false);
    }
}
