//this example tests the round robin scheduler with sealing.
// the request and then ack should be executed using one deterministic schedule

event request;
event response;
event local;

main machine godMachine {
	var Sever:id;
	var Clients:(id,id,id);
	start state {
		entry {
			Server = new Server();
			Clients[0] = new Client(Server);
			Clients[1] = new Client(Server);
			Clients[2] = new Client(Server);
		}
	
	}
}