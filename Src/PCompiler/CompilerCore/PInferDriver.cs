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
        private readonly Dictionary<string, (int, int)> TermPredicateCount = [];
        private int numDistilledInvs = 0;
        private int numTotalInvs = 0;

        public bool CompilePInferHint(Hint hint)
        {
            if (!TraceIndex.GetTraceFolder(hint, out var _))
            {
                Job.Output.WriteWarning($"No trace indexed for this event combination: {string.Join(", ", hint.Quantified.Select(x => x.EventName))}. Skipped ...");
                return false;
            }
            Codegen.Reset();
            Codegen.WithHint(hint);
            foreach (var file in Codegen.GenerateCode(Job, GlobalScope))
            {
                Job.Output.WriteFile(file);
            }
            if (!TermPredicateCount.TryGetValue(hint.Name, out var terms_predicates_cnt))
            {
                terms_predicates_cnt = (-1, -1);
                TermPredicateCount.Add(hint.Name, terms_predicates_cnt);
            }
            if (Codegen.NumTerms == terms_predicates_cnt.Item1 && Codegen.NumPredicates == terms_predicates_cnt.Item2)
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
                return;
            }
            tasks.Add(new($"ae_{name}", false, null) {
                Quantified = events.Select(MkEventVar).ToList()
            });
        }

        public void ExploreFunction(HashSet<Hint> tasks, State s, Function f, PEvent trigger = null)
        {
            if (f == null) return;
            // looking at next state
            // if the current handler sent an event and can move to some other states
            // then there might be some relationships between the send event and
            // listened events in the next states
            foreach (var nextState in f.NextStates)
            {
                foreach (var (recv, h2) in nextState.AllEventHandlers)
                {
                    if (h2 is EventDoAction || h2 is EventGotoState)
                    {
                        foreach (var send in f.SendSet)
                        {
                            AddHint($"{s.OwningMachine.Name}_{s.Name}_send_{send.Name}_then_monitor_{recv.Name}", tasks, send, recv);
                        }
                    }
                }
                // check relationships between send at next state entry and recv in curr function
                if (trigger != null && nextState.Entry != null)
                {
                    foreach (var sendNext in nextState.Entry.SendSet)
                    {
                        AddHint($"{s.OwningMachine.Name}_{nextState.Name}_recved_{trigger.Name}_send_{sendNext.Name}", tasks, sendNext, trigger);
                    }
                }
            }
        }

        public HashSet<Hint> ExploreHandlers(List<State> allStates)
        {
            HashSet<Hint> tasks = new(new Hint.EqualityComparer());
            foreach (var s in allStates)
            {
                // Looking at one state
                // Console.WriteLine($"{s.OwningMachine.Name} @ {s.Name} entry");
                ExploreFunction(tasks, s, s.Entry);
                foreach (var (@event, handler) in s.AllEventHandlers)
                {
                    // Console.WriteLine($"Trigger: {@event.Name} {handler is EventDoAction}");
                    if (handler is EventDoAction action)
                    {
                        // Quantifying:
                        // - forall*exists* e_send, e_recv
                        // - forall*exists* e_send, e_send
                        // - no need to look at `e_recv, e_recv` as this might be sent somewhere
                        foreach (var send in action.Target.SendSet)
                        {
                            // Console.WriteLine($"Send: {send.Name}");
                            AddHint($"{s.OwningMachine.Name}_{s.Name}_recv_{@event.Name}_send_{send.Name}", tasks, send, @event);
                            AddHint($"{s.OwningMachine.Name}_{s.Name}_send_{send.Name}_send_{send.Name}", tasks, send, send);
                        }
                        ExploreFunction(tasks, s, action.Target, trigger: @event);
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
            // foreach (var hint in tasks)
            // {
            //     Console.WriteLine(hint.Name);
            // }
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
                // job.Output.WriteInfo($"\t# invariants distilled: {numInvsDistilled}");
                job.Output.WriteInfo($"... Writing pruned invariants to ./invariants.txt");
                job.Output.WriteInfo($"#Invariants after pruning: {PInferInvoke.WriteRecordTo("invariants.txt")}");
                job.Output.WriteInfo($"\tTime elapsed (seconds): {elapsed}");
            }
        }
    }

    internal class PInferInvoke
    {

        internal static readonly string DistilledInvFile = Path.Combine("PInferOutputs", "distilled_invs.txt");
        internal static readonly string StepbackInvFile = Path.Combine("PInferOutputs", "stepback_invs.txt");
        // map from quantifier headers to guards
        internal static readonly Dictionary<string, List<HashSet<string>>> P = [];
        // map from quantifier headers to filters
        internal static readonly Dictionary<string, List<HashSet<string>>> Q = [];
        internal static readonly Dictionary<string, List<Hint>> Executed = [];
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

        public static IEnumerable<(Hint, HashSet<string>, HashSet<string>)> AllExecuedAndMined()
        {
            foreach (var k in P.Keys)
            {
                for (int i = 0; i < P[k].Count; ++ i)
                {
                    if (P[k][i].Count == 0 && Q[k][i].Count == 0) continue;
                    yield return (Executed[k][i], P[k][i], Q[k][i]);
                }
            }
        }

        public static int WriteRecordTo(string filename)
        {
            using StreamWriter invwrite = new(filename);
            HashSet<string> written = [];
            foreach (var (h, p, q) in PInferInvoke.AllExecuedAndMined())
            {
                if (q.Count == 0) continue;
                var rec = h.GetInvariantReprHeader(string.Join(" ∧ ", p), string.Join(" ∧ ", q));
                if (written.Contains(rec)) continue;
                written.Add(rec);
                invwrite.WriteLine(rec);
            }
            return written.Count;
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
                if (filters.Length > 0 && kept.Length > 0) header += " ∧ ";
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
                if (filters.Length > 0 && stepbacked.Length > 0) header += " ∧ ";
                var inv = header + stepbacked;
                if (!Learned.Contains(inv))
                {
                    invw.WriteLine(inv);
                    Learned.Add(inv);
                }
                invw.Close();
            }
        }

        public static bool Resolution(PInferPredicateGenerator codegen, HashSet<string> p, HashSet<string> q)
        {
            // remove all contradicting predicates in guards
            bool didSth = false;
            HashSet<string> removal = [];
            foreach (var s1 in p)
            {
                foreach (var s2 in q)
                {
                    if (codegen.Contradicting(s1, s2))
                    {
                        removal.Add(s1);
                        removal.Add(s2);
                        didSth = true;
                    }
                }
            }
            p.ExceptWith(removal);
            q.ExceptWith(removal);
            return didSth;
        }

        private static string ShowRecordAt(string k, int i)
        {
            var h = Executed[k][i];
            var p = P[k][i];
            var q = Q[k][i];
            return h.GetInvariantReprHeader(string.Join(" ∧ ", p), string.Join(" ∧ ", q));
        }

        public static void DoChores(PInferPredicateGenerator codegen)
        {
            // iterate through the record and merge/discard any duplicates
            // process till fixpoint
            bool didSth = true;
            while (didSth)
            {
                didSth = false;
                foreach (var k in P.Keys)
                {
                    List<int> removes = [];
                    for (int i = 0; i < P[k].Count; ++i)
                    {
                        var pi = P[k][i];
                        var qi = Q[k][i];
                        for (int j = i + 1; j < P[k].Count; ++j)
                        {
                            var pj = P[k][j];
                            var qj = Q[k][j];
                            // Console.WriteLine($"Check {ShowRecordAt(k, i)} <===> {ShowRecordAt(k, j)}");
                            // Case 1: i ==> j; i.e. pi ==> pj && qj ==> qi
                            // keep j remove i
                            if (pj.IsSubsetOf(pi) && qi.IsSubsetOf(qj))
                            {
                                // Console.WriteLine($"Remove {i}");
                                removes.Add(i);
                            }
                            // Case 2: j ==> i; keep i remove j
                            else if (pi.IsSubsetOf(pj) && qj.IsSubsetOf(qi))
                            {
                                // Console.WriteLine($"Remove {j}");
                                removes.Add(j);
                            }
                            // Case 3: pi <==> pj; merge qi and qj
                            else if (pi.SetEquals(pj))
                            {
                                // merge j to i, remove j
                                foreach (var q in qj)
                                {
                                    qi.Add(q);
                                }
                                // Console.WriteLine($"Merge {i} {j}, remove {j}");
                                removes.Add(j);
                            }
                        }
                    }
                    foreach (var idx in removes.Distinct().OrderByDescending(x => x))
                    {
                        // Console.WriteLine($"RemoveIdx {idx}");
                        P[k].RemoveAt(idx);
                        Q[k].RemoveAt(idx);
                        Executed[k].RemoveAt(idx);
                    }
                    didSth |= removes.Count != 0;
                }
                // Boolean resolution
                foreach (var k in P.Keys)
                {
                    for (int i = 0; i < P[k].Count; ++i)
                    {
                        for (int j = i + 1; j < P[k].Count; ++j)
                        {
                            if (Q[k][i].SetEquals(Q[k][j]))
                            {
                                didSth |= Resolution(codegen, P[k][i], P[k][j]);
                            }
                        }
                    }
                }
            }
        }


        // return the Ps and Qs that should be included to the log
        public static (HashSet<string>, HashSet<string>) UpdateMinedSpecs(ICompilerConfiguration job, PInferPredicateGenerator codegen, Hint hint, HashSet<string> p_prime, HashSet<string> q_prime)
        {
            var quantifiers = hint.GetQuantifierHeader();
            var curr_inv = hint.GetInvariantReprHeader(string.Join(" ∧ ", p_prime), string.Join(" ∧ ", q_prime));
            DoChores(codegen);
            if (P.TryGetValue(quantifiers, out var prevP) && Q.TryGetValue(quantifiers, out var prevQ))
            {
                // first, resolution
                for (int i = 0; i < prevP.Count; ++i)
                {
                    // remove contradicting predicates in both sets if the filters are the same
                    if (prevQ[i].SetEquals(q_prime))
                    {
                        Resolution(codegen, prevP[i], p_prime);
                    }
                }
                for (int i = 0; i < prevP.Count; ++i)
                {
                    var p = prevP[i];
                    var q = prevQ[i];
                    // stronger guards
                    // then only keep filters that
                    // cannot be discovered with weaker guards
                    if (p.IsSubsetOf(p_prime))
                    {
                        q_prime.ExceptWith(q);
                    }
                }
                // can all be captured by some previous invariant
                if (q_prime.Count == 0)
                {
                    job.Output.WriteWarning($"[Subsumed] {curr_inv}");
                    return ([], []);
                }
                // check implication
                for (int i = 0; i < prevP.Count; ++i)
                {
                    var p = prevP[i];
                    var q = prevQ[i];
                    // an invariant is implied by some previous ones if
                    // p_prime -> p && q -> q_prime
                    // might have been captured by previous checks
                    if (p.IsSubsetOf(p_prime) && q_prime.IsSubsetOf(q))
                    {
                        // the new one is subsumed by some previous invariant, so skip adding it to log
                        job.Output.WriteWarning($"[Drop][Subsumed] {curr_inv}");
                        return ([], []);
                    }
                    // same guard, then merge filters
                    if (p.SetEquals(p_prime))
                    {
                        // same guard, then merge q and q_prime; skip the current one
                        foreach (var qs in q_prime)
                        {
                            q.Add(qs);
                        }
                        job.Output.WriteWarning($"[Drop][Merged] {curr_inv}");
                        return ([], []);
                    }
                    // same filter, keep weaker guard if comparable
                    if (q.SetEquals(q_prime))
                    {
                        // same filters, then keep the weaker guard
                        if (p.IsSubsetOf(p_prime))
                        {
                            // existing is weaker, so skip current one
                            job.Output.WriteWarning($"[Drop][StrongerGuards] {curr_inv}");
                            return ([], []);
                        }
                        else if (p_prime.IsSubsetOf(p))
                        {
                            // replace existing one
                            job.Output.WriteWarning($"[Replaced][WeakerGuards] {curr_inv}");
                            prevP[i] = p_prime;
                            return ([], []);
                        }
                    }
                }
            }
            if (!P.ContainsKey(quantifiers)) P[quantifiers] = [];
            if (!Q.ContainsKey(quantifiers)) Q[quantifiers] = [];
            if (!Executed.ContainsKey(quantifiers)) Executed[quantifiers] = [];
            // add the current combination
            P[quantifiers].Add(p_prime);
            Q[quantifiers].Add(q_prime);
            Executed[quantifiers].Add(hint.Copy());
            DoChores(codegen);
            return (p_prime, q_prime);
        }
    
        public static int PruneAndAggregate(ICompilerConfiguration job, Scope globalScope, Hint hint, PInferPredicateGenerator codegen, out int total)
        {
            var parseFilePath = Path.Combine("PInferOutputs", "SpecMining", PreambleConstants.ParseFileName);
            var contents = File.ReadAllLines(parseFilePath);
            int result = 0;
            total = 0;
            for (int i = 0; i < contents.Length; i += 3)
            {
                var guards = contents[i];
                var filters = contents[i + 1];
                var properties = contents[i + 2]
                                .Split("∧")
                                .Where(x => x.Length > 0)
                                .Select(x => x.Trim())
                                // consistent with repr generated by PInfer
                                .Select(x => "(" + x + ")");

                var p = guards.Split("∧").Select(x => x.Trim()).Where(x => x.Length > 0).ToHashSet();
                var q = filters.Split("∧").Select(x => x.Trim()).Where(x => x.Length > 0).ToHashSet();
                total += 1;
                List<string> keep = [];
                List<string> stepback = [];
                foreach (var prop in properties)
                {
                    switch (codegen.CheckForPruning(job, globalScope, prop, out var repr))
                    {
                        case PInferPredicateGenerator.PruningStatus.KEEP: keep.Add(repr); q.Add(repr); break;
                        case PInferPredicateGenerator.PruningStatus.STEPBACK: stepback.Add(repr); break;
                        default: break;
                    }
                }
                var (ps, qs) = UpdateMinedSpecs(job, codegen, hint, p, q);
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
            var numMined = PruneAndAggregate(job, globalScope, hint, codegen, out totalInvs);
            job.Output.WriteWarning($"Currently mined: {Learned.Count} invariant(s)");
            job.Output.WriteWarning($"Currently recorded: {WriteRecordTo("inv_running.txt")} invariant(s)");
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