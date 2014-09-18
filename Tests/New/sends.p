event intTE : (int, int);
event boolTE : (bool, bool);
event seqE : seq[int];
event seqAny : seq[any];
event unit;

main machine Dummy {
	var rec : machine;
	var temp1 : (any, any);
	var tempint : (int, any);
	var tempbool : (any, bool);
	var seqInt : seq[int];
	var seqA : seq[any];
	var A : any;
	start state Init {
		entry {
			rec = new Dummy();
			raise(unit);
		}
	}
	
	state sender {
		entry {
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
			send rec, seqE, seqAny;
			
			send rec, seqAny, seqAny;
			send rec, seqAny, seqInt;
			send rec, seqAny, A;
			send rec, seqAny, 1;
			
			monitor M, seqE, seqInt;
			monitor M, seqAny, tempbool;
			
		}
	
	}
	

}


monitor M {
	start state x {
	
	
	}

}