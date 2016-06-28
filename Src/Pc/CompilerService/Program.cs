using Microsoft.Formula.API;
using Microsoft.Pc;
using System;
using System.Collections.Generic;
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
        NamedPipe pipe;
        object compilerlock = new object();
        Compiler master;

        static void Main(string[] args)
        {
            System.Diagnostics.Contracts.Contract.ContractFailed += OnContractFailed;
            Program p = new Program();
            try
            {
                p.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            Console.WriteLine("Press ENTER to continue...");
            Console.ReadLine();
        }

        private static void OnContractFailed(object sender, System.Diagnostics.Contracts.ContractFailedEventArgs e)
        {
            throw new Exception(e.Message);
        }

        void Run()
        {
            Console.WriteLine("Starting compiler service, listening to named pipe");

            // start the server.
            pipe = new NamedPipe(ServerPipeName, true);
            pipe.MessageArrived += OnMessageArrived;
            if (!pipe.Connect())
            {
                Console.WriteLine("Hmmm, is it already running?");
                return;
            }
            doMoreWork = true;

            // stay awake waiting for work
            while (doMoreWork || busyCount > 0)
            {
                doMoreWork = false;
                Thread.Sleep(60 * 60 * 1000); // timeout after 1 hour
            }

            Console.WriteLine("Compiler service terminating due to lack of work");
        }

        private void OnMessageArrived(object sender, PipeMessageEventArgs e)
        {
            doMoreWork = true;
            Task.Run(new Action(() =>
            {
                ProcessJob(e.Message);
            }));

            pipe.Disconnect(); // go back to waiting for next connection
        }

        private void ProcessJob(string msg)
        {
            Interlocked.Increment(ref busyCount);

            XmlSerializer s = new XmlSerializer(typeof(CommandLineOptions));
            CommandLineOptions options = (CommandLineOptions)s.Deserialize(new StringReader(msg));

            NamedPipe clientPipe = null;
            if (!string.IsNullOrEmpty(options.pipeName))
            {
                clientPipe = new NamedPipe(options.pipeName, false);
                clientPipe.Connect();
            }

            var output = new SerializedOutput(clientPipe);
            if (master == null)
            {
                lock(compilerlock)
                {
                    output.WriteMessage("Generating P compiler", SeverityKind.Info);
                    master = new Compiler(false);
                }
            }

            // share the compiled P program across compiler instances.
            Compiler compiler = new Compiler(master);
            compiler.Options = options;
            compiler.Log = new SerializedOutput(clientPipe);
            bool result = false;
            try
            {
                result = compiler.Compile(options.inputFileName);
            }
            catch (Exception ex)
            {
                // assert failed?
                output.WriteMessage("Compile failed: " + ex.Message, SeverityKind.Error);
            }
            compiler.ResetEnv();
            compiler.Log.WriteMessage("finished:" + result, SeverityKind.Info);

            Thread.Sleep(1000);
            clientPipe.Close();
            Interlocked.Decrement(ref busyCount);
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
                this.pipe.WriteMessage(severity + ": " + msg);
            }
        }
    }
}
