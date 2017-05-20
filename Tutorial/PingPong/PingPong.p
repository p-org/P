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

