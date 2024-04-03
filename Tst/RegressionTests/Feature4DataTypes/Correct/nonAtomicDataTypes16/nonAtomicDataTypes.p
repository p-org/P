//XYZs "Value must have a concrete type" error
//Basic types: int, bool, event
//This XYZ found a bug in Zinger
event E assert 1;
event E1 assert 1;
event E2 assert 1;
event E3 assert 1;

machine Main {
    var y : int;
	var b: bool;
	var e: event;
	var a: any;
	var mac: machine;
	
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
		  if (a == null) { a = 1;};  //Zing bug(fixed): error: "Value must have a concrete type"; runtime: no error
		
		  raise halt;
       }
    }
}

