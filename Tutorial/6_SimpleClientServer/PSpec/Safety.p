// event: initialize the SafetyInvariant spec monitor
event eMonitor_SafetyInitialize: int;

/**********************************
We would like to assert the safety property that:
no two clients are connected to the same server
***********************************/
spec SafetyInvariant observes eConnectAck, eDisconnect, eMonitor_SafetyInitialize
{
  var connections: map[int, int];

  start state Init {
    on eMonitor_SafetyInitialize do {
      connections = default(map[int, int]);
      goto WaitForEvents;
    }
  }

  state WaitForEvents {
    on eConnectAck do (payload: tConnectAck) {
      var serverId: int;
      var clientId: int;
      serverId = payload.serverId;
      clientId = payload.clientId;
      if(!(serverId in connections)) {
        connections[serverId] = clientId;
      } else {
          assert (connections[serverId] == clientId),
          format ("Monitor: Server{0} is connected to both Client{1} and Client{2}", serverId, connections[serverId], clientId);
      }
    }

    on eDisconnect do (payload: tDisconnect) {
      var serverId: int;
      var clientId: int;
      serverId = payload.serverId;
      clientId = payload.clientId;

      assert (serverId in connections),
      format ("Monitor: No existing connection found for Server{0} when disconnecting Client{1}", serverId, clientId);

      assert (connections[serverId] == clientId),
      format ("Monitor: Server{0} found to be connected to Client{1} when disconnecting Client{2}", serverId, connections[serverId], clientId);

      connections -= (serverId);
    }
  }
}