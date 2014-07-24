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
			//new Update_Query_Seq(this);
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
		on local goto PumpRequests;
	}
	
	state PumpRequests
	{
		entry {
			call(Update_Response);
			call(Query_Response);
			if(next >= 1) // only test for now
			{
				next = next + 1;
				call(RandomQuery);
				raise(done);
			}
			else
			{
				raise(local);
			}
		}
		on done goto end;
		on updateHeadTail do updateHeadTailAction;
		on local goto PumpRequests
		{
			next = next + 1;
		};
	}
	
	action updateHeadTailAction {
		headNode = payload.head;
		tailNode = payload.tail;
	}
	
	state end {
		entry {
			raise(delete);
		}
	}
	action Return {
		if(trigger == responsetoquery)
		{
			invoke Update_Query_Seq(responsetoquery, payload);
			
		}
		return;
	}
	
	state Update_Response {
		entry {
			send(headNode, update, (client = this, kv = (key = next * startIn, value = keyvalue[next * startIn])));
			invoke Update_Query_Seq(update, (client = this, kv = (key = next * startIn, value = keyvalue[next * startIn])))
		}
		on responsetoupdate do Return;
	}
	
	state Query_Response {
		entry {
			send(tailNode, query, (client = this, key = next * startIn));
		}
		on responsetoquery do Return;
	}
	
	model fun QueryNonDet () {
		//can query any item between 1 to nextSeqId
		if(*)
		{
			send(tailNode, query, (client = this, key = (next - 1)* startIn));
			success = true;
		}
		else
		{
			send(tailNode, query, (client = this, key = (next + 1) * startIn));
			success = false;
		}
	}
	
	state RandomQuery {
		entry {
			QueryNonDet();
		}
		
		on responsetoquery do Return;
		
	}	
}

main machine TheGodMachine {
	var servers : seq[id];
	var clients : seq[id];
	var temp : id;
	start state Init {
		entry {
			
			temp = new ChainReplicationServer((isHead = false, isTail = true, smId = 4));
			servers.insert(0, temp);
			temp = new ChainReplicationServer((isHead = false, isTail = false, smId = 3));
			servers.insert(0, temp);
			temp = new ChainReplicationServer((isHead = false, isTail = false, smId = 2));
			servers.insert(0, temp);
			temp = new ChainReplicationServer((isHead = true, isTail = false, smId = 1));
			servers.insert(0, temp);
			
			//Global Monitor
			new Update_Propagation_Invariant(servers);
			new UpdateResponse_QueryResponse_Seq(servers);
			
			send(servers[3], predSucc, (pred = servers[2], succ = servers[3]));
			send(servers[2], predSucc, (pred = servers[1], succ = servers[3]));
			send(servers[1], predSucc, (pred = servers[0], succ = servers[2]));
			send(servers[0], predSucc, (pred = servers[0], succ = servers[1]));
			//create the client and start the game
			temp = new Client((head = servers[0], tail = servers[3], startIn = 1));
			clients.insert( 0, temp);
			temp = new Client((head = servers[0], tail = servers[3], startIn = 100));
			clients.insert( 0, temp);
			
			new ChainReplicationMaster((servers = servers, clients = clients));
			raise(delete);
		}
	}

}
