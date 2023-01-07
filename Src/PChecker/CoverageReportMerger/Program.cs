// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Coyote.Coverage;

namespace Microsoft.Coyote
{
    /// <summary>
    /// The coverage report merger.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Output file prefix.
        /// </summary>
        private static string OutputFilePrefix;

        private static void Main(string[] args)
        {
            if (!TryParseArgs(args, out List<CoverageInfo> inputFiles))
            {
                return;
            }

            if (inputFiles.Count == 0)
            {
                Console.WriteLine("Error: No input files provided");
                return;
            }

            var cinfo = new CoverageInfo();
            foreach (var other in inputFiles)
            {
                cinfo.Merge(other);
            }

            // Dump
            string name = OutputFilePrefix;
            string directoryPath = Environment.CurrentDirectory;

            var activityCoverageReporter = new ActivityCoverageReporter(cinfo);

            string[] graphFiles = Directory.GetFiles(directoryPath, name + "_*.dgml");
            string graphFilePath = Path.Combine(directoryPath, name + "_" + graphFiles.Length + ".dgml");

            Console.WriteLine($"... Writing {graphFilePath}");
            activityCoverageReporter.EmitVisualizationGraph(graphFilePath);

            string[] coverageFiles = Directory.GetFiles(directoryPath, name + "_*.coverage.txt");
            string coverageFilePath = Path.Combine(directoryPath, name + "_" + coverageFiles.Length + ".coverage.txt");

            Console.WriteLine($"... Writing {coverageFilePath}");
            activityCoverageReporter.EmitCoverageReport(coverageFilePath);
        }

        /// <summary>
        /// Parses the arguments.
        /// </summary>
        private static bool TryParseArgs(string[] args, out List<CoverageInfo> inputFiles)
        {
            inputFiles = new List<CoverageInfo>();
            OutputFilePrefix = "merged";

            if (args.Length == 0)
            {
                Console.WriteLine("Usage: CoyoteMergeCoverageReports.exe file1.sci file2.sci ... [/output:prefix]");
                return false;
            }

            foreach (var arg in args)
            {
                if (arg.StartsWith("/output:"))
                {
                    OutputFilePrefix = arg.Substring("/output:".Length);
                    continue;
                }
                else if (arg.StartsWith("/"))
                {
                    Console.WriteLine("Error: Unknown flag {0}", arg);
                    return false;
                }
                else
                {
                    // Check the suffix.
                    if (!arg.EndsWith(".sci"))
                    {
                        Console.WriteLine("Error: Only sci files accepted as input, got {0}", arg);
                        return false;
                    }

                    // Check if the file exists?
                    if (!File.Exists(arg))
                    {
                        Console.WriteLine("Error: File {0} not found", arg);
                        return false;
                    }

                    try
                    {
                        CoverageInfo info = CoverageInfo.Load(arg);
                        inputFiles.Add(info);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error: got exception while trying to read input objects: {0}", e.Message);
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
