GlobalFunctions {
	
	//send to the sender machine
	fun _SEND(target:id, e:eid, p:any) {
		send(sendPort, sendMessage, (source = this, target = target, e = e, p = p));
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
	
	var createmachine_return:id;
	var createmachine_param:(nodeManager:id, typeofmachine:int, param:any);
	state _CREATEMACHINE {
		entry{
			//NOTE : That the create machine right now uses the P Send.
			_SEND(createmachine_param.nodeManager, Req_CreatePMachine, (creator = this, typeofmachine = createmachine_param.typeofmachine, constructorparam = createmachine_param.param));
			call(DeferAll_CreateMachine);
			return;
		}
	}
	
	state DeferAll_CreateMachine {
		on Resp_CreatePMachine do PopState;
	}
	
	action PopState {
		createmachine_return = payload.receiver;
		return;
	}
}