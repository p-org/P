/*
The leader election protocol for multi-paxos, the protocol is based on broadcast based approach. 

*/
event Ping : (rank:int, server : id) assume 4;
event newLeader : (rank:int, server : id);
event timeout : (myId : mid);
event startTimer;
event cancelTimer;
event cancelTimerSuccess;

machine LeaderElection {
	var servers : seq[id];
	var parentServer : id;
	var currentLeader : (rank:int, server : id);
	var myRank : int;
	var iter : int;
	
	start state Init {
		entry {
			servers = ((servers: seq[id], parentServer:id, rank : int)) payload.servers;
			parentServer = ((servers: seq[id], parentServer:id, rank : int))payload.parentServer;
			myRank = ((servers: seq[id], parentServer:id, rank : int))payload.rank;
			currentLeader = (rank = myRank, server = this);
			raise(local);
		}
		on local goto SendLeader;
		
	}
	
	state SendLeader {
		entry {
			currentLeader = GetNewLeader();
			assert(currentLeader.rank <= myRank);
			send(parentServer, newLeader, currentLeader);
		}
	}
	model fun GetNewLeader() : (rank:int, server : id) {
			/*iter = 0;
			while(iter < sizeof(servers))
			{
				if((iter + 1) < myRank) {
					if(*)
					{
						return (rank = iter + 1, server = servers[iter]);
					}
				}
				
				iter = iter + 1;	
			}
			return (rank = myRank, server = parentServer);*/
			
			return (rank = 1, server = servers[0]);
		}

}

model machine Timer {
	var target: id;
	var timeoutvalue : int;
	start state Init {
		entry {
			target = ((id, int))payload[0];
			timeoutvalue = ((id, int))payload[1];
			raise(local);
		}
		on local goto Loop;
	}

	state Loop {
		ignore cancelTimer;
		on startTimer goto TimerStarted;
	}

	state TimerStarted {
		ignore startTimer;
		entry {
			if (*) {
				//send(target, timeout, (myId = this));
				raise(local);
			}
		}
		on local goto Loop;
		on cancelTimer goto Loop;
	}
}