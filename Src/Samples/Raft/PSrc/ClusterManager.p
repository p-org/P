// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;

// namespace Raft
// {
machine ClusterManager
{
	//List<machine> Servers;
    //int NumberOfServers;
    //machine Leader;
    //int LeaderTerm;
    //machine Client;
	var Servers: seq[machine];
	var NumberOfServers: int;
	var Leader: machine;
	var LeaderTerm: int;
	var Client: machine;

	start state Init
	{
		entry
		{
			var idx: int;
			var mac: machine;
			NumberOfServers = 5;
			LeaderTerm = 0;
			idx = 0;
			Servers = default(seq[machine]);

			while(idx < NumberOfServers)
			{	
				mac = default(machine);
				Servers += (idx, mac);
				idx = idx + 1;
			}

			Client = default(machine);
			raise LocalEvent;
		}

		on LocalEvent goto Configuring;
	}

	state Configuring
	{
		entry
		{
			var idx: int;
			idx = 0;
			while(idx < NumberOfServers)
			{
				send Servers[idx], SConfigureEvent, (Id=idx, Servers=Servers, ClusterManager=this);
				idx = idx + 1;
			}

			send Client, CConfigureEvent, this;
			raise LocalEvent;
		}

		on LocalEvent goto Unavailable;
	}

	state Unavailable
	{
		on NotifyLeaderUpdate do (payload: (Leader: machine, Term: int)) {
			UpdateLeader(payload);
        	raise LocalEvent;
		}
		on ShutDown do ShuttingDown;
		on LocalEvent goto Available;
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
			send Leader, Request, (Client=payload.Client, Command=payload.Command);
		}

		//TODO: How to address payloads that are events themselves
		on RedirectRequest do (payload : (Client: machine, Command: int)){
			send this, Request, payload;
		}
		on NotifyLeaderUpdate do (payload: (Leader: machine, Term: int)){
			UpdateLeader(payload);
		}
		on ShutDown do ShuttingDown;
		on LocalEvent goto Unavailable;
	}

	fun BecomeUnavailable()
    {

    }
}
// }