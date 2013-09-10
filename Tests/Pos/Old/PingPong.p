event Ping:mid assert 1;
event Pong assert 1;
event Success;

main machine PING {
    var pongId: mid;

    start state Ping_Init {
        entry {
  	    pongId = new PONG();
	    raise (Success);   	   
        }
        on Success goto Ping_SendPing;
    }

    state Ping_SendPing {
        entry {
	    send (pongId, Ping, this);
	    raise (Success);
	}
        on Success goto Ping_WaitPong;
     }

     state Ping_WaitPong {
        on Pong goto Ping_SendPing;
     }

    state Done { }
}

machine PONG {
    start state Pong_WaitPing {
        entry { }
        on Ping goto Pong_SendPong;
    }

    state Pong_SendPong {
	entry {
	     send ((mid) payload, Pong);
	     raise (Success);		 	  
	}
        on Success goto Pong_WaitPing;
    }
}
