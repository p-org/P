// This sample enhances PingPong.p with a push statement.  
// It has unbounded reachable states.
event Ping assert 1 : machine;
event Pong assert 1;
event Success assert 1;

main machine PING {
    var pongId: machine;

    start state Ping_Init {
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
	// This push causes stack to grow unbounded because there is no corresponding pop
        on Pong push Ping_SendPing;  
     }

}

machine PONG {
    start state Pong_WaitPing {
        entry { }
        on Ping goto Pong_SendPong;
    }

    state Pong_SendPong {
	entry {
	     send payload as machine, Pong;
	     raise Success;		 	  
	}
        on Success goto Pong_WaitPing;
    }
}
