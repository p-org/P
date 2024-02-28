// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PChecker.Testing
{
    /// <summary>
    /// A portfolio of systematic testing strategies.
    /// </summary>
    internal static class TestingPortfolio
    {
        /// <summary>
        /// Configures the systematic testing strategy for the current testing process.
        /// </summary>
        internal static void ConfigureStrategyForCurrentProcess(CheckerConfiguration checkerConfiguration)
        {
            // random, fairpct[1], probabilistic[1], fairpct[5], probabilistic[2], fairpct[10], etc.
            if (checkerConfiguration.TestingProcessId == 0)
            {
                checkerConfiguration.SchedulingStrategy = "random";
            }
            else if (checkerConfiguration.TestingProcessId % 2 == 0)
            {
                checkerConfiguration.SchedulingStrategy = "probabilistic";
                checkerConfiguration.StrategyBound = (int)(checkerConfiguration.TestingProcessId / 2);
            }
            else if (checkerConfiguration.TestingProcessId == 1)
            {
                checkerConfiguration.SchedulingStrategy = "fairpct";
                checkerConfiguration.StrategyBound = 1;
            }
            else
            {
                checkerConfiguration.SchedulingStrategy = "fairpct";
                checkerConfiguration.StrategyBound = 5 * (int)((checkerConfiguration.TestingProcessId + 1) / 2);
            }
        }
    }
}