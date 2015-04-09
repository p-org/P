//Tests complex data types in assign/remove/insert errors): sequences, tuples, maps
//Tests that comparison of "any" type var with "null" is allowed
//This test found a bug in Zinger

event E assert 1; 
event E1 assert 1;
event E2 assert 1;
event E3 assert 1;

main machine M
{    
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
		  assert (a == null);   //(before fix) Zing error: "Value must have a concrete type"
		  if (a == null) { a = 1;};  //(before fix) Zing error: "Value must have a concrete type"
		  
		  raise halt;
       }    
    }  
}

