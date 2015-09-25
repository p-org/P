// This sample tests that the receives handle events in the right order.
// The assert(false) statement should be reached.
event E;
event F;
event Unit;

interface I E, F;
main machine A {
	var x: int;
	start state Init {
		entry {
			var b: I;
		    b = new B(this);
			send b, F;
			send b, E;
			send b, F;
		}
	}
}

machine B implements I {
	var x : int;
	start state Init {
		entry {
			raise Unit;
		}
		on Unit goto X;
	}
	
	state X {
		entry {
			receive {
				case E: { x = x + 1;}
			}
			receive {
				case F: { 	
					assert(x ==  1); x = x + 1; 
					receive {
						case F : { assert(false);}
					}
				}
			}
			
		}
		on F goto X with { assert false; };
	}
}
