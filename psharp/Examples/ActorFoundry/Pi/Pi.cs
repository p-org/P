using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Pi
{
    #region Events

    internal class eLocal : Event { }

    internal class eBoot : Event { }

    internal class eStop : Event { }

    internal class eIntervals : Event
    {
        public eIntervals(Object payload)
            : base(payload)
        { }
    }

    internal class eSum : Event
    {
        public eSum(Object payload)
            : base(payload)
        { }
    }

    #endregion

    #region Machines

    [Main]
    internal class Driver : Machine
    {
        private Machine Master;

        private List<Machine> Workers;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                var n = (int)this.Payload;

                Console.WriteLine("[Driver] is Initializing ...\n");

                machine.Workers = new List<Machine>();

                for (int idx = 0; idx < n; idx++)
                {
                    machine.Workers.Add(Machine.Factory.CreateMachine<Worker>(
                        new Tuple<int, int>(idx, n)));
                }

                machine.Master = Machine.Factory.CreateMachine<Master>(machine.Workers);

                this.Send(machine.Master, new eBoot());

                this.Raise(new eLocal());
            }
        }

        private class End : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("[Driver] is Ending ...\n");

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

    internal class Master : Machine
    {
        private List<Machine> Workers;

        private double Result;
        private int Counter;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Master;

                Console.WriteLine("[Master] is Initializing ...\n");

                machine.Workers = (List<Machine>)this.Payload;
                machine.Result = 0.0;
                machine.Counter = 0;

                this.Raise(new eLocal());
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eBoot)
                };
            }
        }

        private class Active : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("[Master] is Active ...\n");
            }
        }

        private void Boot()
        {
            Console.WriteLine("[Master] is Booting ...\n");

            int n = 30000;

            foreach (var worker in this.Workers)
            {
                this.Send(worker, new eIntervals(new Tuple<Machine, int>(this, n)));
            }
        }

        private void Sum()
        {
            Console.WriteLine("[Master] is Summing ...\n");

            var p = (double)this.Payload;

            this.Counter = this.Counter + 1;
            this.Result = this.Result + p;

            if (this.Counter == this.Workers.Count)
            {
                foreach (var worker in this.Workers)
                {
                    this.Send(worker, new eStop());
                }

                Console.WriteLine("[Master] Result is {0}\n", this.Result);

                this.Delete();
            }
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(Active));

            dict.Add(typeof(Init), initDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings activeDict = new ActionBindings();
            activeDict.Add(typeof(eBoot), new Action(Boot));
            activeDict.Add(typeof(eSum), new Action(Sum));

            dict.Add(typeof(Active), activeDict);

            return dict;
        }
    }

    internal class Worker : Machine
    {
        private int Id;

        private int N;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Worker;

                machine.Id = ((Tuple<int, int>)this.Payload).Item1;
                machine.N = ((Tuple<int, int>)this.Payload).Item2;

                Console.WriteLine("[Worker-{0}] is Initializing ...\n", machine.Id);

                this.Raise(new eLocal());
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eIntervals)
                };
            }
        }

        private class Active : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Worker;

                Console.WriteLine("[Worker-{0}] is Active ...\n", machine.Id);
            }
        }

        private void Intervals()
        {
            Console.WriteLine("[Worker-{0}] is Computing ...\n", this.Id);

            var sender = ((Tuple<Machine, int>)this.Payload).Item1;
            var n = ((Tuple<Machine, int>)this.Payload).Item2;

            double h = 1.0 / n;
            double sum = 0;

            for (int idx = this.Id; idx <= n; idx += this.N)
            {
                double x = h * (idx - 0.5);
                sum = sum + (4.0 / (1.0 + x * x));
            }

            this.Send(sender, new eSum(h * sum));
        }

        private void Stop()
        {
            Console.WriteLine("[Worker-{0}] is Stopping ...\n", this.Id);

            this.Delete();
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(Active));

            dict.Add(typeof(Init), initDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings activeDict = new ActionBindings();
            activeDict.Add(typeof(eIntervals), new Action(Intervals));
            activeDict.Add(typeof(eStop), new Action(Stop));

            dict.Add(typeof(Active), activeDict);

            return dict;
        }
    }

    #endregion
}
