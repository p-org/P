// Client module
module Client = { Client };

// Server module
module Server = { BankServer, Database };

// Abstract Server module
module AbstractServer = { AbstractBankServer -> BankServer };