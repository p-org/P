machine ClientMachine : ClientInterface
receives eRespPartStatus, eTransactionFailed, eTransactionSuccess, eTimeOut, eCancelSuccess, eCancelFailure;
sends eTransaction, eReadPartStatus, eStartTimer, eCancelTimer;
{
    var coor: CoorClientInterface;
    var numOfOperation : int;
    var timer : TimerPtr;
    start state Init {
        entry (payload: int){
            /*coor = (payload as (COOR_MACHINE_PUBLIC_IN, int, int)).0;
            numOfOperation = (payload as (COOR_MACHINE_PUBLIC_IN, int, int)).1;
            value = (payload as (COOR_MACHINE_PUBLIC_IN, int, int)).2;
            timer = CreateTimer(this as ITimerClient);*/
            //goto StartPumpingTransactions;
        }
    }
/*
    state StartPumping {
        entry {
            if(numOfOperation == 0)
                raise done;
            send coor, eTransaction, (source = this as ClientInterface, val1 = value, val2 = value));
            send timer, STARTTIMER;
            
            value = value + 1;
            numOfOperation = numOfOperation - 1;
        }
        on TRANSACTION_FAIL do CancelTimer;
        on TRANSACTION_SUCCESS goto StartPumping with {
            success += (0, payload.tid);
        }
        on TIMEOUT goto StartPumping;
        on local goto StartPumping;
        on done goto Block with { i  = 0;}
    }
    
    var i : int;
    state ReadTransaction {
        ignore TIMEOUT, TRANSACTION_FAIL, TRANSACTION_SUCCESS;
        entry {
                if(i >= sizeof(success))
                    raise done;
                SEND(coor, READ_TRANSACTION, (source = this as CLIENT_MACHINE_PUBLIC_IN, tid = success[i]));
                i = i + 1;
        }
        on null goto ReadTransaction;
        on done goto Block;
        on TRANSACTION_VALUE do {
            assert(payload.val == success[payload.tid]);
        }
    }
    
    state Block{
        entry {
            Print();
        }
        ignore TIMEOUT, TRANSACTION_FAIL, TRANSACTION_SUCCESS;
        on TRANSACTION_VALUE do {
            assert(payload.val == success[payload.tid]);
        }
    }
*/
}
