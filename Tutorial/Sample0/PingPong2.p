event PING assert 1: IClient; 
event PONG assert 1; 
event SUCCESS;

type IClient() = { PONG };
type IServer() = { PING };

machine Client : IClient
receives PONG;
sends PING;
{
  var server: IServer;  
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
      send server, PING, this; 
    }
    on PONG goto SendPing; 
  }
}

machine Server : IServer
receives PING, TIMEOUT;
sends PONG, START, CANCEL;
{ 
  var timer1: TimerPtr;
  var timer2: TimerPtr;
  var client: IClient;

  start state Init {  
    entry { 
	    print "Server created\n";
	    timer1 = CreateTimer(this);
      timer2 = CreateTimer(this);
	    goto WaitPing;
    }
  }

  state WaitPing { 
    on PING goto Sleep; 
  }
  
  state Sleep { 
    entry (m: IClient) {       
      client =  m;
      StartTimer(timer1, 1000);
      StartTimer(timer2, 1000);
    } 
    on TIMEOUT goto WaitPing with (timerFired: TimerPtr) { 
      if (timerFired == timer1)
      {
        CancelTimerSafely(timer2);
      }
      else
      {
        CancelTimerSafely(timer1);
      }
	    print "Server sending PONG\n";
      send client, PONG; 
    }
  }

  fun CancelTimerSafely(timer: TimerPtr)
  {
    CancelTimer(timer);
    receive {
      case CANCEL_SUCCESS: {}
      case CANCEL_FAILURE: { receive { case TIMEOUT: {} } }
    }
  }
} 