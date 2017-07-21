machine Main {
	start state Entry {
		entry {
			var x: (a: int, b: bool);
			var y: (a: int, b: bool);
			x = (a = 1, b = true);
			y = x;
		}
	}
}