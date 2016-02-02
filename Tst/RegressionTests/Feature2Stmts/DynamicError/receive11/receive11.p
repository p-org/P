//This sample tests the interaction between receive -- new and monitor.
event E : int;
event F;
event G;
event Unit;

spec M monitors E, F {
	start state Init {
		on E goto Next with (payload: int) { assert (payload == 10);};
	}
	
	state Next {
		on F goto Next with {assert false;};
	}
}

machine A {
	start state Init {
		entry {
			monitor F;
		}
	}
}

main machine B {
	var x : int;
	var m : machine;
	start state Init {
		entry {
			raise Unit;
		}
		on Unit goto X;
	}
	
	state X {
		entry {
			send this, G;
			receive {
				case G: { send this, E, 10; m = new A();}
			}
		}
		on E do {};
	}
}
