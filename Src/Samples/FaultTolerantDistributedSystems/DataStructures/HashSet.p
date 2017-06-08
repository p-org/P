/*
HashSetMachine:
This state machine implements the hashset data-structure.
In the case of fault-tolerant list data-structure, the HashSetMachine is replicated using SMR protocol
*/

machine HashSetMachine: SMRReplicatedMachineInterface, DataStructureInterface
{
    var localStore: map[data, bool];
    var lastRecvOperation: DSOperationType;
    start state Init {
        entry {

        }
        on eDSOperation do (payload: DSOperationType) {
            if(payload.opId <= lastRecvOperation)
            {
                return;
            }
            else
            {
                if(payload.op == ADD)
                {
                    localStore[payload.val as data] = true;
                    send payload.source, eDSOperationResp, (opId = payload.opId, val = true);
                }
                else if(payload.op == REMOVE)
                {
                    assert((payload.val as data) in localStore);
                    localStore -= (payload.val as data);
                    send payload.source, eDSOperationResp, (opId = payload.opId, val = true);
                }
                else if(payload.op == READ)
                {
                    var value = (payload.val as data)
                    if(value in localStore)
                    {
                        send payload.source, eDSOperationResp, (opId = payload.opId, val = true);
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