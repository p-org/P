event eParticipantCommitted: (part:int, tid:int);
event eParticipantAborted: (part:int, tid:int);
event eMonitorTransaction;
event eMonitorTransactionFailed;
event eMonitorTransactionSuccess;
event eMonitorCoordinatorTimeOut;

/**********************************
* Consistency Spec:
The spec monitor asserts that if a participant commits or aborts a transaction then all other 
participants have either made the same decision or have not made any decision yet.
***********************************/

spec ConsistencySpec observes eParticipantCommitted, eParticipantAborted 
{
	//log from partitionId -> transactionid -> CommittedOrAborted.
	var partLog: map[int, map[int, bool]];

	start state Init {
		entry {
			//for two partitions
			var index : int;
			index = 0;
			while(index < NumOfParticipants)
			{
				partLog[index] = default(map[int, bool]);
				index = index + 1;
			}
		}
		on eParticipantCommitted do (payload : (part:int, tid:int)) {
			var partid : int;
			while(partid < NumOfParticipants)
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
			while(partid < NumOfParticipants)
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

/******************************************************
Progress Guarantee:
The progress spec asserts that in the presence of bounded time-outs.
For each transaction, the client always eventually receives
*******************************************************/
spec ProgressSpec observes eMonitorTransaction, eMonitorTransactionFailed, eMonitorTransactionSuccess, eMonitorCoordinatorTimeOut
{
	start state WaitForNewTransaction {
		ignore eMonitorTransactionFailed, eMonitorTransactionSuccess;
		on eMonitorTransaction goto WaitForTransactionCompletion;
	}
	
	cold state ResetTemperature {
		entry {
			goto WaitForTransactionCompletion;
		}
	}
	hot state WaitForTransactionCompletion {
		on eMonitorTransactionFailed, eMonitorTransactionSuccess goto WaitForNewTransaction;
		on eMonitorCoordinatorTimeOut goto ResetTemperature;
	}
}