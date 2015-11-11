//Exit function performed while explicitly popping the state

event E;
main machine Program {
	start state Init {
		entry { raise E; }
		exit { assert (false); }  
		on E push Call;
	}

	state Call {
		   entry { 
			   
				       pop; 					   
			}
			exit { assert (false);}  //this is the line that should be reported
	}
}
