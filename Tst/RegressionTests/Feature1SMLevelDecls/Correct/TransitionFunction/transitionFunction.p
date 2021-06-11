event E1: (x: int, y: machine);
event E2: int;

machine Main {
	var xn: int;
	start state Init {
		entry {
			send this, E1, (x = 1, y = this);
			xn = xn + 1;
			if(xn > 2)
				raise halt;
		}
		on E1 do Foo;
		on E2 goto Init with Bar;
	}

	fun Foo(payload: (x : int, y: machine)) {
		print format("{0} {1}", payload.x , payload.y);
		send payload.y, E2, payload.x;
	}

	fun Bar(x: int) {
		assert x == 1;
	}
}




