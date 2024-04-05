// This sample captures the behavior of null inside receive. This regression should be invoked with the -m option in zing.
// The traces reported should contain only 2 traces, assert failure at line 42 or unhandled event exception.
// This test is identical to receive10, but comments out one of the asserts to allow one of two failures in machine B,
// depending on a race between the send in machine Main and the execution of the null transition in machine B.

event E : int;
event F;
event Unit;

machine Main {
	var x: int;
	start state Init {
		entry {
			var b: machine;
		    b = new B();
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
				case F: {
					send this, E, 10;
					assert(false);
				}
				case null : {raise F;}
			}
		
			
		}
		on F goto X with { assert false;}   //fails
	}
}
