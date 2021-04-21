//Types
type SMROperationType = (source: SMRClientInterface, clientOpId: int, operation: event, val: data);
type SMRResponseType = (clientOpId: int, respId: int, response: event, val: data);

type SMRRepMachOperationType = (respId: int, smrop: SMROperationType);

//Events used to interact with the State Machine Replication (SMR) Protocols
event eSMROperation : SMROperationType;
event eSMRResponse : SMRResponseType;
event eSMRReplicatedMachineOperation : SMRRepMachOperationType;
event eSMRLeaderUpdated : (int, SMRServerInterface);
event eSMRReplicatedLeader;

//Interfaces used by clients of State Machine Replication (SMR)
interface SMRClientInterface(data) receives eSMRResponse, eSMRLeaderUpdated;
interface SMRReplicatedMachineInterface((client:SMRClientInterface, val: data)) receives eSMRReplicatedMachineOperation, eSMRReplicatedLeader;
interface SMRServerInterface(SMRServerConstrutorType) receives eSMROperation;

type SMRServerConstrutorType = (client: SMRClientInterface, reorder: bool, isRoot : bool, ft : FaultTolerance, val: data);

enum FaultTolerance {
    FT1,
    FT2
}

/********************
Helper Functions
********************/

fun SendSMRResponse(target: any, ev: event, val: data, cOpId: int, rId: int, isLeader: bool)
{
    if(isLeader)
        send target as SMRClientInterface, eSMRResponse, (clientOpId = cOpId, respId = rId, response = ev, val = val);
}

fun SendSMROperation(cOpId: int, target: any, ev: event, val: data, src: machine)
{
    send target as SMRServerInterface, eSMROperation, (source = src as SMRClientInterface, clientOpId = cOpId, operation = ev, val = val);
}

fun SendSMRRepMachineOperation(target: any, operation: SMROperationType, rId : int) 
{
    send target as SMRReplicatedMachineInterface, eSMRReplicatedMachineOperation, (respId = rId, smrop = operation);
}

fun SendSMRServerUpdate(target: any, val: (int, SMRServerInterface))
{
    send target as machine, eSMRLeaderUpdated, val;
}

