event createmachine:(creator:id, type: int, parameter: any);
event newMachineCreated:id;
event unit;
event SenderPort:id;
event sendMessage:(target:id, e:eid, p:any);
event networkMessage:(iden:(source:id, seqnum:int), msg:(e:eid, p:any));

main machine NetworkMachine {
	var hostMachine: id;
	var temp:id;
	start state bootingState {
		entry {
			hostMachine = (id)payload;
			temp = new SenderMachine(hostMachine);
			send(hostMachine, SenderPort, temp);
			temp = new ReceiverMachine(hostMachine);
			raise(delete);
		}
		
	}
}


machine ReceiverMachine {
	var hostMachine:id;
	var lastReceivedMessage: map[id, int];
	
	start state bootingState {
		entry {
			hostMachine = (id)payload;
			
		}
		on networkMessage goto Listening;
		
	}
	
	state Listening {
		entry {
			if(payload.iden.source in lastReceivedMessage)
			{
				if(payload.iden.seqnum > lastReceivedMessage[payload.iden.source])
				{
					send(hostMachine, payload.msg.e, payload.msg.p);
					lastReceivedMessage.update(payload.iden.source, payload.iden.seqnum);
				}
			}
			else
			{
				send(hostMachine, payload.msg.e, payload.msg.p);
				lastReceivedMessage.update(payload.iden.source, payload.iden.seqnum);
			}
		}
		on networkMessage goto Listening;
	}

}

machine SenderMachine {
	var hostMachine:id;
	var numberofRetry: int; 
	var sendFail:bool;
	var i: int;
	var CurrentSeqNum:int;
	start state bootingState {
		entry {
			hostMachine = (id)payload;
			numberofRetry = 1;
			sendFail = true;
			CurrentSeqNum = 0;
		}
		on sendMessage goto Listening;
	}
	
	state Listening {
		entry {
			i = numberofRetry;
			while(i != 0 && sendFail)
			{
				sendRPC(payload.target, networkMessage, ((hostMachine, CurrentSeqNum),(payload.e, payload.p)));
				i = i - 1;
			}
		}
		
		on sendMessage goto Listening;
	}
	
	model fun sendRPC(target:id, e:eid, p:any) : bool {
		
		if(*)
		{
			return false;
		}
		else
		{
			send(target, e, p);
			if(*)
				return false;
			else
				return true;
		}
	}
}


machine MachineCreator
{
	var tempparameter:any;
	var typeofMachine : int; // 0 : coordinator, 1 : replica, 2 : client 
	var tempNetworkMachine:id;
	var tempHostMachine:id;
	var createReqter:id;
	start state bootingState {
		entry {
			raise(unit);
		}
		on unit goto CreateMachineS;
	}
	
	state CreateMachineS {
		entry {
			typeofMachine = payload.type;
			tempparameter = payload.parameter;
			
			if(typeofMachine == 0)
			{	
				tempHostMachine = new Coordinator(tempparameter);
			}
			else if (typeofMachine == 1)
			{
				tempHostMachine = new Replica(tempparameter);
			}
			else
			{
				tempHostMachine = new Client(tempparameter);
			}
			
			tempNetworkMachine = new NetworkMachine(tempHostMachine);
			send(createReqter, newMachineCreated, tempNetworkMachine);
		}
		on createmachine goto CreateMachineS;
	}
	
	
}
