//Duplicates: parsing errors:
//multiple: event definitions, state decls, machine declarations, variable declarations, function decls

event a;

machine m2 {
	start state S2 {	
		on a do {}
		on a do { assert(false); }	//unreachable
	}
	state S2 {	
		on a do {}	
	}
	fun foo() {}
}
