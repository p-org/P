event E: int;
event Unit;

main machine M {
    start state S {
		entry {
			var x: machine;
			x = new N(this);
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