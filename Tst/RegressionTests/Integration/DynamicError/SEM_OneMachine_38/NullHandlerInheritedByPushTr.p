// P semantics XYZ, one machine: "null" handler semantics
// XYZing that null handler is inherited by the pushed state


event E;

machine Main {
	var i: int;
	start state Init {
			 entry { i = 0; raise E; }

		exit { assert (false); }  //unreachable
		on E goto Call;
		on null do {assert(false);}  //inherited by Call; reachable
	}

	state Call {
		   entry {
			   if (i == 0) {
				     raise E;
					
			   }
               else {
					i = i + 1;
			   }
			}
			ignore E;
			//at this point, inherited "do" on "null" is executed
			
			exit { assert (false);}  //unreachable, since the state is not popped
	}
}
