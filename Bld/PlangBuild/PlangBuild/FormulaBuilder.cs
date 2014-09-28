namespace PlangBuild
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Net.Http;
    using System.IO;
    using System.IO.Compression;
    using System.Reflection;
    using System.Diagnostics;

    internal static class FormulaBuilder
    {
        public static void PrintOutputs()
        {
            foreach (var o in outputs)
            {
                Program.WriteInfo("Formula dependency: {0}", o);
            }
        }

        private static readonly string[] outputs = new string[]
        {
            "..\\..\\..\\..\\..\\Ext\\Formula\\gplex45.exe",
            "..\\..\\..\\..\\..\\Ext\\Formula\\gppg45.exe",
            "..\\..\\..\\..\\..\\Ext\\Formula\\Formula.exe",
            "..\\..\\..\\..\\..\\Ext\\Formula\\Formula.exe.config",
            "..\\..\\..\\..\\..\\Ext\\Formula\\Core.dll",
            "..\\..\\..\\..\\..\\Ext\\Formula\\Microsoft.Z3.dll",
            "..\\..\\..\\..\\..\\Ext\\Formula\\libz3.dll",
            "..\\..\\..\\..\\..\\Ext\\Formula\\FormulaCodeGeneratorTask.dll" 
        };

        private static readonly Tuple<string, string>[] ReleaseMoveMap = new Tuple<string, string>[]
        {
            new Tuple<string, string>(
                "Ext\\GPLEX\\gplex45.exe",
                "..\\gplex45.exe"),
            new Tuple<string, string>(
                "Ext\\GPPG\\gppg45.exe",
                "..\\gppg45.exe"),
            new Tuple<string, string>(
                "Bld\\Drops\\Formula_Release_x86\\Formula.exe",
                "..\\Formula.exe"),
            new Tuple<string, string>(
                "Bld\\Drops\\Formula_Release_x86\\Formula.exe.config",
                "..\\Formula.exe.config"),
            new Tuple<string, string>(
                "Bld\\Drops\\Formula_Release_x86\\Core.dll",
                "..\\Core.dll"),
            new Tuple<string, string>(
                "Bld\\Drops\\Formula_Release_x86\\Microsoft.Z3.dll",
                "..\\Microsoft.Z3.dll"),
            new Tuple<string, string>(
                "Bld\\Drops\\Formula_Release_x86\\libz3.dll",
                "..\\libz3.dll"),
            new Tuple<string, string>(   
                "Src\\Extensions\\FormulaCodeGeneratorTask\\bin\\x86\\FormulaCodeGeneratorTask.dll",   
                "..\\FormulaCodeGeneratorTask.dll"),  
        };

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

        public static bool Build(bool isRebuildForced)
        {
            if (!isRebuildForced && Verify(outputs))
            {
                Program.WriteInfo("Formula dependencies have already been built; skipping this build step.");
                return true;
            }

            var result = true;
            DirectoryInfo formulaSrcDir;
            result = SourceDownloader.Download(SourceDownloader.DependencyKind.FORMULA, out formulaSrcDir);
            if (!result)
            {
                Program.WriteError("Could not download Formula");
                return false;
            }

            var psi = new ProcessStartInfo();
            psi.UseShellExecute = false;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            psi.WorkingDirectory = formulaSrcDir.FullName + "\\Bld";
            psi.FileName = "build.bat";
            psi.Arguments = "";
            psi.CreateNoWindow = true;

            var process = new Process();
            process.StartInfo = psi;
            process.OutputDataReceived += OutputReceived;
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.WaitForExit();

            Program.WriteInfo("EXIT: {0}", process.ExitCode);
            result = process.ExitCode == 0;
            if (!result)
            {
                return false;
            }

            result = DoMove(formulaSrcDir, ReleaseMoveMap) && result;
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
                        Program.WriteError("Could not find output file {0}", inFile.FullName);
                        continue;
                    }

                    var outFile = new FileInfo(Path.Combine(srcRoot.FullName, t.Item2));
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

        private static void OutputReceived(
            object sender,
            DataReceivedEventArgs e)
        {
            Console.WriteLine("OUT: {0}", e.Data);
        }
    }
}
