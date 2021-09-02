// TestDriver0 creates 1 client and 1 server for checking the client-server system
machine TestDriver0
{
  start state Init {
    entry {
      var server : BankServer;
      var balance: map[int, int];
      balance[1] = 100;

      announce eSpec_BankIsNotAFraud_Init, balance;
      //create server
      server = new BankServer(balance);
      //create client
      new Client((serv = server, accountId = 1, balance = balance[1]));
    }
  }
}

// TestDriver0 creates 2 client and 1 server for checking the client-server system
machine TestDriver1
{
    start state Init {
    entry {
        var server : BankServer;
        var balance: map[int, int];
        balance[1] = 100;
        balance[2] = 1000;

        announce eSpec_BankIsNotAFraud_Init, balance;

        //create server
        server = new BankServer(balance);
        // create client 1
        new Client((serv = server, accountId = 1, balance = balance[1]));
        // create client 2
        new Client((serv = server, accountId = 2, balance = balance[2]));
    }
  }
}