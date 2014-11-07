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
        static void Main(string[] args)
        {
            Console.WriteLine("Registering events to the runtime.\n");
            Runtime.RegisterNewEvent(typeof(eUnit));
            Runtime.RegisterNewEvent(typeof(eReq));
            Runtime.RegisterNewEvent(typeof(eResp));
            Runtime.RegisterNewEvent(typeof(eDone));
            Runtime.RegisterNewEvent(typeof(eInit));
            Runtime.RegisterNewEvent(typeof(eMyCount));

            Console.WriteLine("Registering state machines to the runtime.\n");
            Runtime.RegisterNewMachine(typeof(Scheduler));
            Runtime.RegisterNewMachine(typeof(Process));

            Console.WriteLine("Starting the runtime.\n");
            Runtime.Start();
            Runtime.Wait();

            Console.WriteLine("Performing cleanup.\n");
            Runtime.Dispose();
        }
    }
}
