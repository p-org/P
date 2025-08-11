using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Z3;
using Plang.Compiler.Backend;
using Plang.Compiler.Backend.PInfer;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.AST.States;
using Plang.Compiler.TypeChecker.Types;
using Plang.PInfer;

namespace Plang.Compiler
{
    class CompilationOutputs
    {
        private readonly Dictionary<HashSet<Event>, HashSet<string>> results = [];
        public CompilationOutputs()
        {
        }

        public void Add(PInferPredicateGenerator codegen)
        {
            if (codegen.hint == null) return;
            HashSet<Event> events = codegen.hint.Quantified.Select(x => x.EventDecl).ToHashSet();
            
        }
    }

    class PInferDriver(ICompilerConfiguration job, Scope globalScope, string invOutDir, bool checkTrace = true)
    {
        private readonly ICompilerConfiguration Job = job;
        private readonly Scope GlobalScope = globalScope;
        private readonly PInferPredicateGenerator Codegen = (PInferPredicateGenerator)job.Backend;
        private readonly TraceMetadata TraceIndex = new(job, checkTrace);
        private readonly HashSet<Hint> ExploredHints = new(new Hint.EqualityComparer());
        private readonly Dictionary<Hint, HashSet<string>> GeneratedPredicates = new(new Hint.EqualityComparer());
        private readonly Dictionary<Hint, HashSet<string>> GeneratedTerms = new(new Hint.EqualityComparer());
        private readonly List<List<Event>> AddedCombinations = [];

        private int numDistilledInvs = 0;
        private int numTotalInvs = 0;
        private int numTotalTasks = 0;
        private int numEventCombinations = 0;
        private readonly string InvOutputDir = invOutDir;
        private double tSearchEventCombination = 0.0;
        private double tCandidateTemplateGen = 0.0;
        private double tMining = 0.0;
        private double tPruning = 0.0;
        private Stopwatch globalTimer = new();
        private double tLearnGoals = -1.0;
        private int NumGoals = 0;

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
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Codegen.Reset();
            Codegen.WithHint(hint);
            foreach (var file in Codegen.GenerateCode(Job, GlobalScope))
            {
                Job.Output.WriteFile(file);
            }
            stopwatch.Stop();
            tCandidateTemplateGen += stopwatch.ElapsedMilliseconds;
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
            GeneratedPredicates[cp] = new(Codegen.GeneratedPredicates);
            GeneratedTerms[cp] = new(Codegen.GeneratedTerms);
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
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            numDistilledInvs += PInferInvoke.InvokeMain(Job, TraceIndex, GlobalScope, hint, Codegen, InvOutputDir, out int totalTasks);
            stopwatch.Stop();
            tMining += stopwatch.ElapsedMilliseconds / 1000.0;
            numTotalInvs = PInferInvoke.Recorded.Count;
            numTotalTasks = totalTasks;
            Console.WriteLine("=================Done=================");
            PInferInvoke.CheckLearnedGoals(GlobalScope, Job, Codegen, PInferInvoke.AllExecuedAndMined(), out var _);
            PInferInvoke.CheckLearnedGoals(GlobalScope, Job, Codegen, PInferInvoke.AllExecuedAndMined(), out var _, checkIndInv: true);
            if (PInferInvoke.NumGoalsLearnedWithHints == NumGoals)
            {
                globalTimer.Stop();
                tLearnGoals = globalTimer.ElapsedMilliseconds / 1000.0;
            }
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

        public PEventVariable MkEventVar(Event e, int i)
        {
            return new PEventVariable($"e{i}") {
                EventDecl = e, Type = e.PayloadType, Order = i
            };
        }

        public void AddHint(string name, HashSet<Hint> tasks, params Event[] events)
        {
            // Enusre all events have some payload
            foreach (var e in events)
            {
                if (Job.ConfigEvent != null && e.Name == Job.ConfigEvent)
                {
                    return;
                }
            }
            name = name.Replace(" ", "_").Replace(".", "_");
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

        public void ExploreFunction(HashSet<Hint> tasks, State s, Function f, Event trigger = null)
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
                        if (action.Target.SendSet.Count() > 1)
                        {
                            // look at pairs of sends
                            var sends = action.Target.SendSet.ToList();
                            for (int i = 0; i < sends.Count; ++i)
                            {
                                for (int j = i + 1; j < sends.Count; ++j)
                                {
                                    AddHint($"{s.OwningMachine.Name}_{s.Name}_sends_multi_{sends[i].Name}_{sends[j].Name}", tasks, sends[i], sends[j]);
                                }
                            }
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
            Event configEvent = Job.ConfigEvent != null ? GetPEvent(Job.ConfigEvent) : null;
            HashSet<Hint> tasks = new(new Hint.EqualityComparer());
            HashSet<string> hintQuantifierHeaders = [.. GlobalScope.Hints.Select(x => x.GetQuantifierHeader())];
            foreach (var hint in GlobalScope.Hints.Where(x => !x.Ignore))
            {
                Job.Output.WriteWarning($"Running user-defined hint: {hint.Name}");
                hint.ConfigEvent ??= configEvent;
                ParameterSearch(hint);
                if (TraceIndex.GetTraceFolder(hint, out var _))
                {
                    numEventCombinations += 1;
                }
            }
            if (hintsOnly)
            {
                return;
            }
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var m in machines)
            {
                tasks.UnionWith(ExploreHandlers(m.AllStates().ToList()));
            }
            // remove user-provided hints that are already explored
            tasks = tasks.Where(x => !hintQuantifierHeaders.Contains(x.GetQuantifierHeader())).ToHashSet();
            stopwatch.Stop();
            tSearchEventCombination = stopwatch.ElapsedMilliseconds;
            Job.Output.WriteInfo("Event combinations:");
            foreach (var task in tasks)
            {
                Console.WriteLine(string.Join(", ", task.RelatedEvents().Select(x => x.Name)));
                task.ConfigEvent ??= configEvent;
                if (TraceIndex.GetTraceFolder(task, out var _))
                {
                    numEventCombinations += 1;
                }
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

        internal Event GetPEvent(string name)
        {
            if (GlobalScope.Lookup(name, out Event e))
            {
                return e;
            }
            Job.Output.WriteError($"Event not found when parsing headers: {name}");
            Environment.Exit(1);
            return null;
        }

        private List<(List<PEventVariable>, Event, int, int, bool)> ParseHeaders()
        {
            var dir = Job.InvParseFileDir;
            var headerFile = Path.Combine(dir, "parsable_headers.txt");
            if (!File.Exists(headerFile))
            {
                Job.Output.WriteError($"Inv headers not found: {headerFile}");
                Environment.Exit(1);
            }
            var lines = File.ReadAllLines(headerFile);
            List<(List<PEventVariable>, Event, int, int, bool)> headers = [];
            for (int i = 0; i < lines.Length; i += 5)
            {
                if (lines[i] == "") continue;
                var quantifiedEventsVars = lines[i].Split(" ").ToList();
                var quantified = quantifiedEventsVars.Select(x => {
                    var parts = x.Split(":");
                    var eventDecl = GetPEvent(parts[1]);
                    return new PEventVariable(parts[0]) {
                        EventDecl = eventDecl, Type = eventDecl.PayloadType
                    };
                }).ToList();
                var configEvent = lines[i + 1] == "" ? null : GetPEvent(lines[i + 1]);
                var nexists = int.Parse(lines[i + 2]);
                var termDepth = int.Parse(lines[i + 3]);
                var userHint = bool.Parse(lines[i + 4]);
                headers.Add((quantified, configEvent, nexists, termDepth, userHint));
            }
            return headers;
        }

        public void RunPruningSteps()
        {
            var headers = ParseHeaders();
            var invParsableAll = Path.Combine(Job.InvParseFileDir, $"all_{PreambleConstants.ParseFileName}");
            if (!File.Exists(invParsableAll))
            {
                Job.Output.WriteError($"Inv parsable file not found: {invParsableAll}");
                Environment.Exit(1);
            }
            var invParsable = File.ReadAllLines(invParsableAll);
            string[] failedGuards = [];
            if (Path.Exists(Path.Combine(Job.InvParseFileDir, "failed_guards.txt")))
            {
                failedGuards = File.ReadAllLines(Path.Combine(Job.InvParseFileDir, "failed_guards.txt"));
            }
            int ptr = 0;
            int failedGuardsPtr = 0;
            var allCustomFunctions = GlobalScope.Hints.SelectMany(x => x.CustomFunctions).ToHashSet().ToList();
            List<string> notBadInvs = [];
            Dictionary<string, List<HashSet<string>>> notBadInvPredicates = [];
            Dictionary<string, Hint> headerToHint = [];
            foreach (var (quantified, configEvent, nexists, termDepth, userHint) in headers)
            {
                Hint h = new("pruning", false, null) {
                    Quantified = quantified,
                    ConfigEvent = configEvent,
                    ExistentialQuantifiers = nexists,
                    TermDepth = termDepth,
                    UserHint = userHint,
                    CustomFunctions = allCustomFunctions,
                };
                headerToHint[h.GetQuantifierHeader()] = h;
                if (!notBadInvPredicates.ContainsKey(h.GetQuantifierHeader()))
                {
                    notBadInvPredicates[h.GetQuantifierHeader()] = new();
                }
                Codegen.Reset();
                Codegen.WithHint(h);
                Codegen.GenerateCode(Job, GlobalScope);
                List<string> currentInvs = [];
                while (ptr < invParsable.Length - 1 && !invParsable[ptr].StartsWith("EOT"))
                {
                    currentInvs.Add(invParsable[ptr++]);
                }
                PInferInvoke.PruneAndAggregate(Job, GlobalScope, h, Codegen, [.. currentInvs], out var t);
                if (nexists == 0)
                {
                    while (failedGuardsPtr < failedGuards.Length - 1 && !failedGuards[failedGuardsPtr].StartsWith("EOT"))
                    {
                        var line = failedGuards[failedGuardsPtr++];
                        notBadInvPredicates[h.GetQuantifierHeader()].Add([.. line.Split("∧").Select(x => x.Trim()).Where(x => x.Length > 0)]);
                    }
                }
                numTotalTasks += t;
                if (ptr < invParsable.Length - 1) ptr++;
                else break;
                if (failedGuardsPtr < failedGuards.Length - 1) failedGuardsPtr++;
            }
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            PInferInvoke.DoChores(Job, Codegen);
            // fast pruning
            foreach (var (k, parsedFailedGuards) in notBadInvPredicates)
            {
                HashSet<int> remove = [];
                for (int i = 0; i < parsedFailedGuards.Count; ++i)
                {
                    if (remove.Contains(i)) continue;
                    for (int j = i + 1; j < parsedFailedGuards.Count; ++j)
                    {
                        if (parsedFailedGuards[i].IsSubsetOf(parsedFailedGuards[j]))
                        {
                            remove.Add(j);
                        }
                        else if (parsedFailedGuards[j].IsSubsetOf(parsedFailedGuards[i]))
                        {
                            remove.Add(i);
                        }
                    }
                }
                foreach (var r in remove.OrderByDescending(x => x))
                {
                    parsedFailedGuards.RemoveAt(r);
                }
            }
            foreach (var (k, gs) in notBadInvPredicates)
            {
                // for each G in fg, we have the following invariant:
                // forall e1, e2...en. not G(e1, e2, ..., en)
                Hint h = headerToHint[k];
                foreach (var g in gs)
                {
                    notBadInvs.Add($"{h.GetInvariantReprHeader("", $"!({string.Join(" ∧ ", g)})")}");
                }
            }
            stopwatch.Stop();
            tPruning = stopwatch.ElapsedMilliseconds;
            File.WriteAllLines("not_bad_invs.txt", notBadInvs);
        }

        private void InitializeZ3()
        {
            PInferInvoke.Z3Wrapper = new(GlobalScope, Codegen);
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
            Event configEvent = null;
            if (job.ConfigEvent != null)
            {
                if (globalScope.Lookup(job.ConfigEvent, out Event e))
                {
                    configEvent = e;
                }
                else
                {
                    job.Output.WriteError($"Config event passed through command line not defined: {job.ConfigEvent}");
                }
            }
            PInferInvoke.NewInvFiles();
            var invOutDir = "SpecMining";
            if (Directory.Exists("PInferOutputs"))
            {
                invOutDir = $"SpecMining_{Directory.GetDirectories("PInferOutputs").Length}";
            }
            var stopwatch = new Stopwatch();
            int numTotalTasks = 0;
            PInferDriver driver = null;
            PInferInvoke.UseZ3 = job.UseZ3;
            stopwatch.Start();
            switch (job.PInferAction)
            {
                case PInferAction.Compile:
                    job.Output.WriteInfo($"PInfer - Compile `{givenHint.Name}`");
                    givenHint.ConfigEvent ??= configEvent;
                    driver = new PInferDriver(job, globalScope, invOutDir, checkTrace: false);
                    driver.CompilePInferHint(givenHint);
                    break;
                case PInferAction.Pruning:
                {
                    job.Output.WriteInfo("PInfer - Pruning Steps Only");
                    if (job.InvParseFileDir == null)
                    {
                        job.Output.WriteError("Inv parse file directory not provided");
                        Environment.Exit(1);
                    }
                    driver = new PInferDriver(job, globalScope, invOutDir, checkTrace: false);
                    PInferInvoke.LoadGoals(driver, job, globalScope, driver.Codegen, PInferInvoke.Goals);
                    PInferInvoke.LoadGoals(driver, job, globalScope, driver.Codegen, PInferInvoke.IndInvs, filename: "inductive.json");
                    driver.NumGoals = PInferInvoke.Goals.SelectMany(x => x.Value).Count();
                    driver.InitializeZ3();
                    var pruning_stopwatch = new Stopwatch();
                    pruning_stopwatch.Start();
                    driver.RunPruningSteps();
                    pruning_stopwatch.Stop();
                    driver.tPruning = pruning_stopwatch.ElapsedMilliseconds;
                    break;
                }
                case PInferAction.RunHint:
                {
                    job.Output.WriteInfo($"PInfer - Run `{givenHint.Name}`");
                    givenHint.ConfigEvent ??= configEvent;
                    givenHint.PruningLevel = job.PInferPruningLevel;
                    driver = new PInferDriver(job, globalScope, invOutDir);
                    PInferInvoke.LoadGoals(driver, job, globalScope, driver.Codegen, PInferInvoke.Goals);
                    PInferInvoke.LoadGoals(driver, job, globalScope, driver.Codegen, PInferInvoke.IndInvs, filename: "inductive.json");
                    driver.NumGoals = PInferInvoke.Goals.SelectMany(x => x.Value).Count();
                    driver.InitializeZ3();
                    driver.globalTimer.Start();
                    driver.ParameterSearch(givenHint);
                    driver.numEventCombinations = 1;
                    numTotalTasks = driver.numTotalTasks;
                    break;
                }
                case PInferAction.Auto:
                {
                    job.Output.WriteInfo("PInfer - Auto Exploration");
                    driver = new PInferDriver(job, globalScope, invOutDir);
                    PInferInvoke.LoadGoals(driver, job, globalScope, driver.Codegen, PInferInvoke.Goals);
                    PInferInvoke.LoadGoals(driver, job, globalScope, driver.Codegen, PInferInvoke.IndInvs, filename: "inductive.json");
                    driver.NumGoals = PInferInvoke.Goals.SelectMany(x => x.Value).Count();
                    driver.InitializeZ3();
                    driver.globalTimer.Start();
                    driver.AutoExplore(job.HintsOnly);
                    var pruning_stopwatch = new Stopwatch();
                    pruning_stopwatch.Start();
                    PInferInvoke.DoChores(job, driver.Codegen);
                    pruning_stopwatch.Stop();
                    driver.tPruning = pruning_stopwatch.ElapsedMilliseconds;
                    numTotalTasks = driver.numTotalTasks;
                    // Console.WriteLine($"Num Event combinations: {driver.numEventCombinations}");
                    break;
                }
            }
            stopwatch.Stop();
            if (job.PInferAction == PInferAction.RunHint || job.PInferAction == PInferAction.Auto || job.PInferAction == PInferAction.Pruning)
            {
                var filename = $"./invariants_{driver.TraceIndex.GetTraceCount()}.txt";
                var pruning_filename = $"./pruned_stats_{driver.TraceIndex.GetTraceCount()}.json";
                if (job.PInferAction == PInferAction.Pruning)
                {
                    filename = "./pruned_invariants.txt";
                    pruning_filename = "./pruned_stats.json";
                }
                job.Output.WriteInfo($"... Writing pruned invariants to {filename}");
                var elapsed = stopwatch.ElapsedMilliseconds / 1000.0;
                job.Output.WriteInfo($"PInfer statistics");
                job.Output.WriteInfo($"\t# invariants discovered: {PInferInvoke.NumInvsMined + PInferInvoke.NumInvPrunedAtPredicateSanitizer}");
                job.Output.WriteInfo($"\t# invariants after sanitization: {PInferInvoke.NumInvsMined}");
                // ranking invariants
                var sortedInvs = PInferInvoke.GetSortedInvariants();
                PInferInvoke.CheckLearnedGoals(globalScope, job, driver.Codegen, sortedInvs, out var cumulative);
                PInferInvoke.CheckLearnedGoals(globalScope, job, driver.Codegen, sortedInvs, out var _, checkIndInv: true);
                // job.Output.WriteInfo("Writing cumulative stats to cumulative_stats.txt ...");
                // File.WriteAllLines("cumulative_stats.txt", cumulative.Select(x => $"{x.Item1} {x.Item2}"));
                var numInvAfterPruning = PInferInvoke.WriteRecordTo(filename, sortedInvs);
                job.Output.WriteInfo($"\t#Invariants after pruning: {numInvAfterPruning}");
                job.Output.WriteInfo($"#Times executed by Daikon: {PInferInvoke.NumTasksExecuted}");
                job.Output.WriteInfo($"\tTime elapsed (seconds): {elapsed}");
                job.Output.WriteInfo($"\t#Invariants pruned by grammar: {PInferInvoke.NumInvsPrunedByGrammar}");
                job.Output.WriteInfo($"\t#Invariants pruned by tautology: {PInferInvoke.NumTautologyPruned}");
                job.Output.WriteInfo($"\t#Invariants pruned by subsumption: {PInferInvoke.NumInvsPrunedBySubsumption}");
                job.Output.WriteInfo($"\t#Invariants pruned by symmetry: {PInferInvoke.NumInvsPrunedBySymmetry}");
                job.Output.WriteInfo($"\t#Invariants pruned by sanitizing: {PInferInvoke.NumInvPrunedAtPredicateSanitizer + PInferInvoke.NumInvPrunedBySanitizing}");
                var numGoals = PInferInvoke.Goals.SelectMany(x => x.Value).Count();
                job.Output.WriteInfo($"#Goals learned with hints: {PInferInvoke.NumGoalsLearnedWithHints} / {numGoals}");
                job.Output.WriteInfo($"#Goals learned without hints: {PInferInvoke.NumGoalsLearnedWithoutHints} / {numGoals}");
                job.Output.WriteInfo($"Time to learn all goals: {driver.tLearnGoals}");
                int numActivatedGuards = PInferInvoke.GetNumActivatedGuards(invOutDir);
                int numAllGuards = PInferInvoke.GetNumAllGuards(invOutDir);
                job.Output.WriteInfo($"#Activated Guards: {numActivatedGuards}");
                job.Output.WriteInfo($"#All Guards: {numAllGuards}");
                job.Output.WriteInfo($"%Activated Guards: {numActivatedGuards / (double)numAllGuards}");
                PInferStats stats = new() {
                    NumInvsTotal = PInferInvoke.NumInvsMined + PInferInvoke.NumInvPrunedAtPredicateSanitizer,
                    NumInvsPrunedBySanitizing = PInferInvoke.NumInvPrunedBySanitizing + PInferInvoke.NumInvPrunedAtPredicateSanitizer,
                    NumInvsPrunedByGrammar = PInferInvoke.NumInvsPrunedByGrammar,
                    NumInvsPrunedBySubsumption = PInferInvoke.NumInvsPrunedBySubsumption,
                    NumInvsPrunedBySubsumptionSem = PInferInvoke.NumInvsPrunedBySubsumptionSem,
                    NumInvsPrunedBySymmetry = PInferInvoke.NumInvsPrunedBySymmetry,
                    NumInvsPrunedByTauto = PInferInvoke.NumTautologyPruned,
                    NumInvsPrunedByTautoSem = PInferInvoke.NumTautologyPrunedSem,
                    TimeElapsed = elapsed,
                    TimeCandidateTemplateGen = driver.tCandidateTemplateGen,
                    TimeMining = driver.tMining,
                    TimePruning = driver.tPruning,
                    TimeSMT = PInferInvoke.tSMT,
                    TimeLearnGoals = driver.tLearnGoals,
                    TimeSearchEventCombination = driver.tSearchEventCombination,
                    NumGoalsLearnedWithHints = PInferInvoke.NumGoalsLearnedWithHints,
                    NumGoalsLearnedWithoutHints = PInferInvoke.NumGoalsLearnedWithoutHints,
                    NumGoals = numGoals,
                    NumIndInvs = PInferInvoke.IndInvs.SelectMany(x => x.Value).Count(),
                    NumIndInvsLearned = PInferInvoke.NumIndInvsLearned,
                    NumDaikonInvocations = PInferInvoke.NumTasksExecuted,
                    NumEventCombinations = driver.numEventCombinations,
                    NumActivatedGuards = numActivatedGuards,
                    NumAllGuards = numAllGuards
                };
                File.WriteAllText(pruning_filename, JsonSerializer.Serialize(stats));
                job.Output.WriteInfo("\tWriting monitors to PInferSpecs ...");
                PInferInvoke.WriteMonitors(driver.Codegen, new(new(job)), globalScope);
            }
        }
    }

    internal class PInferStats {
        public int NumInvsTotal { get; set; }
        public int NumInvsPrunedBySubsumption { get; set; }
        public int NumInvsPrunedByTauto { get; set; }
        public int NumInvsPrunedByTautoSem { get; set; }
        public int NumInvsPrunedByGrammar { get; set; }
        public int NumInvsPrunedBySymmetry { get; set; }
        public int NumInvsPrunedBySubsumptionSem { get; set; }
        public int NumInvsPrunedBySanitizing { get; set; }
        public double TimeElapsed { get; set; }
        public double TimeMining { get; set; }
        public double TimePruning { get; set; }
        public double TimeSMT { get; set; }
        public double TimeLearnGoals { get; set; }
        public double TimeSearchEventCombination { get; set; }
        public double TimeCandidateTemplateGen { get; set; }
        public int NumGoalsLearnedWithHints { get; set; }
        public int NumGoalsLearnedWithoutHints { get; set; }
        public int NumGoals { get; set; }
        public int NumDaikonInvocations { get; set; }
        public int NumEventCombinations { get; set; }
        public int NumActivatedGuards { get; set; }
        public int NumAllGuards { get; set; }
        public int NumIndInvs { get; set; }
        public int NumIndInvsLearned { get; set; }
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
        internal static readonly Dictionary<string, HashSet<Event>> EventCombinations = [];
        internal static readonly Dictionary<string, int> NumExists = [];
        internal static readonly Dictionary<string, List<Event>> Quantified = [];
        internal static readonly Dictionary<HashSet<Event>, Dictionary<IPExpr, int>> APFrequency = [];
        internal static Dictionary<string, List<(HashSet<IPExpr>, HashSet<IPExpr>, HashSet<string>, HashSet<string>)>> Goals = [];
        internal static Dictionary<string, List<(HashSet<IPExpr>, HashSet<IPExpr>, HashSet<string>, HashSet<string>)>> IndInvs = [];
        internal static Dictionary<string, Dictionary<string, IPExpr>> ParsedGoalsP = [];
        internal static Dictionary<string, Dictionary<string, IPExpr>> ParsedGoalsQ = [];
        internal static HashSet<string> Learned = [];
        internal static HashSet<string> Recorded = [];
        internal static int NumInvsMined = 0;
        internal static int NumInvsPrunedByGrammar = 0;
        internal static int NumTasksExecuted = 0;
        internal static int NumInvPrunedBySanitizing = 0;
        internal static int NumInvPrunedAtPredicateSanitizer = 0;
        internal static int NumInvsPrunedBySubsumption = 0;
        internal static int NumInvsPrunedBySubsumptionSem = 0;
        internal static int NumInvsPrunedBySymmetry = 0;
        internal static int NumTautologyPruned = 0;
        internal static int NumTautologyPrunedSem = 0;
        internal static bool UseZ3 = false;
        internal static Z3Wrapper Z3Wrapper;
        internal static int NumGoalsLearnedWithHints = 0;
        internal static int NumGoalsLearnedWithoutHints = 0;
        internal static int NumIndInvsLearned = -1;
        internal static double tSMT = 0;

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

        public static int GetNumActivatedGuards(string invOutDir) {
            var activatedGuardsFile = Path.Combine("PInferOutputs", invOutDir, "activated_guards.txt");
            if (!File.Exists(activatedGuardsFile))
            {
                return 0;
            }
            return File.ReadAllLines(activatedGuardsFile).Length;
        }

        public static int GetNumAllGuards(string invOutDir) {
            var allGuardsFile = Path.Combine("PInferOutputs", invOutDir, "all_guards.txt");
            if (!File.Exists(allGuardsFile))
            {
                return 0;
            }
            return File.ReadAllLines(allGuardsFile).Length;
        }

        public static void LoadTo(PInferDriver driver, ICompilerConfiguration job, Scope globalScope, PInferPredicateGenerator codegen, Dictionary<string, List<(HashSet<IPExpr>, HashSet<IPExpr>, HashSet<string>, HashSet<string>)>> dst, List<Goal> goals)
        {
            foreach (var goal in goals)
            {
                List<PEventVariable> quantified = goal.Events.Select(x => {
                    var parts = x.Split(":");
                    var pevent = driver.GetPEvent(parts[1]);
                    return new PEventVariable(parts[0]) {
                        EventDecl = pevent, Type = pevent.PayloadType
                    };
                }).ToList();
                Hint h = new("goal", false, null) {
                    Quantified = quantified,
                    ConfigEvent = goal.ConfigEvent == null ? null : driver.GetPEvent(goal.ConfigEvent),
                    TermDepth = goal.TermDepth,
                    ExistentialQuantifiers = goal.NumExists
                };
                codegen.Reset();
                codegen.WithHint(h);
                codegen.GenerateCode(job, globalScope);
                var key = h.GetQuantifierHeader();
                if (!dst.ContainsKey(key))
                {
                    dst[key] = [];
                    ParsedGoalsP[key] = [];
                    ParsedGoalsQ[key] = [];
                }
                var goalList = dst[key];
                HashSet<IPExpr> p = new(codegen.Comparer);
                HashSet<IPExpr> q = new(codegen.Comparer);
                foreach (var g in goal.Guards)
                {
                    if (codegen.TryParseToExpr(job, globalScope, g, out var expr))
                    {
                        p.Add(expr);
                        ParsedGoalsP[key][g] = expr;
                    }
                    else
                    {
                        job.Output.WriteError($"Error parsing guard in goals: {g}");
                        Environment.Exit(1);
                    }
                }
                foreach (var f in goal.Filters)
                {
                    if (codegen.TryParseToExpr(job, globalScope, f, out var expr))
                    {
                        q.Add(expr);
                        ParsedGoalsQ[key][f] = expr;
                    }
                    else
                    {
                        job.Output.WriteError($"Error parsing filter in goals: {f}");
                        Environment.Exit(1);
                    }
                }
                goalList.Add((p, q, goal.Guards.ToHashSet(), goal.Filters.ToHashSet()));
            }
        }

        public static void LoadGoals(PInferDriver driver, ICompilerConfiguration job, Scope globalScope, PInferPredicateGenerator codegen, Dictionary<string, List<(HashSet<IPExpr>, HashSet<IPExpr>, HashSet<string>, HashSet<string>)>> loadTo, string filename = "goals.json")
        {
            if (!File.Exists(filename))
            {
                job.Output.WriteWarning($"Goals file not found: {filename} ... skipping");
                return;
            }
            if (filename.Contains("inductive"))
            {
                NumIndInvsLearned = 0;
            }
            using StreamReader reader = new(filename);
            string json = reader.ReadToEnd();
            List<Goal> goals = JsonSerializer.Deserialize<List<Goal>>(json);
            reader.Close();
            LoadTo(driver, job, globalScope, codegen, loadTo, goals);
        }

        public static void CheckLearnedGoals(Scope globalScope, ICompilerConfiguration job, PInferPredicateGenerator codegen, IEnumerable<(string, int, int, Hint, HashSet<string>, HashSet<string>)> invariants, out List<(decimal, decimal)> cumulative, bool checkIndInv = false)
        {
            Z3Wrapper = new(globalScope, codegen);
            cumulative = [];
            if (Goals.Count == 0) return;
            if (!checkIndInv)
            {
                NumGoalsLearnedWithHints = 0;
                NumGoalsLearnedWithoutHints = 0;
            }
            Dictionary<string, HashSet<int>> learned = [];
            HashSet<string> identifiedSpecs = [];
            HashSet<string> visitedKey = [];
            int totalInvs = invariants.Count();
            int checkedInvs = 0;
            var targets = checkIndInv ? IndInvs : Goals;
            var numTotalGoals = targets.SelectMany(x => x.Value).Count();
            foreach (var (key, _, _, h, p, q) in invariants)
            {
                if (q.Count == 0) continue;
                if (!targets.ContainsKey(key))
                {
                    checkedInvs += 1;
                    // cumulative.Add((checkedInvs / (decimal)totalInvs, NumGoalsLearnedWithHints / (decimal)numTotalGoals));
                    continue;
                }
                visitedKey.Add(key);
                var goals = targets[key];
                if (!learned.ContainsKey(key))
                {
                    learned[key] = [];
                }
                for (int i = 0; i < goals.Count; ++i)
                {
                    if (learned[key].Contains(i)) continue;
                    var (gp, gq, gps, gqs) = goals[i];
                    var lp = p.Select(x => ParsedP[key][x]).ToList();
                    var lq = q.Select(x => ParsedQ[key][x]).ToList();
                    // syntactic check first
                    var cond1 = p.IsSubsetOf(gps);
                    var cond2 = gqs.IsSubsetOf(q);
                    // check symmetry
                    bool symmCheck = false;
                    if (NumExists[key] == 0 && EventCombinations[key].Count == 1)
                    {
                        var symmGP = ToSymmetricGuards(ParsedGoalsP[key], gps);
                        var symmGQ = ToSymmetricGuards(ParsedGoalsQ[key], gqs);
                        symmCheck = p.IsSubsetOf(symmGP) && symmGQ.IsSubsetOf(q);
                    }
                    if (symmCheck || (cond1 && cond2) || CheckImplies(key, gp, lp) && CheckImplies(key, lq, gq))
                    {
                        if (checkIndInv)
                        {
                            NumIndInvsLearned++;
                        }
                        else
                        {
                            NumGoalsLearnedWithHints++;
                        }
                        if (!h.UserHint && !checkIndInv)
                        {
                            NumGoalsLearnedWithoutHints++;
                        }
                        Console.WriteLine($"Learned: {key}");
                        learned[key].Add(i);
                        var invStr = AssembleInvariant(h, p, q);
                        identifiedSpecs.Add(invStr);
                    }
                }
                checkedInvs += 1;
                // cumulative.Add((checkedInvs / (decimal)totalInvs, NumGoalsLearnedWithHints / (decimal)numTotalGoals));
            }
            // check any missed goals and goals not learned
            var missed = targets.Keys.Except(visitedKey);
            if (missed.Any())
            {
                Console.WriteLine($"Missed: {string.Join(", ", missed)}");
            }
            foreach (var key in targets.Keys)
            {
                if (learned.ContainsKey(key))
                {
                    foreach (var i in Enumerable.Range(0, targets[key].Count))
                    {
                        if (!learned[key].Contains(i))
                        {
                            Console.WriteLine($"Not learned: {key} Guards: {string.Join(" && ", targets[key][i].Item3)} Filters: {string.Join(" && ", targets[key][i].Item4)}");
                        }
                    }
                }
            }
            if (!checkIndInv)
            {
                using StreamWriter writer = new("confirmed_specs.txt");
                foreach (var spec in identifiedSpecs)
                {
                    writer.WriteLine(spec);
                }
            }
        }

        private static bool GetFrequency(string key, HashSet<Event> eventSet, IPExpr e, out int freq, out IPExpr mem)
        {
            foreach (var k in APFrequency.Keys)
            {
                if (k.SetEquals(eventSet) && APFrequency[k].ContainsKey(e))
                {
                    foreach (var p in APFrequency[k].Keys)
                    {
                        if (CheckImplies(key, [p], [e]) && CheckImplies(key, [e], [p]))
                        {
                            freq = APFrequency[k][p];
                            mem = p;
                            return true;
                        }
                    }
                }
            }
            freq = 0;
            mem = null;
            return false;
        }

        public static void ComputeFiltersFrequency()
        {
            APFrequency.Clear();
            foreach (var (key, _, _, h, p, q) in AllExecuedAndMined())
            {
                var quantified = h.QuantifiedEvents().ToHashSet();
                if (!APFrequency.ContainsKey(quantified))
                {
                    APFrequency[quantified] = new(new ASTComparer());
                }
                foreach (var ap in q)
                {
                    if (GetFrequency(key, quantified, ParsedQ[key][ap], out var freq, out var k))
                    {
                        APFrequency[quantified][k] = freq + 1;
                    }
                    else
                    {
                        APFrequency[quantified][ParsedQ[key][ap]] = 1;
                    }
                }
            }
        }

        public static IEnumerable<(string, int, int, Hint, HashSet<string>, HashSet<string>)> GetSortedInvariants()
        {
            ComputeFiltersFrequency();
            List<((string, int, int, Hint, HashSet<string>, HashSet<string>), decimal)> result = [];
            foreach (var (key, n, e, h, p, q) in AllExecuedAndMined())
            {
                decimal weight = 0;
                var quantified = h.QuantifiedEvents().ToHashSet();
                foreach (var ap in q)
                {
                    if (GetFrequency(key, quantified, ParsedQ[key][ap], out var freq, out var k))
                    {
                        // Console.WriteLine($"Frequency of {ap}: {freq}");
                        var numRels = FreeEvents(ParsedQ[key][ap]).Count;
                        weight += numRels / (decimal)freq;
                    }
                }
                // Console.WriteLine(h.GetInvariantReprHeader(string.Join(" ∧ ", p), string.Join(" ∧ ", q)) + " " + weight);
                result.Add(((key, n, e, h, p, q), weight / q.Count));
            }
            return result.OrderByDescending(x => x.Item2).Select(x => x.Item1);
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
            List<MonitorMetadata> monitorMetadata = [];
            string outdir = Path.Combine(transform.context.Job.OutputDirectory.ToString(), "PInferSpecs");
            string rt_monitorDir = Path.Combine(transform.context.Job.OutputDirectory.ToString(), "PInferRT");
            if (Directory.Exists(outdir))
            {
                Directory.Delete(outdir, true);
            }
            Directory.CreateDirectory(outdir);
            foreach (var (key, _, _, h, p, q) in AllExecuedAndMined())
            {
                if (q.Count == 0) continue;
                var ps = string.Join(" ∧ ", p);
                monitorCount[h.Name] = monitorCount.TryGetValue(h.Name, out var cnt) ? cnt + 1 : 1;
                var prop = h.GetInvariantReprHeader(ps, string.Join(" ∧ ", q));
                CompiledFile monitorFile = new($"{h.Name}_{monitorCount[h.Name]}.p", outdir);
                CompiledFile runtimeFile = new($"{h.Name}_{monitorCount[h.Name]}_runtime.py", rt_monitorDir);
                try
                {
                    transform.WithFile(monitorFile);
                    transform.WriteSpecMonitor(c, codegen, transform.context, transform.context.Job, globalScope, h, p, q, ParsedP[key], ParsedQ[key], prop);
                    transform.context.Job.Output.WriteFile(monitorFile);
                    monitorMetadata.Add(new MonitorMetadata
                    {
                        Name = $"{h.Name}_{c}",
                        Specification = prop,
                    });
                    c++;

                    transform.WithFile(runtimeFile);
                    transform.WriteLogMonitor(c, codegen, transform.context, transform.context.Job, globalScope, h, p, q, ParsedP[key], ParsedQ[key], prop);
                    transform.context.Job.Output.WriteFile(runtimeFile);
                }
                catch (Exception e)
                {
                    transform.context.Job.Output.WriteError($"Error writing monitor for {h.Name}:\nInvariant: {prop}\n{e.Message}\n{e.StackTrace}");
                    continue;
                }
            }
            File.WriteAllText(Path.Combine(outdir, "metadata.json"), JsonSerializer.Serialize(monitorMetadata));
        }

        public static string AssembleInvariant(Hint h, HashSet<string> p, HashSet<string> q)
        {
            return h.GetInvariantReprHeader(string.Join(" ∧ ", p), string.Join(" ∧ ", q));
        }

        public static int WriteRecordTo(string filename, IEnumerable<(string, int, int, Hint, HashSet<string>, HashSet<string>)> record)
        {
            using StreamWriter invwrite = new(filename);
            HashSet<string> written = [];
            foreach (var (_, _, _, h, p, q) in record)
            {
                if (q.Count == 0) {
                    NumInvPrunedBySanitizing += 1;
                    continue;
                }
                var rec = AssembleInvariant(h, p, q);
                if (written.Contains(rec)) continue;
                written.Add(rec);
                invwrite.WriteLine(rec);
            }
            return written.Count;
        }

        private static HashSet<string> FreeEvents(IPExpr e)
        {
            switch (e)
            {
                case VariableAccessExpr varAccess:
                {
                    return [varAccess.Variable.Name];
                }
                case BinOpExpr binOpExpr:
                {
                    return FreeEvents(binOpExpr.Lhs).Union(FreeEvents(binOpExpr.Rhs)).ToHashSet();
                }
                case UnaryOpExpr unaryOpExpr:
                {
                    return FreeEvents(unaryOpExpr.SubExpr);
                }
                case FunCallExpr funCallExpr:
                {
                    return funCallExpr.Arguments.SelectMany(FreeEvents).ToHashSet();
                }
                case TupleAccessExpr tupleAccessExpr:
                {
                    return FreeEvents(tupleAccessExpr.SubExpr);
                }
                case NamedTupleAccessExpr namedTupleAccessExpr:
                {
                    return FreeEvents(namedTupleAccessExpr.SubExpr);
                }
                case SizeofExpr sizeofExpr:
                {
                    return FreeEvents(sizeofExpr.Expr);
                }
                default: return [];
            }
        }

        // top k for each key
        public static List<string> TopK(int k)
        {
            List<string> result = [];
            Dictionary<string, List<(string, int)>> topk = [];
            foreach (var (key, _, _, h, p, q) in AllExecuedAndMined())
            {
                if (q.Count == 0) continue;
                var rec = AssembleInvariant(h, p, q);
                if (!topk.ContainsKey(key))
                {
                    topk[key] = [];
                }
                int numRels = 0;
                foreach (var ap in p.Concat(q))
                {
                    if (ParsedP[key].ContainsKey(ap))
                    {
                        numRels += FreeEvents(ParsedP[key][ap]).Count > 1 ? 1 : 0;
                    }
                    else
                    {
                        numRels += FreeEvents(ParsedQ[key][ap]).Count > 1 ? 1 : 0;
                    }
                }
                topk[key].Add((rec, numRels));
            }
            foreach (var key in topk.Keys)
            {
                var sorted = topk[key].OrderByDescending(x => x.Item2).Take(k).Select(x => x.Item1);
                result.AddRange(sorted);
            }
            return result;
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

        public static bool CheckImplies(string key, IEnumerable<IPExpr> lhs, IEnumerable<IPExpr> rhs)
        {
            try
            {
                return Z3Wrapper.CheckImplies(key, lhs, rhs);
            }
            catch (Exception)
            {
                // Console.WriteLine($"Error checking implication: {e.Message}");
                return false;
            }
        }

        public static bool Resolution(PInferPredicateGenerator codegen, string key, HashSet<string> p, HashSet<string> q)
        {
            // remove all contradicting predicates in guards
            bool didSth = false;
            HashSet<string> removal = [];
            bool checkWithZ3(string s1, string s2) {
                if (!UseZ3) return false;
                var s1Expr = ParsedP[key][s1];
                var s2Expr = ParsedP[key][s2];
                var negatedS1 = new UnaryOpExpr(s1Expr.SourceLocation, UnaryOpType.Not, s1Expr);
                return CheckImplies(key, [negatedS1], [s2Expr]) && CheckImplies(key, [s2Expr], [negatedS1]);
            };
            foreach (var s1 in p)
            {
                foreach (var s2 in q)
                {
                    if (codegen.Negating(s1, s2) || checkWithZ3(s1, s2))
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

        private static bool Implies(string key, HashSet<string> p, HashSet<string> q, out bool isSyn, bool recordTime = true)
        {
            bool result = q.IsSubsetOf(p);
            if (!UseZ3 || result)
            {
                isSyn = true;
                return result;
            }
            else
            {
                try
                {
                    isSyn = false;
                    Stopwatch sw = new();
                    sw.Start();
                    var ret = Z3Wrapper.CheckImplies(key, p, q, ParsedP[key], ParsedQ[key]);
                    sw.Stop();
                    if (recordTime)
                    {
                        tSMT += sw.ElapsedMilliseconds;
                    }
                    return ret;
                }
                catch (Exception)
                {
                    // Console.WriteLine($"Error checking implication: {e.Message}");
                    isSyn = true;
                    return result;
                }
            }
        }

        private static bool BiImplies(string key, HashSet<string> p, HashSet<string> q, out bool isSyn)
        {
            var ret = Implies(key, p, q, out var i1);
            ret &= Implies(key, q, p, out var i2);
            isSyn = i1 || i2;
            return ret;
        }

        private static bool BiImplies(string k, Dictionary<string, IPExpr> parsedLhs, Dictionary<string, IPExpr> parsedRhs, HashSet<string> lhs, HashSet<string> rhs, out bool isSyn)
        {
            bool result = lhs.SetEquals(rhs);
            if (!UseZ3 || result)
            {
                isSyn = true;
                return result;
            }
            try
            {
                var lhsExprs = lhs.Select(x => parsedLhs[x]).ToList();
                var rhsExprs = rhs.Select(x => parsedRhs[x]).ToList();
                isSyn = false;
                Stopwatch sw = new();
                sw.Start();
                var ret = Z3Wrapper.CheckImplies(k, lhsExprs, rhsExprs) && Z3Wrapper.CheckImplies(k, rhsExprs, lhsExprs);
                sw.Stop();
                tSMT += sw.ElapsedMilliseconds;
                return ret;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error checking bi-implication: {e.Message}");
                isSyn = false;
                return result;
            }
        }

        private static bool CheckContrapositive(string k, int nExists, Dictionary<string, IPExpr> parsedLhs, Dictionary<string, IPExpr> parsedRhs,
                            HashSet<string> p1, HashSet<string> q1, HashSet<string> p2, HashSet<string> q2)
        {
            if (!UseZ3 || nExists != 0) return false;
            var p1Exprs = p1.Select(x => parsedLhs[x]).ToList();
            var q1Exprs = q1.Select(x => parsedRhs[x]).ToList();
            var p2Exprs = p2.Select(x => parsedLhs[x]).ToList();
            var q2Exprs = q2.Select(x => parsedRhs[x]).ToList();
            try
            {
                Stopwatch sw = new();
                sw.Start();
                var ret = Z3Wrapper.CheckImpliesContrapositive(k, p1Exprs, q1Exprs, p2Exprs, q2Exprs);
                sw.Stop();
                tSMT += sw.ElapsedMilliseconds;
                return ret;

            }
            catch (Exception)
            {
                return false;
            }
            
        }

        public static bool ClearUpExistentials()
        {
            bool didSth = false;
            foreach (var k in Q.Keys)
            {
                int kExists = NumExists[k];
                var quantifiedEvents = Quantified[k];
                HashSet<int> removal = [];
                for (int i = 0; i < Q[k].Count; ++i)
                {
                    var qs = Q[k][i];
                    // check specs with more forall-quantifiers
                    // e.g. if forall* P holds
                    // then forall*exists* P is trivially true
                    // we remove P from forall*exists* in this case
                    foreach (var k1 in P.Keys)
                    {
                        if (!quantifiedEvents.SequenceEqual(Quantified[k1])) continue;
                        int k1Exists = NumExists[k1];
                        if (k1Exists < kExists)
                        {
                            for (int j = 0; j < Q[k1].Count; ++j)
                            {
                                // if (qs.SetEquals(Q[k1][j]))
                                if (BiImplies(k, ParsedQ[k], ParsedQ[k1], qs, Q[k1][j], out var isSyn))
                                {
                                    removal.Add(i);
                                    // NumInvsPrunedBySubsumption += 1;
                                    IncSubsumpPruned(isSyn);
                                    break;
                                }   
                            }
                        }
                        if (removal.Contains(i)) break;
                    }
                }
                foreach (var j in removal.OrderByDescending(x => x))
                {
                    RemoveRecordAt(k, j);
                }
                didSth |= removal.Count > 0;
            }
            return didSth;
        }

        private static HashSet<string> ToSymmetricGuards(Dictionary<string, IPExpr> parsed, HashSet<string> p)
        {
            HashSet<string> result = [];
            (string, string) SwapPayloadAccess(string lhs, string rhs)
            {
                if (lhs.Contains("indexof") || rhs.Contains("indexof"))
                {
                    return (lhs, rhs);
                }
                var lpos = lhs.IndexOf(".payload") + 8;
                var rpos = rhs.IndexOf(".payload") + 8;
                // get everything after the first `.payload`
                var l = lhs[lpos..];
                var r = rhs[rpos..];
                return (lhs[..lpos] + r, rhs[..rpos] + l);
            }
            foreach (var f in p)
            {
                if (parsed[f] is FunCallExpr)
                {
                    // skip custom functions and predicates
                    result.Add(f);
                    continue;
                }
                var g = f;
                if (f.StartsWith('('))
                {
                    g = f[1..^1];
                }
                if (f.Contains("==") || f.Contains("!="))
                {
                    if (!f.StartsWith('('))
                    {
                        result.Add($"({f})");
                    }
                    else
                    {
                        result.Add(f);
                    }
                }
                else if (f.Contains(">="))
                {
                    var parts = g.Split(">=").Select(x => x.Trim()).ToArray();
                    var (lhs, rhs) = SwapPayloadAccess(parts[0], parts[1]);
                    result.Add($"({lhs} <= {rhs})");
                }
                else if (f.Contains("<="))
                {
                    var parts = g.Split("<=").Select(x => x.Trim()).ToArray();
                    var (lhs, rhs) = SwapPayloadAccess(parts[0], parts[1]);
                    result.Add($"({lhs} >= {rhs})");
                }
                else if (f.Contains('<'))
                {
                    var parts = g.Split("<").Select(x => x.Trim()).ToArray();
                    var (lhs, rhs) = SwapPayloadAccess(parts[0], parts[1]);
                    result.Add($"({lhs} > {rhs})");
                }
                else if (f.Contains('>'))
                {
                    var parts = g.Split(">").Select(x => x.Trim()).ToArray();
                    var (lhs, rhs) = SwapPayloadAccess(parts[0], parts[1]);
                    result.Add($"({lhs} < {rhs})");
                }
                else
                {
                    result.Add(f);
                }
            }
            return result;
        }

        public static void IncTautologyPruned(bool isSyn)
        {
            if (isSyn)
            {
                NumTautologyPruned += 1;
            }
            else
            {
                NumTautologyPrunedSem += 1;
            }
        }

        public static void IncSubsumpPruned(bool isSyn)
        {
            if (isSyn)
            {
                NumInvsPrunedBySubsumption += 1;
            }
            else
            {
                NumInvsPrunedBySubsumptionSem += 1;
            }
        }

        public static void DoChores(ICompilerConfiguration job, PInferPredicateGenerator codegen)
        {
            // iterate through the record and merge/discard any duplicates
            // process till fixpoint
            bool didSth = true;
            int numIters = 0;
            var stopwatch = new Stopwatch();
            bool isSyn;
            bool isSyn2;
            while (didSth)
            {
                numIters++;
                didSth = false;
                Console.WriteLine("[Chores] Iteration " + numIters);
                foreach (var k in P.Keys)
                {
                    int numExists = NumExists[k];
                    HashSet<int> removes = [];
                    for (int i = 0; i < P[k].Count; ++i)
                    {
                        if (removes.Contains(i)) continue;
                        var pi = P[k][i];
                        var qi = Q[k][i];
                        if (qi.Count == 0)
                        {
                            removes.Add(i);
                            NumInvsPrunedBySubsumption += 1;
                            continue;
                        }
                        if (Implies(k, pi, qi, out isSyn))
                        {
                            var rec = ShowRecordAt(k, i);
                            // job.Output.WriteWarning($"[Chores][Remove-Tauto] {rec}");
                            removes.Add(i);
                            // NumTautologyPruned += 1;
                            IncTautologyPruned(isSyn);
                            continue;
                        }
                        for (int j = i + 1; j < P[k].Count; ++j)
                        {
                            if (removes.Contains(i)) break;
                            if (removes.Contains(j)) continue;
                            var pj = P[k][j];
                            var qj = Q[k][j];
                            if (BiImplies(k, pi, pj, out isSyn) && numExists == 0)
                            {
                                // can only merge when there is
                                // no existential quantifications
                                var rec = ShowRecordAt(k, j);
                                // job.Output.WriteWarning($"[Chores][Merge-Remove] {rec}; merged with {ShowRecordAt(k, i)}");
                                qi.UnionWith(qj);
                                removes.Add(j);
                                // NumInvsPrunedBySubsumption += 1;
                                IncSubsumpPruned(isSyn);
                            }
                            // Forall-only rules
                            // Case 1: i ==> j; i.e. pi ==> pj && qj ==> qi
                            // keep j remove i
                            // else if (pj.IsSubsetOf(pi) && qi.IsSubsetOf(qj))
                            else if (Implies(k, pi, pj, out isSyn) && Implies(k, qj, qi, out isSyn2))
                            {
                                // Console.WriteLine($"Remove {i}");
                                // job.Output.WriteWarning($"[Chores][Remove] {ShowRecordAt(k, i)} implied by {ShowRecordAt(k, j)}");
                                removes.Add(i);
                                // NumInvsPrunedBySubsumption += 1;
                                IncSubsumpPruned(isSyn && isSyn2);
                            }
                            // Case 2: j ==> i; keep i remove j
                            // else if (pi.IsSubsetOf(pj) && qj.IsSubsetOf(qi))
                            else if (Implies(k, pj, pi, out isSyn) && Implies(k, qi, qj, out isSyn2))
                            {
                                // Console.WriteLine($"Remove {j}");
                                // job.Output.WriteWarning($"[Chores][Remove] {ShowRecordAt(k, j)} implied by {ShowRecordAt(k, i)}");
                                removes.Add(j);
                                // NumInvsPrunedBySubsumption += 1;
                                IncSubsumpPruned(isSyn && isSyn2);
                            }
                            // Case 1.1: i ==> j; i.e., if not qj ==> pi and qj ==> not qi
                            else if (CheckContrapositive(k, NumExists[k], ParsedP[k], ParsedQ[k], pi, qi, pj, qj))
                            {
                                // job.Output.WriteWarning($"Remove {ShowRecordAt(k, j)} by {ShowRecordAt(k, i)}");
                                removes.Add(j);
                                // NumInvsPrunedBySubsumption += 1;
                                IncSubsumpPruned(true);
                            }
                            else if (CheckContrapositive(k, NumExists[k], ParsedP[k], ParsedQ[k], pj, qj, pi, qi))
                            {
                                // job.Output.WriteWarning($"Remove {ShowRecordAt(k, i)} by {ShowRecordAt(k, j)}");
                                removes.Add(i);
                                // NumInvsPrunedBySubsumption += 1;
                                IncSubsumpPruned(false);
                            }
                            // Case 3: if i ==> j, then any thing holds under j also holds under i
                            // we may remove those from pi
                            // e.g. forall* P -> Q, moreover P -> R
                            // if it is the case that forall* R -> Q, we remove Q for the stronger guards P
                            // i.e. keeping the weakest guard for Q
                            // else if (pj.IsSubsetOf(pi))
                            else if (Implies(k, pi, pj, out isSyn))
                            {
                                if (qi.Intersect(qj).Any() && numExists == 0)
                                {
                                    // job.Output.WriteWarning($"[Chores][Remove] common filters from {ShowRecordAt(k, i)} that is also in {ShowRecordAt(k, j)}");
                                    qi.ExceptWith(qj);
                                    didSth = true;
                                }
                            }
                            // else if (pi.IsSubsetOf(pj))
                            else if (Implies(k, pj, pi, out isSyn))
                            {
                                if (qj.Intersect(qi).Any() && numExists == 0)
                                {
                                    // job.Output.WriteWarning($"[Chores][Remove] common filters from {ShowRecordAt(k, j)} that is also in {ShowRecordAt(k, i)}");
                                    qj.ExceptWith(qi);
                                    didSth = true;
                                }
                            }
                            // stopwatch.Stop();
                            // Console.WriteLine($"[Check] Done in {stopwatch.ElapsedTicks} ticks");
                        }
                    }
                    if (NumExists[k] == 0 && EventCombinations[k].Count == 1)
                    {
                        // check for any symmetric guards
                        for (int i = 0; i < P[k].Count; ++i)
                        {
                            if (removes.Contains(i)) continue;
                            var pi = P[k][i];
                            var symPi = ToSymmetricGuards(ParsedP[k], pi);
                            for (int j = i + 1; j < P[k].Count; ++j)
                            {
                                if (removes.Contains(j)) continue;
                                if (symPi.SetEquals(P[k][j]))
                                {
                                    // job.Output.WriteWarning($"[Chores][Remove-Symmetric] {ShowRecordAt(k, j)} by {ShowRecordAt(k, i)}");
                                    removes.Add(j);
                                    NumInvsPrunedBySymmetry += 1;
                                }
                            }
                        }
                    }
                    // stopwatch.Restart();
                    foreach (var idx in removes.OrderByDescending(x => x))
                    {
                        RemoveRecordAt(k, idx);
                    }
                    didSth |= removes.Count != 0;
                    // stopwatch.Stop();
                    // Console.WriteLine($"[Remove] Done in {stopwatch.ElapsedMilliseconds} ms");
                }
                // Boolean resolution
                // stopwatch.Restart();
                HashSet<int> insufficient = [];
                foreach (var k in P.Keys)
                {
                    insufficient = [];
                    for (int i = 0; i < P[k].Count; ++i)
                    {
                        for (int j = i + 1; j < P[k].Count; ++j)
                        {
                            if (BiImplies(k, Q[k][i], Q[k][j], out var _))
                            {
                                didSth |= Resolution(codegen, k, P[k][i], P[k][j]);
                            }
                        }
                        if (NumExists[k] == 0)
                        {
                            // check whether the invariant establishes sufficient relationships (i.e. the set of free events is equal to the quantified events)
                            var freeEventsList = P[k][i].Select(x => FreeEvents(ParsedP[k][x])).Concat(Q[k][i].Select(x => FreeEvents(ParsedQ[k][x]))).ToList();
                            var quantified = Executed[k][i].Quantified.Select(x => x.Name).ToHashSet();
                            if (freeEventsList.All(x => !x.SetEquals(quantified)))
                            {
                                insufficient.Add(i);
                                NumInvPrunedBySanitizing += 1;
                                didSth = true;
                            }
                        }
                    }
                    foreach (var idx in insufficient.OrderByDescending(x => x))
                    {
                        RemoveRecordAt(k, idx);
                    }
                }
                didSth |= ClearUpExistentials();
            }
            // Console.WriteLine($"[Chores] Done in {numIters} iterations");
        }

        // return the Ps and Qs that should be included to the log
        public static bool UpdateMinedSpecs(ICompilerConfiguration job, PInferPredicateGenerator codegen, Hint hint, HashSet<string> p_prime, HashSet<string> q_prime, Dictionary<string, IPExpr> parsedP, Dictionary<string, IPExpr> parsedQ)
        {
            var quantifiers = hint.GetQuantifierHeader();
            // var curr_inv = hint.GetInvariantReprHeader(string.Join(" ∧ ", p_prime), string.Join(" ∨ ", q_prime.Select(x => string.Join(" ∧ ", x))));
            // check whether the invariant establishes sufficient relationships (i.e. the set of free events is equal to the quantified events)
            // var freeEventsList = p_prime.Select(x => FreeEvents(parsedP[x])).Concat(q_prime.Select(x => FreeEvents(parsedQ[x]))).ToList();
            // if (hint.ExistentialQuantifiers == 0 && freeEventsList.All(x => !x.SetEquals(hint.Quantified.Select(x => x.Name))))
            // {
            //     return (p_prime, q_prime);
            // }
            int numExists = hint.ExistentialQuantifiers;
            List<Event> quantifiedEvents = hint.Quantified.Select(x => x.EventDecl).ToList();
            if (!P.ContainsKey(quantifiers)) P[quantifiers] = [];
            if (!Q.ContainsKey(quantifiers)) Q[quantifiers] = [];
            if (!Executed.ContainsKey(quantifiers)) Executed[quantifiers] = [];
            if (!EventCombinations.ContainsKey(quantifiers)) EventCombinations[quantifiers] = hint.QuantifiedEvents().ToHashSet();
            if (!ParsedP.ContainsKey(quantifiers)) ParsedP[quantifiers] = [];
            if (!ParsedQ.ContainsKey(quantifiers)) ParsedQ[quantifiers] = [];
            // add the current combination
            foreach (var (p, q) in P[quantifiers].Zip(Q[quantifiers]))
            {
                if (p.SetEquals(p_prime) && q.SetEquals(q_prime)) return false;
            }
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
            return true;
        }
    
        public static int PruneAndAggregate(ICompilerConfiguration job, Scope globalScope, Hint hint, PInferPredicateGenerator codegen, string[] contents, out int total)
        {
            int result = 0;
            var lastLine = contents[^1].Split(" ");
            total = int.Parse(lastLine[0]);
            NumTasksExecuted += total;
            NumInvPrunedAtPredicateSanitizer += int.Parse(lastLine[1]);
            Dictionary<string, List<(HashSet<string>, HashSet<string>)>> memo = [];
            var quantifiers = hint.GetQuantifierHeader();
            for (int i = 0; i < contents.Length; i += 3)
            {
                if (i + 1 >= contents.Length) break;
                if (!memo.ContainsKey(quantifiers))
                {
                    memo[quantifiers] = [];
                }
                var guards = contents[i];
                var filters = contents[i + 1];
                var properties = contents[i + 2]
                                .Split("∧")
                                .Where(x => x.Length > 0)
                                .Select(x => x.Trim());

                var p = guards.Split("∧").Select(x => x.Trim()).Where(x => x.Length > 0).ToHashSet();
                var q = filters.Split("∧").Select(x => x.Trim()).Where(x => x.Length > 0).ToHashSet();
                if (q.Count + properties.Count() == 0) continue;
                if (memo[quantifiers].Any(x => x.Item1.SetEquals(p) && x.Item2.SetEquals(q.Concat(properties).ToHashSet()))) continue;
                memo[quantifiers].Add((p, q.Concat(properties).ToHashSet()));
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
                    foreach (var f in q)
                    {
                        if (parsedP.ContainsKey(f)) continue;
                        if (codegen.TryParseToExpr(job, globalScope, f, out var parsed))
                        {
                            parsedQ[f] = parsed;
                        }
                        else
                        {
                            throw new Exception($"[ERROR] Filter {f} cannot be parsed");
                        }
                    }
                    if (!UpdateMinedSpecs(job, codegen, hint, p, q, parsedP, parsedQ))
                    {
                        NumInvsPrunedBySubsumption += 1;
                    }
                }
                else
                {
                    NumInvsPrunedByGrammar += 1;
                }
                if (keep.Count > 0)
                {
                    result += 1;
                }
                // WriteInvs(codegen, guards, filters, keep, stepback);
            }
            // DoChores(job, codegen);
            return result;
        }

        public static int InvokeMain(ICompilerConfiguration job, TraceMetadata metadata, Scope globalScope, Hint hint, PInferPredicateGenerator codegen, string invOutDir, out int totalTasks)
        {
            ProcessStartInfo startInfo;
            Process process;
            var depsOpt = Path.Combine(GetPInferDependencies(), "*");
            var classpath = Path.Combine(job.OutputDirectory.ToString(), "target", "classes");
            List<string> args = ["-cp",
                    string.Join(":", [depsOpt, classpath]),
                    $"{job.ProjectName}.pinfer.Main"];
            
            List<string> configArgs = GetMinerConfigArgs(job, metadata, hint, codegen, invOutDir);
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
            var parseFilePath = Path.Combine("PInferOutputs", invOutDir, PreambleConstants.ParseFileName);
            var contents = File.ReadAllLines(parseFilePath);
            var numMined = PruneAndAggregate(job, globalScope, hint, codegen, contents, out totalTasks);
            var parsefileHeaders = Path.Combine("PInferOutputs", invOutDir, "parsable_headers.txt");
            if (!File.Exists(parsefileHeaders))
            {
                File.Create(parsefileHeaders).Close();
            }
            using StreamWriter writer = File.AppendText(parsefileHeaders);
            writer.WriteLine(string.Join(" ", hint.Quantified.Select(x => $"{x.Name}:{x.EventName}")));
            writer.WriteLine(hint.ConfigEvent == null ? "" : hint.ConfigEvent.Name);
            writer.WriteLine(hint.ExistentialQuantifiers);
            writer.WriteLine(hint.TermDepth);
            writer.WriteLine(hint.UserHint);
            writer.Close();
            // ShowAll();
            job.Output.WriteWarning($"Currently mined: {Recorded.Count} invariant(s)");
            job.Output.WriteWarning($"Currently recorded: {WriteRecordTo($"inv_running_{metadata.GetTraceCount()}.txt", AllExecuedAndMined())} invariant(s)");
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

        private static List<string> GetMinerConfigArgs(ICompilerConfiguration configuration, TraceMetadata metadata, Hint hint, PInferPredicateGenerator codegen, string invOutDir)
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
            args.Add("-od");
            args.Add(invOutDir);
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

    internal class Goal {
        [System.Text.Json.Serialization.JsonPropertyName("events")]
        public List<string> Events { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("config_event")]
        public string ConfigEvent { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("exists")]
        public int NumExists { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("term_depth")]
        public int TermDepth { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("guards")]
        public List<string> Guards { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("filters")]
        public List<string> Filters { get; set; }
    }

    internal class MonitorMetadata {
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("spec")]
        public string Specification { get; set; }
    }
}
