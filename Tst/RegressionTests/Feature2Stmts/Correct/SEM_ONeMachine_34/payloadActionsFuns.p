//Tests tuples, sequences and maps as payloads
event myTuple : (int, bool);
event myNmTuple : (first:int, sec:bool);
event mySeq : seq[int];
event myMapSeq : (first: map[int, int], sec : seq[bool]);

interface all myTuple, myMapSeq, myNmTuple, mySeq;

main machine MachOS implements all {
	var INT : int;
	var BOOL : bool;
	var MACH : machine;
	var m: map[int, int];
	var s: seq[bool];
	
	fun foo_1() { 
			INT = payload.0; 
			assert ( INT == 1 );
			BOOL = payload.1;  
			assert ( BOOL == true );			
	}
	
	fun foo_2() {
			INT = payload.first;
			assert ( INT == 0 );
			BOOL = payload.sec;
			assert ( BOOL == false );
	}
	
	fun foo_3() {
			INT = payload[1];
	}
	
	start state Init {
		entry {
			m[0] = 1;
			m[1] = 2;
			s += (0, true);
			s += (1, false);
			s += (2, true);
			send this, myTuple, (1, true);
			send this, myNmTuple, (first = 0, sec = false);
			send this, myMapSeq, (first = m, sec = s);
		
		}	
		on myTuple do foo_1;		
		on myNmTuple do foo_2;	
		on mySeq do foo_3;	
		on myMapSeq do {
			//INT = payload.first[true];     //error
			INT = payload.first[0];
			assert( INT == 1 );
			BOOL = payload.sec[2];
			assert ( BOOL == true );
		};
		on halt do {
			//MACH = payload;                 //error
			//INT = payload;                  //error
		};
	}

}