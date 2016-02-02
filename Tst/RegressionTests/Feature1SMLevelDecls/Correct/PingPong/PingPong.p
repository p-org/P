event Ping assert 1: machine;
event Pong assert 2: machine;
event Success;
event M_Ping;
event M_Pong;

include "PrtDistHelp.p"

machine PING 
{
    var pongMachine: (machine,machine);

    start state Init {
        entry (payload: (machine, machine)) {
			pongMachine = payload;
			raise (Success);   	   
        }
        on Success goto SendPing;
    }

    state SendPing {
        entry {
			monitor M_Ping;
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
	    entry (payload: machine) {
	        monitor M_Pong;
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


spec M monitors M_Ping, M_Pong {
    start state ExpectPing {
        on M_Ping goto ExpectPong_1;
    }

    state ExpectPong_1 {
        on M_Pong goto ExpectPong_2;
		
    }
	
	state ExpectPong_2 {
        on M_Pong goto ExpectPing;
    }
}

static fun _CREATEMACHINE(cner: machine, typeOfMachine: int, param : any, newMachine: machine) : machine
{
	if(typeOfMachine == 1)
	{
		newMachine = new PING(param as (machine, machine));
	}
	else if(typeOfMachine == 2)
	{
		newMachine = new PONG();
	}
	else
	{
		assert(false);
	}
	return newMachine;
}

main machine GodMachine 
{
	var container : machine;
    var pongMachine_1: machine;
	var pongMachine_2: machine;

    start state Init {
	    entry {
			container = _CREATECONTAINER();
			pongMachine_1 = _CREATEMACHINE(container, 2, null, null);
			container = _CREATECONTAINER();
			pongMachine_2 = _CREATEMACHINE(container, 2, null, null);
			container = _CREATECONTAINER();
			_CREATEMACHINE(container, 1, (pongMachine_1, pongMachine_2), null);
	    }
	}
}
