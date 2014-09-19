event pingme;
event local;

main model machine testMachine {
	var check:bool;
	start state init {
		entry {
			new M();
			raise(local);
		}
		on local goto doTestingNow;
	}
	
	state doTestingNow {
		entry {
			check = timeout();
			if(check)
			{
				invoke M(pingme);
			}
			raise(local);
		}
		on local goto doTestingNow;
	}
	
	model fun timeout (): bool {
		return *;
	}
}

monitor M {
	var counttimeout:int;
	start state init {
		entry {
			counttimeout = 0;
		}
		on pingme goto check;
	}
	
	state check {
		entry {
			counttimeout = counttimeout + 1;
			assert(counttimeout < 4);
		}
		on pingme goto check;
	}
}