/**
 * Participant Machine
 * =================
 * 
 * The Participant represents a node in the distributed system that:
 * 1. Stores a portion of the shared key-value data
 * 2. Participates in the two-phase commit protocol
 * 3. Processes read requests directly from clients
 * 
 * As part of the 2PC protocol, each participant:
 * - Votes on whether it can commit transactions (phase 1)
 * - Commits or aborts transactions based on coordinator instructions (phase 2)
 * - Maintains transaction atomicity across the distributed system
 * - Ensures consistency of the stored data
 * 
 * Each participant maintains its own local key-value store which reflects
 * the committed state of the data. It also tracks pending transactions
 * that have been prepared but not yet committed or aborted.
 */
machine Participant {
  // Persistent storage for committed key-value pairs
  var keyValueStore: map[string, tTrans];
  
  // Transactions in the "prepared" state (phase 1 complete, awaiting phase 2)
  var pendingTransactions: map[int, tTrans];
  
  // Reference to the 2PC coordinator for this system
  var coordinatorNode: Coordinator;

  /**
   * Initialization State
   * -----------------
   * Waits to receive the coordinator reference before becoming active
   */
  start state Init {
    // Store the coordinator reference when received
    on eInformCoordinator goto WaitForRequests with (coordinator: Coordinator) {
      coordinatorNode = coordinator;
    }
    
    // Queue any shutdown events until we're properly initialized
    defer eShutDown;
  }

  /**
   * WaitForRequests State
   * ------------------
   * Main processing state that handles protocol messages and client requests
   */
  state WaitForRequests {
    /**
     * Handle transaction abort request (Phase 2 - abort decision)
     * 
     * When coordinator decides to abort a transaction, participants
     * must discard any prepared changes.
     */
    on eAbortTrans do (transactionId: int) {
      // Verify this is for a transaction we're actually tracking
      assert transactionId in pendingTransactions,
      format ("Protocol violation: Abort request for unknown transaction, transId: {0}, pendingTrans set: {1}",
        transactionId, pendingTransactions);
      
      // Discard the pending transaction (no changes are applied)
      pendingTransactions -= (transactionId);
    }

    /**
     * Handle transaction commit request (Phase 2 - commit decision)
     * 
     * When coordinator decides to commit a transaction, participants
     * must apply the prepared changes permanently.
     */
    on eCommitTrans do (transactionId: int) {
      // Verify this is for a transaction we're actually tracking
      assert transactionId in pendingTransactions,
      format ("Protocol violation: Commit request for unknown transaction, transId: {0}, pendingTrans set: {1}",
        transactionId, pendingTransactions);
        
      // Apply the transaction to the key-value store (make it permanent)
      var transaction = pendingTransactions[transactionId];
      keyValueStore[transaction.key] = transaction;
      
      // Remove from pending transactions since it's now committed
      pendingTransactions -= (transactionId);
    }

    /**
     * Handle prepare request (Phase 1 of 2PC protocol)
     * 
     * The participant must decide if it can commit the transaction
     * and vote YES (SUCCESS) or NO (ERROR).
     */
    on ePrepareReq do (prepareRequest: tPrepareReq) {
      // Verify transaction ID uniqueness
      assert !(prepareRequest.transId in pendingTransactions),
      format ("Protocol violation: Duplicate transaction ID received! TransId: {0}, pending transactions: {1}",
        prepareRequest.transId, pendingTransactions);
      
      // Track this transaction as "prepared" (phase 1 complete)
      pendingTransactions[prepareRequest.transId] = prepareRequest;
      
      // Decide whether to vote YES or NO on this transaction
      // Vote YES if:
      // 1. This is a new key we don't have yet, OR
      // 2. This transaction has a newer ID than our current stored value
      if (!(prepareRequest.key in keyValueStore) || 
          (prepareRequest.key in keyValueStore && prepareRequest.transId > keyValueStore[prepareRequest.key].transId)) {
        // Vote YES - We can commit this transaction
        send coordinatorNode, ePrepareResp, (
          participant = this, 
          transId = prepareRequest.transId, 
          status = SUCCESS
        );
      } else {
        // Vote NO - We cannot commit this transaction
        send coordinatorNode, ePrepareResp, (
          participant = this, 
          transId = prepareRequest.transId, 
          status = ERROR
        );
      }
    }

    /**
     * Handle read requests from clients
     * 
     * Reads are served from the committed data only,
     * not from pending transactions.
     */
    on eReadTransReq do (readRequest: tReadTransReq) {
      if(readRequest.key in keyValueStore)
      {
        // Key exists - return the current value
        send readRequest.client, eReadTransResp, (
          key = readRequest.key, 
          val = keyValueStore[readRequest.key].val, 
          status = SUCCESS
        );
      }
      else
      {
        // Key doesn't exist - return error
        send readRequest.client, eReadTransResp, (
          key = "", 
          val = -1, 
          status = ERROR
        );
      }
    }

    /**
     * Handle shutdown request
     * 
     * Cleanly terminates this participant
     */
    on eShutDown do {
      // Stop processing and terminate
      raise halt;
    }
  }
}
