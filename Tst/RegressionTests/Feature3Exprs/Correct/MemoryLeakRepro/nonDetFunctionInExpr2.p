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
	
	var t: (a: seq [int], b: map[int, seq[int]]);
	var i: int;
	
    start state S {
	    entry {

			t.a += (0,2);
			t.a += (1,2);
			i = F();
			t.a -=(i);                 //no memory leak
			
			//t.a -= (foo());          //memory leak due to this line
		}
	}
}
