using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Pi
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements a distributed algorithm for computing an
    /// approximation of Pi taken from the [Evaluating Ordering Heuristics
    /// for Dynamic Partial-order Reduction Techniques] study.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Registering events to the runtime.\n");
            Runtime.RegisterNewEvent(typeof(eLocal));
            Runtime.RegisterNewEvent(typeof(eStart));
            Runtime.RegisterNewEvent(typeof(eStop));
            Runtime.RegisterNewEvent(typeof(eWork));
            Runtime.RegisterNewEvent(typeof(eSum));

            Console.WriteLine("Registering state machines to the runtime.\n");
            Runtime.RegisterNewMachine(typeof(Driver));
            Runtime.RegisterNewMachine(typeof(Master));
            Runtime.RegisterNewMachine(typeof(Worker));

            Console.WriteLine("Configuring the runtime.\n");
            Runtime.Options.Mode = Runtime.Mode.BugFinding;
            //Runtime.Options.MonitorExecutions = true;

            Console.WriteLine("Starting the runtime.\n");
            Runtime.Start(10);
            Runtime.Wait();

            Console.WriteLine("Performing cleanup.\n");
            Runtime.Dispose();
        }
    }
}
