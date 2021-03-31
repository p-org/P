machine Main {
	start state S {
		entry {
			var x : bool;
			var y: set[int];
			var sq: seq[int];
			var z: set[seq[int]];
			var t: map[string, string];

			x = choose();
			y += (110);
			y += (123);
			y += (111);
			y -= (1111);
			y += (111);
			
			assert sizeof(y) == 3;
			assert !(1111 in y);
			
			z += (default(seq[int]));
			z += (sq);
			assert sizeof(z) == 1;
			sq += (0, 1);
			z += (sq);
			assert sizeof(z) == 2;
			assert choose(z) in z;

			t["a"] = "b";
			t["a"] = "c";
			t["b"] = "x";
			t -= (choose(t));
			assert sizeof(t) == 1, format ("sizeof t is {0}", sizeof(t));
		}
	}
}
