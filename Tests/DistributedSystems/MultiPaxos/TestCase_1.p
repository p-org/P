


main machine GodMachine {
	var paxosnodes : seq[id];
	var temp : id;
	var iter : int;
	start state Init {
		entry {
			temp = new PaxosNode((rank = 3));
			paxosnodes.insert(0, temp);
			temp = new PaxosNode((rank = 2));
			paxosnodes.insert(0, temp);
			temp = new PaxosNode((rank = 1));
			paxosnodes.insert(0, temp);
			//send all nodes the other machines
			iter = 0;
			while(iter < sizeof(paxosnodes))
			{
				send(paxosnodes[iter], allNodes, (nodes = paxosnodes));
				iter = iter + 1;
			}
			//create the client nodes
			new Client(paxosnodes);
		}
	}
}

model machine Client {
	var servers :seq[id];
	start state Init {
		entry {
			new ValidityCheck();
			servers = (seq[id])payload;
			raise(local);
		}
		on local goto PumpOneRequest;
	}
	
	state PumpOneRequest {
		entry {
			
			invoke ValidityCheck(monitor_client_sent, 1);
			if(*)
				send(servers[0], update, (seqId  = 0, command = 1));
			else
				send(servers[sizeof(servers) - 1], update, (seqId  = 0, command = 1));
		}
	}

}