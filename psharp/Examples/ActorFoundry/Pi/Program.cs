using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Pi
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements the compute Pi benchmark.
    /// It attempts to be a faithful port from the SOTER
    /// actor version.
    /// </summary>
    public class Program
    {
        public static void Go()
        {
            Runtime.RegisterNewEvent(typeof(eLocal));
            Runtime.RegisterNewEvent(typeof(eBoot));
            Runtime.RegisterNewEvent(typeof(eStop));
            Runtime.RegisterNewEvent(typeof(eIntervals));
            Runtime.RegisterNewEvent(typeof(eSum));

            Runtime.RegisterNewMachine(typeof(Driver));
            Runtime.RegisterNewMachine(typeof(Master));
            Runtime.RegisterNewMachine(typeof(Worker));

            Runtime.Start(30);
            Runtime.Wait();
            Runtime.Dispose();
        }

        static void Main(string[] args)
        {
            Go();
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
