//XYZs complex data types in assign/remove/insert: sequences, tuples, maps
event E assert 1;
machine Main {
    var t, t1, tmp4: (a: seq [int], b: map[int, seq[int]]);
	var ts: (a: int, b: int);
	var tt: (int, int);
    var y, tmp, tmp1, i: int;
	var tmp2: (a: seq [any], b: map[int, seq[any]]);
	var tmp3: map[int, seq[int]];
	var s, s2, s7: seq[int];
    var s1: seq[any];
    var s3: seq[seq[any]];
    var s4, s8: seq[(a: int, b: int)]; 	
	var s5: seq[bool];
	var s6: seq[map[int,any]];
	var mac: machine;
	var m1, m4: map[int,int];
	var m3: map[int,bool];
	//TODO: write asgns for m2
	var m5, m6: map[int,any];
	var m2: map[int,map[int,any]];
	var m7: map[bool,seq[(a: int, b: int)]];
	
    start state S
    {
       entry
       {
	      /****************************************/
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
		
		  tt.0 = ts.b;            //OK
		  assert (tt.0 == 3);     //holds
		
		  ts.b = tt.0 + 1;        //OK
		  assert (ts.b == 4);     //holds
		
	      /////////////////////////sequences of int/any:
		  s += (0, 1);
          s += (1, 2);

          s -= (0);
		  assert(sizeof(s) == 1);   //holds
		  //assert (s[1] == 1);        //"index out-of-bounds" error by zinger and runtime
          s -= (0);
		  assert(sizeof(s) == 0);   //holds	
		  //s -= (0);                 //Zing/runtime error
		
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
		
		  s1 += (0,true);
		  s1 += (1,false);
		  //s1 += (1,1);
		  assert (sizeof(s1) == 2);   //holds
		  s1 += (0,1);             //OK
		
		  s5 += (0, true);
		  s5 += (1, false);
		  assert (sizeof(s5) == 2);   //holds
		
		  s1 = s;
		  assert(s1[0] == 2);        //holds
		
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
		
		  m4 = m1;
		  assert (m4[i] == 2);          //holds
		
		  /////////////////////////sequence of non-atomic types:
		  ////seq of seq's:
		  s3 += (0,s5);
		  s3 += (1,s1);
		  assert (s3[0] == s5);                  //holds
		  assert (s3[1] == s1);                  //holds
		  assert (s3[1][0] == 2);              //holds
		
		  s3[1] = s5;
		  assert (s3[1][0] == true);          //holds
		  assert (sizeof(s3) == 2);           //holds
		
		  s3 -= (1);
		  s3 -= 0;
		  assert (sizeof(s3) == 0);   //holds
		
		  ////seq of maps:
		  s6 += (0,m1);
		  assert (s6[0] == m1);      //holds
		  assert (s6[0][0] == 2);    //holds
		  s6 += (1,m3);
		  assert (sizeof(s6) == 2);   //holds
		  assert (keys(s6[0])[1] == 1);  //holds
		
		  assert (s6[1][2] == false);     //holds
		  s6[1][2] = true;
		  assert (s6[1][2] == true);     //holds
		
		  s6[1][3] = true;               //OK
		  assert (sizeof(s6[1]) == 3);   //holds
		
		  s6[1] = m1;
		  assert (s6[1][0] == 2);       //holds
		
		  /////////////////////////sequence as payload:
		  s2 += (0,1);
          s2 += (0,3);
	      mac = new XYZ(s2);
		
		  ////////////////////////tuple (a: seq [int], b: map[int, seq[int]]):
		  s7 += (0,1);
		  tmp3[0] = s7;
		  t = (a = s7, b = tmp3);
		  assert (sizeof(t.a) == 1);      //holds
		
		  t.a += (0,2);
		  assert ( t.a[0] == 2 );         //holds
		  assert (sizeof(t.a) == 2);      //holds
		
		  t.a += (1,3);
		  assert ( t.a[1] == 3 );         //holds
		  assert ( t.a[2] == 1 );         //holds
		  assert (sizeof(t.a) == 3);      //holds
		
		  t.a += (0,5);
		  assert ( t.a[0] == 5 );         //holds
		  assert ( t.a[1] == 2 );         //holds
		  assert ( t.a[3] == 1 );         //holds
		
		  assert ( sizeof(t.a) == 4 );     //holds
		
		  assert (t.b == tmp3);         //holds
		  assert (t.b[0] == s7);        //holds
		  assert (t.b[0][0] == 1);      //holds
		
		  tmp3[0] = s2;
		  t.b = tmp3;
		  assert (t.b[0][0] == 3);       //holds
		  assert (t.b[0][1] == 1);       //holds
		
		  ////////////////////////map: [int,map[int,any]]
		  m5[0] = 1;
		  m5[1] = false;
		  m5[2] = 2;
		  m5[3] = true;
		  i = keys(m5)[0];
		  assert (i == 0);           //holds
		  i = keys(m5)[3];
		  assert (i == 3);           //holds
		
		  m6 = m5;
		  m6[3] = 5;
		
		  m2[0] = m5;
		  m2[1] = m6;
		  assert (m2[0][0] == m2[1][0]);       //holds
		  assert (m2[0][3] != m2[1][3]);       //holds
		
          i = 0;
		  while (i < 4)
		  {
		      if (i != 3) { assert(m2[0][i] == m2[1][i]); }   //holds
		  	  else { assert(m2[0][i] != m2[1][i]); }        //holds
			  i = i + 1;
		  }
		
		  ////////////////////////map: [bool, seq[(a: int, b: int)]]
		  //var s4, s8: seq[(int,int)];
		  s4 += (0,(a=0,b=0));
		  s4 += (1,(a=1,b=1));
		  s4 += (2, (a=2,b=2));
		
		  s8 += (0,(a=1,b=1));
		  s8 += (1,(a=2,b=2));
		  s8 += (2,(a=3,b=3));
		
		  m7[true] = s4;
		  m7[false] = s8;

		  assert (m7[true][0] == (a=0,b=0));       //holds
		  assert (m7[false][2] == (a=3,b=3));      //holds
		
		  /************************************/
		  ///////////////////////////////////////////////////////////////////////////
		
          //t1.a[foo()-1] = 2;        //runtime error
		  tmp = foo();
		  tmp4 = GetT1();             //Before the fix: StackOverflowException!
          //tmp4.a[tmp] = 1;
		
		  //tmp2 = GetT1();
		  //assert ( tmp2 == t1) ;    //holds?
		   //TODO: uncomment below
		  //tmp2.a[foo()-1] = 1;
		  //assert ( tmp2 != t1);   //holds?
		  //tmp1 = IncY();
		  //assert ( tmp1 == y + 1); //holds?
		  //t.a[foo()] = tmp1;
          //t.a[tmp] = tmp1;
          //y = IncY();
		  //assert ( y == 2 );       //holds?
		  ////////////////////////tuple with sequence and map:
		
		  /***********************************/
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
