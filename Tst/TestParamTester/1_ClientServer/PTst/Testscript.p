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

test param (nClients in [2, 3, 4], g1 in [1,2], g2 in [4, 5]) aaaa1 [main=TestWithConfig]:
  assert BankBalanceIsAlwaysCorrect, GuaranteedWithDrawProgress in
  (union Client, Bank, { TestWithConfig });

test param (nClients in [2, 3, 4], g1 in [1,2], g2 in [4, 5]) assume (nClients + g1 < g2) aaaa2 [main=TestWithConfig]:
  assert BankBalanceIsAlwaysCorrect, GuaranteedWithDrawProgress in
  (union Client, Bank, { TestWithConfig });

param b1: bool;

test param (nClients in [2, 3, 4], g1 in [1,2], g2 in [4, 5], b1 in [true, false]) assume (b1 == (nClients + g1 > g2)) aaaa3 [main=TestWithConfig]:
  assert BankBalanceIsAlwaysCorrect, GuaranteedWithDrawProgress in
  (union Client, Bank, { TestWithConfig });

  test param (nClients in [2, 3, 4], g1 in [1,2], g2 in [4, 5], b1 in [true, false]) 
      assume (b1 == (nClients + g1 > g2)) 
      (4 wise) testTWise4 [main=TestWithConfig]:
  assert BankBalanceIsAlwaysCorrect, GuaranteedWithDrawProgress in
  (union Client, Bank, { TestWithConfig });

  test param (nClients in [2, 3, 4], g1 in [1,2], g2 in [4, 5], b1 in [true, false]) 
      assume (b1 == (nClients + g1 > g2)) 
      (3 wise) testTWise3 [main=TestWithConfig]:
  assert BankBalanceIsAlwaysCorrect, GuaranteedWithDrawProgress in
  (union Client, Bank, { TestWithConfig });

  test param (nClients in [2, 3, 4], g1 in [1,2], g2 in [4, 5], b1 in [true, false]) 
      assume (b1 == (nClients + g1 > g2)) 
      (2 wise) testTWise2 [main=TestWithConfig]:
  assert BankBalanceIsAlwaysCorrect, GuaranteedWithDrawProgress in
  (union Client, Bank, { TestWithConfig });


// Syntax error
// paramtest () wrong1 [main=TestWithSingleClient]:
//   assert BankBalanceIsAlwaysCorrect, GuaranteedWithDrawProgress in
//   (union Client, Bank, { TestWithSingleClient });

// Syntax error
// paramtest (nClients in []) wrong2 [main=TestWithSingleClient]:
//   assert BankBalanceIsAlwaysCorrect, GuaranteedWithDrawProgress in
//   (union Client, Bank, { TestWithSingleClient });

// Duplicate Assign
// paramtest (nClients in [1], nClients in [1]) wrong3 [main=TestWithSingleClient]:
//   assert BankBalanceIsAlwaysCorrect, GuaranteedWithDrawProgress in
//   (union Client, Bank, { TestWithSingleClient });

// Undelared global variable
// paramtest (x in [1], nClients in [1]) wrong4 [main=TestWithSingleClient]:
//   assert BankBalanceIsAlwaysCorrect, GuaranteedWithDrawProgress in
//   (union Client, Bank, { TestWithSingleClient });

// Type mismatch
// paramtest (nClients in [1], dummyGv in [2, 3]) wrong5 [main=TestWithSingleClient]:
//   assert BankBalanceIsAlwaysCorrect, GuaranteedWithDrawProgress in
//   (union Client, Bank, { TestWithSingleClient });

// Type mismatch
// paramtest (nClients in [1], dummyGv in [true, 3]) wrong6 [main=TestWithConfig]:
//   assert BankBalanceIsAlwaysCorrect, GuaranteedWithDrawProgress in
//   (union Client, Bank, { TestWithConfig });