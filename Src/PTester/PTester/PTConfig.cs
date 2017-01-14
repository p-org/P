using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P.Explorer
{
    public class PTConfig
    {
        #region Fields
        public static string PInputFile = "";
        public static string TraceLogFile = "trace.txt";
        public static bool EnableErrorTraceOutput = false;
        public static int DegreeOfParallelism = 1;
        public static bool PrintStats = false;
        public static bool DoSampling = false;
        public static int MaxExecutionDepth = int.MaxValue;
        //public static PTExternalScheduler exScheduler = new PTExternalScheduler();
        public static bool DoLiveness = false;

        #endregion
    }
}
