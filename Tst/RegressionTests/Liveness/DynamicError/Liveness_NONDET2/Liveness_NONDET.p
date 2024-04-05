// Liveness XYZ: "check failed"
// This is a simplest sample with NONDET in liveness
// - compare this XYZ with Correct\Liveness_FAIRNONDET

event UserEvent assert 1;
event Done assert 1;
event Loop assert 1;
event Waiting assert 1;
event Computing assert 1;

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
				if ($) {
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

