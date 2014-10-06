using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace MultiPaxos_2
{
    #region Events

    internal class ePrepare : Event
    {
        public ePrepare(Object payload)
            : base(payload)
        { }
    }

    internal class eAccept : Event
    {
        public eAccept(Object payload)
            : base(payload)
        { }
    }

    internal class eAgree : Event
    {
        public eAgree(Object payload)
            : base(payload)
        { }
    }

    internal class eReject : Event
    {
        public eReject(Object payload)
            : base(payload)
        { }
    }

    internal class eAccepted : Event
    {
        public eAccepted(Object payload)
            : base(payload)
        { }
    }

    internal class eAllNodes : Event
    {
        public eAllNodes(Object payload)
            : base(payload)
        { }
    }

    internal class eChosen : Event
    {
        public eChosen(Object payload)
            : base(payload)
        { }
    }

    internal class eUpdate : Event
    {
        public eUpdate(Object payload)
            : base(payload)
        { }
    }

    internal class ePing : Event
    {
        public ePing(Object payload)
            : base(payload)
        { }
    }

    internal class eNewLeader : Event
    {
        public eNewLeader(Object payload)
            : base(payload)
        { }
    }

    internal class eMonitorValueChosen : Event
    {
        public eMonitorValueChosen(Object payload)
            : base(payload)
        { }
    }

    internal class eMonitorValueProposed : Event
    {
        public eMonitorValueProposed(Object payload)
            : base(payload)
        { }
    }

    internal class eMonitorClientSent : Event
    {
        public eMonitorClientSent(Object payload)
            : base(payload)
        { }
    }

    internal class eMonitorProposerSent : Event
    {
        public eMonitorProposerSent(Object payload)
            : base(payload)
        { }
    }

    internal class eMonitorProposerChosen : Event
    {
        public eMonitorProposerChosen(Object payload)
            : base(payload)
        { }
    }

    internal class eLocal : Event { }
    internal class eSuccess : Event { }
    internal class eGoPropose : Event { }
    internal class eStartTimer : Event { }
    internal class eCancelTimer : Event { }
    internal class eCancelTimerSuccess : Event { }
    internal class eTimeout : Event { }

    #endregion

    #region C# Classes and Structs

    internal struct Proposal
    {
        public int Round;
        public int ServerId;

        public Proposal(int round, int serverId)
        {
            this.Round = round;
            this.ServerId = serverId;
        }
    }

    internal struct Leader
    {
        public int Rank;
        public Machine Server;

        public Leader(int rank, Machine server)
        {
            this.Rank = rank;
            this.Server = server;
        }
    }

    #endregion

    #region Machines

    [Main]
    internal class GodMachine : Machine
    {
        private List<Machine> PaxosNodes;
        private Machine Client;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as GodMachine;

                Console.WriteLine("[GodMachine] Initializing ...\n");

                Machine.Factory.CreateMonitor<PaxosInvariantMonitor>();

                // Create the paxos nodes.
                machine.PaxosNodes = new List<Machine>();
                for (int i = 0; i < 3; i++)
                {
                    machine.PaxosNodes.Insert(0, Machine.Factory.CreateMachine<PaxosNode>(i + 1));
                }

                // Send all paxos nodes the other machines.
                for (int i = 0; i < machine.PaxosNodes.Count; i++)
                {
                    Console.WriteLine("{0} sending event {1} to {2}\n",
                        machine, typeof(eAllNodes), machine.PaxosNodes[i]);
                    this.Send(machine.PaxosNodes[i], new eAllNodes(machine.PaxosNodes));
                }

                // Create the client node.
                machine.Client = Machine.Factory.CreateMachine<Client>(machine.PaxosNodes);
            }
        }
    }

    [Ghost]
    internal class Client : Machine
    {
        private List<Machine> Servers;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Client;

                Console.WriteLine("[Client] Initializing ...\n");

                Machine.Factory.CreateMonitor<ValidityCheckMonitor>();

                machine.Servers = (List<Machine>)this.Payload;

                this.Raise(new eLocal());
            }
        }

        private class PumpRequestOne : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Client;

                Console.WriteLine("[Client] Pumping first request ...\n");

                Console.WriteLine("{0} sending event {1} to monitor {2}\n", machine,
                        typeof(eMonitorClientSent), typeof(ValidityCheckMonitor));
                this.Invoke<ValidityCheckMonitor>(new eMonitorClientSent(1));

                if (Model.Havoc.Boolean)
                {
                    Console.WriteLine("{0} sending event {1} to {2}-{3}\n", machine,
                        typeof(eUpdate), machine.Servers[0], 0);
                    this.Send(machine.Servers[0], new eUpdate(new Tuple<int, int>(0, 1)));
                }
                else
                {
                    Console.WriteLine("{0} sending event {1} to {2}-{3}\n", machine, typeof(eUpdate),
                        machine.Servers[machine.Servers.Count - 1], machine.Servers.Count - 1);
                    this.Send(machine.Servers[machine.Servers.Count - 1], new eUpdate(new Tuple<int, int>(0, 1)));
                }
            }
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(PumpRequestOne));

            dict.Add(typeof(Init), initDict);

            return dict;
        }
    }

    internal class PaxosNode : Machine
    {
        private Leader CurrentLeader;
        private Machine LeaderElectionService;

        // Proposer fields
        private List<Machine> Acceptors;
        private Machine Timer;
        private Proposal NextProposal;
        private Tuple<Proposal, int> ReceivedAgree;
        private int Rank;
        private int ProposeValue;
        private int Majority;
        private int MaxRound;
        private int CountAgree;
        private int CountAccept;

        // Acceptor fields
        private Tuple<Proposal, int> LastSeenProposal;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as PaxosNode;

                machine.Rank = (int)this.Payload;

                Console.WriteLine("[PaxosNode-{0}] Initializing ...\n", machine.Rank);

                machine.LastSeenProposal = new Tuple<Proposal, int>(new Proposal(-1, -1), -1);
                machine.MaxRound = 0;

                machine.Timer = Machine.Factory.CreateMachine<Timer>(new Tuple<Machine, int>(machine, 10));
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(ePing)
                };
            }
        }

        private class PerformOperation : State
        {
            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eAgree)
                };
            }
        }

        private class ProposeValuePhase1 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as PaxosNode;

                Console.WriteLine("[PaxosNode-{0}] ProposeValuePhase1 ...\n", machine.Rank);

                machine.CountAgree = 0;
                machine.NextProposal = machine.GetNextProposal(machine.MaxRound);
                machine.ReceivedAgree = new Tuple<Proposal, int>(new Proposal(-1, -1), -1);

                machine.BroadcastAcceptors(typeof(ePrepare), new Tuple<Machine, Proposal>(
                    machine, machine.NextProposal));

                Console.WriteLine("{0}-{1} sending event {2} to monitor {3}\n", machine, machine.Rank,
                        typeof(eMonitorProposerSent), typeof(ValidityCheckMonitor));
                this.Invoke<ValidityCheckMonitor>(new eMonitorProposerSent(machine.ProposeValue));

                Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                    machine, machine.Rank, typeof(eStartTimer), machine.Timer);
                this.Send(machine.Timer, new eStartTimer());
            }

            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eAccepted)
                };
            }
        }

        private class ProposeValuePhase2 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as PaxosNode;

                Console.WriteLine("[PaxosNode-{0}] ProposeValuePhase2 (entry) ...\n", machine.Rank);

                machine.CountAccept = 0;
                machine.ProposeValue = machine.GetHighestProposedValue();

                Console.WriteLine("{0}-{1} sending event {2} to monitor {3}\n", machine, machine.Rank,
                        typeof(eMonitorValueProposed), typeof(PaxosInvariantMonitor));
                this.Invoke<PaxosInvariantMonitor>(new eMonitorValueProposed(new Tuple<Proposal, int>(
                    machine.NextProposal, machine.ProposeValue)));

                Console.WriteLine("{0}-{1} sending event {2} to monitor {3}\n", machine, machine.Rank,
                        typeof(eMonitorProposerSent), typeof(ValidityCheckMonitor));
                this.Invoke<ValidityCheckMonitor>(new eMonitorProposerSent(machine.ProposeValue));

                machine.BroadcastAcceptors(typeof(eAccept), new Tuple<Machine, Proposal, int>(
                    machine, machine.NextProposal, machine.ProposeValue));

                Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                    machine, machine.Rank, typeof(eStartTimer), machine.Timer);
                this.Send(machine.Timer, new eStartTimer());
            }

            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eAgree)
                };
            }
        }

        private class DoneProposal : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as PaxosNode;

                Console.WriteLine("[PaxosNode-{0}] DoneProposal ...\n", machine.Rank);

                Console.WriteLine("{0}-{1} sending event {2} to monitor {3}\n", machine, machine.Rank,
                    typeof(eMonitorProposerChosen), typeof(ValidityCheckMonitor));
                this.Invoke<ValidityCheckMonitor>(new eMonitorProposerChosen(machine.ProposeValue));

                this.Raise(new eChosen(machine.ProposeValue));
            }
        }

        private class RunLearner : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as PaxosNode;

                Console.WriteLine("[PaxosNode-{0}] RunLearner (doing nothing) ...\n", machine.Rank);
            }

            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eAgree),
                    typeof(eAccepted),
                    typeof(eTimeout),
                    typeof(ePrepare),
                    typeof(eReject),
                    typeof(eAccept)
                };
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eNewLeader)
                };
            }
        }

        private void UpdateAcceptors()
        {
            this.Acceptors = (List<Machine>)this.Payload;
            
            this.Majority = (this.Acceptors.Count / 2) + 1;
            Runtime.Assert(this.Majority == 2, "Machine majority {0} " +
                "is not equal to 2.\n", this.Majority);

            this.LeaderElectionService = Machine.Factory.CreateMachine<LeaderElection>(
                new Tuple<List<Machine>, Machine, int>(this.Acceptors, this, this.Rank));

            this.Raise(new eLocal());
        }

        private void CheckIfLeader()
        {
            if (this.CurrentLeader.Rank == this.Rank)
            {
                this.ProposeValue = ((Tuple<int, int>)this.Payload).Item2;
                this.Raise(new eGoPropose());
            }
            else
            {
                Console.WriteLine("{0}-{1} sending event {2} to {3}\n", this, this.Rank,
                    typeof(eUpdate), this.CurrentLeader.Server);
                this.Send(this.CurrentLeader.Server, new eUpdate(this.Payload));
            }
        }

        private void ForwardToLE()
        {
            Console.WriteLine("{0}-{1} sending event {2} to {3}\n", this, this.Rank,
                typeof(ePing), this.LeaderElectionService);
            this.Send(this.LeaderElectionService, new ePing(this.Payload));
        }

        private void UpdateLeader()
        {
            this.CurrentLeader = (Leader)this.Payload;
        }

        private void Prepare()
        {
            var receivedProposer = ((Tuple<Machine, Proposal>)this.Payload).Item1;
            var receivedProposal = ((Tuple<Machine, Proposal>)this.Payload).Item2;

            Console.WriteLine("{0}-{1} Preparing: round {2}, serverId {3}\n", this,
                this.Rank, receivedProposal.Round, receivedProposal.ServerId);

            if (this.LastSeenProposal.Item2 == -1)
            {
                Console.WriteLine("{0}-{1} sending event {2} to {3}\n", this, this.Rank,
                    typeof(eAgree), receivedProposer);
                this.Send(receivedProposer, new eAgree(new Tuple<Proposal, int>(
                    new Proposal(-1, -1), -1)));
                this.LastSeenProposal = new Tuple<Proposal, int>(receivedProposal,
                    this.LastSeenProposal.Item2);
            }
            else if (this.IsProposalLessThan(receivedProposal, this.LastSeenProposal.Item1))
            {
                Console.WriteLine("{0}-{1} sending event {2} to {3}\n", this, this.Rank,
                    typeof(eReject), receivedProposer);
                this.Send(receivedProposer, new eReject(this.LastSeenProposal.Item1));
            }
            else
            {
                Console.WriteLine("{0}-{1} sending event {2} to {3}\n", this, this.Rank,
                    typeof(eAgree), receivedProposer);
                this.Send(receivedProposer, new eAgree(this.LastSeenProposal));
                this.LastSeenProposal = new Tuple<Proposal, int>(receivedProposal,
                    this.LastSeenProposal.Item2);
            }
        }

        private void Accept()
        {
            var receivedProposer = ((Tuple<Machine, Proposal, int>)this.Payload).Item1;
            var receivedProposal = ((Tuple<Machine, Proposal, int>)this.Payload).Item2;
            var receivedValue = ((Tuple<Machine, Proposal, int>)this.Payload).Item3;

            Console.WriteLine("{0}-{1} Accepting: round {2}, serverId {3}, value {4}\n", this,
                this.Rank, receivedProposal.Round, receivedProposal.ServerId, receivedValue);

            if (!this.AreProposalsEqual(receivedProposal, this.LastSeenProposal.Item1))
            {
                Console.WriteLine("{0}-{1} sending event {2} to {3}\n", this, this.Rank,
                    typeof(eReject), receivedProposer);
                this.Send(receivedProposer, new eReject(this.LastSeenProposal.Item1));
            }
            else
            {
                this.LastSeenProposal = new Tuple<Proposal, int>(receivedProposal, receivedValue);

                Console.WriteLine("{0}-{1} sending event {2} to {3}\n", this, this.Rank,
                typeof(eAccepted), receivedProposer);
                this.Send(receivedProposer, new eAccepted(new Tuple<Proposal, int>(
                    receivedProposal, receivedValue)));
            }
        }

        private void CheckCountAgree()
        {
            Console.WriteLine("[PaxosNode-{0}] CheckCountAgree ...\n", this.Rank);

            var receivedProposal = ((Tuple<Proposal, int>)this.Payload).Item1;
            var receivedValue = ((Tuple<Proposal, int>)this.Payload).Item2;

            this.CountAgree++;

            if (this.IsProposalLessThan(this.ReceivedAgree.Item1, receivedProposal))
            {
                this.ReceivedAgree = new Tuple<Proposal, int>(receivedProposal, receivedValue);
            }

            if (this.CountAgree == this.Majority)
            {
                this.Raise(new eSuccess());
            }
        }

        private void CheckCountAccepted()
        {
            Console.WriteLine("[PaxosNode-{0}] CheckCountAccepted ...\n", this.Rank);

            var receivedProposal = ((Tuple<Proposal, int>)this.Payload).Item1;
            var receivedValue = ((Tuple<Proposal, int>)this.Payload).Item2;

            if (this.AreProposalsEqual(receivedProposal, this.NextProposal))
            {
                this.CountAccept++;
            }

            if (this.CountAccept == this.Majority)
            {
                this.Raise(new eSuccess());
            }
        }

        private void BroadcastAcceptors(Type e, Object pay)
        {
            for (int i = 0; i < this.Acceptors.Count; i++)
            {
                Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                    this, this.Rank, e, this.Acceptors[i]);
                this.Send(this.Acceptors[i], Activator.CreateInstance(e, pay) as Event);
            }
        }

        private int GetHighestProposedValue()
        {
            if (this.ReceivedAgree.Item2 != -1)
            {
                return this.ReceivedAgree.Item2;
            }
            else
            {
                return this.ProposeValue;
            }
        }

        private Proposal GetNextProposal(int maxRound)
        {
            return new Proposal(maxRound + 1, this.Rank);
        }

        private bool AreProposalsEqual(Proposal p1, Proposal p2)
        {
            if (p1.Round == p2.Round && p1.ServerId == p2.ServerId)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsProposalLessThan(Proposal p1, Proposal p2)
        {
            if (p1.Round < p2.Round)
            {
                return true;
            }
            else if (p1.Round == p2.Round)
            {
                if (p1.ServerId < p2.ServerId)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(PerformOperation));

            StepStateTransitions performOperationDict = new StepStateTransitions();
            performOperationDict.Add(typeof(eChosen), typeof(RunLearner));

            // Step transitions for ProposeValuePhase1
            StepStateTransitions proposeValuePhase1Dict = new StepStateTransitions();
            proposeValuePhase1Dict.Add(typeof(eReject), typeof(ProposeValuePhase1), () =>
                {
                    Console.WriteLine("[PaxosNode-{0}] ProposeValuePhase1 (REJECT) ...\n", this.Rank);

                    var round = ((Proposal)this.Payload).Round;

                    if (this.NextProposal.Round <= round)
                    {
                        this.MaxRound = round;
                    }

                    Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                        this, this.Rank, typeof(eStartTimer), this.Timer);
                    this.Send(this.Timer, new eCancelTimer());
                });

            proposeValuePhase1Dict.Add(typeof(eSuccess), typeof(ProposeValuePhase2), () =>
                {
                    Console.WriteLine("[PaxosNode-{0}] ProposeValuePhase1 (SUCCESS) ...\n", this.Rank);

                    Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                        this, this.Rank, typeof(eStartTimer), this.Timer);
                    this.Send(this.Timer, new eCancelTimer());
                });

            proposeValuePhase1Dict.Add(typeof(eTimeout), typeof(ProposeValuePhase1));

            // Step transitions for ProposeValuePhase2
            StepStateTransitions proposeValuePhase2Dict = new StepStateTransitions();
            proposeValuePhase2Dict.Add(typeof(eReject), typeof(ProposeValuePhase1), () =>
                {
                    Console.WriteLine("[PaxosNode-{0}] ProposeValuePhase2 (REJECT) ...\n", this.Rank);

                    var round = ((Proposal)this.Payload).Round;

                    if (this.NextProposal.Round <= round)
                    {
                        this.MaxRound = round;
                    }

                    Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                        this, this.Rank, typeof(eStartTimer), this.Timer);
                    this.Send(this.Timer, new eCancelTimer());
                });

            proposeValuePhase2Dict.Add(typeof(eSuccess), typeof(DoneProposal), () =>
                {
                    Console.WriteLine("[PaxosNode-{0}] ProposeValuePhase2 (SUCCESS) ...\n", this.Rank);

                    Console.WriteLine("{0}-{1} sending event {2} to monitor {3}\n", this, this.Rank,
                            typeof(eMonitorValueChosen), typeof(PaxosInvariantMonitor));
                    this.Invoke<PaxosInvariantMonitor>(new eMonitorValueChosen(new Tuple<Proposal, int>(
                        this.NextProposal, this.ProposeValue)));

                    Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                        this, this.Rank, typeof(eStartTimer), this.Timer);
                    this.Send(this.Timer, new eCancelTimer());
                });

            proposeValuePhase2Dict.Add(typeof(eTimeout), typeof(ProposeValuePhase1));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(PerformOperation), performOperationDict);
            dict.Add(typeof(ProposeValuePhase1), proposeValuePhase1Dict);
            dict.Add(typeof(ProposeValuePhase2), proposeValuePhase2Dict);

            return dict;
        }

        protected override Dictionary<Type, CallStateTransitions> DefineCallStateTransitions()
        {
            Dictionary<Type, CallStateTransitions> dict = new Dictionary<Type, CallStateTransitions>();

            // Call transitions for PerformOperation
            CallStateTransitions performOperationDict = new CallStateTransitions();
            // Proposer
            performOperationDict.Add(typeof(eGoPropose), typeof(ProposeValuePhase1));

            dict.Add(typeof(PerformOperation), performOperationDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings initDict = new ActionBindings();
            initDict.Add(typeof(eAllNodes), new Action(UpdateAcceptors));

            // Action bindings for PerformOperation
            ActionBindings performOperationDict = new ActionBindings();
            // Proposer
            performOperationDict.Add(typeof(eUpdate), new Action(CheckIfLeader));
            // Acceptor
            performOperationDict.Add(typeof(ePrepare), new Action(Prepare));
            performOperationDict.Add(typeof(eAccept), new Action(Accept));
            // Leader Election
            performOperationDict.Add(typeof(ePing), new Action(ForwardToLE));
            performOperationDict.Add(typeof(eNewLeader), new Action(UpdateLeader));

            // Action bindings for ProposeValuePhase1
            ActionBindings proposeValuePhase1Dict = new ActionBindings();
            proposeValuePhase1Dict.Add(typeof(eAgree), new Action(CheckCountAgree));

            // Action bindings for ProposeValuePhase2
            ActionBindings proposeValuePhase2Dict = new ActionBindings();
            proposeValuePhase2Dict.Add(typeof(eAccepted), new Action(CheckCountAccepted));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(PerformOperation), performOperationDict);
            dict.Add(typeof(ProposeValuePhase1), proposeValuePhase1Dict);
            dict.Add(typeof(ProposeValuePhase2), proposeValuePhase2Dict);

            return dict;
        }
    }

    internal class LeaderElection : Machine
    {
        private List<Machine> Servers;
        private Machine ParentServer;
        private Leader CurrentLeader;
        private int Rank;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as LeaderElection;

                machine.Rank = ((Tuple<List<Machine>, Machine, int>)this.Payload).Item3;

                Console.WriteLine("[LeaderElection-{0}] Initializing ...\n", machine.Rank);

                machine.Servers = ((Tuple<List<Machine>, Machine, int>)this.Payload).Item1;
                machine.ParentServer = ((Tuple<List<Machine>, Machine, int>)this.Payload).Item2;
                machine.CurrentLeader = new Leader(machine.Rank, machine);

                this.Raise(new eLocal());
            }
        }

        private class SendLeader : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as LeaderElection;

                Console.WriteLine("[LeaderElection-{0}] SendLeader ...\n", machine.Rank);

                machine.CurrentLeader = machine.GetNewLeader();

                Runtime.Assert(machine.CurrentLeader.Rank <= machine.Rank, "Current leader rank {0} " +
                    "is not less or equal to this machine's rank {1}.\n",
                    machine.CurrentLeader.Rank, machine.Rank);

                Console.WriteLine("{0} sending event {1} to {2}\n",
                    machine, typeof(eNewLeader), machine.ParentServer);
                this.Send(machine.ParentServer, new eNewLeader(machine.CurrentLeader));
            }
        }

        private Leader GetNewLeader()
        {
            return new Leader(1, this.Servers[0]);
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(SendLeader));

            dict.Add(typeof(Init), initDict);

            return dict;
        }
    }

    [Ghost]
    internal class Timer : Machine
    {
        private Machine Target;
        private int TimeoutValue;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Timer;

                Console.WriteLine("[Timer] Initializing ...\n");

                machine.Target = ((Tuple<Machine, int>)this.Payload).Item1;
                machine.TimeoutValue = ((Tuple<Machine, int>)this.Payload).Item2;

                this.Raise(new eLocal());
            }
        }

        private class Loop : State
        {
            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eCancelTimer)
                };
            }
        }

        private class Started : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Timer;

                Console.WriteLine("[Timer] Started ...\n");

                if (Model.Havoc.Boolean)
                {
                    Console.WriteLine("{0} sending event {1} to {2}\n",
                        this.Machine, typeof(eTimeout), machine.Target);
                    this.Send(machine.Target, new eTimeout());
                    this.Raise(new eLocal());
                }
            }

            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eStartTimer)
                };
            }
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(Loop));

            StepStateTransitions loopDict = new StepStateTransitions();
            loopDict.Add(typeof(eStartTimer), typeof(Started));

            StepStateTransitions startedDict = new StepStateTransitions();
            startedDict.Add(typeof(eLocal), typeof(Loop));
            startedDict.Add(typeof(eCancelTimer), typeof(Loop));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(Loop), loopDict);
            dict.Add(typeof(Started), startedDict);

            return dict;
        }
    }

    /// <summary>
    /// The monitor checks the following property:
    /// 
    /// If the chosen proposal has value v, then every higher numbered
    /// proposal issued by any proposer has value v.
    /// </summary>
    [Monitor]
    internal class PaxosInvariantMonitor : Machine
    {
        private Tuple<Proposal, int> LastValueChosen;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as PaxosInvariantMonitor;
                Console.WriteLine("[PaxosInvariantMonitor] Initializing ...\n");
                this.Raise(new eLocal());
            }
        }

        private class WaitForValueChosen : State
        {
            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eMonitorValueProposed)
                };
            }
        }

        private class CheckValueProposed : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("[PaxosInvariantMonitor] CheckValueProposed ...\n");
            }
        }

        private bool IsProposalLessThan(Proposal p1, Proposal p2)
        {
            if (p1.Round < p2.Round)
            {
                return true;
            }
            else if (p1.Round == p2.Round)
            {
                if (p1.ServerId < p2.ServerId)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(WaitForValueChosen));

            // Transitions for WaitForValueChosen
            StepStateTransitions waitForValueChosenDict = new StepStateTransitions();
            waitForValueChosenDict.Add(typeof(eMonitorValueChosen), typeof(CheckValueProposed), () =>
                {
                    this.LastValueChosen = (Tuple<Proposal, int>)this.Payload;

                    Console.WriteLine("[PaxosInvariantMonitor] LastValueChosen: {0}, {1}, {2}\n",
                        this.LastValueChosen.Item1.Round, this.LastValueChosen.Item1.ServerId,
                        this.LastValueChosen.Item2);
                });

            // Transitions for CheckValueProposed
            StepStateTransitions checkValueProposedDict = new StepStateTransitions();
            checkValueProposedDict.Add(typeof(eMonitorValueChosen), typeof(CheckValueProposed), () =>
            {
                var receivedValue = (Tuple<Proposal, int>)this.Payload;
                Console.WriteLine("[PaxosInvariantMonitor] ReceivedValue: {0}, {1}, {2}\n",
                    receivedValue.Item1.Round, receivedValue.Item1.ServerId, receivedValue.Item2);
                Runtime.Assert(this.LastValueChosen.Item2 == receivedValue.Item2,
                    "this.LastValueChosen {0} == receivedValue {1}",
                    this.LastValueChosen.Item2, receivedValue.Item2);
            });

            checkValueProposedDict.Add(typeof(eMonitorValueProposed), typeof(CheckValueProposed), () =>
            {
                Console.WriteLine("[PaxosInvariantMonitor] eMonitorValueProposed ...\n");

                var receivedValue = (Tuple<Proposal, int>)this.Payload;

                if (this.IsProposalLessThan(this.LastValueChosen.Item1, receivedValue.Item1))
                {
                    Runtime.Assert(this.LastValueChosen.Item2 == receivedValue.Item2,
                        "this.LastValueChosen {0} == receivedValue {1}",
                        this.LastValueChosen.Item2, receivedValue.Item2);
                }
            });

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(WaitForValueChosen), waitForValueChosenDict);
            dict.Add(typeof(CheckValueProposed), checkValueProposedDict);

            return dict;
        }
    }

    /// <summary>
    /// The monitor checks the following property:
    /// 
    /// If the proposed value is from the set send by the client (accept), then
    /// the chosen value is the one proposed by at least one proposer (chosen).
    /// </summary>
    [Monitor]
    internal class ValidityCheckMonitor : Machine
    {
        private HashSet<int> ClientSet;
        private HashSet<int> ProposedSet;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as ValidityCheckMonitor;
                Console.WriteLine("[ValidityCheckMonitor] Initializing ...\n");
                machine.ClientSet = new HashSet<int>();
                machine.ProposedSet = new HashSet<int>();
                this.Raise(new eLocal());
            }
        }

        private class Wait : State
        {

        }

        private void AddClientSet()
        {
            Console.WriteLine("[ValidityCheckMonitor] AddClientSet ...\n");
            int value = (int)this.Payload;
            this.ClientSet.Add(value);
        }

        private void AddProposerSet()
        {
            Console.WriteLine("[ValidityCheckMonitor] AddProposerSet ...\n");
            int value = (int)this.Payload;
            Runtime.Assert(this.ClientSet.Contains(value), "{0} does not exist in Client Set", value);
            this.ProposedSet.Add(value);
        }

        private void CheckChosenValidity()
        {
            Console.WriteLine("[ValidityCheckMonitor] CheckChosenValidity ...\n");
            int value = (int)this.Payload;
            Runtime.Assert(this.ProposedSet.Contains(value), "{0} does not exist in Proposed Set", value);
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(Wait));

            dict.Add(typeof(Init), initDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings waitDict = new ActionBindings();
            waitDict.Add(typeof(eMonitorClientSent), new Action(AddClientSet));
            waitDict.Add(typeof(eMonitorProposerSent), new Action(AddProposerSet));
            waitDict.Add(typeof(eMonitorProposerChosen), new Action(CheckChosenValidity));

            dict.Add(typeof(Wait), waitDict);

            return dict;
        }
    }

    #endregion
}
