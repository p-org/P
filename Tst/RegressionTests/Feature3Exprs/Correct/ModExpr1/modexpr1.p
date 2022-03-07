machine Main {
	start state S {
		entry {
			var x, y : int;
			var z, w : float;
			var ys: set[int];
			x = choose(100);
			ys += (113);
			ys += (123);
			ys += (113);

			z = 10.123;
			w = 10.123;

			assert (x % 2 == 0) || (x % 2 == 1), "1";
			assert choose(ys) % 10 == 3, "2";
			assert z % 12.55 == w % 12.55, "3";
			assert 10.22 % 1.34 > 0.84, "4";

		}
	}
}
