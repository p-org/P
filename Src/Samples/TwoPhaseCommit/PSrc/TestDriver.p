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

test Test0[main = TestDriver0]: { TestDriver0, Coordinator, Participant, Timer, Client };

test Test1[main = TestDriver1]: assert Progress in { TestDriver1, Coordinator, Participant, Timer, Client, FailureInjector };

test Test2[main = TestDriver0]: assert Atomicity in { TestDriver0, Coordinator, Participant, Timer, Client };