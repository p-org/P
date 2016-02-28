//Tests complex data types in assign/remove/insert: sequences, tuples, maps
event E assert 1; 
event E1 assert 1;
event E2 assert 1;
main machine M
{    
    var t, t1, tmp4: (a: seq [int], b: map[int, seq[int]]);
	var ts: (a: int, b: int);
	var tt: (int, int);
	var te: (int, event); 
    var y, tmp, tmp1, i: int;
	var tmp2: (a: seq [any], b: map[int, seq[any]]);
	var tmp3: map[int, seq[int]];
	var s, s2, s7: seq[int];
    var s1: seq[any];
    var s3: seq[seq[any]];            
    var s4, s8: seq[(a: int, b: int)]; 	
	var s5: seq[bool];
	var s6: seq[map[int,any]];
	var s9: seq[event];
	var mac: machine;
	var m1, m4: map[int,int];
	var m3: map[int,bool];
	var m5, m6: map[int,any];
	var m2: map[int,map[int,any]];
	var m7: map[bool,seq[(a: int, b: int)]];
	var b: bool;
	var e: event;
	var a: any;
	
    start state S
    {
       entry
       {
	      /////////////////////////default expression:
		  y = 2;
		  assert(y == 2);
		  y = default(int);    
          assert(y == 0);	
		  
		  b = true;
		  assert(b == true);
          b = default(bool);	  
          assert(b == false);

		  e = E;
		  assert(e == E);
          e = default(event);	  
          assert(e == null);
		  
		  mac = this;
          mac = default(machine);	  
          assert(mac == null);
		  
		  a = true;
		  a = default(any);
		  assert (a == null);
		  
		  m5 += (1,true);
		  assert (m5[1] == true);
		  m5 = default(map[int,any]);
		  assert (m5[1] == null);
		  m5 += (1,E);
		  assert (m5[1] == E);
		  /////////////////////////tuples:
		  ts.a = ts.b + 1;
		  assert (ts.a == 1 && ts.b == 0);     //holds
		  ts = (a = 1, b = 2);
		  ts.a = ts.b + 1;
		  assert (ts.a == 3);     //holds
		  
		  ts.a = 2;
		  ts.b = 3;
		  ts.a = ts.b + foo();    //non-primitive expr in RHS
		  assert (ts.a == 4);     //holds?
		  ts.0 = ts.0 + ts.1;     //OK
		  assert (ts.0 == 7);     //holds
		  
		  tt.0 = 1;
		  tt.1 = 2;
		  tt.0 = tt.1 + 1 + foo();
		  assert (tt.0 == 4);     //holds
		  
		  tt = (3,4);             //OK
		  
		  tt.0 = ts.b;            //OK
		  assert (tt.0 == 3);     //holds
		  
		  ts.b = tt.0 + foo();        //OK
		  assert (ts.b == 4);     //holds
		  
		  te = (2,E);            //OK
		  assert(te.0 == 2 && te.1 == E);  //holds
		  te = (2,bar());         //OK
		  assert(te.0 == 2 && te.1 == E);  //holds
		  te = (4,null);          //OK
		  
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
		  
		  s = default(seq[int]);
		  s += (0,1);
		  assert(s[0] == 1);       //holds
		  s[0] = 2;
		  assert(s[0] ==2);        //holds
		  i = 0;
		  assert(s[i] == 2);       //holds
		  
		  s += (0,foo()+2*foo());    
		  assert(s[0] == 3);         //holds
		  s += (1,1);
		  s -= (foo() - 1);
		  assert(s[0] == 1);       //holds 
		 
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
		  
		  s9 += (0,E);
		  s9 += (1,E1);
		  s9 += (2,E2);
		  s9 += (3,null);
		  assert (sizeof(s9) == 4);   //holds
		   
		  /////////////////////////sequence of non-atomic types:
		  ////////////////////////////////seq of seq's: 
		  //var s3: seq[seq[any]];		
		  s1 = default(seq[any]);
		  s1 += (0,true);
		  s1 += (1,false);
		  //s1 += (1,1);
		  assert (sizeof(s1) == 2);   //holds
		  s1 += (0,1);             //OK
		  
		  s5 = default(seq[bool]);
		  s5 += (0, true);           
		  s5 += (1, false);              
		  assert (sizeof(s5) == 2);   //holds
		  
		  s3 += (0,baz());              //OK
		  s3 += (1,s1);
		  assert (s3[0] == s5);                  //holds
		  assert (s3[1] == s1);                  //holds
		  assert (s3[1][0] == 1);              //holds
		  
		  s3[1] = s5;
		  assert (s3[1][0] == true);          //holds
		  assert (sizeof(s3) == 2);           //holds
		  
		  s3 -= (foo());
		  s3 -= 0;
		  assert (sizeof(s3) == 0);   //holds
		   
		  ///////////////////////////////////seq of maps:
		  m1 = default(map[int,int]);
		  m1[0] = 2;
		  m1[1] = 3;
		  
		  m3 = default(map[int,bool]);
		  m3[0] = true;
		  m3[1] = true;
		  
		  s6 = default(seq[map[int,any]]);
		  s6 += (0,daz());
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
		  
		  s6 -= (foo());
		  assert (sizeof(s6) == 2);   //holds
		  
		  /////////////////////////sequence as payload:
		  s2 += (0,1);
          s2 += (0,3);
	      mac = new Test(s2); 
		  
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
		  
		  assert (2 in m3); //holds
		 
		  m4 = m1;
		  assert (m4[i] == 2);          //holds
		  
		  ////////////////////////tuple (a: seq [int], b: map[int, seq[int]]):
		  s7 = default(seq[int]);
		  s7 += (0,1);
		  tmp3 = default(map[int, seq[int]]);
		  tmp3[0] = s7;
		  t = default((a: seq [int], b: map[int, seq[int]]));
		  t = (a = s7, b = tmp3);
		  assert (sizeof(t.a) == foo());      //holds
		  
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
		  
		  s2 = default(seq[int]);
		  s2 += (0,1);
          s2 += (0,3);
		  tmp3[0] = s2;
		  t.b = tmp3;
		  assert (t.b[0][0] == 3);       //holds
		  assert (t.b[0][1] == 1);       //holds
		  
		  ////////////////////////map: var m5, m6: map[int,any];
		  m5[0] = 1;
		  m5[1] = false;
		  //Alternative way for "m5[2] = 2;":
		  m5 += (2,foo()+1);
		  m5[3] = true;
		  i = keys(m5)[0];
		  assert (i == 0);           //holds
		  i = keys(m5)[foo()+2];
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
		  
		  assert (sizeof(m2) == 2);     //holds
		  m2 -= (foo());
		  assert (sizeof(m2) == 1);     //holds
		  
		  ////////////////////////map: [bool, seq[(a: int, b: int)]]
		  //var s4, s8: seq[(int,int)];
		  s4 += (0,(a=0,b=foo()-1));
		  s4 += (1,(a=1,b=1));
		  s4 += (2, (a=2,b=foo()+1));
		  
		  s8 += (0,(a=1,b=1));
		  s8 += (1,(a=2,b=2));
		  s8 += (2,(a=3,b=3));
		  
		  m7[true] = s4;
		  m7[false] = s8;

		  assert (m7[true][0] == (a=0,b=0));       //holds
		  assert (m7[false][2] == (a=3,b=3));      //holds  
		  
		  ////////////////////////tuple with sequence and map, plus functions:
		 
          //t1.a[foo()-1] = 2;          //zinger/runtime "index out-of-bounds" error
		  t1.a += (0,2);
		  assert (t1.a[foo()-1] == foo()+1);   //holds
		  tmp = foo();              
		  
		  tmp4 = GetT1();             
          tmp4.a += (1,1);           
		  
		  tmp2 = GetT1();            
		  assert ( tmp2 == t1) ;    //holds
		  tmp2.a[foo()-1] = foo();
		  assert ( tmp2 != GetT1());     //holds
		  
		  tmp1 = IncY();
		  assert ( tmp1 == 1);       //holds
		  y = IncY();
		  assert ( y == 2 );       //holds
		  
		  tmp1 = 1;
		  t.a[foo()] = tmp1;
		  assert (t.a[foo()] == tmp1);        //holds
          t.a[tmp] = tmp1;   
          assert (t.a[foo()] == tmp1);        //holds	

		  /////////////////expr produces tuple type:
		  tmp = foo();
		  tmp1 = IncY();
		  ts = (a = tmp1, b = tmp + 5);
		  assert (ts.a == 3);    //holds
		  assert (ts.b == 6);    //holds

		  //TODO: test "in" and "idx" operators, including errors (see 4.5.3.9)
		  
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
	
	fun baz() : seq[bool]
	{
		return s5;
	}
	
	fun daz() : map[int,int]
	{
		return m1;
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

machine Test {
	var ss: seq[int];
	start state init {
		entry (payload: seq[int]) {
		    ss = payload;
			assert(ss[0] == 3);            //holds
		}
		
	}
}
