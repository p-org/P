using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace PiRacey
{
    #region Events

    internal class eLocal : Event { }

    internal class eStart : Event { }

    internal class eStop : Event { }

    internal class eWork : Event
    {
        public eWork(Object payload)
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

    internal class Result
    {
        public double Value;

        public Result(double value)
        {
            this.Value = value;
        }
    }

    #region Machines

    [Main]
    internal class Driver : Machine
    {
        private Machine Master;
        private int N;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                Console.WriteLine("[Driver] Initializing ...\n");

                machine.N = (int)this.Payload;
                machine.Master = Machine.Factory.CreateMachine<Master>(machine.N);

                this.Send(machine.Master, new eStart());

                this.Raise(new eLocal());
            }
        }

        private class End : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("[Driver] Ending ...\n");

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
        private int N;
        private int Counter;
        private double Result;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Master;

                Console.WriteLine("[Master] Initializing ...\n");

                machine.Workers = new List<Machine>();
                machine.N = (int)this.Payload;
                machine.Counter = 0;
                machine.Result = 0.0;

                for (int idx = 0; idx < machine.N; idx++)
                {
                    machine.Workers.Add(Machine.Factory.CreateMachine<Worker>(
                        new Tuple<int, int>(idx, machine.N)));
                }
            }
        }

        private class Running : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Master;

                Console.WriteLine("[Master] Running ...\n");

                int n = 1000;

                foreach (var worker in machine.Workers)
                {
                    this.Send(worker, new eWork(new Tuple<Machine, int>(machine, n)));
                }

                this.Raise(new eLocal());
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eSum)
                };
            }
        }

        private class Waiting : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Master;

                Console.WriteLine("[Master] Waiting ...\n");
            }
        }

        private void Sum()
        {
            Console.WriteLine("[Master] Summing ...\n");

            this.Counter++;
            this.Result += ((Result)this.Payload).Value;

            if (this.Counter == this.N)
            {
                foreach (var worker in this.Workers)
                {
                    this.Send(worker, new eStop());
                }

                Console.WriteLine("[Master] Result: {0}\n", this.Result);
                this.Delete();
            }
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eStart), typeof(Running));

            StepStateTransitions runningDict = new StepStateTransitions();
            runningDict.Add(typeof(eLocal), typeof(Waiting));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(Running), runningDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings waitingDict = new ActionBindings();
            waitingDict.Add(typeof(eSum), new Action(Sum));

            dict.Add(typeof(Waiting), waitingDict);

            return dict;
        }
    }

    internal class Worker : Machine
    {
        private int Id;
        private int NumOfWorkers;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Worker;

                machine.Id = ((Tuple<int, int>)this.Payload).Item1;
                machine.NumOfWorkers = ((Tuple<int, int>)this.Payload).Item2;

                Console.WriteLine("[Worker-{0}] Initializing ...\n", machine.Id);

                this.Raise(new eLocal());
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eWork)
                };
            }
        }

        private class Waiting : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Worker;

                Console.WriteLine("[Worker-{0}] Waiting ...\n", machine.Id);
            }
        }

        private void Work()
        {
            Console.WriteLine("[Worker-{0}] Working ...\n", this.Id);

            var master = ((Tuple<Machine, int>)this.Payload).Item1;
            var n = ((Tuple<Machine, int>)this.Payload).Item2;

            double h = 1.0 / n;
            double sum = 0;

            for (int idx = this.Id; idx <= n; idx += this.NumOfWorkers)
            {
                double x = h * (idx - 0.5);
                sum += (4.0 / (1.0 + x*x));
            }

            var result = new Result(h * sum);
            this.Send(master, new eSum(result));
            result.Value = 0;
        }

        private void Stop()
        {
            this.Delete();
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
            waitingDict.Add(typeof(eWork), new Action(Work));
            waitingDict.Add(typeof(eStop), new Action(Stop));

            dict.Add(typeof(Waiting), waitingDict);

            return dict;
        }
    }

    #endregion
}
