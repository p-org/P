using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Chameneos
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements the chameneos benchmark.
    /// It attempts to be a faithful port from the SOTER
    /// actor version.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Registering events to the runtime.\n");
            Runtime.RegisterNewEvent(typeof(eLocal));
            Runtime.RegisterNewEvent(typeof(eStart));
            Runtime.RegisterNewEvent(typeof(eStop));
            Runtime.RegisterNewEvent(typeof(eHook));
            Runtime.RegisterNewEvent(typeof(eSecondRound));
            Runtime.RegisterNewEvent(typeof(eGetCount));
            Runtime.RegisterNewEvent(typeof(eGetCountAck));
            Runtime.RegisterNewEvent(typeof(eGetString));
            Runtime.RegisterNewEvent(typeof(eGetStringAck));
            Runtime.RegisterNewEvent(typeof(eGetNumber));
            Runtime.RegisterNewEvent(typeof(eGetNumberAck));

            Console.WriteLine("Registering state machines to the runtime.\n");
            Runtime.RegisterNewMachine(typeof(Broker));
            Runtime.RegisterNewMachine(typeof(Chameneos));

            Console.WriteLine("Starting the runtime.\n");
            Runtime.Start(10);
            Runtime.Wait();

            Console.WriteLine("Performing cleanup.\n");
            Runtime.Dispose();
        }
    }
}
