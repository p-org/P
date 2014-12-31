using System;
using System.Collections.Generic;
using Microsoft.PSharp;
using Microsoft.PSharp.Scheduling;

namespace ChandyMisra
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements the Chandy-Misra shortest path
    /// algorithm taken from the [Automated systematic testing
    /// of open distributed programs] study.
    /// </summary>
    /// 
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
                    "ChandyMisra",
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
            Runtime.RegisterNewEvent(typeof(eAddNeighbour));
            Runtime.RegisterNewEvent(typeof(eNotify));

            Runtime.RegisterNewMachine(typeof(Master));
            Runtime.RegisterNewMachine(typeof(SPProcess));

            Runtime.Start(4);
            Runtime.Wait();
            Runtime.Dispose();
        }
    }
}
