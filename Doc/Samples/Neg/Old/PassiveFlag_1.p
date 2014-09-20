event E1 assert 1;
event E2: int assert 1;
event E3 assert 1;
event E4 assert 1;
event Unit assert 1;

     
main machine Real {
		  var ghostVar:mid;
		  var test: bool;
		  var counter: int;

   start state Real_Init {
 entry {
	       ghostVar = new Ghost(this); 
		   PassiveFunctionIncrement();
		   raise(Unit);	   
       }

   exit { test = true; PassiveFunctionIncrement(); }
on E2 do Action_5;
on Unit push Real_S1;
on E4 goto Real_S2;
}

   action Action_5 { assert((int)payload == 100); send(ghostVar, E3); PassiveFunctionIncrement(); }




   state Real_S1 {
	   entry {
          send(ghostVar, E1);
	      PassiveFunctionIncrement();
	   }
}

model fun {passive} PassiveFunctionIncrement() { counter = counter + 1; }

       state Real_S2 {
	   entry {
           assert(test == false);
           assert(false);
	   }
	  }


}

model machine Ghost {
 
		    var real:id;
     
   start state Ghost_Init {
			  entry { real = (id) payload; }
			  on E1 goto Ghost_S1;
}

  state Ghost_S1 {
	  entry {
		 send (real, E2, 100);		 	  
	   }
on E3 goto Ghost_S2;
}

  state Ghost_S2 {
	  entry {
		 send (real, E4);	 	  
	  }
   }
}
