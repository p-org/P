using System;
using System.Collections.Generic;
using Microsoft.PSharp;
using Microsoft.PSharp.Scheduling;

namespace TwoPhaseCommitRacy
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements a replication system.
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
                    "TwoPhaseCommitRacy",
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
            Runtime.RegisterNewEvent(typeof(eREQ_REPLICA));
            Runtime.RegisterNewEvent(typeof(eRESP_REPLICA_COMMIT));
            Runtime.RegisterNewEvent(typeof(eRESP_REPLICA_ABORT));
            Runtime.RegisterNewEvent(typeof(eGLOBAL_ABORT));
            Runtime.RegisterNewEvent(typeof(eGLOBAL_COMMIT));
            Runtime.RegisterNewEvent(typeof(eWRITE_REQ));
            Runtime.RegisterNewEvent(typeof(eWRITE_FAIL));
            Runtime.RegisterNewEvent(typeof(eWRITE_SUCCESS));
            Runtime.RegisterNewEvent(typeof(eREAD_REQ));
            Runtime.RegisterNewEvent(typeof(eREAD_FAIL));
            Runtime.RegisterNewEvent(typeof(eREAD_UNAVAILABLE));
            Runtime.RegisterNewEvent(typeof(eREAD_SUCCESS));
            Runtime.RegisterNewEvent(typeof(eUnit));
            Runtime.RegisterNewEvent(typeof(eStop));
            Runtime.RegisterNewEvent(typeof(eTimeout));
            Runtime.RegisterNewEvent(typeof(eStartTimer));
            Runtime.RegisterNewEvent(typeof(eCancelTimer));
            Runtime.RegisterNewEvent(typeof(eCancelTimerFailure));
            Runtime.RegisterNewEvent(typeof(eCancelTimerSuccess));
            Runtime.RegisterNewEvent(typeof(eMONITOR_WRITE));
            Runtime.RegisterNewEvent(typeof(eMONITOR_READ_SUCCESS));
            Runtime.RegisterNewEvent(typeof(eMONITOR_READ_UNAVAILABLE));

            Console.WriteLine("Registering state machines to the runtime.\n");
            Runtime.RegisterNewMachine(typeof(Master));
            Runtime.RegisterNewMachine(typeof(Timer));
            Runtime.RegisterNewMachine(typeof(Replica));
            Runtime.RegisterNewMachine(typeof(Coordinator));
            Runtime.RegisterNewMachine(typeof(Client));
            Runtime.RegisterNewMachine(typeof(Monitor));

            Console.WriteLine("Starting the runtime.\n");
            Runtime.Start();
            Runtime.Wait();

            Console.WriteLine("Performing cleanup.\n");
            Runtime.Dispose();
        }
    }
}
