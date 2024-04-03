event Ping: machine;
event Pong;
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
        on Pong do {
            jumpToDoneNonDeterministic();
            if ($) {
                goto Ping_SendPing;
            }
        }
     }

     fun jumpToDoneNonDeterministic() {
        if ($) {
            jumpToDone();
        }
     }

     fun jumpToDone() {
        goto Done;
     }

    state Done {
        ignore Pong;
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
