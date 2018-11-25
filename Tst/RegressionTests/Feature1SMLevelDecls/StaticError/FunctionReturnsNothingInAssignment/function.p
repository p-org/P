machine Main {
	var x1: int;
	start state Init {
		entry {
			x1 = foo3();
		}
	}
	
	fun foo3() {
	}
}
