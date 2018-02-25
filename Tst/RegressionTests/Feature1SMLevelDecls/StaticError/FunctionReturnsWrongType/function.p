machine Main {
    var x: int;

	start state Init {
		entry {
			x = foo();
		}
	}

	fun foo(): int {
		return true;
	}
}