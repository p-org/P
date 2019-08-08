// We would like to assert the atomicity property that if a transaction is committed by the coordinator then it was committed by all participants

spec Atomicity observes eWriteTransSuccess, eMonitor_LocalCommit, eMonitor_AtomicityInitialize
{
	var receivedLocalCommits: map[machine, int];
	var numParticipants: int;
	start state Init {
		on eMonitor_AtomicityInitialize goto WaitForEvents with (n: int) {
			numParticipants = n;
		}
	}

	state WaitForEvents {
		on eMonitor_LocalCommit do (payload: (parcipant:machine, transId: int)){
			assert(!(payload.parcipant in receivedLocalCommits));
			receivedLocalCommits[payload.parcipant] = payload.transId;
		}
		on eWriteTransSuccess do {
			assert(sizeof(receivedLocalCommits) == numParticipants);
			receivedLocalCommits = default(map[machine, int]);
		}
	}
}


spec Progress observes eWriteTransaction, eWriteTransSuccess, eWriteTransFailed {
	start state Init {
		on eWriteTransaction goto WaitForOperationToFinish;
	}

	hot state WaitForOperationToFinish {
		on eWriteTransSuccess, eWriteTransFailed goto Init;
	}
}
