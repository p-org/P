machine Main {
	var x1: int;
	start state Init {
		entry {
			x1 = foo();
		}
	}

	fun foo(x: any, y: int): int {
		return 1;
	}
}