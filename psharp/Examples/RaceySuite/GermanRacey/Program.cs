using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace GermanRacey
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements German's cache coherence protocol.
    /// </summary>
    public class Program
    {
        public static void Go()
        {
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Registering events to the runtime.\n");
            Runtime.RegisterNewEvent(typeof(eLocal));
            Runtime.RegisterNewEvent(typeof(eStop));
            Runtime.RegisterNewEvent(typeof(eWait));
            Runtime.RegisterNewEvent(typeof(eNormal));
            Runtime.RegisterNewEvent(typeof(eNeedInvalidate));
            Runtime.RegisterNewEvent(typeof(eInvalidate));
            Runtime.RegisterNewEvent(typeof(eInvalidateAck));
            Runtime.RegisterNewEvent(typeof(eGrant));
            Runtime.RegisterNewEvent(typeof(eAck));
            Runtime.RegisterNewEvent(typeof(eGrantExcl));
            Runtime.RegisterNewEvent(typeof(eGrantShare));
            Runtime.RegisterNewEvent(typeof(eAskShare));
            Runtime.RegisterNewEvent(typeof(eAskExcl));
            Runtime.RegisterNewEvent(typeof(eShareReq));
            Runtime.RegisterNewEvent(typeof(eExclReq));

            Console.WriteLine("Registering state machines to the runtime.\n");
            Runtime.RegisterNewMachine(typeof(Host));
            Runtime.RegisterNewMachine(typeof(Client));
            Runtime.RegisterNewMachine(typeof(CPU));

            Console.WriteLine("Configuring the runtime.\n");
            Runtime.Options.Mode = Runtime.Mode.BugFinding;

            Console.WriteLine("Starting the runtime.\n");
            Runtime.Start(3);
            Runtime.Wait();

            Console.WriteLine("Performing cleanup.\n");
            Runtime.Dispose();
        }
    }
}
