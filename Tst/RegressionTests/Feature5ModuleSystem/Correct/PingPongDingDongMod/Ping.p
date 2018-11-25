machine PingDingMachine {
    var pongId: machine;

    start state Ping_start {
        entry {
  	     pongId = new PongDongMachine(this);
         goto SendPing;
        }
    }

    state SendPing {
      entry {
       send pongId, Ping, this;
		  }
      on Pong do {
        send pongId, Ding;
        receive {
          case Dong: {}
        }
      }
    }
}
