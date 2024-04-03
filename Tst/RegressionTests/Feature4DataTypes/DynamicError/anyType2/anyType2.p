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
			assert(vAny == vInt);  //holds
			
			vEvent = E;
			vAny = E;
			//assert(vAny != vEvent);    //fails
			
			vAny = true;
			vInt = 1;
			assert(vAny != vInt);  //holds
			
			assert(vAny == vInt);  //fails
			
			vAny = default(machine);		
			vEvent = default(event);
			assert (vAny == vEvent);  //holds
	        //assert (vAny != vEvent);  //fails
			
			//assert(vAny == 1);  //fails
			
	   }
	}
}
