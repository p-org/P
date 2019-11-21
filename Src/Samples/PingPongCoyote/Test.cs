using Microsoft.Coyote;
using Microsoft.Coyote.TestingServices;
using System;
using System.Linq;

namespace pingpong
{
    public class _TestRegression
    {
        public static void Main(string[] args)
        {
            // Optional: increases verbosity level to see the Coyote runtime log.
            Configuration configuration = Configuration.Create();
            configuration.SchedulingIterations = 10;
            ITestingEngine engine = TestingEngineFactory.CreateBugFindingEngine(configuration, DefaultImpl.Execute);
            engine.Run();
            string bug = engine.TestReport.BugReports.FirstOrDefault();
            if (bug != null)
            {
                Console.WriteLine(bug);
            }
        }
    }
}