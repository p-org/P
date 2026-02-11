

machine ClientServer {

    var server: BankServer;
    var accountId: string;
    var currentBalance: int;

    start state Init {
        entry (input : (serv : BankServer, accountId: string, balance : int)) {
            server = input.serv;
            accountId = input.accountId;
            currentBalance = input.balance;
            goto WithdrawMoney;
        }
    }

    state WithdrawMoney {
        entry {
            send server, eWithdrawReq, (source = this, accountId = accountId, amount = 100);
        }
    }
}

machine BankServer {
    start state Init {
        entry {

        }

        on eWithdrawReq do (wReq: tWithdrawReq) {
            
        }
    }
}

// ========= Everything below this should be generated correctly by the model ==============
type tWithdrawReq = (source: ClientServer, accountId: string, amount: int);
event eWithdrawReq : tWithdrawReq;

