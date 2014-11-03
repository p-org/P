using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Raft
{
    #region Events

    internal class eLocal : Event { }

    internal class eStop : Event { }

    internal class eChangeRoleToFollower : Event { }

    internal class eChangeRoleToCandidate : Event { }

    internal class eChangeRoleToLeader : Event { }

    internal class eClientReq : Event
    {
        public eClientReq(Object payload)
            : base(payload)
        { }
    }

    internal class eClientReqAck : Event
    {
        public eClientReqAck(Object payload)
            : base(payload)
        { }
    }

    internal class eRequestVote : Event
    {
        public eRequestVote(Object payload)
            : base(payload)
        { }
    }

    internal class eRequestVoteAck : Event
    {
        public eRequestVoteAck(Object payload)
            : base(payload)
        { }
    }

    internal class eAppendEntries : Event
    {
        public eAppendEntries(Object payload)
            : base(payload)
        { }
    }

    internal class eAppendEntriesAck : Event
    {
        public eAppendEntriesAck(Object payload)
            : base(payload)
        { }
    }

    internal class eUpdateLeader : Event
    {
        public eUpdateLeader(Object payload)
            : base(payload)
        { }
    }

    internal class eQueryElectionTimeout : Event
    {
        public eQueryElectionTimeout(Object payload)
            : base(payload)
        { }
    }

    internal class eElectionHasNotTimedOut : Event { }

    internal class eElectionTimedOut : Event { }

    internal class eQueryHeartBeating : Event { }

    internal class eSendHeartBeat : Event { }

    internal class eDoNotSendHeartBeat : Event { }

    internal class eQueryClientTimeout : Event { }

    internal class eClientHasNotTimedOut : Event { }

    internal class eClientTimedOut : Event { }

    #endregion

    #region protocol related stuff

    internal class Entry
    {
        public int Term;
        public int Index;
        public Command Command;
    }

    internal class VoteMessage
    {
        public int Term;
        public int CandidateId;
        public int LastLogIndex;
        public int LastLogTerm;
    }

    internal class VoteAckMessage
    {
        public int TargetCandidateId;
        public int SenderId;
        public int Term;
        public bool VoteGranted;
    }

    internal class AppendEntriesMessage
    {
        public int Term;
        public int LeaderId;
        public List<Entry> Entries;
        public int LeaderCommit;
        public int PrevLogIndex;
        public int PrevLogTerm;
    }

    internal class AppendEntriesAckMessage
    {
        public int TargetLeaderId;
        public int SenderId;
        public int Term;
        public bool Success;
    }

    internal class Command
    {
        public CommandTypes CommandType;
        public int Value;

        public Command(CommandTypes type, int value)
        {
            this.CommandType = type;
            this.Value = value;
        }
    }

    internal enum CommandTypes
    {
        Add = 0,
        Subtract = 1,
    }

    #endregion

    #region Machines

    [Main]
    internal class Cluster : Machine
    {
        private List<Machine> Servers;
        private List<Machine> Clients;

        private Machine Leader;

        private int NumOfServers;
        private int NumOfClients;

        private int Counter;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Cluster;

                Console.WriteLine("[Cluster] is Initializing ...\n");

                machine.NumOfServers = ((Tuple<int, int, int>)this.Payload).Item1;
                machine.NumOfClients = ((Tuple<int, int, int>)this.Payload).Item2;
                machine.Counter = ((Tuple<int, int, int>)this.Payload).Item3;
                machine.Servers = new List<Machine>();
                machine.Clients = new List<Machine>();

                for (int idx = 0; idx < machine.NumOfServers; idx++)
                {
                    machine.Servers.Add(Machine.Factory.CreateMachine<Server>(
                        new Tuple<int, Machine, int>(idx, machine, machine.NumOfServers)));
                }

                for (int idx = 0; idx < machine.NumOfClients; idx++)
                {
                    machine.Clients.Add(Machine.Factory.CreateMachine<Client>(
                        new Tuple<int, Machine>(idx, machine)));
                }

                this.Raise(new eLocal());
            }
        }

        private class Running : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Cluster;

                Console.WriteLine("[Cluster] is Running ...\n");
            }
        }

        private void PropagateVote()
        {
            var message = (VoteMessage)this.Payload;

            for (int idx = 0; idx < this.NumOfServers; idx++)
            {
                if (idx != message.CandidateId)
                {
                    this.Send(this.Servers[idx], new eRequestVote(message));
                }
            }
        }

        private void PropagateVoteAck()
        {
            var message = (VoteAckMessage)this.Payload;

            this.Send(this.Servers[message.TargetCandidateId], new eRequestVoteAck(message));
        }

        private void PropagateClientRequest()
        {
            if (this.Counter == 0)
            {
                this.Stop();
            }
            else if (this.Leader != null)
            {
                var command = this.Payload;

                this.Send(this.Leader, new eClientReq(command));

                this.Counter = this.Counter - 1;
            }
            else
            {
                var client = ((Tuple<Machine, Command>)this.Payload).Item1;

                this.Send(client, new eClientReqAck(null));
            }
        }

        private void PropagateAppendEntriesAck()
        {
            if (this.Counter == 0)
            {
                this.Stop();
            }
            else
            {
                var message = (AppendEntriesAckMessage)this.Payload;

                this.Send(this.Servers[message.TargetLeaderId], new eAppendEntriesAck(message));

                this.Counter = this.Counter - 1;
            }
        }

        private void UpdateLeader()
        {
            this.Leader = (Machine)this.Payload;
            this.Send(this.Leader, new eUpdateLeader(this.Servers));
        }

        private void Stop()
        {
            Console.WriteLine("[Cluster] is stopping ...\n");

            foreach (var client in this.Clients)
            {
                this.Send(client, new eStop());
            }

            foreach (var server in this.Servers)
            {
                this.Send(server, new eStop());
            }

            this.Delete();
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(Running));

            dict.Add(typeof(Init), initDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings runningDict = new ActionBindings();
            runningDict.Add(typeof(eRequestVote), new Action(PropagateVote));
            runningDict.Add(typeof(eRequestVoteAck), new Action(PropagateVoteAck));
            runningDict.Add(typeof(eClientReq), new Action(PropagateClientRequest));
            runningDict.Add(typeof(eAppendEntriesAck), new Action(PropagateAppendEntriesAck));
            runningDict.Add(typeof(eUpdateLeader), new Action(UpdateLeader));

            dict.Add(typeof(Running), runningDict);

            return dict;
        }
    }

    internal class Server : Machine
    {
        private int Id;

        private Machine Cluster;
        private Machine Clock;

        private List<Machine> Servers;
        private Machine Client;

        private List<Entry> Log;
        private int CurrentTerm;

        private int CommitIndex;
        private int LastApplied;

        private int[] NextIndex;
        private int[] MatchIndex;

        private int VotedFor;
        private bool[] Votes;
        private List<bool> Acks;

        private int State;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Server;

                machine.Id = ((Tuple<int, Machine, int>)this.Payload).Item1;
                machine.Cluster = ((Tuple<int, Machine, int>)this.Payload).Item2;
                var numOfServers = ((Tuple<int, Machine, int>)this.Payload).Item3;

                Console.WriteLine("[Server-{0} :: 0] is Initializing ...\n", machine.Id);

                machine.Log = new List<Entry>();
                machine.CurrentTerm = 0;
                machine.CommitIndex = 0;
                machine.LastApplied = 0;
                machine.VotedFor = -1;
                machine.State = 0;

                machine.Votes = new bool[numOfServers];
                for (int idx = 0; idx < numOfServers; idx++)
                {
                    machine.Votes[idx] = false;
                }

                machine.Acks = new List<bool>();

                machine.Clock = Machine.Factory.CreateMachine<Clock>(
                    new Tuple<int, Machine>(machine.Id, machine));

                this.Raise(new eChangeRoleToFollower());
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eStop)
                };
            }
        }

        private class Follower : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Server;

                Console.WriteLine("[Server-{0} :: {1}] is Follower ...\n",
                    machine.Id, machine.CurrentTerm);

                this.Send(machine.Clock, new eQueryElectionTimeout(true));
            }

            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eChangeRoleToFollower),
                    typeof(eDoNotSendHeartBeat)
                };
            }
        }

        private class Candidate : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Server;

                Console.WriteLine("[Server-{0} :: {1}] is Candidate ...\n",
                    machine.Id, machine.CurrentTerm);

                machine.Votes[machine.Id] = true;

                var vote = new VoteMessage();
                vote.Term = machine.CurrentTerm;
                vote.CandidateId = machine.Id;

                this.Send(machine.Cluster, new eRequestVote(vote));
                this.Send(machine.Clock, new eQueryElectionTimeout(true));
            }
        }

        private class Leader : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Server;

                Console.WriteLine("[Server-{0} :: {1}] is Leader ...\n",
                    machine.Id, machine.CurrentTerm);

                machine.NextIndex = new int[machine.Votes.Length];
                machine.MatchIndex = new int[machine.Votes.Length];

                this.Send(machine.Cluster, new eUpdateLeader(machine));
                this.Send(machine.Clock, new eQueryHeartBeating());
            }

            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eElectionHasNotTimedOut),
                    typeof(eRequestVote),
                    typeof(eRequestVoteAck)
                };
            }
        }

        private void ProcessVote()
        {
            var receivedVote = (VoteMessage)this.Payload;

            var voteAck = new VoteAckMessage();
            voteAck.TargetCandidateId = receivedVote.CandidateId;
            voteAck.SenderId = this.Id;
            voteAck.Term = this.CurrentTerm;

            Console.WriteLine("[Server-{0} :: {1}] received a vote request from server {2} with term {3}\n",
                this.Id, this.CurrentTerm, receivedVote.CandidateId, receivedVote.Term);

            if (this.CurrentTerm <= receivedVote.Term && VotedFor < 0)
            {
                this.CurrentTerm = receivedVote.Term;
                this.VotedFor = receivedVote.CandidateId;
                voteAck.VoteGranted = true;
            }
            else
            {
                voteAck.VoteGranted = false;
            }

            this.Send(this.Cluster, new eRequestVoteAck(voteAck));
        }

        private void ProcessVoteAck()
        {
            var receivedVoteAck = (VoteAckMessage)this.Payload;

            if (receivedVoteAck.VoteGranted)
            {
                Console.WriteLine("[Server-{0} :: {1}] received a YES vote ack from " +
                    "server {2} with term {3} ...\n", this.Id, this.CurrentTerm,
                    receivedVoteAck.SenderId, receivedVoteAck.Term);
            }
            else
            {
                Console.WriteLine("[Server-{0} :: {1}] received a NO vote ack from " +
                    "server {2} with term {3} ...\n", this.Id, this.CurrentTerm,
                    receivedVoteAck.SenderId, receivedVoteAck.Term);
            }

            if (receivedVoteAck.Term > this.CurrentTerm)
            {
                this.CurrentTerm = receivedVoteAck.Term;
            }

            this.Votes[receivedVoteAck.SenderId] = receivedVoteAck.VoteGranted;

            int voteCounter = 0;
            foreach (var vote in this.Votes)
            {
                if (vote)
                {
                    voteCounter = voteCounter + 1;
                }
            }

            var majority = (this.Votes.Length / 2) + 1;

            if (voteCounter >= majority)
            {
                this.Raise(new eChangeRoleToLeader());
            }
        }

        private void ProcessAppendEntries()
        {
            var receivedVote = (AppendEntriesMessage)this.Payload;

            Console.WriteLine("[Server-{0} :: {1}] received an append entries request from " +
                "server {2} with term {3} ...\n", this.Id, this.CurrentTerm,
                receivedVote.LeaderId, receivedVote.Term);

            var appendEntriesAck = new AppendEntriesAckMessage();
            appendEntriesAck.TargetLeaderId = receivedVote.LeaderId;
            appendEntriesAck.SenderId = this.Id;
            appendEntriesAck.Term = this.CurrentTerm;

            if (this.CurrentTerm > receivedVote.Term)
            {
                appendEntriesAck.Success = false;
            }
            else if (this.Log.Count > receivedVote.PrevLogIndex &&
                this.Log[receivedVote.PrevLogIndex].Term == receivedVote.PrevLogTerm)
            {
                appendEntriesAck.Success = false;
            }
            else
            {
                int indexToRemove = -1;
                foreach (var entry in receivedVote.Entries)
                {
                    if (this.Log.Count > entry.Index &&
                        this.Log[entry.Index].Term != entry.Term)
                    {
                        indexToRemove = entry.Index;
                        break;
                    }
                }

                if (indexToRemove >= 0)
                {
                    this.Log.RemoveRange(indexToRemove, this.Log.Count - indexToRemove + 1);
                }

                this.Log.AddRange(receivedVote.Entries);

                if (receivedVote.LeaderCommit > this.CommitIndex)
                {
                    if (this.Log[this.Log.Count - 1].Index > receivedVote.LeaderCommit)
                    {
                        this.CommitIndex = receivedVote.LeaderCommit;
                    }
                    else
                    {
                        this.CommitIndex = this.Log[this.Log.Count - 1].Index;
                    }
                }

                this.CurrentTerm = receivedVote.Term;
                appendEntriesAck.Success = true;
            }

            this.Send(this.Cluster, new eAppendEntriesAck(appendEntriesAck));

            if (appendEntriesAck.Success)
            {
                this.Raise(new eChangeRoleToFollower());
            }
        }

        private void ProcessAppendEntriesAck()
        {
            var appendEntriesAck = (AppendEntriesAckMessage)this.Payload;

            if (appendEntriesAck.Success)
            {
                Console.WriteLine("[Server-{0} :: {1}] received a successful append entries ack from " +
                    "server {2} with term {3} ...\n", this.Id, this.CurrentTerm,
                    appendEntriesAck.SenderId, appendEntriesAck.Term);
                this.NextIndex[appendEntriesAck.SenderId] = this.Log.Count - 1;
                this.MatchIndex[appendEntriesAck.SenderId] = this.Log.Count - 1;
            }
            else
            {
                Console.WriteLine("[Server-{0} :: {1}] received an un-successful append entries ack from " +
                    "server {2} with term {3} ...\n", this.Id, this.CurrentTerm,
                    appendEntriesAck.SenderId, appendEntriesAck.Term);
                this.NextIndex[appendEntriesAck.SenderId] = this.NextIndex[appendEntriesAck.SenderId] - 1;
                if (this.NextIndex[appendEntriesAck.SenderId] < 0)
                {
                    this.NextIndex[appendEntriesAck.SenderId] = 0;
                }

                if (this.Servers != null && this.Log.Count > 0)
                {
                    var appendEntries = new AppendEntriesMessage();
                    appendEntries.Term = this.CurrentTerm;
                    appendEntries.LeaderId = this.Id;
                    appendEntries.LeaderCommit = this.CommitIndex;
                    appendEntries.Entries = new List<Entry>();

                    var nextIndex = this.NextIndex[appendEntriesAck.SenderId];
                    if (this.Log.Count - 1 >= nextIndex)
                    {
                        for (int i = nextIndex; i < this.Log.Count; i++)
                        {
                            appendEntries.Entries.Add(this.Log[i]);
                        }
                    }
                    else
                    {
                        appendEntries.Entries.Add(this.Log[this.Log.Count - 1]);
                    }

                    if (nextIndex > 0)
                    {
                        appendEntries.PrevLogIndex = nextIndex - 1;
                        appendEntries.PrevLogTerm = this.Log[nextIndex - 1].Term;
                    }

                    this.Send(this.Servers[appendEntriesAck.SenderId], new eAppendEntries(appendEntries));
                }
            }

            if (appendEntriesAck.Term > this.CurrentTerm)
            {
                this.CurrentTerm = appendEntriesAck.Term;
            }

            this.Acks.Add(appendEntriesAck.Success);

            int ackCounter = 0;
            foreach (var ack in this.Acks)
            {
                if (ack)
                {
                    ackCounter = ackCounter + 1;
                }
            }

            var majority = (this.Votes.Length / 2) + 1;

            if (this.Acks.Count == this.Votes.Length
                && this.Log.Count > 0 && this.Client != null)
            {
                if (ackCounter >= majority)
                {
                    var latestCommand = this.Log[this.Log.Count - 1].Command;
                    if (latestCommand.CommandType == CommandTypes.Add)
                    {
                        this.State = this.State + latestCommand.Value;
                    }
                    else
                    {
                        this.State = this.State - latestCommand.Value;
                    }

                    this.CommitIndex = this.Log.Count - 1;
                    this.Send(this.Client, new eClientReqAck(this.State));
                }
                else
                {
                    this.Send(this.Client, new eClientReqAck(null));
                }
            }
        }

        private void ProcessClientReq()
        {
            Console.WriteLine("[Server-{0} :: {1}] is processing a client request ...\n",
                this.Id, this.CurrentTerm);

            this.Client = ((Tuple<Machine, Command>)this.Payload).Item1;

            var newEntry = new Entry();
            newEntry.Command = ((Tuple<Machine, Command>)this.Payload).Item2;
            newEntry.Index = this.Log.Count;
            newEntry.Term = this.CurrentTerm;

            this.Log.Clear();
            this.Log.Add(newEntry);

            var appendEntries = new AppendEntriesMessage();
            appendEntries.Term = this.CurrentTerm;
            appendEntries.LeaderId = this.Id;
            appendEntries.LeaderCommit = this.CommitIndex;
            

            if (this.Servers != null)
            {
                for (int idx = 0; idx < this.Servers.Count; idx++)
                {
                    if (idx == this.Id)
                    {
                        continue;
                    }

                    appendEntries.Entries = new List<Entry>();

                    var nextIndex = this.NextIndex[idx];

                    if (this.Log.Count - 1 >= nextIndex)
                    {
                        for (int i = nextIndex; i < this.Log.Count; i++)
                        {
                            appendEntries.Entries.Add(this.Log[i]);
                        }
                    }
                    else
                    {
                        appendEntries.Entries.Add(newEntry);
                    }

                    this.Send(this.Servers[idx], new eAppendEntries(appendEntries));
                }
            }

            this.Acks.Add(true);
        }

        private void ProcessElectionTimeout()
        {
            Console.WriteLine("[Server-{0} :: {1}] Election has timed out ...\n",
                this.Id, this.CurrentTerm);

            this.CurrentTerm = this.CurrentTerm + 1;
            this.VotedFor = -1;
            this.Raise(new eChangeRoleToCandidate());
        }

        private void QueryElectionTimeout()
        {
            this.Send(this.Clock, new eQueryElectionTimeout(false));
        }

        private void ProcessHeartBeat()
        {
            Console.WriteLine("[Server-{0} :: {1}] is sending heartbeats ...\n",
                this.Id, this.CurrentTerm);

            if (this.Servers != null)
            {
                for (int idx = 0; idx < this.Servers.Count; idx++)
                {
                    if (idx != this.Id)
                    {
                        var appendEntries = new AppendEntriesMessage();
                        appendEntries.Term = this.CurrentTerm;
                        appendEntries.LeaderId = this.Id;
                        appendEntries.Entries = new List<Entry>();

                        this.Send(this.Servers[idx], new eAppendEntries(appendEntries));
                    }
                }
            }
        }

        private void QueryHeartBeat()
        {
            this.Send(this.Clock, new eQueryHeartBeating());
        }

        private void UpdateLeaderInformation()
        {
            this.Servers = (List<Machine>)this.Payload;
        }

        private void Stop()
        {
            Console.WriteLine("[Server-{0} :: {1}] is stopping ...\n",
                this.Id, this.CurrentTerm);

            this.Delete();
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eChangeRoleToFollower), typeof(Follower));

            StepStateTransitions followerDict = new StepStateTransitions();
            followerDict.Add(typeof(eChangeRoleToCandidate), typeof(Candidate));

            StepStateTransitions candidateDict = new StepStateTransitions();
            candidateDict.Add(typeof(eChangeRoleToFollower), typeof(Follower));
            candidateDict.Add(typeof(eChangeRoleToCandidate), typeof(Candidate));
            candidateDict.Add(typeof(eChangeRoleToLeader), typeof(Leader));

            StepStateTransitions leaderDict = new StepStateTransitions();
            leaderDict.Add(typeof(eChangeRoleToFollower), typeof(Follower));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(Follower), followerDict);
            dict.Add(typeof(Candidate), candidateDict);
            dict.Add(typeof(Leader), leaderDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings followerDict = new ActionBindings();
            followerDict.Add(typeof(eElectionTimedOut), new Action(ProcessElectionTimeout));
            followerDict.Add(typeof(eElectionHasNotTimedOut), new Action(QueryElectionTimeout));
            followerDict.Add(typeof(eRequestVote), new Action(ProcessVote));
            followerDict.Add(typeof(eAppendEntries), new Action(ProcessAppendEntries));
            followerDict.Add(typeof(eStop), new Action(Stop));

            ActionBindings candidateDict = new ActionBindings();
            candidateDict.Add(typeof(eElectionTimedOut), new Action(ProcessElectionTimeout));
            candidateDict.Add(typeof(eElectionHasNotTimedOut), new Action(QueryElectionTimeout));
            candidateDict.Add(typeof(eRequestVote), new Action(ProcessVote));
            candidateDict.Add(typeof(eRequestVoteAck), new Action(ProcessVoteAck));
            candidateDict.Add(typeof(eAppendEntries), new Action(ProcessAppendEntries));
            candidateDict.Add(typeof(eStop), new Action(Stop));

            ActionBindings leaderDict = new ActionBindings();
            leaderDict.Add(typeof(eSendHeartBeat), new Action(ProcessHeartBeat));
            leaderDict.Add(typeof(eDoNotSendHeartBeat), new Action(QueryHeartBeat));
            leaderDict.Add(typeof(eAppendEntries), new Action(ProcessAppendEntries));
            leaderDict.Add(typeof(eAppendEntriesAck), new Action(ProcessAppendEntriesAck));
            leaderDict.Add(typeof(eClientReq), new Action(ProcessClientReq));
            leaderDict.Add(typeof(eUpdateLeader), new Action(UpdateLeaderInformation));
            leaderDict.Add(typeof(eStop), new Action(Stop));

            dict.Add(typeof(Follower), followerDict);
            dict.Add(typeof(Candidate), candidateDict);
            dict.Add(typeof(Leader), leaderDict);

            return dict;
        }
    }

    internal class Client : Machine
    {
        private int Id;

        private Machine Cluster;
        private Machine Clock;

        private Random Randomizer;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Client;

                machine.Id = ((Tuple<int, Machine>)this.Payload).Item1;
                machine.Cluster = ((Tuple<int, Machine>)this.Payload).Item2;

                Console.WriteLine("[Client-{0}] is Initializing ...\n", machine.Id);

                machine.Randomizer = new Random();

                machine.Clock = Machine.Factory.CreateMachine<Clock>(
                    new Tuple<int, Machine>(machine.Id + 100, machine));

                this.Raise(new eLocal());
            }
        }

        private class Querying : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Client;

                Console.WriteLine("[Client-{0}] is Querying ...\n", machine.Id);

                this.Send(machine.Clock, new eQueryClientTimeout());
            }
        }

        private void SendNewQuery()
        {
            Console.WriteLine("[Client-{0}] is sending a new query ...\n", this.Id);

            var value = this.Randomizer.Next();
            if (Model.Havoc.Boolean)
            {
                this.Send(this.Cluster, new eClientReq(new Tuple<Machine, Command>(
                    this, new Command(CommandTypes.Add, value))));
            }
            else
            {
                this.Send(this.Cluster, new eClientReq(new Tuple<Machine, Command>(
                    this, new Command(CommandTypes.Subtract, value))));
            }

        }

        private void ProcessesResponse()
        {
            Console.WriteLine("[Client-{0}] received response ...\n", this.Id);

            this.QueryClientTimeout();
        }

        private void QueryClientTimeout()
        {
            this.Send(this.Clock, new eQueryClientTimeout());
        }

        private void Stop()
        {
            Console.WriteLine("[Client-{0}] is stopping ...\n", this.Id);

            this.Delete();
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(Querying));

            dict.Add(typeof(Init), initDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings queryingDict = new ActionBindings();
            queryingDict.Add(typeof(eClientTimedOut), new Action(SendNewQuery));
            queryingDict.Add(typeof(eClientHasNotTimedOut), new Action(QueryClientTimeout));
            queryingDict.Add(typeof(eClientReqAck), new Action(ProcessesResponse));
            queryingDict.Add(typeof(eStop), new Action(Stop));

            dict.Add(typeof(Querying), queryingDict);

            return dict;
        }
    }

    internal class Clock : Machine
    {
        private int Id;

        private Machine Owner;

        private int Timer;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Clock;

                machine.Id = ((Tuple<int, Machine>)this.Payload).Item1;
                machine.Owner = ((Tuple<int, Machine>)this.Payload).Item2;

                Console.WriteLine("[Clock-{0}] is Initializing ...\n", machine.Id);
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eStop)
                };
            }
        }

        private class ElectionTimeout : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Clock;

                Console.WriteLine("[Clock-{0}] is in ElectionTimeout ...\n", machine.Id);

                machine.Timer = 20000;

                this.Raise(new eQueryElectionTimeout(false));
            }
        }

        private class HeartBeating : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Clock;

                Console.WriteLine("[Clock-{0}] is HeartBeating ...\n", machine.Id);

                machine.Timer = 5000;

                this.Raise(new eQueryHeartBeating());
            }
        }

        private class ClientTimeout : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Clock;

                Console.WriteLine("[Clock-{0}] is in ClientTimeout ...\n", machine.Id);

                machine.Timer = 2500;

                this.Raise(new eQueryClientTimeout());
            }
        }

        private void CheckElectionTimeout()
        {
            bool resetTimer = (bool)this.Payload;

            if (resetTimer)
            {
                this.Timer = 20000;
            }
            else if (this.Timer > 0)
            {
                this.Timer = this.Timer - 1;
            }

            if (Model.Havoc.Boolean)
            {
                this.Send(this.Owner, new eElectionHasNotTimedOut());
            }
            else if (Model.Havoc.Boolean)
            {
                this.Send(this.Owner, new eElectionHasNotTimedOut());
            }
            else if (Model.Havoc.Boolean)
            {
                this.Send(this.Owner, new eElectionHasNotTimedOut());
            }
            else if (this.Timer == 0)
            {
                this.Timer = 20000;
                this.Send(this.Owner, new eElectionTimedOut());
            }
            else
            {
                this.Send(this.Owner, new eElectionHasNotTimedOut());
            }
        }

        private void CheckHeartBeat()
        {
            if (this.Timer > 0)
            {
                this.Timer = this.Timer - 1;
            }

            if (Model.Havoc.Boolean)
            {
                this.Send(this.Owner, new eDoNotSendHeartBeat());
            }
            else if (Model.Havoc.Boolean)
            {
                this.Send(this.Owner, new eDoNotSendHeartBeat());
            }
            else if (Model.Havoc.Boolean)
            {
                this.Send(this.Owner, new eDoNotSendHeartBeat());
            }
            else if (this.Timer == 0)
            {
                this.Timer = 5000;
                this.Send(this.Owner, new eSendHeartBeat());
            }
            else
            {
                this.Send(this.Owner, new eDoNotSendHeartBeat());
            }
        }

        private void CheckClientTimeout()
        {
            if (this.Timer > 0)
            {
                this.Timer = this.Timer - 1;
            }

            if (Model.Havoc.Boolean)
            {
                this.Send(this.Owner, new eClientHasNotTimedOut());
            }
            else if (Model.Havoc.Boolean)
            {
                this.Send(this.Owner, new eClientHasNotTimedOut());
            }
            else if (Model.Havoc.Boolean)
            {
                this.Send(this.Owner, new eClientHasNotTimedOut());
            }
            else if (this.Timer == 0)
            {
                this.Timer = 2500;
                this.Send(this.Owner, new eClientTimedOut());
            }
            else
            {
                this.Send(this.Owner, new eClientHasNotTimedOut());
            }
        }

        private void Stop()
        {
            Console.WriteLine("[Clock-{0}] is stopping ...\n", this.Id);

            this.Delete();
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eQueryElectionTimeout), typeof(ElectionTimeout));
            initDict.Add(typeof(eQueryClientTimeout), typeof(ClientTimeout));

            StepStateTransitions electionTimeoutDict = new StepStateTransitions();
            electionTimeoutDict.Add(typeof(eQueryHeartBeating), typeof(HeartBeating));

            StepStateTransitions heartBeatingDict = new StepStateTransitions();
            heartBeatingDict.Add(typeof(eQueryElectionTimeout), typeof(ElectionTimeout));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(ElectionTimeout), electionTimeoutDict);
            dict.Add(typeof(HeartBeating), heartBeatingDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings electionTimeoutDict = new ActionBindings();
            electionTimeoutDict.Add(typeof(eQueryElectionTimeout), new Action(CheckElectionTimeout));
            electionTimeoutDict.Add(typeof(eStop), new Action(Stop));

            ActionBindings heartBeatingDict = new ActionBindings();
            heartBeatingDict.Add(typeof(eQueryHeartBeating), new Action(CheckHeartBeat));
            heartBeatingDict.Add(typeof(eStop), new Action(Stop));

            ActionBindings clientTimeoutDict = new ActionBindings();
            clientTimeoutDict.Add(typeof(eQueryClientTimeout), new Action(CheckClientTimeout));
            clientTimeoutDict.Add(typeof(eStop), new Action(Stop));

            dict.Add(typeof(ElectionTimeout), electionTimeoutDict);
            dict.Add(typeof(HeartBeating), heartBeatingDict);
            dict.Add(typeof(ClientTimeout), clientTimeoutDict);

            return dict;
        }
    }

    #endregion
}
