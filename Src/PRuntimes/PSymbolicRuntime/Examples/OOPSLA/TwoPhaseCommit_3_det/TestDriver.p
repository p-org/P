machine Main {
	start state Init {
		entry {
			var coord : Coordinator;
            var participants: seq[Participant];
            var i : int;
            while (i < 3) {
                participants += (i, new Participant());
                i = i + 1;
            }
            coord = new Coordinator(participants);
			new TestClient(coord);
		}
	}
}
