//XYZs P expressions and operators
//XYZs static errors
//Basic types: int, bool, event
//This XYZ can be further extended for combined non-atomic types

event E assert 1;
event E1 assert 1;
event E2 assert 1;
event E3 assert 1;

machine Main {
    var t : (a: seq [int], b: map[int, seq[int]]);
	var t1 : (a: seq [int], b: map[int, seq[int]]);
	var ts: (a: int, b: int);
	var tt: (int, int);
	var tbool: (bool, bool);
	var te: (int, event);       ///////////////////////////////////////////////////////////
	var b: bool;
    var y : int;
	var tmp: int;
	var tmp1: int;
	var ev: event;
	var a: any;
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
	var s11: seq[int];
	var s12: seq[bool];
	var s13: seq[int];
	var s14: seq[any];
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
	var m9: map[int,any];
	
    start state S
    {
       entry
       {
		  /////////////////////////tuples:
		  tbool.0 = true;            //OK
		  tbool.1 = !tbool.0;        //OK, unary NOT operator
		  assert(tbool.1 == false);  //holds
		  assert(tbool.0 && tbool.1 == false); //holds
		  assert(tbool.0 || tbool.1 == true); //holds
		  assert(tbool.0 != tbool.1);      //holds
		
		  ev = E;
		  assert(ev != null);    //
		
		  tt.0 = 1;
		  tt.1 = - tt.0;            //OK, unary NEG operator
		  assert(tt.0 + tt.1 == 0); //holds
		  assert(tt.0 - tt.1 == 2); //holds
		  assert(tt.0 * tt.1 == -1);  //holds
		  assert(tt.0 / tt.1 == -1);  //holds
		  assert(tt.0 * 5 / 2 == 2);  //holds: 5/2=2
		  assert(tt.0 != tt.1);       //holds
		
		  a = null;
		  assert(tt.1 != a);     //holds
		  a = -1;
		  assert(tt.1 == a);     //holds
		  assert(a != ev);       //holds
		  a = null;
		  assert(mac == a);      //holds
		
		  a = 1;
		  //assert(a == tbool.0);   //fails; TODO: create separate XYZ
		
		  a = !tbool.0;
		  assert(a == tbool.1);   //holds
		
		  a = null;
          assert(a != 1);        //holds
          //assert(a == 1);        //fails; TODO: create separate XYZ	
		  a = 1;
		  assert(a == 1);         //holds
		
	      /////////////////////////sequences:
		
		  s12 += (0,true);
		  s12 += (1, !s12[0]);      //OK, unary NOT operator
		  assert(s12[1] == false);  //holds
		
		  s2 += (0,1);
		  s2 += (1, - s2[0]);    //OK, unary NEG operator
		  assert(s2[1] == -1);   //holds
		
		  /////////////////////////maps:
		  m1[0] = 1;
		  assert (0 in m1);      //holds
		  i = keys(m1)[0];
		  assert(i == 0);        //holds
		
		  m1[1] = 3;
		  m1[2] = - m1[1];            //OK, unary NEG operator
		  assert(m1[1] + m1[2] == 0);  //holds
		
		  assert(m1[0] < m1[1]);       //holds
		  m1[0] = 3;
		  assert(m1[0] <= m1[1]);       //holds
		  assert(m1[1] > m1[2]);       //holds
		  m1[4] = 3;
		  assert(m1[1] >= m1[4]);       //holds
		
		
		  m3[0] = true;
		  m3[2] = false;
		  assert(m3[0] == !m3[2]);   //holds; unary NOT operator
		  assert (sizeof(m3) == 2);  //holds
		
		  ////////////////////////tuple with sequence and map:
		  // var s: seq[int];
		  // var tmp3: map[int, seq[int]];
		  // var t : (a: seq [int], b: map[int, seq[int]]);
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
		
		  ///////////////////////////XYZing that asgns in P perform a deep copy:
		  //checking how updating s affects tmp3 and t.b:
		  assert(s[0] == 1);             //holds
		  assert(tmp3[0][0] == 1);      //holds
		  assert(t.b[0][0] == 1);       //holds
		  s += (0,2);
		  assert(s[0] == 2 && s[1] == 1);  //holds
		  //tmp3 and t.b are not affected by update of s:
		  assert(tmp3[0][0] == 1);      //holds
		  assert(t.b[0][0] == 1);       //holds	

		  tmp3[0] = s;
		  assert(tmp3[0][0] == 2);      //holds
		  //update on tmp3 does not affect t.b:
		  assert(t.b[0][0] == 1);       //holds
		
		  ////////////////////////map: [bool, seq[(a: int, b: int)]]
		  //var s4, s8: seq[(int,int)];
		  s4 += (0,(0,0));
		  s4 += (1,(1,1));
		  s4 += (2, (2,2));
		
		  s8 += (0,(1,2));
		  s8 += (1,(2,3));
		  s8 += (2,(3,4));
		
		  assert(s4[0].0 + s8[1].1 - s8[2].0 == 0);   //holds
		  assert(s4[1].0 * s8[1].1 / s8[2].0 == 1);   //holds
		
		  // Regression Tests for in operator for a sequence
		  s13 += (0, 4);
		  assert((4 in s13) == true);
		  assert((5 in s13) == false);
		
		  s14 += (0, 1);
		  s14 += (1, true);
		  s14 += (2, (3, 4));
		  assert((1 in s14) == true);
		  assert((true in s14) == true);
		  assert(((3, 4) in s14) == true);
		  assert((16 in s14) == false);
		  assert((false in s14) == false);
		  assert(((4, 5) in s14) == false);

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
