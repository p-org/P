event eParticipantCommitted: (part:int, tid:int);
event eParticipantAborted: (part:int, tid:int);

spec AtomicitySpec observes eParticipantCommitted, eParticipantAborted 
{
	var partLog: map[int, map[int, bool]];

	start state Init {
		
		on eParticipantCommitted do (payload : (part:int, tid:int)) {
			if(payload.part == 0){
				if(payload.tid in partLog[1])
				{
					assert(partLog[1][payload.tid]);
				}
			}
			else
			{
				if(payload.tid in partLog[0])
				{
					assert(partLog[0][payload.tid]);
				}
			}
			partLog[payload.part][payload.tid] = true;
		}
		on eParticipantAborted do (payload : (part:int, tid:int)) {
			if(payload.part == 0){
				if(payload.tid in partLog[1])
				{
					assert(!partLog[1][payload.tid]);
				}
			}
			else
			{
				if(payload.tid in partLog[0])
				{
					assert(!partLog[0][payload.tid]);
				}
			}
			partLog[payload.part][payload.tid] = false;
		}
	}
}
