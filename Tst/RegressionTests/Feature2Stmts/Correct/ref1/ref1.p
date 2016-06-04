event E: int;

main machine M {
	fun F(a ref: int, b : int) {
	    a = a + b;
	}

	var g: int;

    start state S {
	    entry {
			var y: int;
			y = 1;
			F(g ref, y);
			assert g == 1;
			assert y == 1;
			raise E, 1;
		}
		on E goto T with (i: int) { 
			assert g == 2;
			i = i + 1;
		}
		exit {
			F(g ref, 1);
		}
	}

	state T {
		entry (j: int) {
			assert j == 2;
		}
	}
}
