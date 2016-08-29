event intTE : (int, int);
event IAEvent : (int, any);
event boolTE : (bool, bool);
event seqE : seq[int];
event seqAny : seq[any];
event unit;

machine Main {
	var rec : machine;
	var AIvar : (any, int);
	var IAvar : (int, any);
	var temp1 : (any, any);
	var tempint : (int, any);
	var tempbool : (any, bool);
	var seqInt : seq[int];
	var seqA : seq[any];
	var A : any;
	var ev : event;
	start state Init {
		entry {
			rec = new Dummy();
			raise(unit);
		}
	}
	
	state sender {
		entry {
			seqA = 1;
			AIvar = IAvar as (any, int);
			
			send rec, intTE, (1,);
			send rec, intTE, (1, 3, 5);
			send rec, intTE, true;
			send rec, intTE, temp1;
			send rec, intTE, tempint;
			
			send rec, boolTE, (1, true);
			send rec, boolTE, (true, false);
			send rec, boolTE, 4;
			send rec, boolTE, (true,);
			send rec, boolTE, temp1;
			send rec, boolTE, tempbool;
			
			send rec, seqE, seqInt;
			send rec, seqE, seqA;
			
			send rec, seqAny, seqA;
			send rec, seqAny, seqInt;
			send rec, seqAny, A;
			send rec, seqAny, 1;
			send rec, IAEvent, AIvar;
			send rec, ev, seqA;
			
			send A, ev, seqA;
			
			announce seqE, seqInt;
			announce seqAny, tempbool;
			announce seqAny;
			
			announce ev, seqE, seqInt;
			
			announce A;						
		}	
	}	
}


spec M observes seqE {
	start state x {
	
	
	}

}
