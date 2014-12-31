using System;
using System.Collections.Generic;
using Microsoft.PSharp;
using Microsoft.PSharp.Scheduling;

namespace GermanBuggy
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements German's cache coherence protocol.
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
                    "GermanBuggy",
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
            Runtime.RegisterNewEvent(typeof(eStop));
            Runtime.RegisterNewEvent(typeof(eWait));
            Runtime.RegisterNewEvent(typeof(eNormal));
            Runtime.RegisterNewEvent(typeof(eNeedInvalidate));
            Runtime.RegisterNewEvent(typeof(eInvalidate));
            Runtime.RegisterNewEvent(typeof(eInvalidateAck));
            Runtime.RegisterNewEvent(typeof(eGrant));
            Runtime.RegisterNewEvent(typeof(eAck));
            Runtime.RegisterNewEvent(typeof(eGrantExcl));
            Runtime.RegisterNewEvent(typeof(eGrantShare));
            Runtime.RegisterNewEvent(typeof(eAskShare));
            Runtime.RegisterNewEvent(typeof(eAskExcl));
            Runtime.RegisterNewEvent(typeof(eShareReq));
            Runtime.RegisterNewEvent(typeof(eExclReq));

            Runtime.RegisterNewMachine(typeof(Host));
            Runtime.RegisterNewMachine(typeof(Client));
            Runtime.RegisterNewMachine(typeof(CPU));

            Runtime.Start(3);
            Runtime.Wait();
            Runtime.Dispose();
        }
    }
}
