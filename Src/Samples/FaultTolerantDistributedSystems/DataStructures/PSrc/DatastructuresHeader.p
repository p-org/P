enum DSOperation {
    ADD,
    REMOVE,
    READ
}

type DSOperationType = (op: DSOperation, val: data);
type DSOperationRespType = (val: data);

event eDSOperation : DSOperationType;
event eDSOperationResp : DSOperationRespType;

event local;