event myTuple : (int, bool);
event myNmTuple : (first:int, sec:bool);
event mySeq : seq[int];
event myMapSeq : (first: map[int, int], sec : seq[bool]);

main machine MachOS {
	var INT : int;
	var BOOL : bool;
	var MACH : machine;
	start state Init {
		entry {
		
		}
		
		on myTuple do { 
			INT = payload.0; 
			BOOL = payload.1; 
		};
		
		on myNmTuple do {
			INT = payload.first;
			BOOL = payload.sec;
		};
		
		on mySeq do {
			INT = payload[1];
		};
		
		on myMapSeq do {
			INT = payload.first[true];
			BOOL = payload.sec[2];
		};
		
		on halt do {
			MACH = payload;
			INT = payload;
		};
		on null do {
			MACH = payload;  //subtype error
			INT = payload;   //subtype error
		};
	}

}