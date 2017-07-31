machine Main {
	start state S {
		entry {
			var x: float;
			x = 10.0;
			assert x == foo(8.5);
		}
	}

  fun foo(x : float) : float {
    return x + 1.5;
  }
}
