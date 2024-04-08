// P semantics XYZ: two machines, machine is halted with "raise halt" (unhandled)

event Ping assert 1 : machine;
event Pong assert 1;
event Success;
event PongIgnored;

machine Main {
    var pongId: machine;
	var count1: int;
    start state Ping_Init {
        entry {
			pongId = new PONG();
			raise Success;   	
        }
        on Success goto Ping_SendPing;
    }

    state Ping_SendPing {
        entry {
			count1 = count1 + 1;
			if (count1 == 1) {
				send pongId, Ping, this;
				}
			if (count1 == 2) {
				send pongId, Ping, this;
			    raise halt;
				}
			raise Success;
		}
        on Success goto Ping_WaitPong;
    }

    state Ping_WaitPong {
		on Pong goto Ping_SendPing;
		on PongIgnored do { assert(false); }   //unreachable
     }
}

machine PONG {
	var count2: int;
    start state Pong_WaitPing {
        entry { }
			on Ping goto Pong_SendPong;
    }

    state Pong_SendPong {
	entry (payload: machine) {
		count2 = count2 + 1;
		if (count2 == 1) {
			 send payload, Pong;			 	
			}
		if (count2 == 2) {
			send payload, PongIgnored;		
			}
		raise Success;	
	}
        on Success goto Pong_WaitPing;
    }
}
