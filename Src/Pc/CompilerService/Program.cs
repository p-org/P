using Microsoft.Formula.API;
using Microsoft.Pc;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Microsoft.Pc
{
    class Program
    {
        const string ServerPipeName = "63642A12-F751-41E3-A9D3-279EE34A0EDB-CompilerService";
        bool doMoreWork;
        int busyCount;
        int maxCompilers = 1;
        object compilerlock = new object();
        List<Compiler> compilerFree = new List<Compiler>();
        List<Compiler> compilerBusy = new List<Compiler>();

        static void Main(string[] args)
        {
            Program p = new Program();
            p.maxCompilers = Environment.ProcessorCount - 1; // leave 1 core for the user :-) 
            try
            {
                p.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        private void OnContractFailed(object sender, System.Diagnostics.Contracts.ContractFailedEventArgs e)
        {
            // compiler might be in a weird state, start over.
            // but which compiler is it?
            string reason = e.Message;
            if (e.OriginalException != null && e.OriginalException.Message != reason)
            {
                reason += ".  " + e.OriginalException.Message;
                if (e.OriginalException.InnerException != null)
                {
                    reason += ".  " + e.OriginalException.InnerException.Message;
                }
            }
            throw new Exception(reason);
        }

        void Run()
        {
            System.Diagnostics.Contracts.Contract.ContractFailed += OnContractFailed;
            Console.WriteLine("Starting compiler service, listening to named pipe");

            // start the server.
            NamedPipe pipe = new NamedPipe(ServerPipeName);
            pipe.ClientConnected += OnClientConnected;

            // we should never use more than Environment.ProcessorCount, the * 2 provides some slop for pipe cleanup from previous jobs.
            pipe.StartServer(Environment.ProcessorCount * 2); 

            doMoreWork = true;

            // stay awake waiting for work
            while (doMoreWork || busyCount > 0)
            {
                doMoreWork = false;
                Thread.Sleep(60 * 60 * 1000); // timeout after 1 hour
            }

            Console.WriteLine("Compiler service terminating due to lack of work");
        }

        private void OnClientConnected(object sender, NamedPipe pipe)
        {
            while (!pipe.IsClosed)
            {
                // the protocol after client connects is to send a job, we send back results until we
                // send the <job-finished> message, then we expect a "<job-finished>" handshake from client
                // or the client sends another job.
                string msg = pipe.ReadMessage();
                if (msg == CompilerServiceClient.JobFinishedMessage)
                {
                    // this session is done, double handshake.
                    pipe.WriteMessage(CompilerServiceClient.JobFinishedMessage);
                    return;
                }
                else if (!string.IsNullOrEmpty(msg))
                {
                    try
                    {
                        doMoreWork = true;
                        SerializedOutput output = new SerializedOutput(pipe);

                        bool result = ProcessJob(msg, output);

                        // now make sure client gets the JobFinishedMessage message!
                        output.WriteMessage(CompilerServiceClient.JobFinishedMessage + result.ToString(), SeverityKind.Info);
                        output.Flush();                        
                    }
                    catch (Exception)
                    {
                        // deserialization of the job failed, so ignore it.
                    }
                }
            }
            pipe.Close();
        }


        private Compiler GetFreeCompiler()
        {
            Compiler compiler = null;
            while (compiler == null)
            {
                lock (compilerlock)
                {
                    if (compilerFree.Count > 0)
                    {
                        int last = compilerFree.Count - 1;
                        compiler = compilerFree[last];
                        compilerFree.RemoveAt(last);
                        compilerBusy.Add(compiler);
                    }
                    else if (compilerBusy.Count < maxCompilers)
                    {
                        compiler = new Compiler(false);
                        compilerBusy.Add(compiler);
                    }
                }
                if (compiler == null)
                {
                    // wait for free compiler
                    Thread.Sleep(1000);
                }
            }
            return compiler;
        }

        private void FreeCompiler(Compiler compiler)
        {
            lock (compilerlock)
            {
                int i = compilerBusy.IndexOf(compiler);
                compilerBusy.RemoveAt(i);
                compilerFree.Add(compiler);
            }
        }

        private Compiler RebuildCompiler(Compiler compiler)
        {
            lock (compilerlock)
            {
                Compiler newCompiler = new Compiler(false);
                int i = compilerBusy.IndexOf(compiler);
                compilerBusy[i] = newCompiler;
                return newCompiler;
            }
        }

        private static void DebugWriteLine(string msg)
        {
            Debug.WriteLine("(" + System.Threading.Thread.CurrentThread.ManagedThreadId + ") " + msg);
        }

        private bool ProcessJob(string msg, ICompilerOutput output)
        {
            Interlocked.Increment(ref busyCount);
            bool result = false;
            Compiler compiler = null;
            CommandLineOptions options = null;

            try
            {
                XmlSerializer s = new XmlSerializer(typeof(CommandLineOptions));
                options = (CommandLineOptions)s.Deserialize(new StringReader(msg));

                bool retry = true;
                bool masterCreated = false;

                compiler = GetFreeCompiler();
                while (retry)
                {
                    retry = false;
                    try
                    {
                        if (options.inputFileNames == null)
                        {
                            // this is odd, compile will fail, but this stops the Debug output from crashing.
                            options.inputFileNames = new List<string>();
                        }

                        if (options.compilerOutput == CompilerOutput.Link)
                        {
                            DebugWriteLine("Linking: " + string.Join(", ", options.inputFileNames));
                            result = compiler.Link(output, options);
                        }
                        else
                        {
                            DebugWriteLine("Compiling: " + options.compilerOutput + ", " + string.Join(", ", options.inputFileNames));
                            result = compiler.Compile(output, options);
                        }
                    }
                    catch (Exception ex)
                    {
                        result = false;
                        if (!masterCreated)
                        {
                            // sometimes the compiler gets out of whack, and rebuilding it solves the problem.
                            masterCreated = true;
                            compiler = RebuildCompiler(compiler);
                            retry = true;
                        }
                        else
                        {
                            output.WriteMessage("Compile failed: " + ex.ToString(), SeverityKind.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result = false;
                if (output != null)
                {
                    output.WriteMessage("internal error: " + ex.ToString(), SeverityKind.Error);
                }
            }
            finally
            {
                Interlocked.Decrement(ref busyCount);
                if (compiler != null)
                {
                    FreeCompiler(compiler);
                }
            }

            try
            {
                if (options != null)
                {
                    DebugWriteLine("Finished: " + options.compilerOutput + ", " + string.Join(", ", options.inputFileNames) + ", result=" + result);
                }                
            }
            catch (Exception ex)
            {
                DebugWriteLine("Internal Error: " + ex.ToString());
            }
            return result;
        }

        public class SerializedOutput : ICompilerOutput
        {
            NamedPipe pipe;

            public SerializedOutput(NamedPipe pipe)
            {
                this.pipe = pipe;
            }

            public void WriteMessage(string msg, SeverityKind severity)
            {
                // send this back to the command line process that invoked this compiler.
                Program.DebugWriteLine("WriteMessage: " + msg);
                this.pipe.WriteMessage(severity + ": " + msg);
            }
            public void Flush()
            {
                this.pipe.Flush();
            }
        }
    }
}
