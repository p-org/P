// This sample XYZs raise of event inside receive.
event E;
event F;

machine Main {
	var x: int;
	start state Init {
		entry {
			x = x + 1;
			assert x == 1;
			foo();
			assert x == 2;
		}
	}
	fun foo() {
		send this, E;
		receive {
			case E: { bar(); }
		}
		receive {
			case F: { }
		}
		x = x + 1;
	}
	fun bar() {
		raise F;
	}
}

