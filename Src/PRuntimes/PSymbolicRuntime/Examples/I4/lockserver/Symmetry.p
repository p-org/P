event eConnect;
event eDisconnect;
event eNext;

machine Global {
  var link : map[int, map[int, bool]];
  var semaphore : map[int, bool];
  var driver : machine;
  var clients : seq[int];
  var servers : seq[int];
  var chosenClients : seq[int];
  var chosenServers : seq[int];

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
      var k : int;
      var added : bool;
      var serverIdxChoices : seq[int];
      var server : int;

      added = false;
      while (i < sizeof(servers)) {
        if (semaphore[servers[i]]) {
          if (!(servers[i] in chosenServers)) {
            if (!added) {
              chosenServers += (sizeof(chosenServers), servers[i]);
              added = true;
            }
          }
          if (servers[i] in chosenServers) {
            serverIdxChoices += (j, i);
            j = j + 1;
          }
        }
        i = i + 1;
      }
      
      i = 0;
      added = false;
      while (!added && (i < sizeof(clients))) {
        if (!(clients[i] in chosenClients)) {
          chosenClients += (sizeof(chosenClients), clients[i]);
          added = true;
        }
        i = i + 1;
      }

      if (sizeof(serverIdxChoices) > 0) {
        server = servers[choose(serverIdxChoices)];
        link[choose(chosenClients)][server] = true;
        semaphore[server] = false;
        send driver, eNext;      
        check();
      }
    }

    on eDisconnect do {
      var clientServerIdxChoices : seq[(client: int, server: int)];
      var client : int;
      var server : int;
      var i : int;
      var j : int;
      var k : int;
      var addedClient : bool;
      var addedServer : bool;

      addedClient = false;
      addedServer = false;
      while (i < sizeof(clients)) {
        j = 0;
        while (j < sizeof(servers)) {
          if (link[clients[i]][servers[j]]) {
            if (!addedClient && !(clients[i] in chosenClients)) {
              chosenClients += (sizeof(chosenClients), clients[i]);
              addedClient = true;
            }
            if (!addedServer && !(servers[j] in chosenServers)) {
              chosenServers += (sizeof(chosenServers), servers[j]);
              addedServer = true;
            }
            if ((clients[i] in chosenClients) && (servers[j] in chosenServers)) {
              clientServerIdxChoices += (k, (client=i, server=j));
            }
            k = k + 1;
          }
          j = j + 1;
        }
        i = i + 1;
      }
      if (sizeof(clientServerIdxChoices) > 0) {
        i = choose(sizeof(clientServerIdxChoices));
        client = clients[clientServerIdxChoices[i].client];
        server = servers[clientServerIdxChoices[i].server];
        link[client][server] = false;
        semaphore[server] = true;
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
      numClients = 5;
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
      raise eNext;
    }

    on eNext do {
      if (choose()) {
          send global, eConnect;
      }
      else {
          send global, eDisconnect;
      }
    }
  }
}
