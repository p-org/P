using System;
using Microsoft.PSharp;
using Microsoft.PSharp.TestingServices;
using System.Linq;

namespace pingpong
{
    public class _TestRegression
    {
        public static void Main(string[] args)
        {
            // Optional: increases verbosity level to see the P# runtime log.
            var configuration = Configuration.Create();
            configuration.SchedulingIterations = 10;
            var engine = TestingEngineFactory.CreateBugFindingEngine(configuration, DefaultImpl.Execute);
            engine.Run();
            var bug = engine.TestReport.BugReports.FirstOrDefault();
            if (bug != null)
                Console.WriteLine(bug);

            /*
            // Creates a new P# runtime instance, and passes an optional configuration.
            var runtime = PSharpRuntime.Create(configuration);

            // Executes the P# program.
            DefaultImpl.Execute(runtime);

            // The P# runtime executes asynchronously, so we wait
            // to not terminate the process.
            Console.WriteLine("Press Enter to terminate...");
            */
        }
    }
}