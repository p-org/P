machine Main {
	start state S {
		entry {
			var x, y : string;

			assert (x % 2 == 0) || (x % 2 == 1), "1";

		}
	}
}
