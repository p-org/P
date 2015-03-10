event myTuple : (int, bool);
event myNmTuple : (first:int, sec:bool);
event mySeq : seq[int];
event myMapSeq : (first: map[int, int], sec : seq[bool]);

main machine MachOS {
	var INT : int;
	var BOOL : bool;
	var MACH : machine;
	
	fun foo_1() {
			INT = payload.0; 
			BOOL = payload.1; 
		}
		
	fun foo_2() {
			MACH = payload;
			INT = payload;
		}
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
		
		on null goto State2;
	}
	
	state State1 {
		entry foo_1;
	}
	
	state State2 {
		entry foo_2;
		on myNmTuple goto State3;
		on myMapSeq goto State4;
	}
	
	state State3 {
		entry {
			INT = payload.first;
			BOOL = payload.sec;
		}
	}
	
	state State4 {
		entry foo_3;
		
	}
	
	fun foo_3() {
			INT = payload.first[true];
			BOOL = payload.sec[2];
		}
}