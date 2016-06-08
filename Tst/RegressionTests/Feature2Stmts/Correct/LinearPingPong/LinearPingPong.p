event PING assert 1: int; 
event PONG assert 1: int; 
event SUCCESS: int;

main machine Client {
	var server: machine;
	var count: int;
	start state Init { 
		entry { 
			count = 0;
			server = new Server(this); 
			raise SUCCESS, 0; 
		} 
		on SUCCESS goto SendPing; 
	}
  
	state SendPing { 
		entry (v: int) { 
			if (count < 10) {
				xfer send server, PING, v;
				count = count + 1;
			}
		} 
		on PONG goto SendPing; 
	}
}

machine Server { 
	var client: machine;

	start state Init {  
		entry (x: machine) {
			client = x; 
		}
		on PING goto SendPong; 
	}
  
	state SendPong { 
		entry (v: int) { 
			xfer send client, PONG, v; 
		} 
		on PING goto SendPong; 
	}
} 
