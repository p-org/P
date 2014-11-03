using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace GermanRacey
{
    #region Events

    internal class eLocal : Event { }

    internal class eStop : Event { }

    internal class eWait : Event { }

    internal class eNormal : Event { }

    internal class eNeedInvalidate : Event { }

    internal class eInvalidate : Event { }

    internal class eInvalidateAck : Event { }

    internal class eGrant : Event { }

    internal class eAck : Event { }

    internal class eGrantExcl : Event { }

    internal class eGrantShare : Event { }

    internal class eAskShare : Event
    {
        public eAskShare(Object payload)
            : base(payload)
        { }
    }

    internal class eAskExcl : Event
    {
        public eAskExcl(Object payload)
            : base(payload)
        { }
    }

    internal class eShareReq : Event
    {
        public eShareReq(Object payload)
            : base(payload)
        { }
    }

    internal class eExclReq : Event
    {
        public eExclReq(Object payload)
            : base(payload)
        { }
    }

    #endregion

    internal class Message
    {
        public int Id;
        public bool Pending;

        public Message(int id, bool pending)
        {
            this.Id = id;
            this.Pending = pending;
        }
    }

    #region Machines

    [Main]
    internal class Host : Machine
    {
        private List<Machine> Clients;
        private Machine CPU;
        private Machine CurrentClient;

        private List<Machine> SharerList;

        private bool isCurrReqExcl;
        private bool isExclGranted;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Host;

                var n = (int)this.Payload;

                Console.WriteLine("[Host] Initializing ...\n");

                machine.Clients = new List<Machine>();
                machine.SharerList = new List<Machine>();
                machine.CurrentClient = null;

                for (int idx = 0; idx < n; idx++)
                {
                    machine.Clients.Add(Machine.Factory.CreateMachine<Client>(
                        new Tuple<int, Machine, bool>(idx, machine, false)));
                }

                machine.CPU = Machine.Factory.CreateMachine<CPU>(new Tuple<Machine, List<Machine>>(
                    machine, machine.Clients));
                Runtime.Assert(machine.SharerList.Count == 0);

                this.Raise(new eLocal());
            }
        }

        private class Receiving : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Host;

                Console.WriteLine("[Host] Receiving ...\n");
            }

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
            protected override void OnEntry()
            {
                var machine = this.Machine as Host;

                Console.WriteLine("[Host] ShareRequest ...\n");

                var id = ((Message)this.Payload).Id;
                ((Message)this.Payload).Pending = true;
                machine.CurrentClient = machine.Clients[id];
                machine.isCurrReqExcl = false;

                this.Raise(new eLocal());
            }
        }

        private class ExclRequest : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Host;

                Console.WriteLine("[Host] ExclRequest ...\n");

                var id = ((Message)this.Payload).Id;
                ((Message)this.Payload).Pending = true;
                machine.CurrentClient = machine.Clients[id];
                machine.isCurrReqExcl = true;

                this.Raise(new eLocal());
            }
        }

        private class Processing : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Host;

                Console.WriteLine("[Host] Processing ...\n");

                if (machine.isCurrReqExcl || machine.isExclGranted)
                {
                    this.Raise(new eNeedInvalidate());
                }
                else
                {
                    this.Raise(new eGrant());
                }
            }
        }

        private class GrantingAccess : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Host;

                Console.WriteLine("[Host] GrantingAccess ...\n");

                if (machine.isCurrReqExcl)
                {
                    machine.isExclGranted = true;
                    this.Send(machine.CurrentClient, new eGrantExcl());
                }
                else
                {
                    this.Send(machine.CurrentClient, new eGrantShare());
                }

                machine.SharerList.Add(machine.CurrentClient);

                this.Send(machine.CPU, new eAck());

                this.Raise(new eLocal());
            }
        }

        private class CheckingInvariant : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Host;

                Console.WriteLine("[Host] CheckingInvariant ...\n");

                if (machine.SharerList.Count == 0)
                {
                    this.Raise(new eGrant());
                }
                else
                {
                    foreach (var sharer in machine.SharerList)
                    {
                        this.Send(sharer, new eInvalidate());
                    }
                }
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eShareReq),
                    typeof(eExclReq),
                    typeof(eStop)
                };
            }
        }

        private void RecAck()
        {
            Console.WriteLine("[Host] RecAck ...\n");

            this.SharerList.RemoveAt(0);
            if (this.SharerList.Count == 0)
            {
                this.Raise(new eGrant());
            }
        }

        private void Stop()
        {
            Console.WriteLine("[Host] Stopping ...\n");

            this.Delete();
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(Receiving));

            StepStateTransitions receivingDict = new StepStateTransitions();
            receivingDict.Add(typeof(eShareReq), typeof(ShareRequest));
            receivingDict.Add(typeof(eExclReq), typeof(ExclRequest));

            StepStateTransitions shareRequestDict = new StepStateTransitions();
            shareRequestDict.Add(typeof(eLocal), typeof(Processing));

            StepStateTransitions exclRequestDict = new StepStateTransitions();
            exclRequestDict.Add(typeof(eLocal), typeof(Processing));

            StepStateTransitions processingDict = new StepStateTransitions();
            processingDict.Add(typeof(eNeedInvalidate), typeof(CheckingInvariant));
            processingDict.Add(typeof(eGrant), typeof(GrantingAccess));

            StepStateTransitions grantingAccessDict = new StepStateTransitions();
            grantingAccessDict.Add(typeof(eLocal), typeof(Receiving));

            StepStateTransitions checkingInvariantDict = new StepStateTransitions();
            checkingInvariantDict.Add(typeof(eGrant), typeof(GrantingAccess));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(Receiving), receivingDict);
            dict.Add(typeof(ShareRequest), shareRequestDict);
            dict.Add(typeof(ExclRequest), exclRequestDict);
            dict.Add(typeof(Processing), processingDict);
            dict.Add(typeof(GrantingAccess), grantingAccessDict);
            dict.Add(typeof(CheckingInvariant), checkingInvariantDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings receivingDict = new ActionBindings();
            receivingDict.Add(typeof(eStop), new Action(Stop));

            ActionBindings checkingInvariantDict = new ActionBindings();
            checkingInvariantDict.Add(typeof(eInvalidateAck), new Action(RecAck));

            dict.Add(typeof(Receiving), receivingDict);
            dict.Add(typeof(CheckingInvariant), checkingInvariantDict);

            return dict;
        }
    }

    internal class Client : Machine
    {
        private int Id;

        private Machine Host;

        private bool Pending;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Client;

                Console.WriteLine("[Client] Initializing ...\n");

                machine.Id = ((Tuple<int, Machine, bool>)this.Payload).Item1;
                machine.Host = ((Tuple<int, Machine, bool>)this.Payload).Item2;
                machine.Pending = ((Tuple<int, Machine, bool>)this.Payload).Item3;

                this.Raise(new eLocal());
            }
        }

        private class Invalid : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Client;

                Console.WriteLine("[Client] Invalid ...\n");
            }
        }

        private class AskedShare : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Client;

                Console.WriteLine("[Client] AskedShare ...\n");

                var message = new Message(machine.Id, machine.Pending);
                this.Send(machine.Host, new eShareReq(message));
                machine.Pending = message.Pending;

                this.Raise(new eLocal());
            }
        }

        private class AskedExcl : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Client;

                Console.WriteLine("[Client] AskedExcl ...\n");

                var message = new Message(machine.Id, machine.Pending);
                this.Send(machine.Host, new eExclReq(message));
                machine.Pending = message.Pending;

                this.Raise(new eLocal());
            }
        }

        private class InvalidWaiting : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Client;

                Console.WriteLine("[Client] InvalidWaiting ...\n");
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eAskShare),
                    typeof(eAskExcl),
                    typeof(eStop)
                };
            }
        }

        private class AskedEx2 : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Client;

                Console.WriteLine("[Client] AskedEx2 ...\n");

                var message = new Message(machine.Id, machine.Pending);
                this.Send(machine.Host, new eExclReq(message));
                machine.Pending = message.Pending;

                this.Raise(new eLocal());
            }
        }

        private class Sharing : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Client;

                Console.WriteLine("[Client] Sharing ...\n");

                machine.Pending = false;
            }
        }

        private class ShareWaiting : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Client;

                Console.WriteLine("[Client] ShareWaiting ...\n");
            }
        }

        private class Exclusive : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Client;

                Console.WriteLine("[Client] Exclusive ...\n");

                machine.Pending = false;
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
            protected override void OnEntry()
            {
                var machine = this.Machine as Client;

                Console.WriteLine("[Client] Invalidating ...\n");

                if (machine.Pending)
                {
                    this.Raise(new eWait());
                }
                else
                {
                    this.Raise(new eNormal());
                }
            }
        }

        private void Ack()
        {
            var cpu = (Machine)this.Payload;

            this.Send(cpu, new eAck());
        }

        private void Stop()
        {
            Console.WriteLine("[Host] Stopping ...\n");

            this.Delete();
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(Invalid));

            StepStateTransitions invalidDict = new StepStateTransitions();
            invalidDict.Add(typeof(eAskShare), typeof(AskedShare));
            invalidDict.Add(typeof(eAskExcl), typeof(AskedExcl));
            invalidDict.Add(typeof(eInvalidate), typeof(Invalidating));
            invalidDict.Add(typeof(eGrantExcl), typeof(Exclusive));
            invalidDict.Add(typeof(eGrantShare), typeof(Sharing));

            StepStateTransitions askedShareDict = new StepStateTransitions();
            askedShareDict.Add(typeof(eLocal), typeof(InvalidWaiting));

            StepStateTransitions askedExclDict = new StepStateTransitions();
            askedExclDict.Add(typeof(eLocal), typeof(InvalidWaiting));

            StepStateTransitions invalidWaitingDict = new StepStateTransitions();
            invalidWaitingDict.Add(typeof(eInvalidate), typeof(Invalidating));
            invalidWaitingDict.Add(typeof(eGrantExcl), typeof(Exclusive));
            invalidWaitingDict.Add(typeof(eGrantShare), typeof(Sharing));

            StepStateTransitions askedEx2Dict = new StepStateTransitions();
            askedEx2Dict.Add(typeof(eLocal), typeof(ShareWaiting));

            StepStateTransitions sharingDict = new StepStateTransitions();
            sharingDict.Add(typeof(eInvalidate), typeof(Invalidating));
            sharingDict.Add(typeof(eGrantShare), typeof(Sharing));
            sharingDict.Add(typeof(eGrantExcl), typeof(Exclusive));
            sharingDict.Add(typeof(eAskExcl), typeof(AskedEx2));

            StepStateTransitions shareWaitingDict = new StepStateTransitions();
            shareWaitingDict.Add(typeof(eInvalidate), typeof(Invalidating));
            shareWaitingDict.Add(typeof(eGrantShare), typeof(Sharing));
            shareWaitingDict.Add(typeof(eGrantExcl), typeof(Exclusive));

            StepStateTransitions exclusiveDict = new StepStateTransitions();
            exclusiveDict.Add(typeof(eInvalidate), typeof(Invalidating));
            exclusiveDict.Add(typeof(eGrantShare), typeof(Sharing));
            exclusiveDict.Add(typeof(eGrantExcl), typeof(Exclusive));

            StepStateTransitions invalidatingDict = new StepStateTransitions();
            invalidatingDict.Add(typeof(eWait), typeof(InvalidWaiting));
            invalidatingDict.Add(typeof(eNormal), typeof(Invalid));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(Invalid), invalidDict);
            dict.Add(typeof(AskedShare), askedShareDict);
            dict.Add(typeof(AskedExcl), askedExclDict);
            dict.Add(typeof(InvalidWaiting), invalidWaitingDict);
            dict.Add(typeof(AskedEx2), askedEx2Dict);
            dict.Add(typeof(Sharing), sharingDict);
            dict.Add(typeof(ShareWaiting), shareWaitingDict);
            dict.Add(typeof(Exclusive), exclusiveDict);
            dict.Add(typeof(Invalidating), invalidatingDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings invalidDict = new ActionBindings();
            invalidDict.Add(typeof(eStop), new Action(Stop));

            ActionBindings sharingDict = new ActionBindings();
            sharingDict.Add(typeof(eStop), new Action(Stop));
            sharingDict.Add(typeof(eAskShare), new Action(Ack));

            ActionBindings exclusiveDict = new ActionBindings();
            exclusiveDict.Add(typeof(eStop), new Action(Stop));

            dict.Add(typeof(Invalid), invalidDict);
            dict.Add(typeof(Sharing), sharingDict);
            dict.Add(typeof(Exclusive), exclusiveDict);

            return dict;
        }
    }

    internal class CPU : Machine
    {
        private Machine Host;
        private List<Machine> Cache;

        private int QueryCounter;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as CPU;

                Console.WriteLine("[CPU] Initializing ...\n");

                machine.Host = ((Tuple<Machine, List<Machine>>)this.Payload).Item1;
                machine.Cache = ((Tuple<Machine, List<Machine>>)this.Payload).Item2;
                machine.QueryCounter = 0;

                this.Raise(new eLocal());
            }
        }

        private class Requesting : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as CPU;

                if (machine.QueryCounter > 9)
                {
                    Console.WriteLine("[CPU] Stopping ...\n");

                    this.Send(machine.Host, new eStop());

                    foreach (var c in machine.Cache)
                    {
                        this.Send(c, new eStop());
                    }

                    this.Delete();
                }

                Console.WriteLine("[CPU] Sending request {0} ...\n", machine.QueryCounter);

                if (Model.Havoc.Boolean)
                {
                    if (Model.Havoc.Boolean)
                    {
                        this.Send(machine.Cache[0], new eAskShare(machine));
                    }
                    else
                    {
                        this.Send(machine.Cache[0], new eAskExcl(machine));
                    }
                }
                else if (Model.Havoc.Boolean)
                {
                    if (Model.Havoc.Boolean)
                    {
                        this.Send(machine.Cache[1], new eAskShare(machine));
                    }
                    else
                    {
                        this.Send(machine.Cache[1], new eAskExcl(machine));
                    }
                }
                else
                {
                    if (Model.Havoc.Boolean)
                    {
                        this.Send(machine.Cache[2], new eAskShare(machine));
                    }
                    else
                    {
                        this.Send(machine.Cache[2], new eAskExcl(machine));
                    }
                }

                machine.QueryCounter++;
            }
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(Requesting));

            StepStateTransitions requestingDict = new StepStateTransitions();
            requestingDict.Add(typeof(eAck), typeof(Requesting));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(Requesting), requestingDict);

            return dict;
        }
    }

    #endregion
}
