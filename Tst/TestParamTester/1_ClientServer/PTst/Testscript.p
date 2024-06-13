/* This file contains three different model checking scenarios */

// assert the properties for the single client and single server scenario
test tcSingleClient [main=TestWithSingleClient]:
  assert BankBalanceIsAlwaysCorrect, GuaranteedWithDrawProgress in
  (union Client, Bank, { TestWithSingleClient });

// assert the properties for the two clients and single server scenario
test tcMultipleClients [main=TestWithMultipleClients]:
  assert BankBalanceIsAlwaysCorrect, GuaranteedWithDrawProgress in
  (union Client, Bank, { TestWithMultipleClients });

// assert the properties for the single client and single server scenario but with abstract server
 test tcAbstractServer [main=TestWithSingleClient]:
  assert BankBalanceIsAlwaysCorrect, GuaranteedWithDrawProgress in
  (union Client, AbstractBank, { TestWithSingleClient });

paramtest (globalnumClients in [2, 3, 4], global1 in [1,2], global2 in [4, 5]) aaaa1 [main=TestWithConfig]:
  assert BankBalanceIsAlwaysCorrect, GuaranteedWithDrawProgress in
  (union Client, Bank, { TestWithConfig });

paramtest (globalnumClients in [1]) aaa2 [main=TestWithConfig]:
  assert BankBalanceIsAlwaysCorrect, GuaranteedWithDrawProgress in
  (union Client, Bank, { TestWithConfig });

constant dummyGv : bool;
paramtest (globalnumClients in [1], dummyGv in [true, false]) aaa3 [main=TestWithConfig]:
  assert BankBalanceIsAlwaysCorrect, GuaranteedWithDrawProgress in
  (union Client, Bank, { TestWithConfig });

constant rich1: float;
constant rich2: any;

paramtest (globalnumClients in [1], rich1 in [3.0, 2.1], rich2 in [null]) aaa4 [main=TestWithConfig]:
  assert BankBalanceIsAlwaysCorrect, GuaranteedWithDrawProgress in
  (union Client, Bank, { TestWithConfig });

// Syntax error
// paramtest () wrong1 [main=TestWithSingleClient]:
//   assert BankBalanceIsAlwaysCorrect, GuaranteedWithDrawProgress in
//   (union Client, Bank, { TestWithSingleClient });

// Syntax error
// paramtest (globalnumClients in []) wrong2 [main=TestWithSingleClient]:
//   assert BankBalanceIsAlwaysCorrect, GuaranteedWithDrawProgress in
//   (union Client, Bank, { TestWithSingleClient });

// Duplicate Assign
// paramtest (globalnumClients in [1], globalnumClients in [1]) wrong3 [main=TestWithSingleClient]:
//   assert BankBalanceIsAlwaysCorrect, GuaranteedWithDrawProgress in
//   (union Client, Bank, { TestWithSingleClient });

// Undelared global variable
// paramtest (x in [1], globalnumClients in [1]) wrong4 [main=TestWithSingleClient]:
//   assert BankBalanceIsAlwaysCorrect, GuaranteedWithDrawProgress in
//   (union Client, Bank, { TestWithSingleClient });

// Type mismatch
// paramtest (globalnumClients in [1], dummyGv in [2, 3]) wrong5 [main=TestWithSingleClient]:
//   assert BankBalanceIsAlwaysCorrect, GuaranteedWithDrawProgress in
//   (union Client, Bank, { TestWithSingleClient });

// Type mismatch
// paramtest (globalnumClients in [1], dummyGv in [true, 3]) wrong6 [main=TestWithConfig]:
//   assert BankBalanceIsAlwaysCorrect, GuaranteedWithDrawProgress in
//   (union Client, Bank, { TestWithConfig });