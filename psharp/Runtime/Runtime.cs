//-----------------------------------------------------------------------
// <copyright file="Runtime.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.PSharp.Compilation;
using Microsoft.PSharp.IO;
using Microsoft.PSharp.Scheduling;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Static class implementing the P# runtime.
    /// </summary>
    public static class Runtime
    {
        #region fields

        /// <summary>
        /// List of state machines in the program.
        /// </summary>
        private static List<Machine> Machines = new List<Machine>();

        /// <summary>
        /// List of monitors in the program.
        /// </summary>
        private static List<Machine> Monitors = new List<Machine>();

        /// <summary>
        /// Set of registered state machine types.
        /// </summary>
        private static HashSet<Type> RegisteredMachineTypes = new HashSet<Type>();

        /// <summary>
        /// Set of registered monitor types.
        /// </summary>
        private static HashSet<Type> RegisteredMonitorTypes = new HashSet<Type>();

        /// <summary>
        /// Set of registered event types.
        /// </summary>
        private static HashSet<Type> RegisteredEventTypes = new HashSet<Type>();

        /// <summary>
        /// Lock used by the runtime.
        /// </summary>
        private static Object Lock = new Object();

        /// <summary>
        /// True if runtime is running. False otherwise.
        /// </summary>
        internal static bool IsRunning = false;

        /// <summary>
        /// Currently active operation. Used only during
        /// bug finding mode.
        /// </summary>
        internal static Operation Operation = new Operation();

        #endregion

        #region P# API methods

        /// <summary>
        /// Register a new event type. Cannot register a new event
        /// type after the runtime has started running.
        /// </summary>
        /// <param name="e">Event</param>
        public static void RegisterNewEvent(Type e)
        {
            Runtime.Assert(Runtime.IsRunning == false, "Cannot register event {0}" +
                "because the P# runtime has already started.\n", e);
            Runtime.RegisteredEventTypes.Add(e);
        }

        /// <summary>
        /// Register a new machine type. Cannot register a new machine
        /// type after the runtime has started running.
        /// </summary>
        /// <param name="m">Machine</param>
        public static void RegisterNewMachine(Type m)
        {
            Runtime.Assert(Runtime.IsRunning == false, "Cannot register machine {0}" +
                "because the P# runtime has already started.\n", m);
            Runtime.Assert(!m.IsDefined(typeof(Monitor), false), "Cannot register machine {0}" +
                "because it is a monitor.\n", m);

            if (m.IsDefined(typeof(Main), false))
            {
                Runtime.Assert(!Runtime.RegisteredMachineTypes.Any(val =>
                    val.IsDefined(typeof(Main), false)),
                    "Only one machine can be declared as main.\n");
            }

            Runtime.RegisteredMachineTypes.Add(m);
        }

        /// <summary>
        /// Register a new monitor type. Cannot register a new monitor
        /// type after the runtime has started running.
        /// </summary>
        /// <param name="m">Machine</param>
        public static void RegisterNewMonitor(Type m)
        {
            Runtime.Assert(Runtime.IsRunning == false, "Cannot register monitor {0}" +
                "because the P# runtime has already started.\n", m);
            Runtime.Assert(m.IsDefined(typeof(Monitor), false), "Cannot register monitor {0}" +
                "because it is not a monitor.\n", m);
            Runtime.Assert(!m.IsDefined(typeof(Main), false),
                "Monitor {0} cannot be declared as main.\n", m);
            Runtime.RegisteredMonitorTypes.Add(m);
        }

        /// <summary>
        /// Starts the P# runtime by invoking the main machine. The
        /// main machine is constructed with an optional payload.
        /// </summary>
        /// <param name="payload">Payload</param>
        public static void Start(Object payload = null)
        {
            if (Runtime.Options.Mode == Mode.BugFinding)
            {
                Runtime.Assert(Runtime.RegisteredMachineTypes.Any(val =>
                    val.IsDefined(typeof(Main), false)),
                    "No main machine is registered.\n");
                Type m = Runtime.RegisteredMachineTypes.First(val =>
                    val.IsDefined(typeof(Main), false));
                Machine.Factory.CreateMachine(m, payload);
            }

            Runtime.IsRunning = true;
        }

        /// <summary>
        /// Waits until the P# runtime stops.
        /// </summary>
        public static void Wait()
        {
            while (Runtime.IsRunning)
                Thread.Sleep(Properties.Settings.Default.WaitDelay);
        }

        /// <summary>
        /// Stops the P# runtime. Also prints additional runtime
        /// results depending ont he enabled runtime options.
        /// </summary>
        public static void Stop()
        {
            if (Runtime.Options.Mode == Mode.BugFinding &&
                Runtime.Options.PrintExploredPath)
            {
                Runtime.PrintExploredPath();
            }
            else if (Runtime.Options.Mode == Mode.Replay &&
                Runtime.Options.CompareExecutions)
            {
                Replayer.CompareExecutions();
            }

            Runtime.IsRunning = false;
            foreach (var m in Runtime.Machines)
                m.StopListener();
            foreach (var m in Runtime.Monitors)
                m.StopListener();
            Runtime.Machines.Clear();
            Runtime.Monitors.Clear();
        }

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <param name="m">Monitor</param>
        /// <param name="e">Event</param>
        public static void Invoke<T>(Event e)
        {
            Utilities.Verbose("Sending event {0} to monitor {1}\n", e, typeof(T));
            Runtime.Assert(Runtime.Monitors.Any(val => val.GetType() == typeof(T)),
                "A monitor of type {0} does not exists.\n", typeof(T));
            Runtime.Assert(Runtime.RegisteredEventTypes.Any(val => val == e.GetType()),
                "The event {0} was not previously registered.\n", e);

            foreach (var m in Runtime.Monitors)
            {
                if (m.GetType() == typeof(T))
                {
                    e.Operation = new Operation(true);
                    m.Inbox.Add(e);
                    return;
                }
            }
        }

        /// <summary>
        /// Replays the previously explored execution path. The
        /// main machine is constructed with an optional payload.
        /// The input payload must be the same as the one in the
        /// previous execution to achieve deterministic replaying.
        /// </summary>
        /// <param name="payload">Payload</param>
        public static void Replay(Object payload = null)
        {
            Runtime.Assert(PathExplorer.Path.Count > 0,
                "A previously executed path was not detected.\n");
            Runtime.Options.Mode = Mode.Replay;
            Type m = Runtime.RegisteredMachineTypes.First(val =>
                    val.IsDefined(typeof(Main), false));
            Replayer.Run(m, payload);
        }

        /// <summary>
        /// Analyzes the latest P# execution.
        /// </summary>
        public static void AnalyzeExecution()
        {
            //Sequentializer.Run();
        }

        /// <summary>
        /// Compiles the events and state machines to P code.
        /// </summary>
        public static void CompileToPLang()
        {
            new PSharpToPLang(Runtime.RegisteredMachineTypes,
                Runtime.RegisteredMonitorTypes,
                Runtime.RegisteredEventTypes).DoIt();
        }

        #endregion

        #region P# internal methods

        /// <summary>
        /// Attempts to create a new machine instance of type T.
        /// </summary>
        /// <param name="m">Type of the machine</param>
        /// <returns>Machine</returns>
        internal static Machine TryCreateNewMachineInstance(Type m)
        {
            Machine machine;

            lock (Runtime.Lock)
            {
                Utilities.Verbose("Creating new machine: {0}\n", m);
                Runtime.Assert(Runtime.RegisteredMachineTypes.Any(val => val == m),
                    "The machine {0} was not previously registered.\n", m);
                machine = Activator.CreateInstance(m) as Machine;
                Runtime.Machines.Add(machine);
            }
            
            return machine;
        }

        /// <summary>
        /// Attempts to create a new machine instance of type T.
        /// </summary>
        /// <typeparam name="T">Type of the machine</typeparam>
        /// <returns>Machine</returns>
        internal static T TryCreateNewMachineInstance<T>()
        {
            Object machine;

            lock (Runtime.Lock)
            {
                Utilities.Verbose("Creating new machine: {0}\n", typeof(T));
                Runtime.Assert(Runtime.RegisteredMachineTypes.Any(val => val == typeof(T)),
                    "The machine {0} was not previously registered.\n", typeof(T));
                machine = Activator.CreateInstance(typeof(T));
                Runtime.Machines.Add(machine as Machine);
            }

            return (T) machine;
        }

        /// <summary>
        /// Attempts to create a new monitor instance of type T.
        /// There can be only one monitor instance of each
        /// monitor type.
        /// </summary>
        /// <param name="m">Type of the monitor</param>
        /// <returns>Monitor machine</returns>
        internal static Machine TryCreateNewMonitorInstance(Type m)
        {
            Utilities.Verbose("Creating new monitor: {0}\n", m);
            Runtime.Assert(Runtime.RegisteredMonitorTypes.Any(val => val == m),
                "The monitor {0} was not previously registered.\n", m);
            Runtime.Assert(!Runtime.Monitors.Any(val => val.GetType() == m),
                "A monitor of type {0} already exists.\n", m);
            Machine machine = Activator.CreateInstance(m) as Machine;
            Runtime.Monitors.Add(machine);
            return machine;
        }

        /// <summary>
        /// Attempts to create a new monitor instance of type T.
        /// There can be only one monitor instance of each
        /// monitor type.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <returns>Monitor machine</returns>
        internal static T TryCreateNewMonitorInstance<T>()
        {
            Utilities.Verbose("Creating new monitor: {0}\n", typeof(T));
            Runtime.Assert(Runtime.RegisteredMonitorTypes.Any(val => val == typeof(T)),
                "The monitor {0} was not previously registered.\n", typeof(T));
            Runtime.Assert(!Runtime.Monitors.Any(val => val.GetType() == typeof(T)),
                "A monitor of type {0} already exists.\n", typeof(T));
            Object machine = Activator.CreateInstance(typeof(T));
            Runtime.Monitors.Add(machine as Machine);
            return (T)machine;
        }

        /// <summary>
        /// Attempts to send an asynchronous event to a machine. The
        /// P# runtime treats the send as a new operation.
        /// </summary>
        /// <param name="sender">Sender machine</param>
        /// <param name="receiver">Receiver machine</param>
        /// <param name="e">Event</param>
        internal static void SendNew(string sender, Machine receiver, Event e)
        {
            lock (Runtime.Lock)
            {
                Utilities.Verbose("Sending event {0} to machine {1}\n", e, receiver);
                Runtime.Assert(Runtime.RegisteredEventTypes.Any(val => val == e.GetType()),
                    "The event {0} was not previously registered.\n", e);
                Runtime.Assert(!receiver.GetType().IsDefined(typeof(Monitor), false),
                    "Cannot use Runtime.Send() to directly send an event to a monitor. " +
                    "Use Runtime.Invoke() instead.\n");

                if (Runtime.Options.Mode == Mode.BugFinding)
                {
                    Runtime.Operation = new Operation();
                    Runtime.Options.Scheduler.Register(Runtime.Operation);
                    e.Operation = Runtime.Operation;
                    List<Operation> availableOps = Runtime.GetMachineOperations();
                    Runtime.Operation = Runtime.Options.Scheduler.Next(availableOps);
                    PathExplorer.Add(sender, receiver.GetType().Name, e.GetType().Name);
                }
                else if (Runtime.Options.Mode == Mode.Replay)
                {
                    Replayer.Add(sender, receiver.GetType().Name, e.GetType().Name);
                }

                receiver.Inbox.Add(e);
            }
        }

        /// <summary>
        /// Attempts to send an asynchronous event to a machine.
        /// </summary>
        /// <param name="sender">Sender machine</param>
        /// <param name="receiver">Receiver machine</param>
        /// <param name="e">Event</param>
        internal static void Send(string sender, Machine receiver, Event e)
        {
            lock (Runtime.Lock)
            {
                Utilities.Verbose("Sending event {0} to machine {1}\n", e, receiver);
                Runtime.Assert(Runtime.RegisteredEventTypes.Any(val => val == e.GetType()),
                    "The event {0} was not previously registered.\n", e);
                Runtime.Assert(!receiver.GetType().IsDefined(typeof(Monitor), false),
                    "Cannot use Runtime.Send() to directly send an event to a monitor. " +
                    "Use Runtime.Invoke() instead.\n");

                if (Runtime.Options.Mode == Mode.BugFinding)
                {
                    e.Operation = Runtime.Operation;
                    List<Operation> availableOps = Runtime.GetMachineOperations();
                    Runtime.Operation = Runtime.Options.Scheduler.Next(availableOps);
                    PathExplorer.Add(sender, receiver.GetType().Name, e.GetType().Name);
                }
                else if (Runtime.Options.Mode == Mode.Replay)
                {
                    Replayer.Add(sender, receiver.GetType().Name, e.GetType().Name);
                }

                receiver.Inbox.Add(e);
            }
        }

        /// <summary>
        /// Prints the explored execution path.
        /// </summary>
        internal static void PrintExploredPath()
        {
            if (Runtime.Options.Mode == Mode.Execution)
            {
                Utilities.WriteLine("The explored schedule can only " +
                    "be printed in bug finding mode.\n");
            }
            else
            {
                PathExplorer.Print();
            }
        }

        /// <summary>
        /// Returns the machine type of the given string.
        /// </summary>
        /// <param name="m">String</param>
        /// <returns>Type of the machine</returns>
        internal static Type GetMachineType(string m)
        {
            Type result = Runtime.RegisteredMachineTypes.FirstOrDefault(t => t.Name.Equals(m));
            Runtime.Assert(result != null, "No machine of type {0} was found.\n", m);
            return result;
        }

        #endregion

        #region helper methods

        /// <summary>
        /// Gets list of available machine operations in
        /// the system.
        /// </summary>
        /// <returns>List<Operation></returns>
        private static List<Operation> GetMachineOperations()
        {
            List<Operation> ops = new List<Operation>();
            ops.Add(Runtime.Operation);

            foreach (Machine m in Runtime.Machines)
            {
                if (m.Operation == null)
                    continue;
                if (m.Operation.Id == 0)
                    continue;
                if (ops.All(val => val.Id != m.Operation.Id))
                    ops.Add(m.Operation);
            }

            return ops;
        }

        #endregion

        #region cleanup methods

        /// <summary>
        /// Deletes a machine from the P# runtime.
        /// </summary>
        /// <param name="m">Machine</param>
        internal static void Delete(Machine m)
        {
            lock (Runtime.Lock)
            {
                if (Runtime.Monitors.Contains(m))
                {
                    Runtime.Monitors.Remove(m);
                }
                else
                {
                    Runtime.Machines.Remove(m);
                }

                if (Runtime.Machines.Count == 0 &&
                Runtime.Monitors.Count == 0)
                {
                    Runtime.IsRunning = false;
                }
            }
        }

        /// <summary>
        /// Stops the P# runtime and performs cleanup.
        /// </summary>
        public static void Dispose()
        {
            Runtime.IsRunning = false;
            foreach (var m in Runtime.Machines)
                m.StopListener();
            Runtime.Machines.Clear();
            foreach (var m in Runtime.Monitors)
                m.StopListener();
            Runtime.Monitors.Clear();
            Runtime.RegisteredMachineTypes.Clear();
            Runtime.RegisteredMonitorTypes.Clear();
            Runtime.RegisteredEventTypes.Clear();
        }

        #endregion

        #region runtime options

        /// <summary>
        /// Static class implementing options for the P# runtime.
        /// </summary>
        public static class Options
        {
            /// <summary>
            /// The active P# runtime mode. P# is by default
            /// executing without ghost machines.
            /// </summary>
            public static Mode Mode = Mode.Execution;

            /// <summary>
            /// The scheduler to be used. The default is the random scheduler.
            /// </summary>
            public static IScheduler Scheduler = new RandomScheduler();

            /// <summary>
            /// When the runtime stops after running in replay mode
            /// it will compare the original and replayed executions.
            /// This behaviour is enabled by default.
            /// </summary>
            public static bool CompareExecutions = true;

            /// <summary>
            /// When the runtime stops after running in bug finding mode
            /// it will print the explored execution path. This behaviour
            /// is enabled by default.
            /// </summary>
            public static bool PrintExploredPath = true;

            /// <summary>
            /// True to switch verbose mode on. False by default.
            /// </summary>
            public static bool Verbose = false;
        }

        /// <summary>
        /// P# runtime mode type.
        /// </summary>
        public enum Mode
        {
            /// <summary>
            /// P# executes without ghost machines. The Main
            /// attribute is not required in this mode.
            /// </summary>
            Execution = 0,
            /// <summary>
            /// P# uses ghost machines to find bugs. The Main
            /// attribute is required in this mode.
            /// </summary>
            BugFinding = 1,
            /// <summary>
            /// P# executes the previously explored path.
            /// </summary>
            Replay = 2
        }

        #endregion

        #region error checking

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        public static void Assert(bool predicate)
        {
            if (!predicate)
            {
                Utilities.ReportError("Assertion failure.\n");
                Console.ReadLine();
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        /// <param name="s">Message</param>
        /// <param name="args">Message arguments</param>
        internal static void Assert(bool predicate, string s, params object[] args)
        {
            if (!predicate)
            {
                Runtime.IsRunning = false;
                string message = Utilities.Format(s, args);
                Utilities.ReportError(message);
                Console.ReadLine();
                Environment.Exit(1);
            }
        }

        #endregion
    }
}
