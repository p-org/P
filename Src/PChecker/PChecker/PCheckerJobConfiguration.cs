using System;
using System.IO;
using System.Linq;

namespace Plang.PChecker
{
    /// <summary>
    /// Implements the configuration of the P Checker Job
    /// </summary>
    public class PCheckerJobConfiguration
    {
        /// <summary>
        /// Path to the DLL of the program under test
        /// </summary>
        public string PathToTestDll { get; }

        /// <summary>
        /// Output directory path where the generated output is dumped
        /// </summary>
        public string OutputDirectoryPath { get; }

        /// <summary>
        /// Number of parallel instances of the checker
        /// </summary>
        public int Parallelism { get; }

        /// <summary>
        /// Test case to run the PChecker on
        /// </summary>
        public string TestCase { get; }

        /// <summary>
        /// Maximum number of schedules to be explored
        /// </summary>
        public int MaxScheduleIterations { get; }

        /// <summary>
        /// Max steps or depth per execution explored
        /// </summary>
        public int MaxStepsPerExecution { get; }

        /// <summary>
        /// Generate an error after the maximum steps
        /// </summary>
        public int ErrorOutAtMaxSteps { get; }

        /// <summary>
        /// Is verbose ON
        /// </summary>
        public bool IsVerbose { get; }

        /// <summary>
        /// Is test mode or replay mode
        /// </summary>
        public bool IsReplay { get; }

        /// <summary>
        /// Error schedule to be replayed
        /// </summary>
        public string ErrorSchedule { get; }

        public PCheckerJobConfiguration()
        {
            // initialize with the default value of each parameter
            PathToTestDll = "";
            OutputDirectoryPath = GetNextOutputDirectoryName("PCheckerOutput");
            Parallelism = Environment.ProcessorCount;
            TestCase = "DefaultImpl.Execute";
            MaxScheduleIterations = 10000;
            MaxStepsPerExecution = 5000;
            ErrorOutAtMaxSteps = 10000;
            IsVerbose = false;
            IsReplay = false;
            ErrorSchedule = "";
        }

        private string GetNextOutputDirectoryName(string v)
        {
            string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), v);
            string folderName = Directory.Exists(directoryPath) ? Directory.GetDirectories(directoryPath).Count().ToString() : "0";
            return Path.Combine(directoryPath, folderName);
        }
    }
}