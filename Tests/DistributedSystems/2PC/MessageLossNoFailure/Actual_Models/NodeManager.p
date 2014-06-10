//The node manager machine is the first machine to be created on a new VM.
//All new machines created on this VM are created by the NodeManager. 
//It should be capable of creating all types of machines

//Creating a new machine involves following stages.
//Stage 1: create the sender machine which acts the sendPort for the machine to be created.
//Stage 2: create the host machine and make it point to the sender machine, it sends all its messages to the
// sender machine.
//Stage 3: create the receiver machine and it points to the host machine. Receiver machine forwards all the
// messages to the host machine.
// Receiver machine is the external point for contact for the host machine and hence is returned by the 
// NodeManager as a response to createMachine request.

/// Replace this with proper enum types but for the time-being the mapping between int -> machineType is


// 1 -> coordinatorMachine
// 2 -> replicaMachine
// 3 -> clientMachine


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
		{new Coordinator((nodemanager = this, param = p, sender = sender, receiver = receiver));}
		else if(typeofmachine == 2)
			{new Replica((nodemanager = this, param = p, sender = sender, receiver = receiver));}
		else if(typeofmachine == 3)
			{new Client((nodemanager = this, param = p, sender = sender, receiver = receiver));}
		else
		{
			assert(false);
		}
	}
\end{NodeManager}