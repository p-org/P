machine Main {
	start state S {
		entry {
			var x: int;
      var y : map[int, float];
      y[x] = 10.1;
			x = -10;
      //y[x to int] = 1.1;
      print "{0}: {1}", y[x to int], x;
			//assert x == 10 to float;
		}
	}
}
