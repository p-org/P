machine Main {
	var x1: int;
	start state Init {
		entry {
			x1 = foo2(null, 3);
		}
	}

	fun foo2(x: any, y: int): int {
		// return;
	}
}