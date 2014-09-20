

//Events
event Req_CreatePMachine:(creator:id, typeofmachine: int, constructorparam: any);
event Resp_CreatePMachine : (receiver:id);

machine NodeManager
\begin{NodeManager}
	
	var sender:id;
	var receiver:id;
	
	state Init {
        on Req_CreatePMachine goto CreateNewMachine;
    }
	state CreateNewMachine {
		entry {

			sender = new SenderMachine((nodemanager = this, param = 3));
            receiver = new ReceiverMachine((nodemanager = this, param = null));
			_CREATELOCALMACHINE(payload.typeofmachine, payload.constructorparam, sender, receiver);
			_SENDRELIABLE(payload.creator, Resp_CreatePMachine, (receiver = receiver));
			
		}
		
		on Req_CreatePMachine goto CreateNewMachine;
	}
	
	fun _CREATELOCALMACHINE(typeofmachine:int, p:any, sender:id, receiver:id) {
		
		if(typeofmachine == 1)
		{
			new PONG((nodemanager = this, param = p, sender = sender, receiver = receiver));
		}
		else if(typeofmachine == 2)
		{
			new PING((nodemanager = this, param = p, sender = sender, receiver = receiver));
		}
		else if(typeofmachine == 3)
		{
			new CentralServer((nodemanager = this, param = p, sender = sender, receiver = receiver));
		}
		else
		{
			assert(false);
		}
	}
\end{NodeManager}