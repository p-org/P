/* This file defines two P specification monitors */

event eSpec_BankIsNotAFraud_Init: map[int, int];

spec BankIsNotAFraud observes eWithDrawReq,  eWithDrawResp, eSpec_BankIsNotAFraud_Init {
  // keep track of the expected bank balance for each client
    var bankBalance: map[int, int];
    var pendingWithDraws: map[int, tWithDrawReq];

    start state Init {
        on eSpec_BankIsNotAFraud_Init goto WaitForWithDrawReqAndResp with (balance: map[int, int]){
            bankBalance = balance;
        }
    }

    state WaitForWithDrawReqAndResp {
        on eWithDrawReq do (req: tWithDrawReq) {
            assert req.accountId in bankBalance,
            format ("Unknown accountId {0} in the with draw request. Valid accountIds = {1}",
            req.accountId, keys(bankBalance));
            pendingWithDraws[req.rId] = req;
        }

        on eWithDrawResp do (resp: tWithDrawResp) {
            assert resp.accountId in bankBalance,
                format ("Unknown accountId {0} in the with draw response!", resp.accountId);
            assert resp.rId in pendingWithDraws,
                format ("Unknown rId {0} in the with draw response!", resp.rId);
            assert resp.balance > 10,
                "Bank balance in all accounts must always be greater than 10!!";

            if(resp.status == WITHDRAW_SUCCESS)
            {
                assert resp.balance == bankBalance[resp.accountId] - pendingWithDraws[resp.rId].amount,
                    format ("Bank balance for the account {0} is {1} and not the expected value {2}, Bank is lying!",
                        resp.accountId, resp.balance, bankBalance[resp.accountId]);
                bankBalance[resp.accountId] = resp.balance;
            }
            else
            {
                // bank can only reject a request if it drops the balance below 10
                assert bankBalance[resp.accountId] - pendingWithDraws[resp.rId].amount <= 10,
                    format ("Bank must accept the with draw request for {0}, bank balance is {1}!", pendingWithDraws[resp.rId].amount, bankBalance[resp.accountId]);
                // if withdraw failed then the account balance must remain the same
                assert bankBalance[resp.accountId] == resp.balance,
                    format ("Withdraw failed BUT the account balance changed! actual: {0}, bank said: {1}", bankBalance[resp.accountId], resp.balance);
            }
        }
    }
}

/**************************************************************************
GuaranteedProgress observes the eRequest and eResponse events, it asserts that
every request is always responded by a successful response.
***************************************************************************/
spec GuaranteedProgress observes eWithDrawReq, eWithDrawResp {
    // keep track of the pending requests
    var pendingWDReqs: set[int];

    start state NopendingRequests {
        on eWithDrawReq goto PendingReqs with (req: tWithDrawReq){
            pendingWDReqs += (req.rId);
        }
    }

    hot state PendingReqs {
        on eWithDrawResp do (resp: tWithDrawResp) {
            assert resp.rId in pendingWDReqs, format ("unexpected rId: {0} received, expected one of {1}", resp.rId, pendingWDReqs);
            pendingWDReqs -= (resp.rId);
            if(sizeof(pendingWDReqs) == 0) // requests already responded
                goto NopendingRequests;
        }

        on eWithDrawReq goto PendingReqs with (req: tWithDrawReq){
            pendingWDReqs += (req.rId);
        }
    }
}