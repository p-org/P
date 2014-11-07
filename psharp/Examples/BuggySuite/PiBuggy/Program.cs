using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace PiBuggy
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
            Runtime.Test(
                () =>
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

                    Console.WriteLine("Starting the runtime.\n");
                    Runtime.Start(5);
                },
                100,
                true,
                Runtime.SchedulingType.Random,
                false);
        }
    }
}
