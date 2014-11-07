using System;
using System.Collections.Generic;
using Microsoft.PSharp;

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
        public static void Go()
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
