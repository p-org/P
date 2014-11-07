using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Pi
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements a distributed algorithm for computing an
    /// approximation of Pi taken from the [Evaluating Ordering Heuristics
    /// for Dynamic Partial-order Reduction Techniques] study.
    /// </summary>
    public class Program
    {
        public static void Go()
        {
            
            Runtime.RegisterNewEvent(typeof(eLocal));
            Runtime.RegisterNewEvent(typeof(eStart));
            Runtime.RegisterNewEvent(typeof(eStop));
            Runtime.RegisterNewEvent(typeof(eWork));
            Runtime.RegisterNewEvent(typeof(eSum));
            Runtime.RegisterNewMachine(typeof(Driver));
            Runtime.RegisterNewMachine(typeof(Master));
            Runtime.RegisterNewMachine(typeof(Worker));
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
