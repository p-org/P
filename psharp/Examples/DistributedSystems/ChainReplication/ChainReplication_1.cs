using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace ChainReplication_1
{
    #region Events

    internal class ePredSucc : Event
    {
        public ePredSucc(Object payload)
            : base(payload)
        { }
    }

    internal class eUpdate : Event
    {
        public eUpdate(Object payload)
            : base(payload)
        { }
    }

    internal class eQuery : Event
    {
        public eQuery(Object payload)
            : base(payload)
        { }
    }

    internal class eResponseToQuery : Event
    {
        public eResponseToQuery(Object payload)
            : base(payload)
        { }
    }

    internal class eFaultDetected : Event
    {
        public eFaultDetected(Object payload)
            : base(payload)
        { }
    }

    internal class eFaultCorrected : Event
    {
        public eFaultCorrected(Object payload)
            : base(payload)
        { }
    }

    internal class eBecomeHead : Event
    {
        public eBecomeHead(Object payload)
            : base(payload)
        { }
    }

    internal class eBecomeTail : Event
    {
        public eBecomeTail(Object payload)
            : base(payload)
        { }
    }

    internal class eNewPredecessor : Event
    {
        public eNewPredecessor(Object payload)
            : base(payload)
        { }
    }

    internal class eNewSuccessor : Event
    {
        public eNewSuccessor(Object payload)
            : base(payload)
        { }
    }

    internal class eUpdateHeadTail : Event
    {
        public eUpdateHeadTail(Object payload)
            : base(payload)
        { }
    }

    internal class eNewSuccInfo : Event
    {
        public eNewSuccInfo(Object payload)
            : base(payload)
        { }
    }

    internal class eBackwardAck : Event
    {
        public eBackwardAck(Object payload)
            : base(payload)
        { }
    }

    internal class eForwardUpdate : Event
    {
        public eForwardUpdate(Object payload)
            : base(payload)
        { }
    }

    internal class eCRPing : Event
    {
        public eCRPing(Object payload)
            : base(payload)
        { }
    }

    internal class eMonitorHistoryUpdate : Event
    {
        public eMonitorHistoryUpdate(Object payload)
            : base(payload)
        { }
    }

    internal class eMonitorSentUpdate : Event
    {
        public eMonitorSentUpdate(Object payload)
            : base(payload)
        { }
    }

    internal class eMonitorUpdateServers : Event
    {
        public eMonitorUpdateServers(Object payload)
            : base(payload)
        { }
    }

    internal class eMonitorResponseToUpdate : Event
    {
        public eMonitorResponseToUpdate(Object payload)
            : base(payload)
        { }
    }

    internal class eMonitorResponseToQuery : Event
    {
        public eMonitorResponseToQuery(Object payload)
            : base(payload)
        { }
    }

    internal class eMonitorUpdateLiveness : Event
    {
        public eMonitorUpdateLiveness(Object payload)
            : base(payload)
        { }
    }

    internal class eMonitorResponseLiveness : Event
    {
        public eMonitorResponseLiveness(Object payload)
            : base(payload)
        { }
    }

    internal class eMonitorQueryLiveness : Event
    {
        public eMonitorQueryLiveness(Object payload)
            : base(payload)
        { }
    }

    internal class eLocal : Event { }
    internal class eDone : Event { }
    internal class eSuccess : Event { }
    internal class eHeadChanged : Event { }
    internal class eTailChanged : Event { }
    internal class eResponseToUpdate : Event { }
    internal class eHeadFailed : Event { }
    internal class eTailFailed : Event { }
    internal class eServerFailed : Event { }
    internal class eFixSuccessor : Event { }
    internal class eFixPredecessor : Event { }
    internal class eStartTimer : Event { }
    internal class eCancelTimer : Event { }
    internal class eCancelTimerSuccess : Event { }
    internal class eTimeout : Event { }
    internal class eCRPong : Event { }
    internal class eMonitorSuccess : Event { }

    #endregion

    #region Machines

    [Main]
    internal class GodMachine : Machine
    {
        private List<Machine> Servers;
        private List<Machine> Clients;
        private Machine ChainReplicationMaster;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as GodMachine;

                Console.WriteLine("[GodMachine] Initializing ...\n");

                machine.Servers = new List<Machine>();
                machine.Clients = new List<Machine>();

                machine.Servers.Insert(0, Machine.Factory.CreateMachine<ChainReplicationServer>(
                        new Tuple<bool, bool, int>(false, true, 3)));
                machine.Servers.Insert(0, Machine.Factory.CreateMachine<ChainReplicationServer>(
                        new Tuple<bool, bool, int>(false, false, 2)));
                machine.Servers.Insert(0, Machine.Factory.CreateMachine<ChainReplicationServer>(
                        new Tuple<bool, bool, int>(true, false, 1)));

                Machine.Factory.CreateMonitor<UpdatePropagationInvariantMonitor>(machine.Servers);
                Machine.Factory.CreateMonitor<UpdateResponseQueryResponseSeqMonitor>(machine.Servers);

                Console.WriteLine("{0} sending event {1} to {2}\n",
                    machine, typeof(ePredSucc), machine.Servers[2]);
                this.Send(machine.Servers[2], new ePredSucc(new Tuple<Machine, Machine>(
                    machine.Servers[1], machine.Servers[2])));
                
                Console.WriteLine("{0} sending event {1} to {2}\n",
                    machine, typeof(ePredSucc), machine.Servers[1]);
                this.Send(machine.Servers[1], new ePredSucc(new Tuple<Machine, Machine>(
                    machine.Servers[0], machine.Servers[2])));
                
                Console.WriteLine("{0} sending event {1} to {2}\n",
                    machine, typeof(ePredSucc), machine.Servers[0]);
                this.Send(machine.Servers[0], new ePredSucc(new Tuple<Machine, Machine>(
                    machine.Servers[0], machine.Servers[1])));

                machine.Clients.Insert(0, Machine.Factory.CreateMachine<Client>(
                    new Tuple<int, Machine, Machine, int>(
                        1, machine.Servers[0], machine.Servers[2], 1)));
                machine.Clients.Insert(0, Machine.Factory.CreateMachine<Client>(
                    new Tuple<int, Machine, Machine, int>(
                        0, machine.Servers[0], machine.Servers[2],  100)));

                machine.ChainReplicationMaster = Machine.Factory.CreateMachine<ChainReplicationMaster>(
                    new Tuple<List<Machine>, List<Machine>>(machine.Servers, machine.Clients));

                this.Delete();
            }
        }
    }

    /// <summary>
    /// The Client machine checks that for a configuration of 3 nodes
    /// an update(k,v) is followed by a successful query(k) == v. Also
    /// a random query is performed in the end.
    /// </summary>
    internal class Client : Machine
    {
        private int Id;
        private int Next;
        private Machine HeadNode;
        private Machine TailNode;
        private int StartIn;
        private Dictionary<int, int> KeyValue;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Client;

                machine.Id = ((Tuple<int, Machine, Machine, int>)this.Payload).Item1;

                Console.WriteLine("[Client-{0}] Initializing ...\n", machine.Id);

                machine.Next = 1;

                machine.HeadNode = ((Tuple<int, Machine, Machine, int>)this.Payload).Item2;
                machine.TailNode = ((Tuple<int, Machine, Machine, int>)this.Payload).Item3;
                machine.StartIn = ((Tuple<int, Machine, Machine, int>)this.Payload).Item4;

                machine.KeyValue = new Dictionary<int, int>();
                machine.KeyValue.Add(1 * machine.StartIn, 100);
                machine.KeyValue.Add(2 * machine.StartIn, 200);
                machine.KeyValue.Add(3 * machine.StartIn, 300);
                machine.KeyValue.Add(4 * machine.StartIn, 400);

                this.Raise(new eLocal());
            }
        }

        private class PumpUpdateRequests : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Client;

                Console.WriteLine("[Client-{0}] PumpUpdateRequests ...\n", machine.Id);

                Console.WriteLine("{0} sending event {1} to {2}\n",
                    machine, typeof(eUpdate), machine.HeadNode);
                this.Send(machine.HeadNode, new eUpdate(new Tuple<Machine, Tuple<int, int>>(
                    machine, new Tuple<int,int>(machine.Next * machine.StartIn,
                        machine.KeyValue[machine.Next * machine.StartIn]))));

                if (machine.Next >= 3)
                {
                    this.Raise(new eDone());
                }
                else
                {
                    this.Raise(new eLocal());
                }
            }

            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eResponseToUpdate)
                };
            }
        }

        private class PumpQueryRequests : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Client;

                Console.WriteLine("[Client-{0}] PumpQueryRequests ...\n", machine.Id);

                Console.WriteLine("{0} sending event {1} to {2}\n",
                    machine, typeof(eQuery), machine.TailNode);
                this.Send(machine.TailNode, new eQuery(new Tuple<Machine, int>(
                    machine, machine.Next * machine.StartIn)));

                if (machine.Next >= 3)
                {
                    this.Raise(new eDone());
                }
                else
                {
                    this.Raise(new eLocal());
                }
            }

            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eResponseToQuery)
                };
            }
        }

        private class End : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Client;
                Console.WriteLine("[Client-{0}] End ...\n", machine.Id);
                this.Delete();
            }
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(PumpUpdateRequests));

            StepStateTransitions pumpUpdateRequestsDict = new StepStateTransitions();
            pumpUpdateRequestsDict.Add(typeof(eDone), typeof(PumpQueryRequests), () =>
                {
                    this.Next = 1;
                });
            pumpUpdateRequestsDict.Add(typeof(eLocal), typeof(PumpUpdateRequests), () =>
                {
                    this.Next++;
                });

            StepStateTransitions pumpQueryRequestsDict = new StepStateTransitions();
            pumpQueryRequestsDict.Add(typeof(eDone), typeof(End));
            pumpQueryRequestsDict.Add(typeof(eLocal), typeof(PumpQueryRequests), () =>
            {
                this.Next++;
            });

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(PumpUpdateRequests), pumpUpdateRequestsDict);
            dict.Add(typeof(PumpQueryRequests), pumpQueryRequestsDict);

            return dict;
        }
    }

    internal class ChainReplicationMaster : Machine
    {
        private List<Machine> Servers;
        private List<Machine> Clients;
        private Machine FaultDetector;

        private Machine Head;
        private Machine Tail;

        private int FaultyNodeIndex;
        private int LastUpdateReceivedSucc;
        private int LastAckSent;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as ChainReplicationMaster;

                Console.WriteLine("[Master] Initializing ...\n");

                machine.Servers = ((Tuple<List<Machine>, List<Machine>>)this.Payload).Item1;
                machine.Clients = ((Tuple<List<Machine>, List<Machine>>)this.Payload).Item2;

                machine.FaultDetector = Machine.Factory.CreateMachine<ChainReplicationFaultDetection>(
                    new Tuple<Machine, List<Machine>>(machine, machine.Servers));

                machine.Head = machine.Servers[0];
                machine.Tail = machine.Servers[machine.Servers.Count - 1];

                this.Raise(new eLocal());
            }
        }

        private class WaitforFault : State
        {

        }

        private class CorrectHeadFailure : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as ChainReplicationMaster;

                Console.WriteLine("[Master] CorrectHeadFailure ...\n");

                machine.Servers.RemoveAt(0);

                Console.WriteLine("{0} sending event {1} to monitor {2}\n", machine,
                    typeof(eMonitorUpdateServers), typeof(UpdatePropagationInvariantMonitor));
                this.Invoke<UpdatePropagationInvariantMonitor>(new eMonitorUpdateServers(
                    machine.Servers));

                Console.WriteLine("{0} sending event {1} to monitor {2}\n", machine,
                    typeof(eMonitorUpdateServers), typeof(UpdateResponseQueryResponseSeqMonitor));
                this.Invoke<UpdateResponseQueryResponseSeqMonitor>(new eMonitorUpdateServers(
                    machine.Servers));

                machine.Head = machine.Servers[0];

                Console.WriteLine("{0} sending event {1} to {2}\n",
                    machine, typeof(eBecomeHead), machine.Head);
                this.Send(machine.Head, new eBecomeHead(machine));
            }
        }

        private class CorrectTailFailure : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as ChainReplicationMaster;

                Console.WriteLine("[Master] CorrectTailFailure ...\n");

                machine.Servers.RemoveAt(machine.Servers.Count - 1);

                Console.WriteLine("{0} sending event {1} to monitor {2}\n", machine,
                    typeof(eMonitorUpdateServers), typeof(UpdatePropagationInvariantMonitor));
                this.Invoke<UpdatePropagationInvariantMonitor>(new eMonitorUpdateServers(
                    machine.Servers));

                Console.WriteLine("{0} sending event {1} to monitor {2}\n", machine,
                    typeof(eMonitorUpdateServers), typeof(UpdateResponseQueryResponseSeqMonitor));
                this.Invoke<UpdateResponseQueryResponseSeqMonitor>(new eMonitorUpdateServers(
                    machine.Servers));

                machine.Tail = machine.Servers[machine.Servers.Count - 1];

                Console.WriteLine("{0} sending event {1} to {2}\n",
                    machine, typeof(eBecomeTail), machine.Tail);
                this.Send(machine.Tail, new eBecomeTail(machine));
            }
        }

        private class CorrectServerFailure : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as ChainReplicationMaster;

                Console.WriteLine("[Master] CorrectServerFailure ...\n");

                machine.Servers.RemoveAt(machine.FaultyNodeIndex);

                Console.WriteLine("{0} sending event {1} to monitor {2}\n", machine,
                    typeof(eMonitorUpdateServers), typeof(UpdatePropagationInvariantMonitor));
                this.Invoke<UpdatePropagationInvariantMonitor>(new eMonitorUpdateServers(
                    machine.Servers));

                Console.WriteLine("{0} sending event {1} to monitor {2}\n", machine,
                    typeof(eMonitorUpdateServers), typeof(UpdateResponseQueryResponseSeqMonitor));
                this.Invoke<UpdateResponseQueryResponseSeqMonitor>(new eMonitorUpdateServers(
                    machine.Servers));

                this.Raise(new eFixSuccessor());
            }
        }

        private void FixSuccessor()
        {
            Console.WriteLine("{0} sending event {1} to {2}\n",
                this, typeof(eNewPredecessor), this.Servers[this.FaultyNodeIndex]);
            this.Send(this.Servers[this.FaultyNodeIndex], new eNewPredecessor(
                new Tuple<Machine, Machine>(this.Servers[this.FaultyNodeIndex - 1], this)));
        }

        private void FixPredecessor()
        {
                Console.WriteLine("{0} sending event {1} to {2}\n",
                    this, typeof(eNewSuccessor), this.Servers[this.FaultyNodeIndex - 1]);
                this.Send(this.Servers[this.FaultyNodeIndex - 1], new eNewSuccessor(
                    new Tuple<Machine, Machine, int, int>(this.Servers[this.FaultyNodeIndex],
                        this, this.LastAckSent, this.LastUpdateReceivedSucc)));
        }

        private void CheckWhichNodeFailed()
        {
            if (this.Servers.Count == 1)
            {
                Runtime.Assert(false, "All nodes have failed.");
            }
            else
            {
                if (this.Head.Equals((Machine)this.Payload))
                {
                    Console.WriteLine("[Master] Head failed ...\n");
                    this.Raise(new eHeadFailed());
                }
                else if (this.Tail.Equals((Machine)this.Payload))
                {
                    Console.WriteLine("[Master] Tail failed ...\n");
                    this.Raise(new eTailFailed());
                }
                else
                {
                    Console.WriteLine("[Master] Server failed ...\n");

                    for (int i = 0; i < this.Servers.Count - 1; i++)
                    {
                        if (this.Servers[i].Equals((Machine)this.Payload))
                        {
                            this.FaultyNodeIndex = i;
                        }
                    }

                    this.Raise(new eServerFailed());
                }
            }
        }

        private void UpdateClients()
        {
            for (int i = 0; i < this.Clients.Count; i++)
            {
                Console.WriteLine("{0} sending event {1} to {2}\n",
                    this, typeof(eUpdateHeadTail), this.Clients[i]);
                this.Send(this.Clients[i], new eUpdateHeadTail(new Tuple<Machine, Machine>(
                    this.Head, this.Tail)));
            }

            this.Raise(new eDone());
        }

        private void SetLastUpdate()
        {
            this.LastUpdateReceivedSucc = ((Tuple<int, int>)this.Payload).Item1;
            this.LastAckSent = ((Tuple<int, int>)this.Payload).Item2;
            this.Raise(new eFixPredecessor());
        }

        private void ProcessSuccess()
        {
            this.Raise(new eDone());
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(WaitforFault));

            StepStateTransitions waitforFaultDict = new StepStateTransitions();
            waitforFaultDict.Add(typeof(eHeadFailed), typeof(CorrectHeadFailure));
            waitforFaultDict.Add(typeof(eTailFailed), typeof(CorrectTailFailure));
            waitforFaultDict.Add(typeof(eServerFailed), typeof(CorrectServerFailure));

            StepStateTransitions correctHeadFailureDict = new StepStateTransitions();
            correctHeadFailureDict.Add(typeof(eDone), typeof(WaitforFault), () =>
                {
                    Console.WriteLine("{0} sending event {1} to {2}\n",
                        this, typeof(eFaultCorrected), this.FaultDetector);
                    this.Send(this.FaultDetector, new eFaultCorrected(this.Servers));
                });

            StepStateTransitions correctTailFailureDict = new StepStateTransitions();
            correctTailFailureDict.Add(typeof(eDone), typeof(WaitforFault), () =>
                {
                    Console.WriteLine("{0} sending event {1} to {2}\n",
                        this, typeof(eFaultCorrected), this.FaultDetector);
                    this.Send(this.FaultDetector, new eFaultCorrected(this.Servers));
                });

            StepStateTransitions correctServerFailureDict = new StepStateTransitions();
            correctServerFailureDict.Add(typeof(eDone), typeof(WaitforFault), () =>
                {
                    Console.WriteLine("{0} sending event {1} to {2}\n",
                        this, typeof(eFaultCorrected), this.FaultDetector);
                    this.Send(this.FaultDetector, new eFaultCorrected(this.Servers));
                });

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(WaitforFault), waitforFaultDict);
            dict.Add(typeof(CorrectHeadFailure), correctHeadFailureDict);
            dict.Add(typeof(CorrectTailFailure), correctTailFailureDict);
            dict.Add(typeof(CorrectServerFailure), correctServerFailureDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings waitforFaultDict = new ActionBindings();
            waitforFaultDict.Add(typeof(eFaultDetected), new Action(CheckWhichNodeFailed));

            ActionBindings correctHeadFailureDict = new ActionBindings();
            correctHeadFailureDict.Add(typeof(eHeadChanged), new Action(UpdateClients));

            ActionBindings correctTailFailureDict = new ActionBindings();
            correctTailFailureDict.Add(typeof(eTailChanged), new Action(UpdateClients));

            ActionBindings correctServerFailureDict = new ActionBindings();
            correctServerFailureDict.Add(typeof(eFixSuccessor), new Action(FixSuccessor));
            correctServerFailureDict.Add(typeof(eNewSuccInfo), new Action(SetLastUpdate));
            correctServerFailureDict.Add(typeof(eFixPredecessor), new Action(FixPredecessor));
            correctServerFailureDict.Add(typeof(eSuccess), new Action(ProcessSuccess));

            dict.Add(typeof(WaitforFault), waitforFaultDict);
            dict.Add(typeof(CorrectHeadFailure), correctHeadFailureDict);
            dict.Add(typeof(CorrectTailFailure), correctTailFailureDict);
            dict.Add(typeof(CorrectServerFailure), correctServerFailureDict);

            return dict;
        }
    }

    internal class ChainReplicationServer : Machine
    {
        private int Id;
        private bool IsHead;
        private bool IsTail;

        private Machine Pred;
        private Machine Succ;

        private List<Tuple<int, Machine, Tuple<int, int>>> Sent;

        private int NextSeqId;
        private List<int> History;
        private Dictionary<int, int> KeyValue;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as ChainReplicationServer;

                machine.Id = ((Tuple<bool, bool, int>)this.Payload).Item3;

                Console.WriteLine("[Server-{0}] Initializing ...\n", machine.Id);

                machine.Sent = new List<Tuple<int, Machine, Tuple<int, int>>>();
                machine.History = new List<int>();
                machine.KeyValue = new Dictionary<int, int>();

                machine.IsHead = ((Tuple<bool, bool, int>)this.Payload).Item1;
                machine.IsTail = ((Tuple<bool, bool, int>)this.Payload).Item2;

                machine.NextSeqId = 0;
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eUpdate),
                    typeof(eQuery),
                    typeof(eBackwardAck),
                    typeof(eForwardUpdate),
                    typeof(eCRPing)
                };
            }
        }

        private class WaitForRequest : State
        {

        }

        private class ProcessUpdate : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as ChainReplicationServer;

                Console.WriteLine("[Server-{0}] ProcessUpdate ...\n", machine.Id);

                var client = ((Tuple<Machine, Tuple<int, int>>)this.Payload).Item1;
                var key = ((Tuple<Machine, Tuple<int, int>>)this.Payload).Item2.Item1;
                var value = ((Tuple<Machine, Tuple<int, int>>)this.Payload).Item2.Item2;

                if (machine.KeyValue.ContainsKey(key))
                {
                    machine.KeyValue[key] = value;
                }
                else
                {
                    machine.KeyValue.Add(key, value);
                }

                machine.History.Add(machine.NextSeqId);

                Console.WriteLine("{0}-{1} sending event {2} to monitor {3}\n", machine, machine.Id,
                    typeof(eMonitorHistoryUpdate), typeof(UpdatePropagationInvariantMonitor));
                this.Invoke<UpdatePropagationInvariantMonitor>(new eMonitorHistoryUpdate(
                    new Tuple<Machine, List<int>>(machine, machine.History)));

                machine.Sent.Add(new Tuple<int, Machine, Tuple<int, int>>(
                    machine.NextSeqId, client, new Tuple<int, int>(key, value)));

                Console.WriteLine("{0}-{1} sending event {2} to monitor {3}\n", machine, machine.Id,
                    typeof(eMonitorSentUpdate), typeof(UpdatePropagationInvariantMonitor));
                this.Invoke<UpdatePropagationInvariantMonitor>(new eMonitorSentUpdate(
                    new Tuple<Machine, List<Tuple<int, Machine, Tuple<int, int>>>>(machine, machine.Sent)));

                Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                    machine, machine.Id, typeof(eForwardUpdate), machine.Succ);
                this.Send(machine.Succ, new eForwardUpdate(new Tuple<Tuple<int, Machine, Tuple<int, int>>, Machine>(
                    new Tuple<int, Machine, Tuple<int, int>>(
                        machine.NextSeqId, client, new Tuple<int, int>(key, value)), machine)));

                this.Raise(new eLocal());
            }
        }

        private class ProcessFwdUpdate : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as ChainReplicationServer;

                Console.WriteLine("[Server-{0}] ProcessFwdUpdate ...\n", machine.Id);

                var seqId = ((Tuple<Tuple<int, Machine, Tuple<int, int>>, Machine>)this.Payload).Item1.Item1;
                var client = ((Tuple<Tuple<int, Machine, Tuple<int, int>>, Machine>)this.Payload).Item1.Item2;
                var key = ((Tuple<Tuple<int, Machine, Tuple<int, int>>, Machine>)this.Payload).Item1.Item3.Item1;
                var value = ((Tuple<Tuple<int, Machine, Tuple<int, int>>, Machine>)this.Payload).Item1.Item3.Item2;
                var pred = ((Tuple<Tuple<int, Machine, Tuple<int, int>>, Machine>)this.Payload).Item2;

                if (pred.Equals(machine.Pred))
                {
                    machine.NextSeqId = seqId;

                    if (machine.KeyValue.ContainsKey(key))
                    {
                        machine.KeyValue[key] = value;
                    }
                    else
                    {
                        machine.KeyValue.Add(key, value);
                    }

                    if (!machine.IsTail)
                    {
                        machine.History.Add(seqId);

                        Console.WriteLine("{0}-{1} sending event {2} to monitor {3}\n", machine, machine.Id,
                            typeof(eMonitorHistoryUpdate), typeof(UpdatePropagationInvariantMonitor));
                        this.Invoke<UpdatePropagationInvariantMonitor>(new eMonitorHistoryUpdate(
                            new Tuple<Machine, List<int>>(machine, machine.History)));

                        machine.Sent.Add(new Tuple<int, Machine, Tuple<int, int>>(
                            seqId, client, new Tuple<int, int>(key, value)));

                        Console.WriteLine("{0}-{1} sending event {2} to monitor {3}\n", machine, machine.Id,
                            typeof(eMonitorSentUpdate), typeof(UpdatePropagationInvariantMonitor));
                        this.Invoke<UpdatePropagationInvariantMonitor>(new eMonitorSentUpdate(
                            new Tuple<Machine, List<Tuple<int, Machine, Tuple<int, int>>>>(machine, machine.Sent)));

                        Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                            machine, machine.Id, typeof(eForwardUpdate), machine.Succ);
                        this.Send(machine.Succ, new eForwardUpdate(new Tuple<Tuple<int, Machine, Tuple<int, int>>, Machine>(
                            new Tuple<int, Machine, Tuple<int, int>>(seqId, client,
                                new Tuple<int, int>(key, value)), machine)));
                    }
                    else
                    {
                        if (!machine.IsHead)
                        {
                            machine.History.Add(seqId);
                        }

                        Console.WriteLine("{0}-{1} sending event {2} to monitor {3}\n", machine, machine.Id,
                            typeof(eMonitorResponseToUpdate), typeof(UpdateResponseQueryResponseSeqMonitor));
                        this.Invoke<UpdateResponseQueryResponseSeqMonitor>(new eMonitorResponseToUpdate(
                            new Tuple<Machine, int, int>(machine, key, value)));

                        //Console.WriteLine("{0}-{1} sending event {2} to monitor {3}\n", machine, machine.Id,
                        //    typeof(eMonitorResponseLiveness), typeof(LivenessUpdatetoResponseMonitor));
                        //Runtime.Invoke<LivenessUpdatetoResponseMonitor>(new eMonitorResponseLiveness(
                        //    seqId));

                        Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                            machine, machine.Id, typeof(eResponseToUpdate), client);
                        this.Send(client, new eResponseToUpdate());

                        Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                            machine, machine.Id, typeof(eBackwardAck), machine.Pred);
                        this.Send(machine.Pred, new eBackwardAck(seqId));
                    }
                }

                this.Raise(new eLocal());
            }
        }

        private class ProcessAck : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as ChainReplicationServer;

                Console.WriteLine("[Server-{0}] ProcessAck ...\n", machine.Id);

                var seqId = (int)this.Payload;

                machine.RemoveItemFromSent(seqId);

                if (!machine.IsHead)
                {
                    Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                        machine, machine.Id, typeof(eBackwardAck), machine.Pred);
                    this.Send(machine.Pred, new eBackwardAck(seqId));
                }

                this.Raise(new eLocal());
            }
        }

        private void InitPred()
        {
            this.Pred = ((Tuple<Machine, Machine>)this.Payload).Item1;
            this.Succ = ((Tuple<Machine, Machine>)this.Payload).Item2;
            this.Raise(new eLocal());
        }

        private void SendPong()
        {
            Machine target = (Machine)this.Payload;

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                this, this.Id, typeof(eCRPong), target);
            this.Send(target, new eCRPong());
        }

        private void BecomeHead()
        {
            this.IsHead = true;
            this.Pred = this;

            Machine target = (Machine)this.Payload;

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                this, this.Id, typeof(eHeadChanged), target);
            this.Send(target, new eHeadChanged());
        }

        private void BecomeTail()
        {
            this.IsTail = true;
            this.Succ = this;

            for (int i = 0; i < this.Sent.Count; i++)
            {
                Console.WriteLine("{0}-{1} sending event {2} to monitor {3}\n", this, this.Id,
                    typeof(eMonitorResponseToUpdate), typeof(UpdateResponseQueryResponseSeqMonitor));
                this.Invoke<UpdateResponseQueryResponseSeqMonitor>(new eMonitorResponseToUpdate(
                    new Tuple<Machine, int, int>(this, this.Sent[i].Item3.Item1, this.Sent[i].Item3.Item2)));

                //Console.WriteLine("{0}-{1} sending event {2} to monitor {3}\n", this, this.Id,
                //    typeof(eMonitorResponseLiveness), typeof(LivenessUpdatetoResponseMonitor));
                //Runtime.Invoke<LivenessUpdatetoResponseMonitor>(new eMonitorResponseLiveness(this.Sent[i].Item1));

                Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                    this, this.Id, typeof(eResponseToUpdate), this.Sent[i].Item2);
                this.Send(this.Sent[i].Item2, new eResponseToUpdate());

                Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                    this, this.Id, typeof(eBackwardAck), this.Pred);
                this.Send(this.Pred, new eBackwardAck(this.Sent[i].Item1));
            }

            Machine target = (Machine)this.Payload;

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                this, this.Id, typeof(eTailChanged), target);
            this.Send(target, new eTailChanged());
        }

        private void UpdatePredecessor()
        {
            this.Pred = ((Tuple<Machine, Machine>)this.Payload).Item1;
            var master = ((Tuple<Machine, Machine>)this.Payload).Item2;

            if (this.History.Count > 0)
            {
                if (this.Sent.Count > 0)
                {
                    Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                        this, this.Id, typeof(eNewSuccInfo), master);
                    this.Send(master, new eNewSuccInfo(new Tuple<int, int>(
                        this.History[this.History.Count - 1], this.Sent[0].Item1)));
                }
                else
                {
                    Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                        this, this.Id, typeof(eNewSuccInfo), master);
                    this.Send(master, new eNewSuccInfo(new Tuple<int, int>(
                        this.History[this.History.Count - 1], this.History[this.History.Count - 1])));
                }
            }
        }

        private void UpdateSuccessor()
        {
            this.Pred = ((Tuple<Machine, Machine, int, int>)this.Payload).Item1;
            var master = ((Tuple<Machine, Machine, int, int>)this.Payload).Item2;
            var lastUpdateRec = ((Tuple<Machine, Machine, int, int>)this.Payload).Item3;
            var lastAckSent = ((Tuple<Machine, Machine, int, int>)this.Payload).Item4;

            if (this.Sent.Count > 0)
            {
                for (int i = 0; i < this.Sent.Count; i++)
                {
                    if (this.Sent[i].Item1 > lastUpdateRec)
                    {
                        Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                            this, this.Id, typeof(eForwardUpdate), this.Succ);
                        this.Send(this.Succ, new eForwardUpdate(new Tuple<Tuple<int, Machine, Tuple<int, int>>, Machine>(
                            this.Sent[i], this)));
                    }
                }

                int tempIndex = -1;
                for (int i = this.Sent.Count - 1; i >= 0; i--)
                {
                    if (this.Sent[i].Item1 == lastAckSent)
                    {
                        tempIndex = i;
                    }
                }

                for (int i = 0; i < tempIndex; i++)
                {
                    Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                        this, this.Id, typeof(eBackwardAck), this.Pred);
                    this.Send(this.Pred, new eBackwardAck(this.Sent[0].Item1));
                    this.Sent.RemoveAt(0);
                }
            }

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                this, this.Id, typeof(eSuccess), master);
            this.Send(master, new eSuccess());
        }

        private void RemoveItemFromSent(int req)
        {
            int removeIdx = -1;

            for (int i = this.Sent.Count - 1; i >= 0; i--)
            {
                if (req == this.Sent[i].Item1)
                {
                    removeIdx = i;
                }
            }

            if (removeIdx != -1)
            {
                this.Sent.RemoveAt(removeIdx);
            }
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(WaitForRequest));

            StepStateTransitions waitForRequestDict = new StepStateTransitions();
            waitForRequestDict.Add(typeof(eUpdate), typeof(ProcessUpdate), () =>
                {
                    Console.WriteLine("[Server-{0}] Request: eUpdate ...\n", this.Id);
                    this.NextSeqId++;
                    Runtime.Assert(this.IsHead, "Server {0} is not head", this.Id);
                });
            waitForRequestDict.Add(typeof(eQuery), typeof(WaitForRequest), () =>
                {
                    var client = ((Tuple<Machine, int>)this.Payload).Item1;
                    var key = ((Tuple<Machine, int>)this.Payload).Item2;

                    Console.WriteLine("[Server-{0}] Request: eQuery ...\n", this.Id);
                    Runtime.Assert(this.IsTail, "Server {0} is not tail", this.Id);

                    if (this.KeyValue.ContainsKey(key))
                    {
                        Console.WriteLine("{0}-{1} sending event {2} to monitor {3}\n", this, this.Id,
                            typeof(eMonitorResponseToQuery), typeof(UpdateResponseQueryResponseSeqMonitor));
                        this.Invoke<UpdateResponseQueryResponseSeqMonitor>(new eMonitorResponseToQuery(
                            new Tuple<Machine, int, int>(this, key, this.KeyValue[key])));

                        Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                            this, this.Id, typeof(eResponseToQuery), client);
                        this.Send(client, new eResponseToQuery(new Tuple<Machine, int>(
                            client, this.KeyValue[key])));
                    }
                    else
                    {
                        Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                            this, this.Id, typeof(eResponseToQuery), client);
                        this.Send(client, new eResponseToQuery(new Tuple<Machine, int>(
                            client, -1)));
                    }
                });
            waitForRequestDict.Add(typeof(eForwardUpdate), typeof(ProcessFwdUpdate));
            waitForRequestDict.Add(typeof(eBackwardAck), typeof(ProcessAck));

            StepStateTransitions processUpdateDict = new StepStateTransitions();
            processUpdateDict.Add(typeof(eLocal), typeof(WaitForRequest));

            StepStateTransitions processFwdUpdateDict = new StepStateTransitions();
            processFwdUpdateDict.Add(typeof(eLocal), typeof(WaitForRequest));

            StepStateTransitions processAckDict = new StepStateTransitions();
            processAckDict.Add(typeof(eLocal), typeof(WaitForRequest));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(WaitForRequest), waitForRequestDict);
            dict.Add(typeof(ProcessUpdate), processUpdateDict);
            dict.Add(typeof(ProcessFwdUpdate), processFwdUpdateDict);
            dict.Add(typeof(ProcessAck), processAckDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings initDict = new ActionBindings();
            initDict.Add(typeof(ePredSucc), new Action(InitPred));

            ActionBindings waitForRequestDict = new ActionBindings();
            waitForRequestDict.Add(typeof(eCRPing), new Action(SendPong));
            waitForRequestDict.Add(typeof(eBecomeHead), new Action(BecomeHead));
            waitForRequestDict.Add(typeof(eBecomeTail), new Action(BecomeTail));
            waitForRequestDict.Add(typeof(eNewPredecessor), new Action(UpdatePredecessor));
            waitForRequestDict.Add(typeof(eNewSuccessor), new Action(UpdateSuccessor));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(WaitForRequest), waitForRequestDict);

            return dict;
        }
    }

    internal class ChainReplicationFaultDetection : Machine
    {
        private List<Machine> Servers;
        private Machine Master;
        private Machine Timer;
        private int CheckNodeIdx;
        private int Faults;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as ChainReplicationFaultDetection;

                Console.WriteLine("[FaultDetection] Initializing ...\n");

                machine.CheckNodeIdx = 0;
                machine.Faults = 100;

                //machine.Timer = Machine.Factory.CreateMachine<Timer>(machine);

                machine.Master = ((Tuple<Machine, List<Machine>>)this.Payload).Item1;
                machine.Servers = ((Tuple<Machine, List<Machine>>)this.Payload).Item2;

                this.Raise(new eLocal());
            }
        }

        private class StartMonitoring : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as ChainReplicationFaultDetection;

                if (machine.Faults < 1)
                {
                    return;
                }

                Console.WriteLine("[FaultDetection] StartMonitoring ...\n");

                //Console.WriteLine("{0} sending event {1} to {2}\n",
                //    machine, typeof(eStartTimer), machine.Timer);
                //this.Send(machine.Timer, new eStartTimer());

                //Console.WriteLine("{0} sending event {1} to {2}\n",
                //    machine, typeof(eCRPing), machine.Servers[machine.CheckNodeIdx]);
                //this.Send(machine.Servers[machine.CheckNodeIdx], new eCRPing(machine));

                machine.BoundedFailureInjection();
                machine.Faults--;
            }
        }

        private class CancelTimer : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as ChainReplicationFaultDetection;

                Console.WriteLine("[FaultDetection] CancelTimer ...\n");

                Console.WriteLine("{0} sending event {1} to {2}\n",
                    machine, typeof(eCancelTimer), machine.Timer);
                this.Send(machine.Timer, new eCancelTimer());
            }
        }

        private class HandleFailure : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as ChainReplicationFaultDetection;

                Console.WriteLine("[FaultDetection] HandleFailure ...\n");

                Console.WriteLine("{0} sending event {1} to {2}\n",
                    machine, typeof(eFaultDetected), machine.Master);
                this.Send(machine.Master, new eFaultDetected(machine.Servers[machine.CheckNodeIdx]));
            }

            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eCRPong)
                };
            }
        }

        private void CallReturn()
        {
            Console.WriteLine("[FaultDetection] CancelTimer (return) ...\n");
            this.Return();
        }

        private void BoundedFailureInjection()
        {
            Console.WriteLine("[FaultDetection] BoundedFailureInjection ...\n");

            if (this.Servers.Count > 1)
            {
                if (Model.Havoc.Boolean)
                {
                    Console.WriteLine("{0} sending event {1} to {2}\n",
                        this, typeof(eTimeout), this);
                    this.Send(this, new eTimeout());
                }
                else
                {
                    Console.WriteLine("{0} sending event {1} to {2}\n",
                        this, typeof(eCRPong), this);
                    this.Send(this, new eCRPong());
                }
            }
            else
            {
                Console.WriteLine("{0} sending event {1} to {2}\n",
                    this, typeof(eCRPong), this);
                this.Send(this, new eCRPong());
            }
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(StartMonitoring));

            StepStateTransitions startMonitoringDict = new StepStateTransitions();
            startMonitoringDict.Add(typeof(eCRPong), typeof(StartMonitoring), () =>
                {
                    //this.Call(CancelTimer);
                    this.CheckNodeIdx++;
                    if (this.CheckNodeIdx == this.Servers.Count)
                    {
                        this.CheckNodeIdx = 0;
                    }
                });
            startMonitoringDict.Add(typeof(eTimeout), typeof(HandleFailure));

            StepStateTransitions handleFailureDict = new StepStateTransitions();
            handleFailureDict.Add(typeof(eFaultCorrected), typeof(StartMonitoring), () =>
                {
                    this.CheckNodeIdx = 0;
                    this.Servers = (List<Machine>)this.Payload;
                });

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(StartMonitoring), startMonitoringDict);
            dict.Add(typeof(HandleFailure), handleFailureDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings cancelTimerDict = new ActionBindings();
            cancelTimerDict.Add(typeof(eTimeout), new Action(CallReturn));
            cancelTimerDict.Add(typeof(eCancelTimerSuccess), new Action(CallReturn));

            dict.Add(typeof(CancelTimer), cancelTimerDict);

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
            startedDict.Add(typeof(eCancelTimer), typeof(Loop), () =>
                {
                    Console.WriteLine("{0} sending event {1} to {2}\n",
                        this, typeof(eCancelTimerSuccess), this.Target);
                    this.Send(this.Target, new eCancelTimerSuccess());
                });

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(Loop), loopDict);
            dict.Add(typeof(Started), startedDict);

            return dict;
        }
    }

    /// <summary>
    /// This monitor checks the Update Propagation Invariant 
    /// Invariant 1: HISTj <= HISTi forall i <= j
    /// Invariant 2: HISTi = HISTj + SENTi
    /// </summary>
    [Monitor]
    internal class UpdatePropagationInvariantMonitor : Machine
    {
        private List<Machine> Servers;

        private Dictionary<Machine, List<int>> HistoryMap;
        private Dictionary<Machine, List<int>> SentMap;
        private List<int> TempSeq;

        private Machine Next;
        private Machine Prev;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as UpdatePropagationInvariantMonitor;

                Console.WriteLine("[UpdatePropagationInvariantMonitor] Initializing ...\n");

                machine.HistoryMap = new Dictionary<Microsoft.PSharp.Machine, List<int>>();
                machine.SentMap = new Dictionary<Microsoft.PSharp.Machine, List<int>>();
                machine.TempSeq = new List<int>();

                machine.Servers = (List<Machine>)this.Payload;

                this.Raise(new eLocal());
            }
        }

        private class WaitForUpdateMessage : State
        {

        }

        private void CheckInvariant1()
        {
            Console.WriteLine("[UpdatePropagationInvariantMonitor] Checking invariant 1 ...\n");

            var target = ((Tuple<Machine, List<int>>)this.Payload).Item1;
            var history = ((Tuple<Machine, List<int>>)this.Payload).Item2;

            this.IsSorted(history);

            if (this.HistoryMap.ContainsKey(target))
            {
                this.HistoryMap[target] = history;
            }
            else
            {
                this.HistoryMap.Add(target, history);
            }

            // HIST(i+1) <= HIST(i)
            this.GetNext(target);
            if (this.Next != null && this.HistoryMap.ContainsKey(this.Next))
            {
                this.CheckLessThan(this.HistoryMap[this.Next], this.HistoryMap[target]);
            }

            // HIST(i) <= HIST(i-1)
            this.GetPrev(target);
            if (this.Prev != null && this.HistoryMap.ContainsKey(this.Prev))
            {
                this.CheckLessThan(this.HistoryMap[target], this.HistoryMap[this.Prev]);
            }
        }

        private void CheckInvariant2()
        {
            Console.WriteLine("[UpdatePropagationInvariantMonitor] Checking invariant 2 ...\n");

            this.ClearTempSeq();

            var target = ((Tuple<Machine, List<Tuple<int, Machine, Tuple<int, int>>>>)this.Payload).Item1;
            var seq = ((Tuple<Machine, List<Tuple<int, Machine, Tuple<int, int>>>>)this.Payload).Item2;

            this.ExtractSeqId(seq);

            if (this.SentMap.ContainsKey(target))
            {
                this.SentMap[target] = this.TempSeq;
            }
            else
            {
                this.SentMap.Add(target, this.TempSeq);
            }

            this.ClearTempSeq();

            // HIST(i) = HIST(i+1) + SENT(i)
            this.GetNext(target);
            if (this.Next != null && this.HistoryMap.ContainsKey(this.Next))
            {
                this.MergeSeq(this.HistoryMap[this.Next], this.SentMap[target]);
                this.CheckEqual(this.HistoryMap[target], this.TempSeq);
            }

            this.ClearTempSeq();

            // HIST(i-1) = HIST(i) + SENT(i-1)
            this.GetPrev(target);
            if (this.Prev != null && this.HistoryMap.ContainsKey(this.Prev))
            {
                this.MergeSeq(this.HistoryMap[target], this.SentMap[this.Prev]);
                this.CheckEqual(this.HistoryMap[this.Prev], this.TempSeq);
            }

            this.ClearTempSeq();
        }

        private void UpdateServers()
        {
            Console.WriteLine("[UpdatePropagationInvariantMonitor] Updating servers ...\n");
            this.Servers = (List<Machine>)this.Payload;
        }

        private void IsSorted(List<int> seq)
        {
            for (int i = 0; i < seq.Count - 1; i++)
            {
                Runtime.Assert(seq[i] < seq[i + 1], "Sequence is not sorted.");
            }
        }

        private void CheckLessThan(List<int> s1, List<int> s2)
        {
            this.IsSorted(s1);
            this.IsSorted(s2);

            Runtime.Assert(s1.Count == s2.Count, "S1 and S2 do not have the same size.");

            for (int i = s1.Count - 1; i >= 0; i--)
            {
                Runtime.Assert(s1[i] == s2[i], "S1[{0}] and S2[{0}] are not equal.", i);
            }
        }

        private void ExtractSeqId(List<Tuple<int, Machine, Tuple<int, int>>> seq)
        {
            this.ClearTempSeq();

            for (int i = seq.Count - 1; i >= 0; i--)
            {
                if (this.TempSeq.Count > 0)
                {
                    this.TempSeq.Insert(0, seq[i].Item1);
                }
                else
                {
                    this.TempSeq.Add(seq[i].Item1);
                }
            }

            this.IsSorted(this.TempSeq);
        }

        private void MergeSeq(List<int> s1, List<int> s2)
        {
            this.ClearTempSeq();
            this.IsSorted(s1);

            if (s1.Count == 0)
            {
                this.TempSeq = s2;
            }
            else if (s2.Count == 0)
            {
                this.TempSeq = s1;
            }
            else
            {
                for (int i = 0; i < s1.Count; i++)
                {
                    if (s1[i] < s2[0])
                    {
                        this.TempSeq.Add(s1[i]);
                    }
                }

                for (int i = 0; i < s2.Count; i++)
                {
                    this.TempSeq.Add(s2[i]);
                }
            }

            this.IsSorted(this.TempSeq);
        }

        private void CheckEqual(List<int> s1, List<int> s2)
        {
            Runtime.Assert(s1.Count == s2.Count, "S1 and S2 do not have the same size.");

            for (int i = s1.Count - 1; i >= 0; i--)
            {
                Runtime.Assert(s1[i] == s2[i], "S1[{0}] and S2[{0}] are not equal.", i);
            }
        }

        private void GetNext(Machine curr)
        {
            this.Next = null;

            for (int i = 1; i < this.Servers.Count; i++)
            {
                if (this.Servers[i - 1].Equals(curr))
                {
                    this.Next = this.Servers[i];
                }
            }
        }

        private void GetPrev(Machine curr)
        {
            this.Prev = null;

            for (int i = 1; i < this.Servers.Count; i++)
            {
                if (this.Servers[i].Equals(curr))
                {
                    this.Prev = this.Servers[i - 1];
                }
            }
        }

        private void ClearTempSeq()
        {
            Runtime.Assert(this.TempSeq.Count <= 6, "Temp sequence has more than 6 elements.");
            this.TempSeq.Clear();
            Runtime.Assert(this.TempSeq.Count == 0, "Temp sequence is not cleared.");
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(WaitForUpdateMessage));

            dict.Add(typeof(Init), initDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings waitForUpdateMessageDict = new ActionBindings();
            waitForUpdateMessageDict.Add(typeof(eMonitorHistoryUpdate), new Action(CheckInvariant1));
            waitForUpdateMessageDict.Add(typeof(eMonitorSentUpdate), new Action(CheckInvariant2));
            waitForUpdateMessageDict.Add(typeof(eMonitorUpdateServers), new Action(UpdateServers));

            dict.Add(typeof(WaitForUpdateMessage), waitForUpdateMessageDict);

            return dict;
        }
    }

    /// <summary>
    /// Checks that a update(x, y) followed immediately by query(x) should return y.
    /// </summary>
    [Monitor]
    internal class UpdateResponseQueryResponseSeqMonitor : Machine
    {
        private List<Machine> Servers;
        private Dictionary<int, int> LastUpdateResponse;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as UpdateResponseQueryResponseSeqMonitor;

                Console.WriteLine("[UpdateResponseQueryResponseSeqMonitor] Initializing ...\n");

                machine.LastUpdateResponse = new Dictionary<int, int>();

                this.Raise(new eLocal());
            }
        }

        private class Wait : State
        {

        }

        private void UpdateServers()
        {
            Console.WriteLine("[UpdatePropagationInvariantMonitor] Updating servers ...\n");
            this.Servers = (List<Machine>)this.Payload;
        }

        private bool Contains(List<Machine> seq, Machine target)
        {
            for (int i = 0; i < this.Servers.Count; i++)
            {
                if (seq[i].Equals(target))
                {
                    return true;
                }
            }

            return false;
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(Wait));

            StepStateTransitions waitDict = new StepStateTransitions();
            waitDict.Add(typeof(eMonitorResponseToUpdate), typeof(Wait), () =>
                {
                    Console.WriteLine("[UpdateResponseQueryResponseSeqMonitor] eMonitorResponseToUpdate ...\n");

                    var tail = ((Tuple<Machine, int, int>)this.Payload).Item1;
                    var key = ((Tuple<Machine, int, int>)this.Payload).Item2;
                    var value = ((Tuple<Machine, int, int>)this.Payload).Item3;

                    if (this.Contains(this.Servers, tail))
                    {
                        if (this.LastUpdateResponse.ContainsKey(key))
                        {
                            this.LastUpdateResponse[key] = value;
                        }
                        else
                        {
                            this.LastUpdateResponse.Add(key, value);
                        }
                    }
                });
            waitDict.Add(typeof(eMonitorResponseToQuery), typeof(Wait), () =>
                {
                    Console.WriteLine("[UpdateResponseQueryResponseSeqMonitor] eMonitorResponseToQuery ...\n");

                    var tail = ((Tuple<Machine, int, int>)this.Payload).Item1;
                    var key = ((Tuple<Machine, int, int>)this.Payload).Item2;
                    var value = ((Tuple<Machine, int, int>)this.Payload).Item3;

                    if (this.Contains(this.Servers, tail))
                    {
                        Runtime.Assert(value == this.LastUpdateResponse[key], "Value {0} is not " +
                            "equal to {1}", value, this.LastUpdateResponse[key]);
                    }
                });

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(Wait), waitDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings waitDict = new ActionBindings();
            waitDict.Add(typeof(eMonitorUpdateServers), new Action(UpdateServers));

            dict.Add(typeof(Wait), waitDict);

            return dict;
        }
    }

    [Monitor]
    internal class LivenessUpdatetoResponseMonitor : Machine
    {
        private int MyRequestId;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as LivenessUpdatetoResponseMonitor;

                Console.WriteLine("[LivenessUpdatetoResponseMonitor] Initializing ...\n");

                machine.MyRequestId = (int)this.Payload;

                this.Raise(new eLocal());
            }
        }

        private class WaitForUpdateRequest : State
        {

        }

        private class WaitForResponse : State
        {

        }

        private class Done : State
        {
            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eMonitorUpdateLiveness),
                    typeof(eMonitorResponseLiveness)
                };
            }
        }

        private void CheckIfMine()
        {
            Console.WriteLine("[LivenessUpdatetoResponseMonitor] CheckIfMine ...\n");
            if ((int)this.Payload == this.MyRequestId)
            {
                this.Raise(new eMonitorSuccess());
            }
        }

        private void AssertNotMine()
        {
            Console.WriteLine("[LivenessUpdatetoResponseMonitor] AssertNotMine ...\n");
            Runtime.Assert(this.MyRequestId != (int)this.Payload, "LivenessUpdatetoResponse failed.");
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(WaitForUpdateRequest));

            StepStateTransitions waitForUpdateRequestDict = new StepStateTransitions();
            waitForUpdateRequestDict.Add(typeof(eMonitorSuccess), typeof(WaitForResponse));

            StepStateTransitions waitForResponseDict = new StepStateTransitions();
            waitForResponseDict.Add(typeof(eMonitorSuccess), typeof(Done));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(WaitForUpdateRequest), waitForUpdateRequestDict);
            dict.Add(typeof(WaitForResponse), waitForResponseDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings waitForUpdateRequestDict = new ActionBindings();
            waitForUpdateRequestDict.Add(typeof(eMonitorUpdateLiveness), new Action(CheckIfMine));
            waitForUpdateRequestDict.Add(typeof(eMonitorResponseLiveness), new Action(AssertNotMine));

            ActionBindings waitForResponseDict = new ActionBindings();
            waitForResponseDict.Add(typeof(eMonitorUpdateLiveness), new Action(AssertNotMine));
            waitForResponseDict.Add(typeof(eMonitorResponseLiveness), new Action(CheckIfMine));

            dict.Add(typeof(WaitForUpdateRequest), waitForUpdateRequestDict);
            dict.Add(typeof(WaitForResponse), waitForResponseDict);

            return dict;
        }
    }

    [Monitor]
    internal class LivenessQuerytoResponseMonitor : Machine
    {
        private int MyRequestId;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as LivenessQuerytoResponseMonitor;

                Console.WriteLine("[LivenessQuerytoResponseMonitor] Initializing ...\n");

                machine.MyRequestId = (int)this.Payload;

                this.Raise(new eLocal());
            }
        }

        private class WaitForQueryRequest : State
        {

        }

        private class WaitForResponse : State
        {

        }

        private class Done : State
        {
            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eMonitorQueryLiveness),
                    typeof(eMonitorResponseLiveness)
                };
            }
        }

        private void CheckIfMine()
        {
            Console.WriteLine("[LivenessQuerytoResponseMonitor] CheckIfMine ...\n");
            if ((int)this.Payload == this.MyRequestId)
            {
                this.Raise(new eMonitorSuccess());
            }
        }

        private void AssertNotMine()
        {
            Console.WriteLine("[LivenessQuerytoResponseMonitor] AssertNotMine ...\n");
            Runtime.Assert(this.MyRequestId != (int)this.Payload, "LivenessQuerytoResponse failed.");
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(WaitForQueryRequest));

            StepStateTransitions waitForQueryRequestDict = new StepStateTransitions();
            waitForQueryRequestDict.Add(typeof(eMonitorSuccess), typeof(WaitForResponse));

            StepStateTransitions waitForResponseDict = new StepStateTransitions();
            waitForResponseDict.Add(typeof(eMonitorSuccess), typeof(Done));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(WaitForQueryRequest), waitForQueryRequestDict);
            dict.Add(typeof(WaitForResponse), waitForResponseDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings waitForQueryRequestDict = new ActionBindings();
            waitForQueryRequestDict.Add(typeof(eMonitorQueryLiveness), new Action(CheckIfMine));
            waitForQueryRequestDict.Add(typeof(eMonitorResponseLiveness), new Action(AssertNotMine));

            ActionBindings waitForResponseDict = new ActionBindings();
            waitForResponseDict.Add(typeof(eMonitorQueryLiveness), new Action(AssertNotMine));
            waitForResponseDict.Add(typeof(eMonitorResponseLiveness), new Action(CheckIfMine));

            dict.Add(typeof(WaitForQueryRequest), waitForQueryRequestDict);
            dict.Add(typeof(WaitForResponse), waitForResponseDict);

            return dict;
        }
    }

    #endregion
}
