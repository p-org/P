event res: int;
event getres: machine;
event e1;
event e2;
event e3;

fun Dummy(m : machine, n: machine) {
	send m, getres, n;
	send m, e2;
	send m, e3;
	receive {
		case res: (payload: int) { send m, getres, m; }
	}
}

machine N {
	start state S {
		entry {
			receive {
				case e2: { assert true; assert false; }
			}
		}
		on getres do (payload: machine){
			receive {
				case e3: { assert true; assert false; assert 1 == 1; }
			}
			send payload, res, 1; }
	}
}
