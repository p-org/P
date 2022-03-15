machine Client {
  var clientId: int;
  var servers: seq[Server];
  var numAttempts: int;
  var maxAttempts: int;
  var linkedServerId: int;

  start state Init {
    entry (payload: (_clientId: int, _maxAttempts: int, _servers: seq[Server])) {
      clientId = payload._clientId;
      maxAttempts = payload._maxAttempts;
      servers = payload._servers;
      numAttempts = 0;
      linkedServerId = -1;
      goto AttemptConnecting;
    }
  }

  state AttemptConnecting {
    entry {
        var randomId: int;
        if (numAttempts < maxAttempts) {
            numAttempts = numAttempts + 1;
            randomId = choose(sizeof(servers));
//            print format ("Client{0}: sending connection request to Server{1}", clientId, randomId);
            send servers[randomId], eConnect, (client = this, clientId = clientId, serverId = randomId);
        } else {
            goto Done;
        }
    }

    on eConnectAck do (payload: tConnectAck) {
        linkedServerId = payload.serverId;
        goto Wait;
    }

    on eUnavailable do (payload: tUnavailable) {
//        print format ("Client{0}: Server{1} is unavailable", clientId, payload.serverId);
        goto AttemptConnecting;
        // add error: comment out above line and uncomment the below line
//        goto Wait;
    }
  }

  state Wait {
    entry {
        assert (linkedServerId != -1),
        format ("Client{0}: is not connected to any server", clientId);

//        print format ("Client{0}: sending disconnection request to Server{1}", clientId, linkedServerId);
        send servers[linkedServerId], eDisconnect, (client = this, clientId = clientId, serverId = linkedServerId);
    }

  }

  state Done {
  }

}
