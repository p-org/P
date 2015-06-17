event Ping assert 1: machine;
event Pong assert 2: machine;
event Success;

include "PrtDisthelp.p"

machine PING 
{

    var pongmachine: (machine,machine);

    start state Init {
        entry {
			pongmachine = payload as (machine, machine);
			raise (Success);   	   
        }
        on Success goto Ping_SendPing;
    }

    state Ping_SendPing {
        entry {
	    monitor M, Ping;
	    _SEND(pongmachine.0, Ping, this);
		_SEND(pongmachine.1, Ping, this);
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

	var createmachine_return:machine;
	var createmachine_param:(nodeManager:machine, typeofmachine:int, param:any);
	state _CREATEMACHINE {
		entry{
			//NOTE : That the create machine right now uses the P Send.
			_SENDRELIABLE(createmachine_param.nodeManager, Req_CreateMachine, (creator = this, typeofmachine = createmachine_param.typeofmachine, constructorparam = createmachine_param.param));
		}
        on Resp_CreateMachine do PopState;
	}

	
	fun PopState() {
		createmachine_return = payload;
		pop;
	}


}


machine PONG
{

    start state Init {
        entry { 
			
		}
        on Ping goto Pong_SendPong;
    }

    state Pong_SendPong {
	entry {
	     monitor M, Pong;
	     _SEND(payload, Pong, this);
	     raise (Success);		 	  
	}
        on Success goto End;
    }
	
	state End{
		entry {
			raise(halt);
		}
	}
	
	var createmachine_return:machine;
	var createmachine_param:(nodeManager:machine, typeofmachine:int, param:any);
	state _CREATEMACHINE {
		entry{
			//NOTE : That the create machine right now uses the P Send.
			_SENDRELIABLE(createmachine_param.nodeManager, Req_CreateMachine, (creator = this, typeofmachine = createmachine_param.typeofmachine, constructorparam = createmachine_param.param));
		}
        on Resp_CreateMachine do PopState;
	}

	
	fun PopState() {
		createmachine_return = payload;
		pop;
	}
}


monitor M {
    start state ExpectPing {
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
{
    var PongMachine_1: machine;
	var PongMachine_2: machine;
	var temp_NM : machine;

    start state Init {
	    entry {
			new M();
			
			temp_NM = _CREATECONTAINER(null);
			createmachine_param = (nodeManager = temp_NM, typeofmachine = 1, param = null);
			push _CREATEMACHINE;
			
			PongMachine_1 = createmachine_return;
			createmachine_param = (nodeManager = temp_NM, typeofmachine = 1, param = null);
			push _CREATEMACHINE;
			PongMachine_2 = createmachine_return;
			temp_NM = _CREATECONTAINER(null);
			createmachine_param = (nodeManager = temp_NM, typeofmachine = 2, param = (PongMachine_1, PongMachine_2));
			push _CREATEMACHINE;
	    }
	}
	
	var createmachine_return:machine;
	var createmachine_param:(nodeManager:machine, typeofmachine:int, param:any);
	state _CREATEMACHINE {
		entry{
			//NOTE : That the create machine right now uses the P Send.
			_SENDRELIABLE(createmachine_param.nodeManager, Req_CreateMachine, (creator = this, typeofmachine = createmachine_param.typeofmachine, constructorparam = createmachine_param.param));
		}
        on Resp_CreateMachine do PopState;
	}

	
	fun PopState() {
		createmachine_return = payload;
		pop;
	}


}
