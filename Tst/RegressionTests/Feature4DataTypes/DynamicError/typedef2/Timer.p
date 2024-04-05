// events from client to timer
event START: int;
// events from timer to client
event TIMEOUT: machine;

// local event for control transfer within timer
event UNIT;
machine Timer {
  var client: machine;
  start state Init {
    entry (arg: machine){
      client = arg;
      raise UNIT;  // goto handler of UNIT
    }
    on UNIT goto WaitForReq;
  }

  state WaitForReq {
    on START do (payload: int) { send client, TIMEOUT, this; }
  }

}
