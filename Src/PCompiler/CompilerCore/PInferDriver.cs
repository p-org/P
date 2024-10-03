using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text.Json;
using Plang.Compiler.Backend;
using Plang.Compiler.Backend.PInfer;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.States;
using Plang.Compiler.TypeChecker.Types;
using Plang.PInfer;

namespace Plang.Compiler
{
    class CompilationOutputs
    {
        private readonly Dictionary<HashSet<PEvent>, HashSet<string>> results = [];
        public CompilationOutputs()
        {
        }

        public void Add(PInferPredicateGenerator codegen)
        {
            if (codegen.hint == null) return;
            HashSet<PEvent> events = codegen.hint.Quantified.Select(x => x.EventDecl).ToHashSet();
            
        }
    }

    class PInferDriver(ICompilerConfiguration job, Scope globalScope, bool checkTrace = true)
    {
        private readonly ICompilerConfiguration Job = job;
        private readonly Scope GlobalScope = globalScope;
        private readonly PInferPredicateGenerator Codegen = (PInferPredicateGenerator)job.Backend;
        private readonly TraceMetadata TraceIndex = new(job, checkTrace);
        private readonly HashSet<Hint> ExploredHints = new(new Hint.EqualityComparer());
        private readonly Dictionary<Hint, HashSet<string>> GeneratedPredicates = new(new Hint.EqualityComparer());
        private readonly Dictionary<Hint, HashSet<string>> GeneratedTerms = new(new Hint.EqualityComparer());
        private readonly List<List<PEvent>> AddedCombinations = [];

        private int numDistilledInvs = 0;
        private int numTotalInvs = 0;
        private int numTotalTasks = 0;

        public bool Converged(Hint h, HashSet<string> predicates, HashSet<string> terms)
        {
            Hint h_prime = h.Copy();
            h_prime.TermDepth -= 1;
            if (GeneratedPredicates.TryGetValue(h_prime, out var preds) && GeneratedTerms.TryGetValue(h_prime, out var tms))
            {
                return preds.SetEquals(predicates) && tms.SetEquals(terms);
            }
            return false;
        }

        public bool CompilePInferHint(Hint hint)
        {
            if (!TraceIndex.GetTraceFolder(hint, out var _) && checkTrace)
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
            Job.Output.WriteInfo($"Compiling generated code...");
            try
            {
                // hint.ShowHint();
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

        public bool RunSpecMiner(Hint hint)
        {
            if (ExploredHints.Contains(hint))
            {
                Job.Output.WriteWarning($"Search space already explored: {hint.Name}, skipping ...");
                return false;
            }
            if (Codegen.hint == null || !Codegen.hint.Equals(hint))
            {
                if (hint.TermDepth == null)
                {
                    hint.TermDepth = 0;
                }
                CompilePInferHint(hint);
            }
            if (Converged(hint, Codegen.GeneratedPredicates, Codegen.GeneratedTerms))
            {
                Job.Output.WriteWarning($"Converged: {hint.Name} @ term depth {hint.TermDepth - 1}, skipping ...");
                return false;
            }
            var cp = hint.Copy();
            GeneratedPredicates[cp] = Codegen.GeneratedPredicates;
            GeneratedTerms[cp] = Codegen.GeneratedTerms;
            ExploredHints.Add(cp);
            if (cp.ConfigEvent != null)
            {
                cp = cp.Copy();
                cp.ConfigEvent = null;
                // if config event is not null, searching the hint without config event
                // is also covered in this iteration.
                ExploredHints.Add(cp);
            }
            Console.WriteLine("===============Running================");
            hint.ShowHint();
            numDistilledInvs += PInferInvoke.InvokeMain(Job, TraceIndex, GlobalScope, hint, Codegen, out int totalTasks);
            numTotalInvs = PInferInvoke.Recorded.Count;
            numTotalTasks = totalTasks;
            Console.WriteLine("=================Done=================");
            return true;
        }

        public void ParameterSearch(Hint hint)
        {
            Console.WriteLine($"Search {hint.Name}");
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
                        if (!RunSpecMiner(h)) break;
                        h.Next(Job, Codegen.MaxArity());
                    }
                }
                else
                {
                    Job.Output.WriteInfo($"Finishing {h.Name} ...");
                }
            }
            ExploredHints.Add(hint.Copy());
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
            var eventSet = events.ToList();
            if (AddedCombinations.Any(x => x.SequenceEqual(eventSet)))
            {
                Job.Output.WriteWarning($"skipping ae_{name} as its combination has been added");
                return;
            }
            AddedCombinations.Add(eventSet);
            tasks.Add(new($"ae_{name}", false, null) {
                Quantified = events.Select(MkEventVar).ToList()
            });
        }

        public void ExploreFunction(HashSet<Hint> tasks, State s, Function f, PEvent trigger = null)
        {
            if (f == null) return;
            // Look at recv inside function
            foreach (var e1 in f.SendSet)
            {
                foreach (var recv in f.RecvSet)
                {
                    AddHint($"{s.OwningMachine}_{s.Name}_sent_{e1.Name}_block_on_recv_{recv.Name}", tasks, e1, recv);
                }
            }
            if (trigger != null)
            {
                foreach (var recv in f.RecvSet)
                {
                    AddHint($"{s.OwningMachine}_{s.Name}_blocked_on_{recv.Name}_triggered_by_{trigger.Name}", tasks, recv, trigger);
                }
            }
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
                        AddHint($"{s.OwningMachine.Name}_{s.Name}_on_{trigger.Name}_goto_{nextState.Name}_send_{sendNext.Name}_on_entry", tasks, sendNext, trigger);
                    }
                    foreach (var recvNext in nextState.Entry.RecvSet)
                    {
                        AddHint($"{s.OwningMachine.Name}_{s.Name}_on_{trigger.Name}_goto_{nextState.Name}_recv_{recvNext.Name}_on_entry", tasks, recvNext, trigger);
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
                ExploreFunction(tasks, s, s.Entry);
                if (s.Entry != null)
                {
                    foreach (var e in s.Entry.SendSet)
                    {
                        AddHint($"{s.OwningMachine.Name}_{s.Name}_entry_sends_{e.Name}", tasks, e);
                        AddHint($"{s.OwningMachine.Name}_{s.Name}_entry_sends_{e.Name}_binary", tasks, e, e);
                    }
                }
                foreach (var (@event, handler) in s.AllEventHandlers)
                {
                    // Console.WriteLine($"Trigger: {@event.Name} {handler is EventDoAction}");
                    AddHint($"{s.OwningMachine.Name}_{s.Name}_recv_{@event.Name}", tasks, @event);
                    if (handler is EventDoAction action)
                    {
                        // Quantifying:
                        // - forall*exists* e_send, e_recv
                        // - forall*exists* e_send, e_send
                        // - no need to look at `e_recv, e_recv` as this might be sent somewhere
                        foreach (var send in action.Target.SendSet)
                        {
                            // Console.WriteLine($"Send: {send.Name}");
                            AddHint($"{s.OwningMachine.Name}_{s.Name}_sends_{send.Name}", tasks, send);
                            AddHint($"{s.OwningMachine.Name}_{s.Name}_recvs_{@event.Name}_sends_{send.Name}", tasks, send, @event);
                            AddHint($"{s.OwningMachine.Name}_{s.Name}_sends_{send.Name}_binary", tasks, send, send);
                        }
                        ExploreFunction(tasks, s, action.Target, trigger: @event);
                    }
                    else if (handler is EventGotoState gotoAction)
                    {
                        var nextState = gotoAction.Target;
                        var trigger = gotoAction.Trigger;
                        // look at entry of next state
                        if (nextState.Entry != null)
                        {
                            // if there is recv, only look at recv
                            if (nextState.Entry.RecvSet.Any())
                            {
                                foreach (var recv in nextState.Entry.RecvSet)
                                {
                                    AddHint($"{s.OwningMachine.Name}_{s.Name}_on_{trigger.Name}_goto_{nextState.Name}_recv_{recv.Name}_on_entry", tasks, recv, trigger);
                                }
                            }
                            else
                            {
                                foreach (var send in nextState.Entry.SendSet)
                                {
                                    AddHint($"{s.OwningMachine.Name}_{s.Name}_on_{trigger.Name}_goto_{nextState.Name}_send_{send.Name}_on_entry", tasks, send, trigger);
                                }
                            }
                        }
                    }
                }
            }
            return tasks;
        }

        public void AutoExplore(bool hintsOnly)
        {
            List<Machine> machines = GlobalScope.Machines.Where(x => !x.IsSpec).ToList();
            // explore single-machine state
            HashSet<Hint> tasks = new(new Hint.EqualityComparer());
            foreach (var hint in GlobalScope.Hints)
            {
                Job.Output.WriteWarning($"Running user-defined hint: {hint.Name}");
                ParameterSearch(hint);
            }
            if (hintsOnly)
            {
                return;
            }
            foreach (var m in machines)
            {
                tasks.UnionWith(ExploreHandlers(m.AllStates().ToList()));
            }
            Job.Output.WriteInfo("Event combinations:");
            foreach (var task in tasks)
            {
                Console.WriteLine(string.Join(", ", task.RelatedEvents().Select(x => x.Name)));
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
            int numTotalTasks = 0;
            PInferDriver driver = null;
            stopwatch.Start();
            switch (job.PInferAction)
            {
                case PInferAction.Compile:
                    job.Output.WriteInfo($"PInfer - Compile `{givenHint.Name}`");
                    givenHint.ConfigEvent ??= configEvent;
                    new PInferDriver(job, globalScope, checkTrace: false).CompilePInferHint(givenHint);
                    break;
                case PInferAction.RunHint:
                {
                    job.Output.WriteInfo($"PInfer - Run `{givenHint.Name}`");
                    givenHint.ConfigEvent ??= configEvent;
                    givenHint.PruningLevel = job.PInferPruningLevel;
                    driver = new PInferDriver(job, globalScope);
                    driver.ParameterSearch(givenHint);
                    numTotalTasks = driver.numTotalTasks;
                    break;
                }
                case PInferAction.Auto:
                {
                    job.Output.WriteInfo("PInfer - Auto Exploration");
                    driver = new PInferDriver(job, globalScope);
                    driver.AutoExplore(job.HintsOnly);
                    numTotalTasks = driver.numTotalTasks;
                    break;
                }
            }
            stopwatch.Stop();
            if (job.PInferAction == PInferAction.RunHint || job.PInferAction == PInferAction.Auto)
            {
                job.Output.WriteInfo($"... Writing pruned invariants to ./invariants_{driver.TraceIndex.GetTraceCount()}.txt");
                var elapsed = stopwatch.ElapsedMilliseconds / 1000.0;
                job.Output.WriteInfo($"PInfer statistics");
                job.Output.WriteInfo($"\t# invariants discovered: {PInferInvoke.NumInvsMined}");
                // job.Output.WriteInfo($"\t# invariants distilled: {numInvsDistilled}");
                var numInvAfterPruning = PInferInvoke.WriteRecordTo($"invariants_{driver.TraceIndex.GetTraceCount()}.txt");
                job.Output.WriteInfo($"\t#Invariants after pruning: {numInvAfterPruning}");
                job.Output.WriteInfo($"#Times executed by Daikon: {numTotalTasks}");
                job.Output.WriteInfo($"\tTime elapsed (seconds): {elapsed}");
                PInferStats stats = new() {
                    NumInvsTotal = PInferInvoke.NumInvsMined,
                    NumInvsPrunedBySubsumption = PInferInvoke.NumInvsMined - PInferInvoke.NumInvsPrunedByGrammar - numInvAfterPruning,
                    NumInvsPrunedByGrammar = PInferInvoke.NumInvsPrunedByGrammar,
                    TimeElapsed = elapsed
                };
                File.WriteAllText($"pinfer_stats_{driver.TraceIndex.GetTraceCount()}.json", JsonSerializer.Serialize(stats));
                job.Output.WriteInfo("\tWriting monitors to PInferSpecs ...");
                PInferInvoke.WriteMonitors(driver.Codegen, new(new(job)), globalScope);
            }
        }
    }

    internal class PInferStats {
        public int NumInvsTotal { get; set; }
        public int NumInvsPrunedBySubsumption { get; set; }
        public int NumInvsPrunedByGrammar { get; set; }
        public double TimeElapsed { get; set; }
    }

    internal class PInferInvoke
    {

        internal static readonly string DistilledInvFile = Path.Combine("PInferOutputs", "distilled_invs.txt");
        internal static readonly string StepbackInvFile = Path.Combine("PInferOutputs", "stepback_invs.txt");
        // map from quantifier headers to guards
        internal static readonly Dictionary<string, List<HashSet<string>>> P = [];
        // map from quantifier headers to filters
        internal static readonly Dictionary<string, List<HashSet<string>>> Q = [];
        internal static readonly Dictionary<string, Dictionary<string, IPExpr>> ParsedP = [];
        internal static readonly Dictionary<string, Dictionary<string, IPExpr>> ParsedQ = [];
        internal static readonly Dictionary<string, List<Hint>> Executed = [];
        internal static readonly Dictionary<string, int> NumExists = [];
        internal static readonly Dictionary<string, List<PEvent>> Quantified = [];
        internal static HashSet<string> Learned = [];
        internal static HashSet<string> Recorded = [];
        internal static int NumInvsMined = 0;
        internal static int NumInvsPrunedByGrammar = 0;

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

        public static IEnumerable<(string, int, int, Hint, HashSet<string>, HashSet<string>)> AllExecuedAndMined()
        {
            foreach (var k in P.Keys)
            {
                for (int i = 0; i < P[k].Count; ++ i)
                {
                    if (P[k][i].Count == 0 && Q[k][i].Count == 0) continue;
                    yield return (k, NumExists[k], i, Executed[k][i], P[k][i], Q[k][i]);
                }
            }
        }

        public static void WriteMonitors(PInferPredicateGenerator codegen, Transform transform, Scope globalScope)
        {
            Dictionary<string, int> monitorCount = [];
            int c = 1;
            foreach (var (key, _, _, h, p, q) in AllExecuedAndMined())
            {
                if (q.Count == 0) continue;
                var ps = string.Join(" ∧ ", p);
                monitorCount[h.Name] = monitorCount.TryGetValue(h.Name, out var cnt) ? cnt + 1 : 1;
                var prop = h.GetInvariantReprHeader(ps, string.Join(" ∧ ", q));
                CompiledFile monitorFile = new($"{h.Name}_{monitorCount[h.Name]}.p", Path.Combine(transform.context.Job.OutputDirectory.ToString(), "PInferSpecs"));
                try
                {
                    transform.WithFile(monitorFile);
                    transform.WriteSpecMonitor(c++, codegen, transform.context, transform.context.Job, globalScope, h, p, q, ParsedP[key], ParsedQ[key], prop);
                    transform.context.Job.Output.WriteFile(monitorFile);
                }
                catch (Exception e)
                {
                    transform.context.Job.Output.WriteError($"Error writing monitor for {h.Name}:\nInvariant: {prop}\n{e.Message}\n{e.StackTrace}");
                    continue;
                }
            }
        }

        public static int WriteRecordTo(string filename)
        {
            using StreamWriter invwrite = new(filename);
            HashSet<string> written = [];
            foreach (var (_, _, _, h, p, q) in PInferInvoke.AllExecuedAndMined())
            {
                if (q.Count == 0) continue;
                var ps = string.Join(" ∧ ", p);
                var rec = h.GetInvariantReprHeader(ps, string.Join(" ∧ ", q));
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
            // Learned.Clear();
            if (keep.Count > 0 || filters.Length > 0)
            {
                using StreamWriter invw = File.AppendText(DistilledInvFile);
                var kept = string.Join(" ∧ ", keep);
                if (filters.Length > 0 && kept.Length > 0) header += " ∧ ";
                var inv = header + kept;
                Recorded.Add(inv);
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
                Recorded.Add(inv);
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
                    if (codegen.Negating(s1, s2))
                    {
                        if (p.Except([s1]).ToHashSet().SetEquals(q.Except([s2])))
                        {
                            removal.Add(s1);
                            removal.Add(s2);
                            didSth = true;
                        }
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

        private static void RemoveRecordAt(string k, int i)
        {
            Executed[k].RemoveAt(i);
            P[k].RemoveAt(i);
            Q[k].RemoveAt(i);
        }

        private static void ShowAll()
        {
            foreach (var k in P.Keys)
            {
                for (int i = 0; i < P[k].Count; ++i)
                {
                    Console.WriteLine(ShowRecordAt(k, i));
                }
            }
        }

        public static bool ClearUpExistentials()
        {
            bool didSth = false;
            foreach (var k in Q.Keys)
            {
                int kExists = NumExists[k];
                var quantifiedEvents = Quantified[k];
                for (int i = 0; i < Q[k].Count; ++i)
                {
                    var qs = Q[k][i];
                    // check specs with more forall-quantifiers
                    // e.g. if forall* P holds
                    // then forall*exists* P is trivially true
                    // we remove P from forall*exists* in this case
                    HashSet<int> removal = [];
                    foreach (var k1 in P.Keys)
                    {
                        if (!quantifiedEvents.SequenceEqual(Quantified[k1])) continue;
                        int k1Exists = NumExists[k1];
                        if (k1Exists < kExists)
                        {
                            for (int j = 0; j < Q[k1].Count; ++j)
                            {
                                if (qs.SetEquals(Q[k1][j]))
                                {
                                    removal.Add(i);
                                }   
                            }
                        }
                    }
                    foreach (var j in removal.OrderByDescending(x => x))
                    {
                        RemoveRecordAt(k, j);
                    }
                    didSth |= removal.Count > 0;
                }
            }
            return didSth;
        }

        public static void DoChores(ICompilerConfiguration job, PInferPredicateGenerator codegen)
        {
            // iterate through the record and merge/discard any duplicates
            // process till fixpoint
            bool didSth = true;
            while (didSth)
            {
                didSth = false;
                foreach (var k in P.Keys)
                {
                    int numExists = NumExists[k];
                    HashSet<int> removes = [];
                    for (int i = 0; i < P[k].Count; ++i)
                    {
                        var pi = P[k][i];
                        var qi = Q[k][i];
                        if (qi.Count == 0)
                        {
                            removes.Add(i);
                            continue;
                        }
                        if (qi.IsSubsetOf(pi))
                        {
                            var rec = ShowRecordAt(k, i);
                            job.Output.WriteWarning($"[Chores][Remove-Tauto] {rec}");
                            removes.Add(i);
                            continue;
                        }
                        for (int j = i + 1; j < P[k].Count; ++j)
                        {
                            var pj = P[k][j];
                            var qj = Q[k][j];
                            if (pi.SetEquals(pj) && numExists == 0)
                            {
                                // can only merge when there is
                                // no existential quantifications
                                var rec = ShowRecordAt(k, j);
                                job.Output.WriteWarning($"[Chores][Merge-Remove] {rec}; merged with {ShowRecordAt(k, i)}");
                                qi.UnionWith(qj);
                                removes.Add(j);
                            }
                            // Forall-only rules
                            // Case 1: i ==> j; i.e. pi ==> pj && qj ==> qi
                            // keep j remove i
                            else if (pj.IsSubsetOf(pi) && qi.IsSubsetOf(qj))
                            {
                                // Console.WriteLine($"Remove {i}");
                                job.Output.WriteWarning($"[Chores][Remove] {ShowRecordAt(k, i)} implied by {ShowRecordAt(k, j)}");
                                removes.Add(i);
                            }
                            // Case 2: j ==> i; keep i remove j
                            else if (pi.IsSubsetOf(pj) && qj.IsSubsetOf(qi))
                            {
                                // Console.WriteLine($"Remove {j}");
                                job.Output.WriteWarning($"[Chores][Remove] {ShowRecordAt(k, j)} implied by {ShowRecordAt(k, i)}");
                                removes.Add(j);
                            }
                            // Case 3: if i ==> j, then any thing holds under j also holds under i
                            // we may remove those from pi
                            // e.g. forall* P -> Q, moreover P -> R
                            // if it is the case that forall* R -> Q, we remove Q for the stronger guards P
                            // i.e. keeping the weakest guard for Q
                            else if (pj.IsSubsetOf(pi))
                            {
                                if (qi.Intersect(qj).Any() && numExists == 0)
                                {
                                    job.Output.WriteWarning($"[Chores][Remove] common filters from {ShowRecordAt(k, i)} that is also in {ShowRecordAt(k, j)}");
                                    qi.ExceptWith(qj);
                                    didSth = true;
                                }
                            }
                            else if (pi.IsSubsetOf(pj))
                            {
                                if (qj.Intersect(qi).Any() && numExists == 0)
                                {
                                    job.Output.WriteWarning($"[Chores][Remove] common filters from {ShowRecordAt(k, j)} that is also in {ShowRecordAt(k, i)}");
                                    qj.ExceptWith(qi);
                                    didSth = true;
                                }
                            }
                        }
                    }
                    foreach (var idx in removes.OrderByDescending(x => x))
                    {
                        RemoveRecordAt(k, idx);
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
                didSth |= ClearUpExistentials();
            }
        }

        // return the Ps and Qs that should be included to the log
        public static (HashSet<string>, HashSet<string>) UpdateMinedSpecs(ICompilerConfiguration job, PInferPredicateGenerator codegen, Hint hint, HashSet<string> p_prime, HashSet<string> q_prime, Dictionary<string, IPExpr> parsedP, Dictionary<string, IPExpr> parsedQ)
        {
            var quantifiers = hint.GetQuantifierHeader();
            var curr_inv = hint.GetInvariantReprHeader(string.Join(" ∧ ", p_prime), string.Join(" ∨ ", q_prime.Select(x => string.Join(" ∧ ", x))));
            int numExists = hint.ExistentialQuantifiers;
            List<PEvent> quantifiedEvents = hint.Quantified.Select(x => x.EventDecl).ToList();
            if (!P.ContainsKey(quantifiers)) P[quantifiers] = [];
            if (!Q.ContainsKey(quantifiers)) Q[quantifiers] = [];
            if (!Executed.ContainsKey(quantifiers)) Executed[quantifiers] = [];
            if (!ParsedP.ContainsKey(quantifiers)) ParsedP[quantifiers] = [];
            if (!ParsedQ.ContainsKey(quantifiers)) ParsedQ[quantifiers] = [];
            // add the current combination
            P[quantifiers].Add(p_prime);
            Q[quantifiers].Add(q_prime);
            Executed[quantifiers].Add(hint.Copy());
            NumExists[quantifiers] = numExists;
            Quantified[quantifiers] = quantifiedEvents;
            foreach (var (k, v) in parsedP)
            {
                ParsedP[quantifiers][k] = v;
            }
            foreach (var (k, v) in parsedQ)
            {
                ParsedQ[quantifiers][k] = v;
            }
            DoChores(job, codegen);
            return (p_prime, q_prime);
        }
    
        public static int PruneAndAggregate(ICompilerConfiguration job, Scope globalScope, Hint hint, PInferPredicateGenerator codegen, out int total)
        {
            var parseFilePath = Path.Combine("PInferOutputs", "SpecMining", PreambleConstants.ParseFileName);
            var contents = File.ReadAllLines(parseFilePath);
            int result = 0;
            total = int.Parse(contents[^1]);
            for (int i = 0; i < contents.Length - 1; i += 3)
            {
                var guards = contents[i];
                var filters = contents[i + 1];
                var properties = contents[i + 2]
                                .Split("∧")
                                .Where(x => x.Length > 0)
                                .Select(x => x.Trim());

                var p = guards.Split("∧").Select(x => x.Trim()).Where(x => x.Length > 0).ToHashSet();
                var q = filters.Split("∧").Select(x => x.Trim()).Where(x => x.Length > 0).ToHashSet();
                NumInvsMined += 1;
                List<string> keep = [];
                List<string> stepback = [];
                Dictionary<string, IPExpr> parsedP = [];
                Dictionary<string, IPExpr> parsedQ = [];
                foreach (var prop in properties)
                {
                    switch (codegen.CheckForPruning(job, globalScope, prop, out var repr, out var parsed))
                    {
                        case PInferPredicateGenerator.PruningStatus.KEEP: keep.Add(repr); q.Add(repr); parsedQ[repr] = parsed; break;
                        case PInferPredicateGenerator.PruningStatus.STEPBACK: stepback.Add(repr); break;
                        default: break;
                    }
                }
                if (q.Count > 0)
                {
                    foreach (var g in p)
                    {
                        if (codegen.TryParseToExpr(job, globalScope, g, out var parsed))
                        {
                            parsedP[g] = parsed;
                        }
                        else
                        {
                            throw new Exception($"[ERROR] Guard {g} cannot be parsed");
                        }
                    }
                    foreach (var f in keep)
                    {
                        if (codegen.TryParseToExpr(job, globalScope, f, out var parsed))
                        {
                            parsedQ[f] = parsed;
                        }
                        else
                        {
                            throw new Exception($"[ERROR] Filter {f} cannot be parsed");
                        }
                    }
                    UpdateMinedSpecs(job, codegen, hint, p, q, parsedP, parsedQ);
                }
                else
                {
                    NumInvsPrunedByGrammar += 1;
                }
                if (keep.Count > 0)
                {
                    result += 1;
                }
                WriteInvs(codegen, guards, filters, keep, stepback);
            }
            return result;
        }

        public static int InvokeMain(ICompilerConfiguration job, TraceMetadata metadata, Scope globalScope, Hint hint, PInferPredicateGenerator codegen, out int totalTasks)
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
                totalTasks = 0;
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
            var numMined = PruneAndAggregate(job, globalScope, hint, codegen, out totalTasks);
            DoChores(job, codegen);
            // ShowAll();
            job.Output.WriteWarning($"Currently mined: {Recorded.Count} invariant(s)");
            job.Output.WriteWarning($"Currently recorded: {WriteRecordTo($"inv_running_{metadata.GetTraceCount()}.txt")} invariant(s)");
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

    internal sealed class TraceMetadata (ICompilerConfiguration job, bool checkTrace = true)
    {
        private readonly TraceIndex traceIndex = new(job.TraceFolder, checkTrace: checkTrace);

        public int GetTraceCount()
        {
            return traceIndex.GetCount();
        }

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