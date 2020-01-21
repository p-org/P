event E1: (x: int, y: machine);
event E2: int;

machine Main {
	start state Init {
		entry {
			send this, E1, (x = 1, y = this);
		}
		on E1 do Foo;
		on E2 goto Init with Bar;
	}

	fun Foo(x : int, y: machine) {
		print "{0} {1}", x , y;
		send y, E2, x;
	}

	fun Bar(x: int) {
		assert x == 1;
	}
}




