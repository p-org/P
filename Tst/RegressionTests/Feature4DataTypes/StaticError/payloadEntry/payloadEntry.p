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
		
		on myTuple goto State1;
		
		on myNmTuple do (payload: (first:int, sec:bool)) {
			INT = payload.first;
			BOOL = payload.sec;
		}
		
		on mySeq do (payload: seq[int]) {
			INT = payload[1];
		}
		
		on myMapSeq do (payload: (first: map[int, int], sec : seq[bool])) {
			INT = payload.first[3];
			BOOL = payload.sec[2];
		}
		
		on null goto State2;
	}
	
	state State1 {
		entry (payload: (int, bool)) {
			INT = payload.0;
			BOOL = payload.1;
		}
	}
	
	state State2 {
		entry (payload: any) {
			//entered upon "null" event
			MACH = payload;    //subtype error
			INT = payload;     //subtype error
		}
		on myNmTuple goto State3;
		on myMapSeq goto State4;
	}
	
	state State3 {
		entry (payload: (first:int, sec:bool)) {
			INT = payload.first;
			BOOL = payload.sec;

		}
		on myNmTuple goto State5;
	}
	
	state State5 {
		entry (payload: (first:int, sec:bool)) {
			INT = payload.first;
			BOOL = payload;
		}
	}
	
	state State4 {
		entry (payload: (first: map[int, int], sec : seq[bool])) {
			INT = payload.first[true];
			BOOL = payload.sec[2];
		}
		
	}
}
