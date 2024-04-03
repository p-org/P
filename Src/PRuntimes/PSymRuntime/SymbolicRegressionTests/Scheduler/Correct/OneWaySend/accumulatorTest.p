event ACC: int;

machine Main
sends ACC;
{
	var accumulator: AccumulatorMachine;
	var numIterations: int;

	start state Init {
		entry {
			numIterations = 10;
			accumulator = new AccumulatorMachine();
			goto Send;
		}
	}


	state Send {
		entry {
			if (numIterations == 0) {
				goto Stop;
			} else if (numIterations > 0) {
        		numIterations = numIterations - 1;
      		}
      		send accumulator, ACC, 0;
      		goto Send;
		}
	}

	state Stop {
		entry {
			print "done";
		}
	}
}

machine AccumulatorMachine
receives ACC;
{
	var pool: int;
	start state Init {
		entry {
			pool = 0;
			goto Wait;
		}
	}

	state Wait {
		on ACC goto Accumulate;
	}

	state Accumulate {
		entry {
			print "accumulated {0} ", pool;
			pool = pool + 1;
			goto Wait;
		}
	}
}
