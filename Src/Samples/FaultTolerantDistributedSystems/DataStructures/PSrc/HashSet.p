/*
HashSetMachine:
This state machine implements the hashset data-structure.
In the case of fault-tolerant list data-structure, the HashSetMachine is replicated using SMR protocol
*/

machine HashSetMachine: SMRReplicatedMachineInterface
sends eSMRResponse;
{
    var localStore: map[data, bool];
    var lastRecvOperation: int;
    var client : SMRClientInterface;
    var isLeader : bool;
    var currClientOpId : int;
    var currRespId: int;
    start state Init {
        entry {
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
                    localStore[payload.val as data] = true;
                    SendSMRResponse(client, eDSOperationResp, (val = true, ), currClientOpId, currRespId, isLeader);
                }
                else if(payload.op == REMOVE)
                {
                    if((payload.val as data) in localStore)
                    {
                        localStore -= (payload.val as data);
                        SendSMRResponse(client, eDSOperationResp, (val = true, ), currClientOpId, currRespId, isLeader);
                    }
                    else
                    {
                        SendSMRResponse(client, eDSOperationResp, (val = false, ), currClientOpId, currRespId, isLeader);
                    }
                }
                else if(payload.op == READ)
                {
                    if((payload.val as data) in localStore)
                    {
                        SendSMRResponse(client, eDSOperationResp, (val = true, ), currClientOpId, currRespId, isLeader);
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