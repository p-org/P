
//unreliable send 
static model fun _SEND(target:machine, e:event, p:any) {
	if($)
		send target, e, p;
}

//perform a reliable send
static model fun _SENDRELIABLE(target:machine, e:event, p:any) {
	send target, e, p;
}

static model fun _CREATECONTAINER(model_h: machine) : machine {
	model_h = new Container();
	return model_h;
}

//Events
event Req_CreateMachine:(creator:machine, typeofmachine: int, constructorparam: any);
event Resp_CreateMachine : machine;

machine Container
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
}




