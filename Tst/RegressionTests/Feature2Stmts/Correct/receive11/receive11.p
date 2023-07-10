//This sample XYZs the interaction between receive -- new and announce.
event E : int;
event F;
event G;
event Unit;

spec M observes E, F {
	start state Init {
		on E goto Next with (payload: int) { assert (payload == 10);}
	}
	
	state Next {
		on F goto Next with { /* assert false; */}
	}
}

machine A {
	start state Init {
		entry {
			announce F;
		}
	}
}

machine Main {
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
		on E do (payload: int) {}
	}
}

test DefaultImpl [main=Main]: assert M in { Main, A };

