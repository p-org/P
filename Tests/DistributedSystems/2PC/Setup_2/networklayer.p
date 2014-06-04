
event sendMessage:(source:id, target:id, e:eid, p:any);
event networkMessage:(iden:(source:id, seqnum:int), msg:(e:eid, p:any));



machine ReceiverMachine {
	var hostMachine:id;
	var lastReceivedMessage: map[id, int];
	var initMessage:(nodemanager:id, param:any, host:id);
	start state bootingState {
		entry {
			initMessage = ((nodemanager:id, param:any, host:id))payload;
			hostMachine = initMessage.host;
			
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
	var numberofRetry: int; 
	var sendFail:bool;
	var i: int;
	var CurrentSeqNum:int;
	start state bootingState {
		entry {
			numberofRetry = 1;
			sendFail = false;
			CurrentSeqNum = 0;
		}
		on sendMessage goto Listening;
	}
	
	state Listening {
		entry {
			i = numberofRetry;
			while(i != 0 && !sendFail)
			{
				sendFail = sendRPC(payload.target, networkMessage, (iden = (source = payload.source, seqnum = CurrentSeqNum),msg = (e = payload.e, p = payload.p)));
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

