// Testing assignments to a nested datatype when the right hand side of the assignment 
// is a side-effect free function with a nondeterministic choice inside.  

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
	
	var x: (f: (g: int));
	var i, j: int;
	var s, s1: seq[int];
	var t, t1: (a: seq [int], b: map[int, seq[int]]);
	var ts: (a: int, b: int);
	var m: map[int,int];
	//var m9, m10: map[int,any];
	//var s6: seq[map[int,any]];
	//var s3, s33: seq[seq[any]];
	
    start state S {
	    entry {
			//+++++++++++++++++++++++++++++++3.3. Index in += for map is non-det:
			t.b = default(map[int, seq[int]]);
			s = default(seq[int]);
			s1 = default(seq[int]);
			s += (0,0);
			s += (1,1);
			s1 += (0,2);
			s1 += (1,3);
			t.b += (0, s);
			//t.b += (0, s1);                 //dyn error: "key must not exist"
			i = F();
			t.b += (i, s1);                     //dynamic error: "key must not exist in map"
		}
	}
}