event Ping assert 1 : machine;
event Pong assert 1;
event Success assert 1;
event Ding assert 1;
event Dong assert 1;


machine Main {
    var pongId: machine;

    start state Ping_start {
        entry {
  	    pongId = new PONG(this);
	    raise Success;   	
        }
        on Success goto Ping_ping1;
    }

    state Ping_ping1 {
        entry {
	    send pongId, Ping, this;
	    	goto Ping_ding1;
		}
		
    }

     state Ping_ding1 {
		entry {
			send pongId, Ding;
		}
        on Dong goto Ping_ping2;
    }
	
	state Ping_ping2 {
		entry {
		}
		on Pong goto Ping_ping1;
	}
	
}

machine PONG {
	var pingid: machine;

    start state _Init {
	entry (payload: machine) { pingid = payload; raise Success; }
        on Success goto Pong_start;
    }

    state Pong_start {
        entry { }
        on Ping goto Pong_dong1;
		
    }

    state Pong_dong1 {
        on Ding goto Pong_dong2;
    }
	
	state Pong_dong2 {
		entry {
			send pingid, Dong;
			raise Success;
		}
		on Success goto Pong_pong1;
	}
	
	state Pong_pong1 {
		entry {
			send pingid, Pong;
			raise Success;
		}
		on Success goto Pong_start;
	}
			
}
