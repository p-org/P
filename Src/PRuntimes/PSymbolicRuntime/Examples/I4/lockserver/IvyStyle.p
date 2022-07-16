event eConnect : (client : int, server : int);
event eDisconnect : (client : int, server : int);
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

    on eConnect do (pld : (client : int, server : int)) {
      if (semaphore[pld.server]) {
        link[pld.client][pld.server] = true;
        semaphore[pld.server] = false;
        send driver, eNext;      
        check();
      }
    }

    on eDisconnect do (pld : (client : int, server : int)) {
      if (link[pld.client][pld.server]) {
        link[pld.client][pld.server] = false;
        semaphore[pld.server] = true;
        send driver, eNext;      
        check();
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
  start state Init {
    entry {
      var i : int;
      var j : int;
      var numClients : int;
      var numServers : int;
      numClients = 3;
      numServers = 2;
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
      if (choose()) {
          send global, eConnect, (client=choose(clients), server=choose(servers));
      }
      else {
          send global, eDisconnect, (client=choose(clients), server=choose(servers));
      }
    }

    on eNext do {
      if (choose()) {
          send global, eConnect, (client=choose(clients), server=choose(servers));
      }
      else {
          send global, eDisconnect, (client=choose(clients), server=choose(servers));
      }
    }
  }
}
