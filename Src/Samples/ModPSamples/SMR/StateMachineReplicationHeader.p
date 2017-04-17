//Types
type SMROperationType = (source: SMRClientInterface, operation: event, val: any);
type SMRResponseType = (response: event, val: any);

//Events used to interact with the State Machine Replication (SMR) Protocols
event eSMROperation : SMROperationType;
event eSMRResponse : SMRResponseType;
event eSMRReplicatedMachineOperation : SMROperationType;
event eSMRLeaderUpdated : (int, SMRServerInterface);

//Interfaces used by clients of State Machine Replication (SMR)
type SMRClientInterface()  = {eSMRResponse, eSMRLeaderUpdated};
type SMRReplicatedMachineInterface((machine, int, bool)) =  { eSMRReplicatedMachineOperation };
type SMRServerInterface((client: SMRClientInterface, reorder: bool, id: int)) = { eSMROperation };


