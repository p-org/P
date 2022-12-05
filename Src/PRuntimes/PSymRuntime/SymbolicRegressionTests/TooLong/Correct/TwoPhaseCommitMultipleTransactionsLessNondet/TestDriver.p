machine Main {
	start state Init {
		entry {
			var coord : machine;
			coord = new Coordinator(2);
			new Client(coord);
			new Client(coord);
		}
	}
}
