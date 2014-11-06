using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace LeaderElectionBuggy
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements a leader election protocol
    /// taken from the [Automated systematic testing of open
    /// distributed programs] study.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Runtime.Test(
                () =>
                {
                    Console.WriteLine("Registering events to the runtime.\n");
                    Runtime.RegisterNewEvent(typeof(eStart));
                    Runtime.RegisterNewEvent(typeof(eNotify));
                    Runtime.RegisterNewEvent(typeof(eCheckAck));
                    Runtime.RegisterNewEvent(typeof(eStop));

                    Console.WriteLine("Registering state machines to the runtime.\n");
                    Runtime.RegisterNewMachine(typeof(Master));
                    Runtime.RegisterNewMachine(typeof(LProcess));

                    Console.WriteLine("Starting the runtime.\n");
                    Runtime.Start(3);
                },
                10000,
                true,
                Runtime.SchedulingType.Random,
                false);
        }
    }
}
