/*
ListMachine:
This state machine implements the list data-structure.
In the case of fault-tolerant list data-structure, the ListMachine is replicated using SMR protocol
*/

machine ListMachine: SMRReplicatedMachineInterface
{
    var localStore: seq[data];
    var lastRecvOperation: DSOperationType;
    var client : SMRClientInterface;
    start state Init {
        entry {
            raise local;
        }
        //install common handler
        on eSMRReplicatedMachineOperation do (payload:SMROperationType){
            client = payload.source;
            raise payload.operation, payload.val;
        }

        on local push WaitForOperationReq;
    }
    state WaitForOperationReq {
        
        on eDSOperation do (payload: DSOperationType) {
            if(payload.opId <= lastRecvOperation)
            {
                return;
            }
            else
            {
                if(payload.op == ADD)
                {
                    localStore += (sizeof(localStore), payload.val as data);
                    send payload.source, eDSOperationResp, (opId = payload.opId, val = true);
                }
                else if(payload.op == REMOVE)
                {
                    if((payload.val as int) < sizeof(localStore))
                    {
                        localStore -= (payload.val as int);
                        send payload.source, eDSOperationResp, (opId = payload.opId, val = true);
                    }
                    else
                    {
                        send payload.source, eDSOperationResp, (opId = payload.opId, val = false);
                    }
                    
                }
                else if(payload.op == READ)
                {
                    if((payload.val as int) < sizeof(payload)) {
                        send payload.source, eDSOperationResp, (opId = payload.opId, val = localStore[value]);
                    }
                    else
                    {
                        send payload.source, eDSOperationResp, (opId = payload.opId, val = false);
                    }
                    
                }
                lastRecvOperation = payload;
            }
        
        }
    }
}