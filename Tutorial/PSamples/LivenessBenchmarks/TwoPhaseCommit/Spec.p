//Monitors

spec ProgressGuarantee observes eTransaction, eTransactionSuccess, eTransactionFailed
{
	start state Init {
		entry {
			goto WaitForTrans;
		}
	}

	state WaitForTrans {
		on eTransaction goto WaitForResponse;
		ignore eTransactionFailed, eTransactionSuccess;
	}

	hot state WaitForResponse {
		ignore eTransaction;
		on eTransactionSuccess goto WaitTransSuccess;
		on eTransactionFailed goto WaitForTrans;
	}

	cold state WaitTransSuccess {
		entry {
			goto WaitForTrans;
		}
	}
}
