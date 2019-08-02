/* 
A client machine pumping in one random transaction
*/

machine Client {
    var coordinator: machine;
    start state Init {
	    entry (payload : machine) {
	        coordinator = payload;
			goto StartPumpingTransactions;
		}
	}

	var randomTransaction : tWriteTransaction;
	state StartPumpingTransactions {
	    entry {
	    	randomTransaction = ChooseTransaction();
			send coordinator, eWriteTransaction, randomTransaction;
		}
		on eWriteTransFailed goto End;
		on eWriteTransSuccess goto ConfirmTransaction;
	}

	state ConfirmTransaction {
	    entry {
			send coordinator, eReadTransaction, (client=this, key = randomTransaction.key);
		}
		on eReadTransFailed do { assert false, "Read Failed after Write!!"; }
		on eReadTransSuccess goto End;
	}

	state End { }

	
}


/* external functions to randomly choose index and values */
fun ChooseTransaction(): tWriteTransaction;

