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
			x = x + 1;
			assert x == 1;
			foo(b, 0);
			assert x == 2;
		}
	}
	fun foo(b: machine, p: int) {
		send b, E, 0;
		send b, G, 1;
		receive {
			case E: (payload: int) { x = x + p + 1; }
			case F: { x = x + p + 2; }
			case G: (payload: int) { x = x + p + payload; }
		}
	}
}

machine B {
	start state Init {
		entry (payload1: machine) {
			var y: machine;
			var z: int;
			z = z + 1;
			y = payload1;
			receive {
				case E: (payload2: int) {
					assert payload2 == 0;
					receive {
						case G: (payload3: int) {
							var x: int;
							var a, b: int;
							var c: event;	
							x = payload3;
							send y, G, x;

							a = 10;
							b = 11;
							assert b == a + z;
						}
					}
					assert payload2 == 0;
				}
			}
			assert y == payload1;
		}
	}
}
