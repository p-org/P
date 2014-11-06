using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace SortingBuggy
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements a distributed sorting algorithm
    /// taken from the [Automated systematic testing of open
    /// distributed programs] study.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            //Runtime.Test(
            //    () =>
            //    {
                    Console.WriteLine("Registering events to the runtime.\n");
                    Runtime.RegisterNewEvent(typeof(eStart));
                    Runtime.RegisterNewEvent(typeof(eUpdate));
                    Runtime.RegisterNewEvent(typeof(eNotifyLeft));
                    Runtime.RegisterNewEvent(typeof(eNotifyRight));
                    Runtime.RegisterNewEvent(typeof(eNotifyMonitor));

                    Console.WriteLine("Registering state machines to the runtime.\n");
                    Runtime.RegisterNewMachine(typeof(Master));
                    Runtime.RegisterNewMachine(typeof(SProcess));
            Runtime.RegisterNewMachine(typeof(SortingMonitor));

                    Console.WriteLine("Starting the runtime.\n");
                    Runtime.Start(new Tuple<List<int>, int>(new List<int> { 3, 2, 5, 1 }, 50));
                    Runtime.Wait();

                    Console.WriteLine("Performing cleanup.\n");
                    Runtime.Dispose();
                //},
                //10000,
                //true,
                //Runtime.SchedulingType.Random,
                //false);
        }
    }
}
