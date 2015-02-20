//Tests complex data types in assignments: sequences, tuples, maps

main machine M
{    
    var t : (a: seq [int], b: map[int, seq[int]]);
	var s: seq[int];
    var s1: seq[any];
    
    start state S
    {
       entry
       {
	      //insert/remove:
		  s += (0, 1);
          s += (1, 2);
          s1 = s;
          s -= (0);
          s -= (0);
		  assert(sizeof(s) == 0);
		  
		  t.a += (0,1);
		  t.a += (1,2);
		  //assert ( sizeof(t.a) == 2 );
		}
	}
}
