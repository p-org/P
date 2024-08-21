using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace Plang.PInfer
{
    public class Metadata
    {
        [System.Text.Json.Serialization.JsonPropertyName("folder")]
        public string Folder { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("events")]
        public IEnumerable<string> Events { get; set; }
    }

    public class TraceIndex
    {
        Dictionary<HashSet<string>, string> traceIndex;
        private readonly string parentFolder;
        private readonly bool canSave;
        private readonly static string METADATA = "metadata.json";

        public TraceIndex([DisallowNull] string traceFolder, bool create = false)
        {
            var filePath = Path.Combine(traceFolder, METADATA);
            canSave = create;
            if (create)
            {
                if (!Directory.Exists(traceFolder))
                {
                    Directory.CreateDirectory(traceFolder);
                }
                if (!File.Exists(filePath))
                {
                    try
                    {
                        using FileStream fs = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                        fs.Close();
                    } catch (IOException)
                    {}
                }
            }
            parentFolder = traceFolder;
            traceIndex = [];
            try{
                var json = TryRead(filePath);
                if (json != "")
                {
                    var metadata = JsonSerializer.Deserialize<List<Metadata>>(json);
                    foreach (Metadata meta in metadata)
                    {
                        HashSet<string> k = meta.Events.ToHashSet();
                        traceIndex[k] = meta.Folder;
                    }
                }
            }
            catch (IOException)
            {
                WriteError($"Cannot find `metadata.json` under {traceFolder}");
                Environment.Exit(1);
            }
            catch (JsonException)
            {
                WriteError($"Mal-formed `metadata.json`");
                Environment.Exit(1);
            }
        }

        public bool TryGet(IEnumerable<string> events, out string path)
        {
            // first exact key check
            HashSet<string> k = events.ToHashSet();
            foreach (var key in traceIndex.Keys)
            {
                if (key.SetEquals(k))
                {
                    path = Path.Combine(parentFolder, traceIndex[key]);
                    return true;
                }
            }
            // next, subset check
            foreach (var key in traceIndex.Keys)
            {
                if (k.IsSubsetOf(key))
                {
                    path = Path.Combine(parentFolder, traceIndex[key]);
                    return true;
                }
            }
            path = null;
            return false;
        }

        public string AddIndex(IEnumerable<string> events, string path)
        {
            HashSet<string> k = events.ToHashSet();
            // first look for exact match
            foreach (var key in traceIndex.Keys)
            {
                if (key.SetEquals(k))
                {
                    var record = traceIndex[key];
                    if (record != path)
                    {
                        // caller is responsible for merging the two directories
                        return record;
                    }
                    return path;
                }
            }
            traceIndex[k] = path;
            return path;
        }

        public void Commit()
        {
            var metadataPath = Path.Combine(parentFolder, METADATA);
            using var jsonFile = File.Open(metadataPath, FileMode.OpenOrCreate, FileAccess.Write);
            JsonSerializer.Serialize(jsonFile, Serialize());
            jsonFile.Close();
        }

        private string TryRead(string file)
        {
            string result = null;
            while (result == null)
            {
                try
                {
                    using FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using StreamReader r = new(fs);
                    result = r.ReadToEnd();
                    r.Close();
                    fs.Close();
                } catch (IOException)
                {
                    Thread.Sleep(1000);
                }
            }
            return result;
        }

        private List<Metadata> Serialize()
        {
            List<Metadata> result = [];
            foreach (var (k, v) in traceIndex)
            {
                result.Add(new Metadata() {
                    Events = k, Folder = v
                });
            }
            return result;
        }

        private void WriteError(string msg)
        {
            var defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[TraceIndex]: " + msg);
            Console.ForegroundColor = defaultColor;
        }
    }

    public class PInferInvoke
    {
        public static int invokeMain(PInferConfiguration configuration)
        {
            ProcessStartInfo startInfo;
            Process process;
            var depsOpt = Path.Combine(GetPInferDependencies(), "*");
            var classpath = Path.Combine(configuration.OutputDirectory, "PInfer", "target", "classes");
            List<string> args = ["-cp",
                    string.Join(":", [depsOpt, classpath]),
                    $"{configuration.ProjectName}.pinfer.Main"];
            
            ShowConfig(configuration);
            startInfo = new ProcessStartInfo("java", args.Concat(GetMinerConfigArgs(configuration)))
            {
                UseShellExecute = true
            };
            if (configuration.Verbose)
            {
                Console.WriteLine($"Run with {string.Join(" ", args.Concat(GetMinerConfigArgs(configuration)))}");
            }
            process = Process.Start(startInfo);
            process.WaitForExit();
            Console.WriteLine("Cleaning up ...");
            var dirInfo = new DirectoryInfo("./");
            foreach (var file in dirInfo.GetFiles())
            {
                if (file.Name.EndsWith(".inv.gz") || file.Name.EndsWith(".dtrace.gz"))
                {
                   file.Delete();
                }
            }
            return process.ExitCode;
        }

        private static void ShowConfig(PInferConfiguration config)
        {
            Console.WriteLine("Mining specifications captured by the following search space:");
            Console.WriteLine($"\t#Forall preceding quantifiers:\t\t{(config.NumForallQuantifiers == -1 ? "#Quantified Events" : config.NumForallQuantifiers)}");
            Console.WriteLine($"\t#Predicates in Guard:\t\t\t{config.NumGuardPredicates}");
            Console.WriteLine($"\t#Predicates in Filter:\t\t\t{config.NumFilterPredicates}");
            Console.WriteLine($"\tArity of target specifications:\t\t{config.InvArity}");
            Console.WriteLine($"\tPruning Level:\t\t\t\t-O{config.PruningLevel}");
            if (config.MustIncludeGuard.Count() > 0)
            {
                Console.WriteLine($"\tMust include guards:\t\t\t{string.Join(", ", config.MustIncludeGuard)}");
            }
            if (config.MustIncludeFilter.Count() > 0)
            {
                Console.WriteLine($"\tMust include filters(Id):\t\t{string.Join(", ", config.MustIncludeFilter)}");
            }
            Console.WriteLine($"\tSkip trivial combinations:\t\t{config.SkipTrivialCombinations}");
        }

        private static List<string> GetMinerConfigArgs(PInferConfiguration configuration)
        {
            var args = new List<string>();
            if (configuration.NumForallQuantifiers != -1)
            {
                args.Add("-nforall");
                args.Add($"{configuration.NumForallQuantifiers}");
                args.Add("-fd");
                args.Add($"{configuration.NumFilterPredicates}");
            }
            args.Add("-gd");
            args.Add($"{configuration.NumGuardPredicates}");
            if (configuration.SkipTrivialCombinations)
            {
                args.Add("-st");
            }
            args.Add("-p");
            args.Add(Path.Combine(configuration.OutputDirectory, "PInfer", $"{configuration.ProjectName}.predicates.json"));
            args.Add("-t");
            args.Add(Path.Combine(configuration.OutputDirectory, "PInfer", $"{configuration.ProjectName}.terms.json"));
            if (configuration.MustIncludeGuard.Count() > 0)
            {
                args.Add("-g");
                foreach (var g in configuration.MustIncludeGuard)
                {
                    args.Add($"{g}");
                }
            }
            if (configuration.MustIncludeFilter.Count() > 0)
            {
                args.Add("-f");
                foreach (var f in configuration.MustIncludeFilter)
                {
                    args.Add($"{f}");
                }
            }
            args.Add("-nt");
            args.Add($"{configuration.InvArity}");
            args.Add("-O");
            args.Add($"{configuration.PruningLevel}");
            if (configuration.Verbose)
            {
                args.Add("-v");
            }
            args.Add("-l");
            foreach (var t in configuration.TracePaths)
            {
                args.Add($"{t}");
            }
            return args;
        }

        private static string GetPInferDependencies()
        {
            var currentDir = AppDomain.CurrentDomain.BaseDirectory;
            DirectoryInfo info = new(currentDir);
            return Path.Combine(info.Parent.Parent.Parent.Parent.Parent.ToString(), "pinfer-dependencies");
        }
    }
}
