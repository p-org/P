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
    ignore TIMEOUT;
    /*
    on TIMEOUT goto WaitPing with { 
	  print "Server sending PONG\n";
      send client, PONG; 
    }
    */
  }
} 