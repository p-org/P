event sendRelMessage:(source:id, target:id, e:eid, p:any);
event sendMessage:(source:id, target:id, e:eid, p:any);
event networkMessage:(iden:(source:id, seqnum:int), msg:(e:eid, p:any));
event hostM:id;
event StartE:any;

machine ReceiverMachine {
	var hostMachine:id;
	var lastReceivedMessage: map[id, int];
	var initMessage:(nodemanager:id, param:any, host:id);
	start state bootingState {
        defer networkMessage;
		entry {
			
		}
        on hostM goto InitHost;
	}
	
    state InitHost {
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
	var numberofRetry: int; 
	var sendFail:bool;
	var i: int;
	var CurrentSeqNum:int;
	start state bootingState {
		entry {
			numberofRetry = 1;//((nodemanager:id, param:int))payload.param;
			sendFail = false;
			CurrentSeqNum = 0;
		}
		on sendMessage goto Listening;
        on sendRelMessage goto Listening;
	}
	
	state Listening {
		entry {
			
			if(trigger == sendRelMessage)
            {
                send(payload.target, networkMessage, (iden = (source = payload.source, seqnum = CurrentSeqNum),msg = (e = payload.e, p = payload.p)));
            }
            else
            {
                i = numberofRetry;
			    while(i != 0 && !sendFail)
			    {
				    sendFail = sendRPC(payload.target, networkMessage, (iden = (source = payload.source, seqnum = CurrentSeqNum),msg = (e = payload.e, p = payload.p)));
				    i = i - 1;
			    }
                
            }
		}
		on sendRelMessage goto Listening;
		on sendMessage goto Listening;
		exit {
			sendFail = false;
			CurrentSeqNum = CurrentSeqNum + 1;
		}
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

