machine PongDongMachine {
	var pingid: machine;
    start state _Init {
      entry (payload: machine)
      {
        pingid = payload;
        goto WaitPing;
      }
    }

    state WaitPing {
      on Ping do {
        send pingid, Pong;
        goto WaitDing;
      }
		
    }

    state WaitDing {
        on Ding do {
        send pingid, Dong;
        goto WaitPing;
      }
    }	
}
