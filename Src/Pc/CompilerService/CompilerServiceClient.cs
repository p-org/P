using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Xml.Serialization;

namespace Microsoft.Pc
{
    /// <summary>
    ///     This class starts the compiler service and sends jobs to it.
    /// </summary>
    public class CompilerServiceClient : IDisposable, ICompiler
    {
        private const string ServerPipeName = "63642A12-F751-41E3-A9D3-279EE34A0EDB-CompilerService";

        public const string JobFinishedMessage = "<job-finished>";
        public const string CompilerLockMessage = "<lock>";
        public const string CompilerFreeMessage = "<free>";
        private string id;
        private NamedPipe service;

        public void Dispose()
        {
            if (service != null && !service.IsClosed)
            {
                // now free the Compiler object
                service.WriteMessage(CompilerFreeMessage + ":" + id);
                string handshake = service.ReadMessage();
                if (handshake != JobFinishedMessage)
                {
                    DebugWriteLine("Job Error: " + handshake);
                }
                else
                {
                    DebugWriteLine("Job Terminated: " + handshake);
                }
            }
            service?.Close(); // we can only write one message at a time.
        }

        public bool Link(ICompilerOutput log, CommandLineOptions options)
        {
            options.isLinkerPhase = true;
            return Compile(log, options);
        }

        private NamedPipe Connect(ICompilerOutput log)
        {
            var processLock = new Mutex(false, "PCompilerService");
            processLock.WaitOne();
            try
            {
                if (service == null)
                {
                    service = new NamedPipe(ServerPipeName);

                    if (!service.Connect())
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = typeof(CompilerServiceClient).Assembly.Location,
                            WindowStyle = ProcessWindowStyle.Hidden
                        });
                        if (!service.Connect())
                        {
                            log.WriteMessage("Cannot start the CompilerService?", SeverityKind.Error);
                            service = null;
                            return null;
                        }
                        else
                        {
                            // now lock a Compiler object until we are disposed so we can get better
                            // performance by sharing the same Compiler across compile, link and test.
                            service.WriteMessage(CompilerLockMessage);
                            id = service.ReadMessage();
                        }
                    }
                }
            }
            finally
            {
                processLock.ReleaseMutex();
            }

            return service;
        }

        private static void DebugWriteLine(string msg)
        {
            Debug.WriteLine("(" + Thread.CurrentThread.ManagedThreadId + ") " + msg);
        }

        public bool Compile(ICompilerOutput log, CommandLineOptions options)
        {
            var finished = false;
            var result = false;

            NamedPipe service = Connect(log);
            
            options.compilerId = id;
            var writer = new StringWriter();
            var serializer = new XmlSerializer(typeof(CommandLineOptions));
            serializer.Serialize(writer, options);
            service.WriteMessage(writer.ToString());

            try
            {
                while (!finished && !service.IsClosed)
                {
                    string msg = service.ReadMessage();
                    DebugWriteLine(msg);
                    int i = msg.IndexOf(':');
                    if (i > 0)
                    {
                        string sev = msg.Substring(0, i);
                        Enum.TryParse(sev, out SeverityKind severity);
                        msg = msg.Substring(i + 2);

                        if (msg.StartsWith(JobFinishedMessage))
                        {
                            string tail = msg.Substring(JobFinishedMessage.Length);
                            finished = true;
                            bool.TryParse(tail, out result);
                        }
                        else
                        {
                            log.WriteMessage(msg, severity);
                        }
                    }
                    else
                    {
                        log.WriteMessage(msg, SeverityKind.Info);
                    }
                }
            }
            catch (Exception)
            {
                result = false;
                log.WriteMessage(
                    "PCompilerService is gone, did someone kill it?  Perhaps the P build is happening in parallel?",
                    SeverityKind.Error);
            }

            return result;
        }
    }
}