using System;
using System.Collections.Generic;
using Microsoft.PSharp;
using Microsoft.PSharp.Scheduling;

namespace Sorting
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
                    "Sorting",
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
            Runtime.RegisterNewEvent(typeof(eStart));
            Runtime.RegisterNewEvent(typeof(eUpdate));
            Runtime.RegisterNewEvent(typeof(eNotifyLeft));
            Runtime.RegisterNewEvent(typeof(eNotifyRight));
            Runtime.RegisterNewEvent(typeof(eNotifyMonitor));

            Runtime.RegisterNewMachine(typeof(Master));
            Runtime.RegisterNewMachine(typeof(SProcess));
            Runtime.RegisterNewMachine(typeof(SortingMonitor));

            Runtime.Start(new List<int> { 3, 2, 5, 1 });
            Runtime.Wait();
            Runtime.Dispose();
        }
    }
}
