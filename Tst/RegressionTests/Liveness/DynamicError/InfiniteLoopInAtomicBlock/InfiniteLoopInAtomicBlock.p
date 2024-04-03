// Zinger XYZ: infinite loop in atomic block
// XYZs that event sent to a machine after it raised the "halt" event is ignored by the halted machine

event Ping assert 1 : machine;
event Pong assert 1;
event Success;
event TimeToHalt;
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
				raise Success;
				}
			if (count1 == 2) {
				send pongId, Ping, this;
			    raise TimeToHalt;
				}		
		}
        on Success goto Ping_WaitPong;
		on TimeToHalt goto Ping_Halt;
    }

    state Ping_WaitPong {
		on Pong goto Ping_SendPing;
		on PongIgnored do { assert(false); } 	//unreachable
     }
	state Ping_Halt {
		entry (payload: any) {
			raise halt;
			}
		on halt goto Ping_Halt;     //stopping
		on Pong goto Ping_SendPing;
		on PongIgnored do { assert(false); }	//reachable
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
