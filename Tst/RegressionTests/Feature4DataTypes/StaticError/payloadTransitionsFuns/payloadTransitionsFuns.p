event myTuple : (int, bool);
event myNmTuple : (first:int, sec:bool);
event mySeq : seq[int];
event myMapSeq : (first: map[int, int], sec : seq[bool]);
event unit;

main machine MachOS {
	var INT : int;
	var BOOL : bool;
	var MACH : machine;
	fun foo_1(){ 
			INT = payload.0; 
			BOOL = payload.1; 
		}
	
	fun foo_2() {
			INT = payload.first;
			BOOL = payload.sec;
		}	
	
	fun foo_3 () {
			INT = payload[1];
		}

	fun foo_4 () {
			INT = payload;
		}
	
	fun foo_5(){
			INT = payload.first[true];
			BOOL = payload.sec[2];
		}
	
	fun foo_6()  {
			MACH = payload;
			INT = payload;
		}
		
	start state Init {
		entry {
		
		}
		
		on myTuple goto State1 with foo_1;
		
		on myNmTuple goto State1 with foo_2;
		
		on mySeq goto State1 with foo_3 ;
		
		on unit goto State1 with foo_4 ;
		
		on myMapSeq goto State1 with foo_5;
		
		on halt goto State1 with foo_6;
		
		on null goto State1 with foo_6;
	}
	
	state State1 {
	
	}

}