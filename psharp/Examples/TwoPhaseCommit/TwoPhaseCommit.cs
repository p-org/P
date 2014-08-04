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

    internal class eWRITE_FAIL : Event { }
    internal class eWRITE_SUCCESS : Event { }
    internal class eREAD_FAIL : Event { }
    internal class eREAD_UNAVAILABLE : Event { }
    internal class eUnit : Event { }
    internal class eTimeout : Event { }
    internal class eCancelTimer : Event { }
    internal class eCancelTimerFailure : Event { }
    internal class eCancelTimerSuccess : Event { }

    #endregion

    #region Machines

    [Ghost]
    internal class Timer : Machine
    {
        private Machine Target;

        [Initial]
        private class Init : State
        {
            public override void OnEntry()
            {
                Console.WriteLine("Initializing Timer ...");

                (this.Machine as Timer).Target = (Machine)this.Payload;
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
            public override void OnEntry()
            {
                if (this.Message == typeof(eCancelTimer))
                {
                    if (Model.Havoc.Boolean)
                    {
                        Console.WriteLine("{0} sending event {1} to {2}", this.Machine, typeof(eCancelTimerFailure),
                            (this.Machine as Timer).Target);
                        this.Send((this.Machine as Timer).Target, new eCancelTimerFailure());

                        Console.WriteLine("{0} sending event {1} to {2}", this.Machine, typeof(eTimeout),
                            (this.Machine as Timer).Target);
                        this.Send((this.Machine as Timer).Target, new eTimeout());
                    }
                    else
                    {
                        Console.WriteLine("{0} sending event {1} to {2}", this.Machine, typeof(eCancelTimerSuccess),
                            (this.Machine as Timer).Target);
                        this.Send((this.Machine as Timer).Target, new eCancelTimerSuccess());
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
            public override void OnEntry()
            {
                if (Model.Havoc.Boolean)
                {
                    Console.WriteLine("{0} sending event {1} to {2}", this.Machine, typeof(eTimeout),
                        (this.Machine as Timer).Target);
                    this.Send((this.Machine as Timer).Target, new eTimeout());
                    this.Raise(new eUnit());
                }
            }
        }

        protected override Dictionary<Type, StateTransitions> DefineStepTransitions()
        {
            Dictionary<Type, StateTransitions> dict = new Dictionary<Type, StateTransitions>();

            StateTransitions initDict = new StateTransitions();
            initDict.Add(typeof(eUnit), typeof(Loop));

            StateTransitions loopDict = new StateTransitions();
            loopDict.Add(typeof(eStartTimer), typeof(TimerStarted));

            StateTransitions timerStartedDict = new StateTransitions();
            timerStartedDict.Add(typeof(eUnit), typeof(Loop));
            timerStartedDict.Add(typeof(eCancelTimer), typeof(Loop));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(Loop), loopDict);
            dict.Add(typeof(TimerStarted), timerStartedDict);

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
            public override void OnEntry()
            {
                Console.WriteLine("Initializing Replica ...");

                (this.Machine as Replica).Data = new Dictionary<int, int>();

                (this.Machine as Replica).Coordinator = (Machine)this.Payload;
                (this.Machine as Replica).LastSeqNum = 0;

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
            Console.WriteLine("Replica: HandleReqReplica");

            this.PendingWriteReq = (Tuple<int, int, int>)this.Payload;

            Runtime.Assert(this.PendingWriteReq.Item1 > this.LastSeqNum);

            this.ShouldCommit = ShouldCommitWrite();
            if (this.ShouldCommit)
            {
                Console.WriteLine("{0} sending event {1} to {2}", this, typeof(eRESP_REPLICA_COMMIT),
                    this.Coordinator);
                this.Send(this.Coordinator, new eRESP_REPLICA_COMMIT(this.PendingWriteReq.Item1));
            }
            else
            {
                Console.WriteLine("{0} sending event {1} to {2}", this, typeof(eRESP_REPLICA_ABORT),
                    this.Coordinator);
                this.Send(this.Coordinator, new eRESP_REPLICA_ABORT(this.PendingWriteReq.Item1));
            }
        }

        private void HandleGlobalAbort()
        {
            Console.WriteLine("Replica: HandleGlobalAbort");

            Runtime.Assert(this.PendingWriteReq.Item1 >= (int)this.Payload);
            if (this.PendingWriteReq.Item1 == (int)this.Payload)
            {
                this.LastSeqNum = (int)this.Payload;
            }
        }

        private void HandleGlobalCommit()
        {
            Console.WriteLine("Replica: HandleGlobalCommit");

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

        [Ghost]
        private bool ShouldCommitWrite()
        {
            return Model.Havoc.Boolean;
        }

        protected override Dictionary<Type, StateTransitions> DefineStepTransitions()
        {
            Dictionary<Type, StateTransitions> dict = new Dictionary<Type, StateTransitions>();

            StateTransitions initDict = new StateTransitions();
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

    internal class Coordinator : Machine
    {
        private Dictionary<int, int> Data;
        private List<Machine> Replicas;
        private int NumReplicas;
        private int I;
        private Tuple<Machine, int, int> PendingWriteReq;
        private Machine Replica;
        private int CurrSeqNum;
        private Machine Timer;

        [Initial]
        private class Init : State
        {
            public override void OnEntry()
            {
                Console.WriteLine("Initializing Coordinator ...");

                (this.Machine as Coordinator).Data = new Dictionary<int, int>();
                (this.Machine as Coordinator).Replicas = new List<Machine>();

                (this.Machine as Coordinator).NumReplicas = (int)this.Payload;
                Runtime.Assert((this.Machine as Coordinator).NumReplicas > 0);

                (this.Machine as Coordinator).I = 0;
                while ((this.Machine as Coordinator).I < (this.Machine as Coordinator).NumReplicas)
                {
                    (this.Machine as Coordinator).Replica =
                        Machine.Factory.CreateMachine<Replica>(this.Machine);
                    (this.Machine as Coordinator).Replicas.Insert(
                        (this.Machine as Coordinator).I, (this.Machine as Coordinator).Replica);
                    (this.Machine as Coordinator).I++;
                }

                (this.Machine as Coordinator).CurrSeqNum = 0;
                (this.Machine as Coordinator).Timer =
                    Machine.Factory.CreateMachine<Timer>(this.Machine);

                this.Raise(new eUnit());
            }
        }

        private class Loop : State
        {
            public override void OnEntry()
            {
                if (this.Message == typeof(eTimeout))
                {
                    (this.Machine as Coordinator).DoGlobalAbort();
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
            public override void OnEntry()
            {
                if (this.Message == typeof(eRESP_REPLICA_COMMIT))
                {
                    if ((this.Machine as Coordinator).CurrSeqNum == (int)this.Payload)
                    {
                        (this.Machine as Coordinator).I--;
                    }
                }

                if ((this.Machine as Coordinator).I == 0)
                {
                    while ((this.Machine as Coordinator).I < (this.Machine as Coordinator).Replicas.Count)
                    {
                        Console.WriteLine("{0} sending event {1} to {2}", this.Machine, typeof(eGLOBAL_COMMIT),
                            (this.Machine as Coordinator).Replicas[(this.Machine as Coordinator).I]);
                        this.Send((this.Machine as Coordinator).Replicas[(this.Machine as Coordinator).I],
                            new eGLOBAL_COMMIT((this.Machine as Coordinator).CurrSeqNum));
                        (this.Machine as Coordinator).I++;
                    }

                    if ((this.Machine as Coordinator).Data.ContainsKey(
                        (this.Machine as Coordinator).PendingWriteReq.Item2))
                    {
                        (this.Machine as Coordinator).Data[(this.Machine as Coordinator).PendingWriteReq.Item2] =
                            (this.Machine as Coordinator).PendingWriteReq.Item3;
                    }
                    else
                    {
                        (this.Machine as Coordinator).Data.Add((this.Machine as Coordinator).PendingWriteReq.Item2,
                            (this.Machine as Coordinator).PendingWriteReq.Item3);
                    }

                    Console.WriteLine("{0} sending event {1} to monitor {2}", this,
                        typeof(eMONITOR_WRITE), typeof(M));
                    Runtime.Invoke<M>(new eMONITOR_WRITE(new Tuple<int, int>(
                        (this.Machine as Coordinator).PendingWriteReq.Item2,
                        (this.Machine as Coordinator).PendingWriteReq.Item3)));

                    Console.WriteLine("{0} sending event {1} to {2}", this.Machine, typeof(eWRITE_SUCCESS),
                        (this.Machine as Coordinator).PendingWriteReq.Item1);
                    this.Send((this.Machine as Coordinator).PendingWriteReq.Item1,
                        new eWRITE_SUCCESS());

                    Console.WriteLine("{0} sending event {1} to {2}", this.Machine, typeof(eCancelTimer),
                        (this.Machine as Coordinator).Timer);
                    this.Send((this.Machine as Coordinator).Timer,
                        new eCancelTimer());

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
            Console.WriteLine("Coordinator: DoRead");

            if (this.Data.ContainsKey(((Tuple<Machine, int>)this.Payload).Item2))
            {
                Console.WriteLine("{0} sending event {1} to monitor {2}", this,
                    typeof(eMONITOR_READ_SUCCESS), typeof(M));
                Runtime.Invoke<M>(new eMONITOR_READ_SUCCESS(new Tuple<int, int>(
                    ((Tuple<Machine, int>)this.Payload).Item2,
                    this.Data[((Tuple<Machine, int>)this.Payload).Item2])));

                Console.WriteLine("{0} sending event {1} to {2}", this, typeof(eREAD_SUCCESS),
                    ((Tuple<Machine, int>)this.Payload).Item1);
                this.Send(((Tuple<Machine, int>)this.Payload).Item1,
                    new eREAD_SUCCESS(this.Data[((Tuple<Machine, int>)this.Payload).Item2]));
            }
            else
            {
                Console.WriteLine("{0} sending event {1} to monitor {2}", this,
                    typeof(eMONITOR_READ_UNAVAILABLE), typeof(M));
                Runtime.Invoke<M>(new eMONITOR_READ_UNAVAILABLE(
                    ((Tuple<Machine, int>)this.Payload).Item2));

                Console.WriteLine("{0} sending event {1} to {2}", this, typeof(eREAD_UNAVAILABLE),
                    ((Tuple<Machine, int>)this.Payload).Item1);
                this.Send(((Tuple<Machine, int>)this.Payload).Item1,
                    new eREAD_UNAVAILABLE());
            }
        }

        private void DoWrite()
        {
            Console.WriteLine("Coordinator: DoWrite");

            this.PendingWriteReq = (Tuple<Machine, int, int>)this.Payload;
            this.CurrSeqNum++;

            this.I = 0;
            while (this.I < this.Replicas.Count)
            {
                Console.WriteLine("{0} sending event {1} to {2}", this, typeof(eREQ_REPLICA),
                    this.Replicas[this.I]);
                this.Send(this.Replicas[this.I],
                    new eREQ_REPLICA(new Tuple<int, int, int>(
                        this.CurrSeqNum, this.PendingWriteReq.Item2, this.PendingWriteReq.Item3)));
                this.I++;
            }

            Console.WriteLine("{0} sending event {1} to {2}", this, typeof(eStartTimer), this.Timer);
            this.Send(this.Timer, new eStartTimer(100));

            this.Raise(new eUnit());
        }

        private void DoGlobalAbort()
        {
            this.I = 0;
            while (this.I < this.Replicas.Count)
            {
                Console.WriteLine("{0} sending event {1} to {2}", this, typeof(eGLOBAL_ABORT),
                    this.Replicas[this.I]);
                this.Send(this.Replicas[this.I], new eGLOBAL_ABORT(this.CurrSeqNum));
                this.I++;
            }

            Console.WriteLine("{0} sending event {1} to {2}", this, typeof(eWRITE_FAIL),
                    this.PendingWriteReq.Item1);
            this.Send(this.PendingWriteReq.Item1, new eWRITE_FAIL());
        }

        private void HandleAbort()
        {
            if (this.CurrSeqNum == (int)this.Payload)
            {
                this.DoGlobalAbort();
                Console.WriteLine("{0} sending event {1} to {2}", this, typeof(eCancelTimer),
                    this.Timer);
                this.Send(this.Timer, new eCancelTimer());
                this.Raise(new eUnit());
            }
        }

        protected override Dictionary<Type, StateTransitions> DefineStepTransitions()
        {
            Dictionary<Type, StateTransitions> dict = new Dictionary<Type, StateTransitions>();

            StateTransitions initDict = new StateTransitions();
            initDict.Add(typeof(eUnit), typeof(Loop));

            StateTransitions loopDict = new StateTransitions();
            loopDict.Add(typeof(eUnit), typeof(CountVote));

            StateTransitions countVoteDict = new StateTransitions();
            countVoteDict.Add(typeof(eRESP_REPLICA_COMMIT), typeof(CountVote));
            countVoteDict.Add(typeof(eTimeout), typeof(Loop));
            countVoteDict.Add(typeof(eUnit), typeof(WaitForCancelTimerResponse));

            StateTransitions waitForCancelTimerResponseDict = new StateTransitions();
            waitForCancelTimerResponseDict.Add(typeof(eTimeout), typeof(Loop));
            waitForCancelTimerResponseDict.Add(typeof(eCancelTimerSuccess), typeof(Loop));
            waitForCancelTimerResponseDict.Add(typeof(eCancelTimerFailure), typeof(WaitForTimeout));

            StateTransitions waitForTimeoutDict = new StateTransitions();
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

            ActionBindings countVoteDict = new ActionBindings();
            countVoteDict.Add(typeof(eREAD_REQ), new Action(DoRead));
            countVoteDict.Add(typeof(eRESP_REPLICA_ABORT), new Action(HandleAbort));

            dict.Add(typeof(Loop), loopDict);
            dict.Add(typeof(CountVote), countVoteDict);

            return dict;
        }
    }

    [Ghost]
    internal class Client : Machine
    {
        private Machine Coordinator;
        private int Idx;
        private int Val;

        [Initial]
        private class Init : State
        {
            public override void OnEntry()
            {
                Console.WriteLine("Initializing Client ...");

                (this.Machine as Client).Coordinator = (Machine)this.Payload;

                this.Raise(new eUnit());
            }
        }

        private class DoWrite : State
        {
            public override void OnEntry()
            {
                Console.WriteLine("Client: DoWrite");

                (this.Machine as Client).Idx = (this.Machine as Client).ChooseIndex();
                (this.Machine as Client).Val = (this.Machine as Client).ChooseValue();

                Console.WriteLine("{0} sending event {1} to {2}", this, typeof(eWRITE_REQ),
                    (this.Machine as Client).Coordinator);
                this.Send((this.Machine as Client).Coordinator, new eWRITE_REQ(
                    new Tuple<Machine, int, int>(
                        this.Machine,
                        (this.Machine as Client).Idx,
                        (this.Machine as Client).Val)));
            }
        }

        private class DoRead : State
        {
            public override void OnEntry()
            {
                Console.WriteLine("Client: DoRead");

                (this.Machine as Client).Idx = (this.Machine as Client).ChooseIndex();

                Console.WriteLine("{0} sending event {1} to {2}", this, typeof(eREAD_REQ),
                    (this.Machine as Client).Coordinator);
                this.Send((this.Machine as Client).Coordinator, new eREAD_REQ(
                    new Tuple<Machine, int>(
                        this.Machine,
                        (this.Machine as Client).Idx)));
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
            public override void OnEntry()
            {
                Console.WriteLine("Client: Finished");
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
                Console.WriteLine("Client: ChooseIndex: 0");
                return 0;
            }
            else
            {
                Console.WriteLine("Client: ChooseIndex: 1");
                return 1;
            }
        }

        private int ChooseValue()
        {
            if (Model.Havoc.Boolean)
            {
                Console.WriteLine("Client: ChooseValue: 0");
                return 0;
            }
            else
            {
                Console.WriteLine("Client: ChooseValue: 1");
                return 1;
            }
        }

        protected override Dictionary<Type, StateTransitions> DefineStepTransitions()
        {
            Dictionary<Type, StateTransitions> dict = new Dictionary<Type, StateTransitions>();

            StateTransitions initDict = new StateTransitions();
            initDict.Add(typeof(eUnit), typeof(DoWrite));

            StateTransitions doWriteDict = new StateTransitions();
            doWriteDict.Add(typeof(eWRITE_FAIL), typeof(End));
            doWriteDict.Add(typeof(eWRITE_SUCCESS), typeof(DoRead));

            StateTransitions doReadDict = new StateTransitions();
            doReadDict.Add(typeof(eREAD_FAIL), typeof(End));
            doReadDict.Add(typeof(eREAD_SUCCESS), typeof(End));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(DoWrite), doWriteDict);
            dict.Add(typeof(DoRead), doReadDict);

            return dict;
        }
    }

    [Monitor]
    internal class M : Machine
    {
        private Dictionary<int, int> Data;

        [Initial]
        private class Init : State
        {
            public override void OnEntry()
            {
                Console.WriteLine("Initializing Monitor ...");
                (this.Machine as M).Data = new Dictionary<int, int>();
            }
        }

        private void DoWrite()
        {
            Console.WriteLine("Monitor: DoWrite");

            if (this.Data.ContainsKey(((Tuple<int, int>)this.Payload).Item1))
                this.Data[((Tuple<int, int>)this.Payload).Item1] = ((Tuple<int, int>)this.Payload).Item2;
            else
                this.Data.Add(((Tuple<int, int>)this.Payload).Item1, ((Tuple<int, int>)this.Payload).Item2);
        }

        private void CheckReadSuccess()
        {
            Console.WriteLine("Monitor: CheckReadSuccess");

            Runtime.Assert(this.Data.ContainsKey(((Tuple<int, int>)this.Payload).Item1));
            Runtime.Assert(this.Data[((Tuple<int, int>)this.Payload).Item1]
                == ((Tuple<int, int>)this.Payload).Item2);
        }

        private void CheckReadUnavailable()
        {
            Console.WriteLine("Monitor: CheckReadUnavailable");

            Runtime.Assert(!this.Data.ContainsKey(((Tuple<int, int>)this.Payload).Item1));
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings initDict = new ActionBindings();
            initDict.Add(typeof(eMONITOR_WRITE), new Action(DoWrite));
            initDict.Add(typeof(eMONITOR_READ_SUCCESS), new Action(CheckReadSuccess));
            initDict.Add(typeof(eMONITOR_READ_UNAVAILABLE), new Action(CheckReadUnavailable));

            dict.Add(typeof(Init), initDict);

            return dict;
        }
    }

    [Main]
    [Ghost]
    internal class TwoPhaseCommit : Machine
    {
        private Machine Coordinator;
        private Machine Client;

        [Initial]
        private class Init : State
        {
            public override void OnEntry()
            {
                Machine.Factory.CreateMonitor<M>();

                (this.Machine as TwoPhaseCommit).Coordinator =
                    Machine.Factory.CreateMachine<Coordinator>(2);

                (this.Machine as TwoPhaseCommit).Client =
                    Machine.Factory.CreateMachine<Client>(
                    (this.Machine as TwoPhaseCommit).Coordinator);

                (this.Machine as TwoPhaseCommit).Client =
                    Machine.Factory.CreateMachine<Client>(
                    (this.Machine as TwoPhaseCommit).Coordinator);
            }
        }
    }

    #endregion

    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements a replication system.
    /// </summary>
    class TwoPhaseCommitExample
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Registering events to the runtime.\n");
            Runtime.RegisterNewEvent(typeof(eREQ_REPLICA));
            Runtime.RegisterNewEvent(typeof(eRESP_REPLICA_COMMIT));
            Runtime.RegisterNewEvent(typeof(eRESP_REPLICA_ABORT));
            Runtime.RegisterNewEvent(typeof(eGLOBAL_ABORT));
            Runtime.RegisterNewEvent(typeof(eGLOBAL_COMMIT));
            Runtime.RegisterNewEvent(typeof(eWRITE_REQ));
            Runtime.RegisterNewEvent(typeof(eWRITE_FAIL));
            Runtime.RegisterNewEvent(typeof(eWRITE_SUCCESS));
            Runtime.RegisterNewEvent(typeof(eREAD_REQ));
            Runtime.RegisterNewEvent(typeof(eREAD_FAIL));
            Runtime.RegisterNewEvent(typeof(eREAD_UNAVAILABLE));
            Runtime.RegisterNewEvent(typeof(eREAD_SUCCESS));
            Runtime.RegisterNewEvent(typeof(eUnit));
            Runtime.RegisterNewEvent(typeof(eTimeout));
            Runtime.RegisterNewEvent(typeof(eStartTimer));
            Runtime.RegisterNewEvent(typeof(eCancelTimer));
            Runtime.RegisterNewEvent(typeof(eCancelTimerFailure));
            Runtime.RegisterNewEvent(typeof(eCancelTimerSuccess));
            Runtime.RegisterNewEvent(typeof(eMONITOR_WRITE));
            Runtime.RegisterNewEvent(typeof(eMONITOR_READ_SUCCESS));
            Runtime.RegisterNewEvent(typeof(eMONITOR_READ_UNAVAILABLE));

            Console.WriteLine("Registering state machines to the runtime.\n");
            Runtime.RegisterNewMachine(typeof(Timer));
            Runtime.RegisterNewMachine(typeof(Replica));
            Runtime.RegisterNewMachine(typeof(Coordinator));
            Runtime.RegisterNewMachine(typeof(Client));
            Runtime.RegisterNewMachine(typeof(TwoPhaseCommit));

            Console.WriteLine("Registering monitors to the runtime.\n");
            Runtime.RegisterNewMonitor(typeof(M));

            Console.WriteLine("Configuring the runtime.\n");
            Runtime.Options.Mode = Runtime.Mode.BugFinding;

            Console.WriteLine("Starting the runtime.\n");
            Runtime.Start();
            Runtime.Wait();

            Console.WriteLine("Performing cleanup.\n");
            Runtime.Dispose();
        }
    }
}
