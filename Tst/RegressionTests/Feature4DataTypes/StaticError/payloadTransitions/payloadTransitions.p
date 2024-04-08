event myTuple : (int, bool);
event myNmTuple : (first:int, sec:bool);
event mySeq : seq[int];
event myMapSeq : (first: map[int, int], sec : seq[bool]);
event unit;

machine Main {
	var INT : int;
	var BOOL : bool;
	var MACH : machine;
	start state Init {
		entry {
		
		}
		
		on myTuple goto State1 with (payload: (int, bool)) {
			INT = payload.0;
			BOOL = payload.1;
		}
		
		on myNmTuple goto State1 with (payload: (first:int, sec:bool)) {
			INT = payload.first;
			BOOL = payload.sec;
		}
		
		on mySeq goto State1 with (payload: seq[int]) {
			INT = payload[1];
		}
		
		on unit goto State1 with (payload: any) {
			INT = payload;
		}
		
		on myMapSeq goto State1 with (payload: (first: map[int, int], sec : seq[bool])) {
			INT = payload.first[true];
			BOOL = payload.sec[2];
		}
		




		
		on null goto State1 with (payload: any) {
			MACH = payload;
			INT = payload;
		}
	}
	
	state State1 {
	
	}

}
