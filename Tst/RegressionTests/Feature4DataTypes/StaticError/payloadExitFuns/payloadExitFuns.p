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
		
		
	}
	fun foo_1 (){
			INT = payload.0; 
			BOOL = payload.1; 
		}
		
	state State1 {
		on myTuple goto State1;
		exit foo_1;
	}
	
	fun foo_2() {
			MACH = payload;
			INT = payload;
		}
		
	state State2 {
		on null goto State2;
		exit foo_2;
		
		
	}
	
	fun foo_3() {
			INT = payload.first;
			BOOL = payload.sec;
		}
		
	state State3 {
		on myNmTuple goto State3;
		exit foo_3;
	}
	
	fun foo_4() {
			INT = payload.first[true];
			BOOL = payload.sec[2];
		}
		
	state State4 {
		on myMapSeq goto State4;
		exit foo_4;
		
	}
	
	fun foo_5() {
			INT = payload.first;
			BOOL = payload.sec;
		}
		
	state State5 {
		on myMapSeq goto State4;
		on myNmTuple goto State3;
		exit foo_5;
	}
}