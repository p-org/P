event myTuple : (int, bool);
event myNmTuple : (first:int, sec:bool);
event mySeq : seq[int];
event myMapSeq : (first: map[int, int], sec : seq[bool]);

machine Main {
	var INT : int;
	var BOOL : bool;
	var MACH : machine;
	
	fun foo_1(payload: (int, bool)) {
			INT = payload.0;
			BOOL = payload;
	}
	
	fun foo_2(payload: (first:int, sec:bool)) {
			INT = payload.first;
			BOOL = payload.sec;
	}
	
	fun foo_3(payload: seq[int]) {
			INT = payload[1];
	}
	
	start state Init {
		entry {
		
		}
		
		on myTuple do (payload: (int, bool)) { foo_1(payload); }
		
		
		on myNmTuple do (payload: (first:int, sec:bool)) { foo_2(payload); }
		
		on mySeq do (payload: seq[int]) { foo_3(payload); }
		
		on myMapSeq do (payload: (first: map[int, int], sec : seq[bool])) {
			INT = payload.first[true];
			BOOL = payload.sec[2];
		}
		




		
		on null do (payload: any) {
			MACH = payload;
			INT = payload;
		}
		
	}

}
