using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace BoundedAsync
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements an asynchronous scheduler communicating
    /// with a number of processes under a predefined bound.
    /// </summary>
    class Program
    {
        public static void Go()
        {
            Runtime.RegisterNewEvent(typeof(eUnit));
            Runtime.RegisterNewEvent(typeof(eReq));
            Runtime.RegisterNewEvent(typeof(eResp));
            Runtime.RegisterNewEvent(typeof(eDone));
            Runtime.RegisterNewEvent(typeof(eInit));
            Runtime.RegisterNewEvent(typeof(eMyCount));

            Runtime.RegisterNewMachine(typeof(Scheduler));
            Runtime.RegisterNewMachine(typeof(Process));

            Runtime.Start();
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
