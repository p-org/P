/***************************************************
User defined types
***************************************************/

type tWithDrawReq = (source: Client, accountId: int, amount: int, rId:int);

type tWithDrawResp = (status: tWithDrawRespStatus, accountId: int, balance: int, rId: int);

enum tWithDrawRespStatus {
    WITHDRAW_SUCCESS,
    WITHDRAW_ERROR
}

event eWithDrawReq : tWithDrawReq;
event eWithDrawResp: tWithDrawResp;

machine Client
{
  var server : BankServer;
  var accountId: int;
  var nextReqId : int;
  var numOfWithdrawOps: int;
  var currentBalance: int;
  start state Init {

    entry (input : (serv : BankServer, accountId: int, balance : int))
    {
      server = input.serv;
      numOfWithdrawOps = 3;
      currentBalance =  input.balance;
      accountId = input.accountId;
      goto StartPumpingRequests;
    }
  }

  state StartPumpingRequests {
    entry {
      var index : int;
      while(index < numOfWithdrawOps)
      {
          send server, eWithDrawReq, (source = this, accountId = accountId, amount = WithdrawAmount(), rId = nextReqId);
          // request ids should be monotonically increasing
          nextReqId = nextReqId + 1;
          index = index + 1;
      }
    }

    on eWithDrawResp do (resp: tWithDrawResp){
        assert resp.balance > 10, "Bank balance must be greater than 10!!";

        if(resp.status == WITHDRAW_SUCCESS)
        {
            print format ("Withdrawal with rId = {0} succeeded, new account balance = {1}", resp.rId, resp.balance);
            currentBalance = resp.balance;
        }
        else
        {
            // if withdraw failed then the account balance must remain the same
            assert currentBalance == resp.balance,
                format ("Withdraw failed BUT the account balance changed! client thinks: {0}, bank balance: {1}", currentBalance, resp.balance);
            print format ("Withdrawal with rId = {0} failed, account balance = {1}", resp.rId, resp.balance);

        }

        if(currentBalance > 10)
        {
            print format ("Still have account balance = {0}, lets try and withdraw more", currentBalance);
        }
    }
  }

  fun WithdrawAmount() : int {
    return choose(currentBalance) + 1;
  }
}