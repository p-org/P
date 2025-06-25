/**
 * Two-Phase Commit (2PC) Type Definitions
 * =====================================
 * This section defines the data structures used in the 2PC protocol implementation.
 */

/**
 * Transaction type - Represents a key-value pair modification with a unique identifier
 * 
 * @field key     The key identifier in the key-value store
 * @field val     The new value to be stored
 * @field transId The unique identifier for this transaction
 */
type tTrans = (key: string, val: int, transId: int);

/**
 * WriteTransactionRequest type - Client's request to modify data
 * 
 * @field client  The client machine initiating the transaction
 * @field trans   The transaction details (key, value, ID)
 */
type tWriteTransReq = (client: Client, trans: tTrans);

/**
 * WriteTransactionResponse type - Coordinator's response to a write request
 * 
 * @field transId The transaction ID this response corresponds to
 * @field status  The result of the transaction (SUCCESS, ERROR, TIMEOUT)
 */
type tWriteTransResp = (transId: int, status: tTransStatus);

/**
 * ReadTransactionRequest type - Client's request to retrieve data
 * 
 * @field client  The client machine requesting data
 * @field key     The key identifier to read from the store
 */
type tReadTransReq = (client: Client, key: string);

/**
 * ReadTransactionResponse type - Response to a read request
 * 
 * @field key     The key that was queried
 * @field val     The value associated with the key (if successful)
 * @field status  The result of the read operation
 */
type tReadTransResp = (key: string, val: int, status: tTransStatus);

/**
 * TransactionStatus enum - Possible outcomes of transaction operations
 */
enum tTransStatus {
  SUCCESS,  // Operation completed successfully
  ERROR,    // Operation failed due to an error
  TIMEOUT   // Operation failed due to timeout
}

/**
 * Client-Coordinator Communication Events
 * ====================================
 * Events used for interaction between clients and the coordinator
 */

// Client sends write transaction request to coordinator
event eWriteTransReq : tWriteTransReq;

// Coordinator sends write transaction result to client
event eWriteTransResp : tWriteTransResp;

// Client sends read data request to coordinator
event eReadTransReq : tReadTransReq;

// Participant sends read data result to client (via coordinator)
event eReadTransResp: tReadTransResp;

/**
 * Coordinator-Participant Communication Events
 * ========================================
 * Events used for the two-phase commit protocol communication
 */

// Phase 1: Coordinator asks participants if they can commit the transaction
event ePrepareReq: tPrepareReq;

// Phase 1: Participants vote on whether they can commit the transaction
event ePrepareResp: tPrepareResp;

// Phase 2: Coordinator instructs participants to commit the transaction
event eCommitTrans: int;  // Payload is transactionId

// Phase 2: Coordinator instructs participants to abort the transaction
event eAbortTrans: int;   // Payload is transactionId

/**
 * Protocol-specific Type Definitions
 * ===============================
 */

/**
 * PrepareRequest type - First phase of 2PC protocol
 * This type is equivalent to tTrans, containing the transaction details
 */
type tPrepareReq = tTrans;

/**
 * PrepareResponse type - Participant's vote on transaction commit
 * 
 * @field participant  The participant machine responding
 * @field transId      The transaction ID being voted on
 * @field status       The participant's vote (SUCCESS = can commit, ERROR/TIMEOUT = cannot commit)
 */
type tPrepareResp = (participant: Participant, transId: int, status: tTransStatus);

// Used during initialization to inform participants about the coordinator
event eInformCoordinator: Coordinator;

/**
 * Coordinator Machine
 * =================
 * 
 * The coordinator is the central component of the two-phase commit protocol,
 * responsible for managing the distributed transaction workflow.
 * 
 * Key responsibilities:
 * 1. Managing write transactions through the two-phase commit process:
 *    - Phase 1: Ask all participants to prepare (can you commit?)
 *    - Phase 2: Based on responses, either commit or abort
 * 
 * 2. Managing read transactions by:
 *    - Selecting an appropriate participant to fulfill the read
 *    - Forwarding read requests to that participant
 * 
 * 3. Transaction sequencing:
 *    - Processing transactions one at a time, in order of reception
 *    - Maintaining atomicity of transactions (all-or-nothing)
 * 
 * 4. Failure handling:
 *    - Using timeouts to detect participant failures
 *    - Aborting transactions when consensus cannot be reached
 *    - Ensuring system consistency even during failures
 */
machine Coordinator
{
  // All participants involved in the distributed transaction system
  var participantNodes: set[Participant];
  
  // Currently processing write transaction (only one active at a time)
  var activeWriteRequest: tWriteTransReq;
  
  // Transaction IDs that have been processed (prevents duplicates)
  var processedTransactionIds: set[int];
  
  // Timer for transaction timeout management
  var transactionTimer: Timer;

  /**
   * Initialization State
   * -----------------
   * Sets up the coordinator and notifies participants
   */
  start state Init {
    entry (payload: set[Participant]){
      // Store references to all participants in the system
      participantNodes = payload;
      
      // Create timer for managing transaction timeouts
      transactionTimer = CreateTimer(this);
      
      // Register this coordinator with all participants
      BroadcastToAllParticipants(eInformCoordinator, this);
      
      // Ready to process transactions
      goto WaitForTransactions;
    }
  }

  /**
   * WaitForTransactions State
   * ----------------------
   * Idle state waiting for client transaction requests (read or write)
   */
  state WaitForTransactions {
    // Handle write transaction requests from clients
    on eWriteTransReq do (writeRequest: tWriteTransReq) {
      // Enforce transaction ID uniqueness - reject duplicates
      if(writeRequest.trans.transId in processedTransactionIds)
      {
        // Respond with timeout status for duplicate transaction IDs
        send writeRequest.client, eWriteTransResp, (
          transId = writeRequest.trans.transId, 
          status = TIMEOUT
        );
        return;
      }

      // Store the current transaction for processing
      activeWriteRequest = writeRequest;
      
      // Phase 1: Send prepare requests to all participants
      BroadcastToAllParticipants(ePrepareReq, writeRequest.trans);
      
      // Start timeout timer for this transaction
      StartTimer(transactionTimer);
      
      // Move to state that collects votes from participants
      goto WaitForPrepareResponses;
    }

    // Handle read transaction requests from clients
    on eReadTransReq do (readRequest: tReadTransReq) {
      // Forward read request to a randomly selected participant
      // This provides load balancing and availability
      send choose(participantNodes), eReadTransReq, readRequest;
    }

    // Ignore prepare responses and timeouts when not processing a transaction
    // These are likely from previous transactions and can be safely ignored
    ignore ePrepareResp, eTimeOut;
  }

  /**
   * Counter for tracking "YES" votes from participants
   * Needs to match the total number of participants for a commit decision
   */
  var positiveVoteCount: int;

  /**
   * WaitForPrepareResponses State
   * --------------------------
   * Collects votes from all participants in phase 1 of 2PC
   * Decides whether to commit or abort based on collected votes
   */
  state WaitForPrepareResponses {
    // Queue any incoming write requests since we're processing a transaction
    // These will be handled after current transaction completes
    defer eWriteTransReq;

    // Handle prepare responses (votes) from participants
    on ePrepareResp do (response: tPrepareResp) {
      // Verify this response is for our active transaction
      if (activeWriteRequest.trans.transId == response.transId) {
        if(response.status == SUCCESS)
        {
          // Count this "YES" vote
          positiveVoteCount = positiveVoteCount + 1;
          
          // Check if we have unanimous agreement from all participants
          if(positiveVoteCount == sizeof(participantNodes))
          {
            // All participants voted YES - commit the transaction
            DoGlobalCommit();
            
            // Return to idle state to process next transaction
            goto WaitForTransactions;
          }
        }
        else
        {
          // At least one participant voted NO - abort the transaction
          DoGlobalAbort(ERROR);
          
          // Return to idle state to process next transaction
          goto WaitForTransactions;
        }
      }
    }

    // Handle transaction timeout (missing responses from participants)
    on eTimeOut goto WaitForTransactions with { DoGlobalAbort(TIMEOUT); }

    // Still handle read requests during an ongoing write transaction
    on eReadTransReq do (readRequest: tReadTransReq) {
      // Forward read to randomly selected participant
      send choose(participantNodes), eReadTransReq, readRequest;
    }

    // Reset vote counter when leaving this state
    exit {
      positiveVoteCount = 0;
    }
  }

  /**
   * DoGlobalAbort - Executes the abort decision for the current transaction
   * 
   * This function handles the phase 2 for an ABORT decision:
   * 1. Notifies all participants to abort the transaction
   * 2. Responds to the client with the failure status
   * 3. Cancels the transaction timer if not already triggered
   * 
   * @param responseStatus The specific reason for the abort (ERROR or TIMEOUT)
   */
  fun DoGlobalAbort(responseStatus: tTransStatus) {
    // Notify all participants to abort/rollback the transaction
    BroadcastToAllParticipants(eAbortTrans, activeWriteRequest.trans.transId);
    
    // Notify client that the transaction has failed
    send activeWriteRequest.client, eWriteTransResp, (
      transId = activeWriteRequest.trans.transId, 
      status = responseStatus
    );
    
    // Cancel the timer if abort is due to error (not needed for timeout)
    if(responseStatus != TIMEOUT)
      CancelTimer(transactionTimer);
  }

  /**
   * DoGlobalCommit - Executes the commit decision for the current transaction
   * 
   * This function handles phase 2 for a COMMIT decision:
   * 1. Instructs all participants to permanently apply the transaction
   * 2. Notifies the client of successful completion
   * 3. Cancels the transaction timeout timer
   */
  fun DoGlobalCommit() {
    // Record this transaction ID as processed
    processedTransactionIds += activeWriteRequest.trans.transId;
    
    // Instruct all participants to permanently commit the transaction
    BroadcastToAllParticipants(eCommitTrans, activeWriteRequest.trans.transId);
    
    // Notify client of successful transaction
    send activeWriteRequest.client, eWriteTransResp, (
      transId = activeWriteRequest.trans.transId, 
      status = SUCCESS
    );
    
    // Cancel the transaction timer
    CancelTimer(transactionTimer);
  }

  /**
   * BroadcastToAllParticipants - Sends a message to all participants in the system
   * 
   * This helper function provides efficient communication with all participants,
   * which is essential for both phases of the 2PC protocol.
   * 
   * @param message The event type to broadcast
   * @param payload The data payload to send with the event
   */
  fun BroadcastToAllParticipants(message: event, payload: any)
  {
    var participantIndex: int;
    while (participantIndex < sizeof(participantNodes)) {
      send participantNodes[participantIndex], message, payload;
      participantIndex = participantIndex + 1;
    }
  }
}
