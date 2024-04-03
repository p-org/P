// Liveness XYZ: simplest sample demonstrating liveness error found
// This is non-terminating program

event UserEvent;
event Done;
event Waiting;
event Computing;

machine Driver {
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
            on Done goto HandleEvent;  //staying in HandleEvent forever
       }
}

spec WatchDog observes Computing, Waiting
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

// asserts the liveness monitor
test tcLiveness [main = Driver]:
  assert WatchDog in { Driver };


