// This sample tests case when exit actions are not executed
event E;

main machine Program {
		     var i: int;
	start state Init {
			 entry { i = 0; raise E; }
		//this assert is unreachable:
		//after the Call state is popped with (i == 3), the queue is empty,
		// machine keeps waiting for an event, and exit actions are never executed
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
			     raise E;   //Call is popped
			 }
	}
}
