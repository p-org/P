// Client module
module Client = { Client };

// Bank module
module Bank = { BankServer, Database };

// Abstract Bank Server module
module AbstractBank = { AbstractBankServer -> BankServer };