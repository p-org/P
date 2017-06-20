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
		on local goto PumpRequests;
	}
	
	state PumpRequests
	{
		entry {
			push Update_Response;
			push Query_Response;
			if(next >= 2) // only test for now
			{
				next = next + 1;
				push RandomQuery;
				raise(done);
			}
			else
			{
				raise(local);
			}
		}
		on done goto end;
		on updateHeadTail do updateHeadTailAction;
		on local goto PumpRequests with 
		{
			next = next + 1;
		};
	}
	
	fun  updateHeadTailAction() {
		headNode = payload.head;
		tailNode = payload.tail;
	}
	
	state end {
		entry {
			raise(halt);
		}
	}
	
	fun Return() {
		if(trigger == responsetoquery)
		{
			monitor Update_Query_Seq, responsetoquery, payload as (client: machine, value : int);
			
		}
		pop;
	}
	
	state Update_Response {
		entry {
			send headNode, update, (client = this, kv = (key = next * startIn, value = keyvalue[next * startIn]));
			monitor Update_Query_Seq, update, (client = this, kv = (key = next * startIn, value = keyvalue[next * startIn]));
		}
		on responsetoupdate do Return;
	}
	
	state Query_Response {
		entry {
			send tailNode, query, (client = this, key = next * startIn);
		}
		on responsetoquery do Return;
	}
	
	model fun QueryNonDet () {
		//can query any item between 1 to nextSeqmachine
		if($)
		{
			send tailNode, query, (client = this, key = (next - 1)* startIn);
			success = true;
		}
		else
		{
			send tailNode, query, (client = this, key = (next + 1) * startIn);
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
	var servers : seq[machine];
	var clients : seq[machine];
	var temp : machine;
	start state Init {
		entry {

			temp = new ChainReplicationServer((isHead = true, isTail = true, smid = 1));
			servers += (0, temp);
			
			//Global Monitor for Safety
			new Update_Propagation_Invariant(servers);
			new UpdateResponse_QueryResponse_Seq(servers);
			
			
			send servers[0], predSucc, (pred = servers[0], succ = servers[0]);
			//create the client and start the game
			temp = new Client((head = servers[0], tail = servers[0], startIn = 1));
			clients += ( 0, temp);
			temp = new Client((head = servers[0], tail = servers[0], startIn = 100));
			clients += ( 0, temp);
			
			new ChainReplicationMaster((clients = clients, servers = servers));
			raise(halt);
		}
	}

}