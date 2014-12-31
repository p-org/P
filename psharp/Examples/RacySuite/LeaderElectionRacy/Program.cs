using System;
using System.Collections.Generic;
using Microsoft.PSharp;
using Microsoft.PSharp.Scheduling;

namespace LeaderElectionRacy
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements a leader election protocol
    /// taken from the [Automated systematic testing of open
    /// distributed programs] study.
    /// </summary>
    public class Program
    {
        static void Main(string[] args)
        {
            new CommandLineOptions(args).Parse();

            if (Runtime.Options.Mode == Runtime.Mode.Execution)
            {
                Program.Run();
            }
            else if (Runtime.Options.Mode == Runtime.Mode.BugFinding)
            {
                TestConfiguration test = new TestConfiguration(
                    "LeaderElectionRacy",
                    Program.Run,
                    new RandomSchedulingStrategy(0),
                    100);

                //test.UntilBugFound = true;
                test.SoftTimeLimit = 600;
                Runtime.Test(test);
                Console.WriteLine(test.Result());
            }
        }

        public static void Run()
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
            Runtime.Wait();

            Console.WriteLine("Performing cleanup.\n");
            Runtime.Dispose();
        }
    }
}
