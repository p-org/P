// Liveness XYZ: simplest sample demonstrating "warm" (unmarked) states

event Unit;
event UserEvent;
event Done;
event Waiting : int;
event Computing;

machine Main {
       start state Init {
			entry { raise Unit; }
			on Unit goto WaitForUser;
       }

       state WaitForUser
       {
            entry {
				announce Waiting, 0;
				send this, UserEvent;
			}
            on UserEvent goto HandleEvent;
       }

       state HandleEvent
       {
            entry {
				announce Computing;
				// send this, Done;
			}			
            on Done goto WaitForUser;
       }
}

spec WatchDog observes Computing, Waiting
{
      start cold state CanGetUserInput
	
      {
             on Waiting goto CanGetUserInput;
             on Computing goto CannotGetUserInput;
      }
	  //If CannotGetUserInput is marked as "hot", Zinger reports liveness error
	  //hot state CannotGetUserInput
	  //"warm" instead of "hot":
	  state CannotGetUserInput
     {
             on Waiting goto CanGetUserInput;
             on Computing goto CannotGetUserInput;
     }
}
