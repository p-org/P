namespace PlangBuild
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Net.Http;
    using System.IO;
    using System.IO.Compression;
    using System.Reflection;
    using Microsoft.Win32;

    internal static class SourceDownloader
    {
        public enum DependencyKind { ZING };
        private const string WinDirEnvVar = "WinDir";
        private const string CscName = "csc.exe";
        private const string MSbuildName = "msbuild.exe";

        private static readonly string[] FrameworkLocs = new string[]
        {
            "Microsoft.NET\\Framework64\\v4.0.30319",
            "Microsoft.NET\\Framework\\v4.0.30319"
        };

        private static readonly string[] FrameworkLocs32 = new string[]
        {
            "Microsoft.NET\\Framework\\v4.0.30319"
        };
        
        private static readonly Tuple<string, string, string, string>[] Versions = new Tuple<string, string, string, string>[] 
        {
            new Tuple<string, string, string, string>("zing", "https://github.com/ZingModelChecker/Zing/archive/master.zip", "..\\..\\..\\..\\..\\Ext\\Zing\\Zing_.zip", "..\\..\\..\\..\\..\\Ext\\Zing\\Zing-master\\")
        };

        public static bool GetBuildRelFile(string filename, out FileInfo file)
        {
            try
            {
                var runningLoc = new FileInfo(Assembly.GetExecutingAssembly().Location);
                file = new FileInfo(Path.Combine(runningLoc.Directory.FullName, filename));
                return file.Exists;
            }
            catch (Exception e)
            {
                file = null;
                Program.WriteError("Could not locate file {0} - {1}", filename, e.Message);
                return false;
            }
        }

        public static bool GetFrameworkDir(out DirectoryInfo framework, bool force32Bit = false)
        {
            try
            {
                var winDir = Environment.GetEnvironmentVariable(WinDirEnvVar);
                var locs = force32Bit ? FrameworkLocs32 : FrameworkLocs;
                foreach (var dir in locs)
                {
                    framework = new DirectoryInfo(Path.Combine(winDir, dir));
                    if (framework.Exists)
                    {
                        return true;
                    }
                }

                framework = null;
                return false;
            }
            catch (Exception e)
            {
                framework = null;
                Program.WriteError("Could not locate .NET framework directory - {0}", e.Message);
                return false;
            }
        }

        public static bool GetMsbuild(out FileInfo msbuild, bool force32Bit = false)
        {
            return GetFrameworkFile(MSbuildName, out msbuild, force32Bit);
        }

        public static bool GetCsc(out FileInfo csc)
        {
            return GetFrameworkFile(CscName, out csc);
        }

        public static bool Download(DependencyKind dep, out DirectoryInfo outputDir)
        {
            try
            {
                var projVersion = Versions[(int)dep];
                var runningLoc = new FileInfo(Assembly.GetExecutingAssembly().Location);
                var outputFile = new FileInfo(Path.Combine(runningLoc.DirectoryName, projVersion.Item3));
                outputDir = new DirectoryInfo(Path.Combine(runningLoc.DirectoryName, projVersion.Item4));
                // Kill existing directories
                if (outputFile.Exists)
                {
                    outputFile.Delete();
                }

                if (outputDir.Exists)
                {
                    outputDir.Delete(true);
                }
                // Create a New HttpClient object.
                Program.WriteInfo("Downloading dependency {0} to {1}...", projVersion.Item1, outputFile.FullName);
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Referrer = new Uri(projVersion.Item2);
                using (var strm = client.GetStreamAsync(projVersion.Item2).Result)
                {
                    using (var sw = new System.IO.StreamWriter(outputFile.FullName))
                    {
                        strm.CopyTo(sw.BaseStream);
                    }
                }

                Program.WriteInfo("Extracting dependency {0} to {1}...", projVersion.Item1, outputDir.FullName);
                ZipFile.ExtractToDirectory(outputFile.FullName, outputDir.FullName + "..\\");
            }
            catch (Exception e)
            {
                outputDir = null;
                Program.WriteError("Failed to get dependency {0} : {1}", dep, e.Message);
                return false;
            }

            return true;
        }

        private static bool GetFrameworkFile(string fileName, out FileInfo file, bool force32Bit = false)
        {
            try
            {
                DirectoryInfo framework;
                if (!GetFrameworkDir(out framework, force32Bit))
                {
                    Program.WriteError("Could not locate {0} - {1}", fileName, "missing .NET framework");
                }

                var files = framework.GetFiles(fileName, SearchOption.TopDirectoryOnly);
                Contract.Assert(files.Length <= 1);
                if (files.Length == 0)
                {
                    file = null;
                    return false;
                }
                else
                {
                    file = files[0];
                    return true;
                }
            }
            catch (Exception e)
            {
                file = null;
                Program.WriteError("Could not locate {0} - {1}", fileName, e.Message);
                return false;
            }
        }
    }
}
