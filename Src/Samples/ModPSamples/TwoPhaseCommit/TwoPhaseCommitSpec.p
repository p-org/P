event eParticipantCommitted: (part:int, tid:int);
event eParticipantAborted: (part:int, tid:int);

spec AtomicitySpec observes eParticipantCommitted, eParticipantAborted 
{
	//log from partitionId -> transactionid -> CommittedOrAborted.
	var partLog: map[int, map[int, bool]];

	start state Init {
		entry {
			//for two partitions
			partLog[0] = default(map[int, bool]);
			partLog[1] = default(map[int, bool]);
		}
		on eParticipantCommitted do (payload : (part:int, tid:int)) {
			var partid : int;
			while(partid < sizeof(partLog))
			{
				if(partid != payload.part)
				{
					if(payload.tid in partLog[partid])
					{
						assert(partLog[partid][payload.tid]);
					}
				}
				partid = partid + 1;
			}
			partLog[payload.part][payload.tid] = true;
		}
		on eParticipantAborted do (payload : (part:int, tid:int)) {
			var partid : int;
			while(partid < sizeof(partLog))
			{
				if(partid != payload.part)
				{
					if(payload.tid in partLog[partid])
					{
						assert(!partLog[partid][payload.tid]);
					}
				}
				partid = partid + 1;
			}
			partLog[payload.part][payload.tid] = false;
		}
	}
}
