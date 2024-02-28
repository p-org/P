// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;

namespace PChecker.Utilities
{
    /// <summary>
    /// The Coyote profiler.
    /// </summary>
    public class Profiler
    {
        private Stopwatch StopWatch;

        /// <summary>
        /// Starts measuring execution time.
        /// </summary>
        public void StartMeasuringExecutionTime()
        {
            StopWatch = new Stopwatch();
            StopWatch.Start();
        }

        /// <summary>
        /// Stops measuring execution time.
        /// </summary>
        public void StopMeasuringExecutionTime()
        {
            if (StopWatch != null)
            {
                StopWatch.Stop();
            }
        }

        /// <summary>
        /// Returns profilling results.
        /// </summary>
        public double Results() =>
            StopWatch != null ? StopWatch.Elapsed.TotalSeconds : 0;
    }
}