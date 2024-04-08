//XYZs complex data types in assign/remove/insert: sequences, tuples, maps
//XYZs "insert" for sequences errors
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
    var s3: seq[seq[any]];
	var s4: seq[(int,int)];
	var s5: seq[bool];
	var s6: seq[map[int,any]];
	var s7: seq[int];
    var i: int;
	var mac: machine;
	var m1: map[int,int];
	var m3: map[int,bool];
	//TODO: write asgns for m2
	var m2: map[int,map[int,any]];
	
    start state S
    {
       entry
       {
	      ////////////////////////tuple (a: seq [int], b: map[int, seq[int]]):
		  s7 += (0,1);
		  tmp3[0] = s7;
		  //assert (tmp3[1] == s);        //fails: "P Assertion failed: Expression: assert(false)"
		  t = (a = s7, b = tmp3);
		
		  assert (t.b == tmp3);         //holds
		
		  assert (t.b[1] == s7);      //fails: "P Assertion failed: Expression: assert(false)"
		  //assert (t.b[0][0] == 1);      //holds
		
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
