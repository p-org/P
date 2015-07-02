//this sample tests the new syntax for monitors.
event local;
event global : int;

machine First monitors local {
	var x : int;
	start state Init {
	
	}
}

main machine MAIN {
	start state Init {
		entry {
			monitor local;
			monitor global, 5;
		}
	}
}
