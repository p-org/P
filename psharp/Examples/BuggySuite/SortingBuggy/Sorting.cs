using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace SortingBuggy
{
    #region Events

    internal class eLocal : Event { }

    internal class eStart : Event
    {
        public eStart(Object payload)
            : base(payload)
        { }
    }

    internal class eUpdate : Event
    {
        public eUpdate(Object payload)
            : base(payload)
        { }
    }

    internal class eNotifyLeft : Event
    {
        public eNotifyLeft(Object payload)
            : base(payload)
        { }
    }

    internal class eNotifyRight : Event
    {
        public eNotifyRight(Object payload)
            : base(payload)
        { }
    }

    internal class eNotifyMonitor : Event
    {
        public eNotifyMonitor(Object payload)
            : base(payload)
        { }
    }

    #endregion

    #region Machines

    [Main]
    internal class Master : Machine
    {
        private List<Machine> SProcesses;
        private Machine Monitor;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Master;

                Console.WriteLine("[Master] Initializing ...\n");

                machine.SProcesses = new List<Machine>();

                var list = ((Tuple<List<int>, int>)this.Payload).Item1;
                var counter = ((Tuple<List<int>, int>)this.Payload).Item2;
                var n = list.Count;

                machine.Monitor = Machine.Factory.CreateMachine<SortingMonitor>(
                    new Tuple<int, int>(n, counter));

                for (int idx = 0; idx < n; idx++)
                {
                    machine.SProcesses.Add(Machine.Factory.CreateMachine<SProcess>(
                        new Tuple<int, Machine, Machine>(idx + 1, machine, machine.Monitor)));
                }

                for (int idx = 0; idx < n; idx++)
                {
                    if (idx == 0)
                    {
                        this.Send(machine.SProcesses[idx], new eStart(new Tuple<Machine, Machine, int>(
                            null, machine.SProcesses[idx + 1], list[idx])));
                    }
                    else if (idx == (n - 1))
                    {
                        this.Send(machine.SProcesses[idx], new eStart(new Tuple<Machine, Machine, int>(
                            machine.SProcesses[idx - 1], null, list[idx])));
                    }
                    else
                    {
                        this.Send(machine.SProcesses[idx], new eStart(new Tuple<Machine, Machine, int>(
                            machine.SProcesses[idx - 1], machine.SProcesses[idx + 1], list[idx])));
                    }
                }

                this.Delete();
            }
        }
    }

    internal class SProcess : Machine
    {
        private int Id;

        private Machine Master;
        private Machine Left;
        private Machine Right;

        private Machine Monitor;

        private int Value;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as SProcess;

                machine.Id = ((Tuple<int, Machine, Machine>)this.Payload).Item1;
                machine.Master = ((Tuple<int, Machine, Machine>)this.Payload).Item2;
                machine.Monitor = ((Tuple<int, Machine, Machine>)this.Payload).Item3;

                Console.WriteLine("[SProcess-{0}] Initializing ...\n", machine.Id);
            }
        }

        private class Running : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as SProcess;

                Console.WriteLine("[SProcess-{0}] Running ...\n", machine.Id);

                machine.Left = ((Tuple<Machine, Machine, int>)this.Payload).Item1;
                machine.Right = ((Tuple<Machine, Machine, int>)this.Payload).Item2;
                machine.Value = ((Tuple<Machine, Machine, int>)this.Payload).Item3;

                this.Send(machine.Monitor, new eNotifyMonitor(new Tuple<int, int>(
                    machine.Id, machine.Value)));

                if (machine.Left != null)
                {
                    this.Send(machine.Left, new eNotifyLeft(machine.Value));
                }

                if (machine.Right != null)
                {
                    this.Send(machine.Right, new eNotifyRight(machine.Value));
                }
            }
        }

        private void Update()
        {
            Console.WriteLine("[SProcess-{0}] Updating ...\n", this.Id);

            this.Value = (int)this.Payload;

            if (this.Left != null)
            {
                this.Send(this.Left, new eNotifyLeft(this.Value));
            }

            if (this.Right != null)
            {
                this.Send(this.Right, new eNotifyRight(this.Value));
            }
        }

        private void ProcessLeft()
        {
            Console.WriteLine("[SProcess-{0}] Processing left ...\n", this.Id);

            int v = (int)this.Payload;

            if (this.Value > v)
            {
                this.Send(this.Right, new eUpdate(this.Value));
                this.Raise(new eUpdate(v));
            }
            else
            {
                this.Send(this.Monitor, new eNotifyMonitor(new Tuple<int, int>(
                    this.Id, this.Value)));
            }
        }

        private void ProcessRight()
        {
            Console.WriteLine("[SProcess-{0}] Processing right ...\n", this.Id);

            int v = (int)this.Payload;

            if (v > this.Value)
            {
                this.Send(this.Left, new eUpdate(this.Value));
                this.Raise(new eUpdate(v));
            }
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eStart), typeof(Running));

            dict.Add(typeof(Init), initDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings runningDict = new ActionBindings();
            runningDict.Add(typeof(eUpdate), new Action(Update));
            runningDict.Add(typeof(eNotifyLeft), new Action(ProcessLeft));
            runningDict.Add(typeof(eNotifyRight), new Action(ProcessRight));

            dict.Add(typeof(Running), runningDict);

            return dict;
        }
    }

    internal class SortingMonitor : Machine
    {
        private int[] Values;
        private int Counter;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as SortingMonitor;

                var n = ((Tuple<int, int>)this.Payload).Item1;
                machine.Counter = ((Tuple<int, int>)this.Payload).Item2;

                Console.WriteLine("[SortingMonitor] Initializing ...\n");

                machine.Values = new int[n];

                this.Raise(new eLocal());
            }
        }

        private class Waiting : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("[SortingMonitor] Waiting ...\n");
            }
        }

        private void Check()
        {
            Console.WriteLine("[SortingMonitor] Checking step {0} ...\n", this.Counter);

            var id = ((Tuple<int, int>)this.Payload).Item1 - 1;
            var v = ((Tuple<int, int>)this.Payload).Item2;

            this.Values[id] = v;
            this.Counter = this.Counter - 1;

            if (this.Counter == 0)
            {
                for (int idx = 0; idx < this.Values.Length - 1; idx++)
                {
                    Runtime.Assert(this.Values[idx] > this.Values[idx + 1], "Assertion failed.");
                }
            }
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(Waiting));

            dict.Add(typeof(Init), initDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings waitingDict = new ActionBindings();
            waitingDict.Add(typeof(eNotifyMonitor), new Action(Check));

            dict.Add(typeof(Waiting), waitingDict);

            return dict;
        }
    }

    #endregion
}
