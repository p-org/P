//XYZs complex data types in assign/remove/insert: sequences, tuples, maps
event E assert 1;
machine Main {
    var t : (a: seq [int], b: map[int, seq[int]]);
	var t1 : (a: seq [int], b: map[int, seq[int]]);
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
		  /////////////////////////tuples:
		  ts.a = ts.b + 1;
		  assert (ts.a == 1 && ts.b == 0);     //holds
		  ts = (a = 1, b = 2);
		  ts.a = ts.b + 1;
		  assert (ts.a == 3);     //holds
		
		  ts.a = 2;
		  ts.b = 3;
		  ts.a = ts.b + 1;
		  assert (ts.a == 4);     //holds
		
		  tt.0 = 1;
		  tt.1 = 2;
		  tt.0 = tt.1 + 1;
		  assert (tt.0 == 3);     //holds
		  tt = (3,4);             //OK
		
	      /////////////////////////sequences of int/any:
		  s += (0, 1);
          s += (1, 2);

          s -= (0);
		  assert(sizeof(s) == 1);   //holds

          s -= (0);
		  assert(sizeof(s) == 0);   //holds	

		
		  s += (0,5);
		  s += (0,6);
		  assert (s[0] == 6);       //holds
		  assert (s[1] == 5);       //holds
		
		  s -= (1);
		  assert (sizeof(s) == 1);   //holds
		
		  s += (0,1);
		  assert(s[0] == 1);       //holds
		  s[0] = 2;
		  assert(s[0] ==2);        //holds
		  i = 0;
		  assert(s[i] == 2);       //holds
		
		  s1 = s;
		  s1 += (0,true);           //OK; Before fix: runtime error, but not zinger error
		
		  raise halt;
       }
    }

    fun foo() : int
    {
       return 1;
    }

    fun GetT1() : (a: seq [int], b: map[int, seq[int]])
    {
        return t1;
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
