// This file tests NodeManager functionality.
// It consists of testing the _CREATEMACHINE and _CREATENODE API.
// We Create 2 machines Ping and Pong on two different Nodes and make them communicate with each other.
// The Example asserts that a Ping is Followed by sending of a Pong.

event Ping:id assert 1;
event Pong assert 1;
event Success;

machine PING 
\begin{PING}
    var pongId: id;

    state Init {
        entry {
			pongId = (id)payload;
			raise (Success);   	   
        }
        on Success goto Ping_SendPing;
    }

    state Ping_SendPing {
        entry {
	    invoke M(Ping);
	    _SEND(pongId, Ping, receivePort);
	    raise (Success);
	}
        on Success goto Ping_WaitPong;
     }

     state Ping_WaitPong {
        on Pong goto Done;
     }

    state Done { }
\end{PING}


machine PONG 
\begin{PONG}
    state Init {
        entry { 
		}
        on Ping goto Pong_SendPong;
    }

    state Pong_SendPong {
	entry {
	     invoke M(Pong);
	     _SEND((id) payload, Pong, null);
	     raise (Success);		 	  
	}
        on Success goto End;
    }
	
	state End{}
\end{PONG}

monitor M {
    start stable state ExpectPing {
        on Ping goto ExpectPong;
    }

    state ExpectPong {
		entry {}
        on Pong goto ExpectPing;
		
    }
}


main machine GodMachine 
\begin{GodMachine}
    var PongMachine: id;
	var temp_NM : id;

    start state Init {
	    entry {
			new M();
			//Let me create my own sender/receiver
			sendPort = new SenderMachine((nodemanager = null, param = 2));
            receivePort = new ReceiverMachine((nodemanager = null, param = null));
            send(receivePort, hostM, this);	
			
			temp_NM = _CREATENODE();
			createmachine_param = (nodeManager = temp_NM, typeofmachine = 1, param = null);
			call(_CREATEMACHINE);
			PongMachine = createmachine_return;
			temp_NM = _CREATENODE();
			createmachine_param = (nodeManager = temp_NM, typeofmachine = 2, param = PongMachine);
			call(_CREATEMACHINE);
	    }
	}
\end{GodMachine}