/* This file contains four different model checking scenarios */

// assert the safety properties for single client, single server scenario
test singleClient [main=TestWithSingleClient]:
  assert BankBalanceIsAlwaysCorrect, GuaranteedWithDrawProgress in
  (union Client, BankServer, { TestWithSingleClient });

// assert the safety properties for the two client, single server scenario
test multipleClients [main=TestWithMultipleClients]:
  assert BankBalanceIsAlwaysCorrect, GuaranteedWithDrawProgress in
  (union Client, BankServer, { TestWithMultipleClients });

// assert the safety properties for the single client, single server scenario but with abstract server
 test singleClientAbstractServer [main=TestWithSingleClient]:
  assert BankBalanceIsAlwaysCorrect, GuaranteedWithDrawProgress in
  (union Client, AbstractBankServer, { TestWithSingleClient });


