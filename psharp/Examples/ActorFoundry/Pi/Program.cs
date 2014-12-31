using System;
using System.Collections.Generic;
using Microsoft.PSharp;
using Microsoft.PSharp.Scheduling;

namespace Pi
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements the compute Pi benchmark.
    /// It attempts to be a faithful port from the SOTER
    /// actor version.
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
                    "Pi",
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
            Runtime.RegisterNewEvent(typeof(eLocal));
            Runtime.RegisterNewEvent(typeof(eBoot));
            Runtime.RegisterNewEvent(typeof(eStop));
            Runtime.RegisterNewEvent(typeof(eIntervals));
            Runtime.RegisterNewEvent(typeof(eSum));

            Runtime.RegisterNewMachine(typeof(Driver));
            Runtime.RegisterNewMachine(typeof(Master));
            Runtime.RegisterNewMachine(typeof(Worker));

            Runtime.Start(30);
            Runtime.Wait();
            Runtime.Dispose();
        }
    }
}
