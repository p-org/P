/*
The client machine checks that for a configuration of 3 nodes 
An update(k,v) is followed by a successful query(k) == v
Also a random query is performed in the end.

*/

machine Client {
	var next : int;
	var headNode: id;
	var tailNode:id;
	var keyvalue: map[int, int];
	var success: bool;
	var startIn : int;
	start state Init {
		entry {
			next = 1;
			headNode = ((head:id, tail:id, startIn:int))payload.head;
			tailNode = ((head:id, tail:id, startIn:int))payload.tail;
			startIn = ((head:id, tail:id, startIn:int))payload.startIn;
			keyvalue.update(1*startIn,100);
			keyvalue.update(2*startIn,200);
			keyvalue.update(3*startIn,300);
			keyvalue.update(4*startIn,400);
			success = true;
			raise(local);
		}
		on local goto PumpUpdateRequests;
	}
	
	state PumpUpdateRequests
	{
		ignore responsetoupdate;
		entry {
			send(headNode, update, (client = this, kv = (key = next * startIn, value = keyvalue[next * startIn])));

			if(next >= 3) // only test for now
			{
				raise(done);
			}
			else
			{
				raise(local);
			}
		}
		on done goto PumpQueryRequests
		{
			next = 1;
		};
		on local goto PumpUpdateRequests
		{
			next = next + 1;
		};
	}
	
	state end {
		entry {
			raise(delete);
		}
	}

	
	
	state PumpQueryRequests {
		ignore responsetoquery;
		entry {
			send(tailNode, query, (client = this, key = next * startIn));
			if(next >= 3) // only test for now
			{
				raise(done);
			}
			else
			{
				raise(local);
			}
		}
		on local goto PumpQueryRequests
		{
			next = next + 1;
		};
		
		on done goto end;
	}
	
}

main machine TheGodMachine {
	var servers : seq[id];
	var clients : seq[id];
	var temp : id;
	start state Init {
		entry {
			//Global Monitor
			new Update_Propagation_Invariant();
			new UpdateResponse_QueryResponse_Seq();
			
			
			temp = new ChainReplicationServer((isHead = false, isTail = true, smId = 3));
			servers.insert(0, temp);
			temp = new ChainReplicationServer((isHead = false, isTail = false, smId = 2));
			servers.insert(0, temp);
			temp = new ChainReplicationServer((isHead = true, isTail = false, smId = 1));
			servers.insert(0, temp);
			send(servers[2], predSucc, (pred = servers[1], succ = servers[2]));
			send(servers[1], predSucc, (pred = servers[0], succ = servers[2]));
			send(servers[0], predSucc, (pred = servers[0], succ = servers[1]));
			//create the client and start the game
			temp = new Client((head = servers[0], tail = servers[2], startIn = 1));
			clients.insert( 0, temp);
			temp = new Client((head = servers[0], tail = servers[2], startIn = 100));
			clients.insert( 0, temp);
			
			new ChainReplicationMaster((servers = servers, clients = clients));
			raise(delete);
		}
	}

}
