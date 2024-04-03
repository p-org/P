// XYZs that event sent to a machine after it received the "halt" event is ignored by the halted machine
// Case when "halt" is explicitly handled
event Ping assert 1 : machine;
event Pong assert 1;
event Success;
event PingIgnored;

machine Main {
    var pongId: machine;
	var count: int;
    start state Ping_Init {
        entry {
			pongId = new PONG();
			raise Success;   	
        }
        on Success goto Ping_SendPing;
    }

    state Ping_SendPing {
        entry {
			count = count + 1;
			if (count == 1) {
				send pongId, Ping, this; }
			// halt PONG after one exchange:
			if (count == 2) {
				send pongId, halt;
				send pongId, PingIgnored;
			}
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
			on halt goto  Pong_Halt;
			
    }

    state Pong_SendPong {
	entry (payload: machine) {
	     send payload, Pong;
	     raise Success;		 	
	}
        on Success goto Pong_WaitPing;
		on PingIgnored do {assert(false); } //unreachable
    }
	state Pong_Halt {
			ignore Ping;
		    ignore PingIgnored;
	}
}
