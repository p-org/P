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
        Dictionary<string, Compiler> compilerFree = new Dictionary<string, Compiler>();
        Dictionary<string, Compiler> compilerBusy = new Dictionary<string, Compiler>();

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
                if (msg == null)
                {
                    // pipe is closing.
                }
                else if (msg == CompilerServiceClient.CompilerLockMessage)
                {
                    HandleLockMessage(pipe);
                }
                else if (msg.StartsWith(CompilerServiceClient.CompilerFreeMessage))
                {
                    // this session is done, double handshake.
                    HandleFreeMessage(pipe, msg);
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

        private void HandleLockMessage(NamedPipe pipe)
        {
            var pair = GetFreeCompiler();
            // send the id to the client for use in subsequent jobs.
            pipe.WriteMessage(pair.Item1);
        }

        private void HandleFreeMessage(NamedPipe pipe, string msg)
        {
            string[] parts = msg.Split(':');
            if (parts.Length == 2 && compilerBusy.ContainsKey(parts[1]))
            {
                string id = parts[1];
                string result = "";
                lock (compilerBusy)
                {
                    if (compilerBusy.ContainsKey(id))
                    {
                        Compiler c = compilerBusy[id];
                        compilerBusy.Remove(id);
                        compilerFree[id] = c;
                        result = CompilerServiceClient.JobFinishedMessage;
                    }
                    else
                    {
                        result = string.Format("Error: '<free>guid', specified compiler not found with id='{0}'", id);
                    }
                }
                pipe.WriteMessage(result);
            }
            else
            {
                pipe.WriteMessage("Protocol error: expecting '<free>guid', to free the specified compiler");
            }
        }

        private Tuple<string,Compiler> GetFreeCompiler()
        {
            Tuple<string, Compiler> result = null;
            while (result == null)
            {
                lock (compilerlock)
                {
                    if (compilerFree.Count > 0)
                    {
                        var pair = compilerFree.First();
                        compilerFree.Remove(pair.Key);
                        compilerBusy[pair.Key] = pair.Value;
                        result = new Tuple<string, Compiler>(pair.Key, pair.Value);
                    }
                    else if (compilerBusy.Count < maxCompilers)
                    {
                        var compiler = new Compiler(false);
                        var id = Guid.NewGuid().ToString();
                        compilerBusy[id] = compiler;
                        result = new Tuple<string, Compiler>(id, compiler);
                    }
                }
                if (result == null)
                {
                    // wait for free compiler
                    Thread.Sleep(1000);
                }
            }
            Interlocked.Increment(ref busyCount);
            return result;
        }

        private void FreeCompiler(Compiler compiler, string id)
        {
            Interlocked.Decrement(ref busyCount);
            lock (compilerlock)
            {
                compilerBusy.Remove(id);
                compilerFree[id] = compiler;
            }
        }

        private Compiler RebuildCompiler(Compiler compiler, string id)
        {
            lock (compilerlock)
            {
                Compiler newCompiler = new Compiler(false);                
                compilerBusy[id] = newCompiler;
                return newCompiler;
            }
        }

        private static void DebugWriteLine(string msg)
        {
            Debug.WriteLine("(" + System.Threading.Thread.CurrentThread.ManagedThreadId + ") " + msg);
        }

        private bool ProcessJob(string msg, ICompilerOutput output)
        {
            bool result = false;
            Compiler compiler = null;
            CommandLineOptions options = null;
            bool freeCompiler = false;
            string compilerId = null;

            try
            {
                XmlSerializer s = new XmlSerializer(typeof(CommandLineOptions));
                options = (CommandLineOptions)s.Deserialize(new StringReader(msg));

                bool retry = true;
                bool masterCreated = false;

                if (options.compilerId == null)
                {
                    var pair = GetFreeCompiler();
                    compiler = pair.Item2;
                    compilerId = pair.Item1 ;
                    freeCompiler = true;
                }
                else
                {
                    // the givein compilerId belongs to our client, so we can safely use it.
                    lock (compilerlock)
                    {
                        compilerId = options.compilerId;
                        compiler = compilerBusy[options.compilerId];
                    }
                }
                
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

                        if (options.isLinkerPhase)
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
                            compiler = RebuildCompiler(compiler, compilerId);
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
                if (freeCompiler)
                {
                    if (compiler != null)
                    {
                        FreeCompiler(compiler, compilerId);
                    }
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
