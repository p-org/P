//-----------------------------------------------------------------------
// <copyright file="TwoPhaseCommit.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace TwoPhaseCommit
{
    #region Events
    internal class eREQ_REPLICA : Event
    {
        public eREQ_REPLICA(Object payload)
            : base(payload)
        { }
    }

    internal class eRESP_REPLICA_COMMIT : Event
    {
        public eRESP_REPLICA_COMMIT(Object payload)
            : base(payload)
        { }
    }

    internal class eRESP_REPLICA_ABORT : Event
    {
        public eRESP_REPLICA_ABORT(Object payload)
            : base(payload)
        { }
    }

    internal class eGLOBAL_ABORT : Event
    {
        public eGLOBAL_ABORT(Object payload)
            : base(payload)
        { }
    }

    internal class eGLOBAL_COMMIT : Event
    {
        public eGLOBAL_COMMIT(Object payload)
            : base(payload)
        { }
    }

    internal class eWRITE_REQ : Event
    {
        public eWRITE_REQ(Object payload)
            : base(payload)
        { }
    }

    internal class eREAD_REQ : Event
    {
        public eREAD_REQ(Object payload)
            : base(payload)
        { }
    }

    internal class eREAD_SUCCESS : Event
    {
        public eREAD_SUCCESS(Object payload)
            : base(payload)
        { }
    }

    internal class eStartTimer : Event
    {
        public eStartTimer(Object payload)
            : base(payload)
        { }
    }

    internal class eMONITOR_WRITE : Event
    {
        public eMONITOR_WRITE(Object payload)
            : base(payload)
        { }
    }

    internal class eMONITOR_READ_SUCCESS : Event
    {
        public eMONITOR_READ_SUCCESS(Object payload)
            : base(payload)
        { }
    }

    internal class eMONITOR_READ_UNAVAILABLE : Event
    {
        public eMONITOR_READ_UNAVAILABLE(Object payload)
            : base(payload)
        { }
    }

    internal class eUpdate : Event
    {
        public eUpdate(Object payload)
            : base(payload)
        { }
    }

    internal class eWRITE_FAIL : Event { }

    internal class eWRITE_SUCCESS : Event { }

    internal class eREAD_FAIL : Event { }

    internal class eREAD_UNAVAILABLE : Event { }

    internal class eUnit : Event { }

    internal class eStop : Event { }

    internal class eTimeout : Event { }

    internal class eCancelTimer : Event { }

    internal class eCancelTimerFailure : Event { }

    internal class eCancelTimerSuccess : Event { }

    #endregion

    internal class Message
    {
        internal Machine Machine;
        internal int Item1;
        internal int Item2;
    }

    #region Machines

    [Main]
    internal class Master : Machine
    {
        private Machine Coordinator;
        private Machine Client;
        private Machine Monitor;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Master;

                Console.WriteLine("[Master] Initializing ...\n");

                machine.Monitor = Machine.Factory.CreateMachine<Monitor>();

                machine.Coordinator = Machine.Factory.CreateMachine<Coordinator>(
                    new Tuple<int, Machine>(2, machine.Monitor));

                machine.Client = Machine.Factory.CreateMachine<Client>(machine.Coordinator);

                this.Raise(new eUnit());
            }
        }

        private class Stopping : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Master;

                Console.WriteLine("[Master] Stopping ...\n");

                //this.Send(machine.Client, new eStop());

                this.Delete();
            }
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eUnit), typeof(Stopping));

            dict.Add(typeof(Init), initDict);

            return dict;
        }
    }

    internal class Coordinator : Machine
    {
        private List<Machine> Replicas;
        private Machine Client;
        private Machine Monitor;
        private Machine Replica;
        private Machine Timer;

        private Dictionary<int, int> Data;
        private Message PendingWriteReq;

        private int CurrSeqNum;
        private int I;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Coordinator;

                Console.WriteLine("[Coordinator] Initializing ...\n");

                var numReplicas = ((Tuple<int, Machine>)this.Payload).Item1;
                machine.Monitor = ((Tuple<int, Machine>)this.Payload).Item2;

                machine.Data = new Dictionary<int, int>();
                machine.Replicas = new List<Machine>();

                Runtime.Assert(numReplicas > 0);

                machine.I = 0;
                while (machine.I < numReplicas)
                {
                    machine.Replica = Machine.Factory.CreateMachine<Replica>(this.Machine);
                    machine.Replicas.Insert(machine.I, machine.Replica);
                    machine.I++;
                }

                machine.CurrSeqNum = 0;
                machine.Timer = Machine.Factory.CreateMachine<Timer>(this.Machine);

                this.Raise(new eUnit());
            }
        }

        private class Loop : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Coordinator;

                if (this.Message == typeof(eTimeout))
                {
                    machine.DoGlobalAbort();
                }
            }

            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eRESP_REPLICA_COMMIT),
                    typeof(eRESP_REPLICA_ABORT)
                };
            }
        }

        private class CountVote : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Coordinator;

                Console.WriteLine("[Coordinator] CountVote ...\n");

                if (this.Message == typeof(eRESP_REPLICA_COMMIT))
                {
                    if (machine.CurrSeqNum == (int)this.Payload)
                    {
                        machine.I--;
                    }
                }

                if (machine.I == 0)
                {
                    while (machine.I < machine.Replicas.Count)
                    {
                        this.Send(machine.Replicas[machine.I],
                            new eGLOBAL_COMMIT(machine.CurrSeqNum));
                        machine.I++;
                    }

                    if (machine.Data.ContainsKey(machine.PendingWriteReq.Item1))
                    {
                        machine.Data[machine.PendingWriteReq.Item1] =
                            machine.PendingWriteReq.Item2;
                    }
                    else
                    {
                        machine.Data.Add(machine.PendingWriteReq.Item1,
                            machine.PendingWriteReq.Item2);
                    }

                    this.Send(machine.Monitor, new eMONITOR_WRITE(machine.PendingWriteReq));

                    this.Send(machine.PendingWriteReq.Machine, new eWRITE_SUCCESS());

                    this.Send(machine.Timer, new eCancelTimer());

                    this.Raise(new eUnit());
                }
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eWRITE_REQ)
                };
            }
        }

        private class WaitForCancelTimerResponse : State
        {
            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eRESP_REPLICA_COMMIT),
                    typeof(eRESP_REPLICA_ABORT)
                };
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eWRITE_REQ),
                    typeof(eREAD_REQ)
                };
            }
        }

        private class WaitForTimeout : State
        {
            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eRESP_REPLICA_COMMIT),
                    typeof(eRESP_REPLICA_ABORT)
                };
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eWRITE_REQ),
                    typeof(eREAD_REQ)
                };
            }
        }

        private void DoRead()
        {
            Console.WriteLine("[Coordinator] DoRead ...\n");

            if (this.Data.ContainsKey(((Tuple<Machine, int>)this.Payload).Item2))
            {
                this.Send(this.Monitor, new eMONITOR_READ_SUCCESS(new Tuple<int, int>(
                    ((Tuple<Machine, int>)this.Payload).Item2,
                    this.Data[((Tuple<Machine, int>)this.Payload).Item2])));

                this.Send(((Tuple<Machine, int>)this.Payload).Item1,
                    new eREAD_SUCCESS(this.Data[((Tuple<Machine, int>)this.Payload).Item2]));
            }
            else
            {
                this.Send(this.Monitor, new eMONITOR_READ_UNAVAILABLE(
                    ((Tuple<Machine, int>)this.Payload).Item2));

                this.Send(((Tuple<Machine, int>)this.Payload).Item1,
                    new eREAD_UNAVAILABLE());

                this.DoGlobalAbort();
            }
        }

        private void DoWrite()
        {
            Console.WriteLine("[Coordinator] DoWrite ...\n");

            this.PendingWriteReq = (Message)this.Payload;
            this.CurrSeqNum++;

            this.I = 0;
            while (this.I < this.Replicas.Count)
            {
                this.Send(this.Replicas[this.I],
                    new eREQ_REPLICA(new Tuple<int, int, int>(this.CurrSeqNum,
                    this.PendingWriteReq.Item1, this.PendingWriteReq.Item2)));
                this.I++;
            }

            this.Send(this.Timer, new eStartTimer(100));

            this.Raise(new eUnit());
        }

        private void HandleAbort()
        {
            Console.WriteLine("[Coordinator] HandleAbort ...\n");

            if (this.CurrSeqNum == (int)this.Payload)
            {
                this.DoGlobalAbort();
                this.Send(this.Timer, new eCancelTimer());
                this.Raise(new eUnit());
            }
        }

        private void DoGlobalAbort()
        {
            Console.WriteLine("[Coordinator] GlobalAbort ...\n");

            this.I = 0;
            while (this.I < this.Replicas.Count)
            {
                this.Send(this.Replicas[this.I], new eGLOBAL_ABORT(this.CurrSeqNum));
                this.I++;
            }

            this.Send(this.Client, new eStop());
            this.Send(this.Timer, new eStop());
            this.Send(this.Monitor, new eStop());

            this.Delete();
        }

        private void Update()
        {
            this.Client = (Machine)this.Payload;
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eUnit), typeof(Loop));

            StepStateTransitions loopDict = new StepStateTransitions();
            loopDict.Add(typeof(eUnit), typeof(CountVote));

            StepStateTransitions countVoteDict = new StepStateTransitions();
            countVoteDict.Add(typeof(eRESP_REPLICA_COMMIT), typeof(CountVote));
            countVoteDict.Add(typeof(eTimeout), typeof(Loop));
            countVoteDict.Add(typeof(eUnit), typeof(WaitForCancelTimerResponse));

            StepStateTransitions waitForCancelTimerResponseDict = new StepStateTransitions();
            waitForCancelTimerResponseDict.Add(typeof(eTimeout), typeof(Loop));
            waitForCancelTimerResponseDict.Add(typeof(eCancelTimerSuccess), typeof(Loop));
            waitForCancelTimerResponseDict.Add(typeof(eCancelTimerFailure), typeof(WaitForTimeout));

            StepStateTransitions waitForTimeoutDict = new StepStateTransitions();
            waitForTimeoutDict.Add(typeof(eTimeout), typeof(Loop));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(Loop), loopDict);
            dict.Add(typeof(CountVote), countVoteDict);
            dict.Add(typeof(WaitForCancelTimerResponse), waitForCancelTimerResponseDict);
            dict.Add(typeof(WaitForTimeout), waitForTimeoutDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings loopDict = new ActionBindings();
            loopDict.Add(typeof(eWRITE_REQ), new Action(DoWrite));
            loopDict.Add(typeof(eREAD_REQ), new Action(DoRead));
            loopDict.Add(typeof(eUpdate), new Action(Update));
            loopDict.Add(typeof(eStop), new Action(DoGlobalAbort));

            ActionBindings countVoteDict = new ActionBindings();
            countVoteDict.Add(typeof(eREAD_REQ), new Action(DoRead));
            countVoteDict.Add(typeof(eRESP_REPLICA_ABORT), new Action(HandleAbort));

            dict.Add(typeof(Loop), loopDict);
            dict.Add(typeof(CountVote), countVoteDict);

            return dict;
        }
    }

    internal class Replica : Machine
    {
        private Machine Coordinator;

        private Dictionary<int, int> Data;
        private Tuple<int, int, int> PendingWriteReq;

        private bool ShouldCommit;
        private int LastSeqNum;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Replica;

                Console.WriteLine("[Replica] Initializing ...\n");

                machine.Data = new Dictionary<int, int>();

                machine.Coordinator = (Machine)this.Payload;
                machine.LastSeqNum = 0;

                this.Raise(new eUnit());
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eGLOBAL_ABORT),
                    typeof(eREQ_REPLICA)
                };
            }
        }

        private class Loop : State
        {

        }

        private void HandleReqReplica()
        {
            Console.WriteLine("[Replica] HandleReqReplica ...\n");

            this.PendingWriteReq = (Tuple<int, int, int>)this.Payload;

            Runtime.Assert(this.PendingWriteReq.Item1 > this.LastSeqNum);

            this.ShouldCommit = ShouldCommitWrite();
            if (this.ShouldCommit)
            {
                this.Send(this.Coordinator, new eRESP_REPLICA_COMMIT(this.PendingWriteReq.Item1));
            }
            else
            {
                this.Send(this.Coordinator, new eRESP_REPLICA_ABORT(this.PendingWriteReq.Item1));
            }
        }

        private void HandleGlobalAbort()
        {
            Console.WriteLine("[Replica] Stopping ...\n");

            Runtime.Assert(this.PendingWriteReq.Item1 >= (int)this.Payload);
            if (this.PendingWriteReq.Item1 == (int)this.Payload)
            {
                this.LastSeqNum = (int)this.Payload;
            }

            this.Delete();
        }

        private void HandleGlobalCommit()
        {
            Console.WriteLine("[Replica] HandleGlobalCommit ...\n");

            Runtime.Assert(this.PendingWriteReq.Item1 >= (int)this.Payload);
            if (this.PendingWriteReq.Item1 == (int)this.Payload)
            {
                if (this.Data.ContainsKey(this.PendingWriteReq.Item2))
                    this.Data[this.PendingWriteReq.Item2] = this.PendingWriteReq.Item3;
                else
                    this.Data.Add(this.PendingWriteReq.Item2, this.PendingWriteReq.Item3);
                this.LastSeqNum = (int)this.Payload;
            }
        }

        private bool ShouldCommitWrite()
        {
            return Model.Havoc.Boolean;
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eUnit), typeof(Loop));

            dict.Add(typeof(Init), initDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings loopDict = new ActionBindings();
            loopDict.Add(typeof(eGLOBAL_ABORT), new Action(HandleGlobalAbort));
            loopDict.Add(typeof(eGLOBAL_COMMIT), new Action(HandleGlobalCommit));
            loopDict.Add(typeof(eREQ_REPLICA), new Action(HandleReqReplica));

            dict.Add(typeof(Loop), loopDict);

            return dict;
        }
    }

    internal class Client : Machine
    {
        private Machine Coordinator;

        private int Idx;
        private int Val;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Client;

                Console.WriteLine("[Client] Initializing ...\n");

                machine.Coordinator = (Machine)this.Payload;
                this.Send(machine.Coordinator, new eUpdate(machine));

                this.Raise(new eUnit());
            }
        }

        private class DoWrite : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Client;

                Console.WriteLine("[Client] DoWrite ...\n");

                machine.Idx = machine.ChooseIndex();
                machine.Val = machine.ChooseValue();

                this.Send(machine.Coordinator, new eWRITE_REQ(
                    new Tuple<Machine, int, int>(
                        this.Machine,
                        machine.Idx,
                        machine.Val)));
            }
        }

        private class DoRead : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Client;

                Console.WriteLine("[Client] DoRead ...\n");

                machine.Idx = machine.ChooseIndex();

                this.Send(machine.Coordinator, new eREAD_REQ(
                    new Tuple<Machine, int>(
                        this.Machine,
                        machine.Idx)));
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eWRITE_FAIL)
                };
            }

            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eREAD_UNAVAILABLE)
                };
            }
        }

        private class End : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Client;

                Console.WriteLine("[Client] Stopping ...\n");

                this.Send(machine.Coordinator, new eStop());

                this.Delete();
            }

            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eWRITE_FAIL),
                    typeof(eREAD_FAIL)
                };
            }
        }

        private int ChooseIndex()
        {
            if (Model.Havoc.Boolean)
            {
                Console.WriteLine("[Client] ChooseIndex: 0 ...\n");
                return 0;
            }
            else
            {
                Console.WriteLine("[Client] ChooseIndex: 1 ...\n");
                return 1;
            }
        }

        private int ChooseValue()
        {
            if (Model.Havoc.Boolean)
            {
                Console.WriteLine("[Client] ChooseValue: 0 ...\n");
                return 0;
            }
            else
            {
                Console.WriteLine("[Client] ChooseValue: 1 ...\n");
                return 1;
            }
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eUnit), typeof(DoWrite));

            StepStateTransitions doWriteDict = new StepStateTransitions();
            doWriteDict.Add(typeof(eWRITE_FAIL), typeof(End));
            doWriteDict.Add(typeof(eWRITE_SUCCESS), typeof(DoRead));
            doWriteDict.Add(typeof(eStop), typeof(End));

            StepStateTransitions doReadDict = new StepStateTransitions();
            doReadDict.Add(typeof(eREAD_FAIL), typeof(End));
            doReadDict.Add(typeof(eREAD_SUCCESS), typeof(End));
            doReadDict.Add(typeof(eStop), typeof(End));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(DoWrite), doWriteDict);
            dict.Add(typeof(DoRead), doReadDict);

            return dict;
        }
    }

    internal class Monitor : Machine
    {
        private Dictionary<int, int> Data;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Monitor;

                Console.WriteLine("[Monitor] Initializing ...\n");

                machine.Data = new Dictionary<int, int>();
            }
        }

        private void DoWrite()
        {
            Console.WriteLine("[Monitor] DoWrite ...\n");

            if (this.Data.ContainsKey(((Tuple<int, int>)this.Payload).Item1))
                this.Data[((Tuple<int, int>)this.Payload).Item1] = ((Tuple<int, int>)this.Payload).Item2;
            else
                this.Data.Add(((Tuple<int, int>)this.Payload).Item1, ((Tuple<int, int>)this.Payload).Item2);
        }

        private void CheckReadSuccess()
        {
            Console.WriteLine("[Monitor] CheckReadSuccess ...\n");

            Runtime.Assert(this.Data.ContainsKey(((Tuple<int, int>)this.Payload).Item1));
            Runtime.Assert(this.Data[((Tuple<int, int>)this.Payload).Item1]
                == ((Tuple<int, int>)this.Payload).Item2);
        }

        private void CheckReadUnavailable()
        {
            Console.WriteLine("[Monitor] CheckReadUnavailable ...\n");

            var item = (int)this.Payload;

            Runtime.Assert(!this.Data.ContainsKey(item));
        }

        private void Stop()
        {
            Console.WriteLine("[Monitor] Stopping ...\n");

            this.Delete();
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings initDict = new ActionBindings();
            initDict.Add(typeof(eMONITOR_WRITE), new Action(DoWrite));
            initDict.Add(typeof(eMONITOR_READ_SUCCESS), new Action(CheckReadSuccess));
            initDict.Add(typeof(eMONITOR_READ_UNAVAILABLE), new Action(CheckReadUnavailable));
            initDict.Add(typeof(eStop), new Action(Stop));

            dict.Add(typeof(Init), initDict);

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

                this.Raise(new eUnit());
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
            protected override void OnEntry()
            {
                var machine = this.Machine as Timer;

                if (this.Message == typeof(eCancelTimer))
                {
                    if (Model.Havoc.Boolean)
                    {
                        this.Send(machine.Target, new eCancelTimerFailure());

                        this.Send(machine.Target, new eTimeout());
                    }
                    else
                    {
                        this.Send(machine.Target, new eCancelTimerSuccess());
                    }
                }
            }

            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eCancelTimer)
                };
            }
        }

        private class TimerStarted : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Timer;

                Console.WriteLine("[Timer] Started ...\n");

                if (Model.Havoc.Boolean)
                {
                    this.Send(machine.Target, new eTimeout());
                    this.Raise(new eUnit());
                }
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
            initDict.Add(typeof(eUnit), typeof(Loop));

            StepStateTransitions loopDict = new StepStateTransitions();
            loopDict.Add(typeof(eStartTimer), typeof(TimerStarted));

            StepStateTransitions timerStartedDict = new StepStateTransitions();
            timerStartedDict.Add(typeof(eUnit), typeof(Loop));
            timerStartedDict.Add(typeof(eCancelTimer), typeof(Loop));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(Loop), loopDict);
            dict.Add(typeof(TimerStarted), timerStartedDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings loopDict = new ActionBindings();
            loopDict.Add(typeof(eStop), new Action(Stop));

            ActionBindings timerStartedDict = new ActionBindings();
            timerStartedDict.Add(typeof(eStop), new Action(Stop));

            dict.Add(typeof(Loop), loopDict);
            dict.Add(typeof(TimerStarted), timerStartedDict);

            return dict;
        }
    }

    #endregion
}
