using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Chameneos
{
    #region Events

    internal class eLocal : Event { }

    internal class eStart : Event { }

    internal class eStop : Event { }

    internal class eHook : Event
    {
        public eHook(Object payload)
            : base(payload)
        { }
    }

    internal class eSecondRound : Event
    {
        public eSecondRound(Object payload)
            : base(payload)
        { }
    }

    internal class eGetCount : Event { }

    internal class eGetCountAck : Event
    {
        public eGetCountAck(Object payload)
            : base(payload)
        { }
    }

    internal class eGetString : Event { }

    internal class eGetStringAck : Event
    {
        public eGetStringAck(Object payload)
            : base(payload)
        { }
    }

    internal class eGetNumber : Event
    {
        public eGetNumber(Object payload)
            : base(payload)
        { }
    }

    internal class eGetNumberAck : Event
    {
        public eGetNumberAck(Object payload)
            : base(payload)
        { }
    }

    #endregion

    internal enum Colour
    {
        blue,
        red,
        yellow
    }

    #region Machines

    [Main]
    internal class Broker : Machine
    {
        private List<Machine> Creatures;

        private int TotalRendezvous;
        private Machine FirstHooker;
        private Colour FirstColour;

        private int TotalCreatures;
        private int TotalStoppedCreatures;
        private int Round;

        private int TR;
        private int TT;

        private List<List<Colour>> Groups;

        private int Counter;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Broker;

                Console.WriteLine("[Broker] is Initializing ...\n");

                machine.TR = (int)this.Payload;
                machine.TotalRendezvous = machine.TR;
                machine.Round = 1;

                machine.Groups = new List<List<Colour>>();
                machine.Groups.Add(new List<Colour> {
                    Colour.blue, Colour.red, Colour.yellow
                });
                machine.Groups.Add(new List<Colour> {
                    Colour.blue, Colour.red, Colour.yellow,
                    Colour.red, Colour.yellow, Colour.blue,
                    Colour.red, Colour.yellow, Colour.red,
                    Colour.blue
                });

                this.Raise(new eLocal());
            }
        }

        private class Booting : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Broker;

                Console.WriteLine("[Broker] is Booting ...\n");

                machine.TT = 0;
                machine.Counter = 0;

                machine.Creatures = new List<Machine>();
                machine.TotalCreatures = machine.Groups[0].Count;
                machine.TotalStoppedCreatures = 0;

                for (int i = 0; i < machine.Groups[0].Count; i++)
                {
                    var creature = Machine.Factory.CreateMachine<Chameneos>(
                        new Tuple<int, Machine, Colour>(i, machine, machine.Groups[0][i]));
                    machine.Creatures.Add(creature);
                    this.Send(creature, new eStart());
                }
            }
        }

        private class SecondRound : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Broker;

                Console.WriteLine("[Broker] is in SecondRound ...\n");

                machine.TotalRendezvous = (int)this.Payload;

                machine.Creatures = new List<Machine>();
                machine.TotalCreatures = machine.Groups[1].Count;
                machine.TotalStoppedCreatures = 0;

                for (int i = 0; i < machine.Groups[1].Count; i++)
                {
                    var creature = Machine.Factory.CreateMachine<Chameneos>(
                        new Tuple<int, Machine, Colour>(i + 10, machine, machine.Groups[1][i]));
                    machine.Creatures.Add(creature);
                    this.Send(creature, new eStart());
                }
            }
        }

        private void Hook()
        {
            Console.WriteLine("[Broker] is Hooking ...\n");

            var creature = ((Tuple<Machine, Colour>)this.Payload).Item1;
            var colour = ((Tuple<Machine, Colour>)this.Payload).Item2;

            if (this.TotalRendezvous == 0)
            {
                this.TotalStoppedCreatures++;
                this.DoPostRoundProcessing();
                return;
            }

            if (this.FirstHooker == null)
            {
                this.FirstHooker = creature;
                this.FirstColour = colour;
            }
            else
            {
                this.Send(this.FirstHooker, new eHook(new Tuple<Machine, Colour>(
                    creature, colour)));
                this.Send(creature, new eHook(new Tuple<Machine, Colour>(
                    this.FirstHooker, this.FirstColour)));
                this.FirstHooker = null;
                this.TotalRendezvous = this.TotalRendezvous - 1;
            }
        }

        private void DoPostRoundProcessing()
        {
            if (this.TotalCreatures == this.TotalStoppedCreatures)
            {
                foreach (var creature in this.Creatures)
                {
                    this.Send(creature, new eGetString());
                    this.Send(creature, new eGetCount());
                    this.Send(creature, new eGetNumber(this.TT));
                }
            }
            else
            {
                foreach (var c in this.Creatures)
                {
                    this.Send(c, new eStop());
                }

                this.Delete();
            }
        }

        private void ProcessPostRoundResults()
        {
            this.TT = this.TT + (int)this.Payload;

            this.Counter = this.Counter + 1;

            if (this.Counter == this.Creatures.Count)
            {
                if (this.Round == 1)
                {
                    foreach (var c in this.Creatures)
                    {
                        this.Send(c, new eStop());
                    }

                    this.Round = 2;
                    this.TT = 0;
                    this.Counter = 0;
                    this.Raise(new eSecondRound(this.TR));
                }
            }
        }

        private void HandleGetStringAck()
        {
            var str = (string)this.Payload;
            Console.WriteLine("[Broker] {0} ...\n", str);
        }

        private void HandleGetNumberAck()
        {
            var str = (string)this.Payload;
            Console.WriteLine("[Broker] {0} ...\n", str);
        }

        private void Stop()
        {
            Console.WriteLine("[Broker] is Stopping ...\n");

            this.Delete();
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(Booting));

            StepStateTransitions bootingDict = new StepStateTransitions();
            bootingDict.Add(typeof(eSecondRound), typeof(SecondRound));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(Booting), bootingDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings bootingDict = new ActionBindings();
            bootingDict.Add(typeof(eHook), new Action(Hook));
            bootingDict.Add(typeof(eGetCountAck), new Action(ProcessPostRoundResults));
            bootingDict.Add(typeof(eGetStringAck), new Action(HandleGetStringAck));
            bootingDict.Add(typeof(eGetNumberAck), new Action(HandleGetNumberAck));

            ActionBindings secondRoundDict = new ActionBindings();
            secondRoundDict.Add(typeof(eHook), new Action(Hook));
            secondRoundDict.Add(typeof(eGetCountAck), new Action(ProcessPostRoundResults));
            secondRoundDict.Add(typeof(eGetStringAck), new Action(HandleGetStringAck));
            secondRoundDict.Add(typeof(eGetNumberAck), new Action(HandleGetNumberAck));

            dict.Add(typeof(Booting), bootingDict);
            dict.Add(typeof(SecondRound), secondRoundDict);

            return dict;
        }
    }

    internal class Chameneos : Machine
    {
        private int Id;

        private Machine Broker;

        private Colour Colour;

        private int MyHooks;

        private int SelfHooks;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Chameneos;

                machine.Id = ((Tuple<int, Machine, Colour>)this.Payload).Item1;
                machine.Broker = ((Tuple<int, Machine, Colour>)this.Payload).Item2;
                machine.Colour = ((Tuple<int, Machine, Colour>)this.Payload).Item3;

                Console.WriteLine("[Chameneos-{0}] is Initializing ...\n", machine.Id);

                machine.MyHooks = 0;
                machine.SelfHooks = 0;
            }
        }

        private class Starting : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Chameneos;

                Console.WriteLine("[Chameneos-{0}] is Starting ...\n", machine.Id);

                this.Send(machine.Broker, new eHook(new Tuple<Machine, Colour>(
                    machine, machine.Colour)));
            }
        }

        private void Hook()
        {
            Console.WriteLine("[Chameneos-{0}] is Hooking ...\n", this.Id);

            var creature = ((Tuple<Machine, Colour>)this.Payload).Item1;
            var colour = ((Tuple<Machine, Colour>)this.Payload).Item2;

            this.Colour = this.DoCompliment(this.Colour, colour);

            if (this.Equals(creature))
            {
                this.SelfHooks = this.SelfHooks + 1;
            }

            this.MyHooks = this.MyHooks + 1;

            this.Raise(new eStart());
        }

        private void GetNumber()
        {
            Console.WriteLine("[Chameneos-{0}] is getting number ...\n", this.Id);

            var n = (int)this.Payload;

            var str = this.CreateNumber(n);

            this.Send(this.Broker, new eGetNumberAck(str));
        }

        private void GetCount()
        {
            Console.WriteLine("[Chameneos-{0}] is getting count ...\n", this.Id);

            this.Send(this.Broker, new eGetCountAck(this.MyHooks));
        }

        private void GetString()
        {
            Console.WriteLine("[Chameneos-{0}] is getting string ...\n", this.Id);

            var str = MyHooks.ToString() + this.CreateNumber(this.SelfHooks);

            this.Send(this.Broker, new eGetStringAck(str));
        }

        private void Stop()
        {
            Console.WriteLine("[Chameneos-{0}] is Stopping ...\n", this.Id);

            this.Delete();
        }

        private string CreateNumber(int n)
        {
            string str = "";
            string nStr = n.ToString();

            for (int i = 0; i < nStr.Length; i++)
            {
                str = str + " ";
                str = str + nStr[i];
            }

            return str;
        }

        private Colour DoCompliment(Colour c1, Colour c2)
        {
            if (c1 == Colour.blue)
            {
                if (c1 == Colour.blue)
                {
                    return Colour.blue;
                }
                else if (c1 == Colour.red)
                {
                    return Colour.yellow;
                }
                else
                {
                    return Colour.red;
                }
            }
            else if (c1 == Colour.red)
            {
                if (c1 == Colour.blue)
                {
                    return Colour.yellow;
                }
                else if (c1 == Colour.red)
                {
                    return Colour.red;
                }
                else
                {
                    return Colour.blue;
                }
            }
            else
            {
                if (c1 == Colour.blue)
                {
                    return Colour.red;
                }
                else if (c1 == Colour.red)
                {
                    return Colour.blue;
                }
                else
                {
                    return Colour.yellow;
                }
            }
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eStart), typeof(Starting));

            StepStateTransitions startingDict = new StepStateTransitions();
            startingDict.Add(typeof(eStart), typeof(Starting));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(Starting), startingDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings startingDict = new ActionBindings();
            startingDict.Add(typeof(eHook), new Action(Hook));
            startingDict.Add(typeof(eGetNumber), new Action(GetNumber));
            startingDict.Add(typeof(eGetCount), new Action(GetCount));
            startingDict.Add(typeof(eGetString), new Action(GetString));
            startingDict.Add(typeof(eStop), new Action(Stop));

            dict.Add(typeof(Starting), startingDict);

            return dict;
        }
    }

    #endregion
}
