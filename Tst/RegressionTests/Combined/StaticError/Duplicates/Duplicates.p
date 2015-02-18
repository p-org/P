//Duplicates: parsing errors:
//multiple: event definitions, state decls, machine declarations, variable declarations, function decls

event x;
event x: int;
event a;
event b;
event c;

main machine m1 {
	start state S1 {
	}
	state S1 {
	  on x do { foo(); };	
	}
}

main machine m1 {
	var x : int;
	var x : bool;
}

machine m2 {	
	state S1 {	
	on a goto S2;
	on b goto S1;
	on a goto S1;
	on x do { foo(); };	
	}
	state S2 {	
		on a do {};
		on a do { assert(false); };	
	}
	state S2 {	
		on a do {};	
	}
	fun foo() {}
	fun foo() :int { return 1; }
}