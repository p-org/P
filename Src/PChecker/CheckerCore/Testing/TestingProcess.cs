// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using PChecker.Coverage;
using PChecker.SystematicTesting;
using PChecker.Interfaces;
using PChecker.SmartSockets;

namespace PChecker.Testing
{
    /// <summary>
    /// A testing process, this can also be the client side of a multi-process test
    /// </summary>
    public class TestingProcess
    {
        /// <summary>
        /// Whether this process is terminating.
        /// </summary>
        private bool Terminating;

        /// <summary>
        /// A name for the test client
        /// </summary>
        private readonly string Name = "PCheckerProcess";

        /// <summary>
        /// CheckerConfiguration.
        /// </summary>
        private readonly CheckerConfiguration _checkerConfiguration;

        /// <summary>
        /// The testing engine associated with
        /// this testing process.
        /// </summary>
        private readonly TestingEngine TestingEngine;

        /// <summary>
        /// The channel to the TestProcessScheduler.
        /// </summary>
        private SmartSocketClient Server;

        /// <summary>
        /// A way to synchronouse background progress task with the main thread.
        /// </summary>
        private ProgressLock ProgressTask;

        /// <summary>
        /// Creates a Coyote testing process.
        /// </summary>
        public static TestingProcess Create(CheckerConfiguration checkerConfiguration)
        {
            return new TestingProcess(checkerConfiguration);
        }

        /// <summary>
        /// Get the current test report.
        /// </summary>
        public TestReport GetTestReport()
        {
            return TestingEngine.TestReport.Clone();
        }

        // Gets a handle to the standard output and error streams.
        private readonly TextWriter StdOut = Console.Out;

        /// <summary>
        /// Runs the Coyote testing process.
        /// </summary>
        public void Run()
        {
            RunAsync().Wait();
        }

        private async Task RunAsync()
        {
            TestingEngine.Run();

            Console.SetOut(StdOut);

            Terminating = true;

            // wait for any pending progress
            var task = ProgressTask;
            if (task != null)
            {
                task.Wait(30000);
            }

            if (!_checkerConfiguration.PerformFullExploration &&
                TestingEngine.TestReport.NumOfFoundBugs > 0)
            {
                Console.WriteLine($"Checker found a bug.");
            }

            // we want the graph generation even if doing full exploration.
            if ((!_checkerConfiguration.PerformFullExploration && TestingEngine.TestReport.NumOfFoundBugs > 0) ||
                (_checkerConfiguration.IsDgmlGraphEnabled && !_checkerConfiguration.IsDgmlBugGraph))
            {
                await EmitTraces();
            }

            // Closes the remote notification listener.
            if (_checkerConfiguration.IsVerbose)
            {
                Console.WriteLine($"... ### Process {_checkerConfiguration.TestingProcessId} is terminating");
            }

            Disconnect();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestingProcess"/> class.
        /// </summary>
        private TestingProcess(CheckerConfiguration checkerConfiguration)
        {
            Name = Name + "." + checkerConfiguration.TestingProcessId;

            if (checkerConfiguration.SchedulingStrategy is "portfolio")
            {
                TestingPortfolio.ConfigureStrategyForCurrentProcess(checkerConfiguration);
            }

            if (checkerConfiguration.RandomGeneratorSeed.HasValue)
            {
                checkerConfiguration.RandomGeneratorSeed = checkerConfiguration.RandomGeneratorSeed.Value +
                    (673 * checkerConfiguration.TestingProcessId);
            }

            checkerConfiguration.EnableColoredConsoleOutput = true;

            _checkerConfiguration = checkerConfiguration;
            TestingEngine = TestingEngine.Create(_checkerConfiguration);
        }

        /// <inheritdoc />
        ~TestingProcess()
        {
            Terminating = true;
        }

        /// <summary>
        /// Opens the remote notification listener. If this is
        /// not a parallel testing process, then this operation
        /// does nothing.
        /// </summary>
        private async Task ConnectToServer()
        {
            var serviceName = _checkerConfiguration.TestingSchedulerEndPoint;
            var source = new CancellationTokenSource();

            var resolver = new SmartSocketTypeResolver(typeof(BugFoundMessage),
                                                       typeof(TestReportMessage),
                                                       typeof(TestServerMessage),
                                                       typeof(TestProgressMessage),
                                                       typeof(TestTraceMessage),
                                                       typeof(TestReport),
                                                       typeof(CoverageInfo),
                                                       typeof(CheckerConfiguration));

            SmartSocketClient client = null;
            client = await SmartSocketClient.FindServerAsync(serviceName, Name, resolver, source.Token);
            

            if (client == null)
            {
                throw new Exception("Failed to connect to server");
            }

            client.Error += OnClientError;
            client.ServerName = serviceName;
            Server = client;

            // open back channel so server can also send messages to us any time.
            await client.OpenBackChannel(OnBackChannelConnected);
        }

        private void OnBackChannelConnected(object sender, SmartSocketClient e)
        {
            Task.Run(() => HandleBackChannel(e));
        }

        private async void HandleBackChannel(SmartSocketClient server)
        {
            while (!Terminating && server.IsConnected)
            {
                var msg = await server.ReceiveAsync();
                if (msg is TestServerMessage)
                {
                    HandleServerMessage((TestServerMessage)msg);
                }
            }
        }

        private void OnClientError(object sender, Exception e)
        {
            // todo: error handling, happens if we fail to get a message to the server for some reason.
        }

        /// <summary>
        /// Closes the remote notification listener. If this is
        /// not a parallel testing process, then this operation
        /// does nothing.
        /// </summary>
        private void Disconnect()
        {
            using (Server)
            {
                if (Server != null)
                {
                    Server.Close();
                }
            }
        }

        /// <summary>
        /// Notifies the remote testing scheduler
        /// about a discovered bug.
        /// </summary>
        private async Task NotifyBugFound()
        {
            await Server.SendReceiveAsync(new BugFoundMessage("BugFoundMessage", Name, _checkerConfiguration.TestingProcessId));
        }

        /// <summary>
        /// Sends the test report associated with this testing process.
        /// </summary>
        private async Task SendTestReport()
        {
            var report = TestingEngine.TestReport.Clone();
            await Server.SendReceiveAsync(new TestReportMessage("TestReportMessage", Name, _checkerConfiguration.TestingProcessId, report));
        }

        /// <summary>
        /// Emits the testing traces.
        /// </summary>
        private async Task EmitTraces()
        {
            var file = Path.GetFileNameWithoutExtension(_checkerConfiguration.AssemblyToBeAnalyzed);
            file += "_" + _checkerConfiguration.TestingProcessId;

            Console.WriteLine($"... Emitting traces:");
            var traces = new List<string>(TestingEngine.TryEmitTraces(_checkerConfiguration.OutputDirectory, file));

            if (Server != null && Server.IsConnected)
            {
                await SendTraces(traces);
            }
        }

        private async Task SendTraces(List<string> traces)
        {
            var localEndPoint = (IPEndPoint)Server.Socket.LocalEndPoint;
            var serverEndPoint = (IPEndPoint)Server.Socket.RemoteEndPoint;
            var differentMachine = localEndPoint.Address.ToString() != serverEndPoint.Address.ToString();
            foreach (var filename in traces)
            {
                string contents = null;
                if (differentMachine)
                {
                    Console.WriteLine($"... Sending trace file: {filename}");
                    contents = File.ReadAllText(filename);
                }

                await Server.SendReceiveAsync(new TestTraceMessage("TestTraceMessage", Name, _checkerConfiguration.TestingProcessId, filename, contents));
            }
        }

        /// <summary>
        /// Creates a task that pings the server with a heartbeat telling the server our current progress..
        /// </summary>
        private async void StartProgressMonitorTask()
        {
            while (!Terminating)
            {
                await Task.Delay(100);
                using (ProgressTask = new ProgressLock())
                {
                    await SendProgressMessage();
                }
            }
        }

        /// <summary>
        /// Sends the TestProgressMessage and if server cannot be reached, stop the testing.
        /// </summary>
        private async Task SendProgressMessage()
        {
            if (Server != null && !Terminating && Server.IsConnected)
            {
                var progress = 0.0; // todo: get this from the TestingEngine.
                try
                {
                    await Server.SendReceiveAsync(new TestProgressMessage("TestProgressMessage", Name, _checkerConfiguration.TestingProcessId, progress));
                }
                catch (Exception)
                {
                    // can't contact the server, so perhaps it died, time to stop.
                    TestingEngine.Stop();
                }
            }
        }

        private void HandleServerMessage(TestServerMessage tsr)
        {
            if (tsr.Stop)
            {
                // server wants us to stop!
                if (_checkerConfiguration.IsVerbose)
                {
                    StdOut.WriteLine($"... ### Client {_checkerConfiguration.TestingProcessId} is being told to stop!");
                }

                TestingEngine.Stop();
                Terminating = true;
            }
        }

        internal class ProgressLock : IDisposable
        {
            private bool Disposed;
            private bool WaitingOnProgress;
            private readonly object SyncObject = new object();
            private readonly ManualResetEvent ProgressEvent = new ManualResetEvent(false);

            public ProgressLock()
            {
            }

            ~ProgressLock()
            {
                Dispose();
            }

            public void Wait(int timeout = 10000)
            {
                var wait = false;
                lock (SyncObject)
                {
                    if (Disposed)
                    {
                        return;
                    }

                    WaitingOnProgress = true;
                    wait = true;
                }

                if (wait)
                {
                    ProgressEvent.WaitOne(timeout);
                }
            }

            public void Dispose()
            {
                lock (SyncObject)
                {
                    Disposed = true;
                    if (WaitingOnProgress)
                    {
                        ProgressEvent.Set();
                    }
                }

                GC.SuppressFinalize(this);
            }
        }
    }
}
