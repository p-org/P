using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace ChandyMisraBuggy
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements the Chandy-Misra shortest path
    /// algorithm taken from the [Automated systematic testing
    /// of open distributed programs] study.
    /// </summary>
    public class Program
    {
        public static void Go()
        {
            Runtime.RegisterNewEvent(typeof(eLocal));
            Runtime.RegisterNewEvent(typeof(eAddNeighbour));
            Runtime.RegisterNewEvent(typeof(eNotify));
            Runtime.RegisterNewMachine(typeof(Master));
            Runtime.RegisterNewMachine(typeof(SPProcess));
            Runtime.Start(4);
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
