// XYZs that event sent to a machine after it received the "halt" event is ignored by the halted machine
// Case when "halt" is not explicitly handled, hence, PONG instance should be "halted"
event Ping assert 1 : machine;
event Pong assert 1;
event Success;
event PingIgnored;
//event PongHalted;

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
				send pongId, PingIgnored;  //dequeuing of PingIgnored will not be in the runtime trace
				//raise PongHalted;
			}
			raise Success;
	}
        on Success goto Ping_WaitPong;
		//on PongHalted do {assert(false); ;} ; //reachable (used for validating the XYZ)
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
			on PingIgnored do {assert(false); } //unreachable
    }

    state Pong_SendPong {
	entry (payload: machine) {
	     send payload, Pong;
	     raise Success;		 	
	}
        on Success goto Pong_WaitPing;
    }
}
