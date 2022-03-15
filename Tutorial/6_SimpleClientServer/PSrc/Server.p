machine Server {
  var serverId: int;
  var semaphore: bool;
  var linkedClientId: int;

  start state Init {
    entry (index: int) {
      serverId = index;
      semaphore = true;
      linkedClientId = -1;
      goto WaitForRequests;
    }
  }

  state WaitForRequests {
    on eConnect do (payload: tConnect) {
        var available: bool;
        available = $;
        if (available && semaphore) {
//            print format ("Server{0}: connected to Client{1}", serverId, payload.clientId);
            semaphore = false;
            linkedClientId = payload.clientId;
            send payload.client, eConnectAck, (server = this, serverId = serverId, clientId = payload.clientId);
        } else {
            send payload.client, eUnavailable, (server = this, serverId = serverId, clientId = payload.clientId);
        }
    }

    on eDisconnect do (payload: tDisconnect) {
//        print format ("Server{0}: disconnected to Client{1}", serverId, payload.clientId);
        semaphore = true;
        linkedClientId = -1;
    }
  }
}
