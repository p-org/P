/**
 * Database Communication Events
 * ============================
 * These events facilitate communication between the bank server and database
 */

/**
 * Update query event - Sent to update an account's balance in the database
 *
 * @field accountId The account identifier to update
 * @field balance   The new balance value to store
 */
event eUpdateQuery: (accountId: int, balance: int);

/**
 * Read query event - Sent to request an account's current balance
 *
 * @field accountId The account identifier to read
 */
event eReadQuery: (accountId: int);

/**
 * Read query response event - Database response with the requested balance
 *
 * @field accountId The account identifier that was queried
 * @field balance   The current balance value stored for this account
 */
event eReadQueryResp: (accountId: int, balance: int);

/**
 * BankServer Machine
 * =================
 * 
 * The BankServer processes client withdrawal requests by:
 * 1. Receiving withdrawal requests from clients
 * 2. Querying the database for the current account balance
 * 3. Validating if sufficient funds exist for withdrawal
 * 4. Updating the database with new balance if withdrawal succeeds
 * 5. Sending appropriate response back to the client
 * 
 * The BankServer maintains a policy that all accounts must maintain
 * a minimum balance of 10.
 */
machine BankServer
{
  var databaseInstance: Database;  // Reference to the backend database service

  /**
   * Initial state for bank server setup
   */
  start state Init {
    entry (initialBalance: map[int, int]){
      // Create a new database instance with initial account balances
      databaseInstance = new Database((server = this, initialBalance = initialBalance));
      goto WaitForWithdrawRequests;
    }
  }

  /**
   * Main processing state - Handles client withdrawal requests
   */
  state WaitForWithdrawRequests {
    on eWithDrawReq do (withdrawRequest: tWithDrawReq) {
      var accountBalance: int;
      var withdrawalResponse: tWithDrawResp;

      // Query database for current account balance
      accountBalance = ReadBankBalance(databaseInstance, withdrawRequest.accountId);
      
      // Check if withdrawal would maintain minimum balance requirement (10)
      if(accountBalance - withdrawRequest.amount >= 10)
      {
        // Sufficient funds: update database with new balance
        var newBalance = accountBalance - withdrawRequest.amount;
        UpdateBankBalance(databaseInstance, withdrawRequest.accountId, newBalance);
        
        // Prepare success response
        withdrawalResponse = (
          status = WITHDRAW_SUCCESS, 
          accountId = withdrawRequest.accountId, 
          balance = newBalance, 
          rId = withdrawRequest.rId
        );
      }
      else // Insufficient funds for withdrawal
      {
        // Prepare error response (balance remains unchanged)
        withdrawalResponse = (
          status = WITHDRAW_ERROR, 
          accountId = withdrawRequest.accountId, 
          balance = accountBalance, 
          rId = withdrawRequest.rId
        );
      }

      // Send response back to the requesting client
      send withdrawRequest.source, eWithDrawResp, withdrawalResponse;
    }
  }
}

/**
 * Database Machine
 * ===============
 * 
 * Provides persistent storage for account balances. The Database handles:
 * 1. Storing and retrieving account balances
 * 2. Processing update and read queries from the BankServer
 * 3. Validating account existence before operations
 * 
 * External interaction occurs through two helper functions:
 * - ReadBankBalance: Retrieves the current balance for an account
 * - UpdateBankBalance: Updates the balance for an account
 */
machine Database
{
  var ownerServer: BankServer;                // Reference to the bank server that owns this database
  var accountBalances: map[int, int];         // Storage for account balances (accountId -> balance)
  
  start state Init {
    entry(input: (server : BankServer, initialBalance: map[int, int])){
      ownerServer = input.server;
      accountBalances = input.initialBalance;
    }
    
    // Handle balance update requests
    on eUpdateQuery do (updateRequest: (accountId: int, balance: int)) {
      // Validate account exists before updating
      assert updateRequest.accountId in accountBalances, "Invalid accountId received in the update query!";
      
      // Update the account balance
      accountBalances[updateRequest.accountId] = updateRequest.balance;
    }
    
    // Handle balance read requests
    on eReadQuery do (readRequest: (accountId: int))
    {
      // Validate account exists before reading
      assert readRequest.accountId in accountBalances, "Invalid accountId received in the read query!";
      
      // Send the current balance back to the requesting server
      send ownerServer, eReadQueryResp, (
        accountId = readRequest.accountId, 
        balance = accountBalances[readRequest.accountId]
      );
    }
  }
}

/**
 * ReadBankBalance - Helper function to retrieve an account's balance
 * 
 * @param database  The Database instance to query
 * @param accountId The account to look up
 * @return The current balance for the specified account
 */
fun ReadBankBalance(database: Database, accountId: int) : int {
    var retrievedBalance: int;
    
    // Send read request to database
    send database, eReadQuery, (accountId = accountId,);
    
    // Wait for and process database response
    receive {
      case eReadQueryResp: (response: (accountId: int, balance: int)) {
        retrievedBalance = response.balance;
      }
    }
    
    return retrievedBalance;
}

/**
 * UpdateBankBalance - Helper function to modify an account's balance
 * 
 * @param database  The Database instance to update
 * @param accountId The account to modify
 * @param newBalance The new balance to set for the account
 */
fun UpdateBankBalance(database: Database, accountId: int, newBalance: int)
{
  // Send update request to database
  send database, eUpdateQuery, (accountId = accountId, balance = newBalance);
}
