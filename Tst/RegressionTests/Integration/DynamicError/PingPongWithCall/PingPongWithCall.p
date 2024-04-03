// why this XYZ is in the "DynamicError" category:
// the number of reachable states is unbounded"
// unlimited number of "push" transitions with no "pop"
// so zing is not running on this XYZ
event Ping assert 1 : machine;
event Pong assert 1;
event Success assert 1;

machine Main {
    var pongId: machine;

    start state Ping_Init {
        entry {
  	    pongId = new PONG();
	    raise Success;   	
        }
        on Success goto Ping_SendPing;
    }
    var counter: int;
    state Ping_SendPing {
      entry {
	     if(counter > 100)
       {
          assert false;
       }
       else
       {
          counter = counter + 1;
       }
       send pongId, Ping, this;
	     raise Success;
	}
        on Success goto Ping_WaitPong;
     }

     state Ping_WaitPong {
        on Pong goto Ping_SendPing;
     }

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
