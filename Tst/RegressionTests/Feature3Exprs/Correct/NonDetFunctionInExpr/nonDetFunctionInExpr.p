// Bug repro in PtoZing translation: assignment to a nested datatype when the right hand side of the assignment
// is a side-effect free function with a nondeterministic choice inside. 
machine Main {
    fun F() : int {
	    if ($) {
		    return 0;
		} else {
		    return 1;
		}
	}
	var x: (f: (g: int));
	var i: int;
    start state S {
	    entry {
			i = F();
			x.f.g = i;
		    //x.f.g = F();
			assert (x.f.g == 0 || x.f.g == 1);   //holds
		}
	}
}
