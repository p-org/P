// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using PChecker.Coverage;
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
            this.TestingProcesses = new Dictionary<uint, Process>();
            this.TestingProcessChannels = new Dictionary<string, SmartSocketClient>();
            this.TestReports = new ConcurrentDictionary<uint, TestReport>();
            this.traceFiles = new ConcurrentDictionary<uint, string>();
            this.GlobalTestReport = new TestReport(checkerConfiguration);
            this.Profiler = new Profiler();
            this.SchedulerLock = new object();
            this.BugFoundByProcess = null;
            
            this.IsVerbose = checkerConfiguration.IsVerbose;

            checkerConfiguration.EnableColoredConsoleOutput = true;

            this._checkerConfiguration = checkerConfiguration;
        }

        /// <summary>
        /// Notifies the testing process scheduler that a bug was found.
        /// </summary>
        private void NotifyBugFound(uint processId)
        {
            string name = "PCheckerProcess." + processId;
            lock (this.Terminating)
            {
                this.Terminating.Add(name);
            }

            lock (this.SchedulerLock)
            {
                if (!this._checkerConfiguration.PerformFullExploration && this.BugFoundByProcess is null)
                {
                    Console.WriteLine($"... Process {processId} found a bug.");
                    this.BugFoundByProcess = processId;
                    // must be async relative to this NotifyBugFound handler.
                    Task.Run(() => this.CleanupTestProcesses(processId));
                }
            }
        }

        private async void CleanupTestProcesses(uint bugProcessId, int maxWait = 60000)
        {
            try
            {
                string serverName = this._checkerConfiguration.TestingSchedulerEndPoint;
                var stopRequest = new TestServerMessage("TestServerMessage", serverName)
                {
                    Stop = true
                };

                var snapshot = new Dictionary<uint, Process>(this.TestingProcesses);

                foreach (var testingProcess in snapshot)
                {
                    if (testingProcess.Key != bugProcessId)
                    {
                        string name = "PCheckerProcess." + testingProcess.Key;

                        lock (this.Terminating)
                        {
                            this.Terminating.Add(name);
                        }

                        if (this.TestingProcessChannels.TryGetValue(name, out SmartSocketClient client) && client.BackChannel != null)
                        {
                            // use the back channel to stop the client immediately, which will trigger client
                            // to also send us their TestReport (on the regular channel).
                            await client.BackChannel.SendAsync(stopRequest);
                        }
                    }
                }

                await this.WaitForParallelTestReports(maxWait);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"... Exception: {ex.Message}");
            }
        }

        private void KillTestingProcesses()
        {
            lock (this.SchedulerLock)
            {
                foreach (var testingProcess in this.TestingProcesses)
                {
                    try
                    {
                        var process = testingProcess.Value;
                        if (!process.HasExited)
                        {
                            PChecker.IO.Debug.WriteLine("... Killing child process : " + process.Id);
                            process.Kill();
                            process.Dispose();
                        }
                    }
                    catch (Exception e)
                    {
                        PChecker.IO.Debug.WriteLine("... Unable to terminate testing process: " + e.Message);
                    }
                }

                this.TestingProcesses.Clear();
            }
        }

        /// <summary>
        /// Sets the test report from the specified process.
        /// </summary>
        private void SetTestReport(TestReport testReport, uint processId)
        {
            lock (this.SchedulerLock)
            {
                this.MergeTestReport(testReport, processId);
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
            Console.WriteLine($"Starting TestingProcessScheduler in process Id {Process.GetCurrentProcess().Id}");

            // Start the local server.
            this.StartServer();

            this.Profiler.StartMeasuringExecutionTime();

            this.CreateAndRunInMemoryTestingProcess();
            
            this.Profiler.StopMeasuringExecutionTime();

            // Stop listening and close the server.
            this.StopServer();

            if (!IsProcessCanceled)
            {
                // Merges and emits the test report.
                this.EmitTestReport();
            }
        }

        private async Task WaitForParallelTestingProcesses()
        {
            if (this.TestingProcesses.Count > 0)
            {
                Console.WriteLine($"... Waiting for testing processes to start. Use the following command line to launch each test");
                Console.WriteLine($"... Make sure to change /testing-process-id:x so that x goes from 0 to {this.TestingProcesses.Count}");
                Process p = this.TestingProcesses[0];
                Console.WriteLine($"{p.StartInfo.FileName} {p.StartInfo.Arguments}");
            }

            await this.WaitForParallelTestReports();
        }

        private async Task WaitForParallelTestReports(int maxWait = 60000)
        {
            this.LastMessageTime = Environment.TickCount;

            // wait 60 seconds for tasks to call back with all their reports and disconnect.
            // and reset the click each time a message is received
            while (this.TestingProcessChannels.Count > 0)
            {
                await Task.Delay(100);
                this.AssertTestProcessActivity(maxWait);
            }
        }

        private void AssertTestProcessActivity(int maxWait)
        {
            if (this.LastMessageTime + maxWait < Environment.TickCount)
            {
                // oh dear, haven't heard from anyone in 60 seconds, and they have not
                // disconnected, so time to get out the sledge hammer and kill them!
                this.KillTestingProcesses();
                throw new Exception("Terminating TestProcesses due to inactivity");
            }
        }

        /// <summary>
        /// Creates and runs an in-memory testing process.
        /// </summary>
        private void CreateAndRunInMemoryTestingProcess()
        {
            TestingProcess testingProcess = TestingProcess.Create(this._checkerConfiguration);

            Console.WriteLine($"... Created '1' testing task.");

            // Runs the testing process.
            testingProcess.Run();

            // Get and merge the test report.
            TestReport testReport = testingProcess.GetTestReport();
            if (testReport != null)
            {
                this.MergeTestReport(testReport, 0);
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
            SocketMessage response = new TestServerMessage("ok", this._checkerConfiguration.TestingSchedulerEndPoint);
            TestServerMessage message = null;
            lock (this.Terminating)
            {
                if (this.Terminating.Contains(e.Name))
                {
                    message = new TestServerMessage("ok", this._checkerConfiguration.TestingSchedulerEndPoint)
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
            lock (this.SchedulerLock)
            {
                this.TestingProcessChannels.Remove(e.Name);
            }
        }

        private void OnClientConnected(object sender, SmartSocketClient e)
        {
            e.Error += this.OnClientError;

            if (this.IsVerbose)
            {
                Console.WriteLine($"... TestProcess '{e.Name}' is connected");
            }

            Task.Run(() => this.HandleClientAsync(e));
        }

        private async void HandleClientAsync(SmartSocketClient client)
        {
            while (client.IsConnected)
            {
                SocketMessage e = await client.ReceiveAsync();
                if (e != null)
                {
                    this.LastMessageTime = Environment.TickCount;
                    uint processId = 0;

                    if (e.Id == SmartSocketClient.ConnectedMessageId)
                    {
                        lock (this.SchedulerLock)
                        {
                            this.TestProcessesConnected++;
                            this.TestingProcessChannels.Add(e.Sender, client);
                        }
                    }
                    else if (e is BugFoundMessage)
                    {
                        BugFoundMessage bug = (BugFoundMessage)e;
                        processId = bug.ProcessId;
                        await client.SendAsync(new SocketMessage("ok", this._checkerConfiguration.TestingSchedulerEndPoint));
                        if (this.IsVerbose)
                        {
                            Console.WriteLine($"... Bug report received from '{bug.Sender}'");
                        }

                        this.NotifyBugFound(processId);
                    }
                    else if (e is TestReportMessage)
                    {
                        TestReportMessage report = (TestReportMessage)e;
                        processId = report.ProcessId;
                        await client.SendAsync(new SocketMessage("ok", this._checkerConfiguration.TestingSchedulerEndPoint));
                        if (this.IsVerbose)
                        {
                            Console.WriteLine($"... Test report received from '{report.Sender}'");
                        }

                        this.SetTestReport(report.TestReport, report.ProcessId);
                    }
                    else if (e is TestTraceMessage)
                    {
                        TestTraceMessage report = (TestTraceMessage)e;
                        processId = report.ProcessId;
                        await client.SendAsync(new SocketMessage("ok", this._checkerConfiguration.TestingSchedulerEndPoint));
                        this.SaveTraceReport(report);
                    }
                    else if (e is TestProgressMessage)
                    {
                        TestProgressMessage progress = (TestProgressMessage)e;
                        processId = progress.ProcessId;
                        await client.SendAsync(new SocketMessage("ok", this._checkerConfiguration.TestingSchedulerEndPoint));
                        // todo: do something fun with progress info.
                    }
                }
            }
        }

        private void SaveTraceReport(TestTraceMessage report)
        {
            if (report.Contents != null)
            {
                string fileName = this._checkerConfiguration.AssemblyToBeAnalyzed;
                string targetDir = Path.GetDirectoryName(fileName);
                string outputDir = Path.Combine(targetDir, "Output", Path.GetFileName(fileName), "PCheckerOutput");
                string remoteFileName = Path.GetFileName(report.FileName);
                string localTraceFile = Path.Combine(outputDir, remoteFileName);
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
            SmartSocketClient client = (SmartSocketClient)sender;
            if (!this.Terminating.Contains(client.Name))
            {
                Console.WriteLine($"### Error from client {client.Name}: {e.Message}");
            }
        }

        /// <summary>
        /// Closes the local server, if we have one.
        /// </summary>
        private void StopServer()
        {
            if (this.Server != null)
            {
                this.Server.Stop();
                this.Server = null;
            }
        }

        /// <summary>
        /// Merges the test report from the specified process.
        /// </summary>
        private void MergeTestReport(TestReport testReport, uint processId)
        {
            if (this.TestReports.TryAdd(processId, testReport))
            {
                // Merges the test report into the global report.
                PChecker.IO.Debug.WriteLine($"... Merging task {processId} test report.");
                this.GlobalTestReport.Merge(testReport);
            }
            else
            {
                PChecker.IO.Debug.WriteLine($"... Unable to merge test report from task '{processId}'. " +
                                            " Report is already merged.");
            }
        }

        /// <summary>
        /// Emits the test report.
        /// </summary>
        private void EmitTestReport()
        {
            var testReports = new List<TestReport>(this.TestReports.Values);
            foreach (var process in this.TestingProcesses)
            {
                if (!this.TestReports.ContainsKey(process.Key))
                {
                    Console.WriteLine($"... Task {process.Key} failed due to an internal error.");
                }
            }

            if (this.TestReports.Count == 0)
            {
                Environment.ExitCode = (int)ExitCode.InternalError;
                return;
            }

            if (this._checkerConfiguration.ReportActivityCoverage)
            {
                Console.WriteLine($"... Emitting coverage reports:");
                Reporter.EmitTestingCoverageReport(this.GlobalTestReport);
            }

            if (this._checkerConfiguration.DebugActivityCoverage)
            {
                Console.WriteLine($"... Emitting debug coverage reports:");
                foreach (var report in this.TestReports)
                {
                    Reporter.EmitTestingCoverageReport(report.Value, report.Key, isDebug: true);
                }
            }

            Console.WriteLine(this.GlobalTestReport.GetText(this._checkerConfiguration, "..."));
            Console.WriteLine($"... Elapsed {this.Profiler.Results()} sec.");

            if (this.GlobalTestReport.InternalErrors.Count > 0)
            {
                Environment.ExitCode = (int)ExitCode.InternalError;
            }
            else if (this.GlobalTestReport.NumOfFoundBugs > 0)
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
