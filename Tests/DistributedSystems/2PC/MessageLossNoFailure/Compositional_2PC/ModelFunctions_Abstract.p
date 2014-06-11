GlobalFunctions 
\begin{GlobalFunctions}
	//necessary variable
    var initMessage:(param:any);

    //common start state
    start state BootingState {
		entry {
			initMessage = ((param:any))payload;
            raise(StartE, initMessage.param);
		}
		on StartE goto Init;
	}

	//send to the sender machine
	model fun _SEND(target:id, e:eid, p:any) {
		if(*)
			send(target, e, p);
	}

    fun _SENDRELIABLE(target:id, e:eid, p:any) {
        send(target, e, p);
    }
	
	model fun _CREATENODE() : id {
		return null;
	}
	
	var createmachine_return:id;
	var createmachine_param:(nodeManager:id, typeofmachine:int, param:any);
	state _CREATEMACHINE {
		entry{
			if(createmachine_param.typeofmachine == 1)
				createmachine_return = new Coordinator((param = createmachine_param.param));
			else if(createmachine_param.typeofmachine == 2)
				createmachine_return = new Replica((param = createmachine_param.param));
			else if(createmachine_param.typeofmachine == 3)
				createmachine_return = new Client((param = createmachine_param.param));
			return;
		}
	}

\end{GlobalFunctions}

machine GodMachine 
\begin{GodMachine}


    //send to the sender machine
	fun _SEND(target:id, e:eid, p:any) {
		send(target, e, p);
	}

    fun _SENDRELIABLE(target:id, e:eid, p:any) {
        send(target, e, p);
    }
	
	model fun _CREATENODE() : id {
		return null;
	}
	
	var createmachine_return:id;
	var createmachine_param:(nodeManager:id, typeofmachine:int, param:any);
	state _CREATEMACHINE {
		entry{
			if(createmachine_param.typeofmachine == 1)
				createmachine_return = new Coordinator((param = createmachine_param.param));
			else if(createmachine_param.typeofmachine == 2)
				createmachine_return = new Replica((param = createmachine_param.param));
			else if(createmachine_param.typeofmachine == 3)
				createmachine_return = new Client((param = createmachine_param.param));
			return;
		}
	}
\end{GodMachine}