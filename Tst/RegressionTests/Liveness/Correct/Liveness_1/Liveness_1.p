// Liveness XYZ: "check passed"
// This is a simplest sample demonstrating liveness checking
// by Zing

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

spec WatchDog observes Waiting, Computing
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

