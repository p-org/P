// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using PChecker.Coverage;
using PChecker.SystematicTesting;

namespace PChecker.Utilities
{
    /// <summary>
    /// The Coyote testing reporter.
    /// </summary>
    internal static class Reporter
    {
        /// <summary>
        /// Emits the testing coverage report.
        /// </summary>
        /// <param name="report">TestReport</param>
        /// <param name="processId">Optional process id that produced the report</param>
        /// <param name="isDebug">Is a debug report</param>
        internal static void EmitTestingCoverageReport(TestReport report, uint? processId = null, bool isDebug = false)
        {
            var file = Path.GetFileNameWithoutExtension(report.CheckerConfiguration.AssemblyToBeAnalyzed);
            if (isDebug && processId != null)
            {
                file += "_" + processId;
            }

            var directory = report.CheckerConfiguration.OutputDirectory;
            if (isDebug)
            {
                directory += $"Debug{Path.DirectorySeparatorChar}";
                Directory.CreateDirectory(directory);
            }

            EmitTestingCoverageOutputFiles(report, directory, file);
        }

        /// <summary>
        /// Returns (and creates if it does not exist) the output directory with an optional suffix.
        /// </summary>
        internal static string GetOutputDirectory(string userOutputDir, string assemblyPath, string suffix = "", bool createDir = true)
        {
            string directoryPath;

            if (!string.IsNullOrEmpty(userOutputDir))
            {
                directoryPath = userOutputDir + Path.DirectorySeparatorChar;
            }
            else
            {
                var subpath = Path.GetDirectoryName(assemblyPath);
                if (subpath.Length == 0)
                {
                    subpath = ".";
                }

                directoryPath = subpath +
                                Path.DirectorySeparatorChar + "Output" + Path.DirectorySeparatorChar +
                                Path.GetFileName(assemblyPath) + Path.DirectorySeparatorChar;
            }

            if (suffix.Length > 0)
            {
                directoryPath += suffix + Path.DirectorySeparatorChar;
            }

            if (createDir)
            {
                Directory.CreateDirectory(directoryPath);
            }

            return directoryPath;
        }

        /// <summary>
        /// Emits all the testing coverage related output files.
        /// </summary>
        /// <param name="report">TestReport containing CoverageInfo</param>
        /// <param name="directory">Output directory name, unique for this run</param>
        /// <param name="file">Output file name</param>
        private static void EmitTestingCoverageOutputFiles(TestReport report, string directory, string file)
        {
            var codeCoverageReporter = new ActivityCoverageReporter(report.CoverageInfo);
            var filePath = $"{directory}{file}";

            var graphFilePath = $"{filePath}.dgml";
            Console.WriteLine($"..... Writing {graphFilePath}");
            codeCoverageReporter.EmitVisualizationGraph(graphFilePath);

            var coverageFilePath = $"{filePath}.coverage.txt";
            Console.WriteLine($"..... Writing {coverageFilePath}");
            codeCoverageReporter.EmitCoverageReport(coverageFilePath);

            var serFilePath = $"{filePath}.sci";
            Console.WriteLine($"..... Writing {serFilePath}");
            report.CoverageInfo.Save(serFilePath);
        }
    }
}