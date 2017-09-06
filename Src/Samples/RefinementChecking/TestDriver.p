//Test driver that creates 1 client and 1 server for testing the client-server system
machine TestDriver0
receives;
sends;
 {
  start state Init {
    entry {
      var server : ServerClientInterface;
      //create server
      server = new ServerClientInterface();
      //create client
      new ClientInterface(server);
    }
  }
}