event E: int;
event Unit;

machine Main {
    start state S {
		entry {
			var x: machine;
			x = new N();
			send x, E, 0;
			send x, E, 0;
		}
	}

}

machine N {
    fun DoReceive() {
		receive {
			case E: (payload: int) {}
		}	
	}

	start state S {
		entry {
			raise Unit;
		}

		exit {
		    DoReceive();
		}
		on Unit goto T with {
			DoReceive();
		}
	}

	state T {
	}
}
