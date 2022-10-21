// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
#endif
using System.IO;
#if NETFRAMEWORK
using System.Linq;

using Microsoft.Coyote.IO;
using Microsoft.Coyote.SystematicTesting.Utilities;
#endif

namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// Instruments a binary for code coverage.
    /// </summary>
    internal static class CodeCoverageInstrumentation
    {
        internal static string OutputDirectory = string.Empty;
#if NETFRAMEWORK
        internal static List<string> InstrumentedAssemblyNames = new List<string>();

        internal static void Instrument(Configuration configuration)
        {
            // HashSet in case of duplicate file specifications.
            var assemblyNames = new HashSet<string>(DependencyGraph.GetDependenciesToCoyote(configuration)
                                                    .Union(GetAdditionalAssemblies(configuration)));
            InstrumentedAssemblyNames.Clear();

            foreach (var assemblyName in assemblyNames)
            {
                if (!Instrument(assemblyName))
                {
                    Restore();
                    Environment.Exit(1);
                }

                InstrumentedAssemblyNames.Add(assemblyName);
            }
        }

        private static IEnumerable<string> GetAdditionalAssemblies(Configuration configuration)
        {
            var testAssemblyPath = Path.GetDirectoryName(configuration.AssemblyToBeAnalyzed);
            if (testAssemblyPath.Length == 0)
            {
                testAssemblyPath = ".";
            }

            IEnumerable<string> resolveFileSpec(string spec)
            {
                // If not rooted, the file path is relative to testAssemblyPath.
                var gdn = Path.GetDirectoryName(spec);
                var dir = Path.IsPathRooted(gdn) ? gdn : Path.Combine(testAssemblyPath, gdn);
                var fullDir = Path.GetFullPath(dir);
                var fileSpec = Path.GetFileName(spec);
                var fullNames = Directory.GetFiles(fullDir, fileSpec);
                foreach (var fullName in fullNames)
                {
                    if (!File.Exists(fullName))
                    {
                        Error.ReportAndExit($"Cannot find specified file for code-coverage instrumentation: '{fullName}'.");
                    }

                    yield return fullName;
                }
            }

            IEnumerable<string> resolveAdditionalFiles(KeyValuePair<string, bool> kvp)
            {
                if (!kvp.Value)
                {
                    foreach (var file in resolveFileSpec(kvp.Key))
                    {
                        yield return file;
                    }

                    yield break;
                }

                var dir = Path.GetDirectoryName(kvp.Key);
                var fullDir = Path.GetFullPath(dir.Length > 0 ? dir : testAssemblyPath);
                var listFile = Path.Combine(fullDir, Path.GetFileName(kvp.Key));
                if (!File.Exists(listFile))
                {
                    Error.ReportAndExit($"Cannot find specified list file for code-coverage instrumentation: '{kvp.Key}'.");
                }

                foreach (var spec in File.ReadAllLines(listFile).Where(line => line.Length > 0).Select(line => line.Trim())
                                                                .Where(line => !line.StartsWith("//")))
                {
                    foreach (var file in resolveFileSpec(spec))
                    {
                        yield return file;
                    }
                }
            }

            // Note: Resolution has been deferred to here so that all empty path qualifiations, including to the list
            // file, will resolve to testAssemblyPath (as config coverage parameters may be specified before /test).
            // Return .ToList() to force iteration and return errors before we start instrumenting.
            return configuration.AdditionalCodeCoverageAssemblies.SelectMany(kvp => resolveAdditionalFiles(kvp)).ToList();
        }

        private static bool Instrument(string assemblyName)
        {
            int exitCode;
            string error;
            Console.WriteLine($"Instrumenting {assemblyName}");

            using (var instrProc = new Process())
            {
                instrProc.StartInfo.FileName = GetToolPath("VSInstrToolPath", "VSInstr");
                instrProc.StartInfo.Arguments = $"/coverage {assemblyName}";
                instrProc.StartInfo.UseShellExecute = false;
                instrProc.StartInfo.RedirectStandardOutput = true;
                instrProc.StartInfo.RedirectStandardError = true;
                instrProc.Start();

                error = instrProc.StandardError.ReadToEnd();

                instrProc.WaitForExit();
                exitCode = instrProc.ExitCode;
            }

            // Exit code 0 means that the file was instrumented successfully.
            // Exit code 4 means that the file was already instrumented.
            if (exitCode != 0 && exitCode != 4)
            {
                Error.Report($"[Coyote] 'VSInstr' failed to instrument '{assemblyName}'.");
                IO.Debug.WriteLine(error);
                return false;
            }

            return true;
        }

        internal static void Restore()
        {
            try
            {
                foreach (var assemblyName in InstrumentedAssemblyNames)
                {
                    Restore(assemblyName);
                }
            }
            finally
            {
                OutputDirectory = string.Empty;
                InstrumentedAssemblyNames.Clear();
            }
        }

        internal static void Restore(string assemblyName)
        {
            // VSInstr creates a backup of the uninstrumented .exe with the suffix ".exe.orig", and
            // writes an instrumented .pdb with the suffix ".instr.pdb". We must restore the uninstrumented
            // .exe after the coverage run, and viewing the coverage file requires the instrumented .exe,
            // so move the instrumented files to the output directory and restore the uninstrumented .exe.
            var origExe = $"{assemblyName}.orig";
            var origDir = Path.GetDirectoryName(assemblyName);
            if (origDir.Length == 0)
            {
                origDir = ".";
            }

            origDir += Path.DirectorySeparatorChar;
            var instrExe = $"{OutputDirectory}{Path.GetFileName(assemblyName)}";
            var instrPdb = $"{Path.GetFileNameWithoutExtension(assemblyName)}.instr.pdb";
            try
            {
                if (!string.IsNullOrEmpty(OutputDirectory) && File.Exists(origExe))
                {
                    if (TestingProcessScheduler.IsProcessCanceled)
                    {
                        File.Delete(assemblyName);
                        File.Delete(instrPdb);
                        Directory.Delete(OutputDirectory, true);
                    }
                    else
                    {
                        File.Move(assemblyName, instrExe);
                        File.Move($"{origDir}{instrPdb}", $"{OutputDirectory}{instrPdb}");
                    }

                    File.Move(origExe, assemblyName);
                }
            }
            catch (IOException ex)
            {
                // Don't exit here as we're already shutting down the app, and we may have more assemblies to restore.
                Error.Report($"[Coyote] Failed to restore non-instrumented '{assemblyName}': {ex.Message}.");
            }
        }

        /// <summary>
        /// Returns the tool path to the code coverage instrumentor.
        /// </summary>
        /// <param name="settingName">The name of the setting; also used to query the environment variables.</param>
        /// <param name="toolName">The name of the tool; used in messages only.</param>
        internal static string GetToolPath(string settingName, string toolName)
        {
            string toolPath = string.Empty;
            try
            {
                toolPath = Environment.GetEnvironmentVariable(settingName);
                if (string.IsNullOrEmpty(toolPath))
                {
                    toolPath = ConfigurationManager.AppSettings[settingName];
                }
                else
                {
                    Console.WriteLine($"{toolName} overriding app settings path with environment variable");
                }
            }
            catch (ConfigurationErrorsException)
            {
                Error.ReportAndExit($"[Coyote] required '{settingName}' value is not set in configuration file.");
            }

            toolPath = toolPath.Replace("$(DevEnvDir)", Environment.GetEnvironmentVariable("DevEnvDir"));

            if (!File.Exists(toolPath))
            {
                Error.ReportAndExit($"[Coyote] '{toolName}' tool '{toolPath}' not found.");
            }

            return toolPath;
        }
#endif

        /// <summary>
        /// Set the <see cref="OutputDirectory"/> to either the user-specified <see cref="Configuration.OutputFilePath"/>
        /// or to a unique output directory name in the same directory as <see cref="Configuration.AssemblyToBeAnalyzed"/>
        /// and starting with its name.
        /// </summary>
        internal static void SetOutputDirectory(Configuration configuration, bool makeHistory)
        {
            if (OutputDirectory.Length > 0)
            {
                return;
            }

            // Do not create the output directory yet if we have to scroll back the history first.
            OutputDirectory = Reporter.GetOutputDirectory(configuration.OutputFilePath, configuration.AssemblyToBeAnalyzed,
                "CoyoteOutput", createDir: !makeHistory);
            if (!makeHistory)
            {
                return;
            }

            // The MaxHistory previous results are kept under the directory name with a suffix scrolling back from 0 to 9 (oldest).
            const int MaxHistory = 10;
            string makeHistoryDirName(int history) => OutputDirectory.Substring(0, OutputDirectory.Length - 1) + history;
            var older = makeHistoryDirName(MaxHistory - 1);

            if (Directory.Exists(older))
            {
                Directory.Delete(older, true);
            }

            for (var history = MaxHistory - 2; history >= 0; --history)
            {
                var newer = makeHistoryDirName(history);
                if (Directory.Exists(newer))
                {
                    Directory.Move(newer, older);
                }

                older = newer;
            }

            if (Directory.Exists(OutputDirectory))
            {
                Directory.Move(OutputDirectory, older);
            }

            // Now create the new directory.
            Directory.CreateDirectory(OutputDirectory);
        }
    }
}
