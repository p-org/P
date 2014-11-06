using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace ChandyMisraBuggy
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
            Runtime.Test(
                () =>
                {
                    Console.WriteLine("Registering events to the runtime.\n");
                    Runtime.RegisterNewEvent(typeof(eLocal));
                    Runtime.RegisterNewEvent(typeof(eAddNeighbour));
                    Runtime.RegisterNewEvent(typeof(eNotify));

                    Console.WriteLine("Registering state machines to the runtime.\n");
                    Runtime.RegisterNewMachine(typeof(Master));
                    Runtime.RegisterNewMachine(typeof(SPProcess));

                    Console.WriteLine("Starting the runtime.\n");
                    Runtime.Start(4);
                },
                1,
                true,
                Runtime.SchedulingType.Random,
                false);
        }
    }
}
