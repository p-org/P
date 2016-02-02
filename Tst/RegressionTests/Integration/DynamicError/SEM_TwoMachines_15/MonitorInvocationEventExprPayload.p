// P semantics test: two machines, monitor invocation with non-constant event expression with payload
// This test found a bug (?) in pc.exe
event E1 assert 1;
event E2 assert 1: bool;

main machine Real1 {
    var test: bool; 
	var ev2: event;
    start state Real1_Init {
        entry { 
			ev2 = E2;
			monitor ev2, test;  
		}
	}
}
spec M monitors E2 {
	start state x {
		entry {
		}
		on E2 do (payload: bool) { assert (payload == true); };  //fails in Zinger
	}
}
