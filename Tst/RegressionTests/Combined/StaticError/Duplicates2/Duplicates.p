//Duplicates: parsing errors:
//multiple: event definitions, state decls, machine declarations, variable declarations, function decls

event x;

machine Main {
	start state S1 {
	}
	state S1 {
	  on x do { }	
	}
}