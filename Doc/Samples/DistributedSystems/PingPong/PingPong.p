// This file tests Network functionality.
// Two Pong Machines are created on the same node as they use the same NodeManager
// Ping Machine sends message to them and they respond by Pong. This repeats. We use Reliable sends.
// We Create 2 machines Ping and Pong on two different Nodes and make them communicate with each other.
// The Example asserts that a Ping is Followed by sending of a Pong message by each message.

event Ping:id assert 1;
event Pong assert 2;
event Success;

machine PING 
\begin{PING}
    var pongId: (id,id);

    state Init {
        entry {
			pongId = ((id,id))payload;
			raise (Success);   	   
        }
        on Success goto Ping_SendPing;
    }

    state Ping_SendPing {
        entry {
	    invoke M(Ping);
	    _SEND(pongId[0], Ping, receivePort);
		_SEND(pongId[1], Ping, receivePort);
	    raise (Success);
	}
        on Success goto Ping_WaitPong_1;
     }

     state Ping_WaitPong_1 {
        on Pong goto Ping_WaitPong_2;
     }

	 state Ping_WaitPong_2 {
        on Pong goto Done;
     }
    state Done {}
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
        on Ping goto ExpectPong_1;
    }

    state ExpectPong_1 {
		entry {}
        on Pong goto ExpectPong_2;
		
    }
	
	state ExpectPong_2 {
		entry {}
        on Pong goto ExpectPing;
		
    }
}


main machine GodMachine 
\begin{GodMachine}
    var PongMachine_1: id;
	var PongMachine_2: id;
	var temp_NM : id;

    start state Init {
	    entry {
			new M();
			//Let me create my own sender/receiver
			sendPort = new SenderMachine((nodemanager = null, param = 3));
            receivePort = new ReceiverMachine((nodemanager = null, param = null));
            send(receivePort, hostM, this);	
			
			//create central server 
			_CREATECENTRALSERVER();
			temp_NM = _CREATENODE();
			createmachine_param = (nodeManager = temp_NM, typeofmachine = 1, param = null);
			call(_CREATEMACHINE);
			PongMachine_1 = createmachine_return;
			createmachine_param = (nodeManager = temp_NM, typeofmachine = 1, param = null);
			call(_CREATEMACHINE);
			PongMachine_2 = createmachine_return;
			temp_NM = _CREATENODE();
			createmachine_param = (nodeManager = temp_NM, typeofmachine = 2, param = (PongMachine_1,PongMachine_2));
			call(_CREATEMACHINE);
	    }
	}
\end{GodMachine}