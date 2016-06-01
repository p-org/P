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
    fun DoReceive() {
		receive {
			case E: {}
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