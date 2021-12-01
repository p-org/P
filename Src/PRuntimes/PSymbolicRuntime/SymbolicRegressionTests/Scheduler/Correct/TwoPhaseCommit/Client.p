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
	    	randomTransaction = ChooseTransaction(this);
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


/* function to randomly choose index and values */
fun ChooseTransaction(src : machine): tWriteTransaction {
    return (client=src, key=0, val=0);
}
