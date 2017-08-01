machine M {
	start state S {
		entry {
		}
	}

	fun foo(x : int) : (a: int, b: bool) {
	    var z : int;
	    z = x + 1;
		return (a = z, b = (z == 1));
	}
}
