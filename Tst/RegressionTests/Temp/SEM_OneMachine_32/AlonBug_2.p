// This sample is *supposed* to test the case when the deadlocked state is not the only state in the stack
event E;

main machine Program {
	var i: int;
	start state Init {
			 entry { i = 0; raise E; }
		//this assert is unreachable:
		//after the Call state is popped with (i == 3), the queue is empty,
		// machine keeps waiting for an event, and exit actions are never executed
		exit { assert (false); }  //reachable
		on E push Call;
	}

	state Call {
		   entry { 
			   if (i == 0) {
				       pop; 
					   
			   }
               //else {
				//	i = i + 1;
			   //}
			   //raise E;   //Call is popped
			}
			exit { assert (false); }  //unreachable???
	}
}
