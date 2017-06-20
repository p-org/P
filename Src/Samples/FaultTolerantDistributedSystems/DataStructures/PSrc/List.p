/*
ListMachine:
This state machine implements the list data-structure.
In the case of fault-tolerant list data-structure, the ListMachine is replicated using SMR protocol
*/

machine ListMachine: SMRReplicatedMachineInterface
sends eSMRResponse;
{
    var localStore: seq[data];
    var lastRecvOperation: DSOperationType;
    var client : SMRClientInterface;
    var isLeader : bool;
    start state Init {
        entry {
            isLeader = false;
            raise local;
        }
        //install common handler
        on eSMRReplicatedMachineOperation do (payload:SMROperationType){
            client = payload.source;
            raise payload.operation, payload.val;
        }

        on eSMRReplicatedLeader do {
			isLeader = true;
		}

        on local push WaitForOperationReq;
    }
    state WaitForOperationReq {
        
        on eDSOperation do (payload: DSOperationType) {
            if(payload.opId <= lastRecvOperation.opId)
            {
                return;
            }
            else
            {
                if(payload.op == ADD)
                {
                    localStore += (sizeof(localStore), payload.val as data);
                    SendSMRResponse(client, eDSOperationResp, (opId = payload.opId, val = true), isLeader);
                }
                else if(payload.op == REMOVE)
                {
                    if((payload.val as int) < sizeof(localStore))
                    {
                        localStore -= (payload.val as int);
                        SendSMRResponse(client, eDSOperationResp, (opId = payload.opId, val = true), isLeader);
                    }
                    else
                    {
                        SendSMRResponse(client, eDSOperationResp, (opId = payload.opId, val = false), isLeader);
                    }
                    
                }
                else if(payload.op == READ)
                {
                    if((payload.val as int) < sizeof(localStore)) {
                        SendSMRResponse(client, eDSOperationResp, (opId = payload.opId, val = localStore[payload.val as int]), isLeader);
                    }
                    else
                    {
                        SendSMRResponse(client, eDSOperationResp, (opId = payload.opId, val = false), isLeader);
                    }
                    
                }
                lastRecvOperation = payload;
            }
        
        }
    }
}