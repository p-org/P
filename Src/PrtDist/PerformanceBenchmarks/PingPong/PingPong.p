// This file tests Network functionality.
// Two Pong Machines are created on the same node as they use the same NodeManager
// Ping Machine sends message to them and they respond by Pong. This repeats. We use Reliable sends.
// We Create 2 machines Ping and Pong on two different Nodes and make them communicate with each other.
// The Example asserts that a Ping is Followed by sending of a Pong message by each message.

event Ping assert 1: machine;
event Pong assert 2: machine;
event Success;

//unreliable send 
static model fun _SEND(target:machine, e:event, p:any) {
	if($)
		send target, e, p;
}

//perform a reliable send
static model fun _SENDRELIABLE(target:machine, e:event, p:any) {
	send target, e, p;
}

static model fun _CREATENODE(model_h: machine) : machine {
	model_h = new NodeManager();
	return model_h;
}
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
			
			temp_NM = _CREATENODE(null);
			createmachine_param = (nodeManager = temp_NM, typeofmachine = 1, param = null);
			push _CREATEMACHINE;
			
			PongMachine_1 = createmachine_return;
			createmachine_param = (nodeManager = temp_NM, typeofmachine = 1, param = null);
			push _CREATEMACHINE;
			PongMachine_2 = createmachine_return;
			temp_NM = _CREATENODE(null);
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


//Events
event Req_CreateMachine:(creator:machine, typeofmachine: int, constructorparam: any);
event Resp_CreateMachine : machine;

machine NodeManager
{

	
	var newMachine: machine;
	start state Init {
        on Req_CreateMachine goto CreateNewMachine;
    }
	state CreateNewMachine {
		entry {
			
			_CREATELOCALMACHINE(payload.typeofmachine, payload.constructorparam);
			_SENDRELIABLE(payload.creator, Resp_CreateMachine, newMachine);
			
		}
		
		on Req_CreateMachine goto CreateNewMachine;
	}
	
	
	fun _CREATELOCALMACHINE(typeofmachine:int, param:any) {
		
		if(typeofmachine == 1)
		{
			newMachine = new PONG(param);
		}
		else if(typeofmachine == 2)
		{
			newMachine = new PING(param);
		}
		else
		{
			assert(false);
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

