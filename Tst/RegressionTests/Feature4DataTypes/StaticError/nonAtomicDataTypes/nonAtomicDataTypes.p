//XYZs complex data types in assign/remove/insert errors): sequences, tuples, maps
//XYZs static errors; XYZ Correct\nonAtomicDataTypes XYZs all asserts
//Basic types: int, bool, event

event E assert 1;
event E1 assert 1;
event E2 assert 1;
event E3 assert 1;

machine Main {
    var t : (a: seq [int], b: map[int, seq[int]]);
	var t1 : (a: seq [int], b: map[int, seq[int]]);
	var t2: (a: seq [int], a: map[int, seq[int]]);       //error
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
		  y = default(bool);    //error
          b = default(bool);	

		  e = E;
		  b = default(event);    //error
          e = default(event);	
		
		  mac = this;
		  e = default(machine);    //error
          mac = default(machine);	
		
		  a = true;
		  a = default(any);
		
		  m5 += (1,true);
		  m5 = default(map[int,any]);
		  m5 += (1,E);
		  /////////////////////////tuples:
		  //ts = (a = 1, b = 2);
		  //ts = (a = 1);           //parsing error
		  //ts = (5);                 //error
		  ts = (1,2);               //error
		  ts += (1,2);              //error
		  ts -= (1,2);              //error
		  ts.b = 1;
		  ts.a = ts.b + foo();	   //non-primitive expr in RHS; OK?
		  ts = default((a: int, b: int));
		
		  tt = (1, foo() + 1);     //OK
		  tt = (5);                //error
		  tt += (2,3);             //error
		  tt -= (2,3);             //error
		
		  i = 1;
		  tt.i = 5;             //error
		  ts.i = 5;              //error
		
		  tt = ts;                //error
		  ts = tt;                //error
		
		  tt = default((int, int));
		
		  te = (1,1);             //error
		  te = (2,E2);            //OK
		  te = (3,bar());         //OK
		  te = (4,null);          //OK
		
		  te = (null,E2);         //error
		
	      /////////////////////////sequences:
		  s += (0, 1);
          s += (1, 2);
          s1 = s;
          s -= (1);
		
		
		  s += (0,5);
		  s += (0,6);
		  s -= (1);                 //removes 1st element
		  assert (6 in s);           //error: ""in" expects a map"
		  //Removal of 5th element from sequence of size 1:
		  s -= (5,7);               //static error: "index must be an integer"
		
		  s = default(seq[int]);
		  s += (0,1);
		  s[0] = 2;
		  s[true] = 9;             //error
		  i = 0;
		  s += (1,null);           //error
		
		  s += (0,foo()+2*foo());   //OK
		  s -= (foo() - 1);         //OK
		
		  s9 += (0,E);
		  s9 += (1,E1);
		  s9 += (2,E2);
		  s9 += (3,null);
		  //assert (sizeof(s9) == 4);   //holds
		  s9 += (4,15);               //error
		  s10 += (0,E);                //OK
		  s11 += (0,E1);              //error
		
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
		  //assert(m1[0] == 1);    //holds
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
		  assert (true in m3);        //error: “Value can never be in the map"
		  assert(values(m3)[0] == 1); //error
		
		  m3[true] = false;         //error
		
		  //m3 += 1;                  //error
		  //m4 += (1);                //error
		  //m3[2] += true;            //error
		
		  m3[false] = true;         //error
		
		  m8[0] = E;                 //OK
		  m8[1] = E1;                //OK
		  m8[2] = null;              //OK
		  m8[3] = 5;                 //error
		
		  m9[0] = E;                  //OK
		  m9[1] = null;               //OK
		
		  /////////////////////////sequence of non-atomic types:
		  s3 += (0,s5);
		  s3 += (1,s1);
		  assert (s3[0] == s5);                  //holds
		  assert (s3[1][0] == true);              //holds
		
		  s3 -= (1);
		  s3 -= 0;
		  assert (sizeof(s3) == 0);   //holds
		
		  s3 += 1;             //error
		  s33 += (1);           //error
		  s3[0] += 1;          //error
		  tmp += (0,1);        //error
		
		  s1 += (0,null);     //OK
		
		  s1 += (0,true);
		  s3 += (0,s5);
		  s3 += (1,s1);
		  assert (s3[0] == s5);
		  assert (s3[1][0] == true);
		  s3 = default(seq[seq[any]]);
		  assert(s3[1][0] == null);
		  assert(s3[0][0] == null);
		
		  ////////////////////////sequence of maps (casting any => seq[int] is involved)
		  //s6: seq[map[int,any]];
		  m9[0] = E;                  //OK
		  m9[1] = null;               //OK
		  s6 += (0,m9);
		  s12 += (0,1);
		  s12 += (1,2);
		  //m10 += (1, 100);
		  //m10 += (5, true);
		  //m10 += (10, s12);   //seq type used as "any"
		  s6 += (1,m10);
		  assert(s6[0][0] == E);
		  assert(s6[0][1] == null);
		  assert(s6[1][5] == true);
		  assert(s6[1][10] == s12);
		  tmp4 = s6[1][10];                      //error: "invalid assignment. right hand side is not a subtype of left hand side"
		  tmp4 = s6[1][10] as seq[int];          //OK
		  assert(tmp4[0] == 1);
		  assert(s6[1][10][0] == 1);             // error for s6[1][10][0]: "Indexer must be applied to a sequence or map"
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
		
		  m7[true] = s4;      //error
		  m7[false] = s8;     //error
		
		  ////////////////////////map: var m2: map[int,map[int,any]];
		
		  m2[true] = m5;   //error
		
		  ////////////////////////////////////////////
          t.a[foo()] = 2;
		
          GetT().a[foo()] = 1;       //error
		  tmp = foo();

		  GetT().a[tmp] = 1;       //error
		  tmp2 = GetT();
		  //assert ( tmp2 == t) ;
		  tmp2.a[foo()] = 1;
		  //assert ( tmp2 != t);
		
		  tmp2.b = null;                //error
		  tmp2 = (a = s, b = null);     //error
		  tmp2 = (a = null, b = null);    //error
		  tmp2 = (a = s1, b = null);      //error

		
		  tmp1 = IncY();
		  //assert ( tmp1 == y + 1);
		  t.a[foo()] = tmp1;
          t.a[tmp] = tmp1;
          y = IncY();
		  //assert ( y == 2 );
		  //////////////////////// IDX:
		  tmp[0] = 0;         //error
		  ////////////////////// WHILE:
		  while (4)                  //error
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
