
// PingPong.p 
event PING; 
event PONG; 
event SUCCESS;

main machine Client {
  start state Init { 
    entry { 
      raise SUCCESS; 
    } 
    on SUCCESS goto SendPing; 
  }
  state SendPing { 
    entry { 
      send this, PING; 
      raise SUCCESS; 
    } 
    on SUCCESS goto WaitPing; 
  }
  state WaitPing { 
    on PING goto SendPong; 
  }
  state SendPong {
    entry { 
      send this, PONG; 
      raise SUCCESS; 
    } 
    on SUCCESS goto WaitPong; 
  }
  state WaitPong { 
    on PONG goto SendPing; 
  }
}
