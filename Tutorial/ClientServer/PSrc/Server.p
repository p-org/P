// Events used to communicate between the server and helper
event eUpdateQuery: (accountId: int, balance: int);
event eReadQuery: (accountId: int);
event eReadQueryResp: (accountId: int, balance: int);

/*************************************************************
Server receives eRequest event from the client and performs local computation.
Based on the local computation using a helper, server responds with either eReqSuccessful or eReqFailed.
Server responds to requests in the order in which they were received.
*************************************************************/
machine BankServer
{
  var database: Database;
  start state Init {
    entry (initialBalance: map[int, int]){
      database = new Database((server = this, initialBalance = initialBalance));
      goto WaitForWithdrawRequests;
    }
  }

  state WaitForWithdrawRequests {
    on eWithDrawReq do (wReq: tWithDrawReq) {
      send database, eReadQuery, (accountId = wReq.accountId,);
      receive {
        case eReadQueryResp: (resp: (accountId: int, balance: int)) {
            if(resp.balance - wReq.amount > 10)
            {
                send database, eUpdateQuery, (accountId = wReq.accountId, balance = resp.balance - wReq.amount);
                send wReq.source, eWithDrawResp, (status = WITHDRAW_SUCCESS, accountId = wReq.accountId, balance = resp.balance - wReq.amount, rId = wReq.rId);
            }
            else
            {
                send wReq.source, eWithDrawResp, (status = WITHDRAW_ERROR, accountId = wReq.accountId, balance = resp.balance, rId = wReq.rId);
            }
        }
      }
    }
  }
}

/***************************************************************
The helper machine performs some random computation and returns
either eReqSuccessful or eReqFailed.
****************************************************************/
machine Database
{
  var server: BankServer;
  var balance: map[int, int];
  start state Init {
    entry(input: (server : BankServer, initialBalance: map[int, int])){
      server = input.server;
      balance = input.initialBalance;
    }
    on eUpdateQuery do (query: (accountId: int, balance: int)) {
        assert query.accountId in balance, "Invalid accountId received in the update query!";
        balance[query.accountId] = query.balance;
    }
    on eReadQuery do (query: (accountId: int))
    {
        assert query.accountId in balance, "Invalid accountId received in the read query!";
        send server, eReadQueryResp, (accountId = query.accountId, balance = balance[query.accountId]);
    }
  }
}
