/* This file implements various test-drivers and also provides the various test-cases that are model-checked by the P Checker*/


/*
This machine creates the 2 participants, 1 coordinator, and 2 clients 
*/
machine TestDriver0 {
	start state Init {
		entry {
			var coord : machine;
			var participants: seq[machine];
			var i : int;
			while (i < 2) {
				participants += (i, new Participant());
				i = i + 1;
			}
			coord = new Coordinator(participants);
			new Client(coord);
			new Client(coord);
		}
	}
}

/*
This machine creates the 2 participants, 1 coordinator, 1 Failure injector, and 2 clients 
*/
machine TestDriver1 {
	start state Init {
		entry {
			var coord : machine;
			var participants: seq[machine];
			var i: int;
			while (i < 2) {
				participants += (i, new Participant());
				i = i + 1;
			}
			coord = new Coordinator(participants);
			new FailureInjector(participants);
			new Client(coord);
			new Client(coord);
		}
	}
}

/* 
The failure injector machine randomly selects a participant machine and enqueues a special event "halt"
On dequeueing a halt event, the P machine destroyes itself safely. 
This is one way of modeling node failures in P.
Note that as the model-checker explores all possible interleavings. The failure injecture is exhaustive and can add a failure at all possible interleaving points.
*/

machine FailureInjector {
	start state Init {
		entry (participants: seq[machine]){
			var i : int;
			i = 0;
			while(i< sizeof(participants))
			{
				if($)
				{
					send participants[i], halt;
				}
				i = i + 1;
			}		
		}
	}
}

// checks that alll events are handled correctly and also the local assertions in the P machines.
test Test0[main = TestDriver0]: { TestDriver0, Coordinator, Participant, Timer, Client };

// asserts the liveness monitor along with the default properties
test Test1[main = TestDriver1]: assert Progress in { TestDriver1, Coordinator, Participant, Timer, Client, FailureInjector };

// asserts the atomicity monitor along with the default properties
test Test2[main = TestDriver0]: assert Atomicity in { TestDriver0, Coordinator, Participant, Timer, Client };