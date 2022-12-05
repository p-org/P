type ProposalIdType = (serverid: int, round: int);
type ProposalType = (pid: ProposalIdType, value: int);


// Events related to Paxos
event prepare assume 3: (proposer: machine, proposal: ProposalType);
event accept assume 3: (proposer: machine, proposal: ProposalType);
event agree assume 3: ProposalType;
event reject assume 3: ProposalIdType;
event accepted assume 3: ProposalType;


//Global constants
//enum GlobalContants {
//  GC_NumOfAccptNodes = 3,
//  GC_NumOfProposerNodes = 2,
//  GC_Default_Value = 0
//}

