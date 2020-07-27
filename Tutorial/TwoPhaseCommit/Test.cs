using System;
using Microsoft.Coyote;
using Microsoft.Coyote.SystematicTesting;
using System.Linq;

namespace TwoPhaseCommit
{
    public class _TestRegression
    {
        public static void Main(string[] args)
        {
            // Optional: increases verbosity level to see the Coyote runtime log.
            var configuration = Configuration.Create().WithTestingIterations(10);
            var engine = TestingEngine.Create(configuration, Test0.Execute);
            engine.Run();
            var bug = engine.TestReport.BugReports.FirstOrDefault();
            if (bug != null)
                Console.WriteLine(bug);
        }
    }
}