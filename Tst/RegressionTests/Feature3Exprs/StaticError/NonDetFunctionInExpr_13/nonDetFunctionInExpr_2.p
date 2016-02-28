// Testing assignments to a nested datatype when the right hand side of the assignment 
// is a side-effect free function with a nondeterministic choice inside.  
// Includes tests for tuples, maps and sequences

main machine M {
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