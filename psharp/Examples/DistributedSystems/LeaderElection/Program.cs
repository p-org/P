using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace LeaderElection
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
            //Console.WriteLine("Registering events to the runtime.\n");
            //Runtime.RegisterNewEvent(typeof(eStart));
            //Runtime.RegisterNewEvent(typeof(eNotify));
            //Runtime.RegisterNewEvent(typeof(eCheckAck));

            //Console.WriteLine("Registering state machines to the runtime.\n");
            //Runtime.RegisterNewMachine(typeof(Master));
            //Runtime.RegisterNewMachine(typeof(LProcess));

            //Console.WriteLine("Configuring the runtime.\n");
            //Runtime.Options.Mode = Runtime.Mode.BugFinding;
            ////Runtime.Options.MonitorExecutions = true;

            //Console.WriteLine("Starting the runtime.\n");
            //Runtime.Start(3);
            //Runtime.Wait();

            //Console.WriteLine("Performing cleanup.\n");
            //Runtime.Dispose();

            Runtime.Test(
                () => {
                    Runtime.RegisterNewEvent(typeof(eStart));
                    Runtime.RegisterNewEvent(typeof(eNotify));
                    Runtime.RegisterNewEvent(typeof(eCheckAck));
                    Runtime.RegisterNewMachine(typeof(Master));
                    Runtime.RegisterNewMachine(typeof(LProcess));
                    Runtime.Start(3);
                    Runtime.Wait();
                    Runtime.Dispose();
                },
                10000,
                true,
                Runtime.SchedulingType.Random);
        }
    }
}
