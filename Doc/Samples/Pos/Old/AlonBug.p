event E;

main machine Program {
		     var i: int;
	start state Init {
			 entry { i = 0; raise (E); }
		exit { assert (false); }
		on E push Call;
	}

	state Call {
		   entry { 
			 if (i == 3) {
				     return; 
			 }
                         else
			    {
			    i = i + 1;
			    }
			 raise (E); 
			 }
	}
}
