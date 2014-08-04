//-----------------------------------------------------------------------
// <copyright file="German.cs" company="Microsoft">
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

namespace German
{
    #region Events

    internal class eUnit : Event { }
    internal class eNeedInvalidate : Event { }
    internal class eInvalidateAck : Event { }
    internal class eGrant : Event { }
    internal class eAskShare : Event { }
    internal class eAskExcl : Event { }
    internal class eInvalidate : Event { }
    internal class eGrantExcl : Event { }
    internal class eGrantShare : Event { }
    internal class eNormal : Event { }
    internal class eWait : Event { }

    internal class eReqShare : Event
    {
        public eReqShare(Object payload)
            : base(payload)
        { }
    }

    internal class eReqExcl : Event
    {
        public eReqExcl(Object payload)
            : base(payload)
        { }
    }

    internal class eInvalidateSharers : Event
    {
        public eInvalidateSharers(Object payload)
            : base(payload)
        { }
    }

    internal class eSharerId : Event
    {
        public eSharerId(Object payload)
            : base(payload)
        { }
    }

    #endregion

    #region Machines

    [Main]
    internal class Host : Machine
    {
        private Machine CurrentClient;
        private Machine[] Clients;
        private Machine CurrentCPU;
        private List<Machine> SharerList;
        private bool IsCurrReqExcl;
        private bool IsExclGranted;
        private int I;
        private int S;

        [Initial]
        private class Init : State
        {
            public override void OnEntry()
            {
                Console.WriteLine("Initializing Host");

                (this.Machine as Host).Clients = new Machine[3];

                (this.Machine as Host).Clients[0] =
                    Machine.Factory.CreateMachine<Client>(new Tuple<Machine, bool>(this.Machine, false));

                (this.Machine as Host).Clients[1] =
                    Machine.Factory.CreateMachine<Client>(new Tuple<Machine, bool>(this.Machine, false));

                (this.Machine as Host).Clients[2] =
                    Machine.Factory.CreateMachine<Client>(new Tuple<Machine, bool>(this.Machine, false));

                (this.Machine as Host).CurrentClient = null;

                (this.Machine as Host).CurrentCPU =
                    Machine.Factory.CreateMachine<CPU>((this.Machine as Host).Clients);

                (this.Machine as Host).SharerList = new List<Machine>();

                Runtime.Assert((this.Machine as Host).SharerList.Count == 0);
                
                this.Raise(new eUnit());
            }
        }

        private class Receive : State
        {
            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eInvalidateAck)
                };
            }
        }

        private class ShareRequest : State
        {
            public override void OnEntry()
            {
                (this.Machine as Host).CurrentClient = (Machine)this.Payload;
                (this.Machine as Host).IsCurrReqExcl = false;

                this.Raise(new eUnit());
            }
        }

        private class ExclRequest : State
        {
            public override void OnEntry()
            {
                (this.Machine as Host).CurrentClient = (Machine)this.Payload;
                (this.Machine as Host).IsCurrReqExcl = true;

                this.Raise(new eUnit());
            }
        }

        private class ProcessReq : State
        {
            public override void OnEntry()
            {
                if ((this.Machine as Host).IsCurrReqExcl || (this.Machine as Host).IsExclGranted)
                {
                    Console.WriteLine("{0} raising event {1}", this.Machine, typeof(eInvalidate));
                    this.Raise(new eInvalidate());
                }
                else
                {
                    Console.WriteLine("{0} raising event {1}", this.Machine, typeof(eGrant));
                    this.Raise(new eGrant());
                }
            }
        }

        private class Inv : State
        {
            public override void OnEntry()
            {
                (this.Machine as Host).I = 0;
                (this.Machine as Host).S = (this.Machine as Host).SharerList.Count;

                if ((this.Machine as Host).S == 0)
                {
                    this.Raise(new eGrant());
                }

                while ((this.Machine as Host).I < (this.Machine as Host).S)
                {
                    Console.WriteLine("{0} sending event {1} to {2}", this.Machine, typeof(eInvalidate),
                        (this.Machine as Host).SharerList[(this.Machine as Host).I]);
                    this.Send((this.Machine as Host).SharerList[(this.Machine as Host).I],
                        new eInvalidate());
                    (this.Machine as Host).I++;
                }
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eReqShare),
                    typeof(eReqExcl)
                };
            }
        }

        private class GrantAccess : State
        {
            public override void OnEntry()
            {
                if ((this.Machine as Host).IsCurrReqExcl)
                {
                    (this.Machine as Host).IsExclGranted = true;

                    Console.WriteLine("{0} sending event {1} to {2}", this.Machine,
                        typeof(eGrantExcl), (this.Machine as Host).CurrentClient);
                    this.Send((this.Machine as Host).CurrentClient, new eGrantExcl());
                }
                else
                {
                    Console.WriteLine("{0} sending event {1} to {2}", this.Machine,
                        typeof(eGrantShare), (this.Machine as Host).CurrentClient);
                    this.Send((this.Machine as Host).CurrentClient, new eGrantShare());
                }

                (this.Machine as Host).SharerList.Insert(0, (this.Machine as Host).CurrentClient);

                this.Raise(new eUnit());
            }
        }

        private void RecAck()
        {
            this.SharerList.RemoveAt(0);
            this.S = this.SharerList.Count;

            if (this.S == 0)
            {
                Console.WriteLine("{0} raising event {1}", this, typeof(eGrant));
                this.Raise(new eGrant());
            }
        }

        protected override Dictionary<Type, StateTransitions> DefineStepTransitions()
        {
            Dictionary<Type, StateTransitions> dict = new Dictionary<Type, StateTransitions>();

            StateTransitions initDict = new StateTransitions();
            initDict.Add(typeof(eUnit), typeof(Receive));

            StateTransitions receiveDict = new StateTransitions();
            receiveDict.Add(typeof(eReqShare), typeof(ShareRequest));
            receiveDict.Add(typeof(eReqExcl), typeof(ExclRequest));

            StateTransitions shareRequestDict = new StateTransitions();
            shareRequestDict.Add(typeof(eUnit), typeof(ProcessReq));

            StateTransitions exclRequestDict = new StateTransitions();
            exclRequestDict.Add(typeof(eUnit), typeof(ProcessReq));

            StateTransitions processReqDict = new StateTransitions();
            processReqDict.Add(typeof(eInvalidate), typeof(Inv));
            processReqDict.Add(typeof(eGrant), typeof(GrantAccess));

            StateTransitions invDict = new StateTransitions();
            invDict.Add(typeof(eGrant), typeof(GrantAccess));

            StateTransitions grantAccessDict = new StateTransitions();
            grantAccessDict.Add(typeof(eUnit), typeof(Receive));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(Receive), receiveDict);
            dict.Add(typeof(ShareRequest), shareRequestDict);
            dict.Add(typeof(ExclRequest), exclRequestDict);
            dict.Add(typeof(ProcessReq), processReqDict);
            dict.Add(typeof(Inv), invDict);
            dict.Add(typeof(GrantAccess), grantAccessDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings invDict = new ActionBindings();
            invDict.Add(typeof(eInvalidateAck), new Action(RecAck));

            dict.Add(typeof(Inv), invDict);

            return dict;
        }
    }

    internal class Client : Machine
    {
        private Machine Host;
        private bool Pending;

        [Initial]
        private class Init : State
        {
            public override void OnEntry()
            {
                Console.WriteLine("Initializing Client");

                (this.Machine as Client).Host = ((Tuple<Machine, bool>)this.Payload).Item1;
                (this.Machine as Client).Pending = ((Tuple<Machine, bool>)this.Payload).Item2;

                this.Raise(new eUnit());
            }
        }

        private class Invalid : State
        {

        }

        private class AskedShare : State
        {
            public override void OnEntry()
            {
                Console.WriteLine("{0} sending event {1} to {2}", this.Machine,
                        typeof(eReqShare), (this.Machine as Client).Host);
                this.Send((this.Machine as Client).Host, new eReqShare(this.Machine));

                (this.Machine as Client).Pending = true;

                this.Raise(new eUnit());
            }

            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eAskShare),
                    typeof(eAskExcl)
                };
            }
        }

        private class AskedExcl : State
        {
            public override void OnEntry()
            {
                Console.WriteLine("{0} sending event {1} to {2}", this.Machine,
                        typeof(eReqExcl), (this.Machine as Client).Host);
                this.Send((this.Machine as Client).Host, new eReqExcl(this.Machine));

                (this.Machine as Client).Pending = true;

                this.Raise(new eUnit());
            }

            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eAskShare),
                    typeof(eAskExcl)
                };
            }
        }

        private class InvalidWait : State
        {
            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eAskShare),
                    typeof(eAskExcl)
                };
            }
        }

        private class AskedEx2 : State
        {
            public override void OnEntry()
            {
                Console.WriteLine("{0} sending event {1} to {2}", this.Machine,
                        typeof(eReqExcl), (this.Machine as Client).Host);
                this.Send((this.Machine as Client).Host, new eReqExcl(this.Machine));

                (this.Machine as Client).Pending = true;

                this.Raise(new eUnit());
            }
        }

        private class Sharing : State
        {
            public override void OnEntry()
            {
                Console.WriteLine("Client is sharing ...");
                (this.Machine as Client).Pending = false;
            }
        }

        private class SharingWait : State
        {
            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eAskShare),
                    typeof(eAskExcl)
                };
            }
        }

        private class Exclusive : State
        {
            public override void OnEntry()
            {
                Console.WriteLine("Client is exclusive ...");
                (this.Machine as Client).Pending = false;
            }

            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eAskShare),
                    typeof(eAskExcl)
                };
            }
        }

        private class Invalidating : State
        {
            public override void OnEntry()
            {
                Console.WriteLine("Client is invalidating ...");

                Console.WriteLine("{0} sending event {1} to {2}", this.Machine,
                        typeof(eInvalidateAck), (this.Machine as Client).Host);
                this.Send((this.Machine as Client).Host, new eInvalidateAck());

                if ((this.Machine as Client).Pending)
                {
                    Console.WriteLine("{0} raising event {1}", this.Machine, typeof(eWait));
                    this.Raise(new eWait());
                }
                else
                {
                    Console.WriteLine("{0} raising event {1}", this.Machine, typeof(eNormal));
                    this.Raise(new eNormal());
                }
            }
        }

        protected override Dictionary<Type, StateTransitions> DefineStepTransitions()
        {
            Dictionary<Type, StateTransitions> dict = new Dictionary<Type, StateTransitions>();

            StateTransitions initDict = new StateTransitions();
            initDict.Add(typeof(eUnit), typeof(Invalid));

            StateTransitions invalidDict = new StateTransitions();
            invalidDict.Add(typeof(eAskShare), typeof(AskedShare));
            invalidDict.Add(typeof(eAskExcl), typeof(AskedExcl));
            invalidDict.Add(typeof(eInvalidate), typeof(Invalidating));
            invalidDict.Add(typeof(eGrantExcl), typeof(Exclusive));
            invalidDict.Add(typeof(eGrantShare), typeof(Sharing));

            StateTransitions askedShareDict = new StateTransitions();
            askedShareDict.Add(typeof(eUnit), typeof(InvalidWait));

            StateTransitions askedExclDict = new StateTransitions();
            askedExclDict.Add(typeof(eUnit), typeof(InvalidWait));

            StateTransitions invalidWaitDict = new StateTransitions();
            invalidWaitDict.Add(typeof(eInvalidate), typeof(Invalidating));
            invalidWaitDict.Add(typeof(eGrantExcl), typeof(Exclusive));
            invalidWaitDict.Add(typeof(eGrantShare), typeof(Sharing));

            StateTransitions askedEx2Dict = new StateTransitions();
            askedEx2Dict.Add(typeof(eUnit), typeof(SharingWait));

            StateTransitions sharingDict = new StateTransitions();
            sharingDict.Add(typeof(eInvalidate), typeof(Invalidating));
            sharingDict.Add(typeof(eGrantShare), typeof(Sharing));
            sharingDict.Add(typeof(eGrantExcl), typeof(Exclusive));
            sharingDict.Add(typeof(eAskShare), typeof(Sharing));
            sharingDict.Add(typeof(eAskExcl), typeof(AskedEx2));

            StateTransitions sharingWaitDict = new StateTransitions();
            sharingWaitDict.Add(typeof(eInvalidate), typeof(Invalidating));
            sharingWaitDict.Add(typeof(eGrantShare), typeof(SharingWait));
            sharingWaitDict.Add(typeof(eGrantExcl), typeof(Exclusive));

            StateTransitions exclusiveDict = new StateTransitions();
            exclusiveDict.Add(typeof(eInvalidate), typeof(Invalidating));
            exclusiveDict.Add(typeof(eGrantShare), typeof(Sharing));
            exclusiveDict.Add(typeof(eGrantExcl), typeof(Exclusive));

            StateTransitions invalidatingDict = new StateTransitions();
            invalidatingDict.Add(typeof(eWait), typeof(InvalidWait));
            invalidatingDict.Add(typeof(eNormal), typeof(Invalid));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(Invalid), invalidDict);
            dict.Add(typeof(AskedShare), askedShareDict);
            dict.Add(typeof(AskedExcl), askedExclDict);
            dict.Add(typeof(InvalidWait), invalidWaitDict);
            dict.Add(typeof(AskedEx2), askedEx2Dict);
            dict.Add(typeof(Sharing), sharingDict);
            dict.Add(typeof(SharingWait), sharingWaitDict);
            dict.Add(typeof(Exclusive), exclusiveDict);
            dict.Add(typeof(Invalidating), invalidatingDict);

            return dict;
        }
    }

    [Ghost]
    internal class CPU : Machine
    {
        private Machine[] Cache;

        [Initial]
        private class Init : State
        {
            public override void OnEntry()
            {
                Console.WriteLine("Initializing CPU");

                (this.Machine as CPU).Cache = (Machine[])this.Payload;

                this.Raise(new eUnit());
            }
        }

        private class MakeReq : State
        {
            public override void OnEntry()
            {
                if (Model.Havoc.Boolean)
                {
                    if (Model.Havoc.Boolean)
                    {
                        Console.WriteLine("CPU is making a new request to cache {0}: {1}", 0, typeof(eAskShare));
                        this.Send((this.Machine as CPU).Cache[0], new eAskShare());
                    }
                    else
                    {
                        Console.WriteLine("CPU is making a new request to cache {0}: {1}", 0, typeof(eAskExcl));
                        this.Send((this.Machine as CPU).Cache[0], new eAskExcl());
                    }
                }
                else if (Model.Havoc.Boolean)
                {
                    if (Model.Havoc.Boolean)
                    {
                        Console.WriteLine("CPU is making a new request to cache {0}: {1}", 1, typeof(eAskShare));
                        this.Send((this.Machine as CPU).Cache[1], new eAskShare());
                    }
                    else
                    {
                        Console.WriteLine("CPU is making a new request to cache {0}: {1}", 1, typeof(eAskExcl));
                        this.Send((this.Machine as CPU).Cache[1], new eAskExcl());
                    }
                }
                else
                {
                    if (Model.Havoc.Boolean)
                    {
                        Console.WriteLine("CPU is making a new request to cache {0}: {1}", 2, typeof(eAskShare));
                        this.Send((this.Machine as CPU).Cache[2], new eAskShare());
                    }
                    else
                    {
                        Console.WriteLine("CPU is making a new request to cache {0}: {1}", 2, typeof(eAskExcl));
                        this.Send((this.Machine as CPU).Cache[2], new eAskExcl());
                    }
                }

                Model.Sleep(5000);

                this.Raise(new eUnit());
            }
        }

        protected override Dictionary<Type, StateTransitions> DefineStepTransitions()
        {
            Dictionary<Type, StateTransitions> dict = new Dictionary<Type, StateTransitions>();

            StateTransitions initDict = new StateTransitions();
            initDict.Add(typeof(eUnit), typeof(MakeReq));

            StateTransitions makeReqDict = new StateTransitions();
            makeReqDict.Add(typeof(eUnit), typeof(MakeReq));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(MakeReq), makeReqDict);

            return dict;
        }
    }

    #endregion

    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements German's cache coherence protocol.
    /// </summary>
    class German
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Registering events to the runtime.\n");
            Runtime.RegisterNewEvent(typeof(eUnit));
            Runtime.RegisterNewEvent(typeof(eNeedInvalidate));
            Runtime.RegisterNewEvent(typeof(eInvalidateAck));
            Runtime.RegisterNewEvent(typeof(eGrant));
            Runtime.RegisterNewEvent(typeof(eAskShare));
            Runtime.RegisterNewEvent(typeof(eAskExcl));
            Runtime.RegisterNewEvent(typeof(eInvalidate));
            Runtime.RegisterNewEvent(typeof(eGrantExcl));
            Runtime.RegisterNewEvent(typeof(eGrantShare));
            Runtime.RegisterNewEvent(typeof(eNormal));
            Runtime.RegisterNewEvent(typeof(eWait));
            Runtime.RegisterNewEvent(typeof(eReqShare));
            Runtime.RegisterNewEvent(typeof(eReqExcl));
            Runtime.RegisterNewEvent(typeof(eInvalidateSharers));
            Runtime.RegisterNewEvent(typeof(eSharerId));

            Console.WriteLine("Registering state machines to the runtime.\n");
            Runtime.RegisterNewMachine(typeof(Host));
            Runtime.RegisterNewMachine(typeof(Client));
            Runtime.RegisterNewMachine(typeof(CPU));

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
