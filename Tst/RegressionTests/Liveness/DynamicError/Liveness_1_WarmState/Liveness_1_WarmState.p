// Liveness XYZ: simplest sample demonstrating "warm" (unmarked) states

event UserEvent;
event Done;
event Waiting;
event Computing;

machine Main {
       start state WaitForUser
       {
            entry {
				announce Waiting;
				send this, UserEvent;
				}
            on UserEvent goto HandleEvent;
       }

       state HandleEvent
       {
            entry {
				announce Computing;
				send this, Done;
				}			
            on Done goto WaitForUser;
       }
}

spec WatchDog observes Computing, Waiting
{
	  // For "cold" state CanGetUserInput, the XYZ passes
      //start cold state CanGetUserInput
	  start state CanGetUserInput
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

