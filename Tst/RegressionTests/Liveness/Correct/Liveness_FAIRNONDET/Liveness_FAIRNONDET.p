// Liveness test: "check passed"
// This is a simplest sample with FAIRNONDET in liveness

event UserEvent assert 1;
event Done assert 1;
event Loop assert 1;
event Waiting assert 1;
event Computing assert 1;

main machine EventHandler
{
       start state WaitForUser
       {
            entry { 
				new WatchDog();
				monitor WatchDog, Waiting;
				send this, UserEvent;
				}
            on UserEvent goto HandleEvent;
       }
  
       state HandleEvent
       {
            entry { 
				monitor WatchDog, Computing;
				if ($$) {
					send this, Done;
				}
				else {
					send this, Loop;
				}
				//send this, Done;
				}			
            on Done goto WaitForUser;
			on Loop goto HandleEvent;
       }
}

monitor WatchDog
{
      start cold state CanGetUserInput
      {
             on Waiting goto CanGetUserInput;
             on Computing goto CannotGetUserInput;
      } 
	  hot state CannotGetUserInput
     {
             on Waiting goto CanGetUserInput;
             on Computing goto CannotGetUserInput;
     }
}

