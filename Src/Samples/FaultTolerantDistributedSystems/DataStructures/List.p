/*
ListMachine:
This state machine implements the list data-structure.
In the case of fault-tolerant list data-structure, the ListMachine is replicated using SMR protocol
*/

machine ListMachine: SMRReplicatedMachineInterface, DataStructureInterface
{
    var localStore: seq[data];
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
                    localStore += (sizeof(localStore), payload.val as data);
                    send payload.source, eDSOperationResp, (opId = payload.opId, val = true);
                }
                else if(payload.op == REMOVE)
                {
                    assert((payload.val as int) < sizeof(payload));
                    localStore -= (payload.val as int);
                    send payload.source, eDSOperationResp, (opId = payload.opId, val = true);
                }
                else if(payload.op == READ)
                {
                    var value = (payload.val as int)
                    assert(value < sizeof(payload));
                    send payload.source, eDSOperationResp, (opId = payload.opId, val = localStore[value]);
                }
                lastRecvOperation = payload;
            }
        }
    }
}