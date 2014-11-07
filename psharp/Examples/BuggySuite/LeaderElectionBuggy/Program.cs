using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace LeaderElectionBuggy
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements a leader election protocol
    /// taken from the [Automated systematic testing of open
    /// distributed programs] study.
    /// </summary>
    public class Program
    {
        public static void Go()
        {
            Runtime.RegisterNewEvent(typeof(eStart));
            Runtime.RegisterNewEvent(typeof(eNotify));
            Runtime.RegisterNewEvent(typeof(eCheckAck));
            Runtime.RegisterNewEvent(typeof(eStop));
            Runtime.RegisterNewMachine(typeof(Master));
            Runtime.RegisterNewMachine(typeof(LProcess));
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
                10000,
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
