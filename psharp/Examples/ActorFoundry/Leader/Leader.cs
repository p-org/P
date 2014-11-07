using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Leader
{
    #region Events

    internal class eLocal : Event { }

    internal class eStart : Event { }

    internal class eInit : Event
    {
        public eInit(Object payload)
            : base(payload)
        { }
    }

    internal class eReceive : Event
    {
        public eReceive(Object payload)
            : base(payload)
        { }
    }

    #endregion

    #region Machines

    [Main]
    internal class Driver : Machine
    {
        private List<Machine> Processes;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Driver;

                var n = (int)this.Payload;

                Console.WriteLine("[Driver] is Initializing ...\n");

                machine.Processes = new List<Machine>();

                for (int idx = 0; idx < n; idx++)
                {
                    machine.Processes.Add(Machine.Factory.CreateMachine<LProcess>());
                }

                for (int idx = 0; idx < n; idx++)
                {
                    var x = machine.Processes[idx];
                    var right = machine.Processes[(idx + 1) % n];

                    this.Send(x, new eInit(new Tuple<Machine, int>(right, idx)));
                }

                for (int idx = 0; idx < n; idx++)
                {
                    var x = machine.Processes[idx];

                    this.Send(x, new eStart());
                }

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

    internal class LProcess : Machine
    {
        private int Id;

        private Machine Right;

        private int Number;
        private int NeighborR;

        private bool IsActive;

        private int MaxId;


        [Initial]
        private class _Init : State
        {

        }

        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as LProcess;

                machine.Right = ((Tuple<Machine, int>)this.Payload).Item1;
                machine.Id = ((Tuple<Machine, int>)this.Payload).Item2;
                machine.MaxId = machine.Id;

                Console.WriteLine("[LProcess-{0}] is Initializing ...\n", machine.Id);

                machine.IsActive = true;

                this.Raise(new eLocal());
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eStart)
                };
            }
        }

        private class Active : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as LProcess;

                Console.WriteLine("[LProcess-{0}] is Active ...\n", machine.Id);
            }
        }

        private void Start()
        {
            Console.WriteLine("[LProcess-{0}] is Starting ...\n", this.Id);

            this.Send(this.Right, new eReceive(new Tuple<int,int>(0, this.MaxId)));
        }

        private void Receive()
        {
            Console.WriteLine("[LProcess-{0}] is Receiving ...\n", this.Id);

            var type = ((Tuple<int, int>)this.Payload).Item1;
            var id = ((Tuple<int, int>)this.Payload).Item2;

            if (type == 0)
            {
                var number = id;
                if (this.IsActive && number != this.MaxId)
                {
                    this.Send(this.Right, new eReceive(new Tuple<int, int>(1, number)));
                    this.NeighborR = number;
                }
                else if (!this.IsActive)
                {
                    this.Send(this.Right, new eReceive(new Tuple<int, int>(0, number)));
                }
            }
            else if (type == 1)
            {
                var number = id;
                if (this.IsActive)
                {
                    if (this.NeighborR > number && this.NeighborR > this.MaxId)
                    {
                        this.MaxId = this.NeighborR;
                        this.Send(this.Right, new eReceive(new Tuple<int, int>(0, this.NeighborR)));
                    }
                    else
                    {
                        this.IsActive = false;
                    }
                }
                else
                {
                    this.Send(this.Right, new eReceive(new Tuple<int, int>(1, number)));
                }
            }
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions _initDict = new StepStateTransitions();
            _initDict.Add(typeof(eInit), typeof(Init));

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(Active));

            dict.Add(typeof(_Init), _initDict);
            dict.Add(typeof(Init), initDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings activeDict = new ActionBindings();
            activeDict.Add(typeof(eStart), new Action(Start));
            activeDict.Add(typeof(eReceive), new Action(Receive));

            dict.Add(typeof(Active), activeDict);

            return dict;
        }
    }

    #endregion
}
