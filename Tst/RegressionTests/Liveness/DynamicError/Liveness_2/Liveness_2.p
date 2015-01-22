// Liveness test: simplest sample demonstrating liveness error found
// This is non-terminating program

event UserEvent;
event Done;
event Waiting;
event Computing;

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
				send this, Done;
				}			
            on Done goto HandleEvent;  //staying in HandleEvent forever
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
		entry {
		}
             on Waiting goto CanGetUserInput;
             on Computing goto CannotGetUserInput;
     }
}

