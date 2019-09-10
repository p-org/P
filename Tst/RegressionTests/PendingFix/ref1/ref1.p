event E: int;

machine Main {
	fun F(a : int, b : int) {
	    a = a + b;
	}

	var g: int;

    start state S {
	    entry {
			var y: int;
			var g_local: int;
			y = 1;
			g = g_local swap;
			F(g_local swap, y);
			g = g_local move;
			assert g == 1;
			assert y == 1;
			raise E, 1;
		}
		on E goto T with (i: int) { 
			assert g == 2;
			i = i + 1;
		}
		exit {
			var g_local: int;
			g = g_local swap; 
			F(g_local swap, 1);
			g = g_local move;
		}
	}

	state T {
		entry (j: int) {
			assert j == 2;
		}
	}
}
