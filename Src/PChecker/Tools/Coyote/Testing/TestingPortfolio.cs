// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// A portfolio of systematic testing strategies.
    /// </summary>
    internal static class TestingPortfolio
    {
        /// <summary>
        /// Configures the systematic testing strategy for the current testing process.
        /// </summary>
        internal static void ConfigureStrategyForCurrentProcess(Configuration configuration)
        {
            // random, fairpct[1], probabilistic[1], fairpct[5], probabilistic[2], fairpct[10], etc.
            if (configuration.TestingProcessId == 0)
            {
                configuration.SchedulingStrategy = "random";
            }
            else if (configuration.TestingProcessId % 2 == 0)
            {
                configuration.SchedulingStrategy = "probabilistic";
                configuration.StrategyBound = (int)(configuration.TestingProcessId / 2);
            }
            else if (configuration.TestingProcessId == 1)
            {
                configuration.SchedulingStrategy = "fairpct";
                configuration.StrategyBound = 1;
            }
            else
            {
                configuration.SchedulingStrategy = "fairpct";
                configuration.StrategyBound = 5 * (int)((configuration.TestingProcessId + 1) / 2);
            }
        }
    }
}
