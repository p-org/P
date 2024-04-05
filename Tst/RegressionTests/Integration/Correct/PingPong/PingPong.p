event Ping assert 1 : machine;
event Pong assert 1;
event Success;

machine Main {
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
	entry (payload: machine) {
	     send payload, Pong;
	     raise Success;		 	
	}
        on Success goto Pong_WaitPing;
    }
}
