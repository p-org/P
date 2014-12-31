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
using System.Diagnostics;
using System.Linq;
using System.IO.Pipes;
using System.Threading;

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
        /// List of machine tasks.
        /// </summary>
        private static List<Thread> MachineTasks = new List<Thread>();

        /// <summary>
        /// The bug-finding scheduler.
        /// </summary>
        internal static Scheduler Scheduler = new Scheduler();

        /// <summary>
        /// True if runtime is running. False otherwise.
        /// </summary>
        internal static bool IsRunning = false;

        /// <summary>
        /// Assertion failure counter.
        /// </summary>
        private static int AssertionFailureCount = 0;

        #endregion

        #region P# API methods

        /// <summary>
        /// Register a new event type. Cannot register a new event
        /// type after the runtime has started running.
        /// </summary>
        /// <param name="e">Event</param>
        public static void RegisterNewEvent(Type e)
        {
            Runtime.Assert(Runtime.IsRunning == false, "Cannot register event '{0}'" +
                "because the P# runtime has already started.\n", e.Name);
            Runtime.RegisteredEventTypes.Add(e);
        }

        /// <summary>
        /// Register a new machine type. Cannot register a new machine
        /// type after the runtime has started running.
        /// </summary>
        /// <param name="m">Machine</param>
        public static void RegisterNewMachine(Type m)
        {
            Runtime.Assert(Runtime.IsRunning == false, "Cannot register machine '{0}'" +
                "because the P# runtime has already started.\n", m.Name);
            Runtime.Assert(!m.IsDefined(typeof(Monitor), false), "Cannot register machine '{0}'" +
                "because it is a monitor.\n", m.Name);

            if (m.IsDefined(typeof(Main), false))
            {
                Runtime.Assert(!Runtime.RegisteredMachineTypes.Any(val =>
                    val.IsDefined(typeof(Main), false)),
                    "Machine '{0}' cannot be declared as main. A main machine already " +
                    "exists.\n", m.Name);
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
            Runtime.Assert(Runtime.IsRunning == false, "Cannot register monitor '{0}'" +
                "because the P# runtime has already started.\n", m.Name);
            Runtime.Assert(m.IsDefined(typeof(Monitor), false), "Cannot register " +
                "monitor '{0}' because it is not a monitor.\n", m.Name);
            Runtime.Assert(!m.IsDefined(typeof(Main), false),
                "Monitor '{0}' cannot be declared as main.\n", m.Name);
            Runtime.RegisteredMonitorTypes.Add(m);
        }

        /// <summary>
        /// Starts the P# runtime by invoking the main machine. The
        /// main machine is constructed with an optional payload.
        /// </summary>
        /// <param name="payload">Optional payload</param>
        public static void Start(Object payload = null)
        {
            Runtime.Assert(Runtime.RegisteredMachineTypes.Any(val =>
                    val.IsDefined(typeof(Main), false)),
                    "No main machine is registered.\n");

            Runtime.IsRunning = true;
            if (Runtime.Options.Mode == Runtime.Mode.Execution)
            {
                // Start the main machine.
                Type mainMachine = Runtime.RegisteredMachineTypes.First(val =>
                    val.IsDefined(typeof(Main), false));
                Machine.Factory.CreateMachine(mainMachine, payload);
            }
            else if (Runtime.Options.Mode == Runtime.Mode.BugFinding)
            {
                // Start the main machine.
                Type mainMachine = Runtime.RegisteredMachineTypes.First(val =>
                    val.IsDefined(typeof(Main), false));
                Machine.Factory.CreateMachine(mainMachine, payload);
            }
        }

        /// <summary>
        /// Waits until the P# runtime stops.
        /// </summary>
        public static void Wait()
        {
            Thread[] taskArray = null;
            lock (Runtime.Lock)
            {
                taskArray = Runtime.MachineTasks.ToArray();
            }

            foreach (Thread thread in taskArray)
            {
                thread.Join();
            }

            bool moreTasksExist = false;
            lock (Runtime.Lock)
            {
                moreTasksExist = taskArray.Length != Runtime.MachineTasks.Count;
            }

            if (moreTasksExist)
            {
                Runtime.Wait();
            }
        }

        /// <summary>
        /// Stops the P# runtime. Also prints additional runtime
        /// results depending on the enabled runtime options.
        /// </summary>
        public static void Stop()
        {
            if (Runtime.Options.Mode == Runtime.Mode.BugFinding &&
                Runtime.Options.PrintExploredSchedule)
            {
                Runtime.PrintExploredSchedule();
            }
            else if (Runtime.Options.Mode == Runtime.Mode.Replay &&
                Runtime.Options.CompareExecutions)
            {
                Replayer.CompareExecutions();
            }

            Runtime.IsRunning = false;
            foreach (var m in Runtime.Machines)
                m.StopListener();
            foreach (var m in Runtime.Monitors)
                m.StopListener();

            //foreach(ThreadInfo threadInfo in threadMap.Values)
            //{
            //    lock(threadInfo)
            //    {
            //        System.Threading.Monitor.PulseAll(threadInfo);
            //    }
            //}

            Runtime.Machines.Clear();
            Runtime.Monitors.Clear();
            Runtime.MachineTasks.Clear();
        }

        /// <summary>
        /// Tests the P# program using the given runtime action. The program is
        /// tested a used-defined number of times. It enables bug-finding mode
        /// by default, and measures assertion failures and the testing runtime.
        /// </summary>
        /// <param name="testConfig">Test configuration</param>
        public static void Test(TestConfiguration testConfig)
        {
            Runtime.Scheduler.SchedulingStrategy = testConfig.SchedulingStrategy;
            Runtime.Options.Mode = Runtime.Mode.BugFinding;
            Runtime.Options.CountAssertions = true;

            Console.WriteLine("Starting: " + testConfig.Name);

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            while (testConfig.NumSchedules < testConfig.ScheduleLimit)
            {
                //Console.WriteLine("Starting iteration: {0}", iteration + 1);

                bool cont = Runtime.Scheduler.Reset();

                if (testConfig.SoftTimeLimit > 1 &&
                    stopWatch.Elapsed.TotalSeconds > testConfig.SoftTimeLimit)
                {
                    break;
                }

                if (!cont)
                {
                    testConfig.Completed = true;
                    break;
                }

                testConfig.EntryPoint();

                if (testConfig.NumSchedules == 0)
                {
                    testConfig.NumSteps = Runtime.Scheduler.SchedulingStrategy.GetNumSchedPoints();
                }

                testConfig.NumSchedules++;

                // If it is a "real" deadlock (not due to e.g. an assertion failure)
                // then we record this.
                if (Runtime.Scheduler.DeadlockHasOccurred &&
                    !Runtime.Scheduler.ErrorHasOccurred)
                {
                    testConfig.NumDeadlocks++;
                    Console.WriteLine("Terminated due to DEADLOCK!");
                }

                if (Runtime.Scheduler.ErrorHasOccurred)
                {
                    Console.WriteLine("Terminated due to ERROR!");
                }

                // If it is an error or a deadlock then deadlockHasOccurred will be set.
                if (Runtime.Scheduler.DeadlockHasOccurred)
                {
                    testConfig.NumBuggy++;
                    if (testConfig.NumSchedulesToFirstBug == -1)
                    {
                        testConfig.NumSchedulesToFirstBug = testConfig.NumSchedules;
                        testConfig.TimeToFirstBug = stopWatch.Elapsed.TotalSeconds;
                    }
                }

                if (Runtime.Scheduler.HitDepthBound)
                {
                    testConfig.NumHitDepthBound++;
                }

                //Console.WriteLine("Finished iteration: {0}", iteration + 1);
                if (testConfig.NumSchedules % 500 == 0)
                {
                    Console.Error.WriteLine("Finished schedule {0}", testConfig.NumSchedules);
                }

                if (testConfig.UntilBugFound && testConfig.NumBuggy > 0)
                {
                    break;
                }
            }

            stopWatch.Stop();
            testConfig.Time = stopWatch.Elapsed.TotalSeconds;

            Console.Error.WriteLine("Found {0} buggy schedules.", testConfig.NumBuggy);
            Console.Error.WriteLine("  ({0} of them were deadlocks.)", testConfig.NumDeadlocks);
            Console.Error.WriteLine("Explored {0} schedules.", testConfig.NumSchedules);
            Console.Error.WriteLine("There were {0} steps on the first schedule.", testConfig.NumSteps);
            Console.Error.WriteLine("Elapsed: {0} seconds.", testConfig.Time);
        }

        /// <summary>
        /// Replays the previously explored execution schedule. The
        /// main machine is constructed with an optional payload.
        /// The input payload must be the same as the one in the
        /// previous execution to achieve deterministic replaying.
        /// </summary>
        /// <param name="payload">Payload</param>
        public static void Replay(Object payload = null)
        {
            Runtime.Assert(ScheduleExplorer.Schedule.Count > 0,
                "A previously executed schedule was not detected.\n");
            Runtime.Options.Mode = Runtime.Mode.Replay;
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

        #region P# runtime internal methods

        /// <summary>
        /// Attempts to create a new machine instance of type T with
        /// the given payload.
        /// </summary>
        /// <param name="m">Type of the machine</param>
        /// <param name="payload">Payload</param>
        /// <returns>Machine</returns>
        internal static Machine TryCreateNewMachineInstance(Type m, Object payload)
        {
            Machine machine;

            lock (Runtime.Lock)
            {
                Utilities.Verbose("Creating new machine: {0}\n", m);
                Runtime.Assert(Runtime.RegisteredMachineTypes.Any(val => val == m),
                    "Machine '{0}' has not been registered with the P# runtime.\n", m.Name);
                machine = Activator.CreateInstance(m) as Machine;
                Runtime.Machines.Add(machine);
            }

            if (Runtime.Options.Mode == Runtime.Mode.Execution)
            {
                lock (Runtime.Lock)
                {
                    Runtime.MachineTasks.Add(machine.Start(payload));
                }
            }
            else if (Runtime.Options.Mode == Runtime.Mode.BugFinding)
            {
                lock (Runtime.Lock)
                {
                    Runtime.MachineTasks.Add(machine.ScheduledStart(payload));
                }
            }

            return machine;
        }

        /// <summary>
        /// Attempts to create a new machine instance of type T with
        /// the given payload.
        /// </summary>
        /// <typeparam name="T">Type of the machine</typeparam>
        /// <param name="payload">Payload</param>
        /// <returns>Machine</returns>
        internal static T TryCreateNewMachineInstance<T>(Object payload)
        {
            Object machine;

            lock (Runtime.Lock)
            {
                Utilities.Verbose("Creating new machine: {0}\n", typeof(T));
                Runtime.Assert(Runtime.RegisteredMachineTypes.Any(val => val == typeof(T)),
                    "Machine '{0}' has not been registered with the P# runtime.\n", typeof(T).Name);
                machine = Activator.CreateInstance(typeof(T));
                Runtime.Machines.Add(machine as Machine);
            }

            if (Runtime.Options.Mode == Runtime.Mode.Execution)
            {
                lock (Runtime.Lock)
                {
                    Runtime.MachineTasks.Add((machine as Machine).Start(payload));
                }
            }
            else if (Runtime.Options.Mode == Runtime.Mode.BugFinding)
            {
                lock (Runtime.Lock)
                {
                    Runtime.MachineTasks.Add((machine as Machine).ScheduledStart(payload));
                }
            }

            return (T)machine;
        }

        /// <summary>
        /// Attempts to create a new monitor instance of type T with
        /// the given payload. There can be only one monitor instance
        /// of each monitor type.
        /// </summary>
        /// <param name="m">Type of the monitor</param>
        /// <param name="payload">Payload</param>
        /// <returns>Monitor machine</returns>
        internal static Machine TryCreateNewMonitorInstance(Type m, Object payload)
        {
            Utilities.Verbose("Creating new monitor: {0}\n", m);
            Runtime.Assert(Runtime.RegisteredMonitorTypes.Any(val => val == m),
                "Monitor '{0}' has not been registered with the P# runtime.\n", m.Name);
            Runtime.Assert(!Runtime.Monitors.Any(val => val.GetType() == m),
                "A monitor of type '{0}' already exists.\n", m.Name);

            Machine machine = Activator.CreateInstance(m) as Machine;

            Runtime.Monitors.Add(machine);


            if (Runtime.Options.Mode == Runtime.Mode.Execution)
            {
                lock (Runtime.Lock)
                {
                    Runtime.MachineTasks.Add(machine.Start(payload));
                }
            }
            else if (Runtime.Options.Mode == Runtime.Mode.BugFinding)
            {
                lock (Runtime.Lock)
                {
                    Runtime.MachineTasks.Add(machine.ScheduledStart(payload));
                }
            }

            return machine;
        }

        /// <summary>
        /// Attempts to create a new monitor instance of type T with
        /// the given payload. There can be only one monitor instance
        /// of each monitor type.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="payload">Payload</param>
        /// <returns>Monitor machine</returns>
        internal static T TryCreateNewMonitorInstance<T>(Object payload)
        {
            Utilities.Verbose("Creating new monitor: {0}\n", typeof(T));
            Runtime.Assert(Runtime.RegisteredMonitorTypes.Any(val => val == typeof(T)),
                "Monitor '{0}' has not been registered with the P# runtime.\n", typeof(T).Name);
            Runtime.Assert(!Runtime.Monitors.Any(val => val.GetType() == typeof(T)),
                "A monitor of type '{0}' already exists.\n", typeof(T).Name);

            Object machine = Activator.CreateInstance(typeof(T));
            Runtime.Monitors.Add(machine as Machine);

            if (Runtime.Options.Mode == Runtime.Mode.Execution)
            {
                lock (Runtime.Lock)
                {
                    Runtime.MachineTasks.Add((machine as Machine).Start(payload));
                }
            }
            else if (Runtime.Options.Mode == Runtime.Mode.BugFinding)
            {
                lock (Runtime.Lock)
                {
                    Runtime.MachineTasks.Add((machine as Machine).ScheduledStart(payload));
                }
            }

            return (T)machine;
        }

        /// <summary>
        /// Attempts to send an asynchronous event to a machine.
        /// </summary>
        /// <param name="sender">Sender machine</param>
        /// <param name="receiver">Receiver machine</param>
        /// <param name="e">Event</param>
        internal static void Send(string sender, Machine receiver, Event e)
        {
            Utilities.Verbose("Sending event {0} to machine {1}\n", e, receiver);
            Runtime.Assert(Runtime.RegisteredEventTypes.Any(val => val == e.GetType()),
                "Event '{0}' has not been registered with the P# runtime.\n", e);
            Runtime.Assert(!receiver.GetType().IsDefined(typeof(Monitor), false),
                "Cannot use Runtime.Send() to directly send an event to a monitor. " +
                "Use Runtime.Invoke() instead.\n");

            if (Runtime.Options.Mode == Runtime.Mode.Execution)
            {
                receiver.Inbox.Add(e);
            }
            else if (Runtime.Options.Mode == Runtime.Mode.BugFinding)
            {
                receiver.ScheduledInbox.Add(e);
                ScheduleExplorer.Add(sender, receiver.GetType().Name, e.GetType().Name);
            }
            else if (Runtime.Options.Mode == Runtime.Mode.Replay)
            {
                receiver.ScheduledInbox.Add(e);
                Replayer.Add(sender, receiver.GetType().Name, e.GetType().Name);
            }
        }

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        internal static void Invoke<T>(Event e)
        {
            Utilities.Verbose("Sending event {0} to monitor {1}\n", e, typeof(T));
            Runtime.Assert(Runtime.Monitors.Any(val => val.GetType() == typeof(T)),
                "A monitor of type '{0}' does not exists.\n", typeof(T).Name);
            Runtime.Assert(Runtime.RegisteredEventTypes.Any(val => val == e.GetType()),
                "Event '{0}' has not been registered with the P# runtime.\n", e.GetType().Name);

            foreach (var m in Runtime.Monitors)
            {
                if (m.GetType() == typeof(T))
                {
                    if (Runtime.Options.Mode == Runtime.Mode.Execution)
                    {
                        m.Inbox.Add(e);
                    }
                    else if (Runtime.Options.Mode == Runtime.Mode.BugFinding)
                    {
                        m.ScheduledInbox.Add(e);
                    }

                    return;
                }
            }
        }

        /// <summary>
        /// Prints the explored execution schedule.
        /// </summary>
        internal static void PrintExploredSchedule()
        {
            if (Runtime.Options.Mode == Runtime.Mode.Execution)
            {
                Utilities.WriteLine("The explored schedule can only " +
                    "be printed in bug finding mode.\n");
            }
            else
            {
                ScheduleExplorer.Print();
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
            Runtime.Assert(result != null, "No machine of type '{0}' was found.\n", m);
            return result;
        }

        #endregion

        #region cleanup methods

        /// <summary>
        /// Deletes a machine from the P# runtime.
        /// </summary>
        /// <param name="m">Machine</param>
        internal static void Delete(Machine m)
        {
            //System.Diagnostics.Debug.Assert(false);
            return;
            //lock (Runtime.Lock)
            //{
            //    if (Runtime.Monitors.Contains(m))
            //    {
            //        Runtime.Monitors.Remove(m);
            //    }
            //    else
            //    {
            //        Runtime.Machines.Remove(m);
            //    }

            //    if (Runtime.Machines.Count == 0 &&
            //        Runtime.Monitors.Count == 0)
            //    {
            //        Runtime.IsRunning = false;
            //    }
            //}
        }

        /// <summary>
        /// Stops the P# runtime and performs cleanup.
        /// </summary>
        public static void Dispose()
        {
            Runtime.IsRunning = false;

            //foreach (var m in Runtime.Machines)
            //    m.StopListener();
            //Runtime.Machines.Clear();
            //foreach (var m in Runtime.Monitors)
            //    m.StopListener();

            Runtime.Machines.Clear();
            Runtime.Monitors.Clear();

            Runtime.RegisteredMachineTypes.Clear();
            Runtime.RegisteredMonitorTypes.Clear();
            Runtime.RegisteredEventTypes.Clear();

            Runtime.MachineTasks.Clear();

            Machine.ResetMachineIDCounter();
            Runtime.Options.SchedulingStrategy.Reset();
            ScheduleExplorer.ResetExploredSchedule();

            if (Runtime.Options.MonitorExecutions)
            {
                Runtime.SendResultsToPSharpMonitor(false);
            }
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
            public static Mode Mode = Runtime.Mode.Execution;

            /// <summary>
            /// The scheduling strategy to be used. The default is the random
            /// scheduling strategy.
            /// </summary>
            public static ISchedulingStrategy SchedulingStrategy =
                new RandomSchedulingStrategy(0);

            /// <summary>
            /// When the runtime stops after running in replay mode
            /// it will compare the original and replayed executions.
            /// This behaviour is enabled by default.
            /// </summary>
            public static bool CompareExecutions = true;

            /// <summary>
            /// Connect to the P# monitor for systematic testing. Works only
            /// in bug finding mode. This behaviour is disabled by default.
            /// </summary>
            public static bool MonitorExecutions = false;

            /// <summary>
            /// When the runtime stops after running in bug finding mode
            /// it will print the explored execution schedule. This
            /// behaviour is enabled by default.
            /// </summary>
            public static bool PrintExploredSchedule = true;

            /// <summary>
            /// True to switch verbose mode on. False by default.
            /// </summary>
            public static bool Verbose = false;

            /// <summary>
            /// Counts the assertion failures. Only enabled internally
            /// when runtime is in testing mode. When enabled, assertions
            /// do not cause the environment to exit.
            /// </summary>
            public static bool CountAssertions = false;

            /// <summary>
            /// Static class implementing monitoring options for the P# runtime.
            /// </summary>
            internal static class Monitoring
            {
                /// <summary>
                /// True to pause the runtime when a bug is found.
                /// False by default.
                /// </summary>
                internal static bool PauseAtBug = false;

                /// <summary>
                /// True to pause the runtime at the end of execution.
                /// False by default.
                /// </summary>
                internal static bool PauseAtExecutionEnd = false;
            }
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
            /// P# executes the previously explored schedule.
            /// </summary>
            Replay = 2
        }

        /// <summary>
        /// P# runtime scheduling type.
        /// </summary>
        public enum SchedulingType
        {
            /// <summary>
            /// Enables the random scheduler.
            /// </summary>
            Random = 0,
            /// <summary>
            /// Enables the round robin scheduler.
            /// </summary>
            RoundRobin = 1,
            /// <summary>
            /// Enables the depth first search scheduler.
            /// </summary>
            DFS = 2
        }

        #endregion

        #region monitor communication methods

        /// <summary>
        /// Communicates with the P# monitor to receive the configuration
        /// for this execution.
        /// </summary>
        private static void ReceiveConfigurationFromPSharpMonitor()
        {
            string request = "";

            using (var pipe = new NamedPipeClientStream(".", "PSharpMonitorConfigurationPipe",
                PipeDirection.In))
            {
                Utilities.Verbose("Connecting to the P# monitor ...\n");
                pipe.Connect();

                byte[] buffer = new byte[256];
                pipe.Read(buffer, 0, buffer.Length);

                int bufferIdx = buffer.Length - 1;
                while (buffer[bufferIdx] == 0) --bufferIdx;
                byte[] finalBuffer = new byte[bufferIdx + 1];
                Array.Copy(buffer, finalBuffer, bufferIdx + 1);

                request = System.Text.Encoding.UTF8.GetString(finalBuffer, 0, finalBuffer.Length);

                Utilities.Verbose("Successfully received configuration from the P# monitor.\n");
            }

            string[] configurations = request.Split(' ');
            Console.WriteLine(request);
            int schedulerType = Int32.Parse(configurations[0]);
            int delayBound = Int32.Parse(configurations[1]);
            Runtime.Options.Monitoring.PauseAtBug = Boolean.Parse(configurations[2]);
            Runtime.Options.Monitoring.PauseAtExecutionEnd = Boolean.Parse(configurations[3]);
        }

        /// <summary>
        /// Communication with the P# monitor to sends execution results.
        /// </summary>
        /// <param name="foundBug">Found a bug?</param>
        private static void SendResultsToPSharpMonitor(bool foundBug)
        {
            if ((Runtime.Options.Monitoring.PauseAtBug && foundBug) ||
                Runtime.Options.Monitoring.PauseAtExecutionEnd)
            {
                Console.ReadLine();
            }

            string request = foundBug.ToString();

            using (var pipe = new NamedPipeServerStream("PSharpMonitorResultsPipe",
                PipeDirection.Out))
            {
                Utilities.Verbose("Connecting to the P# monitor ...\n");
                pipe.WaitForConnection();
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(request);
                pipe.Write(buffer, 0, buffer.Length);
                pipe.Flush();
                pipe.WaitForPipeDrain();
                Utilities.Verbose("Results successfully sent to the P# monitor.\n");
            }
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

                if (Runtime.Options.Mode == Runtime.Mode.BugFinding &&
                    Runtime.Options.MonitorExecutions)
                {
                    Runtime.SendResultsToPSharpMonitor(true);
                }
                else if (!Runtime.Options.CountAssertions)
                {
                    //Console.ReadLine();
                    //Environment.Exit(1);
                }
                else
                {
                    Runtime.AssertionFailureCount++;
                }

                //Runtime.Stop();
                Runtime.Scheduler.ErrorOccurred();
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        /// <param name="s">Message</param>
        /// <param name="args">Message arguments</param>
        public static void Assert(bool predicate, string s, params object[] args)
        {
            if (!predicate)
            {
                string message = Utilities.Format(s, args);
                Utilities.ReportError(message);

                if (Runtime.Options.Mode == Runtime.Mode.BugFinding &&
                    Runtime.Options.MonitorExecutions)
                {
                    Runtime.SendResultsToPSharpMonitor(true);
                }
                else if (!Runtime.Options.CountAssertions)
                {
                    Console.ReadLine();
                    Environment.Exit(1);
                }
                else
                {
                    Runtime.AssertionFailureCount++;
                }

                //Runtime.Stop();
                //throw new TaskCanceledException();
                Runtime.Scheduler.ErrorOccurred();
            }
        }

        #endregion
    }
}
