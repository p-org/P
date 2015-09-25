// This sample tests local variables and nested receive statements
event E: int;
event F;
event G: int;

interface All E, F, G;

main machine A implements All {
	var x: int;
	start state Init {
		entry {
			var b: I_B;
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
			case E: { x = x + p + 1; } 
			case F: { x = x + p + 2; }
			case G: { x = x + p + payload; }
		}
	}
}
interface I_B E;

machine B implements I_B 
{
	start state Init {
		entry {
			var y: machine;
			var z: int;
			z = z + 1;
			y = payload as All;
			receive {
				case E: {
					assert payload == 0;
					receive { 
						case G: { 
							var x: int;
							var a, b: int;
							var c: event;	
							x = payload;
							send y, G, x;

							a = 10;
							b = 11;
							assert b == a + z;
						} 
					}
					assert payload == 0;
				}
			}
			assert y == payload as machine;
		}
	}
}
