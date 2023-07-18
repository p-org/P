machine Main {
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
		}
	}
}
