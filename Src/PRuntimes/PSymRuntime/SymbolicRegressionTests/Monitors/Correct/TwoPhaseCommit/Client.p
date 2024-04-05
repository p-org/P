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
		on eReadTransSuccess do (res:int) {
                    assert res == randomTransaction.val, "Read wrong value out!!";
                    if (res == 0) {
                      print "res 0";
                    }
                    if (res == 1) {
                      print "res 1";
                    }
                    if (res == 2) {
                      print "res 2";
                    }
                    if (res == 3) {
                      print "res 3";
                    }
                    if (randomTransaction.val == 0) {
                      print "val 0";
                    }
                    if (randomTransaction.val == 1) {
                      print "val 1";
                    }
                    if (randomTransaction.val == 2) {
                      print "val 2";
                    }
                    if (randomTransaction.val == 3) {
                      print "val 3";
                    }
                    goto End;
                }
	}

	state End { }

	
}


/* function to randomly choose index and values */
fun ChooseTransaction(src : machine): tWriteTransaction {
    return (client=src, key=0, val=randomInt(getIntHolder()));
}

/* foreign functions */
fun getIntHolder() : IntHolder;
fun randomInt(max : IntHolder) : int;
