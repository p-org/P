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
			BOOL = payload; 
	}
	
	fun foo_2() {
			INT = payload.first;
			BOOL = payload.sec;
	}
	
	fun foo_3() {
			INT = payload[1];
	}
	
	start state Init {
		entry {
		
		}
		
		on myTuple do foo_1;
		
		
		on myNmTuple do foo_2;
		
		on mySeq do foo_3;
		
		on myMapSeq do {
			INT = payload.first[true];
			BOOL = payload.sec[2];
		};
		
		on halt do {
			MACH = payload;
			INT = payload;
		};
	}

}