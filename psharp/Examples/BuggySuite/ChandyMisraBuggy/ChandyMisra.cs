using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace ChandyMisraBuggy
{
    #region Events

    internal class eLocal : Event { }

    internal class eAddNeighbour : Event
    {
        public eAddNeighbour(Object payload)
            : base(payload)
        { }
    }

    internal class eNotify : Event
    {
        public eNotify(Object payload)
            : base(payload)
        { }
    }

    #endregion

    #region Machines

    [Main]
    internal class Master : Machine
    {
        private Machine A0;
        private Machine A1;
        private Machine A2;
        private Machine A3;
        private Machine A4;

        private int N;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Master;

                Console.WriteLine("[Master] Initializing ...\n");

                machine.N = (int)this.Payload;

                machine.A0 = Machine.Factory.CreateMachine<SPProcess>(
                    new Tuple<int, Machine>(0, machine));
                machine.A1 = Machine.Factory.CreateMachine<SPProcess>(
                    new Tuple<int, Machine>(1, machine));
                machine.A2 = Machine.Factory.CreateMachine<SPProcess>(
                    new Tuple<int, Machine>(2, machine));
                machine.A3 = Machine.Factory.CreateMachine<SPProcess>(
                    new Tuple<int, Machine>(3, machine));
                machine.A4 = Machine.Factory.CreateMachine<SPProcess>(
                    new Tuple<int, Machine>(4, machine));

                if (machine.N == 4)
                {
                    this.Send(machine.A0, new eAddNeighbour(new Tuple<Machine, int>(
                        machine.A1, 10)));
                    this.Send(machine.A1, new eAddNeighbour(new Tuple<Machine, int>(
                        machine.A0, 10)));
                    this.Send(machine.A0, new eAddNeighbour(new Tuple<Machine, int>(
                        machine.A2, 10)));
                    this.Send(machine.A2, new eAddNeighbour(new Tuple<Machine, int>(
                        machine.A0, 10)));
                    this.Send(machine.A0, new eAddNeighbour(new Tuple<Machine, int>(
                        machine.A3, 10)));
                    this.Send(machine.A3, new eAddNeighbour(new Tuple<Machine, int>(
                        machine.A0, 10)));
                    this.Send(machine.A1, new eAddNeighbour(new Tuple<Machine, int>(
                        machine.A3, 10)));
                    this.Send(machine.A3, new eAddNeighbour(new Tuple<Machine, int>(
                        machine.A1, 10)));
                    this.Send(machine.A2, new eAddNeighbour(new Tuple<Machine, int>(
                        machine.A3, 10)));
                    this.Send(machine.A3, new eAddNeighbour(new Tuple<Machine, int>(
                        machine.A2, 10)));

                    this.Send(machine.A0, new eNotify(new Tuple<Machine, int, int>(
                        null, 0, 0)));
                }
                else
                {
                    this.Send(machine.A0, new eAddNeighbour(new Tuple<Machine, int>(
                        machine.A1, 10)));
                    this.Send(machine.A1, new eAddNeighbour(new Tuple<Machine, int>(
                        machine.A0, 10)));
                    this.Send(machine.A0, new eAddNeighbour(new Tuple<Machine, int>(
                        machine.A2, 10)));
                    this.Send(machine.A2, new eAddNeighbour(new Tuple<Machine, int>(
                        machine.A0, 10)));
                    this.Send(machine.A1, new eAddNeighbour(new Tuple<Machine, int>(
                        machine.A3, 10)));
                    this.Send(machine.A3, new eAddNeighbour(new Tuple<Machine, int>(
                        machine.A1, 10)));
                    this.Send(machine.A4, new eAddNeighbour(new Tuple<Machine, int>(
                        machine.A1, 10)));
                    this.Send(machine.A4, new eAddNeighbour(new Tuple<Machine, int>(
                        machine.A0, 10)));
                    this.Send(machine.A1, new eAddNeighbour(new Tuple<Machine, int>(
                        machine.A4, 10)));
                    this.Send(machine.A0, new eAddNeighbour(new Tuple<Machine, int>(
                        machine.A4, 10)));

                    this.Send(machine.A4, new eNotify(new Tuple<Machine, int, int>(
                        null, 0, 0)));
                }
            }
        }
    }

    internal class SPProcess : Machine
    {
        private int Id;

        private Machine Master;

        private List<Machine> Neighbours;
        private List<int> NeighboursD;

        private int D;
        private Machine N;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as SPProcess;

                machine.Id = ((Tuple<int, Machine>)this.Payload).Item1;
                machine.Master = ((Tuple<int, Machine>)this.Payload).Item2;

                Console.WriteLine("[SPProcess-{0}] Initializing ...\n", machine.Id);

                machine.Neighbours = new List<Machine>();
                machine.NeighboursD = new List<int>();
                machine.D = -1;
                machine.N = null;

                this.Raise(new eLocal());
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eAddNeighbour)
                };
            }
        }

        private class Running : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as SPProcess;

                Console.WriteLine("[SPProcess-{0}] Running ...\n", machine.Id);
            }
        }

        private void AddNeighbour()
        {
            Console.WriteLine("[SPProcess-{0}] AddingNeighbour ...\n", this.Id);

            var proc = ((Tuple<Machine, int>)this.Payload).Item1;
            var d = ((Tuple<Machine, int>)this.Payload).Item2;

            this.Neighbours.Add(proc);
            this.NeighboursD.Add(d);
        }

        private void Process()
        {
            Console.WriteLine("[SPProcess-{0}] Process ...\n", this.Id);

            var sender = ((Tuple<Machine, int, int>)this.Payload).Item1;
            var d = ((Tuple<Machine, int, int>)this.Payload).Item2;
            var w = ((Tuple<Machine, int, int>)this.Payload).Item3;

            if (this.D == -1 || this.D > d + w)
            {
                this.D = d + w;
                this.N = sender;

                for (int idx = 0; idx < this.Neighbours.Count + 1; idx++)
                {
                    this.Send(this.Neighbours[idx], new eNotify(new Tuple<Machine, int, int>(
                        this, this.D, this.NeighboursD[idx])));
                }
            }
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
            runningDict.Add(typeof(eAddNeighbour), new Action(AddNeighbour));
            runningDict.Add(typeof(eNotify), new Action(Process));

            dict.Add(typeof(Running), runningDict);

            return dict;
        }
    }

    #endregion
}
