using Microsoft.Formula.API;
using Microsoft.Pc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Microsoft.Pc
{

    /// <summary>
    /// This class starts the compiler service and sends jobs to it.
    /// </summary>
    public class CompilerServiceClient : IDisposable
    {
        const string ServerPipeName = "63642A12-F751-41E3-A9D3-279EE34A0EDB-CompilerService";
        NamedPipe service;
        string id;

        public static string JobFinishedMessage = "<job-finished>";
        public static string CompilerLockMessage = "<lock>";
        public static string CompilerFreeMessage = "<free>";

        public bool Link(CommandLineOptions options, TextWriter log)
        {
            options.isLinkerPhase = true;
            return Compile(options, log);
        }

        private NamedPipe Connect(TextWriter log)
        {
            Mutex processLock = new Mutex(false, "PCompilerService");
            processLock.WaitOne();
            try
            {
                if (service == null)
                {
                    service = new NamedPipe(ServerPipeName);

                    if (!service.Connect())
                    {
                        ProcessStartInfo info = new ProcessStartInfo();
                        info.FileName = typeof(CompilerServiceClient).Assembly.Location;
                        info.WindowStyle = ProcessWindowStyle.Hidden;
                        Process p = Process.Start(info);
                        if (!service.Connect())
                        {
                            log.WriteLine("Cannot start the CompilerService?");
                            service = null;
                            return null;
                        }
                        else
                        {
                            // now lock a Compiler object until we re disposed so we can get better
                            // performance by sharing the same Compiler across compile, link and test.
                            service.WriteMessage(CompilerLockMessage);
                            this.id = service.ReadMessage();
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
            Debug.WriteLine("(" + System.Threading.Thread.CurrentThread.ManagedThreadId + ") " + msg);
        }


        public bool Compile(CommandLineOptions options, TextWriter log)
        {
            bool finished = false;
            bool result = false;

            NamedPipe service = Connect(log);

            CompilerOutputStream output = new CompilerOutputStream(log);
            options.compilerId = id;
            StringWriter writer = new StringWriter();
            XmlSerializer serializer = new XmlSerializer(typeof(CommandLineOptions));
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
                        SeverityKind severity = SeverityKind.Info;
                        Enum.TryParse<SeverityKind>(sev, out severity);
                        msg = msg.Substring(i + 2);

                        if (msg.StartsWith(JobFinishedMessage))
                        {
                            string tail = msg.Substring(JobFinishedMessage.Length);
                            finished = true;
                            bool.TryParse(tail, out result);
                        }
                        else
                        {
                            output.WriteMessage(msg, severity);
                        }
                    }
                    else
                    {
                        log.WriteLine(msg);
                    }
                }
            }
            catch (Exception)
            { 
                result = false;
                output.WriteMessage("PCompilerService is gone, did someone kill it?  Perhaps the P build is happening in parallel?", SeverityKind.Error);
                finished = true;
            }

            return result;
        }

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
            service.Close(); // we can only write one message at a time.
        }
    }
}
