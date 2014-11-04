using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace RaftBuggy
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements the Raft distributed concencus protocol.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            int count = 0;
            Runtime.Test(
                () =>
                {
                    Console.Error.WriteLine("iteration: " + count++);
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
                    Runtime.Start(new Tuple<int, int, int>(5, 1, 5));
                    Runtime.Wait();

                    Console.WriteLine("Performing cleanup.\n");
                    Runtime.Dispose();
                },
                10000,
                true,
                Runtime.SchedulingType.Random,
                false);
        }
    }
}
