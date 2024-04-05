// This sample XYZs local variables and nested receive statements
event E: int;
event F;
event G: int;

machine Main {
	var x: int;
	start state Init {
		entry {
			var b: machine;
		    b = new B(this);
			foo(b, 0);
		}
	}
	fun foo(b: machine, p: int) {
		receive {
			case E: (payload: int) { assert false; }
		}
	}
}

machine B {
	start state Init {
		entry (p: machine) {
			send p, halt;
			send p, E, 100;
		}
	}
}
