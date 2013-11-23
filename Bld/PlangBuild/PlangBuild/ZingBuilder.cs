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
    class ZingBuilder
    {
        public static void PrintOutputs()
        {
            foreach (var o in outputs)
            {
                Program.WriteInfo("Zing dependency: {0}", o);
            }
        }

        private const string msbuildArgs = "\"{0}\" /p:Configuration=Release";
        private const string zingSln = "ZING.sln";

        private static readonly string[] outputs = new string[]
        {
            "..\\..\\..\\..\\..\\Ext\\Zing\\zc.exe",
            "..\\..\\..\\..\\..\\Ext\\Zing\\Microsoft.Comega.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\Microsoft.Comega.Runtime.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\Microsoft.Zing.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\Microsoft.Zing.Runtime.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\System.Compiler.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\System.Compiler.Framework.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\System.Compiler.Runtime.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\ZingDelayingScheduler.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\ZingOptions.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\ZingPlugin.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\Zinger.exe",
            "..\\..\\..\\..\\..\\Ext\\Zing\\FrontierTree.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\ParallelExplorer.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\ZingDelayingScheduler.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\ZingStateSpaceTraversal.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\RandomDelayingScheduler.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\RoundRobinScheduler.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\RunToCompletionDBSched.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\StateCoveragePlugin.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\StateVisitCount.dll"
        };

        private static void OutputReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine("OUT: {0}", e.Data);
        }
        
        private static readonly Tuple<string, string>[] ZingMoveMap = new Tuple<string, string>[]
        {
            new Tuple<string, string>("zc\\bin\\Release\\zc.exe", "..\\zc.exe"),
            new Tuple<string, string>("zc\\bin\\Release\\Microsoft.Comega.dll", "..\\Microsoft.Comega.dll"),
            new Tuple<string, string>("zc\\bin\\Release\\Microsoft.Comega.Runtime.dll", "..\\Microsoft.Comega.Runtime.dll"),
            new Tuple<string, string>("zc\\bin\\Release\\Microsoft.Zing.dll", "..\\Microsoft.Zing.dll"),
            new Tuple<string, string>("zc\\bin\\Release\\Microsoft.Zing.Runtime.dll", "..\\Microsoft.Zing.Runtime.dll"),
            new Tuple<string, string>("zc\\bin\\Release\\System.Compiler.dll", "..\\System.Compiler.dll"),
            new Tuple<string, string>("zc\\bin\\Release\\System.Compiler.Framework.dll", "..\\System.Compiler.Framework.dll"),
            new Tuple<string, string>("zc\\bin\\Release\\System.Compiler.Runtime.dll", "..\\System.Compiler.Runtime.dll"),
            new Tuple<string, string>("zc\\bin\\Release\\ZingDelayingScheduler.dll", "..\\ZingDelayingScheduler.dll"),
            new Tuple<string, string>("zc\\bin\\Release\\ZingOptions.dll", "..\\ZingOptions.dll"),
            new Tuple<string, string>("zc\\bin\\Release\\ZingPlugin.dll", "..\\ZingPlugin.dll"),
            new Tuple<string, string>("Zinger\\bin\\Release\\Zinger.exe", "..\\Zinger.exe"),
            new Tuple<string, string>("Zinger\\bin\\Release\\FrontierTree.dll", "..\\FrontierTree.dll"),
            new Tuple<string, string>("Zinger\\bin\\Release\\ParallelExplorer.dll", "..\\ParallelExplorer.dll"),
            new Tuple<string, string>("Zinger\\bin\\Release\\ZingDelayingScheduler.dll", "..\\ZingDelayingScheduler.dll"),
            new Tuple<string, string>("Zinger\\bin\\Release\\ZingStateSpaceTraversal.dll", "..\\ZingStateSpaceTraversal.dll"),
            new Tuple<string, string>("Schedulers\\RandomDelayingScheduler\\bin\\Release\\RandomDelayingScheduler.dll", "..\\RandomDelayingScheduler.dll"),
            new Tuple<string, string>("Schedulers\\RoundRobinScheduler\\bin\\Release\\RoundRobinScheduler.dll", "..\\RoundRobinScheduler.dll"),
            new Tuple<string, string>("Schedulers\\RunToCompletionDBSched\\bin\\Release\\RunToCompletionDBSched.dll", "..\\RunToCompletionDBSched.dll"),
            new Tuple<string, string>("Plugins\\StateCoveragePlugin\\bin\\Release\\StateCoveragePlugin.dll", "..\\StateCoveragePlugin.dll"),
            new Tuple<string, string>("Plugins\\StateVisitCount\\bin\\Release\\StateVisitCount.dll", "..\\StateVisitCount.dll")
        };

        public static bool Build(bool isRebuildForced)
        {
            if (!isRebuildForced && Verify(outputs))
            {
                Program.WriteInfo("Zing dependencies have already been built; skipping this build step.");
                return true;
            }

            var result = true;
            DirectoryInfo zingSrc;
            result = SourceDownloader.Download(SourceDownloader.DependencyKind.ZING, out zingSrc) && result;
            if (!result)
            {
                Program.WriteError("Could not download Zing dependencies");
                return false;
            }

            FileInfo csc;
            result = SourceDownloader.GetCsc(out csc) && result;
            if (!result)
            {
                Program.WriteError("Could not find CSharp compiler");
                return false;
            }

            FileInfo msbuild;
            result = SourceDownloader.GetMsbuild(out msbuild) && result;
            if (!result)
            {
                Program.WriteError("Could not find msbuild");
                return false;
            }

            result = UpgradeAll(zingSrc, "v4.5") && 
                     Compile(zingSrc, zingSln, msbuild) &&
                     DoMove(zingSrc, ZingMoveMap) &&
                     result;
            if (!result)
            {
                Program.WriteError("Could not compile the Zing dependency");
                return false;
            }

            return result;
        }

        private static bool DoMove(DirectoryInfo srcRoot, Tuple<string, string>[] moveMap)
        {
            bool result = true;
            try
            {
                foreach (var t in moveMap)
                {
                    var inFile = new FileInfo(Path.Combine(srcRoot.FullName, t.Item1));
                    if (!inFile.Exists)
                    {
                        result = false;
                        Program.WriteError("Could not find output file {0}", inFile.Name);
                    }

                    inFile.CopyTo(Path.Combine(srcRoot.FullName, t.Item2), true);
                    Program.WriteInfo("Moved output {0} --> {1}", inFile.FullName, Path.Combine(srcRoot.FullName, t.Item2));
                }

                return result;
            }
            catch (Exception e)
            {
                Program.WriteError("Unable to move output files - {0}", e.Message);
                return false;
            }
        }

        /// <summary>
        /// Upgrades all csproj files to the target framework.
        /// </summary>
        private static bool UpgradeAll(DirectoryInfo srcRoot, string frameworkVersion)
        {
            try
            {
                foreach (var inProj in srcRoot.EnumerateFiles("*.csproj", SearchOption.AllDirectories))
                {
                    var outProj = new FileInfo(inProj.FullName + ".tmp");
                    Program.WriteInfo("Upgrading {0} to framework version {1}", inProj.FullName, frameworkVersion);
                    using (var sr = new StreamReader(inProj.FullName))
                    {
                        using (var sw = new StreamWriter(outProj.FullName))
                        {
                            while (!sr.EndOfStream)
                            {
                                var line = sr.ReadLine();
                                if (line.Trim().StartsWith("<TargetFrameworkVersion>", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    sw.WriteLine("<TargetFrameworkVersion>{0}</TargetFrameworkVersion>", frameworkVersion);
                                }
                                else
                                {
                                    sw.WriteLine(line);
                                }
                            }
                        }
                    }

                    inProj.Delete();
                    outProj.MoveTo(inProj.FullName);
                }

                return true;
            }
            catch (Exception e)
            {
                Program.WriteError("Could not complile Zing - {0}", e.Message);
                return false;
            }
        }

        private static bool Compile(DirectoryInfo srcRoot, string projFile, FileInfo msbuild)
        {
            try
            {
                //// First, write bat file to create absolute paths
                var inProj = new FileInfo(Path.Combine(srcRoot.FullName, projFile));
                if (!inProj.Exists)
                {
                    Program.WriteError("Cannot find file {0}", inProj.FullName);
                    return false;
                }

                var psi = new ProcessStartInfo();
                psi.UseShellExecute = false;
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;
                psi.WorkingDirectory = inProj.Directory.FullName;
                psi.FileName = msbuild.FullName;
                psi.Arguments = string.Format(msbuildArgs, inProj.FullName);
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
                Program.WriteError("Could not complile Zing - {0}", e.Message);
                return false;
            }
        }

        private static bool Verify(string[] outputs)
        {
            try
            {
                var runningLoc = new FileInfo(Assembly.GetExecutingAssembly().Location);
                foreach (var t in outputs)
                {
                    var outFile = new FileInfo(Path.Combine(runningLoc.Directory.FullName, t));
                    if (!outFile.Exists)
                    {
                        return false;
                    }
                }
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
