// This sample tests if the events sent to 'this' are handled correctly and the payload or different types are dequeued correctly.
// The assert false on line 33 should be hit because of the raise on line 27.
event E : int;
event F;
event G: seq[int];
event Unit;

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
				case E: { send this, F, 10;}
			}
			receive {
				case F: { 	
					assert(payload ==  10); 
					send this, G, default(seq[int]);
					receive {
						case G : { assert(sizeof(payload) == 0); raise F;}
					}
				}
			}
			
		}
		on F goto X with { assert false; // should end here.};
	}
}
