// PingPong.p 
event PING assert 1: machine; 
event PONG assert 1; 
event SUCCESS;
 
machine Client {
  var server: machine;  
  start state Init {  
    entry { 	
	  print "Client created\n";
      server = new Server(); 
      raise SUCCESS; 
    } 
    on SUCCESS goto SendPing; 
  }
  state SendPing { 
    entry { 
	  print "Client sending PING\n";
      announce M_PING, this;
      send server, PING, this;
      raise SUCCESS; 
    } 
    on SUCCESS goto WaitPong; 
  }
  state WaitPong { 
    on PONG goto SendPing; 
  }
}

machine Server { 
  var timer: TimerPtr;
  var client: machine;

  start state Init {  
    entry { 
	  print "Server created\n";
      timer = CreateTimer(this);
      raise SUCCESS; 
    }
    on SUCCESS goto WaitPing; 
  }

  state WaitPing { 
    on PING goto Sleep; 
  }
  
  state Sleep { 
    entry (payload: machine) {       
      client =  payload;
      StartTimer(timer, 1000);
    } 
    on TIMEOUT goto SendPong;
  }

  state SendPong { 
    entry (payload: TimerPtr) { 
	  print "Server sending PONG\n";
      announce M_PONG, client;
      send client, PONG; 
      raise SUCCESS; 
    } 
    on SUCCESS goto WaitPing; 
  }
} 

event M_PING: machine;
event M_PONG: machine;

spec Safety observes M_PING, M_PONG { 
    var pending: map[machine, int];
    start state Init { 
        on M_PING do (payload: machine) { 
            if (!(payload in pending)) 
                pending[payload] = 0; 
            pending[payload] = pending[payload] + 1; 
            assert (pending[payload] <= 3); 
        }
        on M_PONG do (payload: machine) {
            assert (payload in pending); 
            assert (0 < pending[payload]); 
            pending[payload] = pending[payload] - 1;
        }
    }
} 

