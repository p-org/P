fun Dummy(m : machine) {
	send m, getres, this;
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
