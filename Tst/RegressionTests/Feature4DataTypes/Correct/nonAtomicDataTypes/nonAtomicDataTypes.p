//XYZs complex data types in assign/remove/insert errors): sequences, tuples, maps
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
    var s3, s33: seq[seq[any]];
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
	//TODO: write asgns for m2
	var m5, m6: map[int,any];
	var m2: map[int,map[int,any]];
	var m7: map[bool,seq[(a: int, b: int)]];
	var m8: map[int,event];                    //////////////////////////////////////
	var m9, m10: map[int,any];
	var m11: map[any,any];
	
    start state S
    {
       entry
       {
	      /////////////////////////default expression:
		  y = 2;
		  y = default(int);
		
		  b = true;
          b = default(bool);	

		  e = E;
          //e = default(event);	
		
		  mac = this;          mac = default(machine);	
		
		  a = true;
		  //a = default(any);
		
		  m5[1]  = true;
		  m5 = default(map[int,any]);
		  m5[1] = E;
		  /////////////////////////tuples:
		  //ts = (a = 1, b = 2);
		  //ts = (a = 1);           //parsing error
		  //ts = (5);                 //error
		  ts.b = 1;
		  ts.a = ts.b + foo();	   //non-primitive expr in RHS; OK?
		  ts = default((a: int, b: int));
		
		  tt = (1, foo() + 1);     //OK
		
		  tt = default((int, int));
		
		  te = (2,E2);            //OK
		  te = (3,bar());         //OK
		  te = (4,null as event);          //OK
		
	      /////////////////////////sequences:
		  s += (0, 1);
          s += (1, 2);
          s1 = s;
          s -= (1);
		
		
		  s += (0,5);
		  s += (0,6);
		  s -= (1);                 //removes 1st element
		
		  s = default(seq[int]);
		  s += (0,1);
		  s[0] = 2;
		  i = 0;
		
		  s += (0,foo()+2*foo());   //OK
		  s -= (foo() - 1);         //OK
		
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
		  assert( values(m1)[0] == 1);  //OK
		  assert(m1[0] == 1);    //holds
		  m1[0] = 2;
		  //assert(m1[0] == 2);    //holds
		  m1 -= (0);
		  assert (sizeof(m1) == 0);  //holds
		  m1[0] = 2;
		  i = 0;
		  assert(m1[i] == 2);    //holds
		  m1[1] = 3;
		
		  m3[0] = true;
		  m3[2] = false;
		  //assert (sizeof(m3) == 2);  //holds
		
		  m8[0] = E;                 //OK
		  m8[1] = E1;                //OK
		  m8[2] = null as event;              //OK

		  m9[0] = E;                  //OK
		  m9[1] = null;               //OK
		
		  /////////////////////////sequence of non-atomic types:
		  s3 += (0,s5);
		  s3 += (1,s1);
		  assert (s3[0] == s5);                  //holds
		  assert (s3[1][0] == 1);              //holds
		
		  s3 -= (1);
		  s3 -= 0;
		  assert (sizeof(s3) == 0);   //holds
		
		  s1 += (0,null);     //OK
		
		  s1 += (0,true);
		  s3 += (0,s5);
		  s3 += (1,s1);
		  assert (s3[0] == s5);
		  assert (s3[1][0] == true);
		  s3 = default(seq[seq[any]]);
		
		  ////////////////////////sequence of maps (casting any => seq[int] is involved)
		  //s6: seq[map[int,any]];
		  m9[0] = E;                  //OK
		  m9[1] = null;               //OK
		  s6 += (0,m9);
		  s12 += (0,1);
		  s12 += (1,2);
		  m10[1] =  100;
		  m10[5] = true;
		  m10[10] = s12;   //seq type used as "any"
		  s6 += (1,m10);
		  assert(s6[0][0] == E);
		  assert(s6[0][1] == null);
		  assert(s6[1][5] == true);
		  assert(s6[1][10] == s12);
		  tmp4 = s6[1][10] as seq[int];          //OK
		  assert(tmp4[0] == 1);
		  assert((s6[1][10] as seq[int])[0] == 1);   //OK
		
		  ////////////////////////tuple with sequence and map:
		  s += (0,1);
		  tmp3[0] = s;
		  t = (a = s, b = tmp3);
		
		  t.a += (0,2);
		  assert ( t.a[0] == 2 );         //holds
		
		  t.a += (1,2);
		  assert ( t.a[1] == 2 );
		
		  t.a += (0,3);
		  assert ( t.a[0] == 3 );
		  assert ( t.a[1] == 2 );
		
		  ////////////////////////map: [bool, seq[(a: int, b: int)]]
		  //var s4, s8: seq[(int,int)];
		  s4 += (0,(0,0));
		  s4 += (1,(1,1));
		  s4 += (2, (2,2));
		
		  s8 += (0,(1,1));
		  s8 += (1,(2,2));
		  s8 += (2,(3,3));
		  ////////////////////////map: var m2: map[int,map[int,any]];
		  m5 = default(map[int,any]);
		  m5[1] = true;
		  m5[2] = E;
		  m5[5] = 5;
		
		  m6 = default(map[int,any]);
		  m6[0] = 0;
		  m6[2] = 2;
		  m6[4] = 4;
		  m6[6] = E;
		  //OK above
		  m2[1] = m5;
		  m2[2] = m6;		
		  ////////////////////////////////////////////
          t.a[foo()] = 2;
		
		  tmp = foo();
		  tmp2 = GetT();
		  assert ( tmp2 == t) ;
		  tmp2.a[foo()] = 1;
		  assert ( tmp2 != t);
		  //m11 += (null,null);   //dynamic error: "key must not exist in map"
		  m11[1] = null;      //OK
		  //m11[null] = 1;      //OK

		  y = default(int);
		  tmp1 = IncY();
		  assert ( tmp1 == 1);
		  t.a[foo()] = tmp1;
          t.a[tmp] = tmp1;
          y = IncY();
		  assert ( y == 2 );
		  raise halt;
       }
    }

    fun foo() : int
    {
       return 1;
    }

	fun bar() : event
    {
       return E;
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
