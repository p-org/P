//Tests complex data types in assign/remove/insert: sequences, tuples, maps
event E assert 1; 
main machine M
{    
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
		  ts.0 = ts.0 + ts.1;
		  assert (ts.0 == 7);     //holds
		  
		  tt.0 = 1;
		  tt.1 = 2;
		  tt.0 = tt.1 + 1;
		  assert (tt.0 == 3);     //holds
		  tt = (3,4);             //OK
		  
	      /////////////////////////sequences:
		  s += (0, 1);
          s += (1, 2);
          s1 = s;
          s -= (0);
		  assert(sizeof(s) == 1);   //holds
          s -= (0);
		  assert(sizeof(s) == 0);   //holds
		  
		  s -= (0);                 //Zing/runtime error
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
		  m1[0] = 2;
		  i = 0;
		  assert(m1[i] == 2);    //holds 
		  
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
		  assert ( t.a[1] == 2 );         //error
		  
          // Why: fails
		  assert ( sizeof(t.a) == 2 );
		  //TODO: uncomment below
          //t.a[foo()] = 2;  
		  
          //GetX().a[foo()] = 1;  
		  //tmp = foo();
		  //tmp2 = GetT();
		  //assert ( tmp2 == t) ;
		  //tmp2.a[foo()] = 1;
		  //assert ( tmp2 != t);
		  //tmp1 = IncY();
		  //assert ( tmp1 == y + 1);
		  //t.a[foo()] = tmp1;
          //t.a[tmp] = tmp1;          
          //y = IncY();
		  //assert ( y == 2 );
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
