//XYZs complex data types in assign/remove/insert: sequences, tuples, maps
event E assert 1;
event E1 assert 1;
event E2 assert 1;
machine Main {
    var y, tmp, tmp1, i: int;
	var tmp2: (a: seq [any], b: map[int, seq[any]]);
	var tmp3: map[int, seq[int]];
	var mac: machine;
	var m1, m4: map[int,int];
	var m3: map[int,bool];
	var m5, m6: map[int,any];
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
		
		  m5[1] = true;
		  assert (m5[1] == true);
		  m5 = default(map[int,any]);
		  assert (m5[0] == null);               //dynamic error: key not found
		  //assert (m5[1] == null);             //dynamic error: key not found
		  m5[1] = E;
		  assert (m5[1] == E);
		
		  raise halt;
       }
    }
}
