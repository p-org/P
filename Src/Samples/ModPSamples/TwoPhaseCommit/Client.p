machine ClientMachine : ClientInterface
receives eRespPartStatus, eTransactionFailed, eTransactionSuccess, eTimeOut, eCancelSuccess, eCancelFailure;
sends eTransaction, eReadPartStatus, eStartTimer, eCancelTimer;
{
    var coor: CoorClientInterface;
    var numOfOperation : int;
    var timer : TimerPtr;
    var valueAtParticipant: map[int, int];
    start state Init {
        entry (payload: (CoorClientInterface, int)){
            coor = payload.0;
            numOfOperation = payload.1;
            timer = CreateTimer(this as ITimerClient);
            valueAtParticipant[0] = 10;
            valueAtParticipant[1] = 101;
            //goto StartPumpingTransactions;
        }
    }
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
        if(op1.op == ADD_AMOUNT)
            valueAtParticipant[0] = valueAtParticipant[0] + op1.val;
        else
            valueAtParticipant[0] = valueAtParticipant[0] - op1.val;

        if(op2.op == ADD_AMOUNT)
            valueAtParticipant[1] = valueAtParticipant[1] + op2.val;
        else
            valueAtParticipant[1] = valueAtParticipant[1] - op2.val;
    }
    
    var op1 : OperationType;
    var op2 : OperationType;
    state StartPumpingTransactions {
        entry {
            if(numOfOperation == 0)
                return;
            
            op1 = ChooseOp();
            op2 = ChooseOp();
            send coor, eTransaction, (source = this as ClientInterface, op1 = op1, op2 = op2);
            StartTimer(timer, 100);
            numOfOperation = numOfOperation - 1;
            if($)
            {
                //goto ReadStatusOfParticipant;
            }
        }
        on eTransactionFailed goto StartPumpingTransactions with { CancelTimer(timer); }
        on eTransactionSuccess goto StartPumpingTransactions with UpdateValues;
        on eTimeOut goto StartPumpingTransactions;
    }
    /*
    state ReadStatusOfParticipant {
        ignore eTimeOut, eTransactionFailed;
        entry {
                var p: int;
                if($) p = 0; else p = 1;
                send coor, eReadPartStatus, (source = this as ClientInterface, part = p);
        }
        on eTransactionSuccess do UpdateValues;
        on eRespPartStatus goto StartPumpingTransactions with (payload: ParticipantStatusType){
            assert(payload.val == valueAtParticipant[payload.part]);
        }
    }*/
}

