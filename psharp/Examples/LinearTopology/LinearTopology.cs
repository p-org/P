//-----------------------------------------------------------------------
// <copyright file="LinearTopology.cs" company="Microsoft">
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

namespace LinearTopology
{
    #region Events

    //Local transitions
    internal class eLocal : Event { }
    internal class eUnit : Event { }

    //State decision event sent periodically by parent clock to port machines
    internal class eStateDecisionEvent : Event { }

    // all the below events are used for atomic state decision calculation
    internal class eAck : Event { }

    //Recommended state
    internal class eGoMaster : Event { }
    internal class eGoSlave : Event { }
    internal class eGoPassive : Event { }

    //Done state update
    internal class eDoneStateChange : Event { }

    //announce message
    internal class eAnnounce : Event
    {
        public eAnnounce(Object payload)
            : base(payload)
        { }
    }

    //Initialise event to initialize the network (portconnectedto, myclock)
    internal class eInitialise : Event
    {
        public eInitialise(Object payload)
            : base(payload)
        { }
    }

    //Initialize variables on PowerUp when machines start ((ParentGM))
    internal class ePowerUp : Event
    {
        public ePowerUp(Object payload)
            : base(payload)
        { }
    }

    internal class eErBest : Event
    {
        public eErBest(Object payload)
            : base(payload)
        { }
    }

    internal class eUpdateParentGM : Event
    {
        public eUpdateParentGM(Object payload)
            : base(payload)
        { }
    }

    #endregion

    #region Machines

    /// <summary>
    ///  The god machine which creates the verification instance.
    /// </summary>
    [Main]
    [Ghost]
    internal class GodMachine : Machine
    {
        private Machine OC1;
        private Machine OC2;
        private Machine BC1;
        private Machine BC2;

        // Ports connecting the clocks together.
        private Machine PT1;
        private Machine PT2;
        private Machine PT3;
        private Machine PT4;
        private Machine PT5;
        private Machine PT6;

        // Temporary variable
        private List<Machine> Link;

        [Initial]
        private class Init : State
        {
            public override void OnEntry()
            {
                Console.WriteLine("GodMachine is creating all post machines ...");

                // Create all the post machines.
                (this.Machine as GodMachine).PT1 =
                    Machine.Factory.CreateMachine<PortMachine>();
                (this.Machine as GodMachine).PT2 =
                    Machine.Factory.CreateMachine<PortMachine>();
                (this.Machine as GodMachine).PT3 =
                    Machine.Factory.CreateMachine<PortMachine>();
                (this.Machine as GodMachine).PT4 =
                    Machine.Factory.CreateMachine<PortMachine>();
                (this.Machine as GodMachine).PT5 =
                    Machine.Factory.CreateMachine<PortMachine>();
                (this.Machine as GodMachine).PT6 =
                    Machine.Factory.CreateMachine<PortMachine>();

                Console.WriteLine("GodMachine is creating all ordinary machines ...");

                // Create the ordinary clock machines.
                (this.Machine as GodMachine).Link = new List<Machine>();

                (this.Machine as GodMachine).Link.Insert(0, (this.Machine as GodMachine).PT1);
                (this.Machine as GodMachine).OC1 = Machine.Factory.CreateMachine<Clock>(
                    new Tuple<List<Machine>, int>((this.Machine as GodMachine).Link, 1));
                (this.Machine as GodMachine).Link.RemoveAt(0);
                Runtime.Assert((this.Machine as GodMachine).Link.Count == 0);

                (this.Machine as GodMachine).Link.Insert(0, (this.Machine as GodMachine).PT6);
                (this.Machine as GodMachine).OC2 = Machine.Factory.CreateMachine<Clock>(
                    new Tuple<List<Machine>, int>((this.Machine as GodMachine).Link, 2));
                (this.Machine as GodMachine).Link.RemoveAt(0);
                Runtime.Assert((this.Machine as GodMachine).Link.Count == 0);

                // Create the boundary clocks.
                (this.Machine as GodMachine).Link.Insert(0, (this.Machine as GodMachine).PT2);
                (this.Machine as GodMachine).Link.Insert(0, (this.Machine as GodMachine).PT3);
                (this.Machine as GodMachine).BC1 = Machine.Factory.CreateMachine<Clock>(
                    new Tuple<List<Machine>, int>((this.Machine as GodMachine).Link, 3));
                (this.Machine as GodMachine).Link.RemoveAt(0);
                (this.Machine as GodMachine).Link.RemoveAt(0);
                Runtime.Assert((this.Machine as GodMachine).Link.Count == 0);

                (this.Machine as GodMachine).Link.Insert(0, (this.Machine as GodMachine).PT4);
                (this.Machine as GodMachine).Link.Insert(0, (this.Machine as GodMachine).PT5);
                (this.Machine as GodMachine).BC2 = Machine.Factory.CreateMachine<Clock>(
                    new Tuple<List<Machine>, int>((this.Machine as GodMachine).Link, 4));
                (this.Machine as GodMachine).Link.RemoveAt(0);
                (this.Machine as GodMachine).Link.RemoveAt(0);
                Runtime.Assert((this.Machine as GodMachine).Link.Count == 0);

                // Initialize all the ports appropriately with
                // the connections and power them up.
                Console.WriteLine("{0} sending event {1} to {2}", this.Machine,
                        typeof(eInitialise), (this.Machine as GodMachine).PT1);
                this.Send((this.Machine as GodMachine).PT1,
                    new eInitialise(new Tuple<Machine, Machine>(
                        (this.Machine as GodMachine).PT2,
                        (this.Machine as GodMachine).OC1)));

                Console.WriteLine("{0} sending event {1} to {2}", this.Machine,
                        typeof(eInitialise), (this.Machine as GodMachine).PT2);
                this.Send((this.Machine as GodMachine).PT2,
                    new eInitialise(new Tuple<Machine, Machine>(
                        (this.Machine as GodMachine).PT1,
                        (this.Machine as GodMachine).BC1)));

                Console.WriteLine("{0} sending event {1} to {2}", this.Machine,
                        typeof(eInitialise), (this.Machine as GodMachine).PT3);
                this.Send((this.Machine as GodMachine).PT3,
                    new eInitialise(new Tuple<Machine, Machine>(
                        (this.Machine as GodMachine).PT4,
                        (this.Machine as GodMachine).BC1)));

                Console.WriteLine("{0} sending event {1} to {2}", this.Machine,
                        typeof(eInitialise), (this.Machine as GodMachine).PT4);
                this.Send((this.Machine as GodMachine).PT4,
                    new eInitialise(new Tuple<Machine, Machine>(
                        (this.Machine as GodMachine).PT3,
                        (this.Machine as GodMachine).BC2)));

                Console.WriteLine("{0} sending event {1} to {2}", this.Machine,
                        typeof(eInitialise), (this.Machine as GodMachine).PT5);
                this.Send((this.Machine as GodMachine).PT5,
                    new eInitialise(new Tuple<Machine, Machine>(
                        (this.Machine as GodMachine).PT6,
                        (this.Machine as GodMachine).BC2)));

                Console.WriteLine("{0} sending event {1} to {2}", this.Machine,
                        typeof(eInitialise), (this.Machine as GodMachine).PT6);
                this.Send((this.Machine as GodMachine).PT6,
                    new eInitialise(new Tuple<Machine, Machine>(
                        (this.Machine as GodMachine).PT5,
                        (this.Machine as GodMachine).OC2)));

                // Delete the God machine, the job is done
                this.Delete();
            }
        }
    }

    /// <summary>
    /// Boundary clock machine.
    /// This machine manages multiple port state machines
    /// and also makes sure that the state-changes are atomic
    /// across the port machine.
    /// </summary>
    internal class Clock : Machine
    {
        // Port machines in this clock.
        private List<Machine> Ports;

        // Pointer to the parent GM for this clock (machine, rank).
        private Tuple<Machine, int> ParentGM;

        // Number of boundary clocks from the parent GM clock,
        // it basically corresponds to the steps removed.
        private int LengthFromGM;

        // My rank.
        private int D0;

        // Best message received in the current announce
        // interval (received from, (GM, GM_rank)).
        private Tuple<Machine, Tuple<Machine, int>> EBest;

        // ErBest from each port machine.
        private List<Tuple<Machine, Tuple<Machine, int>>> ErBestSeq;

        // Temporary variables.
        private bool Check;
        private int I;
        private int CountAck;

        [Initial]
        private class Init : State
        {
            public override void OnEntry()
            {
                // Initialize the EBest value to random.
                (this.Machine as Clock).Ports = ((Tuple<List<Machine>, int>)this.Payload).Item1;
                (this.Machine as Clock).D0 = ((Tuple<List<Machine>, int>)this.Payload).Item2;

                (this.Machine as Clock).ParentGM =
                    new Tuple<Machine, int>(this.Machine, (this.Machine as Clock).D0);

                (this.Machine as Clock).EBest =
                    new Tuple<Machine, Tuple<Machine, int>>(null, new Tuple<Machine, int>(null, 100000));
                
                (this.Machine as Clock).CountAck = 0;
                (this.Machine as Clock).I = (this.Machine as Clock).Ports.Count - 1;

                while ((this.Machine as Clock).I >= 0)
                {
                    Console.WriteLine("{0} sending event {1} to {2}", this.Machine,
                        typeof(ePowerUp), (this.Machine as Clock).Ports[(this.Machine as Clock).I]);
                    this.Send((this.Machine as Clock).Ports[(this.Machine as Clock).I],
                        new ePowerUp((this.Machine as Clock).ParentGM));
                    (this.Machine as Clock).I--;
                }

                this.Raise(new eLocal());
            }
        }

        private class PeriodicStateDecision : State
        {
            public override void OnEntry()
            {
                (this.Machine as Clock).Check =
                    (this.Machine as Clock).IsPeriodicAnnounceTimeOut();

                if ((this.Machine as Clock).Check)
                {
                    (this.Machine as Clock).I = (this.Machine as Clock).Ports.Count - 1;
                    while ((this.Machine as Clock).I >= 0)
                    {
                        Console.WriteLine("{0} sending event {1} to {2}", this.Machine,
                            typeof(eStateDecisionEvent), (this.Machine as Clock).Ports[(this.Machine as Clock).I]);
                        this.Send((this.Machine as Clock).Ports[(this.Machine as Clock).I],
                            new eStateDecisionEvent());
                        (this.Machine as Clock).I--;
                    }

                    Console.WriteLine("Clock goes to atomic transaction mode");
                    // Goes to atomic transaction mode.
                    this.Call(typeof(WaitForErBest));
                }

                this.Raise(new eUnit());
            }
        }

        private class WaitForErBest : State
        {

        }

        private class CalculateRecommendedState : State
        {
            public override void OnEntry()
            {
                (this.Machine as Clock).I = (this.Machine as Clock).Ports.Count - 1;

                // For each port calculate the recommended state.
                while ((this.Machine as Clock).I >= 0)
                {
                    // Check if I am the GM or my clock is better than all ErBest.
                    // D0 is class stratum 1.
                    if ((this.Machine as Clock).D0 == 1)
                    {
                        // D0 is better than EBest.
                        if ((this.Machine as Clock).D0 == (this.Machine as Clock).
                            ErBestSeq[(this.Machine as Clock).I].Item2.Item2)
                        {
                            // The parentGM point to current node.
                            (this.Machine as Clock).ParentGM =
                                new Tuple<Machine, int>(this.Machine, (this.Machine as Clock).D0);
                            Console.WriteLine("{0} sending event {1} to {2}", this.Machine,
                                typeof(eGoMaster), (this.Machine as Clock).ErBestSeq[(this.Machine as Clock).I].Item1);
                            this.Send((this.Machine as Clock).ErBestSeq[(this.Machine as Clock).I].Item1,
                                new eGoMaster());
                        }
                        else
                        {
                            // No change in the parentGM.
                            Console.WriteLine("{0} sending event {1} to {2}", this.Machine,
                                typeof(eGoPassive), (this.Machine as Clock).ErBestSeq[(this.Machine as Clock).I].Item1);
                            this.Send((this.Machine as Clock).ErBestSeq[(this.Machine as Clock).I].Item1,
                                new eGoPassive());
                        }
                    }
                    else
                    {
                        if ((this.Machine as Clock).D0 < (this.Machine as Clock).EBest.Item2.Item2)
                        {
                            // GM is the current node.
                            (this.Machine as Clock).ParentGM =
                                new Tuple<Machine, int>(this.Machine, (this.Machine as Clock).D0);
                            Console.WriteLine("{0} sending event {1} to {2}", this.Machine,
                                typeof(eGoMaster), (this.Machine as Clock).ErBestSeq[(this.Machine as Clock).I].Item1);
                            this.Send((this.Machine as Clock).ErBestSeq[(this.Machine as Clock).I].Item1,
                                new eGoMaster());
                        }
                        else
                        {
                            // Check on which port Ebest was received.
                            if ((this.Machine as Clock).EBest.Item1 == (this.Machine as Clock).
                                ErBestSeq[(this.Machine as Clock).I].Item1)
                            {
                                (this.Machine as Clock).ParentGM =
                                    (this.Machine as Clock).ErBestSeq[(this.Machine as Clock).I].Item2;
                                Console.WriteLine("{0} sending event {1} to {2}", this.Machine,
                                    typeof(eGoSlave), (this.Machine as Clock).ErBestSeq[(this.Machine as Clock).I].Item1);
                                this.Send((this.Machine as Clock).ErBestSeq[(this.Machine as Clock).I].Item1,
                                    new eGoSlave());
                            }
                            else
                            {
                                if ((this.Machine as Clock).EBest.Item2.Item2 <
                                    (this.Machine as Clock).ErBestSeq[(this.Machine as Clock).I].Item2.Item2)
                                {
                                    Console.WriteLine("{0} sending event {1} to {2}", this.Machine,
                                        typeof(eGoPassive), (this.Machine as Clock).ErBestSeq[(this.Machine as Clock).I].Item1);
                                    this.Send((this.Machine as Clock).ErBestSeq[(this.Machine as Clock).I].Item1,
                                        new eGoPassive());
                                }
                                else
                                {
                                    (this.Machine as Clock).ParentGM = (this.Machine as Clock).EBest.Item2;
                                    Console.WriteLine("{0} sending event {1} to {2}", this.Machine,
                                        typeof(eGoMaster), (this.Machine as Clock).ErBestSeq[(this.Machine as Clock).I].Item1);
                                    this.Send((this.Machine as Clock).ErBestSeq[(this.Machine as Clock).I].Item1,
                                        new eGoMaster());
                                }
                            }
                        }
                    }

                    (this.Machine as Clock).I--;
                }

                // Clear the Erbest seq.
                (this.Machine as Clock).I = (this.Machine as Clock).ErBestSeq.Count - 1;
                while ((this.Machine as Clock).I >= 0)
                {
                    (this.Machine as Clock).ErBestSeq.RemoveAt(0);
                    (this.Machine as Clock).I--;
                }

                // Send all the ports their new ParentGM.
                (this.Machine as Clock).I = (this.Machine as Clock).Ports.Count - 1;
                while ((this.Machine as Clock).I >= 0)
                {
                    Console.WriteLine("{0} sending event {1} to {2}", this.Machine,
                        typeof(eUpdateParentGM), (this.Machine as Clock).Ports[(this.Machine as Clock).I]);
                    this.Send((this.Machine as Clock).Ports[(this.Machine as Clock).I],
                        new eUpdateParentGM((this.Machine as Clock).ParentGM));
                    (this.Machine as Clock).I--;
                }
            }
        }

        private void ReceiveErBest()
        {
            // Add to the sequence.
            this.ErBestSeq.Insert(0, this.Payload as Tuple<Machine, Tuple<Machine, int>>);

            if (this.ErBestSeq.Count == this.Ports.Count)
            {
                // Calculate EBest and also clear ErBest.
                this.I = this.ErBestSeq.Count - 1;
                while (this.I >= 0)
                {
                    if (this.EBest.Item2.Item2 > this.ErBestSeq[this.I].Item2.Item2)
                    {
                        this.EBest = this.ErBestSeq[this.I];
                    }
                    this.I--;
                }

                this.Raise(new eLocal());
            }
        }

        private void ReceiveAck()
        {
            this.CountAck++;
            if (this.CountAck == this.Ports.Count)
            {
                this.CountAck = 0;
                this.I = this.Ports.Count - 1;
                while (this.I >= 0)
                {
                    Console.WriteLine("{0} sending event {1} to {2}", this,
                        typeof(eAck), this.Ports[this.I]);
                    this.Send(this.Ports[this.I], new eAck());
                    this.I--;
                }
            }
        }

        [Ghost]
        private bool IsPeriodicAnnounceTimeOut()
        {
            if (Model.Havoc.Boolean)
            {
                Console.WriteLine("Clock: IsPeriodicAnnounceTimeOut: True");
                return true;
            }
            else
            {
                Console.WriteLine("Clock: IsPeriodicAnnounceTimeOut: False");
                return false;
            }
        }

        protected override Dictionary<Type, StateTransitions> DefineStepTransitions()
        {
            Dictionary<Type, StateTransitions> dict = new Dictionary<Type, StateTransitions>();

            StateTransitions initDict = new StateTransitions();
            initDict.Add(typeof(eLocal), typeof(PeriodicStateDecision));

            StateTransitions periodicStateDecisionDict = new StateTransitions();
            periodicStateDecisionDict.Add(typeof(eUnit), typeof(PeriodicStateDecision));

            StateTransitions waitForErBestDict = new StateTransitions();
            waitForErBestDict.Add(typeof(eLocal), typeof(CalculateRecommendedState));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(PeriodicStateDecision), periodicStateDecisionDict);
            dict.Add(typeof(WaitForErBest), waitForErBestDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings periodicStateDecisionDict = new ActionBindings();
            periodicStateDecisionDict.Add(typeof(eErBest), new Action(ReceiveErBest));

            ActionBindings calculateRecommendedStateDict = new ActionBindings();
            calculateRecommendedStateDict.Add(typeof(eAck), new Action(ReceiveAck));

            dict.Add(typeof(PeriodicStateDecision), periodicStateDecisionDict);
            dict.Add(typeof(CalculateRecommendedState), calculateRecommendedStateDict);

            return dict;
        }
    }

    /// <summary>
    /// The port state machine.
    /// </summary>
    internal class PortMachine : Machine
    {
        private Machine ConnectedTo;
        private Machine MyClock;
        private Tuple<Machine, int> ErBestVar;
        private Tuple<Machine, int> ParentGM;

        // Temporary variables.
        private bool Check;
        private int NumOfAnnounceIntervals;

        // 0 : master, 1: slave, 2 : passive
        private int RecState;

        [Initial]
        private class Init : State
        {
            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eAnnounce)
                };
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eStateDecisionEvent),
                    typeof(ePowerUp)
                };
            }
        }

        private class ConnectionInitialized : State
        {
            public override void OnEntry()
            {
                (this.Machine as PortMachine).ConnectedTo =
                    ((Tuple<Machine, Machine>)this.Payload).Item1;
                (this.Machine as PortMachine).MyClock =
                    ((Tuple<Machine, Machine>)this.Payload).Item2;

                (this.Machine as PortMachine).ErBestVar =
                    new Tuple<Machine, int>(this.Machine, 10000);
            }

            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eAnnounce)
                };
            }
        }

        private class Initializing : State
        {
            public override void OnEntry()
            {
                (this.Machine as PortMachine).ParentGM = (Tuple<Machine, int>)this.Payload;

                (this.Machine as PortMachine).Check =
                    (this.Machine as PortMachine).IsAnnounceReceiptTimeOut();

                if ((this.Machine as PortMachine).Check)
                {
                    this.Raise(new eLocal());
                }

                this.Raise(new eUnit());
            }

            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eAnnounce)
                };
            }
        }

        private class Listening : State
        {
            public override void OnEntry()
            {
                (this.Machine as PortMachine).Check =
                    (this.Machine as PortMachine).IsThreeAnnounceReceiptTimeOut();

                if ((this.Machine as PortMachine).Check)
                {
                    this.Raise(new eGoMaster());
                }
            }
        }

        private class Master : State
        {
            public override void OnEntry()
            {
                (this.Machine as PortMachine).Check =
                    (this.Machine as PortMachine).IsAnnounceReceiptTimeOut();
                if ((this.Machine as PortMachine).Check)
                {
                    Console.WriteLine("{0} sending event {1} to {2}", this.Machine,
                        typeof(eAnnounce), (this.Machine as PortMachine).ConnectedTo);
                    this.Send((this.Machine as PortMachine).ConnectedTo,
                        new eAnnounce((this.Machine as PortMachine).ParentGM));
                }

                this.Raise(new eUnit());
            }
        }

        private class DeferAll : State
        {
            public override void OnEntry()
            {
                this.Call(typeof(SendErBestAndWaitForRecState));

                if ((this.Machine as PortMachine).RecState == 0)
                {
                    this.Raise(new eGoMaster());
                }

                if ((this.Machine as PortMachine).RecState == 1)
                {
                    this.Raise(new eGoSlave());
                }

                if ((this.Machine as PortMachine).RecState == 2)
                {
                    this.Raise(new eGoPassive());
                }
            }

            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eAnnounce)
                };
            }
        }

        private class SendErBestAndWaitForRecState : State
        {

        }

        private void HandleAnnounce()
        {
            if (this.ErBestVar.Item2 > ((Tuple<Machine, int>)this.Payload).Item2)
            {
                this.ErBestVar = (Tuple<Machine, int>)this.Payload;
            }
        }

        private void UpdateState()
        {
            if (this.ErBestVar.Item2 > ((Tuple<Machine, int>)this.Payload).Item2)
            {
                this.ErBestVar = (Tuple<Machine, int>)this.Payload;
            }
        }

        [Ghost]
        private bool IsAnnounceReceiptTimeOut()
        {
            if (Model.Havoc.Boolean)
            {
                Console.WriteLine("Clock: IsAnnounceReceiptTimeOut: True");
                return true;
            }
            else
            {
                Console.WriteLine("Clock: IsAnnounceReceiptTimeOut: False");
                return false;
            }
        }

        [Ghost]
        private bool IsThreeAnnounceReceiptTimeOut()
        {
            if (Model.Havoc.Boolean)
            {
                Console.WriteLine("Clock: IsThreeAnnounceReceiptTimeOut: True");
                return true;
            }
            else
            {
                Console.WriteLine("Clock: IsThreeAnnounceReceiptTimeOut: False");
                return false;
            }
        }

        protected override Dictionary<Type, StateTransitions> DefineStepTransitions()
        {
            Dictionary<Type, StateTransitions> dict = new Dictionary<Type, StateTransitions>();

            StateTransitions initDict = new StateTransitions();
            initDict.Add(typeof(eInitialise), typeof(ConnectionInitialized));

            StateTransitions connectionInitializedDict = new StateTransitions();
            connectionInitializedDict.Add(typeof(ePowerUp), typeof(Initializing));

            StateTransitions initializingDict = new StateTransitions();
            initializingDict.Add(typeof(eUnit), typeof(Initializing));
            initializingDict.Add(typeof(eLocal), typeof(Listening));

            StateTransitions listeningDict = new StateTransitions();
            listeningDict.Add(typeof(eStateDecisionEvent), typeof(DeferAll));
            listeningDict.Add(typeof(eGoMaster), typeof(Master));

            StateTransitions masterDict = new StateTransitions();
            masterDict.Add(typeof(eUnit), typeof(Master));
            masterDict.Add(typeof(eStateDecisionEvent), typeof(DeferAll));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(ConnectionInitialized), connectionInitializedDict);
            dict.Add(typeof(Initializing), initializingDict);
            dict.Add(typeof(Listening), listeningDict);
            dict.Add(typeof(Master), masterDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings listeningDict = new ActionBindings();
            listeningDict.Add(typeof(eAnnounce), new Action(HandleAnnounce));

            ActionBindings masterDict = new ActionBindings();
            masterDict.Add(typeof(eAnnounce), new Action(HandleAnnounce));

            dict.Add(typeof(Listening), listeningDict);
            dict.Add(typeof(Master), masterDict);

            return dict;
        }
    }

    #endregion

    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements the linear topology:
    /// OC1 -- BC1 -- BC2 -- OC2
    /// 
    /// If Rank = 1 it implies that the clock is a stratum 1 clock.
    /// </summary>
    class LinearTopology
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Registering events to the runtime.\n");
            Runtime.RegisterNewEvent(typeof(eLocal));
            Runtime.RegisterNewEvent(typeof(eUnit));
            Runtime.RegisterNewEvent(typeof(eStateDecisionEvent));
            Runtime.RegisterNewEvent(typeof(eAck));
            Runtime.RegisterNewEvent(typeof(eGoMaster));
            Runtime.RegisterNewEvent(typeof(eGoSlave));
            Runtime.RegisterNewEvent(typeof(eGoPassive));
            Runtime.RegisterNewEvent(typeof(eDoneStateChange));
            Runtime.RegisterNewEvent(typeof(eAnnounce));
            Runtime.RegisterNewEvent(typeof(eInitialise));
            Runtime.RegisterNewEvent(typeof(ePowerUp));
            Runtime.RegisterNewEvent(typeof(eErBest));
            Runtime.RegisterNewEvent(typeof(eUpdateParentGM));

            Console.WriteLine("Registering state machines to the runtime.\n");
            Runtime.RegisterNewMachine(typeof(GodMachine));
            Runtime.RegisterNewMachine(typeof(Clock));
            Runtime.RegisterNewMachine(typeof(PortMachine));

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
