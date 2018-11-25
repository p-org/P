event a : int;

machine Main {
	start state X1 {
		entry {
			foo(5);
		}

		on null goto X2 with { }
		on a goto X1 with { }
		on a do { }
	}

	state X2 {}

	fun foo(x : int) {
		
	}
}
