// This sample XYZs if the events sent to 'this' are handled correctly and the payload or different types are dequeued correctly.
// The assert false on line 33 should be hit because of the raise on line 27.
event E;
event F : int;
event G: seq[int];
event Unit;

machine Main {
	var x : int;
	start state Init {
		entry {
			raise Unit;
		}
		on Unit goto X;
	}
	
	state X {
		entry {
			send this, F, 10;
			receive {
				case F: (payload1: int) { 	
					assert(payload1 == 10);
					send this, G, default(seq[int]);
					receive {
						case G : (payload2: seq[int]) { assert(sizeof(payload2) == 0); raise E;}
					}
				}
			}
			
		}
		on E goto X with { assert (false);}
	}
}
