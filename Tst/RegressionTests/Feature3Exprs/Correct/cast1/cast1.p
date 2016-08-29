machine Main {
	start state S {
		entry {
			var x: int;
			x = (foo() as int) + (foo() as int);
			assert x == 6;
		}
	}

	fun foo() : any {
		return 3;
	}
}
