// event used to initialize the AtomicityInvariant spec monitor
// event eMonitor_AtomicityInitialize: int;

// We would like to assert the atomicity property that if a transaction is committed by the coordinator then it was agreed on by all participants
spec AtomicityInvariant observes eWriteTransFailed, eWriteTransSuccess, ePrepareFailed, ePrepareSuccess, eMonitor_AtomicityInitialize
{
    // a map from transaction id to a map from responses status to number of participants with that response
	var participantsResponse: map[int, map[bool, int]];
	var numParticipants: int;
	start state Init {
		on eMonitor_AtomicityInitialize goto WaitForEvents with (n: int) {
			numParticipants = n;
		}
	}

	state WaitForEvents {
          on ePrepareSuccess do (resp: int){
            if(!(resp in participantsResponse))
            {
                participantsResponse[resp] = default(map[bool, int]);
                participantsResponse[resp][true] = 0;
                participantsResponse[resp][false] = 0;
            }
            participantsResponse[resp][false] = participantsResponse[resp][true] + 1;
          }
	  on ePrepareFailed do (resp: int){
            if(!(resp in participantsResponse))
            {
                participantsResponse[resp] = default(map[bool, int]);
                participantsResponse[resp][true] = 0;
                participantsResponse[resp][false] = 0;
            }
            participantsResponse[resp][false] = participantsResponse[resp][false] + 1;
	  }
	  on eWriteTransSuccess do {
         }
	  on eWriteTransFailed do {
          }
	}
}

/**************************************************************************
Every received transaction from a client must be eventually responded back.
Note, the usage of hot and cold states.
***************************************************************************/
spec Progress observes eWriteTransaction, eWriteTransFailed, eWriteTransSuccess {
    var pendingTransactions: int;
	start state Init {
		on eWriteTransaction goto WaitForResponses with { pendingTransactions = pendingTransactions + 1; }
	}

	hot state WaitForResponses
	{
		on eWriteTransSuccess do {
		    pendingTransactions = pendingTransactions - 1;
		    if(pendingTransactions == 0)
		    {
		        goto AllTransactionsFinished;
		    }
        }
		on eWriteTransFailed do {
		    pendingTransactions = pendingTransactions - 1;
		    if(pendingTransactions == 0)
		    {
		        goto AllTransactionsFinished;
		    }
        }
        on eWriteTransaction do { pendingTransactions = pendingTransactions + 1; }
	}

	cold state AllTransactionsFinished {
	    on eWriteTransaction goto WaitForResponses with { pendingTransactions = pendingTransactions + 1; }
	}
}
