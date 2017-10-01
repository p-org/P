/*
ListMachine:
This state machine implements the list data-structure.
In the case of fault-tolerant list data-structure, the ListMachine is replicated using SMR protocol
*/

machine ListMachine
sends eSMRResponse;
{
    var localStore: seq[data];
    var lastRecvOperation: int;
    var client : SMRClientInterface;
    var isLeader : bool;
    var currClientOpId : int;
    var currRespId: int;
    start state Init {
        entry (payload: (client:SMRClientInterface, val: data)){
            isLeader = false;
            raise local;
        }
        //install common handler
        on eSMRReplicatedMachineOperation do (payload:SMRRepMachOperationType){
            currRespId = payload.respId;
            currClientOpId = payload.smrop.clientOpId;
            client = payload.smrop.source;
            raise payload.smrop.operation, payload.smrop.val;
        }

        on eSMRReplicatedLeader do {
			isLeader = true;
		}

        on local push WaitForOperationReq;
    }
    state WaitForOperationReq {
        
        on eDSOperation do (payload: DSOperationType) {
            if(currClientOpId <= lastRecvOperation)
            {
                return;
            }
            else
            {
                if(payload.op == ADD)
                {
                    localStore += (sizeof(localStore), payload.val as data);
                    SendSMRResponse(client, eDSOperationResp, (val = true, ), currClientOpId, currRespId, isLeader);
                }
                else if(payload.op == REMOVE)
                {
                    if((payload.val as int) < sizeof(localStore))
                    {
                        localStore -= (payload.val as int);
                        SendSMRResponse(client, eDSOperationResp, (val = true, ), currClientOpId, currRespId, isLeader);
                    }
                    else
                    {
                        SendSMRResponse(client, eDSOperationResp, (val = false, ), currClientOpId, currRespId, isLeader);
                    }
                    
                }
                else if(payload.op == READ)
                {
                    if((payload.val as int) < sizeof(localStore)) {
                        SendSMRResponse(client, eDSOperationResp, (val = localStore[payload.val as int], ), currClientOpId, currRespId, isLeader);
                    }
                    else
                    {
                        SendSMRResponse(client, eDSOperationResp, (val = false, ), currClientOpId, currRespId, isLeader);
                    }
                    
                }
                lastRecvOperation = currClientOpId;
            }
        
        }
    }
}