/*
The client machine checks that for a configuration of 3 nodes 
An update(k,v) is followed by a successful query(k) == v
Also a random query is performed in the end.

*/

machine Client {
	var next : int;
	var headNode: machine;
	var tailNode:machine;
	var keyvalue: map[int, int];
	var success: bool;
	var startIn : int;
	start state Init {
		entry {
			next = 1;
			//new Update_Query_Seq(this);
			headNode = (payload as (head:machine, tail:machine, startIn:int)).head;
			tailNode = (payload as (head:machine, tail:machine, startIn:int)).tail;
			startIn = (payload as (head:machine, tail:machine, startIn:int)).startIn;
			keyvalue[1*startIn] = 100;
			keyvalue[2*startIn] = 200;
			keyvalue[3*startIn] = 300;
			keyvalue[4*startIn] = 400;
			success = true;
			raise(local);
		}
		on local goto PumpUpdateRequests;
	}
	
	state PumpUpdateRequests
	{
		ignore responsetoupdate;
		entry {
			send headNode, update, (client = this, kv = (key = next * startIn, value = keyvalue[next * startIn]));

			if(next >= 3) // only test for now
			{
				raise(done);
			}
			else
			{
				raise(local);
			}
		}
		on done goto PumpQueryRequests with
		{
			next = 1;
		};
		on local goto PumpUpdateRequests with 
		{
			next = next + 1;
		};
	}
	
	state end {
		entry {
			raise(halt);
		}
	}

	
	
	state PumpQueryRequests {
		ignore responsetoquery;
		entry {
			send tailNode, query, (client = this, key = next * startIn);
			if(next >= 3) // only test for now
			{
				raise(done);
			}
			else
			{
				raise(local);
			}
		}
		on local goto PumpQueryRequests with
		{
			next = next + 1;
		};
		
		on done goto end;
	}
	
}

main machine TheGodMachine {
	var servers : seq[machine];
	var clients : seq[machine];
	var temp : machine;
	start state Init {
		entry {
			
			temp = new ChainReplicationServer((isHead = false, isTail = true, smid = 3));
			servers += (0, temp);
			temp = new ChainReplicationServer((isHead = false, isTail = false, smid = 2));
			servers += (0, temp);
			temp = new ChainReplicationServer((isHead = true, isTail = false, smid = 1));
			servers += (0, temp);
			
			//Global Monitor
			new Update_Propagation_Invariant(servers);
			new UpdateResponse_QueryResponse_Seq(servers);
			
			send servers[2], predSucc, (pred = servers[1], succ = servers[2]) ;
			send servers[1], predSucc, (pred = servers[0], succ = servers[2]);
			send servers[0], predSucc, (pred = servers[0], succ = servers[1]);
			//create the client and start the game
			temp = new Client((head = servers[0], tail = servers[2], startIn = 1));
			clients += ( 0, temp);
			temp = new Client((head = servers[0], tail = servers[2], startIn = 100));
			clients += ( 0, temp);
			
			new ChainReplicationMaster((clients = clients, servers = servers));
			raise(halt);
		}
	}

}
