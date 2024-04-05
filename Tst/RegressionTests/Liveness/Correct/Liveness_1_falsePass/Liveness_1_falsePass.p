// Liveness XYZ: "check passed", however this is a false pass:
// WatchDog announce is never instantiated, hence, Zing ignores
// all invocations of it
// TODO: need to issue a warning (or error)
// Compare this XYZ to Liveness_1.p

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

