//this example tests the round robin scheduler with sealing.
// the request and then ack should be executed using one deterministic schedule

event request;
event response;
event local;
event c_init : id;
main machine godMachine {
	var S:id;
	var temp :id;
	start state init{
		entry {
			S = new Server();
			temp  = new Client(S);
			temp  = new Client(S);
			temp  = new Client(S);
		}
	
	}
}

machine Server {
	var Clients:(id,id,id);
	var count:int;
	action clientUpdate {
		if(count == 0){
		Clients[0] = (id) payload;
		}
		if(count == 1){
		Clients[1] = (id) payload;
		}
		if(count == 2){
		Clients[2] = (id) payload;
		raise(local);
		}
		count = count + 1;
	}
	start state init {
		entry {
			count = 0;
			Clients = (this, this, this);
		}
		on c_init do clientUpdate;
		on local goto dostuff;
	}
	
	state dostuff {
		ignore response;
		entry {
			count = 0;
			__seal();

				send(Clients[0], request);
				send(Clients[1], request);
				send(Clients[2], request);

			__unseal();
			raise(local);
		}
		on local goto done;
		
	}
	
	state done {
		entry {
			assert(false);
		}
	}
}

machine Client {
	var server : id;
	var myid : int; 
	start state init {
		entry {
			server = (id) payload;
			__seal();
			send(server, c_init, this);
			__unseal();
			raise(local);
		}
		on local goto wait;
		
	}
	
	state wait {
		entry {
			
		}
		on request do sendresp;
	}
	
	action sendresp {
		send(server, response);
	}
}