using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace SortingRacey
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements a distributed sorting algorithm
    /// taken from the [Automated systematic testing of open
    /// distributed programs] study.
    /// </summary>
    public class Program
    {
        public static void Go()
        {
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Registering events to the runtime.\n");
            Runtime.RegisterNewEvent(typeof(eStart));
            Runtime.RegisterNewEvent(typeof(eUpdate));
            Runtime.RegisterNewEvent(typeof(eNotifyLeft));
            Runtime.RegisterNewEvent(typeof(eNotifyRight));
            Runtime.RegisterNewEvent(typeof(eNotifyMonitor));

            Console.WriteLine("Registering state machines to the runtime.\n");
            Runtime.RegisterNewMachine(typeof(Master));
            Runtime.RegisterNewMachine(typeof(SProcess));

            Console.WriteLine("Registering monitors to the runtime.\n");
            Runtime.RegisterNewMonitor(typeof(SortingMonitor));

            Console.WriteLine("Configuring the runtime.\n");
            Runtime.Options.Mode = Runtime.Mode.BugFinding;

            Console.WriteLine("Starting the runtime.\n");
            Runtime.Start(new List<int> { 3, 2, 5, 1 });
            Runtime.Wait();

            Console.WriteLine("Performing cleanup.\n");
            Runtime.Dispose();
        }
    }
}

