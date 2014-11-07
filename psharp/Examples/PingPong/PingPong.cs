//-----------------------------------------------------------------------
// <copyright file="PingPong.cs" company="Microsoft">
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

namespace PingPong
{
    #region Events

    internal class Ping : Event { }
    internal class Pong : Event { }
    internal class Stop : Event { }
    internal class Unit : Event { }

    #endregion

    #region Machines

    [Main]
    internal class Server : Machine
    {
        private Client Client;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                (this.Machine as Server).Client =
                    Machine.Factory.CreateMachine<Client>(this.Machine);
                this.Raise(new Unit());
            }
        }

        private class Playing : State
        {
            protected override void OnEntry()
            {
                this.Send((this.Machine as Server).Client, new Pong());
            }
        }

        private void SendPong()
        {
            this.Send(this.Client, new Pong());
        }

        private void StopIt()
        {
            Console.WriteLine("Server stopped.\n");
            this.Delete();
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(Unit), typeof(Playing));

            dict.Add(typeof(Init), initDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings playingDict = new ActionBindings();
            playingDict.Add(typeof(Unit), new Action(SendPong));
            playingDict.Add(typeof(Ping), new Action(SendPong));
            playingDict.Add(typeof(Stop), new Action(StopIt));

            dict.Add(typeof(Playing), playingDict);

            return dict;
        }
    }

    internal class Client : Machine
    {
        private Machine Server;
        private int counter;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                (this.Machine as Client).Server = (Machine) this.Payload;
                (this.Machine as Client).counter = 0;
                this.Raise(new Unit());
            }
        }

        private class Playing : State
        {
            protected override void OnEntry()
            {
                if ((this.Machine as Client).counter == 5)
                {
                    this.Send((this.Machine as Client).Server, new Stop());
                    (this.Machine as Client).StopIt();
                }
            }
        }

        private void SendPing()
        {
            this.counter++;
            Console.WriteLine("\nTurns: {0} / 5\n", this.counter);
            this.Send(this.Server, new Ping());
            this.Raise(new Unit());
        }

        private void StopIt()
        {
            Console.WriteLine("Client stopped.\n");
            this.Delete();
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(Unit), typeof(Playing));

            StepStateTransitions playingDict = new StepStateTransitions();
            playingDict.Add(typeof(Unit), typeof(Playing));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(Playing), playingDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings playingDict = new ActionBindings();
            playingDict.Add(typeof(Pong), new Action(SendPing));
            playingDict.Add(typeof(Stop), new Action(StopIt));

            dict.Add(typeof(Playing), playingDict);

            return dict;
        }
    }

    #endregion

    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements a Ping Pong game between
    /// a server and a client.
    /// </summary>
    public class PingPong
    {
        
        public static void Go()
        {
            Runtime.RegisterNewEvent(typeof(Ping));
            Runtime.RegisterNewEvent(typeof(Pong));
            Runtime.RegisterNewEvent(typeof(Stop));
            Runtime.RegisterNewEvent(typeof(Unit));

            Runtime.RegisterNewMachine(typeof(Server));
            Runtime.RegisterNewMachine(typeof(Client));
            Runtime.Start();
            Runtime.Wait();
            Runtime.Dispose();
        }
        static void Main(string[] args)
        {

            Runtime.Test(
                () =>
                {
                    Go();
                },
                10,
                true,
                Runtime.SchedulingType.DFS,
                false);
        }
    }
    public class ChessTest
    {
        public static bool Run()
        {
            PingPong.Go();
            return true;
        }
    }
}
