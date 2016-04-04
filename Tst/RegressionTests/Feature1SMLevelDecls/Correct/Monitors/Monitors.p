//this sample tests the new syntax for monitors.
event local;
event global : int;

spec First monitors local {
	var x : int;
	start state Init {
		on local do { x = x + 1; }
		on global do { assert(x == 2); }
	}
}

main machine MAIN {
	start state Init {
		entry {
			send this, local;
			monitor local;
			monitor global, 5;
		}
		ignore local;
	}
}
