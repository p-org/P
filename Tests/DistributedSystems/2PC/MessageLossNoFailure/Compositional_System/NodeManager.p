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
	var host:id;
	var receiver:id;
	
	state Init {
        on Req_CreatePMachine goto CreateNewMachine;
    }
	state CreateNewMachine {
		entry {

			sender = new SenderMachine((nodemanager = this, param = null));
            receiver = new ReceiverMachine((nodemanager = this, param = null));
			//switch case
			
			if(payload.typeofmachine == 1)
				host = new PONG((nodemanager = this, param = payload.constructorparam, sender = sender, receiver = receiver));
			else if(payload.typeofmachine == 2)
				host = new PING((nodemanager = this, param = payload.constructorparam, sender = sender, receiver = receiver));
			
			_SENDRELIABLE(payload.creator, Resp_CreatePMachine, (receiver = receiver));
			

		}
		
		on Req_CreatePMachine goto CreateNewMachine;
	}
	
\end{NodeManager}