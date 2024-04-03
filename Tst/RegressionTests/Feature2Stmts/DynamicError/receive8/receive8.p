// This sample XYZs that the receives handle events in the right order.
// The assert(false) statement should be reached.
event E;
event F;
event Unit;

machine Main {
	var x: int;
	start state Init {
		entry {
			var b: machine;
		    b = new B();
			send b, F;
			send b, E;
			send b, F;
		}
	}
}

machine B {
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
						case F : { assert(false), format("Assertion 1 failed"); }
					}
				}
			}
			
		}
		on F goto X with { assert false, format("Assertion 2 failed"); }
	}
}
