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
    public class CompilerServiceClient
    {
        const string ServerPipeName = "63642A12-F751-41E3-A9D3-279EE34A0EDB-CompilerService";
        NamedPipe client;
        NamedPipe service;

        public bool Compile(CommandLineOptions options)
        {
            service = new NamedPipe(ServerPipeName, false);
            Mutex processLock = new Mutex(false, "PCompilerService");
            processLock.WaitOne();
            try
            {
                if (!service.Connect())
                {
                    ProcessStartInfo info = new ProcessStartInfo();
                    info.FileName = typeof(CompilerServiceClient).Assembly.Location;
                    info.WindowStyle = ProcessWindowStyle.Hidden;
                    Process p = Process.Start(info);
                    if (!service.Connect())
                    {
                        Console.WriteLine("Cannot start the CompilerService?");
                        return false;
                    }
                }
            }
            finally
            {
                processLock.ReleaseMutex();
            }
            Guid clientPipe = Guid.NewGuid();
            string clientPipeName = clientPipe.ToString() + "-CompilerServiceClient";
            client = new NamedPipe(clientPipeName, true);
            if (!client.Connect())
            {
                Console.WriteLine("weird, the process that launched this job is gone?");
                return false;
            }
            AutoResetEvent msgEvent = new AutoResetEvent(false);
            bool finished = false;
            bool result = false;

            StandardOutput stdout = new StandardOutput();
            client.MessageArrived += (s2, e2) =>
                        {
                            string msg = e2.Message;
                            int i = msg.IndexOf(':');
                            if (i > 0)
                            {
                                string sev = msg.Substring(0, i);
                                msg = msg.Substring(i + 2);
                                if (msg.StartsWith("finished:"))
                                {
                                    i = msg.IndexOf(':');
                                    string tail = msg.Substring(i + 1);
                                    finished = true;
                                    bool.TryParse(tail, out result);
                                    msgEvent.Set();
                                }
                                else
                                {
                                    SeverityKind severity = SeverityKind.Info;
                                    Enum.TryParse<SeverityKind>(sev, out severity);
                                    stdout.WriteMessage(msg, severity);
                                }
                            }
                            else
                            {
                                Console.WriteLine(e2.Message);
                            }
                        };

            options.pipeName = clientPipeName;

            StringWriter writer = new StringWriter();
            XmlSerializer serializer = new XmlSerializer(typeof(CommandLineOptions));
            serializer.Serialize(writer, options);
            service.WriteMessage(writer.ToString());

            while (!finished)
            {
                msgEvent.WaitOne(1000);
                if (client.IsClosed)
                {
                    result = false;
                    stdout.WriteMessage("PCompilerService is gone, did someone kill it?  Perhaps the P build is happening in parallel?", SeverityKind.Error);
                    finished = true;
                }
            }
            service.Close();
            client.Close();
            return result;
        }
    }
}
