machine Main {
	start state S {
		entry {
			var x : bool;
			var y: set[int];
			var z: seq[float];
			x = choose();
			y += (110);
			y += (123);
			y += (111);
			y -= (1111);

			z += (0, 1.111);
			z += (0, 13.111);
			z += (0, 14.1511);
			z += (0, 12.1114);

			assert x == true || x == false;
			assert choose(0) == 0 && choose(1) == 0;
			assert choose(10) < 10 && choose(10) >= 0;
			assert choose(y) in y;
			assert choose(z) < 15.0 && choose(z) > 1.11;

		}
	}
}
