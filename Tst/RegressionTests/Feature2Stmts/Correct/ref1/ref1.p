event E: int;

machine Main {
	fun F(a : int, b : int) {
	    a = a + b;
	}

	var g: int;

    start state S {
	    entry {
			var y: int;
			y = 1;
			F(g swap, y);
			assert g == 1;
			assert y == 1;
			raise E, 1;
		}
		on E goto T with (i: int) { 
			assert g == 2;
			i = i + 1;
		}
		exit {
			F(g swap, 1);
		}
	}

	state T {
		entry (j: int) {
			assert j == 2;
		}
	}
}
