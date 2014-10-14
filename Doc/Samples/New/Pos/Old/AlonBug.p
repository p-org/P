// This sample tests that a pop statement only pops one stack frame
event E;

main machine Program {
		     var i: int;
	start state Init {
			 entry { i = 0; raise E; }
		exit { assert (false); }
		on E push Call;
	}

	state Call {
		   entry { 
			 if (i == 3) {
				     pop; 
			 }
                         else
			    {
			    i = i + 1;
			    }
			 raise E; 
			 }
	}
}
