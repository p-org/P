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

			z += (0, 111.111);
			z += (0, 13.111);
			z += (0, 14.1511);
			z += (0, 12.1114);

			assert x == true || x == false, format ("Assertion 0 failed");
			assert choose(0) == 0 && choose(1) == 0, format ("Assertion 1 failed");
			assert choose(10) < 10 && choose(10) >= 0, format ("Assertion 2 failed");
			assert choose(y) in y, format ("Assertion 3 failed");
			assert choose(z) < 115.0 && choose(z) > 1.11, format ("Assertion 4 failed");
			assert choose(z) to int != choose(y), format ("Assertion 5 failed");

		}
	}
}
