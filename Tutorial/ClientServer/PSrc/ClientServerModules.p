// Client module
module Client = { Client };

// Bank Server module
module BankServer = { BankServer, Database };

// Abstract Bank Server module
module AbstractBankServer = { AbstractBankServer -> BankServer };