//-----------------------------------------------------------------------
// <copyright file="PingPongFarm.cs" company="Microsoft">
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

namespace PingPongFarm
{
    #region Events

    internal class Ping : Event { }
    internal class Pong : Event { }
    internal class Stop : Event { }
    internal class Unit : Event { }

    internal class Configuration : Event
    {
        public Configuration(Object payload)
            : base(payload)
        { }
    }

    #endregion

    #region Machines

    internal class Server : Machine
    {
        private Machine Master;
        private Machine Client;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                (this.Machine as Server).Master = (Machine)this.Payload;
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(Ping)
                };
            }
        }

        private class Configuring : State
        {
            protected override void OnEntry()
            {
                (this.Machine as Server).Client = (Machine)this.Payload;
                this.Raise(new Unit());
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(Ping),
                    typeof(Stop)
                };
            }
        }

        private class Playing : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("{0} sending event {1} to {2}\n", this.Machine,
                        typeof(Pong), (this.Machine as Server).Client);
                this.Send((this.Machine as Server).Client, new Pong());
            }
        }

        private class Closing : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("{0} sending event {1} to {2}\n", this,
                        typeof(Stop), (this.Machine as Server).Master);
                this.Send((this.Machine as Server).Master, new Stop());
                Console.WriteLine("Server stopped.\n");
                this.Delete();
            }

            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(Ping),
                    typeof(Stop)
                };
            }
        }

        private void SendPong()
        {
            Console.WriteLine("{0} sending event {1} to {2}\n", this,
                        typeof(Pong), this.Client);
            this.Send(this.Client, new Pong());
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(Configuration), typeof(Configuring));

            StepStateTransitions configuringDict = new StepStateTransitions();
            configuringDict.Add(typeof(Unit), typeof(Playing));

            StepStateTransitions playingDict = new StepStateTransitions();
            playingDict.Add(typeof(Stop), typeof(Closing));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(Configuring), configuringDict);
            dict.Add(typeof(Playing), playingDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings playingDict = new ActionBindings();
            playingDict.Add(typeof(Unit), new Action(SendPong));
            playingDict.Add(typeof(Ping), new Action(SendPong));

            dict.Add(typeof(Playing), playingDict);

            return dict;
        }
    }

    internal class Client : Machine
    {
        private Machine Server;
        private int Turns;
        private int Counter;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                (this.Machine as Client).Counter = 0;
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(Pong),
                    typeof(Stop)
                };
            }
        }

        private class Configuring : State
        {
            protected override void OnEntry()
            {
                Tuple<Machine, int> pair = (Tuple<Machine, int>)this.Payload;

                (this.Machine as Client).Server = pair.Item1;
                (this.Machine as Client).Turns = pair.Item2;
                this.Raise(new Unit());
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(Pong)
                };
            }
        }

        private class Playing : State
        {
            protected override void OnEntry()
            {
                if ((this.Machine as Client).Counter == (this.Machine as Client).Turns)
                {
                    Console.WriteLine("{0} sending event {1} to {2}\n", this.Machine,
                        typeof(Stop), (this.Machine as Client).Server);
                    this.Send((this.Machine as Client).Server, new Stop());
                    this.Raise(new Stop());
                }
            }
        }

        private class Closing : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("Client stopped.\n");
                this.Delete();
            }

            protected override HashSet<Type> DefineIgnoredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(Pong),
                    typeof(Stop)
                };
            }
        }

        private void SendPing()
        {
            this.Counter++;
            Console.WriteLine("\nTurns: {0} / {1}\n", this.Counter, this.Turns);

            Console.WriteLine("{0} sending event {1} to {2}\n", this,
                        typeof(Ping), this.Server);
            this.Send(this.Server, new Ping());
            this.Raise(new Unit());
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(Configuration), typeof(Configuring));

            StepStateTransitions configuringDict = new StepStateTransitions();
            configuringDict.Add(typeof(Unit), typeof(Playing));

            StepStateTransitions playingDict = new StepStateTransitions();
            playingDict.Add(typeof(Stop), typeof(Closing));
            playingDict.Add(typeof(Unit), typeof(Playing));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(Configuring), configuringDict);
            dict.Add(typeof(Playing), playingDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings playingDict = new ActionBindings();
            playingDict.Add(typeof(Pong), new Action(SendPing));

            dict.Add(typeof(Playing), playingDict);

            return dict;
        }
    }

    [Main]
    [Ghost]
    internal class Master : Machine
    {
        private Machine[] Servers;
        private Machine[] Clients;
        private int Size;
        private int Counter;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                (this.Machine as Master).Size = 5;

                (this.Machine as Master).Servers = new Machine[(this.Machine as Master).Size];
                (this.Machine as Master).Clients = new Machine[(this.Machine as Master).Size];
                (this.Machine as Master).Counter = 0;

                for (int idx = 0; idx < (this.Machine as Master).Size; idx++)
                {
                    Console.WriteLine("Master is spawning Server-{0}", idx);
                    (this.Machine as Master).Servers[idx] =
                        Machine.Factory.CreateMachine<Server>(this.Machine);

                    Console.WriteLine("Master is spawning Client-{0}", idx);
                    (this.Machine as Master).Clients[idx] =
                        Machine.Factory.CreateMachine<Client>(this.Machine);
                }

                this.Raise(new Unit());
            }
        }

        private class KickOffFarm : State
        {
            protected override void OnEntry()
            {
                for (int idx = 0; idx < (this.Machine as Master).Size; idx++)
                {
                    Console.WriteLine("{0} sending event {1} to {2}\n", this.Machine,
                        typeof(Configuration), (this.Machine as Master).Servers[idx]);
                    this.Send((this.Machine as Master).Servers[idx],
                        new Configuration((this.Machine as Master).Clients[idx]));

                    Console.WriteLine("{0} sending event {1} to {2}\n", this.Machine,
                        typeof(Configuration), (this.Machine as Master).Clients[idx]);
                    this.Send((this.Machine as Master).Clients[idx],
                        new Configuration(new Tuple<Machine, int>(
                            (this.Machine as Master).Servers[idx], idx + 1)
                            ));
                }
            }
        }

        private void StopIt()
        {
            this.Counter++;

            if (this.Counter == this.Servers.Length)
            {
                Console.WriteLine("Master stopped.\n");
                this.Delete();
            }
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(Unit), typeof(KickOffFarm));

            dict.Add(typeof(Init), initDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings kickOffFarmDict = new ActionBindings();
            kickOffFarmDict.Add(typeof(Stop), new Action(StopIt));

            dict.Add(typeof(KickOffFarm), kickOffFarmDict);

            return dict;
        }
    }

    #endregion

    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements a Ping Pong Farm. The master spawns
    /// servers and clients, which then proceed to play a game.
    /// </summary>
    class PingPongFarm
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Registering events to the runtime.\n");
            Runtime.RegisterNewEvent(typeof(Ping));
            Runtime.RegisterNewEvent(typeof(Pong));
            Runtime.RegisterNewEvent(typeof(Stop));
            Runtime.RegisterNewEvent(typeof(Unit));
            Runtime.RegisterNewEvent(typeof(Configuration));

            Console.WriteLine("Registering state machines to the runtime.\n");
            Runtime.RegisterNewMachine(typeof(Server));
            Runtime.RegisterNewMachine(typeof(Client));
            Runtime.RegisterNewMachine(typeof(Master));

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
