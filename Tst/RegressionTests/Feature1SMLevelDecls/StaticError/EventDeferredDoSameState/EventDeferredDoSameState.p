event PING assert 1 : machine;
event PONG assert 1;
event SUCCESS;

machine Main {
    var server: machine;
	var XYZ: bool;
    start state Init {
        entry {
  	    server = new Server();
	    raise SUCCESS;   	
        }
		//static error:
		on PING do Action1;
		defer PING;
    }

    state SendPing {
        entry {
	    send server, PING, this;
	    raise SUCCESS;
	}
        on SUCCESS goto WaitPong;
     }

     state WaitPong {
		on PONG do Action1;
     }
	
	 fun Action1() {
		XYZ = true;
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
