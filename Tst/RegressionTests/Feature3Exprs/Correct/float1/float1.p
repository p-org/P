machine Main {
	start state S {
		entry {
			var x: float;
			x = float(10);
			assert x == 10;
		}
	}
}
