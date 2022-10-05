event eConnect;
event eDisconnect;
event eNext;

machine Global {
  var link : map[int, map[int, bool]];
  var semaphore : map[int, bool];
  var driver : machine;
  var clients : seq[int];
  var servers : seq[int];

  start state Init {
    entry (pld : (driver : machine, clients : seq[int], servers : seq[int])) {
      var i : int;
      var j : int;
      var serverToBool : map[int, bool];
      driver = pld.driver;
      while (i < sizeof(pld.servers)) {
        semaphore[pld.servers[i]] = true;
        serverToBool[pld.servers[i]] = false;
        servers += (i, pld.servers[i]);
        i = i + 1;
      }
      j = 0;
      while (j < sizeof(pld.clients)) { 
        link[pld.clients[j]] = serverToBool;
        clients += (j, pld.clients[j]);
        j = j + 1;
      }
    }

    on eConnect do {
      var i : int;
      var j : int;
      var serverIdxChoices : seq[int];
      var server : int;
      var client : int;
      while (i < sizeof(servers)) {
        if (semaphore[servers[i]]) {
          serverIdxChoices += (j, i);
          j = j + 1; 
        }
        i = i + 1;
      }
      
      if (sizeof(serverIdxChoices) > 0) {
        server = servers[choose(serverIdxChoices)];
        link[choose(clients)][server] = true;
        semaphore[server] = false;
        send driver, eNext;      
        check();
      }
    }

    on eDisconnect do {
      var clientIdxChoices : seq[int];
      var serverIdxChoices : seq[int];
      var client : int;
      var server : int;
      var i : int;
      var j : int;
      var k : int;
      while (i < sizeof(clients)) {
        j = 0;
        while (j < sizeof(servers)) {
          if (link[clients[i]][servers[j]]) {
            clientIdxChoices += (k, i);
            k = k + 1;
          }
          j = j + 1;
        }
        i = i + 1;
      }
      if (sizeof(clientIdxChoices) > 0) {
        client = clients[choose(clientIdxChoices)];
        i = 0;
        j = 0;
        while (i < sizeof(servers)) {
          if (link[client][servers[i]]) {
            serverIdxChoices += (j, i);
            j = j + 1;
          }
          i = i + 1;
        } 
        if (sizeof(serverIdxChoices) > 0) {
          server = choose(serverIdxChoices);
          server = servers[choose(serverIdxChoices)];
          link[client][server] = false;
          semaphore[server] = true;
          send driver, eNext;      
          check();
        }
      }
    }
  }

  fun check() {
    var i : int;
    var j : int;
    var k : int;
    while (i < sizeof(clients)) {
      j = 0;
      while (j < sizeof(servers)) {
        if (link[clients[i]][servers[j]]) {
          k = 0;
          while (k < sizeof(clients)) {
            if (clients[k] != clients[i]) {
              assert(link[clients[k]][servers[j]] == false);
            }
            k = k + 1;
          }
        }
        j = j + 1;
      }
      i = i + 1;
    }
  }
}

machine Main {
  var clients : seq[int];
  var servers : seq[int];
  var global  : machine;
  var maxNumRequests : int;
  var numRequests : int;
  start state Init {
    entry {
      var i : int;
      var j : int;
      var numClients : int;
      var numServers : int;
      numClients = 5;
      numServers = 2;
      maxNumRequests = 10;
      i = 0;
      while (i < numClients) {
        clients += (i, i);
        i = i + 1;
      }
      j = 0;
      while (j < numServers) {
        servers += (j, j + i);
        j = j + 1;
      }
      global = new Global((driver=this, clients=clients, servers=servers)); 
      raise eNext;
    }

    on eNext do {
      numRequests = numRequests + 1;
      if (numRequests >= maxNumRequests) {
          raise halt;
      }
      if (choose()) {
          send global, eConnect;
      }
      else {
          send global, eDisconnect;
      }
    }
  }
}
