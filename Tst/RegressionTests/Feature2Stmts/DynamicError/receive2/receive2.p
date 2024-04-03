// This sample XYZs null event in a receive case.
event E;
event F;
event G: int;

machine Main {
	var x: int;
	start state Init {
		entry {
			var b: machine;
		    b = new B(this);
			x = x + 1;
			assert x == 1;
			foo(b, 0);
			assert x == 2;
		}
	}
	fun foo(b: machine, p: int) {
		send b, E;
		receive {
			case E: { x = x + p + 1; }
			case F: { x = x + p + 2; }
			case G: (payload: int) { x = x + p + payload; }
			case null: { }
		}
	}
}

machine B {
	start state Init {
		entry (payload: machine) {
			var y: machine;
			var z: int;
			z = z + 1;
			y = payload;
			receive {
				case E: {
					receive {
						case G: (x: int) {
						}
					}
				}
			}
		}
	}
}
