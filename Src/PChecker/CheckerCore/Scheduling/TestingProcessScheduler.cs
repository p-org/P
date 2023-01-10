// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using PChecker.SystematicTesting;
using PChecker.Interfaces;
using PChecker.SmartSockets;
using PChecker.Testing;
using PChecker.Utilities;

namespace PChecker.Scheduling
{
    public class TestingProcessScheduler
    {
        /// <summary>
        /// CheckerConfiguration.
        /// </summary>
        private readonly CheckerConfiguration _checkerConfiguration;

        /// <summary>
        /// The server that all the TestingProcess clients will connect to.
        /// </summary>
        private SmartSocketServer Server;

        /// <summary>
        /// Map from testing process ids to testing processes.
        /// </summary>
        private readonly Dictionary<uint, Process> TestingProcesses;

        /// <summary>
        /// Map from testing process name to testing process channels.
        /// </summary>
        private readonly Dictionary<string, SmartSocketClient> TestingProcessChannels;

        /// <summary>
        /// Total number of remote test processes that have called home.
        /// </summary>
        private int TestProcessesConnected;

        /// <summary>
        /// Time that last message was received from a parallel test.
        /// </summary>
        private int LastMessageTime;

        /// <summary>
        /// Records if we want certain child test processes to terminate, this key here is the
        /// SmartSocketClient Name.
        /// </summary>
        private readonly HashSet<string> Terminating = new HashSet<string>();

        /// <summary>
        /// The test reports per process.
        /// </summary>
        private readonly ConcurrentDictionary<uint, TestReport> TestReports;

        /// <summary>
        /// Test Trace files.
        /// </summary>
        private readonly ConcurrentDictionary<uint, string> traceFiles;

        /// <summary>
        /// The global test report, which contains merged information
        /// from the test report of each testing process.
        /// </summary>
        private readonly TestReport GlobalTestReport;

        /// <summary>
        /// The testing profiler.
        /// </summary>
        private readonly Profiler Profiler;

        /// <summary>
        /// The scheduler lock.
        /// </summary>
        private readonly object SchedulerLock;

        /// <summary>
        /// The process id of the process that discovered a bug, else null.
        /// </summary>
        private uint? BugFoundByProcess;

        /// <summary>
        /// Set if ctrl-c or ctrl-break occurred.
        /// </summary>
        public static bool IsProcessCanceled;

        /// <summary>
        /// Whether to write verbose output.
        /// </summary>
        private readonly bool IsVerbose;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestingProcessScheduler"/> class.
        /// </summary>
        private TestingProcessScheduler(CheckerConfiguration checkerConfiguration)
        {
            TestingProcesses = new Dictionary<uint, Process>();
            TestingProcessChannels = new Dictionary<string, SmartSocketClient>();
            TestReports = new ConcurrentDictionary<uint, TestReport>();
            traceFiles = new ConcurrentDictionary<uint, string>();
            GlobalTestReport = new TestReport(checkerConfiguration);
            Profiler = new Profiler();
            SchedulerLock = new object();
            BugFoundByProcess = null;
            
            IsVerbose = checkerConfiguration.IsVerbose;

            checkerConfiguration.EnableColoredConsoleOutput = true;

            _checkerConfiguration = checkerConfiguration;
        }

        /// <summary>
        /// Notifies the testing process scheduler that a bug was found.
        /// </summary>
        private void NotifyBugFound(uint processId)
        {
            var name = "PCheckerProcess." + processId;
            lock (Terminating)
            {
                Terminating.Add(name);
            }

            lock (SchedulerLock)
            {
                if (!_checkerConfiguration.PerformFullExploration && BugFoundByProcess is null)
                {
                    Console.WriteLine($"... Task {processId} found a bug.");
                    BugFoundByProcess = processId;
                    // must be async relative to this NotifyBugFound handler.
                    Task.Run(() => CleanupTestProcesses(processId));
                }
            }
        }

        private async void CleanupTestProcesses(uint bugProcessId, int maxWait = 60000)
        {
            try
            {
                var serverName = _checkerConfiguration.TestingSchedulerEndPoint;
                var stopRequest = new TestServerMessage("TestServerMessage", serverName)
                {
                    Stop = true
                };

                var snapshot = new Dictionary<uint, Process>(TestingProcesses);

                foreach (var testingProcess in snapshot)
                {
                    if (testingProcess.Key != bugProcessId)
                    {
                        var name = "PCheckerProcess." + testingProcess.Key;

                        lock (Terminating)
                        {
                            Terminating.Add(name);
                        }

                        if (TestingProcessChannels.TryGetValue(name, out var client) && client.BackChannel != null)
                        {
                            // use the back channel to stop the client immediately, which will trigger client
                            // to also send us their TestReport (on the regular channel).
                            await client.BackChannel.SendAsync(stopRequest);
                        }
                    }
                }

                await WaitForParallelTestReports(maxWait);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"... Exception: {ex.Message}");
            }
        }

        private void KillTestingProcesses()
        {
            lock (SchedulerLock)
            {
                foreach (var testingProcess in TestingProcesses)
                {
                    try
                    {
                        var process = testingProcess.Value;
                        if (!process.HasExited)
                        {
                            IO.Debug.WriteLine("... Killing child process : " + process.Id);
                            process.Kill();
                            process.Dispose();
                        }
                    }
                    catch (Exception e)
                    {
                        IO.Debug.WriteLine("... Unable to terminate testing process: " + e.Message);
                    }
                }

                TestingProcesses.Clear();
            }
        }

        /// <summary>
        /// Sets the test report from the specified process.
        /// </summary>
        private void SetTestReport(TestReport testReport, uint processId)
        {
            lock (SchedulerLock)
            {
                MergeTestReport(testReport, processId);
            }
        }

        /// <summary>
        /// Creates a new testing process scheduler.
        /// </summary>
        public static TestingProcessScheduler Create(CheckerConfiguration checkerConfiguration)
        {
            return new TestingProcessScheduler(checkerConfiguration);
        }

        /// <summary>
        /// Runs the Coyote testing scheduler.
        /// </summary>
        public void Run()
        {
            Profiler.StartMeasuringExecutionTime();

            CreateAndRunInMemoryTestingProcess();
            
            Profiler.StopMeasuringExecutionTime();

            if (!IsProcessCanceled)
            {
                // Merges and emits the test report.
                EmitTestReport();
            }
        }

        private async Task WaitForParallelTestReports(int maxWait = 60000)
        {
            LastMessageTime = Environment.TickCount;

            // wait 60 seconds for tasks to call back with all their reports and disconnect.
            // and reset the click each time a message is received
            while (TestingProcessChannels.Count > 0)
            {
                await Task.Delay(100);
                AssertTestProcessActivity(maxWait);
            }
        }

        private void AssertTestProcessActivity(int maxWait)
        {
            if (LastMessageTime + maxWait < Environment.TickCount)
            {
                // oh dear, haven't heard from anyone in 60 seconds, and they have not
                // disconnected, so time to get out the sledge hammer and kill them!
                KillTestingProcesses();
                throw new Exception("Terminating TestProcesses due to inactivity");
            }
        }

        /// <summary>
        /// Creates and runs an in-memory testing process.
        /// </summary>
        private void CreateAndRunInMemoryTestingProcess()
        {
            var testingProcess = TestingProcess.Create(_checkerConfiguration);

            // Runs the testing process.
            testingProcess.Run();

            // Get and merge the test report.
            var testReport = testingProcess.GetTestReport();
            if (testReport != null)
            {
                MergeTestReport(testReport, 0);
            }
        }

        /// <summary>
        /// Opens the local server for TestingProcesses to connect to.
        /// If we are not running anything out of process then this does nothing.
        /// </summary>
        private void StartServer()
        {
            return;
        }

        private async void OnBackChannelOpened(object sender, SmartSocketClient e)
        {
            // this is the socket we can use to communicate directly to the client... it will be
            // available as the "BackChannel" property on the associated client socket.
            // But if we've already asked this client to terminate then tell it to stop.
            SocketMessage response = new TestServerMessage("ok", _checkerConfiguration.TestingSchedulerEndPoint);
            TestServerMessage message = null;
            lock (Terminating)
            {
                if (Terminating.Contains(e.Name))
                {
                    message = new TestServerMessage("ok", _checkerConfiguration.TestingSchedulerEndPoint)
                    {
                        Stop = true
                    };
                }
            }

            if (message != null)
            {
                await e.BackChannel.SendAsync(message);
            }
        }

        private void OnClientDisconnected(object sender, SmartSocketClient e)
        {
            lock (SchedulerLock)
            {
                TestingProcessChannels.Remove(e.Name);
            }
        }

        private void OnClientConnected(object sender, SmartSocketClient e)
        {
            e.Error += OnClientError;

            if (IsVerbose)
            {
                Console.WriteLine($"... TestProcess '{e.Name}' is connected");
            }

            Task.Run(() => HandleClientAsync(e));
        }

        private async void HandleClientAsync(SmartSocketClient client)
        {
            while (client.IsConnected)
            {
                var e = await client.ReceiveAsync();
                if (e != null)
                {
                    LastMessageTime = Environment.TickCount;
                    uint processId = 0;

                    if (e.Id == SmartSocketClient.ConnectedMessageId)
                    {
                        lock (SchedulerLock)
                        {
                            TestProcessesConnected++;
                            TestingProcessChannels.Add(e.Sender, client);
                        }
                    }
                    else if (e is BugFoundMessage)
                    {
                        var bug = (BugFoundMessage)e;
                        processId = bug.ProcessId;
                        await client.SendAsync(new SocketMessage("ok", _checkerConfiguration.TestingSchedulerEndPoint));
                        if (IsVerbose)
                        {
                            Console.WriteLine($"... Bug report received from '{bug.Sender}'");
                        }

                        NotifyBugFound(processId);
                    }
                    else if (e is TestReportMessage)
                    {
                        var report = (TestReportMessage)e;
                        processId = report.ProcessId;
                        await client.SendAsync(new SocketMessage("ok", _checkerConfiguration.TestingSchedulerEndPoint));
                        if (IsVerbose)
                        {
                            Console.WriteLine($"... Test report received from '{report.Sender}'");
                        }

                        SetTestReport(report.TestReport, report.ProcessId);
                    }
                    else if (e is TestTraceMessage)
                    {
                        var report = (TestTraceMessage)e;
                        processId = report.ProcessId;
                        await client.SendAsync(new SocketMessage("ok", _checkerConfiguration.TestingSchedulerEndPoint));
                        SaveTraceReport(report);
                    }
                    else if (e is TestProgressMessage)
                    {
                        var progress = (TestProgressMessage)e;
                        processId = progress.ProcessId;
                        await client.SendAsync(new SocketMessage("ok", _checkerConfiguration.TestingSchedulerEndPoint));
                        // todo: do something fun with progress info.
                    }
                }
            }
        }

        private void SaveTraceReport(TestTraceMessage report)
        {
            if (report.Contents != null)
            {
                var fileName = _checkerConfiguration.AssemblyToBeAnalyzed;
                var targetDir = Path.GetDirectoryName(fileName);
                var outputDir = Path.Combine(targetDir, "Output", Path.GetFileName(fileName), "PCheckerOutput");
                var remoteFileName = Path.GetFileName(report.FileName);
                var localTraceFile = Path.Combine(outputDir, remoteFileName);
                File.WriteAllText(localTraceFile, report.Contents);
                Console.WriteLine($"... Saved trace report: {localTraceFile}");
            }
            else
            {
                // tests ran locally so the file name is good!
                Console.WriteLine($"... See trace report: {report.FileName}");
            }
        }

        private void OnClientError(object sender, Exception e)
        {
            // todo: handle client failures?  The client process died, etc...
            var client = (SmartSocketClient)sender;
            if (!Terminating.Contains(client.Name))
            {
                Console.WriteLine($"### Error from client {client.Name}: {e.Message}");
            }
        }

        /// <summary>
        /// Merges the test report from the specified process.
        /// </summary>
        private void MergeTestReport(TestReport testReport, uint processId)
        {
            if (TestReports.TryAdd(processId, testReport))
            {
                // Merges the test report into the global report.
                IO.Debug.WriteLine($"... Merging task {processId} test report.");
                GlobalTestReport.Merge(testReport);
            }
            else
            {
                IO.Debug.WriteLine($"... Unable to merge test report from task '{processId}'. " +
                                            " Report is already merged.");
            }
        }

        /// <summary>
        /// Emits the test report.
        /// </summary>
        private void EmitTestReport()
        {
            var testReports = new List<TestReport>(TestReports.Values);
            foreach (var process in TestingProcesses)
            {
                if (!TestReports.ContainsKey(process.Key))
                {
                    Console.WriteLine($"... Task {process.Key} failed due to an internal error.");
                }
            }

            if (TestReports.Count == 0)
            {
                Environment.ExitCode = (int)ExitCode.InternalError;
                return;
            }

            if (_checkerConfiguration.ReportActivityCoverage)
            {
                Console.WriteLine($"... Emitting coverage reports:");
                Reporter.EmitTestingCoverageReport(GlobalTestReport);
            }

            if (_checkerConfiguration.DebugActivityCoverage)
            {
                Console.WriteLine($"... Emitting debug coverage reports:");
                foreach (var report in TestReports)
                {
                    Reporter.EmitTestingCoverageReport(report.Value, report.Key, isDebug: true);
                }
            }

            Console.WriteLine(GlobalTestReport.GetText(_checkerConfiguration, "..."));
            Console.WriteLine($"... Elapsed {Profiler.Results()} sec.");

            if (GlobalTestReport.InternalErrors.Count > 0)
            {
                Environment.ExitCode = (int)ExitCode.InternalError;
            }
            else if (GlobalTestReport.NumOfFoundBugs > 0)
            {
                Environment.ExitCode = (int)ExitCode.BugFound;
            }
            else
            {
                Environment.ExitCode = (int)ExitCode.Success;
            }
        }
    }
}
