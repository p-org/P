// Liveness test: simplest sample demonstrating liveness error found:

event Unit;
event UserEvent;
event Done;
event Waiting : int;
event Computing;

main machine EventHandler
{
       start state Init {
			entry { raise Unit; }
			on Unit goto WaitForUser;
       }

       state WaitForUser
       {
            entry { 
				monitor Waiting, 0;
				send this, UserEvent;
			}
            on UserEvent goto HandleEvent;
       }
  
       state HandleEvent
       {
            entry { 
				monitor Computing;
				// send this, Done;
			}			
            on Done goto WaitForUser;
       }
}

spec WatchDog monitors Computing, Waiting
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
