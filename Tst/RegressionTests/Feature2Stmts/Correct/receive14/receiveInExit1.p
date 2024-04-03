event E: int;
event Unit;

machine Main {
    start state S {
		entry {
			new N(this);
			raise Unit;
		}
		exit {
			receive {
				case E: (payload: int) {}
			}
		}
		on Unit goto T with {
			receive {
				case E: (payload: int) {}
			}
		}
	}

	state T {
	}
}

machine N {
	start state S {
		entry (t: machine) {
			send t, E, 0;
			send t, E, 0;
		}
	}
}
