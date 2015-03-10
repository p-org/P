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
		
		}
		
		on myTuple goto State1 with { 
			INT = payload.0; 
			BOOL = payload.1; 
		};
		
		on myNmTuple goto State1 with {
			INT = payload.first;
			BOOL = payload.sec;
		};
		
		on mySeq goto State1 with {
			INT = payload[1];
		};
		
		on unit goto State1 with {
			INT = payload;
		};
		
		on myMapSeq goto State1 with {
			INT = payload.first[true];
			BOOL = payload.sec[2];
		};
		
		on halt goto State1 with {
			MACH = payload;
			INT = payload;
		};
		
		on null goto State1 with {
			MACH = payload;
			INT = payload;
		};
	}
	
	state State1 {
	
	}

}