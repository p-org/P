/*
HashSetMachine:
This state machine implements the hashset data-structure.
In the case of fault-tolerant list data-structure, the HashSetMachine is replicated using SMR protocol
*/

machine HashSetMachine: SMRReplicatedMachineInterface
sends eSMRResponse;
{
    var localStore: map[data, bool];
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
                    localStore[payload.val as data] = true;
                    SendSMRResponse(client, eDSOperationResp, (opId = payload.opId, val = true), isLeader);
                }
                else if(payload.op == REMOVE)
                {
                    if((payload.val as data) in localStore)
                    {
                        localStore -= (payload.val as data);
                        SendSMRResponse(client, eDSOperationResp, (opId = payload.opId, val = true), isLeader);
                    }
                    else
                    {
                        SendSMRResponse(client, eDSOperationResp, (opId = payload.opId, val = false), isLeader);
                    }
                }
                else if(payload.op == READ)
                {
                    if((payload.val as data) in localStore)
                    {
                        SendSMRResponse(client, eDSOperationResp, (opId = payload.opId, val = true), isLeader);
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