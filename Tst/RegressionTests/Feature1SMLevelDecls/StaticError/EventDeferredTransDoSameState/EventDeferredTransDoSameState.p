//This is a repro for a bug in static checking:
//SUCCESS is deferred and there's a transition on SUCCESS in the same state
event PING assert 1 : machine;
event PONG assert 1;
event SUCCESS;

main machine Client {
    var server: machine;
	var test: bool;
    start state Init {
        entry {
  	    server = new Server();
	    raise SUCCESS;   	   
        }
		on PING do Action1;  
    }

    state SendPing {
        entry {
	    send server, PING, this;
	    raise SUCCESS;
	}
		//No error is detected:
        on SUCCESS goto WaitPong;  
		defer SUCCESS;
     }

     state WaitPong {
		on PONG do Action1;
     }
	 
	 fun Action1() {
		test = true;
    }
}

machine Server {
    start state WaitPing {
        on PING goto SendPong;
    }

    state SendPong {
		entry (payload: machine) {
			send payload, PONG;
			raise SUCCESS;		 	  
		}
        on SUCCESS goto WaitPing;
    }
}
