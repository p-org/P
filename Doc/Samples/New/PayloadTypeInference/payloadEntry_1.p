event myTuple : (int, bool);
event myNmTuple : (first:int, sec:bool);
event mySeq : seq[int];
event myMapSeq : (first: map[int, int], sec : seq[bool]);

main machine MachOS {
	var INT : int;
	var BOOL : bool;
	var MACH : machine;
	start state Init {
		
		on myTuple goto State1;
		
		on myNmTuple do {
			INT = payload.first;
			BOOL = payload.sec;
		};
		
		on mySeq do {
			INT = payload[1];
		};
		
		on myMapSeq do {
			INT = payload.first[3];
			BOOL = payload.sec[2];
		};
		
		on default goto State2;
	}
	
	state State3 {
		entry {
			MACH = payload;
			INT = payload;
		}
	}
	
	state State1 {
		entry {
			INT = payload.0; 
			BOOL = payload.1; 
		}
	}
	
	state State2 {
		entry {
			MACH = payload;
			INT = payload;
		}
		on myNmTuple goto State3;
		on myMapSeq goto State4;
	}
	
	
	
	state State4 {
		entry {
			INT = payload.first[true];
			BOOL = payload.sec[2];
		}
		
	}
}