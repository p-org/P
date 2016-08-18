/*
event Ping assert 1: machine;
event Pong assert 1;
event Success;

include "PrtDistHelp.p"

machine PING 
{
    var pongId: machine;

    start state Init {
        entry {
			pongId = new PONG();
	        raise Success;   	   
        }
        on Success goto Ping_SendPing;
    }

    state Ping_SendPing {
        entry {
			send pongId, Ping, this;
	        raise Success;
	    }
        on Success goto Ping_WaitPong;
     }

     state Ping_WaitPong {
        on Pong goto Ping_SendPing;
     }

     state Done {}
}

machine PONG {
start state Pong_WaitPing {
        entry { }
        on Ping goto Pong_SendPong;
    }

    state Pong_SendPong {
	entry (payload: machine) {
	     send payload, Pong;
	     raise Success;		 	  
	}
        on Success goto Pong_WaitPing;
    }
}
*/
namespace TwoMachines
{
    public class Application : PStateImpl
    {
        #region Constructors
        public Application() : base()
        {
            //initialize all the fields
        }

        public override PStateImpl MakeSkeleton()
        {
            return new Application();
        }

        public Application(bool initialize) : base()
        {
            //create the main machine
            CreateMainMachine();
        }
        #endregion
        public static PrtEvent Ping = new PrtEvent("Ping", PrtType.NullType, )
    }
}



