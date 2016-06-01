event E: int;
event Unit;

main machine M {
    start state S {
		entry {
			new N(this);
			raise Unit;
		}
		exit {
			receive {
				case E: {}
			}
		}
		on Unit goto T with { 
			receive {
				case E: {}
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