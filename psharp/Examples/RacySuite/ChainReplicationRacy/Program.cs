using System;
using System.Collections.Generic;
using Microsoft.PSharp;
using Microsoft.PSharp.Scheduling;

namespace ChainReplicationRacy
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements the Chain Replication protocol
    /// from OSDI'04.
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
                    "ChainReplicationRacy",
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
            Runtime.RegisterNewEvent(typeof(ePredSucc));
            Runtime.RegisterNewEvent(typeof(eUpdate));
            Runtime.RegisterNewEvent(typeof(eQuery));
            Runtime.RegisterNewEvent(typeof(eResponseToQuery));
            Runtime.RegisterNewEvent(typeof(eFaultDetected));
            Runtime.RegisterNewEvent(typeof(eFaultCorrected));
            Runtime.RegisterNewEvent(typeof(eBecomeHead));
            Runtime.RegisterNewEvent(typeof(eBecomeTail));
            Runtime.RegisterNewEvent(typeof(eNewPredecessor));
            Runtime.RegisterNewEvent(typeof(eNewSuccessor));
            Runtime.RegisterNewEvent(typeof(eUpdateHeadTail));
            Runtime.RegisterNewEvent(typeof(eNewSuccInfo));
            Runtime.RegisterNewEvent(typeof(eBackwardAck));
            Runtime.RegisterNewEvent(typeof(eForwardUpdate));
            Runtime.RegisterNewEvent(typeof(eCRPing));
            Runtime.RegisterNewEvent(typeof(eMonitorHistoryUpdate));
            Runtime.RegisterNewEvent(typeof(eMonitorSentUpdate));
            Runtime.RegisterNewEvent(typeof(eMonitorUpdateServers));
            Runtime.RegisterNewEvent(typeof(eMonitorResponseToUpdate));
            Runtime.RegisterNewEvent(typeof(eMonitorResponseToQuery));
            Runtime.RegisterNewEvent(typeof(eMonitorUpdateLiveness));
            Runtime.RegisterNewEvent(typeof(eMonitorResponseLiveness));
            Runtime.RegisterNewEvent(typeof(eMonitorQueryLiveness));
            Runtime.RegisterNewEvent(typeof(eLocal));
            Runtime.RegisterNewEvent(typeof(eDone));
            Runtime.RegisterNewEvent(typeof(eSuccess));
            Runtime.RegisterNewEvent(typeof(eHeadChanged));
            Runtime.RegisterNewEvent(typeof(eTailChanged));
            Runtime.RegisterNewEvent(typeof(eResponseToUpdate));
            Runtime.RegisterNewEvent(typeof(eHeadFailed));
            Runtime.RegisterNewEvent(typeof(eTailFailed));
            Runtime.RegisterNewEvent(typeof(eServerFailed));
            Runtime.RegisterNewEvent(typeof(eFixSuccessor));
            Runtime.RegisterNewEvent(typeof(eFixPredecessor));
            Runtime.RegisterNewEvent(typeof(eStartTimer));
            Runtime.RegisterNewEvent(typeof(eCancelTimer));
            Runtime.RegisterNewEvent(typeof(eCancelTimerSuccess));
            Runtime.RegisterNewEvent(typeof(eTimeout));
            Runtime.RegisterNewEvent(typeof(eCRPong));
            Runtime.RegisterNewEvent(typeof(eMonitorSuccess));
            Runtime.RegisterNewEvent(typeof(eStop));

            Console.WriteLine("Registering state machines to the runtime.\n");
            Runtime.RegisterNewMachine(typeof(GodMachine));
            Runtime.RegisterNewMachine(typeof(Client));
            Runtime.RegisterNewMachine(typeof(ChainReplicationMaster));
            Runtime.RegisterNewMachine(typeof(ChainReplicationServer));
            Runtime.RegisterNewMachine(typeof(ChainReplicationFaultDetection));
            Runtime.RegisterNewMachine(typeof(Timer));

            Console.WriteLine("Registering monitors to the runtime.\n");
            Runtime.RegisterNewMonitor(typeof(UpdatePropagationInvariantMonitor));
            Runtime.RegisterNewMonitor(typeof(UpdateResponseQueryResponseSeqMonitor));
            Runtime.RegisterNewMonitor(typeof(LivenessUpdatetoResponseMonitor));
            Runtime.RegisterNewMonitor(typeof(LivenessQuerytoResponseMonitor));

            Console.WriteLine("Starting the runtime.\n");
            Runtime.Start();
            Runtime.Wait();

            Console.WriteLine("Performing cleanup.\n");
            Runtime.Dispose();
        }
    }
}
