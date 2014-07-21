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
			new Update_Query_Seq(this);
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
			if(next >= 2) // only test for now
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
		
		on local goto PumpRequests
		{
			next = next + 1;
		};
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
	
	action checkReturn {
			if(success)
			{
				assert(keyvalue[(next - 1)* startIn] == ((client:id, value:int))payload.value);
			}
			else
			{
				assert(((client:id, value:int))payload.value == -1);
			}
			return;
	}
	state RandomQuery {
		entry {
			QueryNonDet();
		}
		
		on responsetoquery do checkReturn;
		
	}	
}

main machine TheGodMachine {
	var servers : (one:id, two:id, three:id);
	start state Init {
		entry {
			//Global Monitor
			new Update_Propagation_Invariant();
			new UpdateResponse_QueryResponse_Seq();
			
			
			servers.three = new ChainReplicationServer((isHead = false, isTail = true, smId = 3));
			servers.two = new ChainReplicationServer((isHead = false, isTail = false, smId = 2));
			servers.one = new ChainReplicationServer((isHead = true, isTail = false, smId = 1));
			send(servers.three, predSucc, (pred = servers.two, succ = servers.three));
			send(servers.two, predSucc, (pred = servers.one, succ = servers.three));
			send(servers.one, predSucc, (pred = servers.one, succ = servers.two));
			//create the client and start the game
			new Client((head = servers.one, tail = servers.three, startIn = 1));
			new Client((head = servers.one, tail = servers.three, startIn = 100));
			raise(delete);
		}
	}

}
