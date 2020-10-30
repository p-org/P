// We would like to assert the atomicity property that if a transaction is committed by the coordinator then it was agreed on by all participants
spec Atomicity observes eWriteTransSuccess, eWriteTransFailed, eMonitor_LocalCommit, eMonitor_AtomicityInitialize
{
	var receivedLocalCommits: map[machine, int];
	var numParticipants: int;
	start state Init {
		on eMonitor_AtomicityInitialize goto WaitForEvents with (n: int) {

			numParticipants = n;
		}
	}

	state WaitForEvents {
		on eMonitor_LocalCommit do (payload: (participant:machine, transId: int)){
			assert(!(payload.participant in receivedLocalCommits));
			receivedLocalCommits[payload.participant] = payload.transId;
		}
		on eWriteTransSuccess do {
			assert(sizeof(receivedLocalCommits) == numParticipants);
			// reset the map with default value which is empty map.
			receivedLocalCommits = default(map[machine, int]);
		}
		on eWriteTransFailed do { receivedLocalCommits = default(map[machine, int]); }
	}
}

/* 
Every received transaction from a client must be eventually responded back.
TODO: This spec is not complete.
*/
spec Progress observes eWriteTransaction, eWriteTransSuccess, eWriteTransFailed {
	start state Init {
		ignore eWriteTransFailed, eWriteTransSuccess;
		on eWriteTransaction goto WaitForOperationToFinish;
	}

	hot state WaitForOperationToFinish 
	{
		ignore eWriteTransaction;
		on eWriteTransSuccess, eWriteTransFailed goto Init;
	}


}
