event eEvent : (client : int, server : int);
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

    on eEvent do (pld : (client : int, server : int)) {
      var connect : bool;
      var disconnect : bool;
      connect = semaphore[pld.server];
      disconnect = link[pld.client][pld.server];
      // choice
      if (connect && disconnect) {
        print("both");
        if (choose()) {
          connect = false;
        } else {
          disconnect = false;
        }
      }
      if (connect) {
        link[pld.client][pld.server] = true;
        semaphore[pld.server] = false;
      }
      if (disconnect) {
        link[pld.client][pld.server] = false;
        semaphore[pld.server] = true;
      }
      if (connect || disconnect) {
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
      var client : int;
      var server : int;
      numRequests = numRequests + 1;
      if (numRequests >= maxNumRequests) {
          raise halt;
      }
      client = choose(clients);
      server = choose(servers);
      send global, eEvent, (client=client, server=server);
    }
  }
}
