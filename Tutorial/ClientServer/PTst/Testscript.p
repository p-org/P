/* This file contains four different model checking scenarios */

// assert the safety properties for single client, single server scenario
test singleClientServer [main=TestDriver0]:
    assert BankIsNotAFraud, GuaranteedProgress in
    (union Client, Server, { TestDriver0 });

// assert the safety properties for the two client, single server scenario
test multipleClientsServer [main=TestDriver1]:
    assert BankIsNotAFraud, GuaranteedProgress in
    (union Client, Server, { TestDriver1 });

// assert the safety properties for the single client, single server scenario but with abstract server
 test singleClientAbstractServer [main=TestDriver0]:
    assert BankIsNotAFraud, GuaranteedProgress in
    (union Client, AbstractServer, { TestDriver0 });


