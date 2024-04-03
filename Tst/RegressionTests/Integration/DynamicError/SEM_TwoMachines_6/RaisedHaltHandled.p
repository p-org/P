// P semantics XYZ: two machines, machine is halted with "raise halt" (handled)
// This XYZ is for the case when "halt" is explicitly handled - hence, it is processed as any other event.
//
event Ping assert 1 : machine;
event Pong assert 1;
event Success;
event PongIgnored;
//event PongHalted;

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
			send payload, Pong;
			raise halt;			
			}
		raise Success;	
	}
	on halt do { assert(false); }   //reachable
    on Success goto Pong_WaitPing;
    }
}
