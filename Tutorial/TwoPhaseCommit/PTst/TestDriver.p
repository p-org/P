
/*
This machine creates the 2 participants, 1 coordinator, and 2 clients 
*/
type t2PCSystemConfig = (
    numClients: int,
    numParticipants: int,
    numTransPerClient: int
)
fun SetUpTwoPhaseCommitSystem(config: t2PCSystemConfig)
{
    var coord : Coordinator;
    var participants: seq[Participant];
    var i : int;
    while (i < 2) {
        participants += (i, new Participant());
        i = i + 1;
    }
    coord = new Coordinator(participants);
    new Client((coor = coord, n = 2));
    new Client((coor = coord, n = 2));
}

fun InitializeTwoPhaseCommitSpecifications() {

}

machine TestDriver0 {
	start state Init {
		entry {
			var coord : Coordinator;
			var participants: seq[Participant];
			var i : int;
			while (i < 2) {
				participants += (i, new Participant());
				i = i + 1;
			}
			coord = new Coordinator(participants);
			new Client((coor = coord, n = 2));
			new Client((coor = coord, n = 2));
		}
	}
}

/*
This machine creates the 2 participants, 1 coordinator, 1 Failure injector, and 2 clients 
*/
machine TestDriver1 {
	start state Init {
		entry {
			var coord : Coordinator;
			var participants: seq[Participant];
			var i: int;
			while (i < 2) {
				participants += (i, new Participant());
				i = i + 1;
			}
			coord = new Coordinator(participants);
			new FailureInjector(participants);
			new Client((coor = coord, n = 2));
			new Client((coor = coord, n = 2));
		}
	}
}
