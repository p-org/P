machine Main {
	start state S {
		entry {
			var x: float;
			x = 10.9;
			assert x == 10;
		}
	}
}
