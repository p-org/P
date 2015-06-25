// unreliable send 
static model fun _SEND(target:machine, e:event, p:any) {
	if ($)
		send target, e, p;
}

// reliable send
static model fun _SENDRELIABLE(target:machine, e:event, p:any) {
	send target, e, p;
}

static model fun _CREATECONTAINER(model_h: machine) : machine {
	model_h = new Container();
	return model_h;
}

// events
event Req_CreateMachine: (creator:machine, machineType: int, param: any);
event Resp_CreateMachine: machine;

machine Container
{
	var newMachine: machine;
	start state Init {
        on Req_CreateMachine do {
		    CreateLocalMachine(payload.machineType, payload.param);
			_SENDRELIABLE(payload.creator, Resp_CreateMachine, newMachine);
		};
    }
	
	fun CreateLocalMachine(machineType:int, param:any) {
		if (machineType == 1)
		{
			newMachine = new Node(param);
		}
		else
		{
			assert(false);
		}
	}
}




