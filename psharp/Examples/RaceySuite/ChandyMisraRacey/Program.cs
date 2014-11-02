using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace ChandyMisraRacey
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements the Chandy-Misra shortest path
    /// algorithm taken from the [Automated systematic testing
    /// of open distributed programs] study.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Registering events to the runtime.\n");
            Runtime.RegisterNewEvent(typeof(eLocal));
            Runtime.RegisterNewEvent(typeof(eAddNeighbour));
            Runtime.RegisterNewEvent(typeof(eNotify));

            Console.WriteLine("Registering state machines to the runtime.\n");
            Runtime.RegisterNewMachine(typeof(Master));
            Runtime.RegisterNewMachine(typeof(SPProcess));

            Console.WriteLine("Configuring the runtime.\n");
            Runtime.Options.Mode = Runtime.Mode.BugFinding;

            Console.WriteLine("Starting the runtime.\n");
            Runtime.Start(4);
            Runtime.Wait();

            Console.WriteLine("Performing cleanup.\n");
            Runtime.Dispose();
        }
    }
}
