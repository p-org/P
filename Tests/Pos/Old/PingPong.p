event Ping:mid assert 1;
event Pong assert 1;
event Success;
event Bye;

main machine PING {
    var pongId: mid;
    var ctr:int;

    start state Ping_Init {
        entry {
        ctr = 0;
  	    pongId = new PONG();
	    raise (Success);   	   
        }
        on Success goto Ping_SendPing;
    }

    state Ping_SendPing {
        entry {
        if (ctr > 2) raise(Bye);
	    send (pongId, Ping, this);
	    raise (Success);
	}
        on Success goto Ping_WaitPong;
        on Bye goto Done;
     }

     state Ping_WaitPong {
	entry { ctr = ctr + 1; }
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
