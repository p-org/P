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
	var CommunicateLeaderTimeout : machine;
	var BroadCastTimeout : machine;
	var iter : int;
	
	start state Init {
		entry {
			servers = (payload as (servers: seq[machine], parentServer:machine, rank : int)).servers;
			parentServer = (payload as (servers: seq[machine], parentServer:machine, rank : int)).parentServer;
			myRank = (payload as (servers: seq[machine], parentServer:machine, rank : int)).rank;
			currentLeader = (rank = myRank, server = this);
			CommunicateLeaderTimeout = new Timer((this, 100));
			BroadCastTimeout = new Timer((this, 10));
			raise(local);
		}
		on local goto ProcessPings;
	}
	
	fun calculateLeader() {
		if(payload.rank < myRank)
		{
			currentLeader = payload;
		}
	}
	fun BroadCast(ev : event, pd : any) {
		iter = 0;
		while(iter < sizeof(servers))
		{
			send servers[iter], ev, pd;
		}
	}
	state ProcessPings {
		entry {
				BroadCast(Ping, (rank = myRank, server = this));
				send BroadCastTimeout, startTimer;
		}
		on Ping do calculateLeader;
		on timeout goto ProcessPings with
		{
			if(payload.mymachine == CommunicateLeaderTimeout)
			{
				assert(currentLeader.rank <= myRank);
				send parentServer, newLeader, currentLeader;
				currentLeader = (rank = myRank, server = this);
				send CommunicateLeaderTimeout, startTimer;
				send BroadCastTimeout, cancelTimer;
			}
		};
	}
}

model Timer {
	var target: machine;
	var timeoutvalue : int;
	start state Init {
		entry {
			target = (payload as (machine, int)).0;
			timeoutvalue = (payload as (machine, int)).1;
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
			if ($) {
				send target, timeout, (mymachine = this,);
				raise(local);
			}
		}
		on local goto Loop;
		on cancelTimer goto Loop;
	}
}