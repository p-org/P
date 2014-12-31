using System;
using System.Collections.Generic;
using Microsoft.PSharp;
using Microsoft.PSharp.Scheduling;

namespace BoundedAsync
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements an asynchronous scheduler communicating
    /// with a number of processes under a predefined bound.
    /// </summary>
    class Program
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
                    "BoundedAsync",
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
            Runtime.RegisterNewEvent(typeof(eUnit));
            Runtime.RegisterNewEvent(typeof(eReq));
            Runtime.RegisterNewEvent(typeof(eResp));
            Runtime.RegisterNewEvent(typeof(eDone));
            Runtime.RegisterNewEvent(typeof(eInit));
            Runtime.RegisterNewEvent(typeof(eMyCount));

            Runtime.RegisterNewMachine(typeof(Scheduler));
            Runtime.RegisterNewMachine(typeof(Process));

            Runtime.Start();
            Runtime.Wait();
            Runtime.Dispose();
        }
    }
}
