// Bug repro in PtoZing translation: assignment to a nested datatype when the right hand side of the assignment 
// is a side-effect free function with a nondeterministic choice inside.  
main machine M {
    fun F() : int {
	    if ($) {
		    return 0;
		} else {
		    return 1;
		}
	}
	var x: (f: (g: int));
    start state S {
	    entry {
		    x.f.g = F();
			assert (x.f.g == 0 || x.f.g == 1);   //holds
		}
	}
}