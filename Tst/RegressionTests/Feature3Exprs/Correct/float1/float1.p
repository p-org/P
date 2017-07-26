machine Main {
	start state S {
		entry {
			var x: float;
      var y : map[int, float];
      y[x as int] = 10.1;
			//x = 10.9 as int;
			//assert x == 10;
		}
	}
}
