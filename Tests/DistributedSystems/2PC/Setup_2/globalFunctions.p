GlobalFunctions {
	
	//send to the sender machine
	fun _SEND(target:id, e:eid, p:any) : bool {
		send(sendPort, sendMessage, (target = target, e = e, p = p));
	}

	// This function sets up the entire VM and sets up the nodeManager.
	var model_s:id;
	var model_r:id;
	var model_h:id;
	model fun _CREATENODE() : id {
		//set up the VM
		model_s = new SenderMachine((nodemanager = null, param = null));
		model_h = new NodeManager((nodemanager = null, param = null, sender = model_s));
		model_r = new ReceiverMachine((nodemanager = this, param = null, host = model_h));
		return model_r;
	}
	
	var func_return:id;
	fun _CREATEMACHINE(nodeManager:id, typeofmachine:int, param:any) : id {
		_SEND(nodeManager, Req_CreatePMachine, (creator = this, typeofmachine = typeofmachine, constructorparam = param));
		call(DeferAll_CreateMachine);
		return func_return;
	}
	
	state DeferAll_CreateMachine {
		on Resp_CreatePMachine do PopState;
	}
	
	action PopState {
		func_return = payload.receiver;
		return;
	}
}