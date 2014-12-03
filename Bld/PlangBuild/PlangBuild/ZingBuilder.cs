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

        private const string MsBuildCommand = "\"{0}\" /p:Configuration=Release /p:Platform={1}";
        private const string PlatformX86 = "x86";
        private const string PlatformX64 = "x64";
        private const string Folderx86 = "..\\x86\\";
        private const string Folderx64 = "..\\x64\\";

        private const string zingSln = "ZING.sln";

        private static readonly string[] outputs = new string[]
        {
            "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\zc.exe",
            "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\Microsoft.Comega.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\Microsoft.Comega.Runtime.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\Microsoft.Zing.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\Microsoft.Zing.Runtime.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\System.Compiler.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\System.Compiler.Framework.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\System.Compiler.Runtime.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\Zinger.exe",
            "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\RandomDelayingScheduler.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\RoundRobinDelayingScheduler.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\RunToCompletionDelayingScheduler.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\ZingExplorer.dll",

            "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\zc.exe",
            "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\Microsoft.Comega.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\Microsoft.Comega.Runtime.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\Microsoft.Zing.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\Microsoft.Zing.Runtime.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\System.Compiler.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\System.Compiler.Framework.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\System.Compiler.Runtime.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\Zinger.exe",
            "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\RandomDelayingScheduler.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\RoundRobinDelayingScheduler.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\RunToCompletionDelayingScheduler.dll",
            "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\ZingExplorer.dll",
        };

        private static void OutputReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine("OUT: {0}", e.Data);
        }
        
        private static readonly Tuple<string, string>[] ZingMoveMap = new Tuple<string, string>[]
        {
            new Tuple<string, string>("zc\\bin\\x64\\Release\\zc.exe", "..\\x64\\zc.exe"),
            new Tuple<string, string>("zc\\bin\\x64\\Release\\Microsoft.Comega.dll", "..\\x64\\Microsoft.Comega.dll"),
            new Tuple<string, string>("zc\\bin\\x64\\Release\\Microsoft.Comega.Runtime.dll", "..\\x64\\Microsoft.Comega.Runtime.dll"),
            new Tuple<string, string>("zc\\bin\\x64\\Release\\Microsoft.Zing.dll", "..\\x64\\Microsoft.Zing.dll"),
            new Tuple<string, string>("zc\\bin\\x64\\Release\\Microsoft.Zing.Runtime.dll", "..\\x64\\Microsoft.Zing.Runtime.dll"),
            new Tuple<string, string>("zc\\bin\\x64\\Release\\System.Compiler.dll", "..\\x64\\System.Compiler.dll"),
            new Tuple<string, string>("zc\\bin\\x64\\Release\\System.Compiler.Framework.dll", "..\\x64\\System.Compiler.Framework.dll"),
            new Tuple<string, string>("zc\\bin\\x64\\Release\\System.Compiler.Runtime.dll", "..\\x64\\System.Compiler.Runtime.dll"),
            new Tuple<string, string>("Zinger\\bin\\x64\\Release\\Zinger.exe", "..\\x64\\Zinger.exe"),
            new Tuple<string, string>("Zinger\\bin\\x64\\Release\\ZingExplorer.dll", "..\\x64\\ZingExplorer.dll"),
            new Tuple<string, string>("DelayingSchedulers\\RandomDelayingScheduler\\bin\\x64\\Release\\RandomDelayingScheduler.dll", "..\\x64\\RandomDelayingScheduler.dll"),
            new Tuple<string, string>("DelayingSchedulers\\RoundRobinDelayingScheduler\\bin\\x64\\Release\\RoundRobinDelayingScheduler.dll", "..\\x64\\RoundRobinDelayingScheduler.dll"),
            new Tuple<string, string>("DelayingSchedulers\\RunToCompletionDelayingScheduler\\bin\\x64\\Release\\RunToCompletionDelayingScheduler.dll", "..\\x64\\RunToCompletionDelayingScheduler.dll"),

            new Tuple<string, string>("zc\\bin\\x86\\Release\\zc.exe", "..\\x86\\zc.exe"),
            new Tuple<string, string>("zc\\bin\\x86\\Release\\Microsoft.Comega.dll", "..\\x86\\Microsoft.Comega.dll"),
            new Tuple<string, string>("zc\\bin\\x86\\Release\\Microsoft.Comega.Runtime.dll", "..\\x86\\Microsoft.Comega.Runtime.dll"),
            new Tuple<string, string>("zc\\bin\\x86\\Release\\Microsoft.Zing.dll", "..\\x86\\Microsoft.Zing.dll"),
            new Tuple<string, string>("zc\\bin\\x86\\Release\\Microsoft.Zing.Runtime.dll", "..\\x86\\Microsoft.Zing.Runtime.dll"),
            new Tuple<string, string>("zc\\bin\\x86\\Release\\System.Compiler.dll", "..\\x86\\System.Compiler.dll"),
            new Tuple<string, string>("zc\\bin\\x86\\Release\\System.Compiler.Framework.dll", "..\\x86\\System.Compiler.Framework.dll"),
            new Tuple<string, string>("zc\\bin\\x86\\Release\\System.Compiler.Runtime.dll", "..\\x86\\System.Compiler.Runtime.dll"),
            new Tuple<string, string>("Zinger\\bin\\x86\\Release\\Zinger.exe", "..\\x86\\Zinger.exe"),
            new Tuple<string, string>("Zinger\\bin\\x86\\Release\\ZingExplorer.dll", "..\\x86\\ZingExplorer.dll"),
            new Tuple<string, string>("DelayingSchedulers\\RandomDelayingScheduler\\bin\\x86\\Release\\RandomDelayingScheduler.dll", "..\\x86\\RandomDelayingScheduler.dll"),
            new Tuple<string, string>("DelayingSchedulers\\RoundRobinDelayingScheduler\\bin\\x86\\Release\\RoundRobinDelayingScheduler.dll", "..\\x86\\RoundRobinDelayingScheduler.dll"),
            new Tuple<string, string>("DelayingSchedulers\\RunToCompletionDelayingScheduler\\bin\\x86\\Release\\RunToCompletionDelayingScheduler.dll", "..\\x86\\RunToCompletionDelayingScheduler.dll"),
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
                //create the folders if not present
                if(!Directory.Exists(Path.Combine(srcRoot.FullName, Folderx64)))  Directory.CreateDirectory(Path.Combine(srcRoot.FullName, Folderx64));
                if(!Directory.Exists(Path.Combine(srcRoot.FullName, Folderx86))) Directory.CreateDirectory(Path.Combine(srcRoot.FullName, Folderx86));

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

                //build x64
                var psi = new ProcessStartInfo();
                psi.UseShellExecute = false;
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;
                psi.WorkingDirectory = inProj.Directory.FullName;
                psi.FileName = msbuild.FullName;
                psi.Arguments = string.Format(MsBuildCommand, inProj.FullName, PlatformX64);
                psi.CreateNoWindow = true;

                var process = new Process();
                process.StartInfo = psi;
                process.OutputDataReceived += OutputReceived;
                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                process.WaitForExit();

                Program.WriteInfo("EXIT: {0}", process.ExitCode);

                if (process.ExitCode != 0)
                    return false;

                // build x86
                psi = new ProcessStartInfo();
                psi.UseShellExecute = false;
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;
                psi.WorkingDirectory = inProj.Directory.FullName;
                psi.FileName = msbuild.FullName;
                psi.Arguments = string.Format(MsBuildCommand, inProj.FullName, PlatformX86);
                psi.CreateNoWindow = true;

                process = new Process();
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
