event Ping assert 1 : machine;
event Pong assert 1;
event Success;

machine Main {
    var pongId: machine;

    start state Ping_Init {
        entry {
      	    pongId = new PONG();
    	    send this, Success;
        }
        on Success goto Ping_SendPing;
    }

    state Ping_SendPing {
        entry {
            send pongId, Ping, this;
            send this, Success;
	    }
        on Success goto Ping_WaitPong;
        defer Pong;
     }

     state Ping_WaitPong {
        on Pong goto Ping_SendPing;
     }

    state Done {
        entry {
            assert false, format ("Assertion reached");
        }
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
