// Liveness XYZ: "check passed"
// This is a simplest sample with FAIRNONDET in liveness
//checking symmetry in how FAIRNONDET works

event UserEvent;
event Done;
event Loop;
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
				if ($$) {
					send this, Loop;
				}
				else {
					send this, Done;
				}
				}			
            on Done goto WaitForUser;
			on Loop goto HandleEvent;
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
             on Waiting goto CanGetUserInput;
             on Computing goto CannotGetUserInput;
     }
}

