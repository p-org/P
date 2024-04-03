// Liveness: "pass" from Zing expected

event UserEvent;
event Done;
event Continue;
event Waiting;
event Computing;

machine Main {
       start state WaitForUser
       {
            entry {
				new Loop();
				announce Waiting;
				send this, UserEvent;
				}
            on UserEvent goto HandleEvent;
       }

       state HandleEvent
       {
            entry {
				announce Computing;
				send this, Continue;
				}			
            on Continue goto HandleEvent;
       }
}

machine Loop
{
	start state Looping{
		entry {
			send this, Done;
		}
		on Done goto Looping;
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

