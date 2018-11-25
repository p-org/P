//Duplicates: parsing errors:
//multiple: event definitions, state decls, machine declarations, variable declarations, function decls

machine m2 {	
	start state S1 {
	}
	fun foo() {}
	fun foo() :int { return 1; }
}
