machine ClientMachine
{
    var coor: machine;
    var numOfOperation : int;
    var timer : TimerPtr;
    var valueAtParticipant: map[int, int];
    start state Init {
        entry (payload: (machine, int)){
            coor = payload.0;
            numOfOperation = payload.1;
            timer = CreateTimer(this);
            valueAtParticipant[0] = 0;
            valueAtParticipant[1] = 0;
            goto StartPumpingTransactions;
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
        if(oper1.op == ADD_AMOUNT)
            valueAtParticipant[0] = valueAtParticipant[0] + oper1.val;
        else
            valueAtParticipant[0] = valueAtParticipant[0] - oper1.val;

        if(oper2.op == ADD_AMOUNT)
            valueAtParticipant[1] = valueAtParticipant[1] + oper2.val;
        else
            valueAtParticipant[1] = valueAtParticipant[1] - oper2.val;
    }
    
    var oper1 : OperationType;
    var oper2 : OperationType;
    state StartPumpingTransactions {
        entry {
            var x : machine;
            /*if(numOfOperation == 0)
                return;*/
            
            oper1 = ChooseOp();
            oper2 = ChooseOp();
            
            x =  this as machine;
            send coor, eTransaction, (source = x, op1 = oper1, op2 = oper2);
            StartTimer(timer, 100);
            numOfOperation = numOfOperation - 1;
        }
        on eTransactionFailed do { CancelTimer(timer); goto StartPumpingTransactions; }
        on eTransactionSuccess goto StartPumpingTransactions with UpdateValues;
        on eTimeOut goto StartPumpingTransactions;
    }
    
    state ReadStatusOfParticipant {
        ignore eTimeOut, eTransactionFailed;
        entry {
                var p: int;
                if($) p = 0; else p = 1;
                send coor, eReadPartStatus, (source = this, part = p);
        }
        on eTransactionSuccess do UpdateValues;
        on eRespPartStatus goto StartPumpingTransactions with (payload: ParticipantStatusType){
            assert(payload.val == valueAtParticipant[payload.part]);
        }
    }
}

