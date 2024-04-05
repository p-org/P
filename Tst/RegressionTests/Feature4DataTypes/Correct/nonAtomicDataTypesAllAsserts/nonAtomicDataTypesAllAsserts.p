//XYZs complex data types in assign/remove/insert errors): sequences, tuples, maps
//XYZs static errors
//Basic types: int, bool, event

event E assert 1;
event E1 assert 1;
event E2 assert 1;
event E3 assert 1;

machine Main {
    var t : (a: seq [int], b: map[int, seq[int]]);
	var t1 : (a: seq [int], b: map[int, seq[int]]);
	var ts: (a: int, b: int);
	var tt: (int, int);
	var te: (int, event);       ///////////////////////////////////////////////////////////
    var y : int;
	var b: bool;
	var e: event;
	var a: any;
	var tmp: int;
	var tmp1: int;
	var tmp2: (a: seq [any], b: map[int, seq[any]]);
	var tmp3: map[int, seq[int]];
	var s: seq[int];
    var s1: seq[any];
    var s2: seq[int];
    var s3: seq[seq[any]];
	var s4, s8: seq[(int,int)];
	var s5: seq[bool];
	var s6: seq[map[int,any]];
	var s7: seq[int];
	var s9: seq[event];        /////////////////////////////////////////////////////////
	var s10: seq[any];
	var s11, s12, tmp4: seq[int];
    var i: int;
	var mac: machine;
	var m1: map[int,int];
	var m4: map[int,int];
	var m3: map[int,bool];
	var m5, m6: map[int,any];
	var m2: map[int,map[int,any]];
	var m7: map[bool,seq[(a: int, b: int)]];
	var m8: map[int,event];                    //////////////////////////////////////
	var m9, m10: map[int,any];
	
    start state S
    {
       entry
       {
	      /////////////////////////default expression:
		  y = 2;
		  assert(y == 2);      //holds
		  y = default(int);
          assert(y == 0);	   //holds
		
		  b = true;
		  assert(b == true);   //holds
          b = default(bool);	
          assert(b == false);  //holds
		
		  e = E;
		  assert(e == E);       //holds
          e = default(event);	
          assert(e == null);    //holds
		
		  mac = this;
          mac = default(machine);	
          assert(mac == null);    //holds
		
		  a = true;
		  a = default(any);
		  //assert (a == null);   //error: "Value must have a concrete type"
		
		  m5[1] = true;
		  assert (m5[1] == true);  //holds
		  m5 = default(map[int,any]);
		  //assert (m5[1] == null);  //error: "key not found"
		  m5[1] = E;
		  assert (m5[1] == E);     //holds
		
	      ////////////////////////machine type:
		  mac = null as machine;                //OK
		  assert (mac == null);      //holds
		
		  /////////////////////////tuples:
		  ts.a = ts.b + 1;
		  assert (ts.a == 1 && ts.b == 0);    //holds
		  ts = (a = 1, b = 2);                 //OK
		  assert (ts.a == 1 && ts.b == 2);    //holds
		  ts = default((a: int, b: int));
		  assert(ts.a == 0 && ts.b == 0);     //holds
		
		  tt = (1,2);              //OK
		  assert(tt.0 == 1 && tt.1 == 2);   //holds
		  tt = default((int, int));
		  assert(tt.0 == 0 && tt.1 == 0);   //holds
		
		  te = (2,E2);            //OK
		  te = (3,null as event);          //OK
		  assert (te.1 == null);
		
	      /////////////////////////sequences:
		
		  s += (0, 1);
          s += (1, 2);
          s1 = s;
          s -= (1);
		
		
		  s += (0,5);
		  s += (0,6);
		  assert (s[1] == 5);
		  s -= (1);                 //removes 1st element
		  assert (sizeof(s) == 2);   //holds
		
		  s += (0,1);
		  assert(s[0] == 1);       //holds
		  s[0] = 2;
		  assert(s[0] ==2);        //holds
		  i = 0;
		  assert(s[i] == 2);       //holds
		
		  s9 += (0,E);
		  s9 += (1,E1);
		  s9 += (2,E2);
		  s9 += (3,null as event);
		  assert (sizeof(s9) == 4);   //holds
		  s10 += (0,E);                //OK
		
		  /////////////////////////sequence as payload:
		  s2 += (0,1);
          s2 += (0,3);
	      mac = new XYZ(s2);
		
		  /////////////////////////maps:
		  m1[0] = 1;
		  assert (0 in m1);      //holds
		  i = keys(m1)[0];
		  assert(i == 0);        //holds
		  assert( values(m1)[0] == 1);  //holds
		  assert(m1[0] == 1);    //holds
		  m1[0] = 2;
		  assert(m1[0] == 2);    //holds
		  m1 -= (0);
		  assert (sizeof(m1) == 0);  //holds
		  assert (sizeof(values(m1)) == 0);
		  m1[0] = 2;
		  i = 0;
		  assert(m1[i] == 2);    //holds
		  m1[1] = 3;
		  assert (sizeof(m1) == 2);
		  assert (sizeof(values(m1)) == 2);
		
		  m3[0] = true;
		  m3[2] = false;
		  assert (sizeof(m3) == 2);  //holds
		
		  m8[0] = E;                 //OK
		  m8[1] = E1;                //OK
		  m8[2] = null as event;              //OK
		
		  m9[0] = E;                  //OK
		  m9[1] = null;               //OK
		
		  /////////////////////////sequence of non-atomic types:
		  s5 += (0,true);
		  s5 += (1,false);
		  s3 += (0,s5);
		  s3 += (1,s1);
		  assert (s3[0] == s5);                  //holds
		  assert (s3[0][1] == false);           //holds
		  assert (s3[1][0] == 1);              //holds, refer to lines 65
		  assert (sizeof(s3[0]) == 2);
		
		  s3 -= (1);
		  s3 -= 0;
		  assert (sizeof(s3) == 0);   //holds
		  assert (sizeof(s5) == 2);
		
		  s1 += (0,true);
		  s3 += (0,s5);
		  s3 += (1,s1);
		  assert (s3[0] == s5);                   //holds
		  assert (s3[1][0] == true);              //holds
		  s3 = default(seq[seq[any]]);
		  //assert (s3[0] == default(seq[any]);       //parse error
		  assert (s3 == default(seq[seq[any]]));  //holds
		  //assert(s3[1][0] == null);             //index out-of-bounds
		  //assert(s3[0][0] == null);              //index out-of-bounds
		
		  s1 = default(seq[any]);
		  s3 += (0,s1);
		  assert (s3[0] == default(seq[any]));     //holds
		  //assert (s3[0][0] == null);              //index out-of-bounds
		  assert (s3[0] == s1);                  //holds
		
		  ////////////////////////sequence of maps (casting any => seq[int] is involved)
		  //s6: seq[map[int,any]];
		  m9[0] = E;                  //OK
		  m9[1] = null;               //OK
		  s6 += (0,m9);
		  s12 += (0,1);
		  s12 += (1,2);
		  m10[1] = 100;
		  m10[5] = true;
		  m10[10] =  s12;   //seq type used as "any"
		  s6 += (1,m10);
		  assert(s6[0][0] == E);    //holds
		  assert(s6[0][1] == null);  //holds
		  assert(s6[1][5] == true);  //holds
		  assert(s6[1][10] == s12);  //holds
		  //tmp4 = s6[1][10];                      //error: "invalid assignment. right hand side is not a subtype of left hand side"
		  tmp4 = s6[1][10] as seq[int];          //OK
		  assert(tmp4[0] == 1);                  //holds
		  //assert(s6[1][10][0] == 1);             // error for s6[1][10][0]: "Indexer must be applied to a sequence or map"
		  assert((s6[1][10] as seq[int])[0] == 1);   //OK
		
		  ////////////////////////tuple with sequence and map:
		  // var tmp3: map[int, seq[int]];
		  // var t : (a: seq [int], b: map[int, seq[int]]);
		  s += (0,1);
		  assert (sizeof(s) == 4);
		  tmp3[0] = s;
		  t = (a = s, b = tmp3);
		  assert (t.b[0] == s);
		  assert (t.b[0][0] == 1);
		
		  t.a += (0,2);
		  assert ( t.a[0] == 2 );         //holds
		
		  t.a += (1,2);
		  assert ( t.a[1] == 2 );         //holds
		
		  t.a += (0,3);
		  assert ( t.a[0] == 3 );         //holds
		  assert ( t.a[1] == 2 );         //holds
		  assert (sizeof(t.a) == 7);
		
		  ////////////////////////map: [bool, seq[(a: int, b: int)]]
		  //var s4, s8: seq[(int,int)];
		  s4 += (0,(0,0));
		  s4 += (1,(1,1));
		  s4 += (2, (2,2));
		  assert (sizeof(s4) == 3);            //holds
		  assert (s4[2].0 == 2);           //holds
		
		  s8 += (0,(1,1));
		  s8 += (1,(2,2));
		  s8 += (2,(3,3));
		  assert (sizeof(s8) == 3);            //holds
		  assert (s8[2] == (3,3));           //holds
		
		  assert (t.a[foo()] == 2);
          t.a[foo()] = 300;
		  assert(t.a[foo()] == 300);
		
		  tmp = foo();               // tmp = 1
		  tmp2 = GetT();             // tmp2 = t;
		  assert(tmp2.a[foo()] == 300);
		  tmp2.a[foo()] = 100;
		  assert(tmp2.a[foo()] == 100);
		  assert ( tmp2 != t);
		
		  tmp1 = IncY();              //tmp1 = 1; y 1
		  assert ( tmp1 == y);
		
		  t.a[foo()] = tmp1;
		  assert (t.a[1] == 1);
          t.a[tmp] = tmp1 + 1;
          assert (t.a[1] == 2);		
		
          y = IncY();                  // y is 2
		  assert ( y == 2 );

		  ////////////////////// WHILE:
		  // var m2: map[int,map[int,any]];
		  // var m9, m10: map[int,any];
		  // earlier:
		  // m10 += (1, 100);
		  // m10 += (5, true);
		  // m10 += (10, s12);
		
		  //m10 += (1, 100);                //error: "key must exist in map"??
		  m10[1] = 100;                       //OK
		  m10[2] = 200;
		  m10[3] = 300;
		  m10[4] = 400;
		  m10[5] = 500;
		  m2[0] = m10;
		  assert (m2[0] == m10);
		  m10[3] = 333;
		  m2[1] = m10;
		  assert (m2[1] == m10);
		  i = 1;
		  while (i < 5)
		  {
		      if (i != 3) { assert(m2[0][i] == m2[1][i]); }   //holds
		  	  else { assert(m2[0][i] != m2[1][i]); }        //holds
			  i = i + 1;
		  }
		
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
