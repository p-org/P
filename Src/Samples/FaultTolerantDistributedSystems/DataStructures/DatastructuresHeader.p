enum DSOperation {
    ADD,
    REMOVE,
    READ
}

type DSOperationType = (opId: int, op: DSOperation, val: data);
type DSOperationRespType = (opId: int, val: data);

event eDSOperation : DSOperationType;
event eDSOperationResp : DSOperationRespType;

event local;