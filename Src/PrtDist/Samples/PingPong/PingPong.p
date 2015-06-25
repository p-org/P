event Ping assert 1: machine;
event Pong assert 2: machine;
event Success;

include "PrtDistHelp.p"

machine PING 
{
    var pongMachine: (machine,machine);

    start state Init {
        entry {
			pongMachine = payload as (machine, machine);
			raise (Success);   	   
        }
        on Success goto SendPing;
    }

    state SendPing {
        entry {
	        monitor M, Ping;
			_SEND(pongMachine.0, Ping, this);
			_SEND(pongMachine.1, Ping, this);
			raise (Success);
	    }
        on Success goto WaitPong_1;
     }

     state WaitPong_1 {
        on Pong goto WaitPong_2;
     }

	 state WaitPong_2 {
        on Pong goto Done;
     }

     state Done {}
}

machine PONG
{
    start state Init {
        on Ping goto SendPong;
    }

    state SendPong {
	    entry {
	        monitor M, Pong;
			_SEND(payload, Pong, this);
			raise (Success);		 	  
	    }
        on Success goto End;
    }
	
	state End {
		entry {
			raise(halt);
		}
	}
}


monitor M {
    start state ExpectPing {
        on Ping goto ExpectPong_1;
    }

    state ExpectPong_1 {
        on Pong goto ExpectPong_2;
		
    }
	
	state ExpectPong_2 {
        on Pong goto ExpectPing;
    }
}

main machine GodMachine 
{
	var container : machine;
    var pongMachine_1: machine;
	var pongMachine_2: machine;

    start state Init {
	    entry {
			new M();
			
			container = _CREATECONTAINER(null);
			createMachine_param = (container = container, machineType = 1, param = null);
			push CreateMachine;
			pongMachine_1 = createMachine_return;
			createMachine_param = (container = container, machineType = 1, param = null);
			push CreateMachine;
			pongMachine_2 = createMachine_return;
			
			container = _CREATECONTAINER(null);
			createMachine_param = (container = container, machineType = 2, param = (pongMachine_1, pongMachine_2));
			push CreateMachine;
	    }
	}
	
	var createMachine_param: (container: machine, machineType:int, param:any);
	var createMachine_return:machine;
	state CreateMachine {
		entry {
			_SENDRELIABLE(createMachine_param.container, Req_CreateMachine, 
			              (creator = this, machineType = createMachine_param.machineType, param = createMachine_param.param));
		}
        on Resp_CreateMachine do PopState;
	}

	fun PopState() {
		createMachine_return = payload;
		pop;
	}
}
