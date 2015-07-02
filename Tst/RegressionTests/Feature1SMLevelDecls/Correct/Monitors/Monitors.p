//this sample tests the new syntax for monitors.
event local;
event global : int;

spec First monitors local {
	var x : int;
	start state Init {
		on local do { assert (false); };
	}
}

main machine MAIN {
	start state Init {
		entry {
			new First();
			send this, local;
			monitor local;
			monitor global, 5;
		}
	}
}
