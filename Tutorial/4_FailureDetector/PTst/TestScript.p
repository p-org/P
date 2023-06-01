test tcTest_FailureDetector [main=TestMultipleClients]:
  assert ReliableFailureDetector in
  union { TestMultipleClients }, FailureDetector, FailureInjector;