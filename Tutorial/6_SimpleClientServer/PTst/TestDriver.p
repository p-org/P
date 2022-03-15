machine Main {
  var numServers  : int;
  var numClients  : int;
  var maxAttempts : int;

  start state Init {
    entry {
      var servers: seq[Server];
      var clients: seq[Client];
      var index : int;

      numServers = 2;
      numClients = 3;
      maxAttempts = 2;

      //print format ("No. of servers = {0}, No. of clients = {1}, Max connection attempts = {2}", numServers, numClients, maxAttempts);

      //create servers
      index = 0;
      while(index < numServers) {
        servers += (index, new Server(index));
        index = index + 1;
      }

      //create clients
      index = 0;
      while(index < numClients) {
          clients += (index, new Client((_clientId = index, _maxAttempts = maxAttempts, _servers = servers)));
          index = index + 1;
      }

      announce eMonitor_SafetyInitialize;
    }
  }
}
