machine Main {
	start state S {
		entry {
			var x: float;
      var y : map[int, float];
      y[x to int] = 10.1;
			x = -10.9;
      y[x to int] = x;
      print "{0}: {1}", y[x], x;
			//assert x == 10 to float;
		}
	}
}
