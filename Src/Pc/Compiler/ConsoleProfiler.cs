using System;
using System.Diagnostics;
using Microsoft.Formula.API;

namespace Microsoft.Pc
{
    public class ConsoleProfiler : IProfiler
    {
        ICompilerOutput Log;

        public ConsoleProfiler(ICompilerOutput log)
        {
            this.Log = log;
        }

        public IDisposable Start(string operation, string message)
        {
            return new ConsoleProfileWatcher(Log, operation, message);
        }

        class ConsoleProfileWatcher : IDisposable
        {
            Stopwatch watch = new Stopwatch();
            string operation;
            string message;
            ICompilerOutput Log;

            public ConsoleProfileWatcher(ICompilerOutput log, string operation, string message)
            {
                this.Log = log;
                this.operation = operation;
                this.message = message;
                watch.Start();
            }
            public void Dispose()
            {
                watch.Stop();
                string msg = string.Format("{0}: {1} {2} {3}", DateTime.Now.ToShortTimeString(), watch.Elapsed.ToString(), operation, message);
                Log.WriteMessage(msg, SeverityKind.Info);
            }
        }

    }
}