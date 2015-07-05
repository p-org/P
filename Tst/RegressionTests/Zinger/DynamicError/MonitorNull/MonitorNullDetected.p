// P semantics test: two machines, monitor invocation for non-constant event expressions

event E1 assert 1;
event E2 assert 1: bool;

main machine Real1 {
    var test: bool; 
	var mac: machine;
	var ev1: event;
	var ev2: event;
	var ev3: int;
    start state Real1_Init {
        entry { 
			monitor ev1; //Zing: null event expr in monitor invocation			
        } 	
        exit {   }
	}
    fun Action1() {
		test = true;
    }
	
}
spec M monitors E1 {
	start state x {
	}
}