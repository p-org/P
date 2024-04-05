//XYZs tuples, sequences and maps as payloads
event myTuple : (int, bool);
event myNmTuple : (first:int, sec:bool);
event mySeq : seq[int];
event myMapSeq : (first: map[int, int], sec : seq[bool]);

machine Main {
	var INT : int;
	var BOOL : bool;
	var MACH : machine;
	var m: map[int, int];
	var s: seq[bool];
	
	fun foo_1(payload: (int, bool)) {
			INT = payload.0;
			assert ( INT == 1 );
			BOOL = payload.1;
			assert ( BOOL == true );			
	}
	
	fun foo_2(payload: (first:int, sec:bool)) {
			INT = payload.first;
			assert ( INT == 0 );
			BOOL = payload.sec;
			assert ( BOOL == false );
	}
	
	fun foo_3(payload: seq[int]) {
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
		on myTuple do (payload: (int, bool)) { foo_1(payload); }		
		on myNmTuple do (payload: (first:int, sec:bool)) { foo_2(payload); }	
		on mySeq do (payload: seq[int]) { foo_3(payload); }	
		on myMapSeq do (payload: (first: map[int, int], sec : seq[bool])) {
			//INT = payload.first[true];     //error
			INT = payload.first[0];
			assert( INT == 1 );
			BOOL = payload.sec[2];
			assert ( BOOL == true );
		}
		on halt do {
			//MACH = payload;                 //error
			//INT = payload;                  //error
		}
	}

}
