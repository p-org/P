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
		    if(payload.participant in receivedLocalCommits)
			    assert (receivedLocalCommits[payload.participant] != payload.transId), "Multiple local commits received from the same participant";

			receivedLocalCommits[payload.participant] = payload.transId;
		}
		on eWriteTransSuccess do {
			assert(sizeof(receivedLocalCommits) == numParticipants);
			// reset the map with default value which is empty map.
			receivedLocalCommits = default(map[machine, int]);
		}
		on eWriteTransFailed do {
		    // reset the map with default value which is empty map.
		    receivedLocalCommits = default(map[machine, int]);
        }
	}
}

/* 
Every received transaction from a client must be eventually responded back.
*/
spec Progress observes eWriteTransaction, eWriteTransSuccess, eWriteTransFailed {
    var pendingTransactions: int;
	start state Init {
		ignore eWriteTransFailed, eWriteTransSuccess;
		on eWriteTransaction goto WaitForOperationToFinish with { pendingTransactions = pendingTransactions + 1; }
	}

	hot state WaitForOperationToFinish 
	{
		on eWriteTransSuccess, eWriteTransFailed do {
		    pendingTransactions = pendingTransactions - 1;
		    if(pendingTransactions == 0)
		    {
		        goto AllTransactionsFinished;
		    }
        }
        on eWriteTransaction do { pendingTransactions = pendingTransactions + 1; }
	}

	cold state AllTransactionsFinished {
	    on eWriteTransaction goto WaitForOperationToFinish with { pendingTransactions = pendingTransactions + 1; }
	}
}
