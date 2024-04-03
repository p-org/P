//XYZs P expressions and operators
//XYZs static errors; XYZ Correct\ExprOperatorsAsserts XYZs all asserts
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
		  assert(tbool.0 - tbool.1 == 1);  //error: "Operator expected first argument to be an integer value"
		  tbool.1 = !y;              //error: "Operator expected a boolean value"
		  assert(tbool.0 > tbool.1);  //error: "Operator expected first argument to be an integer value"
		  assert(1 > tbool.1);       //error: "Operator expected second argument to be an integer value"
		  assert(tbool.1 && 0 == false); //error: "Operator expected second argument to be a boolean value"
		  assert(0 && tbool.1 == false); //error: "Operator expected first argument to be a boolean value"
		  ev = E;
		  assert(ev != tbool.0);    //error
		
		  tt.0 = 1;
		  tt.1 = - tt.0;            //OK, unary NEG operator
		  assert(tt.0 + tt.1 == 0); //holds
		  assert(tt.0 - tt.1 == 2); //holds
		  assert(tt.0 * tt.1 == -1);  //holds
		  assert(tt.0 / tt.1 == -1);  //holds
		  assert(tt.0 * 5 / 2 == 2);  //holds: 5/2=2
		  assert(tt.0 != tt.1);       //holds
		
		  tt.0 = tt.1 + tbool.1;    //error
		  tt.1 = tbool.0 - tt.1;    //error
		  tt.1 = tt.1 * tbool.0;    //error
		  tt.1 = tt.1 / tbool.0;    //error
		
		  assert (tt.0 < tbool.1);  //error
		  assert (tt.0 <= tbool.1);  //error
		  assert (tt.0 > tbool.1);  //error
		  assert (tt.0 >= tbool.1);  //error
		
		  assert(tt.0 && tbool.1);   //error
		  assert(tt.0 || tbool.1);   //error
		  assert(tbool.1 && ev);     //error
		
		  assert(tt.0 / tbool.0);    //error: "Operator expected second argument to be an integer value"
		  tt.1 = - tbool.0;         //error: "Operator expected an integer value"
		  assert(tt.0 == ev);       //error
		
		  tbool.0 = !tt.1;       //error
		  tbool.1 = !ev;         //error

		  tt.0 = -ev;            //error
		

		  assert (ev == a);       //OK
		
		  assert(tt.1 == -a);     //error
		  assert(a * (-1) == -1);  //error
		  assert(a + 1 != a);      //error
		
		  assert(a > tt.1);       //error
		  assert(a <= tt.0);      //error
		
		  a = 1;

		  assert (ev == a);       //OK?????????
		



		


		
		  a = false;
		  assert(a && tbool.1 == false);  //error
		
	      /////////////////////////sequences:
		
		  s12 += (0,true);
		  s12 += (1, !s12[0]);      //OK, unary NOT operator
		  assert(s12[1] == false);  //holds
		  s12 += (0, E);            //error: "value must be a subtype of sequence type"
		
		  s2 += (0,1);
		  s2 += (1, - s2[0]);    //OK, unary NEG operator
		  assert(s2[1] == -1);   //holds
		
		  s5 += (false,false);   //error: "key must be an integer"
		
		  /////////////////////////maps:
		  m1[0] = 1;
		  assert (0 in m1);      //holds
		  i = keys(m1)[0];
		  assert(i == 0);        //holds
		
		  i = keys(tbool)[0];    //error: "Operator expected a map value"
		  i = values(tbool)[1];  //error: "Operator expected a map value"
		  i = sizeof(tbool);     //error: "Operator expected a map or sequence value"
		
		  m1[1] = 3;
		  m1[2] = - m1[1];            //OK, unary NEG operator
		  assert(m1[1] + m1[2] == 0);  //holds
		
		  assert(m1[0] < m1[1]);       //holds
		  m1[0] = 3;
		  assert(m1[0] <= m1[1]);       //holds
		  assert(m1[1] > m1[2]);       //holds
		  m1[4] = 3;
		  assert(m1[1] >= m1[4]);       //holds
		  assert(m1[6] > 0);         //no error????? (dynamic error only?)
		  i = 100;
		  assert(m1[i] > 0);         //no error????? (dynamic error only?)
		
		
		  m3[0] = true;
		  m3[2] = false;
		  assert(m3[0] == !m3[1]);   //holds; unary NOT operator
		  assert (sizeof(m3) == 2);  //holds
		  assert (true in m3);        //error: “Value can never be in the map"
		  assert(values(m3)[0] == 1); //fails
		
		  m3 += (true,1);               //error: "key not in the domain of the map"
		  m3 += (3, 1);                 //error: "value not in the codomain of the map"
		
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
		
		  m7[true] = s4;      //error
		  m7[false] = s8;     //error
		
		  assert(s4[0].0 + s8[1].1 - s8[2].0 == 0);   //holds
		  assert(s4[1].0 * s8[1].1 / s8[2].0 == 1);   //holds
		
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
