event PING assert 1: int; 
event PONG assert 1: int; 
event SUCCESS: int;

machine Main {
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
				send server, PING, v move;
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
			send client, PONG, v move; 
		} 
		on PING goto SendPong; 
	}
} 
