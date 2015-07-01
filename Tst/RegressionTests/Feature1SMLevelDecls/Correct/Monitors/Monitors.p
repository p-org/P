//this sample tests the new syntax for monitors.
event local;
event global;

First monitors a {
	var x : int;
	start state Init {
	
	}
}

main machine MAIN {
	start state Init {}
}
