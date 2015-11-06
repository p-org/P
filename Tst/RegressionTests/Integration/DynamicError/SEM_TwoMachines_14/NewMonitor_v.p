// P semantics test: two machines, monitor monitor instantiation parameter
// This is validation test for MonitorInvocation.p
event E2 assert 1: bool;

main machine Real1 {
    var test: bool; 
	var ev2: event;
    start state Real1_Init {
        entry { 
			new M(true);
			monitor ev2, test;  //"null event" error in Zing
		}
	}
}
spec M monitors E2 {
	start state x {
		entry (payload: bool) {
			// executed upon "new":
			assert (payload == true);   //passes
		}
	}
}