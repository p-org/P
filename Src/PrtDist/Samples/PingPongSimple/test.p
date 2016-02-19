#include "timer.p"

// PingPong.p 
event PING assert 1: machine; 
event PONG assert 1; 
event SUCCESS;

main machine Client {
  var server: machine;
  start state Init { 
    entry { 
      server = new Server(); 
      raise SUCCESS; 
    } 
    on SUCCESS goto SendPing; 
  }
  state SendPing { 
    entry { 
      monitor M_PING, this;
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
  var timer: machine;
  var client: machine;

  start state WaitPing {  
    entry { 
      timer = new Timer(this);
    }
    on PING goto Sleep; 
  }
  
  state Sleep { 
    entry (payload: machine) {       
      client =  payload;
      send timer, START, 100;
    } 
    on TIMEOUT goto SendPong;
  }

  state SendPong { 
    entry { 
      monitor M_PONG, client;
      send client, PONG; 
      raise SUCCESS; 
    } 
    on SUCCESS goto WaitPing; 
  }
} 

event M_PING: machine;
event M_PONG: machine;

spec Safety monitors M_PING, M_PONG { 
    var pending: map[machine, int];
    start state Init { 
        on M_PING do (payload: machine) { 
            if (!(payload in pending)) 
                pending[payload] = 0; 
            pending[payload] = pending[payload] + 1; 
            assert (pending[payload] <= 3); 
        }; 
        on M_PONG do (payload: machine) {
            assert (payload in pending); 
            assert (0 < pending[payload]); 
            pending[payload] = pending[payload] - 1;
        };
    }
} 

