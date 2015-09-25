// This sample tests that a receive statement ignores the event handlers of the enclosing context.
event E;
event F;
event Unit;

main machine A {
	var x: int;
	start state Init {
		entry {
			var b: I_B;
		    b = new B(this);
			send b, F;
		}
	}
}

interface I_B E, F;
machine B implements I_B {
	start state Init {
		entry {
			raise Unit;
		}
		on Unit goto X;
	}
	
	state X {
		entry {
			receive {
				case E: { }
				case null: { }
			}
		}
		on F goto X with { assert false; };
	}
}
