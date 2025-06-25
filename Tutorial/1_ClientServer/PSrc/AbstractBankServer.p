/**
 * AbstractBankServer
 * =================
 * 
 * Simplified implementation of the BankServer that handles withdrawals
 * without relying on a separate Database machine.
 * 
 * Purpose:
 * -------
 * 1. Demonstrates component abstraction in P modeling
 * 2. Simplifies testing by removing database interactions
 * 3. Provides identical external interface to the full BankServer
 * 
 * Key Design Points:
 * ----------------
 * - Account balances are stored directly in this machine rather than in a separate Database
 * - All database communication is eliminated, simplifying the component
 * - The external API remains identical, allowing this to be a drop-in replacement for BankServer
 * - Enables faster testing of client behavior without sacrificing correctness verification
 * 
 * This abstraction technique is useful when:
 * - You want to focus testing on specific components without the complexity of related components
 * - The internal details of a component aren't relevant to the properties being verified
 * - You need to reduce the state space for more efficient verification
 */

machine AbstractBankServer
{
  // Storage for all account balances (accountId -> balance)
  var accountBalances: map[int, int];
  /**
   * Main processing state - Handles client withdrawal requests directly
   */
  start state WaitForWithdrawRequests {
    entry (initialBalances: map[int, int])
    {
      // Initialize all account balances
      accountBalances = initialBalances;
    }

    /**
     * Process withdrawal request directly without database interaction
     */
    on eWithDrawReq do (withdrawRequest: tWithDrawReq) {
      // Validate that account exists
      assert withdrawRequest.accountId in accountBalances, "Invalid accountId received in the withdraw request!";
      
      // Check if withdrawal would maintain minimum balance requirement
      // NOTE: This contains a subtle bug - the comparison should be >= 10, not > 10
      if(accountBalances[withdrawRequest.accountId] - withdrawRequest.amount > 10) /* hint: bug */
      {
        // Sufficient funds: update account balance
        accountBalances[withdrawRequest.accountId] = accountBalances[withdrawRequest.accountId] - withdrawRequest.amount;
        
        // Send success response
        send withdrawRequest.source, eWithDrawResp, (
          status = WITHDRAW_SUCCESS, 
          accountId = withdrawRequest.accountId, 
          balance = accountBalances[withdrawRequest.accountId], 
          rId = withdrawRequest.rId
        );
      }
      else
      {
        // Insufficient funds: send error response (balance remains unchanged)
        send withdrawRequest.source, eWithDrawResp, (
          status = WITHDRAW_ERROR, 
          accountId = withdrawRequest.accountId, 
          balance = accountBalances[withdrawRequest.accountId], 
          rId = withdrawRequest.rId
        );
      }
    }
  }
}
