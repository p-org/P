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
	      /////////////////////////sequences:
		  s += (0, 1);
          s += (1, 2);
          s1 = s;
          s -= (0);
		  assert(sizeof(s) == 1);   //holds
          s -= (0);
		  assert(sizeof(s) == 0);   //holds	
		
		  s += (0,5);
		  s += (0,6);
		  assert (s[0] == 6);       //holds
		  assert (s[1] == 5);       //holds
		  assert(sizeof(s) == 2);   //holds
		
		  /////////////////////////maps:
		  m1[0] = 1;
		  assert (0 in m1);      //holds
		  i = keys(m1)[0];
		  assert(i == 0);        //holds
		  assert(m1[0] == 1);    //holds
		  m1[0] = 2;
		  assert(m1[0] == 2);    //holds
		  m1 -= (0);
		  assert (sizeof(m1) == 0);  //holds
		  m1[0] = 2;
		  i = 0;
		  assert(m1[i] == 2);    //holds
		  m1[1] = 3;
		
		  m3[0] = true;
		  m3[2] = false;
		  assert (sizeof(m3) == 2);  //holds
		
		  /////////////////////////sequence of non-atomic types:
		  s6 += (0,m1);
		  assert (s6[0] == m1);      //holds
		  assert (s6[0][0] == 2);    //holds
		  s6 += (1,m3);
		  assert (sizeof(s6) == 2);   //holds
		  assert (keys(s6[0])[2] == 1);  //0 <= index && index <= size
		
		  assert (s6[1][2] == false);     //holds
		  s6[1][2] = true;
		  assert (s6[1][2] == true);     //holds
		
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
