// This sample is XYZing the case when the deadlocked state is not the only state on the stack
// State "Call" is deadlocked, b/c the Zing reports "passed", hence, "assert" on line 26
// is not reachable, which means that the "Call" state is never popped
// In the deadlocked state, stack contains Init and Call states 
event E;

machine Main {
	var i: int;
	start state Init {
		entry {
			i = 0;
			raise E;
		}

		exit {
			//unreachable
			assert(false);
		}
		
		on E push Call;
	}

	state Call {
		entry {
			if (i == 0) {
				raise E;
			} else {
				i = i + 1;
			}
		}

		ignore E;

		exit {
			//unreachable, which means that the state is not popped
			assert(false);
		}
	}
}