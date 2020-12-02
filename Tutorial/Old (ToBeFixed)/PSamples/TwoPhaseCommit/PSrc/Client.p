/* 
A client machine pumping in one random transaction 
and then asserting that if the transaction succeeded then the read should also succeed.
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
		on eReadTransSuccess goto End with (payload: int ){ assert payload == randomTransaction.val, "Incorrect value returned !!"; }
	}

	state End {
		entry {
			raise halt;
		}
	}

	
}


/* 
This is an external functions (implemented in C# or C) to randomly choose transaction values
In P funtion declarations without body are considered as foreign functions.
*/
fun ChooseTransaction(): tWriteTransaction;

