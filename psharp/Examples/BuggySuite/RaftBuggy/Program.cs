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
    public class Program
    {
        public static void Go()
        {
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
            Runtime.RegisterNewMachine(typeof(Cluster));
            Runtime.RegisterNewMachine(typeof(Server));
            Runtime.RegisterNewMachine(typeof(Clock));
            Runtime.RegisterNewMachine(typeof(Client));

            Runtime.Start(new Tuple<int, int, int>(5, 1, 5));
            Runtime.Wait();
            Runtime.Dispose();
        }
        static void Main(string[] args)
        {
            Runtime.Test(
                () =>
                {
                    Go();
                },
                100,
                true,
                Runtime.SchedulingType.Random,
                false);
        }
    }
    public class ChessTest
    {
        public static bool Run()
        {
            Program.Go();
            return true;
        }
    }
}
