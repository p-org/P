type tRecord = (key: tPreds, val: tPreds);
type writeRequest = (client: machine, rec: tRecord);
event write: writeRequest;
event writeResp;
type readRequest = (client: machine, key: tPreds);
event read: readRequest;
event readResp: tRecord;

enum op { READ, WRITE }


type ProposalIdType = (serverid: int, round: int);
type ProposalType = (ty: op, pid: ProposalIdType, value: tRecord);


// Events related to Paxos
event prepare assume 3: (proposer: machine, proposal: ProposalType);
event accept assume 3: (proposer: machine, proposal: ProposalType);
event agree assume 3: ProposalType;
event reject assume 3: ProposalIdType;
event accepted assume 3: ProposalType;

pred enum tPreds {
    EQKEY,
    EQVAL,
    NEQKEY,
    NEQVAL,
    DEFAULT
}


//Global constants
//enum GlobalContants {
//  GC_NumOfAccptNodes = 3,
//  GC_NumOfProposerNodes = 2,
//  GC_Default_Value = 0
//}

