// Liveness test: simplest sample demonstrating "warm" (unmarked) states

event Unit;
event UserEvent;
event Done;
event Waiting : int;
event Computing;

main machine EventHandler
{
       start state Init {
			entry { new WatchDog(); raise Unit; }
			on Unit goto WaitForUser;
       }

       state WaitForUser
       {
            entry { 
				monitor WatchDog, Waiting, 0;
				send this, UserEvent;
			}
            on UserEvent goto HandleEvent;
       }
  
       state HandleEvent
       {
            entry { 
				monitor WatchDog, Computing;
				// send this, Done;
			}			
            on Done goto WaitForUser;
       }
}

monitor WatchDog
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
