event myTuple : (int, bool);
event myNmTuple : (first:int, sec:bool);
event mySeq : seq[int];
event myMapSeq : (first: map[int, int], sec : seq[bool]);
event unit;
main machine MachOS {
	var INT : int;
	var BOOL : bool;
	var MACH : machine;
	start state Init {
		entry {
			INT = payload.1;
		}
		//on myNmTuple goto CalledState;
		on myTuple goto Init;
	}
	
	state X {
		entry {

		}
	}
	state CalledState {
		entry {
			MACH = payload;
			INT = payload.first;
			BOOL = payload.sec;
		}
	
	}
}
