//Monitors

//We need two properties 
//1) Atomicity of transaction. If a transaction is either committed on all participants or aborted on all.



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
