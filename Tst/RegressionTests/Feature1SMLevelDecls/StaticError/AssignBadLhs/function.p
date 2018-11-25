event x;
machine Main {
	start state Init {
		entry {
			x = foo();
		}
	}
	fun foo() : event {
		return x;
	}
}