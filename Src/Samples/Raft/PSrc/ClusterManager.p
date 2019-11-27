// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

machine ClusterManager
{
	var Servers: seq[machine];
	var NumberOfServers: int;
	var Leader: machine;
	var LeaderTerm: int;
	var Client: machine;
	var Timer: machine;

	start state Init
	{
		entry
		{
			var idx: int;
			var mac: machine;
			NumberOfServers = 2;
			LeaderTerm = -1;
			idx = 0;
			Servers = default(seq[machine]);
			print "clustermanager";

			while(idx < NumberOfServers)
			{	
				mac = new Server();
				Servers += (idx, mac);
				idx = idx + 1;
			}
			print "made servers";
			

			Client = new Client();
			raise LocalEvent;
		}

		on LocalEvent goto Configuring;
		defer SentAllTicks;
	}

	state Configuring
	{
		entry
		{
			var idx: int;
			idx = 0;
			Timer = new WallclockTimer();
            send Timer, ConfigureWallclock, (Servers=Servers, ClusterManager=this);
			while(idx < NumberOfServers)
			{
				print "[ClusterManager | Configure] Configuring server {0}", idx;
				send Servers[idx], SConfigureEvent, (Id = idx, Servers = Servers, ClusterManager = this);
				idx = idx + 1;
			}
			send Client, CConfigureEvent, this;
			raise LocalEvent;
		}

		on LocalEvent goto Unavailable;
	}

	state Unavailable
	{
		entry {
			send Timer, StartTimer;
		}
		on NotifyLeaderUpdate do (payload: (Leader: machine, Term: int)) {
			UpdateLeader(payload);
        	raise LocalEvent;
		}
		on ShutDown do ShuttingDown;
		on LocalEvent goto Available;
		on SentAllTicks do {
			send Timer, TickEvent;
		}
		defer Request;
	}

	fun UpdateLeader(request: (Leader: machine, Term: int))
    {
        if (LeaderTerm < request.Term)
        {
            Leader = request.Leader;
            LeaderTerm = request.Term;
        }
    }

	fun ShuttingDown()
	{
		var idx: int;
		idx = 0;
		while (idx < NumberOfServers)
        {
            send Servers[idx], ShutDown;
        	idx = idx + 1;
        }

        raise halt;
	}

	state Available
	{
		on Request do (payload: (Client: machine, Command: int)){
			print "[ClusterManager] Request {0} sent from client {1}", payload.Command, payload.Client;
			send Leader, Request, (Client=payload.Client, Command=payload.Command);
		}
		on RedirectRequest do (payload : (Client: machine, Command: int)){
			send this, Request, payload;
			raise LocalEvent;
		}
		on NotifyLeaderUpdate do (payload: (Leader: machine, Term: int)){
			UpdateLeader(payload);
		}
		on ShutDown do ShuttingDown;
		on LocalEvent goto Unavailable;
		on SentAllTicks do {
			send Timer, TickEvent;
		}
	}

	fun BecomeUnavailable()
    {

    }
}
// }