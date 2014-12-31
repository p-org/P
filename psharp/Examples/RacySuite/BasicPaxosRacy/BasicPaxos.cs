using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace BasicPaxosRacy
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

    internal class eTimeout : Event { }
    internal class eStartTimer : Event { }
    internal class eCancelTimer : Event { }
    internal class eCancelTimerSuccess : Event { }
    internal class eLocal : Event { }
    internal class eSuccess : Event { }
    internal class eStop : Event { }

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

    #endregion

    #region Machines

    [Main]
    internal class GodMachine : Machine
    {
        private List<Machine> Proposers;
        private List<Machine> Acceptors;
        private Machine PaxosInvariantMonitor;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as GodMachine;

                Console.WriteLine("[GodMachine] Initializing ...\n");

                machine.PaxosInvariantMonitor = Machine.Factory.CreateMachine<PaxosInvariantMonitor>();

                machine.Proposers = new List<Machine>();
                machine.Acceptors = new List<Machine>();

                for (int i = 0; i < 3; i++)
                {
                    machine.Acceptors.Insert(0, Machine.Factory.CreateMachine<Acceptor>(i + 1));
                }

                machine.Proposers.Insert(0, Machine.Factory.CreateMachine<Proposer>(
                        new Tuple<Machine, List<Machine>, List<Machine>, int, int>(
                            machine.PaxosInvariantMonitor, machine.Proposers, machine.Acceptors, 1, 1)));

                machine.Proposers.Insert(0, Machine.Factory.CreateMachine<Proposer>(
                        new Tuple<Machine, List<Machine>, List<Machine>, int, int>(
                            machine.PaxosInvariantMonitor, machine.Proposers, machine.Acceptors, 2, 100)));

                this.Raise(new eLocal());
            }
        }

        private class End : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("[GodMachine] Stopping ...\n");

                this.Delete();
            }
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(End));

            dict.Add(typeof(Init), initDict);

            return dict;
        }
    }

    internal class Acceptor : Machine
    {
        private int Id;

        private Proposal LastSeenProposal;
        private int LastSeenProposalValue;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Acceptor;

                machine.Id = (int)this.Payload;

                Console.WriteLine("[Acceptor-{0}] Initializing ...\n", machine.Id);

                machine.LastSeenProposal = new Proposal(-1, -1);
                machine.LastSeenProposalValue = -1;

                this.Raise(new eLocal());
            }
        }

        private class Wait : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Acceptor;

                Console.WriteLine("[Acceptor-{0}] Waiting ...\n", machine.Id);
            }
        }

        private void Prepare()
        {
            var receivedMessage = (Tuple<Machine, Proposal, int>)this.Payload;

            Console.WriteLine("{0}-{1} Preparing: {2}, {3}, {4}\n",
                    this, this.Id, receivedMessage.Item2.Round, receivedMessage.Item2.ServerId,
                    receivedMessage.Item3);

            if (this.LastSeenProposalValue == -1)
            {
                Console.WriteLine("{0}-{1} Sending Agree: 0, 0, -1\n", this, this.Id);
                this.Send(receivedMessage.Item1, new eAgree(
                    new Tuple<Proposal, int>(new Proposal(0, 0), -1)));

                this.LastSeenProposal = receivedMessage.Item2;
                this.LastSeenProposalValue = receivedMessage.Item3;
            }
            else if (this.IsProposalLessThan(receivedMessage.Item2, this.LastSeenProposal))
            {
                Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                    this, this.Id, typeof(eReject), receivedMessage.Item1);
                this.Send(receivedMessage.Item1, new eReject(new Proposal(
                    this.LastSeenProposal.Round, this.LastSeenProposal.ServerId)));
            }
            else
            {
                Console.WriteLine("{0}-{1} Sending Agree: {2}, {3}, {4}\n", this, this.Id,
                    this.LastSeenProposal.Round, this.LastSeenProposal.ServerId, this.LastSeenProposalValue);
                this.Send(receivedMessage.Item1, new eAgree(new Tuple<Proposal, int>(
                    new Proposal(this.LastSeenProposal.Round, this.LastSeenProposal.ServerId),
                    this.LastSeenProposalValue)));

                this.LastSeenProposal = receivedMessage.Item2;
                this.LastSeenProposalValue = receivedMessage.Item3;
            }
        }

        private void Accept()
        {
            var receivedMessage = (Tuple<Machine, Proposal, int>)this.Payload;

            Console.WriteLine("{0}-{1} Accepting: {2}, {3}, {4}\n",
                    this, this.Id, receivedMessage.Item2.Round, receivedMessage.Item2.ServerId,
                    receivedMessage.Item3);

            if (!this.AreProposalsEqual(receivedMessage.Item2, this.LastSeenProposal))
            {
                Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                    this, this.Id, typeof(eReject), receivedMessage.Item1);
                this.Send(receivedMessage.Item1, new eReject(
                    new Proposal(this.LastSeenProposal.Round, this.LastSeenProposal.ServerId)));
            }
            else
            {
                Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                    this, this.Id, typeof(eAccepted), receivedMessage.Item1);
                this.Send(receivedMessage.Item1, new eAccepted(
                    new Tuple<Proposal, int>(receivedMessage.Item2, receivedMessage.Item3)));
            }
        }

        private void Stop()
        {
            Console.WriteLine("[Acceptor-{0}] Initializing ...\n", this.Id);

            this.Delete();
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
            initDict.Add(typeof(eLocal), typeof(Wait));

            dict.Add(typeof(Init), initDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings waitDict = new ActionBindings();
            waitDict.Add(typeof(ePrepare), new Action(Prepare));
            waitDict.Add(typeof(eAccept), new Action(Accept));
            waitDict.Add(typeof(eStop), new Action(Stop));

            dict.Add(typeof(Wait), waitDict);

            return dict;
        }
    }

    internal class Proposer : Machine
    {
        private Machine PaxosInvariantMonitor;

        private List<Machine> Proposers;
        private List<Machine> Acceptors;
        private Machine Timer;

        private Proposal NextProposal;
        private List<Tuple<Proposal, int>> ReceivedAgreeList;

        private int ProposeVal;
        private int Majority;
        private int Id;
        private int MaxRound;
        private int CountAccept;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Proposer;

                machine.PaxosInvariantMonitor = ((Tuple<Machine, List<Machine>, List<Machine>, int, int>)this.Payload).Item1;
                machine.Proposers = ((Tuple<Machine, List<Machine>, List<Machine>, int, int>)this.Payload).Item2;
                machine.Acceptors = ((Tuple<Machine, List<Machine>, List<Machine>, int, int>)this.Payload).Item3;
                machine.Id = ((Tuple<Machine, List<Machine>, List<Machine>, int, int>)this.Payload).Item4;
                machine.ProposeVal = ((Tuple<Machine, List<Machine>, List<Machine>, int, int>)this.Payload).Item5;

                Console.WriteLine("[Proposer-{0}] Initializing ...\n", machine.Id);

                machine.MaxRound = 0;
                machine.Timer = Machine.Factory.CreateMachine<Timer>(machine);

                machine.Majority = (machine.Acceptors.Count / 2) + 1;
                Runtime.Assert(machine.Majority == 2, "Machine majority {0} " +
                    "is not equal to 2.\n", machine.Majority);

                machine.ReceivedAgreeList = new List<Tuple<Proposal, int>>();

                this.Raise(new eLocal());
            }
        }

        private class ProposeValuePhase1 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Proposer;

                machine.NextProposal = machine.GetNextProposal(machine.MaxRound);

                Console.WriteLine("[Proposer-{0}] Propose 1: round {1}, value {2}\n",
                    machine.Id, machine.NextProposal.Round, machine.ProposeVal);

                machine.BroadcastAcceptors(typeof(ePrepare), new Tuple<Machine, Proposal, int>(
                    machine, machine.NextProposal, machine.ProposeVal));

                Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                    machine, machine.Id, typeof(eStartTimer), machine.Timer);
                this.Send(machine.Timer, new eStartTimer());
                machine.NextProposal.ServerId = 0;
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
                var machine = this.Machine as Proposer;

                machine.CountAccept = 0;
                machine.ProposeVal = machine.GetHighestProposedValue();

                Console.WriteLine("[Proposer-{0}] Propose 2: round {1}, value {2}\n",
                    machine.Id, machine.NextProposal.Round, machine.ProposeVal);

                Console.WriteLine("{0}-{1} sending event {2} to {3}\n", machine, machine.Id,
                        typeof(eMonitorValueProposed), typeof(PaxosInvariantMonitor));
                this.Send(machine.PaxosInvariantMonitor, new eMonitorValueProposed(
                    new Tuple<Proposal, int>(new Proposal(
                        machine.NextProposal.Round, machine.NextProposal.ServerId),
                        machine.ProposeVal)));
                
                machine.BroadcastAcceptors(typeof(eAccept), new Tuple<Machine, Proposal, int>(
                    machine, machine.NextProposal, machine.ProposeVal));

                Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                    machine, machine.Id, typeof(eStartTimer), machine.Timer);
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

        private class Done : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Proposer;

                Console.WriteLine("[Proposer-{0}] Stopping ...\n", machine.Id);

                foreach (var acceptor in machine.Acceptors)
                {
                    this.Send(acceptor, new eStop());
                }

                foreach (var proposer in machine.Proposers)
                {
                    if (!proposer.Equals(machine))
                    {
                        this.Send(proposer, new eStop());
                    }
                }

                this.Send(machine.Timer, new eStop());
                this.Send(machine.PaxosInvariantMonitor, new eStop());

                this.Delete();
            }

            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eReject),
                    typeof(eAgree),
                    typeof(eTimeout),
                    typeof(eAccepted)
                };
            }
        }

        private void CheckCountAgree()
        {
            Console.WriteLine("[Proposer-{0}] CheckCountAgree ...\n", this.Id);

            this.ReceivedAgreeList.Add((Tuple<Proposal, int>)this.Payload);

            if (this.ReceivedAgreeList.Count == this.Majority)
            {
                this.Raise(new eSuccess());
            }
        }

        private void CheckCountAccepted()
        {
            Console.WriteLine("[Proposer-{0}] CheckCountAccepted ...\n", this.Id);

            if (this.AreProposalsEqual(((Tuple<Proposal, int>)this.Payload).Item1,
                this.NextProposal))
            {
                this.CountAccept++;
            }

            if (this.CountAccept == this.Majority)
            {
                this.Raise(new eSuccess());
            }
        }

        private void Stop()
        {
            Console.WriteLine("[Proposer-{0}] Stopping ...\n", this.Id);

            this.Delete();
        }

        private void BroadcastAcceptors(Type e, Tuple<Machine, Proposal, int> pay)
        {
            for (int i = 0; i < this.Acceptors.Count; i++)
            {
                Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                    this, this.Id, e, this.Acceptors[i]);
                this.Send(this.Acceptors[i], Activator.CreateInstance(e, pay) as Event);
            }
        }

        private int GetHighestProposedValue()
        {
            Proposal tempProposal = new Proposal(-1, 0);
            int tempVal = -1;

            foreach (var receivedAgree in this.ReceivedAgreeList)
            {
                if (this.IsProposalLessThan(tempProposal, receivedAgree.Item1))
                {
                    tempProposal = receivedAgree.Item1;
                    tempVal = receivedAgree.Item2;
                }
            }

            if (tempVal != -1)
            {
                return tempVal;
            }
            else
            {
                return ProposeVal;
            }
        }

        private Proposal GetNextProposal(int maxRound)
        {
            return new Proposal(maxRound + 1, this.Id);
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
            initDict.Add(typeof(eLocal), typeof(ProposeValuePhase1));

            // Transitions for ProposeValuePhase1
            StepStateTransitions proposeValuePhase1Dict = new StepStateTransitions();
            proposeValuePhase1Dict.Add(typeof(eReject), typeof(ProposeValuePhase1), () =>
                {
                    Console.WriteLine("[Proposer-{0}] ProposeValuePhase1 (REJECT) ...\n", this.Id);

                    int round = ((Proposal)this.Payload).Round;

                    if (this.NextProposal.Round <= round)
                    {
                        this.MaxRound = round;
                    }

                    Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                        this, this.Id, typeof(eCancelTimer), this.Timer);
                    this.Send(this.Timer, new eCancelTimer());
                });

            proposeValuePhase1Dict.Add(typeof(eSuccess), typeof(ProposeValuePhase2), () =>
                {
                    Console.WriteLine("[Proposer-{0}] ProposeValuePhase1 (SUCCESS) ...\n", this.Id);

                    Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                        this, this.Id, typeof(eCancelTimer), this.Timer);
                    this.Send(this.Timer, new eCancelTimer());
                });

            proposeValuePhase1Dict.Add(typeof(eTimeout), typeof(ProposeValuePhase1));
            proposeValuePhase1Dict.Add(typeof(eStop), typeof(Done));

            // Transitions for ProposeValuePhase2
            StepStateTransitions proposeValuePhase2Dict = new StepStateTransitions();
            proposeValuePhase2Dict.Add(typeof(eReject), typeof(ProposeValuePhase1), () =>
            {
                Console.WriteLine("[Proposer-{0}] ProposeValuePhase2 (REJECT) ...\n", this.Id);

                int round = ((Proposal)this.Payload).Round;

                if (this.NextProposal.Round <= round)
                {
                    this.MaxRound = round;
                }

                this.ReceivedAgreeList.Clear();

                Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                    this, this.Id, typeof(eCancelTimer), this.Timer);
                this.Send(this.Timer, new eCancelTimer());
            });

            proposeValuePhase2Dict.Add(typeof(eSuccess), typeof(Done), () =>
            {
                Console.WriteLine("[Proposer-{0}] ProposeValuePhase2 (SUCCESS) ...\n", this.Id);

                Console.WriteLine("{0}-{1} sending event {2} to {3}\n", this, this.Id,
                        typeof(eMonitorValueChosen), typeof(PaxosInvariantMonitor));
                this.Send(this.PaxosInvariantMonitor, new eMonitorValueChosen(
                    new Tuple<Proposal, int>(this.NextProposal, this.ProposeVal)));

                Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                    this, this.Id, typeof(eCancelTimer), this.Timer);
                this.Send(this.Timer, new eCancelTimer());
            });

            proposeValuePhase2Dict.Add(typeof(eTimeout), typeof(ProposeValuePhase1), () =>
                {
                    this.ReceivedAgreeList.Clear();
                });
            proposeValuePhase2Dict.Add(typeof(eStop), typeof(Done));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(ProposeValuePhase1), proposeValuePhase1Dict);
            dict.Add(typeof(ProposeValuePhase2), proposeValuePhase2Dict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings proposeValuePhase1Dict = new ActionBindings();
            proposeValuePhase1Dict.Add(typeof(eAgree), new Action(CheckCountAgree));

            ActionBindings proposeValuePhase2Dict = new ActionBindings();
            proposeValuePhase2Dict.Add(typeof(eAccepted), new Action(CheckCountAccepted));

            dict.Add(typeof(ProposeValuePhase1), proposeValuePhase1Dict);
            dict.Add(typeof(ProposeValuePhase2), proposeValuePhase2Dict);

            return dict;
        }
    }

    [Ghost]
    internal class Timer : Machine
    {
        private Machine Target;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Timer;

                Console.WriteLine("[Timer] Initializing ...\n");

                machine.Target = (Machine)this.Payload;

                this.Raise(new eLocal());
            }

            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eCancelTimer)
                };
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eStartTimer)
                };
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

        private void Stop()
        {
            Console.WriteLine("[Timer] Stopping ...\n");

            this.Delete();
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

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings loopDict = new ActionBindings();
            loopDict.Add(typeof(eStop), new Action(Stop));

            ActionBindings startedDict = new ActionBindings();
            startedDict.Add(typeof(eStop), new Action(Stop));

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
    internal class PaxosInvariantMonitor : Machine
    {
        private Tuple<Proposal, int> LastValueChosen;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("[Monitor] Initializing ...\n");
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
                Console.WriteLine("[Monitor] CheckValueProposed ...\n");
            }
        }

        private void Stop()
        {
            Console.WriteLine("[Monitor] Stopping ...\n");

            this.Delete();
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
                    Console.WriteLine("[Monitor] LastValueChosen: {0}, {1}, {2}\n",
                        this.LastValueChosen.Item1.Round, this.LastValueChosen.Item1.ServerId,
                        this.LastValueChosen.Item2);
                });

            // Transitions for CheckValueProposed
            StepStateTransitions checkValueProposedDict = new StepStateTransitions();
            checkValueProposedDict.Add(typeof(eMonitorValueChosen), typeof(CheckValueProposed), () =>
            {
                var receivedValue = (Tuple<Proposal, int>)this.Payload;
                Console.WriteLine("[Monitor] ReceivedValue: {0}, {1}, {2}\n",
                        receivedValue.Item1.Round, receivedValue.Item1.ServerId,
                        receivedValue.Item2);
                Runtime.Assert(this.LastValueChosen.Item2 == receivedValue.Item2,
                    "this.LastValueChosen {0} == receivedValue {1}",
                    this.LastValueChosen.Item2, receivedValue.Item2);
            });

            checkValueProposedDict.Add(typeof(eMonitorValueProposed), typeof(CheckValueProposed), () =>
            {
                Console.WriteLine("[Monitor] eMonitorValueProposed ...\n");

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

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings checkValueProposedDict = new ActionBindings();
            checkValueProposedDict.Add(typeof(eStop), new Action(Stop));

            dict.Add(typeof(CheckValueProposed), checkValueProposedDict);

            return dict;
        }
    }

    #endregion
}
