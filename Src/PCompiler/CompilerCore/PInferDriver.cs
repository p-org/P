using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Plang.Compiler.Backend.PInfer;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler
{
    class PInferDriver
    {
        public static void CompilePInferHint(ICompilerConfiguration job, Scope globalScope, Hint hint)
        {
            PInferPredicateGenerator codegen = (PInferPredicateGenerator) job.Backend;
            codegen.Reset();
            codegen.WithHint(hint);
            foreach (var file in codegen.GenerateCode(job, globalScope))
            {
                job.Output.WriteFile(file);
            }
            job.Output.WriteInfo($"Compiling generated code...");
            try
            {
                codegen.Compile(job);
            }
            catch (TranslationException e)
            {
                job.Output.WriteError($"[Compiling Generated Code:]\n" + e.Message);
                job.Output.WriteError("[THIS SHOULD NOT HAVE HAPPENED, please report it to the P team or create a GitHub issue]\n" + e.Message);
            }
        }

        public static void RunSpecMiner(ICompilerConfiguration job, TraceMetadata metadata, Scope globalScope, Hint hint)
        {
            PInferPredicateGenerator backend = (PInferPredicateGenerator) job.Backend;
            if (backend.hint == null || !backend.hint.Equals(hint))
            {
                job.Output.WriteWarning($"Have not compiled with {hint.Name}. Re-compling...");
                CompilePInferHint(job, globalScope, hint);
            }
            Console.WriteLine("Running the following hint:");
            hint.ShowHint();
            PInferInvoke.InvokeMain(job, metadata, globalScope, hint, backend);
        }

        public static void ParameterSearch(ICompilerConfiguration job, TraceMetadata metadata, Scope globalScope, Hint hint)
        {
            if (hint.Exact)
            {
                RunSpecMiner(job, metadata, globalScope, hint);
                return;
            }
            // Given event combination
            // Enumerate term depth
            List<Hint> worklist = [];
            if (hint.TermDepth != null)
            {
                worklist.Add(hint.Copy());
            }
            else
            {
                for (int i = 0; i <= job.TermDepth; ++i)
                {
                    Hint h = hint.Copy();
                    h.TermDepth = i;
                    worklist.Add(h);
                }
            }
            PInferPredicateGenerator codegen = (PInferPredicateGenerator) job.Backend;
            job.Output.WriteInfo($"Number of Hints: {worklist.Count}");
            foreach (var h in worklist)
            {
                CompilePInferHint(job, globalScope, h);
                while (h.HasNext(job, codegen.MaxArity()))
                {
                    RunSpecMiner(job, metadata, globalScope, h);
                    h.Next(job, codegen.MaxArity());
                }
            }
        }

        public static void PerformInferAction(ICompilerConfiguration job, Scope globalScope)
        {
            Hint givenHint = null;
            if (job.PInferAction == PInferAction.Compile || job.PInferAction == PInferAction.RunHint)
            {
                string availableHints = "";
                foreach (var hint in globalScope.Hints)
                {
                    availableHints += "- " + hint.Name + "\n";
                }
                if (job.HintName == null)
                {
                    job.Output.WriteError($"[Error] No hint provided. Available hints:\n{availableHints}");
                    Environment.Exit(1);
                }
                if (!globalScope.Get(job.HintName, out givenHint))
                {
                    job.Output.WriteWarning($"Hint \"{job.HintName}\" not found. Available hints:\n{availableHints}");
                    Environment.Exit(1);
                }
            }
            PEvent configEvent = null;
            if (job.ConfigEvent != null)
            {
                if (globalScope.Lookup(job.ConfigEvent, out PEvent e))
                {
                    configEvent = e;
                }
                else
                {
                    job.Output.WriteError($"Config event passed through command line not defined: {job.ConfigEvent}");
                }
            }
            switch (job.PInferAction)
            {
                case PInferAction.Compile:
                    Console.WriteLine("Compile Mode");
                    givenHint.ConfigEvent ??= configEvent;
                    CompilePInferHint(job, globalScope, givenHint);
                    break;
                case PInferAction.RunHint:
                    Console.WriteLine("RunHint Modde");
                    TraceMetadata metadata = new TraceMetadata(job);
                    givenHint.ConfigEvent ??= configEvent;
                    ParameterSearch(job, metadata, globalScope, givenHint);
                    break;
            }
        }
    }

    internal class PInferInvoke
    {
        public static int InvokeMain(ICompilerConfiguration job, TraceMetadata metadata, Scope globalScope, Hint hint, PInferPredicateGenerator codegen)
        {
            ProcessStartInfo startInfo;
            Process process;
            var depsOpt = Path.Combine(GetPInferDependencies(), "*");
            var classpath = Path.Combine(job.OutputDirectory.ToString(), "target", "classes");
            List<string> args = ["-cp",
                    string.Join(":", [depsOpt, classpath]),
                    $"{job.ProjectName}.pinfer.Main"];
            
            ShowConfig(hint);
            List<string> configArgs = GetMinerConfigArgs(job, metadata, hint, codegen);
            if (configArgs == null)
            {
                job.Output.WriteWarning("Skipped due to config argument error ...");
                return -1;
            }
            startInfo = new ProcessStartInfo("java", args.Concat(configArgs))
            {
                UseShellExecute = true
            };
            if (job.Verbose)
            {
                Console.WriteLine($"Run with {string.Join(" ", args.Concat(configArgs))}");
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

        private static void ShowConfig(Hint hint)
        {
            
        }

        private static List<string> GetMinerConfigArgs(ICompilerConfiguration configuration, TraceMetadata metadata, Hint hint, PInferPredicateGenerator codegen)
        {
            var args = new List<string>();
            if (hint.ExistentialQuantifiers > 0)
            {
                args.Add("-nforall");
                args.Add($"{hint.Quantified.Count - hint.ExistentialQuantifiers}");
                args.Add("-fd");
                args.Add($"{hint.NumFilterPredicates}");
            }
            args.Add("-gd");
            args.Add($"{hint.NumGuardPredicates}");
            // skip trivial by default
            args.Add("-st");
            args.Add("-p");
            args.Add(Path.Combine(configuration.OutputDirectory.ToString(), $"{configuration.ProjectName}.predicates.json"));
            args.Add("-t");
            args.Add(Path.Combine(configuration.OutputDirectory.ToString(), $"{configuration.ProjectName}.terms.json"));
            if (hint.GuardPredicates.Count > 0)
            {
                args.Add("-g");
                foreach (var g in hint.GuardPredicates)
                {
                    args.Add($"{codegen.GetPredicateId(g, configuration)}");
                }
            }
            if (hint.FilterPredicates.Count > 0)
            {
                args.Add("-f");
                foreach (var f in hint.FilterPredicates)
                {
                    args.Add($"{codegen.GetPredicateId(f, configuration)}");
                }
            }
            args.Add("-nt");
            args.Add($"{hint.Arity}");
            args.Add("-O");
            args.Add($"{hint.PruningLevel}");
            if (configuration.Verbose)
            {
                args.Add("-v");
            }
            args.Add("-l");
            if (metadata.GetTraceFolder(hint, out var folder))
            {
                // args.Add();
                foreach (var file in Directory.GetFiles(Path.Combine(configuration.TraceFolder, folder)))
                {
                    args.Add(file);
                }
            }
            else
            {
                configuration.Output.WriteError($"No trace indexed for the following event combination:\n{string.Join(", ", hint.Quantified.Select(x => x.EventName))}");
                return null;
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

    internal sealed class TraceMetadata
    {
        Dictionary<HashSet<string>, string> traceIndex;

        public TraceMetadata(ICompilerConfiguration job)
        {
            var filePath = Path.Combine(job.TraceFolder, "metadata.json");
            traceIndex = [];
            try{
                using StreamReader r = new(filePath);
                string json = r.ReadToEnd();
                var metadata = JsonSerializer.Deserialize<List<Metadata>>(json);
                foreach (Metadata meta in metadata)
                {
                    HashSet<string> k = meta.Events.ToHashSet();
                    traceIndex[k] = meta.Folder;
                }
            }
            catch (Exception e)
            {
                job.Output.WriteError(e.Message + " trace folder not found");
                Environment.Exit(1);
            }
        }

        public bool GetTraceFolder(Hint h, out string folder)
        {
            HashSet<string> k = h.Quantified.Select(x => x.EventName).ToHashSet();
            if (h.ConfigEvent != null)
            {
                k.Add(h.ConfigEvent.Name);
            }
            foreach (var key in traceIndex.Keys)
            {
                if (key.SetEquals(k))
                {
                    folder = traceIndex[key];
                    return true;
                }
            }
            folder = null;
            return false;
        }
    }

    internal sealed class Metadata
    {
        [System.Text.Json.Serialization.JsonPropertyName("folder")]
        public string Folder { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("events")]
        public IEnumerable<string> Events { get; set; }
    }
}