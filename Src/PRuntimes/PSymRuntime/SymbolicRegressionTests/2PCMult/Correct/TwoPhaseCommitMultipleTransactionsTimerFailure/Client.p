/*
A client machine pumping in one random transaction
*/

machine Client {
    var coordinator: machine;
    var count: int;
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
		on eReadTransSuccess do (res:int) {
                    assert res == randomTransaction.val, "Read wrong value out!!";
                    if (count == 1) {
                        goto End;
                    } else {
                        count = count + 1;
                        goto StartPumpingTransactions;
                    }
                }
	}

	state End { }

	
}


/* function to randomly choose index and values */
fun ChooseTransaction(src : machine): tWriteTransaction {
    return (client=src, key=0, val=0);
}
