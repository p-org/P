using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace PlangBuild
{
    class PlangBuilder
    {
        private const string MsBuildCommand = "\"{0}\" /p:Configuration={1} /p:Platform={2}";
        private const string ConfigDebug = "Debug";
        private const string ConfigRelease = "Release";
        private const string PlatformX86 = "x86";
        private const string PlatformX64 = "x64";
        private const string PlatformAny = "AnyCPU";

        /// <summary>
        /// Project is described by:
        /// (1) true if can only be built with 32-bit version of MsBuild (e.g. VS extensions)
        /// (2) the relative location of the project file
        /// (3) the platform on which it should be built
        /// </summary>
        private static readonly Tuple<bool, string, string>[] Projects = new Tuple<bool, string, string>[]
        {
            new Tuple<bool, string, string>(false, "..\\..\\..\\..\\..\\Src\\Compilers\\P2Formula\\P2Formula.csproj", PlatformAny),
            new Tuple<bool, string, string>(false, "..\\..\\..\\..\\..\\Src\\Compilers\\PCompiler\\PCompiler.csproj", PlatformAny),
        };
        
        private static readonly Tuple<string, string>[] DebugMoveMap = new Tuple<string, string>[]
        {
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Formula\\Core.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\Core.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Formula\\Microsoft.Z3.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\Microsoft.Z3.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Formula\\libz3.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\libz3.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\zc.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\zc.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\Microsoft.Comega.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\Microsoft.Comega.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\Microsoft.Comega.Runtime.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\Microsoft.Comega.Runtime.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\Microsoft.Zing.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\Microsoft.Zing.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\Microsoft.Zing.Runtime.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\Microsoft.Zing.Runtime.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\System.Compiler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\System.Compiler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\System.Compiler.Framework.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\System.Compiler.Framework.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\System.Compiler.Runtime.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\System.Compiler.Runtime.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\ZingDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\ZingDelayingScheduler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\ZingOptions.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\ZingOptions.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\ZingPlugin.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\ZingPlugin.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\Zinger.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\Zinger.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\FrontierTree.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\FrontierTree.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\ParallelExplorer.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\ParallelExplorer.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\ZingDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\ZingDelayingScheduler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\ZingStateSpaceTraversal.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\ZingStateSpaceTraversal.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\RandomDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\RandomDelayingScheduler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\RoundRobinScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\RoundRobinScheduler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\RunToCompletionDBSched.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\RunToCompletionDBSched.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\StateCoveragePlugin.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\StateCoveragePlugin.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\StateVisitCount.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\StateVisitCount.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Formula\\Domains\\PData.4ml", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\PData.4ml"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Formula\\Domains\\ZingData.4ml", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\ZingData.4ml"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Formula\\Domains\\CData.4ml", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\CData.4ml"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Compilers\\P2Formula\\bin\\Debug\\P2Formula.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\P2Formula.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Compilers\\PCompiler\\bin\\Debug\\PCompiler.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\PCompiler.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Compilers\\PCompiler\\bin\\Debug\\CParser.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\CParser.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Compilers\\PCompiler\\bin\\Debug\\ZingParser.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\ZingParser.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Compilers\\PCompiler\\bin\\Debug\\Domains.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\Domains.dll")
        };

        private static readonly Tuple<string, string>[] ReleaseMoveMap = new Tuple<string, string>[]
        {
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Formula\\Core.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\Core.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Formula\\Microsoft.Z3.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\Microsoft.Z3.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Formula\\libz3.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\libz3.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\zc.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\zc.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\Microsoft.Comega.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\Microsoft.Comega.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\Microsoft.Comega.Runtime.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\Microsoft.Comega.Runtime.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\Microsoft.Zing.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\Microsoft.Zing.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\Microsoft.Zing.Runtime.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\Microsoft.Zing.Runtime.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\System.Compiler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\System.Compiler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\System.Compiler.Framework.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\System.Compiler.Framework.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\System.Compiler.Runtime.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\System.Compiler.Runtime.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\ZingDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\ZingDelayingScheduler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\ZingOptions.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\ZingOptions.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\ZingPlugin.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\ZingPlugin.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\Zinger.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\Zinger.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\FrontierTree.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\FrontierTree.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\ParallelExplorer.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\ParallelExplorer.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\ZingDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\ZingDelayingScheduler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\ZingStateSpaceTraversal.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\ZingStateSpaceTraversal.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\RandomDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\RandomDelayingScheduler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\RoundRobinScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\RoundRobinScheduler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\RunToCompletionDBSched.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\RunToCompletionDBSched.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\StateCoveragePlugin.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\StateCoveragePlugin.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\StateVisitCount.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\StateVisitCount.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Formula\\Domains\\PData.4ml", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\PData.4ml"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Formula\\Domains\\ZingData.4ml", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\ZingData.4ml"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Formula\\Domains\\CData.4ml", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\CData.4ml"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Compilers\\P2Formula\\bin\\Release\\P2Formula.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\P2Formula.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Compilers\\PCompiler\\bin\\Release\\PCompiler.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\PCompiler.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Compilers\\PCompiler\\bin\\Release\\CParser.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\CParser.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Compilers\\PCompiler\\bin\\Release\\ZingParser.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\ZingParser.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Compilers\\PCompiler\\bin\\Release\\Domains.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\Domains.dll")
        };

        public static bool Build(bool isBldDebug)
        {
            var result = true;
            FileInfo msbuild, msbuild32 = null;
            result = SourceDownloader.GetMsbuild(out msbuild) && 
                     SourceDownloader.GetMsbuild(out msbuild32, true) &&
                     result;
            if (!result)
            {
                Program.WriteError("Could not build Plang, unable to find msbuild");
                return false;
            }

            var config = isBldDebug ? ConfigDebug : ConfigRelease;
            foreach (var proj in Projects)
            {
                Program.WriteInfo("Building {0}: Config = {1}, Platform = {2}", proj.Item2, config, proj.Item3);
                result = BuildCSProj(proj.Item1 ? msbuild32 : msbuild, proj.Item2, config, proj.Item3) && result;
            }

            if (!result)
            {
                return false;
            }

            result = DoMove(isBldDebug ? DebugMoveMap : ReleaseMoveMap) && result;

            var path = isBldDebug ? "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime" : "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime";
            var runningLoc = new FileInfo(Assembly.GetExecutingAssembly().Location);
            var inDir = new DirectoryInfo(Path.Combine(runningLoc.Directory.FullName, "..\\..\\..\\..\\..\\Runtime"));
            var outDir = new DirectoryInfo(Path.Combine(runningLoc.Directory.FullName, path));
            if (!outDir.Exists)
            {
                outDir.Create();
            }
            foreach (var subInDir in inDir.GetDirectories())
            {
                foreach (var inFile in subInDir.GetFiles())
                {
                    var outFile = new FileInfo(Path.Combine(runningLoc.Directory.FullName, path, inFile.Name));
                    inFile.CopyTo(outFile.FullName, true);
                }
            }
            return result;
        }

        private static bool DoMove(Tuple<string, string>[] moveMap)
        {
            bool result = true;
            try
            {
                var runningLoc = new FileInfo(Assembly.GetExecutingAssembly().Location);
                foreach (var t in moveMap)
                {
                    var inFile = new FileInfo(Path.Combine(runningLoc.Directory.FullName, t.Item1));
                    if (!inFile.Exists)
                    {
                        result = false;
                        Program.WriteError("Could not find output file {0}", inFile.FullName);
                        continue;
                    }

                    var outFile = new FileInfo(Path.Combine(runningLoc.Directory.FullName, t.Item2));
                    if (!outFile.Directory.Exists)
                    {
                        outFile.Directory.Create();
                    }

                    inFile.CopyTo(outFile.FullName, true);
                    Program.WriteInfo("Moved output {0} --> {1}", inFile.FullName, outFile.FullName);
                }

                return result;
            }
            catch (Exception e)
            {
                Program.WriteError("Unable to move output files - {0}", e.Message);
                return false;
            }
        }

        private static bool BuildCSProj(FileInfo msbuild, string projFileName, string config, string platform)
        {
            try
            {
                FileInfo projFile;
                if (!SourceDownloader.GetBuildRelFile(projFileName, out projFile) || !projFile.Exists)
                {
                    Program.WriteError("Could not find project file {0}", projFileName);
                }

                var psi = new ProcessStartInfo();
                psi.UseShellExecute = false;
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;
                psi.WorkingDirectory = projFile.Directory.FullName;
                psi.FileName = msbuild.FullName;
                psi.Arguments = string.Format(MsBuildCommand, projFile.Name, config, platform);
                psi.CreateNoWindow = true;

                var process = new Process();
                process.StartInfo = psi;
                process.OutputDataReceived += OutputReceived;
                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                process.WaitForExit();

                Program.WriteInfo("EXIT: {0}", process.ExitCode);
                return process.ExitCode == 0;
            }
            catch (Exception e)
            {
                Program.WriteError("Failed to build project {0} - {1}", projFileName, e.Message);
                return false;
            }
        }

        private static void OutputReceived(
            object sender,
            DataReceivedEventArgs e)
        {
            Console.WriteLine("OUT: {0}", e.Data);
        }
    }
}
