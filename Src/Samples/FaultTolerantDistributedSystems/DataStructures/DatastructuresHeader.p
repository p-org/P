enum DSOperation {
    ADD,
    REMOVE,
    READ
}

type DSClientInterface() = { eDSOperationResp }
type DSOperationType = (source: DSClientInterface, opId: int, op: DSOperation, val: data);
type DSOperationRespType = (opId: int, val: data);

event eDSOperation : DSOperationType;
event eDSOperationResp : DSOperationRespType;