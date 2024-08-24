using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Plang.Compiler.Backend.PInfer;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.States;
using Plang.Compiler.TypeChecker.Types;
using Plang.PInfer;

namespace Plang.Compiler
{
    class PInferDriver(ICompilerConfiguration job, Scope globalScope)
    {
        private readonly ICompilerConfiguration Job = job;
        private readonly Scope GlobalScope = globalScope;
        private readonly PInferPredicateGenerator Codegen = (PInferPredicateGenerator)job.Backend;
        private readonly TraceMetadata TraceIndex = new(job);
        private readonly HashSet<Hint> ExploredHints = new(new Hint.EqualityComparer());
        private int numDistilledInvs = 0;
        private int numTotalInvs = 0;

        public bool CompilePInferHint(Hint hint)
        {
            int origNumTerms = Codegen.NumTerms;
            int origNumPreds = Codegen.NumPredicates;
            Codegen.Reset();
            Codegen.WithHint(hint);
            foreach (var file in Codegen.GenerateCode(Job, GlobalScope))
            {
                Job.Output.WriteFile(file);
            }
            if (Codegen.NumTerms == origNumTerms && Codegen.NumPredicates == origNumPreds)
            {
                // increasing the term depth does not add new term/prediates
                Job.Output.WriteWarning($"Term depth limit reached ... Done for {hint.Name}");
                Codegen.Reset();
                return false;
            }
            Job.Output.WriteInfo($"Compiling generated code...");
            try
            {
                Codegen.Compile(Job);
                return true;
            }
            catch (TranslationException e)
            {
                Job.Output.WriteError($"[Compiling Generated Code:]\n" + e.Message);
                Job.Output.WriteError("[THIS SHOULD NOT HAVE HAPPENED, please report it to the P team or create a GitHub issue]\n" + e.Message);
                return false;
            }
        }

        public void RunSpecMiner(Hint hint)
        {
            if (Codegen.hint == null || !Codegen.hint.Equals(hint))
            {
                if (hint.TermDepth == null)
                {
                    hint.TermDepth = 0;
                }
                CompilePInferHint(hint);
            }
            if (ExploredHints.Contains(hint))
            {
                Job.Output.WriteInfo($"Search space already explored: {hint.Name}, skipping ...");
                return;
            }
            ExploredHints.Add(hint.Copy());
            Console.WriteLine("===============================");
            Console.WriteLine("Running the following hint:");
            hint.ShowHint();
            Console.WriteLine("===============================");
            numDistilledInvs += PInferInvoke.InvokeMain(Job, TraceIndex, GlobalScope, hint, Codegen, out int total);
            numTotalInvs += total;
        }

        public void ParameterSearch(Hint hint)
        {
            if (hint.Exact)
            {
                RunSpecMiner(hint);
                return;
            }
            if (ExploredHints.Contains(hint))
            {
                Job.Output.WriteInfo($"Search space already explored: {hint.Name}, skipping ...");
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
                for (int i = 0; i <= Job.TermDepth; ++i)
                {
                    Hint h = hint.Copy();
                    h.TermDepth = i;
                    worklist.Add(h);
                }
            }
            // Job.Output.WriteInfo($"Number of Hints: {worklist.Count}");
            foreach (var h in worklist)
            {
                if (CompilePInferHint(h))
                {
                    while (h.HasNext(Job, Codegen.MaxArity()))
                    {
                        RunSpecMiner(h);
                        h.Next(Job, Codegen.MaxArity());
                    }
                }
            }
            ExploredHints.Add(hint);
        }

        public PEventVariable MkEventVar(PEvent e, int i)
        {
            return new PEventVariable($"e{i}") {
                EventDecl = e, Type = e.PayloadType, Order = i
            };
        }

        public void AddHint(string name, HashSet<Hint> tasks, params PEvent[] events)
        {
            // Enusre all events have some payload
            if (events.Select(e => e.PayloadType == PrimitiveType.Null).Any(x => x))
            {
                Job.Output.WriteWarning($"skipping ae_{name} due to empty payload(s)");
            }
            tasks.Add(new($"ae_{name}", false, null) {
                Quantified = events.Select(MkEventVar).ToList()
            });
        }

        public HashSet<Hint> ExploreHandlers(List<State> allStates)
        {
            HashSet<Hint> tasks = new(new Hint.EqualityComparer());
            foreach (var s in allStates)
            {
                // Looking at one state
                foreach (var (@event, handler) in s.AllEventHandlers)
                {
                    AddHint($"{s.OwningMachine.Name}_{s.Name}_recv_{@event.Name}", tasks, @event);
                    if (handler is EventDoAction action && action.Target.CanSend == true)
                    {
                        // Quantifying:
                        // - forall*exists* e_send, e_recv
                        // - forall*exists* e_send, e_send
                        // - no need to look at `e_recv, e_recv` as this might be sent somewhere
                        foreach (var send in action.Target.SendSet)
                        {
                            AddHint($"{s.OwningMachine.Name}_{s.Name}_recv_{@event.Name}_send_{send.Name}", tasks, send, @event);
                            AddHint($"{s.OwningMachine.Name}_{s.Name}_send_{send.Name}_send_{send.Name}", tasks, send, send);
                        }
                    }
                }

                // Looking at two states
                for (int i = 0; i < allStates.Count; ++i)
                {
                    for (int j = 0; j < allStates.Count; ++j)
                    {
                        if (i == j) continue;
                        var s1 = allStates[i];
                        var s2 = allStates[j];
                        var m1 = s1.OwningMachine;
                        var m2 = s2.OwningMachine;
                        // Looking for s1.send is in s2.recv
                        foreach (var (recv_1, h1) in s1.AllEventHandlers)
                        {
                            if (h1 is not EventDoAction a1 || a1.Target.CanSend != true) continue; 
                            foreach (var (recv_2, h2) in s2.AllEventHandlers)
                            {
                                if (h2 is not EventDoAction a2 || a2.Target.CanSend != true) continue;
                                if (a1.Target.SendSet.Contains(recv_2)) {
                                    // explore s2.send, s1.recv
                                    foreach (PEvent s2send in a2.Target.SendSet)
                                    {
                                        AddHint($"{m2.Name}_{s2.Name}_send_{s2send.Name}_{m1.Name}_{s1.Name}_recv_{recv_1.Name}", tasks, s2send, recv_1);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return tasks;
        }

        public void AutoExplore()
        {
            List<Machine> machines = GlobalScope.Machines.Where(x => !x.IsSpec).ToList();
            // explore single-machine state
            HashSet<Hint> tasks = new(new Hint.EqualityComparer());
            foreach (var m in machines)
            {
                tasks.UnionWith(ExploreHandlers(m.AllStates().ToList()));
            }
            for (int i = 0; i <= Job.TermDepth; ++i)
            {
                foreach (var task in tasks)
                {
                    task.TermDepth = i;
                    ParameterSearch(task);
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
                    job.Output.WriteWarning($"No hint provided. Available hints:\n{availableHints}");
                    Environment.Exit(1);
                }
                if (!globalScope.Get(job.HintName, out givenHint))
                {
                    job.Output.WriteWarning($"Hint \"{job.HintName}\" not found. Available hints:\n{availableHints}");
                    Environment.Exit(1);
                }
            }
            if (job.PInferAction == PInferAction.RunHint || job.PInferAction == PInferAction.Auto)
            {
                if (job.TraceFolder == null)
                {
                    job.Output.WriteError("An indexed trace folder has to be provided for `auto` and `run`.");
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
            PInferInvoke.NewInvFiles();
            var stopwatch = new Stopwatch();
            int numInvsDistilled = 0;
            int numInvsMined = 0;
            stopwatch.Start();
            switch (job.PInferAction)
            {
                case PInferAction.Compile:
                    job.Output.WriteInfo($"PInfer - Compile `{givenHint.Name}`");
                    givenHint.ConfigEvent ??= configEvent;
                    new PInferDriver(job, globalScope).CompilePInferHint(givenHint);
                    break;
                case PInferAction.RunHint:
                {
                    job.Output.WriteInfo($"PInfer - Run `{givenHint.Name}`");
                    givenHint.ConfigEvent ??= configEvent;
                    givenHint.PruningLevel = job.PInferPruningLevel;
                    var driver = new PInferDriver(job, globalScope);
                    driver.ParameterSearch(givenHint);
                    numInvsDistilled = driver.numDistilledInvs;
                    numInvsMined = driver.numTotalInvs;
                    break;
                }
                case PInferAction.Auto:
                {
                    job.Output.WriteInfo("PInfer - Auto Exploration");
                    var driver = new PInferDriver(job, globalScope);
                    driver.AutoExplore();
                    numInvsDistilled = driver.numDistilledInvs;
                    numInvsMined = driver.numTotalInvs;
                    break;
                }
            }
            stopwatch.Stop();
            if (job.PInferAction == PInferAction.RunHint || job.PInferAction == PInferAction.Auto)
            {
                var elapsed = stopwatch.ElapsedMilliseconds / 1000.0;
                job.Output.WriteInfo($"PInfer statistics");
                job.Output.WriteInfo($"\t# invariants discovered: {numInvsMined}");
                job.Output.WriteInfo($"\t# invariants distilled: {numInvsDistilled}");
                job.Output.WriteInfo($"\tTime elapsed (seconds): {elapsed}");
            }
        }
    }

    internal class PInferInvoke
    {

        internal static readonly string DistilledInvFile = Path.Combine("PInferOutputs", "distilled_invs.txt");
        internal static readonly string StepbackInvFile = Path.Combine("PInferOutputs", "stepback_invs.txt");
        internal static HashSet<string> Learned = [];

        public static void NewInvFiles()
        {
            DirectoryInfo dir = new("PInferOutputs");
            if (!dir.Exists) return;
            if (File.Exists(DistilledInvFile))
            {
                int cnt = dir.GetFiles().Where(x => x.Name.StartsWith("distilled_invs")).Count();
                File.Move(DistilledInvFile, Path.Combine("PInferOutputs", $"distilled_invs_{cnt - 1}.txt"));
            }
            if (File.Exists(StepbackInvFile))
            {
                int cnt = dir.GetFiles().Where(x => x.Name.StartsWith("stepback_invs")).Count();
                File.Move(StepbackInvFile, Path.Combine("PInferOutputs", $"stepback_invs_{cnt - 1}.txt"));
            }
        }

        public static void WriteInvs(PInferPredicateGenerator codegen, string guards, string filters, List<string> keep, List<string> stepback)
        {
            if (!File.Exists(DistilledInvFile))
            {
                File.Create(DistilledInvFile).Close();
            }
            if (!File.Exists(StepbackInvFile))
            {
                File.Create(StepbackInvFile).Close();
            }
            var header = codegen.hint.GetInvariantReprHeader(guards, filters);
            if (keep.Count > 0 || filters.Length > 0)
            {
                using StreamWriter invw = File.AppendText(DistilledInvFile);
                var kept = string.Join(" ∧ ", keep);
                if (filters.Length > 0) header += " ∧ ";
                var inv = header + kept;
                if (!Learned.Contains(inv))
                {
                    invw.WriteLine(inv);
                    Learned.Add(inv);
                }
                invw.Close();
            }
            if (stepback.Count > 0 || filters.Length > 0)
            {
                using StreamWriter invw = File.AppendText(StepbackInvFile);
                var stepbacked = string.Join(" ∧ ", stepback);
                if (filters.Length > 0) header += " ∧ ";
                var inv = header + stepbacked;
                if (!Learned.Contains(inv))
                {
                    invw.WriteLine(inv);
                    Learned.Add(inv);
                }
                invw.Close();
            }
        }
    
        public static int PruneAndAggregate(ICompilerConfiguration job, Scope globalScope, PInferPredicateGenerator codegen, out int total)
        {
            var parseFilePath = Path.Combine("PInferOutputs", "SpecMining", PreambleConstants.ParseFileName);
            var contents = File.ReadAllLines(parseFilePath);
            int result = 0;
            total = 0;
            for (int i = 0; i < contents.Length; i += 3)
            {
                var guards = contents[i];
                var filters = contents[i + 1];
                var properties = contents[i + 2].Split("∧").Select(x => x.Trim());
                total += 1;
                List<string> keep = [];
                List<string> stepback = [];
                foreach (var prop in properties)
                {
                    switch (codegen.CheckForPruning(job, globalScope, prop, out var repr))
                    {
                        case PInferPredicateGenerator.PruningStatus.KEEP: keep.Add(repr); break;
                        case PInferPredicateGenerator.PruningStatus.STEPBACK: stepback.Add(repr); break;
                        default: break;
                    }
                }
                if (keep.Count > 0)
                {
                    result += 1;
                }
                WriteInvs(codegen, guards, filters, keep, stepback);
            }
            return result;
        }

        public static int InvokeMain(ICompilerConfiguration job, TraceMetadata metadata, Scope globalScope, Hint hint, PInferPredicateGenerator codegen, out int totalInvs)
        {
            ProcessStartInfo startInfo;
            Process process;
            var depsOpt = Path.Combine(GetPInferDependencies(), "*");
            var classpath = Path.Combine(job.OutputDirectory.ToString(), "target", "classes");
            List<string> args = ["-cp",
                    string.Join(":", [depsOpt, classpath]),
                    $"{job.ProjectName}.pinfer.Main"];
            
            List<string> configArgs = GetMinerConfigArgs(job, metadata, hint, codegen);
            if (configArgs == null)
            {
                totalInvs = 0;
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
            // aggregate results
            var numMined = PruneAndAggregate(job, globalScope, codegen, out totalInvs);
            Console.WriteLine("Cleaning up ...");
            var dirInfo = new DirectoryInfo("./");
            foreach (var dir in dirInfo.GetDirectories())
            {
                if (dir.Name.Equals("tmp_daikon_traces"))
                {
                    dir.Delete(true);
                }
            }
            foreach (var f in dirInfo.GetFiles())
            {
                if (f.Name.EndsWith(".inv.gz"))
                {
                    f.Delete();
                }
            }
            return numMined;
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
                foreach (var file in Directory.GetFiles(folder))
                {
                    args.Add(file);
                }
            }
            else
            {
                configuration.Output.WriteWarning($"No trace indexed for this event combination: {string.Join(", ", hint.Quantified.Select(x => x.EventName))}. Skipped ...");
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

    internal sealed class TraceMetadata (ICompilerConfiguration job)
    {
        private readonly TraceIndex traceIndex = new(job.TraceFolder);

        public bool GetTraceFolder(Hint h, out string folder)
        {
            HashSet<string> k = h.Quantified.Select(x => x.EventName).ToHashSet();
            if (h.ConfigEvent != null)
            {
                k.Add(h.ConfigEvent.Name);
            }
            return traceIndex.TryGet(k, out folder);
        }
    }
}