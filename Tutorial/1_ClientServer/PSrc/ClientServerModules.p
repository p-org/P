/**
 * ClientServerModules.p
 * ====================
 * This file defines module encapsulations that organize the client-server
 * system components into logical units and define interfaces between them.
 */

/**
 * Client Module
 * ------------
 * Represents the client side of the banking application.
 * Contains the Client state machine that interacts with the bank.
 */
module Client = { Client };

/**
 * Bank Module
 * ----------
 * Represents the fully implemented bank service with database backend.
 * Contains both the BankServer and its dependent Database machine.
 */
module Bank = { BankServer, Database };

/**
 * Abstract Bank Module
 * ------------------
 * Provides a simplified bank implementation for testing.
 * Maps AbstractBankServer to the BankServer interface, allowing it to be
 * used as a drop-in replacement for the full Bank module when testing clients.
 * This reduces the state space by eliminating database interactions.
 */
module AbstractBank = { AbstractBankServer -> BankServer };
