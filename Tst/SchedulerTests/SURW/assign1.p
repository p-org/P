event WriteReq : int;
event ReadResp: int;
event ReadReq: Main;

machine ClientA {
  start state Init {
    entry(server: Server) {
      send server, WriteReq, (1);
      send server, WriteReq, (2);
      send server, WriteReq, (3);
      send server, WriteReq, (4);
      send server, WriteReq, (5);
      send server, WriteReq, (6);
    }
  }
}

machine ClientB {
  start state Init {
    entry(server: Server) {
      send server, WriteReq, (7);
      send server, WriteReq, (8);
      send server, WriteReq, (9);
      send server, WriteReq, (10);
      send server, WriteReq, (11);
      send server, WriteReq, (12);
    }
  }
}

machine ClientC {
  start state Init {
    entry(server: Server) {
      send server, WriteReq, (13);
      send server, WriteReq, (14);
      send server, WriteReq, (15);
      send server, WriteReq, (16);
      send server, WriteReq, (17);
      send server, WriteReq, (18);
    }
  }
}


machine Server {
  var value: int;
  start state Init {
    entry {
      value = 0;
    }
    on WriteReq do (req: int) {
      value = req;
    }
    on ReadReq do (req: Main) {
      send req, ReadResp, value;
    }
  }
}

machine Main {
 start state Init {
  entry {
    var server: Server;
    server = new Server();
    new ClientA(server);
    new ClientB(server);
    new ClientC(server);
    send server, ReadReq, (this);
  }
  on ReadResp do (resp: int) {
    print format("Value:  {0}", resp);
  }
 }
}
