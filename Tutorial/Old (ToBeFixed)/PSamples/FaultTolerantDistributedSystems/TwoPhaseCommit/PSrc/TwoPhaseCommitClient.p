/*****************************************************************
* Description: This file implements the client of the two phase commit protocol
* In this case study the client uses two phase commit protocol to implement bank transactions.
*****************************************************************/

/* Operation performed in a transaction */
enum BankOperations {
    ADD_AMOUNT,
    SUBS_AMOUNT
}

/* Perform operantion function invoked by the participant on receiving a transaction */
fun PerformParticipantOp(opt: OperationType, oldVal: data)
{
    var bankOp : BankOperations;
    var oldAmount : int;
    var opVal : int;
    if(oldVal == null)
    {
        oldVal = 0;
    }
    oldAmount = oldVal as int;
    opVal = opt.val as int;

    bankOp = opt.op as BankOperations;
    if(bankOp == ADD_AMOUNT)
    {
        oldVal = oldAmount + opVal;
    }
    else
    {
        oldVal = oldAmount - opVal;
    }
}


machine ClientMachine
receives eRespPartStatus, eTransactionFailed, eTransactionSuccess, eTimeOut, eCancelSuccess, eCancelFailure;
sends eTransaction, eMonitorTransaction, eReadPartStatus, eStartTimer, eCancelTimer;
{
    var coor: CoorClientInterface;
    var numOfOperation : int;
    var timer : TimerPtr;
    var valueAtParticipant: int;
    start state Init {
        entry (payload: (CoorClientInterface, int)){
            coor = payload.0;
            numOfOperation = payload.1;
            timer = CreateTimer(this to ITimerClient);
            //initially amount at participants is 100
            valueAtParticipant = 100;
            goto StartPumpingTransactions;
        }
    }

    /* Choose next operation */
    fun ChooseOp(): OperationType {
        var oper: OperationType;
        if($)
            oper.op = ADD_AMOUNT;
        else
            oper.op = SUBS_AMOUNT;
        if($)
            oper.val = 10;
        else
            oper.val = 5;

        return oper;
    }
    

    fun UpdateValues() {
        if((lastOperation.op as BankOperations) == ADD_AMOUNT)
            valueAtParticipant = valueAtParticipant + lastOperation.val as int;
        else
            valueAtParticipant = valueAtParticipant - lastOperation.val as int;
    }

    var lastOperation : OperationType;
    state StartPumpingTransactions {
        entry {
            var x : ClientInterface;
            if(numOfOperation == 0)
                raise halt;
            
            lastOperation = ChooseOp();
            
            x =  this to ClientInterface;
            announce eMonitorTransaction;
            send coor, eTransaction, (source = x, op = lastOperation);
            StartTimer(timer, 100);
            numOfOperation = numOfOperation - 1;
            if($)
            {
                goto ReadStatusOfParticipant;
            }
        }
        on eTransactionFailed do { CancelTimer(timer); goto StartPumpingTransactions; }
        on eTransactionSuccess goto StartPumpingTransactions with UpdateValues;
        on eTimeOut do { print "Client Timed Out !\n"; }
    }
    
    state ReadStatusOfParticipant {
        ignore eTimeOut, eTransactionFailed;
        entry {
                var p: int;
                if($) p = 0; else p = 1;
                send coor, eReadPartStatus, (source = this to ClientInterface, part = p);
        }
        on eTransactionSuccess do UpdateValues;
        on eRespPartStatus goto StartPumpingTransactions with (payload: ParticipantStatusType){
            //print payload.val; print valueAtParticipant;
            //assert(payload.val == valueAtParticipant);
        }
    }
}