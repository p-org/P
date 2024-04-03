// This sample XYZs case when exit actions are executed before "goto" transition
// Compare to AlonBug.p
event E;

machine Main {
		     var i: int;
	start state Init {
			 entry { i = 0; raise E; }
		//this assert is reachable:
		exit { assert (false); }
		on E goto Call;    //exit actions are executed before this transition
	}

	state Call {
		   entry {
			 if (i == 3) {
//				    pop;
			}
            else
			    {
					i = i + 1;
			    }
			     raise E;
			 }
	}
}
