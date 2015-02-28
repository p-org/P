//Tests complex data types in assign/remove/insert errors): sequences, tuples, maps
//Tests static errors
event E assert 1; 
main machine M
{    
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
	var s4, s8: seq[(int,int)];              
	var s5: seq[bool];
	var s6: seq[map[int,any]];
	var s7: seq[int];
    var i: int;
	var mac: machine;
	var m1: map[int,int];
	var m4: map[int,int];
	var m3: map[int,bool];
	//TODO: write asgns for m2
	var m5, m6: map[int,any];
	var m2: map[int,map[int,any]];
	var m7: map[bool,seq[(a: int, b: int)]];
	
    start state S
    {
       entry
       {
		  /////////////////////////tuples:
		  //ts = (a = 1, b = 2);
		  //ts = (a = 1);           //parsing error
		  //ts = (5);                 //error
		  ts = (1,2);               //error 
		  ts += (1,2);              //error
		  ts -= (1,2);              //error
		  ts.a = ts.b + 1;
		  //assert (ts.a == 3);     //holds
		  
		  tt = (1,2);              //OK
		  tt = (5);                //error
		  tt += (2,3);             //error
		  tt -= (2,3);             //error
		  
		  i = 1;
		  tt.i = 5;             //error
		  ts.i = 5;              //error
		  
		  tt = ts;                //error
		  ts = tt;                //error
		  
	      /////////////////////////sequences:
		  s += (0, 1);
          s += (1, 2);
          s1 = s;
          s -= (1);
		  
		  
		  s += (0,5);
		  s += (0,6);
		  s -= (1);                 //removes 1st element
		  assert (sizeof(s) == 1);   //holds
		  assert (6 in s);           //error: ""in" expects a map"
		  //Removal of 5th element from sequence of size 1:
		  s -= (5,7);               //static error: "index must be an integer"
		  
		  s += (0,1);
		  assert(s[0] == 1);       //holds
		  s[0] = 2;
		  s[true] = 9;             //error
		  assert(s[0] ==2);        //holds
		  i = 0;
		  assert(s[i] == 2);       //holds
		  
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
		  assert (true in m3);        //error: “Value can never be in the map" 
		  
		  m3[true] = false;         //error
		  
		  m3 += 1;                  //error
		  m3 += (1);                //error
		  m3[2] += true;            //error
		  
		  m3[false] = true;         //error
		  
		  /////////////////////////sequence of non-atomic types:
		  s3 += (0,s5);
		  s3 += (1,s1);
		  assert (s3[0] == s5);                  //holds
		  assert (s3[1][0] == true);              //holds
		  
		  s3 -= (1);
		  s3 -= 0;
		  assert (sizeof(s3) == 0);   //holds
		  
		  s3 += 1;             //error
		  s3 += (1);           //error
		  s3[0] += 1;          //error
		  tmp += (0,1);        //error
		  
		  //tests for s6: seq[map[int,any]];
		  
		  ////////////////////////tuple with sequence and map:
		  s += (0,1);
		  tmp3[0] = s;
		  t = (a = s, b = tmp3);
		  
		  t.a += (0,2);
		  assert ( t.a[0] == 2 );         //holds
		  
		  t.a += (1,2);
		  assert ( t.a[1] == 2 );         //holds
		  
		  t.a += (0,3);
		  assert ( t.a[0] == 3 );         //holds
		  assert ( t.a[1] == 2 );         //holds
		  
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
		  
		  //TODO: uncomment below
          t.a[foo()] = 2;  
		  
          GetT().a[foo()] = 1;       //error
		  tmp = foo();
		  GetT().a[tmp] = 1;       //error
		  tmp2 = GetT();
		  //assert ( tmp2 == t) ;
		  tmp2.a[foo()] = 1;
		  //assert ( tmp2 != t);
		  tmp1 = IncY();
		  //assert ( tmp1 == y + 1);
		  t.a[foo()] = tmp1;
          t.a[tmp] = tmp1;          
          y = IncY();
		  //assert ( y == 2 );
		  //////////////////////// IDX:
		  tmp[0] = 0;         //error
		  ////////////////////// WHILE:
		  while (4)
		  {
		      if (i != 3) { assert(m2[0][i] == m2[1][i]); }   //holds
		  	  else { assert(m2[0][i] != m2[1][i]); }        //holds
			  i = i + 1;
		  }
		  ////////////////////////tuple with sequence and map:
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

machine Test {
	var ss: seq[int];
	start state init {
		entry {
		    ss = payload as seq[int];
			assert(ss[0] == 3);            //holds
		}
		
	}
}
