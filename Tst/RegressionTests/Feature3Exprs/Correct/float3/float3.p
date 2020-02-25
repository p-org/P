machine Main {
	start state S {
		entry {
			var x: float;
			x = 10.0;
			print format("{0} == {1}\n", bar(9), foo(8.5));
		}
	}

  fun foo(x : float) : float {
    return x + 1.5;
  }

  fun bar(x : int) : int {
    return ((x to float) + 1.4) to int;
  }
}
