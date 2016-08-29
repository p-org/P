machine Main {
	start state S {
		entry {
			var x: int;
			var y: any;
			y = foo();
			x = (y as int) + (y as int);
			assert x == 6;
		}
	}

	fun foo() : any {
		return 3;
	}
}
