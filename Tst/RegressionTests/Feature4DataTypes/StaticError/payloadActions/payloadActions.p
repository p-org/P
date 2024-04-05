event myTuple : (int, bool);
event myNmTuple : (first:int, sec:bool);
event mySeq : seq[int];
event myMapSeq : (first: map[int, int], sec : seq[bool]);

machine Main {
	var INT : int;
	var BOOL : bool;
	var MACH : machine;
	start state Init {
		entry {
		
		}
		
		on myTuple do (payload: (int, bool)) {
			INT = payload.0;
			BOOL = payload.1;
		}
		
		on myNmTuple do (payload: (first:int, sec:bool)) {
			INT = payload.first;
			BOOL = payload.sec;
		}
		
		on mySeq do (payload: seq[int]) {
			INT = payload[1];
		}
		
		on myMapSeq do (payload: (first: map[int, int], sec : seq[bool])) {
			INT = payload.first[true];
			BOOL = payload.sec[2];
		}
		




		on null do (payload: any) {
			MACH = payload;  //subtype error
			INT = payload;   //subtype error
		}
	}

}
