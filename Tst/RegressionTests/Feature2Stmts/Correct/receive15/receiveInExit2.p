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
	start state S {
		entry {
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
