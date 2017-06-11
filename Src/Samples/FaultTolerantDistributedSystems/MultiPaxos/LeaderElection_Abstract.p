/*
The leader election protocol for multi-paxos, the protocol is based on broadcast based approach. 

*/
event Ping  assume 4 : (rank:int, server : machine);
event newLeader : (rank:int, server : machine);
event timeout : (mymachine : machine);
event startTimer;
event cancelTimer;
event cancelTimerSuccess;

machine LeaderElection {
	var servers : seq[machine];
	var parentServer : machine;
	var currentLeader : (rank:int, server : machine);
	var myRank : int;
	var iter : int;
	
	start state Init {
		entry {
			servers = (payload as (servers: seq[machine], parentServer:machine, rank : int)).servers;
			parentServer = (payload as (servers: seq[machine], parentServer:machine, rank : int)).parentServer;
			myRank = (payload as (servers: seq[machine], parentServer:machine, rank : int)).rank;
			currentLeader = (rank = myRank, server = this);
			raise(local);
		}
		on local goto SendLeader;
		
	}
	
	state SendLeader {
		entry {
			currentLeader = GetNewLeader();
			assert(currentLeader.rank <= myRank);
			send parentServer, newLeader, currentLeader;
		}
	}
	
	model fun GetNewLeader() : (rank:int, server : machine) {
			return (rank = 1, server = servers[0]);
	}

}