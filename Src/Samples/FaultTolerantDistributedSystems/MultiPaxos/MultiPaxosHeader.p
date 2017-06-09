event Ping  assume 4 : (rank:int, server : machine);
event newLeader : (rank:int, server : machine);

/*
The multi-paxos events. 
*/
event prepare assume 3: (proposer: machine, slot : int, proposal : (round: int, servermachine : int)) ;
event accept  assume 3: (proposer: machine, slot: int, proposal : (round: int, servermachine : int), value : int);
event agree assume 6: (slot:int, proposal : (round: int, servermachine : int), value : int) ;
event reject  assume 6: (slot: int, proposal : (round: int, servermachine : int));
event accepted  assume 6: (slot:int, proposal : (round: int, servermachine : int), value : int);
event success;
event goPropose;
event chosen : (slot:int, proposal : (round: int, servermachine : int), value : int);
/**** client events ********/
event update : (seqmachine: int, command : int);