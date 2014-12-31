using System;
using System.Collections.Generic;
using Microsoft.PSharp;
using Microsoft.PSharp.Scheduling;

namespace RaftRacy
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements the Raft distributed concencus protocol.
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
                    "RaftRacy",
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
            Runtime.RegisterNewEvent(typeof(eLocal));
            Runtime.RegisterNewEvent(typeof(eStop));
            Runtime.RegisterNewEvent(typeof(eChangeRoleToFollower));
            Runtime.RegisterNewEvent(typeof(eChangeRoleToCandidate));
            Runtime.RegisterNewEvent(typeof(eChangeRoleToLeader));
            Runtime.RegisterNewEvent(typeof(eClientReq));
            Runtime.RegisterNewEvent(typeof(eClientReqAck));
            Runtime.RegisterNewEvent(typeof(eRequestVote));
            Runtime.RegisterNewEvent(typeof(eRequestVoteAck));
            Runtime.RegisterNewEvent(typeof(eAppendEntries));
            Runtime.RegisterNewEvent(typeof(eAppendEntriesAck));
            Runtime.RegisterNewEvent(typeof(eUpdateLeader));
            Runtime.RegisterNewEvent(typeof(eQueryElectionTimeout));
            Runtime.RegisterNewEvent(typeof(eElectionHasNotTimedOut));
            Runtime.RegisterNewEvent(typeof(eElectionTimedOut));
            Runtime.RegisterNewEvent(typeof(eQueryHeartBeating));
            Runtime.RegisterNewEvent(typeof(eSendHeartBeat));
            Runtime.RegisterNewEvent(typeof(eDoNotSendHeartBeat));
            Runtime.RegisterNewEvent(typeof(eQueryClientTimeout));
            Runtime.RegisterNewEvent(typeof(eClientHasNotTimedOut));
            Runtime.RegisterNewEvent(typeof(eClientTimedOut));

            Console.WriteLine("Registering state machines to the runtime.\n");
            Runtime.RegisterNewMachine(typeof(Cluster));
            Runtime.RegisterNewMachine(typeof(Server));
            Runtime.RegisterNewMachine(typeof(Clock));
            Runtime.RegisterNewMachine(typeof(Client));

            Console.WriteLine("Starting the runtime.\n");
            Runtime.Start(new Tuple<int, int, int>(5, 1, 100));
            Runtime.Wait();

            Console.WriteLine("Performing cleanup.\n");
            Runtime.Dispose();
        }
    }
}
