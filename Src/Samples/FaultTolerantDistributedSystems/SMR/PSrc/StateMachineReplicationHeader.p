//Types
type SMROperationType = (source: SMRClientInterface, operation: event, val: data);
type SMRResponseType = (response: event, val: data);

//Events used to interact with the State Machine Replication (SMR) Protocols
event eSMROperation : SMROperationType;
event eSMRResponse : SMRResponseType;
event eSMRReplicatedMachineOperation : SMROperationType;
event eSMRLeaderUpdated : (int, SMRServerInterface);

//Interfaces used by clients of State Machine Replication (SMR)
type SMRClientInterface(data)  = {eSMRResponse, eSMRLeaderUpdated};
type SMRReplicatedMachineInterface((client:SMRClientInterface, val: data)) =  { eSMRReplicatedMachineOperation };
type SMRServerInterface((client: SMRClientInterface, reorder: bool, val: data)) = { eSMROperation };


/********************
Helper Functions
********************/

fun SendSMRResponse(target: any, ev: event, val: data)
{
    send target as SMRClientInterface, eSMRResponse, (response = ev, val = val);
}

fun SendSMROperation(target: any, ev: event, val: data, src: machine)
{
    send target as SMRServerInterface, eSMROperation, (source = src as SMRClientInterface, operation = ev, val = val);
}

fun SendSMRRepMachineOperation(target: any, operation: SMROperationType) 
{
    send target as SMRReplicatedMachineInterface, eSMRReplicatedMachineOperation, operation;
}

fun SendSMRServerUpdate(target: any, val: (int, SMRServerInterface))
{
    send target as machine, eSMRLeaderUpdated, val;
}

