using System;
using System.Collections.Generic;
using Microsoft.PSharp;
using Microsoft.PSharp.Scheduling;

namespace Elevator
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements an elevator and its environment.
    /// </summary>
    public class Program
    {
        static void Main(string[] args)
        {
            new CommandLineOptions(args).Parse();

            if (Runtime.Options.Mode == Runtime.Mode.Execution)
            {
                Program.Run();
            }
            else if (Runtime.Options.Mode == Runtime.Mode.BugFinding)
            {
                TestConfiguration test = new TestConfiguration(
                    "Elevator",
                    Program.Run,
                    new RandomSchedulingStrategy(0),
                    100);

                //test.UntilBugFound = true;
                test.SoftTimeLimit = 600;
                Runtime.Test(test);
                Console.WriteLine(test.Result());
            }
        }

        public static void Run()
        {
            Runtime.RegisterNewEvent(typeof(eOpenDoor));
            Runtime.RegisterNewEvent(typeof(eCloseDoor));
            Runtime.RegisterNewEvent(typeof(eResetDoor));
            Runtime.RegisterNewEvent(typeof(eDoorOpened));
            Runtime.RegisterNewEvent(typeof(eDoorClosed));
            Runtime.RegisterNewEvent(typeof(eDoorStopped));
            Runtime.RegisterNewEvent(typeof(eObjectDetected));
            Runtime.RegisterNewEvent(typeof(eTimerFired));
            Runtime.RegisterNewEvent(typeof(eOperationSuccess));
            Runtime.RegisterNewEvent(typeof(eOperationFailure));
            Runtime.RegisterNewEvent(typeof(eSendCommandToOpenDoor));
            Runtime.RegisterNewEvent(typeof(eSendCommandToCloseDoor));
            Runtime.RegisterNewEvent(typeof(eSendCommandToStopDoor));
            Runtime.RegisterNewEvent(typeof(eSendCommandToResetDoor));
            Runtime.RegisterNewEvent(typeof(eStartDoorCloseTimer));
            Runtime.RegisterNewEvent(typeof(eStopDoorCloseTimer));
            Runtime.RegisterNewEvent(typeof(eUnit));
            Runtime.RegisterNewEvent(typeof(eStopTimerReturned));
            Runtime.RegisterNewEvent(typeof(eObjectEncountered));

            Runtime.RegisterNewMachine(typeof(Elevator));
            Runtime.RegisterNewMachine(typeof(User));
            Runtime.RegisterNewMachine(typeof(Door));
            Runtime.RegisterNewMachine(typeof(Timer));

            Runtime.Start();
            Runtime.Wait();
            Runtime.Dispose();
        }
    }
}
