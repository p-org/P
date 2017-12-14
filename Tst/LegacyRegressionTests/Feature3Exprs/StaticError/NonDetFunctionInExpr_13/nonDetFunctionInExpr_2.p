// XYZing assignments to a nested datatype when the right hand side of the assignment 
// is a side-effect free function with a nondeterministic choice inside.  
// Includes XYZs for tuples, maps and sequences

machine Main {
    fun F() : int {
	    if ($) {
		    return 0;
		} else {
		    return 1;
		}
	}
	
	fun foo() : int
    {
       return 1;
    }   
	
	var i, j: int;
	
    start state S {
	    entry {
			i = F() + 1;                    //static error
			
		}
	}
}
