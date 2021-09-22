/*****************************************************
This file defines two P specification monitors

- BankBalanceIsAlwaysCorrect (safety property):
BankBalanceIsCorrect checks the global invariant that the account balance communicated
to the client by the bank is always correct and there is no error on the banks side where it lost
clients money!

- GuaranteedWithDrawProgress (liveness property):
GuaranteedWithDrawProgress checks the liveness (or progress) property that all withdraw requests
submitted by the client are eventually responded.

__ Note that BankBalanceIsCorrect also checks that if there is enough money in the account then the withdraw
request must not error. Hence, the two properties above together ensure that every withdraw request
if allowed will eventually succeed and the bank cannot block legal withdraws. __

*****************************************************/

// event used to initialize the monitor with initial account balances for all clients
event eSpec_BankBalanceIsCorrect_Init: map[int, int];

/****************************************************
BankBalanceIsAlwaysCorrect checks the global invariant that the account balance communicated
to the client by the bank is always correct and there is no error on the banks side where it lost
clients money.

For checking this property the spec machine observes the withdraw request and response events.
- On receiving the eWithDrawReq, it adds the request in the pending withdraws map so that on receiving a
response for this withdraw we can assert that the amount of money deducted from the account is same as
what was requested by the client.
- On receiving the eWithDrawResp, we look up the corresponding withdraw request and check that, the
new account balance is correct and if the withdraw failed it because the withdraw will make the account
balance go below 10 dollars which is against bank policies!
****************************************************/
spec BankBalanceIsAlwaysCorrect observes eWithDrawReq,  eWithDrawResp, eSpec_BankBalanceIsCorrect_Init {
	// keep track of the bank balance for each client, map from accountId to bank balance.
	var bankBalance: map[int, int];
	// keep track of the pending withdraw requests that have not been responded yet.
	// map from reqId -> withdraw request
	var pendingWithDraws: map[int, tWithDrawReq];

	start state Init {
		on eSpec_BankBalanceIsCorrect_Init goto WaitForWithDrawReqAndResp with (balance: map[int, int]){
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
			assert resp.balance >= 10,
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
				// bank can only reject a request if it will drop the balance below 10
				assert bankBalance[resp.accountId] - pendingWithDraws[resp.rId].amount <= 10,
					format ("Bank must accept the with draw request for {0}, bank balance is {1}!",
						pendingWithDraws[resp.rId].amount, bankBalance[resp.accountId]);
				// if withdraw failed then the account balance must remain the same
				assert bankBalance[resp.accountId] == resp.balance,
					format ("Withdraw failed BUT the account balance changed! actual: {0}, bank said: {1}",
						bankBalance[resp.accountId], resp.balance);
			}
		}
	}
}

/**************************************************************************
GuaranteedWithDrawProgress checks the liveness (or progress) property that all withdraw requests
submitted by the client are eventually responded.
***************************************************************************/
spec GuaranteedWithDrawProgress observes eWithDrawReq, eWithDrawResp {
	// keep track of the pending withdraw requests
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
			if(sizeof(pendingWDReqs) == 0) // all requests responded
				goto NopendingRequests;
		}

		on eWithDrawReq goto PendingReqs with (req: tWithDrawReq){
			pendingWDReqs += (req.rId);
		}
	}
}