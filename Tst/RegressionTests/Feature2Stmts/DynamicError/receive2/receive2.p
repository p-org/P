// This sample tests null event in a receive case.
event E;
event F;
event G: int;

interface all E, F, G;
main machine A implements all {
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
		send b as all, E; 
		receive { 
			case E: { x = x + p + 1; } 
			case F: { x = x + p + 2; }
			case G: { x = x + p + payload; }
			case null: { }
		}
	}
}

machine B implements all {
	start state Init {
		entry {
			var y: machine;
			var z: int;
			z = z + 1;
			y = payload as machine;
			receive {
				case E: {
					receive { 
						case G: { 
						} 
					}
				}
			}
		}
	}
}
