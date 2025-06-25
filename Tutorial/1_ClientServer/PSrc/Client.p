/**
 * Type and Event Definitions for Client-Server Communication
 * ========================================================
 */

/**
 * WithdrawRequest type - Represents a request to withdraw money from an account
 * 
 * @field source    The client machine that initiated this request
 * @field accountId The account identifier to withdraw from
 * @field amount    The amount of money to withdraw
 * @field rId       A unique request identifier used to correlate responses
 */
type tWithDrawReq = (source: Client, accountId: int, amount: int, rId:int);

/**
 * WithdrawResponse type - Represents a response to a withdrawal request
 * 
 * @field status    The status of the withdrawal operation (success/error)
 * @field accountId The account identifier that was accessed
 * @field balance   The current balance after the attempted withdrawal
 * @field rId       The request identifier this response corresponds to
 */
type tWithDrawResp = (status: tWithDrawRespStatus, accountId: int, balance: int, rId: int);

/**
 * WithdrawResponseStatus enum - Possible outcomes of a withdrawal request
 */
enum tWithDrawRespStatus {
  WITHDRAW_SUCCESS,  // The withdrawal was successful
  WITHDRAW_ERROR     // The withdrawal failed (e.g., insufficient funds)
}

// Events for client-server communication
event eWithDrawReq : tWithDrawReq;    // Client to Server: Request to withdraw money
event eWithDrawResp: tWithDrawResp;   // Server to Client: Response to withdrawal request
/**
 * Client Machine 
 * ==============
 * Represents a bank customer that can request money withdrawals
 * from their account and track their current balance.
 */
machine Client
{
  var bankServer : BankServer;      // Bank server this client communicates with
  var accountId: int;               // Client's unique account identifier
  var nextRequestId : int;          // Tracking the next request ID for client requests
  var withdrawalCount: int;         // Number of withdrawal operations performed
  var currentBalance: int;          // Client's view of their current account balance

  /**
   * Initial state: Set up client with account details
   */
  start state Init {

    entry (input : (serv : BankServer, accountId: int, balance : int))
    {
      bankServer = input.serv;
      currentBalance =  input.balance;
      accountId = input.accountId;
      // Generate unique request IDs for this client based on account ID
      // This ensures each client has a unique sequence of request IDs
      nextRequestId = accountId*100 + 1; 
      goto WithdrawMoney;
    }
  }

  /**
   * WithdrawMoney state
   * 
   * In this state, the client attempts to withdraw money from their account
   * and processes the bank's response.
   */
  state WithdrawMoney {
    entry {
      // Check if balance is at or below minimum threshold
      if(currentBalance <= 10)
        goto NoMoneyToWithDraw;

      // Send request to bank with a randomly selected withdrawal amount
      send bankServer, eWithDrawReq, (source = this, accountId = accountId, amount = WithdrawAmount(), rId = nextRequestId);
      nextRequestId = nextRequestId + 1;
    }

    on eWithDrawResp do (resp: tWithDrawResp) {
      // Bank rule: All accounts must maintain minimum balance of 10
      assert resp.balance >= 10, "Error: Bank balance fell below minimum threshold of 10!";
      
      if(resp.status == WITHDRAW_SUCCESS) // withdraw succeeded
      {
        print format ("Withdrawal with rId = {0} succeeded, new account balance = {1}", resp.rId, resp.balance);
        currentBalance = resp.balance;
        withdrawalCount = withdrawalCount + 1;
      }
      else // withdraw failed
      {
        // Verify consistency: if withdrawal failed, balance should remain unchanged
        assert currentBalance == resp.balance,
          format ("Integrity error: Balance changed despite failed withdrawal! Client expected: {0}, Bank reports: {1}", currentBalance, resp.balance);
        print format ("Withdrawal with rId = {0} failed, account balance = {1}", resp.rId, resp.balance);
      }

      // Decide whether to continue withdrawing
      if (currentBalance > 10)
/* Hint 3: Reduce the number of times WithdrawAmount() is called by changing the above line to the following:
      if (currentBalance > 10 && nextRequestId < (accountId*100 + 5))
*/
      {
        print format ("Still have account balance = {0}, lets try and withdraw more", currentBalance);
        goto WithdrawMoney;
      }
    }
  }

  /**
   * Calculates a withdrawal amount for the next transaction
   * 
   * @returns A random amount between 1 and currentBalance+1
   */
  fun WithdrawAmount() : int {
    // Choose a random amount between 1 and currentBalance (inclusive)
    return choose(currentBalance) + 1;
/* Hint 2: Reduce the number of choices by changing the above line to the following:
    return ((choose(5) * currentBalance) / 4) + 1;
*/
  }

  /**
   * NoMoneyToWithDraw state
   * 
   * Terminal state when the client's balance has reached the minimum allowed
   * threshold and no further withdrawals are possible.
   */
  state NoMoneyToWithDraw {
    entry {
      // Verify we've reached the exact minimum balance threshold
      assert currentBalance == 10, "Invariant violation: Reached NoMoneyToWithDraw state but balance is not at minimum threshold!";
      print format ("No Money to withdraw, waiting for more deposits!");
    }
  }
}
