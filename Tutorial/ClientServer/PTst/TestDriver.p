// TestDriver0 creates 1 client and 1 server for checking the client-server system
machine TestDriver0
{
  start state Init {
    entry {
      var server : Server;
      //create server
      server = new Server();
      //create client
      new Client(server);
    }
  }
}

// TestDriver0 creates 2 client and 1 server for checking the client-server system
machine TestDriver1
{
    start state Init {
    entry {
        var server : Server;
        //create server
        server = new Server();
        // create client 1
        new Client(server);
        // create client 2
        new Client(server);
    }
  }
}