using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace GermanBuggy
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements German's cache coherence protocol.
    /// </summary>
    public class Program
    {
        public static void Go()
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
