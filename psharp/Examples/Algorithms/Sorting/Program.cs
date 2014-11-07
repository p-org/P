using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Sorting
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements a distributed sorting algorithm
    /// taken from the [Automated systematic testing of open
    /// distributed programs] study.
    /// </summary>
    public class Program
    {
        public static void Go()
        {
            Runtime.RegisterNewEvent(typeof(eStart));
            Runtime.RegisterNewEvent(typeof(eUpdate));
            Runtime.RegisterNewEvent(typeof(eNotifyLeft));
            Runtime.RegisterNewEvent(typeof(eNotifyRight));
            Runtime.RegisterNewEvent(typeof(eNotifyMonitor));
            Runtime.RegisterNewMachine(typeof(Master));
            Runtime.RegisterNewMachine(typeof(SProcess));
            Runtime.RegisterNewMonitor(typeof(SortingMonitor));
            Runtime.Start(new List<int> { 3, 2, 5, 1 });
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
