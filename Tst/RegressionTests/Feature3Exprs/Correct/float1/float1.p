machine Main {
	start state S {
		entry {
			var x: float;
      		var y : map[int, float];
      		y[x to int] = 10.1;
			x = -10.1;
      		y[x to int] = x;
      		print format ("{0}:{1}:{2}\n", y[0], y[x to int] + (1 to float), x);
			assert -x == 10.1;
		}
	}
}
