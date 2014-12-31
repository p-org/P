using System;
using System.Collections.Generic;
using Microsoft.PSharp;
using Microsoft.PSharp.Scheduling;

namespace Chameneos
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements the chameneos benchmark.
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
                    "Chameneos",
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
            Runtime.RegisterNewEvent(typeof(eStart));
            Runtime.RegisterNewEvent(typeof(eStop));
            Runtime.RegisterNewEvent(typeof(eHook));
            Runtime.RegisterNewEvent(typeof(eSecondRound));
            Runtime.RegisterNewEvent(typeof(eGetCount));
            Runtime.RegisterNewEvent(typeof(eGetCountAck));
            Runtime.RegisterNewEvent(typeof(eGetString));
            Runtime.RegisterNewEvent(typeof(eGetStringAck));
            Runtime.RegisterNewEvent(typeof(eGetNumber));
            Runtime.RegisterNewEvent(typeof(eGetNumberAck));

            Runtime.RegisterNewMachine(typeof(Broker));
            Runtime.RegisterNewMachine(typeof(Chameneos));

            Runtime.Start(10);
            Runtime.Wait();
            Runtime.Dispose();
        }
    }
}
