// P semantics test, one machine: "default" handler semantics 
// Testing that default handler is inherited by the pushed state


event E;

main machine Program {
	var i: int;
	start state Init {
			 entry { i = 0; raise E; }

		exit { assert (false); }  //unreachable
		on E push Call;
		on default do {assert(false);;;};  //inherited by Call; reachable
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
			//at this point, inherited "do" on "default" is executed
			
			exit { assert (false); ;}  //unreachable, since the state is not popped
	}
}
