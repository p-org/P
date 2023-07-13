// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace PChecker.Testing
{
    /// <summary>
    /// The Coyote testing process factory.
    /// </summary>
    internal static class TestingProcessFactory
    {
        /// <summary>
        /// Creates a new testing process.
        /// </summary>
        public static Process Create(uint id, CheckerConfiguration checkerConfiguration)
        {
            var assembly = Assembly.GetExecutingAssembly().Location;
            Console.WriteLine("Launching " + assembly);
#if NETFRAMEWORK
            ProcessStartInfo startInfo = new ProcessStartInfo(assembly,
                CreateArgumentsFromConfiguration(id, checkerConfiguration));
#else
            var startInfo = new ProcessStartInfo("dotnet", assembly + " " +
                                                           CreateArgumentsFromConfiguration(id, checkerConfiguration));
#endif
            startInfo.UseShellExecute = false;

            var process = new Process();
            process.StartInfo = startInfo;

            return process;
        }

        /// <summary>
        /// Creates arguments from the specified checkerConfiguration.
        /// </summary>
        internal static string CreateArgumentsFromConfiguration(uint id, CheckerConfiguration checkerConfiguration)
        {
            var arguments = new StringBuilder();

            arguments.Append($"test {checkerConfiguration.AssemblyToBeAnalyzed} ");

            if (checkerConfiguration.EnableDebugging)
            {
                arguments.Append("--debug ");
            }

            if (!string.IsNullOrEmpty(checkerConfiguration.TestCaseName))
            {
                arguments.Append($"--testcase {checkerConfiguration.TestCaseName} ");
            }

            arguments.Append($"--iterations {checkerConfiguration.TestingIterations} ");
            arguments.Append($"--timeout {checkerConfiguration.Timeout} ");

            if (checkerConfiguration.UserExplicitlySetMaxFairSchedulingSteps)
            {
                arguments.Append($"--max-steps {checkerConfiguration.MaxUnfairSchedulingSteps} " +
                    $"{checkerConfiguration.MaxFairSchedulingSteps} ");
            }
            else
            {
                arguments.Append($"--max-steps {checkerConfiguration.MaxUnfairSchedulingSteps} ");
            }

            if (checkerConfiguration.SchedulingStrategy is "pct" ||
                checkerConfiguration.SchedulingStrategy is "fairpct" ||
                checkerConfiguration.SchedulingStrategy is "probabilistic" ||
                checkerConfiguration.SchedulingStrategy is "rl")
            {
                arguments.Append($"--sch-{checkerConfiguration.SchedulingStrategy} {checkerConfiguration.StrategyBound} ");
            }
            else if (checkerConfiguration.SchedulingStrategy is "random" ||
                checkerConfiguration.SchedulingStrategy is "portfolio")
            {
                arguments.Append($"--sch-{checkerConfiguration.SchedulingStrategy} ");
            }

            if (checkerConfiguration.RandomGeneratorSeed.HasValue)
            {
                arguments.Append($"--seed {checkerConfiguration.RandomGeneratorSeed.Value} ");
            }

            if (checkerConfiguration.PerformFullExploration)
            {
                arguments.Append("--explore ");
            }

            if (checkerConfiguration.ReportCodeCoverage && checkerConfiguration.ReportActivityCoverage)
            {
                arguments.Append("--coverage ");
            }
            else if (checkerConfiguration.ReportCodeCoverage)
            {
                arguments.Append("--coverage code ");
            }
            else if (checkerConfiguration.ReportActivityCoverage)
            {
                arguments.Append("--coverage activity ");
            }

            if (checkerConfiguration.IsDgmlGraphEnabled)
            {
                arguments.Append("--graph ");
            }

            if (checkerConfiguration.IsXmlLogEnabled)
            {
                arguments.Append("--xml-trace ");
            }

            if (!string.IsNullOrEmpty(checkerConfiguration.CustomActorRuntimeLogType))
            {
                arguments.Append($"--actor-runtime-log {checkerConfiguration.CustomActorRuntimeLogType} ");
            }

            if (checkerConfiguration.OutputPath.Length > 0)
            {
                arguments.Append($"--outdir {checkerConfiguration.OutputPath} ");
            }

            arguments.Append("--run-as-parallel-testing-task ");

            return arguments.ToString();
        }
    }
}
