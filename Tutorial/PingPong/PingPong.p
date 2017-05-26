event PING assert 1: IClient; 
event PONG assert 1; 
event SUCCESS;

type IClient(int) = { PONG };
type IServer() = { PING };

machine Client : IClient
receives PONG;
sends PING;
{
  var server: IServer;
  var numIterations: int;  

  start state Init {  
    entry (n: int) { 	
	    print "Client created\n";
      numIterations = n;
      server = new IServer(); 
      raise SUCCESS; 
    } 
    on SUCCESS goto SendPing; 
  }

  state SendPing { 
    entry { 
      if (numIterations == 0) {
        goto Stop;
      } else if (numIterations > 0) {
        numIterations = numIterations - 1;
      }
	    print "Client sending PING\n";
      send server, PING, this; 
    }
    on PONG goto SendPing; 
  }

  state Stop {
    entry {
      StopProgram();
    }
  }

  model fun StopProgram()
  {
    
  }
}

machine Server : IServer
receives PING, TIMEOUT;
sends PONG, START;
{ 
  var timer: TimerPtr;
  var client: IClient;

  start state Init {  
    entry { 
	    print "Server created\n";
      timer = CreateTimer(this);
	    goto WaitPing;
    }
  }

  state WaitPing { 
    on PING goto Sleep; 
  }
  
  state Sleep { 
    entry (m: IClient) {       
      client =  m;
      StartTimer(timer, 1000);
    } 
    on TIMEOUT goto WaitPing with { 
	    print "Server sending PONG\n";
      send client, PONG; 
    }
  }
} 