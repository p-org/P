//Types
type SMROperationType = (source: SMRClientInterface, operation: event, val: data);
type SMRResponseType = (response: event, val: data);

//Events used to interact with the State Machine Replication (SMR) Protocols
event eSMROperation : SMROperationType;
event eSMRResponse : SMRResponseType;
event eSMRReplicatedMachineOperation : SMROperationType;
event eSMRLeaderUpdated : (int, SMRServerInterface);

//Interfaces used by clients of State Machine Replication (SMR)
type SMRClientInterface()  = {eSMRResponse, eSMRLeaderUpdated};
type SMRReplicatedMachineInterface((client:SMRClientInterface, val: data)) =  { eSMRReplicatedMachineOperation };
type SMRServerInterface((client: SMRClientInterface, reorder: bool, val: data)) = { eSMROperation };


/********************
Helper Functions
********************/

fun SendSMRResponse(target: machine, ev: event, val: data)
{
    send target as SMRClientInterface, eSMRResponse, (response = ev, val = val);
}

fun SendSMROperation(source: machine, target: machine, ev: event, val: data)
{
    send target as SMRServerInterface, eSMROperation, (source = source as SMRClientInterface, operation = ev, val = val);
}

fun SendSMRRepMachineOperation(target: machine, operation: SMROperationType) 
{
    send target as SMRReplicatedMachineInterface, eSMRReplicatedMachineOperation, operation;
}

fun SendSMRServerUpdate(target: machine, val: (int, machine))
{
    send target, eSMRLeaderUpdated, val;
}

