//Duplicates: parsing errors:
//multiple: event definitions, state decls, machine declarations, variable declarations, function decls

machine Main {
	start state S1 {
	}
}

machine Main {
	var x : int;
	start state S1 {
	}
}
