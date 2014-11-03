using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace LeaderElectionRacey
{
    #region Events

    internal class eStart : Event
    {
        public eStart(Object payload)
            : base(payload)
        { }
    }

    internal class eNotify : Event
    {
        public eNotify(Object payload)
            : base(payload)
        { }
    }

    internal class eCheckAck : Event
    {
        public eCheckAck(Object payload)
            : base(payload)
        { }
    }

    #endregion

    internal class Message
    {
        public int Type;
        public int Value;
    }

    #region Machines

    [Main]
    internal class Master : Machine
    {
        private List<Machine> LProcesses;
        private int N;

        private List<bool> ActiveProcs;
        private int Counter;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Master;

                Console.WriteLine("[Master] Initializing ...\n");

                machine.LProcesses = new List<Machine>();
                machine.N = (int)this.Payload;
                machine.ActiveProcs = new List<bool>();
                machine.Counter = 0;

                for (int idx = 0; idx < machine.N; idx++)
                {
                    machine.LProcesses.Insert(0, Machine.Factory.CreateMachine<LProcess>(
                        new Tuple<int, Machine>(idx + 1, machine)));
                    machine.ActiveProcs.Add(false);
                }

                for (int idx = 0; idx < machine.N; idx++)
                {
                    this.Send(machine.LProcesses[idx], new eStart(machine.LProcesses[(idx + 1) % machine.N]));
                }
            }
        }

        private void Check()
        {
            Console.WriteLine("[Master] Checking ...\n");

            int id = ((Tuple<int, bool>)this.Payload).Item1;
            bool active = ((Tuple<int, bool>)this.Payload).Item2;

            this.ActiveProcs[id - 1] = active;
            this.Counter++;

            if (this.Counter != 3)
            {
                return;
            }

            int count = 0;
            foreach (var p in this.ActiveProcs)
            {
                if (p)
                {
                    count++;
                }
            }

            this.Counter = 0;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings initDict = new ActionBindings();
            initDict.Add(typeof(eCheckAck), new Action(Check));

            dict.Add(typeof(Init), initDict);

            return dict;
        }
    }

    internal class LProcess : Machine
    {
        private int Id;

        private Machine Master;
        private Machine Right;

        private int Number;
        private int MaxId;
        private int NeighborR;

        private bool IsActive;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as LProcess;

                machine.Id = ((Tuple<int, Machine>)this.Payload).Item1;
                machine.Master = ((Tuple<int, Machine>)this.Payload).Item2;

                Console.WriteLine("[LProcess-{0}] Initializing ...\n", machine.Id);
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eNotify),
                    typeof(eCheckAck)
                };
            }
        }

        private class Running : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as LProcess;

                Console.WriteLine("[LProcess-{0}] Running ...\n", machine.Id);

                machine.Right = (LProcess)this.Payload;
                machine.MaxId = machine.Id;
                machine.IsActive = true;

                var message = new Message();
                message.Type = 0;
                message.Value = machine.Id;
                this.Send(machine.Right, new eNotify(message));
            }
        }

        private void Process()
        {
            Console.WriteLine("[LProcess-{0}] Processing ...\n", this.Id);

            int type = ((Message)this.Payload).Type;
            int id = ((Message)this.Payload).Value;

            var message = new Message();

            if (type == 0)
            {
                this.Number = id;
                if (this.IsActive && this.Number != this.MaxId)
                {
                    message.Type = 1;
                    message.Value = this.Number;
                    this.Send(this.Right, new eNotify(message));
                    this.NeighborR = this.Number;
                }
                else if (!this.IsActive)
                {
                    message.Type = 0;
                    message.Value = this.Number;
                    this.Send(this.Right, new eNotify(message));
                }
            }
            else if (type == 1)
            {
                this.Number = id;
                if (this.IsActive)
                {
                    if (this.NeighborR > this.Number && this.NeighborR > this.MaxId)
                    {
                        message.Type = 0;
                        message.Value = this.NeighborR;
                        this.MaxId = this.NeighborR;
                        this.Send(this.Right, new eNotify(message));
                    }
                    else
                    {
                        this.IsActive = false;
                    }
                }
                else
                {
                    message.Type = 1;
                    message.Value = this.Number;
                    this.Send(this.Right, new eNotify(message));
                }
            }

            message.Value = 0;
            this.Send(this.Master, new eCheckAck(new Tuple<int, bool>(this.Id, this.IsActive)));
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
            runningDict.Add(typeof(eNotify), new Action(Process));

            dict.Add(typeof(Running), runningDict);

            return dict;
        }
    }

    #endregion
}
