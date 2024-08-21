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

        public void CompilePInferHint(Hint hint)
        {
            Codegen.Reset();
            Codegen.WithHint(hint);
            foreach (var file in Codegen.GenerateCode(Job, GlobalScope))
            {
                Job.Output.WriteFile(file);
            }
            Job.Output.WriteInfo($"Compiling generated code...");
            try
            {
                Codegen.Compile(Job);
            }
            catch (TranslationException e)
            {
                Job.Output.WriteError($"[Compiling Generated Code:]\n" + e.Message);
                Job.Output.WriteError("[THIS SHOULD NOT HAVE HAPPENED, please report it to the P team or create a GitHub issue]\n" + e.Message);
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
            if (Job.Verbose)
            {
                Console.WriteLine("===============================");
                Console.WriteLine("Running the following hint:");
                hint.ShowHint();
                Console.WriteLine("===============================");
            }
            PInferInvoke.InvokeMain(Job, TraceIndex, GlobalScope, hint, Codegen);
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
                CompilePInferHint(h);
                while (h.HasNext(Job, Codegen.MaxArity()))
                {
                    RunSpecMiner(h);
                    h.Next(Job, Codegen.MaxArity());
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

        public void ExploreEventComb(string name, params PEvent[] events)
        {
            // Enusre all events have some payload
            if (events.Select(e => e.PayloadType == PrimitiveType.Null).Any(x => x))
            {
                Job.Output.WriteWarning($"skipping ae_{name} due to empty payload(s)");
                return;
            }
            Hint h = new($"ae_{name}", false, null) {
                Quantified = events.Select(MkEventVar).ToList()
            };
            ParameterSearch(h);
        }

        public void ExploreHandlers(List<State> allStates)
        {
            foreach (var s in allStates)
            {
                // Looking at one state
                foreach (var (@event, handler) in s.AllEventHandlers)
                {
                    ExploreEventComb($"{s.OwningMachine.Name}_{s.Name}_recv_{@event.Name}", @event);
                    if (handler is EventDoAction action && action.Target.CanSend == true)
                    {
                        // Quantifying:
                        // - forall*exists* e_send, e_recv
                        // - forall*exists* e_send, e_send
                        // - no need to look at `e_recv, e_recv` as this might be sent somewhere
                        foreach (var send in action.Target.SendSet)
                        {
                            ExploreEventComb($"{s.OwningMachine.Name}_{s.Name}_recv_{@event.Name}_send_{send.Name}", send, @event);
                            ExploreEventComb($"{s.OwningMachine.Name}_{s.Name}_send_{send.Name}_send_{send.Name}", send, send);
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
                                        ExploreEventComb($"{m2.Name}_{s2.Name}_send_{s2send.Name}_{m1.Name}_{s1.Name}_recv_{recv_1.Name}", s2send, recv_1);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void AutoExplore()
        {
            List<Machine> machines = GlobalScope.Machines.Where(x => !x.IsSpec).ToList();
            // explore single-machine state
            foreach (var m in machines)
            {
                ExploreHandlers(m.AllStates().ToList());
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
            switch (job.PInferAction)
            {
                case PInferAction.Compile:
                    job.Output.WriteInfo($"PInfer - Compile `{givenHint.Name}`");
                    givenHint.ConfigEvent ??= configEvent;
                    new PInferDriver(job, globalScope).CompilePInferHint(givenHint);
                    break;
                case PInferAction.RunHint:
                    job.Output.WriteInfo($"PInfer - Run `{givenHint.Name}`");
                    givenHint.ConfigEvent ??= configEvent;
                    givenHint.PruningLevel = job.PInferPruningLevel;
                    new PInferDriver(job, globalScope).ParameterSearch(givenHint);
                    break;
                case PInferAction.Auto:
                    job.Output.WriteInfo("PInfer - Auto Exploration");
                    new PInferDriver(job, globalScope).AutoExplore();
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
            
            List<string> configArgs = GetMinerConfigArgs(job, metadata, hint, codegen);
            if (configArgs == null)
            {
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
            return process.ExitCode;
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