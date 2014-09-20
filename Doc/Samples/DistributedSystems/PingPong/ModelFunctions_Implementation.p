GlobalFunctions 
\begin{GlobalFunctions}
	//necessary variable
	var sendPort:id;
    var receivePort:id;
    var initMessage:(nodemanager:id, param:any, sender:id, receiver:id);

    //common start state
    start state BootingState {
		entry {
			initMessage = ((nodemanager:id, param:any, sender:id, receiver:id))payload;
			sendPort = initMessage.sender;
            receivePort = initMessage.receiver;
            send(receivePort, hostM, this);
            raise(StartE, initMessage.param);
		}
		on StartE goto Init;
	}

	//send to the sender machine
	fun _SEND(target:id, e:eid, p:any) {
		send(sendPort, sendMessage, (source = this, target = target, e = e, p = p));
	}

    fun _SENDRELIABLE(target:id, e:eid, p:any) {
        send(sendPort, sendRelMessage, (source = this, target = target, e = e, p = p));
    }
	// This function sets up the entire VM and sets up the nodeManager.
	var model_s:id;
	var model_r:id;
	var model_h:id;
	model fun _CREATENODE() : id {
		//set up the VM
		model_s = new SenderMachine((nodemanager = null, param = 3));
		model_r = new ReceiverMachine((nodemanager = null, param = null));
        model_h = new NodeManager((nodemanager = null, param = null, sender = model_s, receiver = model_r));
		
		return model_r;
	}
	
	var createmachine_return:id;
	var createmachine_param:(nodeManager:id, typeofmachine:int, param:any);
	state _CREATEMACHINE {
		entry{
			//NOTE : That the create machine right now uses the P Send.
			_SENDRELIABLE(createmachine_param.nodeManager, Req_CreatePMachine, (creator = receivePort, typeofmachine = createmachine_param.typeofmachine, constructorparam = createmachine_param.param));
		}
        on Resp_CreatePMachine do PopState;
	}

	
	action PopState {
		createmachine_return = payload.receiver;
		return;
	}

\end{GlobalFunctions}

machine GodMachine 
\begin{GodMachine}
    
    var sendPort:id;
    var receivePort:id;

	// central server 
	model fun _CREATECENTRALSERVER()
	{
	
	}
	
    //send to the sender machine
	fun _SEND(target:id, e:eid, p:any) {
		send(sendPort, sendMessage, (source = this, target = target, e = e, p = p));
	}

    fun _SENDRELIABLE(target:id, e:eid, p:any) {
        send(sendPort, sendRelMessage, (source = this, target = target, e = e, p = p));
    }
	// This function sets up the entire VM and sets up the nodeManager.
	var model_s:id;
	var model_r:id;
	var model_h:id;
	
	model fun _CREATENODE() : id {
		//set up the VM
		model_s = new SenderMachine((nodemanager = null, param = 3));
		model_r = new ReceiverMachine((nodemanager = null, param = null));
        model_h = new NodeManager((nodemanager = null, param = null, sender = model_s, receiver = model_r));
		
		return model_r;
	}
	
	var createmachine_return:id;
	var createmachine_param:(nodeManager:id, typeofmachine:int, param:any);
	state _CREATEMACHINE {
		entry{
			//NOTE : That the create machine right now uses the P Send.
			_SENDRELIABLE(createmachine_param.nodeManager, Req_CreatePMachine, (creator = receivePort, typeofmachine = createmachine_param.typeofmachine, constructorparam = createmachine_param.param));
		}
        on Resp_CreatePMachine do PopState;
	}

	
	action PopState {
		createmachine_return = payload.receiver;
		return;
	}
\end{GodMachine}