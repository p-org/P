//this sample XYZs the new syntax for observes.
event local;
event global : int;

spec First observes local {
	var x : int;
	start state Init {
		on local do { x = x + 1; }
		on global do (payload: int) { assert(x == 2); }
	}
}

machine Main {
	start state Init {
		entry {
			send this, local;
			announce local;
			announce global, 5;
		}
		ignore local;
	}
}
