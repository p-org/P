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

machine Server
{
    var ServerId : int;
    var ClusterManager : machine;
    var Servers: seq[machine];
    var LeaderId: machine;
    var ElectionTimer: machine;
    var PeriodicTimer: machine;
    var CurrentTerm: int;
    var VotedFor: machine;
    var Logs: seq[Log];
    var CommitIndex: int;
    var LastApplied: int;
    var NextIndex: map[machine, int];
    var MatchIndex: map[machine, int];
    var VotesReceived: int;
    var LastClientRequest: (Client: machine, Command: int);
    var i: int;

    start state Init
    {
        entry
        {
            i = 0;
            CurrentTerm = 0;
            LeaderId = default(machine);
            VotedFor = default(machine);
            Logs = default(seq[Log]);
            CommitIndex = 0;
            LastApplied = 0;
            NextIndex = default(map[machine, int]);
            MatchIndex = default(map[machine, int]);
        }

        on SConfigureEvent do (payload: (Id: int, Servers: seq[machine], ClusterManager: machine)) {
            ServerId = payload.Id;
            Servers = payload.Servers;
            ClusterManager = payload.ClusterManager;

            ElectionTimer = default(machine);
            send ElectionTimer, EConfigureEvent, this;

            PeriodicTimer = default(machine);
            send PeriodicTimer, PConfigureEvent, this;

            raise BecomeFollower;
        }
        on BecomeFollower goto Follower;
        defer VoteRequest, AppendEntriesRequest;
    }

    state Follower
    {
        entry
        {
            LeaderId = default(machine);
            VotesReceived = 0;

            send ElectionTimer, EStartTimer;
        }

        on Request do (payload: (Client: machine, Command: int)) {
            if (LeaderId != null)
            {
                send LeaderId, Request, payload.Client, payload.Command;
            }
            else
            {
                send ClusterManager, RedirectRequest, payload;
            }
        }
        on VoteRequest do (payload: (Term: int, CandidateId: machine, LastLogIndex: int, LastLogTerm: int)) {
            if (payload.Term > CurrentTerm)
            {
                CurrentTerm = payload.Term;
                VotedFor = default(machine);
            }

            Vote(payload);
        }
        on VoteResponse do (request: (Term: int, VoteGranted: bool)) {
            if (request.Term > CurrentTerm)
            {
                CurrentTerm = request.Term;
                VotedFor = default(machine);
            }
        }
        on AppendEntriesRequest do (request: (Term: int, LeaderId: machine, PrevLogIndex: int, 
            PrevLogTerm: int, Entries: seq[Log], LeaderCommit: int, ReceiverEndpoint: machine)){
            if (request.Term > CurrentTerm)
            {
                CurrentTerm = request.Term;
                VotedFor = default(machine);
            }

            AppendEntries(request);
        }
        on AppendEntriesResponse do (request: (Term: int, Success: bool, Server: machine,
         ReceiverEndpoint: machine)){
            if (request.Term > CurrentTerm)
            {
                CurrentTerm = request.Term;
                VotedFor = default(machine);
            }
        }
        on ETimeout do {
            raise BecomeCandidate;
        }
        on ShutDown do { 
            ShuttingDown();
        }
        on BecomeFollower goto Follower;
        on BecomeCandidate goto Candidate;
        ignore PTimeout;
    }


    state Candidate
    {
        entry
        {
            CurrentTerm = CurrentTerm + 1;
            VotedFor = this;
            VotesReceived = 1;

            send ElectionTimer, EStartTimer;

            //Logger.WriteLine("\n [Candidate] " + this.ServerId + " | term " + this.CurrentTerm + " | election votes " + this.VotesReceived + " | log " + this.Logs.Count + "\n");

            BroadcastVoteRequests();
        }

        on Request do (payload: (Client: machine, Command: int)) {
            if (LeaderId != null)
            {
                send LeaderId, Request, payload.Client, payload.Command;
            }
            else
            {
                send ClusterManager, RedirectRequest, payload;
            }
        }
        on VoteRequest do (request: (Term: int, CandidateId: machine, LastLogIndex: int, LastLogTerm: int)){
            if (request.Term > CurrentTerm)
            {
                CurrentTerm = request.Term;
                VotedFor = default(machine);
                Vote(request);
                raise BecomeFollower;
            }
            else
            {
                Vote(request);
            }
        }
        on VoteResponse do (request: (Term: int, VoteGranted: bool)) {
            if (request.Term > CurrentTerm)
            {
                CurrentTerm = request.Term;
                VotedFor = default(machine);
                raise BecomeFollower;
            }
            else if (request.Term != CurrentTerm)
            {
            }

            else if (request.VoteGranted)
            {
                VotesReceived = VotesReceived + 1;
                if (VotesReceived >= (sizeof(Servers) / 2) + 1)
                {
                   // this.Logger.WriteLine("\n [Leader] " + this.ServerId + " | term " + this.CurrentTerm +
                    //    " | election votes " + this.VotesReceived + " | log " + this.Logs.Count + "\n");
                    VotesReceived = 0;
                    raise BecomeLeader;
                }
            }
        }
        on AppendEntriesRequest do (request: (Term: int, LeaderId: machine, PrevLogIndex: int, PrevLogTerm: int,
         Entries: seq[Log], LeaderCommit: int, ReceiverEndpoint: machine)) {
            if (request.Term > CurrentTerm)
            {
                CurrentTerm = request.Term;
                VotedFor = default(machine);
                AppendEntries(request);
                raise BecomeFollower;
            }
            else
            {
                AppendEntries(request);
            }
        }
        on AppendEntriesResponse do (request: (Term: int, Success: bool, Server: machine, ReceiverEndpoint: machine)) {
            RespondAppendEntriesAsCandidate(request);
        }
        on ETimeout do {
            raise BecomeCandidate;
        }
        on PTimeout do BroadcastVoteRequests;
        on ShutDown do ShuttingDown;
        on BecomeLeader goto Leader;
        on BecomeFollower goto Follower;
        on BecomeCandidate goto Candidate;
    }

    fun BroadcastVoteRequests()
    {
        // BUG: duplicate votes from same follower
        var idx: int;
        var lastLogIndex: int;
        var lastLogTerm: int; 

        send PeriodicTimer, PStartTimer;
        idx = 0;
        while (idx < sizeof(Servers)) {
           if (idx == ServerId) {
                continue;
           }
            lastLogIndex = sizeof(Logs);
            lastLogTerm = GetLogTermForIndex(lastLogIndex);

            send Servers[idx], VoteRequest, (Term=CurrentTerm, CandidateId=this, LastLogIndex=lastLogIndex, LastLogTerm=lastLogTerm);
            idx = idx + 1;
        }
    }

    fun RespondAppendEntriesAsCandidate(request: (Term: int, Success: bool, Server: machine, ReceiverEndpoint: machine))
    {
        if (request.Term > CurrentTerm)
        {
            CurrentTerm = request.Term;
            VotedFor = default(machine);
            raise BecomeFollower;
        }
    }

    state Leader
    {
        entry
        {
            var logIndex: int;
            var logTerm: int;
            var idx: int;

            announce EMonitorInit, (NotifyLeaderElected, CurrentTerm);
            //monitor<SafetyMonitor>(NotifyLeaderElected, CurrentTerm);
            send ClusterManager, NotifyLeaderUpdate, this, CurrentTerm;

            logIndex = sizeof(Logs);
            logTerm = GetLogTermForIndex(logIndex);

            //this.NextIndex.Clear();
            //this.MatchIndex.Clear();
            NextIndex = default(map[machine, int]);
            MatchIndex = default(map[machine, int]);
            
            idx = 0;
            while (idx < sizeof(Servers))
            {
                if (idx == ServerId)
                    continue;
                NextIndex[Servers[idx]] = logIndex + 1;
                MatchIndex[Servers[idx]] = 0;
                idx = idx + 1;
            }

            idx = 0;
            while (idx < sizeof(Servers))
            {
                if (idx == ServerId)
                    continue;
                send Servers[idx], AppendEntriesRequest, 
                    (Term=CurrentTerm, LeaderId=this, PrevLogIndex=logIndex, PrevLogTerm=logTerm, Entries=default(seq[Log]), LeaderCommit=CommitIndex, ReceiverEndpoint=default(machine));
            }
        }

        on Request do (request: (Client: machine, Command: int)) {
            ProcessClientRequest(request);
        }
        on VoteRequest do (request: (Term: int, CandidateId: machine, LastLogIndex: int, LastLogTerm: int)) {
            VoteAsLeader(request);
        }
        on VoteResponse do (request: (Term: int, VoteGranted: bool)) {
            RespondVoteAsLeader(request);
        }
        on AppendEntriesRequest do (request: (Term: int, LeaderId: machine, PrevLogIndex: int, 
            PrevLogTerm: int, Entries: seq[Log], LeaderCommit: int, ReceiverEndpoint: machine)) {
            AppendEntriesAsLeader(request);
        }
        on AppendEntriesResponse do (request: (Term: int, Success: bool, Server: machine, ReceiverEndpoint: machine)) {
            RespondAppendEntriesAsLeader(request);
        }
        on ShutDown do ShuttingDown;
        on BecomeFollower goto Follower;
        ignore ETimeout, PTimeout;
    }

    fun ProcessClientRequest(trigger: (Client: machine, Command: int))
    {
        var log: Log;

        LastClientRequest = trigger;
        log = default(Log);
        log.Term = CurrentTerm;
        log.Command = LastClientRequest.Command;
        Logs += (i, log);
        i = i + 1;

        BroadcastLastClientRequest();
    }

    fun BroadcastLastClientRequest()
    {
        //this.Logger.WriteLine("\n [Leader] " + this.ServerId + " sends append requests | term " +
            //this.CurrentTerm + " | log " + this.Logs.Count + "\n");
        var lastLogIndex: int;
        var idx: int;
        var prevLogIndex: int;
        var prevLogTerm: int;
        var server: machine;
        var logsAppend: seq[Log];

        lastLogIndex = sizeof(Logs);
        VotesReceived = 1;
        while (idx < sizeof(Servers))
        {
            if (idx == ServerId)
                continue;
            server = Servers[idx];
            if (lastLogIndex < NextIndex[server])
                continue;

           // List<Log> logs = this.Logs.GetRange(this.NextIndex[server] - 1,
              //  this.Logs.Count - (this.NextIndex[server] - 1));
            logsAppend = default(seq[Log]);

            idx = NextIndex[server] - 1;
            while (idx < sizeof(Logs)) {
                logsAppend += (idx, Logs[idx]);
                idx = idx + 1;
            }

            prevLogIndex = NextIndex[server] - 1;
            prevLogTerm = GetLogTermForIndex(prevLogIndex);

            send server, AppendEntriesRequest, (Term=CurrentTerm, LeaderId=this, PrevLogIndex=prevLogIndex,
                PrevLogTerm=prevLogTerm, Entries=Logs, LeaderCommit=CommitIndex, ReceiverEndpoint=LastClientRequest.Client);
        }
    }

    fun VoteAsLeader(request: (Term: int, CandidateId: machine, LastLogIndex: int, LastLogTerm: int))
    {
        if (request.Term > CurrentTerm)
        {
            CurrentTerm = request.Term;
            VotedFor = default(machine);

            RedirectLastClientRequestToClusterManager();
            Vote(request);

            raise BecomeFollower;
        }
        else
        {
            Vote(request);
        }
    }

    fun RespondVoteAsLeader(request: (Term: int, VoteGranted: bool))
    {
        if (request.Term > CurrentTerm)
        {
            CurrentTerm = request.Term;
            VotedFor = default(machine);

            RedirectLastClientRequestToClusterManager();
            raise BecomeFollower;
        }
    }

    fun AppendEntriesAsLeader(request: (Term: int, LeaderId: machine, PrevLogIndex: int, PrevLogTerm: int, Entries: seq[Log], LeaderCommit: int, ReceiverEndpoint: machine))
    {
        if (request.Term > CurrentTerm)
        {
            CurrentTerm = request.Term;
            VotedFor = default(machine);

            RedirectLastClientRequestToClusterManager();
            AppendEntries(request);

            raise BecomeFollower;
        }
    }

    fun RespondAppendEntriesAsLeader(request: (Term: int, Success: bool, Server: machine, ReceiverEndpoint: machine))
    {
        var commitIndex: int;
        var logsAppend: seq[Log];
        var prevLogIndex: int;
        var prevLogTerm: int; 
        var idx: int;
        if (request.Term > CurrentTerm)
        {
            CurrentTerm = request.Term;
            VotedFor = default(machine);

            RedirectLastClientRequestToClusterManager();
            raise BecomeFollower;
        }
        else if (request.Term != CurrentTerm)
        {
        }

        else if (request.Success)
        {
            NextIndex[request.Server] = sizeof(Logs) + 1;
            MatchIndex[request.Server] = sizeof(Logs);

            VotesReceived = VotesReceived + 1;
            if (request.ReceiverEndpoint != null &&
                VotesReceived >= (sizeof(Servers) / 2) + 1)
            {
                //this.Logger.WriteLine("\n [Leader] " + this.ServerId + " | term " + this.CurrentTerm +
                  //  " | append votes " + this.VotesReceived + " | append success\n");

                commitIndex = MatchIndex[request.Server];
                if (commitIndex > CommitIndex &&
                    Logs[commitIndex - 1].Term == CurrentTerm)
                {
                    CommitIndex = commitIndex;

                   // this.Logger.WriteLine("\n [Leader] " + this.ServerId + " | term " + this.CurrentTerm + " | log " + this.Logs.Count + " | command " + this.Logs[commitIndex - 1].Command + "\n");
                }

                VotesReceived = 0;
                LastClientRequest = (Client=default(machine), Command=default(int));

                send request.ReceiverEndpoint, Response;
            }
        }
        else
        {
            if (NextIndex[request.Server] > 1)
            {
                NextIndex[request.Server] = NextIndex[request.Server] - 1;
            }

//            List<Log> logs = this.Logs.GetRange(this.NextIndex[request.Server] - 1, this.Logs.Count - (this.NextIndex[request.Server] - 1));
            logsAppend = default(seq[Log]);
            idx = NextIndex[request.Server] - 1;
            while (idx < sizeof(Logs)) {
                logsAppend += (idx, Logs[idx]);
                idx = idx + 1;
            }

            prevLogIndex = NextIndex[request.Server] - 1;
            prevLogTerm = GetLogTermForIndex(prevLogIndex);

            //this.Logger.WriteLine("\n [Leader] " + this.ServerId + " | term " + this.CurrentTerm + " | log " + this.Logs.Count + " | append votes " + this.VotesReceived + " | append fail (next idx = " + this.NextIndex[request.Server] + ")\n");

            send request.Server, AppendEntriesRequest, (Term=CurrentTerm, LeaderId=this, PrevLogIndex=prevLogIndex,
                PrevLogTerm=prevLogTerm, Entries=Logs, LeaderCommit=CommitIndex, ReceiverEndpoint=request.ReceiverEndpoint);
        }
    }

    fun Vote(request: (Term: int, CandidateId: machine, LastLogIndex: int, LastLogTerm: int))
    {
        var lastLogIndex: int;
        var lastLogTerm: int;
        lastLogIndex = sizeof(Logs);
        lastLogTerm = GetLogTermForIndex(lastLogIndex);

        if (request.Term < CurrentTerm ||
            (VotedFor != null && VotedFor != request.CandidateId) ||
            lastLogIndex > request.LastLogIndex ||
            lastLogTerm > request.LastLogTerm)
        {
            //this.Logger.WriteLine("\n [Server] " + this.ServerId + " | term " + this.CurrentTerm +
              //  " | log " + this.Logs.Count + " | vote false\n");
            send request.CandidateId, VoteResponse, (Term=CurrentTerm, VoteGranted=false);
        }
        else
        {
            //this.Logger.WriteLine("\n [Server] " + this.ServerId + " | term " + this.CurrentTerm +
               // " | log " + this.Logs.Count + " | vote true\n");

            VotedFor = request.CandidateId;
            LeaderId = default(machine);

            send request.CandidateId, VoteResponse, (Term=CurrentTerm, VoteGranted=true);
        }
    }

    fun AppendEntries(request: (Term: int, LeaderId: machine, PrevLogIndex: int, PrevLogTerm: int, Entries: seq[Log], LeaderCommit: int, ReceiverEndpoint: machine))
    {
        var currentIndex: int;
        var idx: int;
        var decIdx: int;
        var logEntry: Log;

        if (request.Term < CurrentTerm)
        {
            //print "\n [Server] " + ServerId + " | term " + CurrentTerm + " | log " +
              //  this.Logs.Count + " | last applied: " + this.LastApplied + " | append false (< term)\n";

            send request.LeaderId, AppendEntriesResponse, (Term=CurrentTerm, Success=false, Server=this, ReceiverEndpoint=request.ReceiverEndpoint);
        }
        else
        {
            if (request.PrevLogIndex > 0 &&
                (sizeof(Logs) < request.PrevLogIndex ||
                Logs[request.PrevLogIndex - 1].Term != request.PrevLogTerm))
            {
                //this.Logger.WriteLine("\n [Server] " + this.ServerId + " | term " + this.CurrentTerm + " | log " +
                  //  this.Logs.Count + " | last applied: " + this.LastApplied + " | append false (not in log)\n");

                send request.LeaderId, AppendEntriesResponse, (Term=CurrentTerm, Success=false, Server=this, ReceiverEndpoint=request.ReceiverEndpoint);
            }
            else
            {
                if (sizeof(request.Entries) > 0)
                {
                    currentIndex = request.PrevLogIndex + 1;
                    idx = 0;
                    while (idx < sizeof(request.Entries))
                    {
                        logEntry = request.Entries[idx];
                        if (sizeof(Logs) < currentIndex)
                        {
                            Logs += (idx, logEntry);
                        }
                        else if (Logs[currentIndex - 1].Term != logEntry.Term)
                        {
                            //this.Logs.RemoveRange(currentIndex - 1, this.Logs.Count - (currentIndex - 1));
                            decIdx = sizeof(Logs) - 1;
                            while (decIdx >= currentIndex-1) {
                                Logs -= decIdx;
                                decIdx = decIdx - 1;
                            }
                            Logs += (decIdx, logEntry);
                        }
                        idx = idx + 1;
                        currentIndex = currentIndex + 1;
                    }
                }

                if (request.LeaderCommit > CommitIndex &&
                    sizeof(Logs) < request.LeaderCommit)
                {
                    CommitIndex = sizeof(Logs);
                }
                else if (request.LeaderCommit > CommitIndex)
                {
                    CommitIndex = request.LeaderCommit;
                }

                if (CommitIndex > LastApplied)
                {
                    LastApplied = LastApplied + 1;
                }

                //this.Logger.WriteLine("\n [Server] " + this.ServerId + " | term " + this.CurrentTerm + " | log " +
                  //  this.Logs.Count + " | entries received " + request.Entries.Count + " | last applied " +
                    //this.LastApplied + " | append true\n");

                LeaderId = request.LeaderId;
                send request.LeaderId, AppendEntriesResponse, (Term=CurrentTerm, Success=true, Server=this, ReceiverEndpoint=request.ReceiverEndpoint);
            }
        }
    }

    fun RedirectLastClientRequestToClusterManager()
    {
        if (LastClientRequest != null)
        {
            send ClusterManager, Request, (Client=LastClientRequest.Client, Command=LastClientRequest.Command);
        }
    }

    fun GetLogTermForIndex(logIndex: int) : int
    {
        var logTerm: int;
        logTerm = 0;
        if (logIndex > 0)
        {
            logTerm = Logs[logIndex - 1].Term;
        }

        return logTerm;
    }

    fun ShuttingDown()
    {
        send ElectionTimer, halt;
        send PeriodicTimer, halt;

        raise halt;
    }
}
// }

