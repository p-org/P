
/**
 * TwoPhaseCommit Module
 * -------------------
 * This module unifies:
 * 1. Coordinator - The central component that manages the commit protocol
 * 2. Participant - Individual nodes that vote and follow coordinator instructions
 * 3. Timer - Used for timeouts in the protocol to handle failures
 * 
 * The union directive creates a single module containing all these components,
 * allowing them to interact in a distributed transaction environment.
 */
module TwoPhaseCommit = union { Coordinator, Participant }, Timer;
