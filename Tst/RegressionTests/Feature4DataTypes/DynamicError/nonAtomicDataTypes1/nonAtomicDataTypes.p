//XYZs complex data types in assign/remove/insert: sequences, tuples, maps
//XYZs "index-out-of-bounds" error detection in Zinger and runtime
event E assert 1;
machine Main {
    var t : (a: seq [int], b: map[int, seq[int]]);
	var ts: (a: int, b: int);
	var tt: (int, int);
    var y : int;
	var tmp: int;
	var tmp1: int;
	var tmp2: (a: seq [any], b: map[int, seq[any]]);
	var tmp3: map[int, seq[int]];
	var s: seq[int];
    var s1: seq[any];
    var s2: seq[int];
    var i: int;
	var mac: machine;
	var m1: map[int,int];
	//TODO: write asgns for m2
	var m2: map[int,map[int,int]];
	
    start state S
    {
       entry
       {
	      /////////////////////////sequences:
		  s += (0, 1);
          s += (1, 2);
          s1 = s;
          s -= (0);
		  assert(sizeof(s) == 1);   //holds
          s -= (0);
		  assert(sizeof(s) == 0);   //holds
		
		  s -= (0);                 //Zinger/runtime error
		  assert(sizeof(s) == 0);

		  raise halt;
       }
    }

    fun foo() : int
    {
       return 1;
    }

    fun GetT() : (a: seq [int], b: map[int, seq[int]])
    {
        return t;
    }

    fun IncY() : int
    {
       y = y + 1;
       return y;
    }
}

machine XYZ {
	var ss: seq[int];
	start state init {
		entry (payload: seq[int]) {
		    ss = payload;
			assert(ss[0] == 3);            //holds
		}
		
	}
}
