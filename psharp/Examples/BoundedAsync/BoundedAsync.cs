//-----------------------------------------------------------------------
// <copyright file="BoundedAsync.cs" company="Microsoft">
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

namespace BoundedAsync
{
    #region Events

    internal class eUnit : Event { }
    internal class eReq : Event { }
    internal class eResp : Event { }
    internal class eDone : Event { }

    internal class eInit : Event
    {
        public eInit(Object payload)
            : base(payload)
        { }
    }

    internal class eMyCount : Event
    {
        public eMyCount(Object payload)
            : base(payload)
        { }
    }

    #endregion

    #region Machines

    [Main]
    internal class Scheduler : Machine
    {
        private Machine Process1;
        private Machine Process2;
        private Machine Process3;
        private int Count;
        private int DoneCounter;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("Initializing Scheduler");

                (this.Machine as Scheduler).Process1 =
                    Machine.Factory.CreateMachine<Process>(this.Machine);
                (this.Machine as Scheduler).Process2 =
                    Machine.Factory.CreateMachine<Process>(this.Machine);
                (this.Machine as Scheduler).Process3 =
                    Machine.Factory.CreateMachine<Process>(this.Machine);

                Console.WriteLine("{0} sending event {1} to {2}",
                    this.Machine, typeof(eInit), (this.Machine as Scheduler).Process1);
                this.Send((this.Machine as Scheduler).Process1,
                    new eInit(new Tuple<Machine, Machine>(
                        (this.Machine as Scheduler).Process2,
                        (this.Machine as Scheduler).Process3)
                        ));

                Console.WriteLine("{0} sending event {1} to {2}",
                    this.Machine, typeof(eInit), (this.Machine as Scheduler).Process1);
                this.Send((this.Machine as Scheduler).Process2,
                    new eInit(new Tuple<Machine, Machine>(
                        (this.Machine as Scheduler).Process1,
                        (this.Machine as Scheduler).Process3)
                        ));

                Console.WriteLine("{0} sending event {1} to {2}",
                    this.Machine, typeof(eInit), (this.Machine as Scheduler).Process1);
                this.Send((this.Machine as Scheduler).Process3,
                    new eInit(new Tuple<Machine, Machine>(
                        (this.Machine as Scheduler).Process1,
                        (this.Machine as Scheduler).Process2)
                        ));

                (this.Machine as Scheduler).Count = 0;
                Console.WriteLine("Scheduler: Count: {0}", (this.Machine as Scheduler).Count);

                (this.Machine as Scheduler).DoneCounter = 3;

                Console.WriteLine("{0} raising event {1}", this.Machine, typeof(eResp));
                this.Raise(new eUnit());
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eReq)
                };
            }
        }

        private class Sync : State
        {
            protected override void OnExit()
            {
                Console.WriteLine("{0} sending event {1} to {2}",
                    this.Machine, typeof(eResp), (this.Machine as Scheduler).Process1);
                this.Send((this.Machine as Scheduler).Process1, new eResp());

                Console.WriteLine("{0} sending event {1} to {2}",
                    this.Machine, typeof(eResp), (this.Machine as Scheduler).Process1);
                this.Send((this.Machine as Scheduler).Process2, new eResp());

                Console.WriteLine("{0} sending event {1} to {2}",
                    this.Machine, typeof(eResp), (this.Machine as Scheduler).Process1);
                this.Send((this.Machine as Scheduler).Process3, new eResp());
            }
        }

        private class Done : State
        {
            protected override void OnEntry()
            {
                this.Delete();
            }
        }

        private void CountReq()
        {
            this.Count++;
            Console.WriteLine("Scheduler: Count: {0}", this.Count);

            if (this.Count == 3)
            {
                this.Count = 0;
                Console.WriteLine("Scheduler: Count: {0}", this.Count);

                Console.WriteLine("{0} raising event {1}", this, typeof(eResp));
                this.Raise(new eResp());
            }
        }

        private void CheckIfDone()
        {
            this.DoneCounter--;

            if (this.DoneCounter == 0)
            {
                Console.WriteLine("Scheduler: Done");
                this.Delete();
            }
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eUnit), typeof(Sync));

            StepStateTransitions syncDict = new StepStateTransitions();
            syncDict.Add(typeof(eResp), typeof(Sync));
            syncDict.Add(typeof(eUnit), typeof(Done));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(Sync), syncDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings syncDict = new ActionBindings();
            syncDict.Add(typeof(eReq), new Action(CountReq));
            syncDict.Add(typeof(eDone), new Action(CheckIfDone));

            dict.Add(typeof(Sync), syncDict);

            return dict;
        }
    }

    internal class Process : Machine
    {
        private int Count;
        private Machine Scheduler;
        private Machine LeftProcess;
        private Machine RightProcess;

        [Initial]
        private class _Init : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("Initializing Process");

                (this.Machine as Process).Scheduler = (Machine)this.Payload;

                Console.WriteLine("{0} raising event {1}", this.Machine, typeof(eUnit));
                this.Raise(new eUnit());
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eInit)
                };
            }
        }

        private class Init : State
        {
            protected override void OnEntry()
            {
                (this.Machine as Process).Count = 0;
                Console.WriteLine("Process: Count: {0}", (this.Machine as Process).Count);
            }
        }

        private class SendCount : State
        {
            protected override void OnEntry()
            {
                (this.Machine as Process).Count++;
                Console.WriteLine("Process: Count: {0}", (this.Machine as Process).Count);

                Console.WriteLine("{0} sending event {1} to {2}",
                    this.Machine, typeof(eMyCount), (this.Machine as Process).LeftProcess);
                this.Send((this.Machine as Process).LeftProcess,
                    new eMyCount((this.Machine as Process).Count));

                Console.WriteLine("{0} sending event {1} to {2}",
                    this.Machine, typeof(eMyCount), (this.Machine as Process).RightProcess);
                this.Send((this.Machine as Process).RightProcess,
                    new eMyCount((this.Machine as Process).Count));

                Console.WriteLine("{0} sending event {1} to {2}",
                    this.Machine, typeof(eReq), (this.Machine as Process).Scheduler);
                this.Send((this.Machine as Process).Scheduler, new eReq());

                if ((this.Machine as Process).Count > 10)
                {
                    this.Raise(new eUnit());
                }
            }
        }

        private class Done : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("Process: Done");
                this.Send((this.Machine as Process).Scheduler, new eDone());
                this.Delete();
            }

            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eResp),
                    typeof(eMyCount)
                };
            }
        }

        private void InitAction()
        {
            Console.WriteLine("Process: Performing InitAction");

            this.LeftProcess = ((Tuple<Machine, Machine>)this.Payload).Item1;
            this.RightProcess = ((Tuple<Machine, Machine>)this.Payload).Item2;

            Console.WriteLine("{0} sending event {1} to {2}",
                    this, typeof(eReq), this.Scheduler);
            this.Send(this.Scheduler, new eReq());
        }

        private void ConfirmThatInSync()
        {
            Console.WriteLine("Process: Performing ConfirmThatInSync");
            Runtime.Assert((this.Count <= (int)this.Payload) &&
                (this.Count >= ((int)this.Payload - 1)));
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions _initDict = new StepStateTransitions();
            _initDict.Add(typeof(eUnit), typeof(Init));

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eMyCount), typeof(Init));
            initDict.Add(typeof(eResp), typeof(SendCount));

            StepStateTransitions sendCountDict = new StepStateTransitions();
            sendCountDict.Add(typeof(eUnit), typeof(Done));
            sendCountDict.Add(typeof(eResp), typeof(SendCount));

            dict.Add(typeof(_Init), _initDict);
            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(SendCount), sendCountDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings initDict = new ActionBindings();
            initDict.Add(typeof(eInit), new Action(InitAction));

            ActionBindings sendCountDict = new ActionBindings();
            sendCountDict.Add(typeof(eMyCount), new Action(ConfirmThatInSync));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(SendCount), sendCountDict);

            return dict;
        }
    }

    #endregion
}
