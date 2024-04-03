//XYZs "any" type in EQ and NE
//TODO: Create separate XYZs for all failing asserts.
event E assert 1;

machine Main {
	var vAny: any;
	var vEvent: event;
	var vInt: int;
	
	start state S
    {
       entry
       { 	
			vAny = 1;
			
			vInt = 1;
			assert(vAny == vInt), format("Assertion 0 failed");  //holds
			
			vEvent = E;
			vAny = E;
			//assert(vAny != vEvent);    //fails
			
			vAny = true;
			vInt = 1;
			assert(vAny != vInt), format("Assertion 1 failed");  //holds
			
			//assert(vAny == vInt);  //fails
			
			vAny = null;
			
			vEvent = default(event);
			assert (vAny == vEvent), format("Assertion 2 failed");  //holds
			
			assert(vAny == 1), format("Assertion 3 failed");  //fails
			
	   }
	}
}
