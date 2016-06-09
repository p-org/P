
// PingPong.p 
event PING: seq[int]; 
event PONG: seq[int]; 
event SUCCESS;

main machine Client {
  var server: machine;
  var arg: seq[int];

  start state Init { 
    entry { 
	  var i:int;
	  server = new Server(this);
	  while (i < 10)
	  {
	     arg += (i,0);
		 i = i + 1;
	  }
      raise SUCCESS; 
    } 
    on SUCCESS goto SendPing; 
  }
  state SendPing { 
    entry { 
      send server, PING, arg; 
      raise SUCCESS; 
    } 
    on SUCCESS goto WaitPong; 
  }
  state WaitPong { 
    on PONG goto SendPing; 
  }
}

machine Server
{
  var client: machine;

  start state Init { 
    entry (payload:machine) { 
	  client = payload;
      raise SUCCESS; 
    } 
    on SUCCESS goto WaitPing; 
  }
  state WaitPing { 
    on PING goto SendPong; 
  }
  state SendPong {
    entry (payload:seq[int])
	{
      send client, PONG, payload; 
      raise SUCCESS; 
    } 
    on SUCCESS goto WaitPing; 
  }
}
