using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using LiteDB;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.AST.Statements;
using Plang.Compiler.TypeChecker.AST.States;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.PVerifier;

public class PVerifierCache
{
    public string Reply { get; set; }
    public byte[] Checksum { get; set; }
}

public class PVerifierCodeGenerator : ICodeGenerator
{
    private CompilationContext _ctx;
    private CompiledFile _src;
    private HashSet<PLanguageType> _optionsToDeclare;
    private HashSet<PLanguageType> _chooseToDeclare;
    private HashSet<PLanguageType> _setCheckersToDeclare;
    private Dictionary<Event, List<string>> _specListenMap; // keep track of the procedure names for each event
    private Dictionary<string, ProofCommand> _fileToProofCommands;
    private Dictionary<ProofCommand, List<string>> _proofCommandToFiles;
    private List<ProofCommand> _commands;
    private Dictionary<Invariant, HashSet<Invariant>> _invariantDependencies;
    private HashSet<Invariant> _provenInvariants;
    private Scope _globalScope;
    private Invariant _defaultInv;

    public bool HasCompilationStage => true;

    private byte[] ComputeCheckSum(FileStream stream)
    {
        string file_content = new StreamReader(stream).ReadToEnd();
        // ignore `set_solver_option` statements
        file_content = Regex.Replace(file_content, @"set_solver_option\((.*)\);", "", RegexOptions.Multiline);
        using (var md5 = MD5.Create())
        {
            return md5.ComputeHash(Encoding.UTF8.GetBytes(file_content));
        }
    }

    public void Compile(ICompilerConfiguration job)
    {
        HashSet<string> failMessages = [];
        HashSet<Invariant> succeededInv = [];
        HashSet<Invariant> failedInv = [];
        var missingDefault = true;

        // Open database (or create if doesn't exist)
        var db = new LiteDatabase(Path.Join(job.OutputDirectory.FullName, ".verifier-cache.db"));
        // Get a collection (or create, if doesn't exist)
        var qCollection = db.GetCollection<PVerifierCache>("qCollection");

        int parallelism = job.Parallelism;
        if (parallelism == 0)
        {
            parallelism = Environment.ProcessorCount;
        }

        foreach (var cmd in _commands)
        {
            if (cmd.Name.Contains("default"))
            {
                missingDefault = false;
            }

            job.Output.WriteInfo($"Proving: {cmd.Name}");
            Dictionary<string, bool> checklist = _proofCommandToFiles[cmd].ToDictionary(x => x, x => false);
            Dictionary<string, Process> tasks = [];

            // prefill (check cache for everything, but only spin up `parallelism` number of runs 
            foreach (var f in checklist)
            {
                using (var stream = File.OpenRead(Path.Join(job.OutputDirectory.FullName, f.Key)))
                {
                    var checksum = ComputeCheckSum(stream);
                    var hit = qCollection.FindOne(x => x.Checksum == checksum);
                    if (hit != null)
                    {
                        checklist[f.Key] = true;
                        var currUndefs = Regex.Matches(hit.Reply, @"UNDEF -> (.*), line (\d+)");
                        var currFails = Regex.Matches(hit.Reply, @"FAILED -> (.*), line (\d+)");
                        var (invs, failed, msgs) =
                            AggregateResults(job, f.Key, currUndefs.ToList(), currFails.ToList());
                        succeededInv.UnionWith(invs);
                        failedInv.UnionWith(failed);
                        failMessages.UnionWith(msgs);
                    }
                    else if (tasks.Count < parallelism)
                    {
                        var args = new[] { "-M", f.Key };
                        tasks.Add(f.Key, Compiler.NonBlockingRun(job.OutputDirectory.FullName, "uclid", args));
                    }
                }
            }

            var numCompleted = checklist.Values.Sum(x => x ? 1 : 0);
            Console.Write($"\r🔍 Checked {numCompleted}/{checklist.Count} goals...");

            // fetch
            while (checklist.ContainsValue(false))
            {
                Dictionary<string, Process> newTasks = [];
                foreach (var r in tasks)
                {
                    if (!r.Value.HasExited || checklist[r.Key]) continue;
                    // checklist is true if we've already done this
                    checklist[r.Key] = true;

                    var exitCode = Compiler.WaitForResult(r.Value, out var stdout, out var stderr);
                    if (exitCode != 0)
                    {
                        throw new TranslationException($"Verifying generated UCLID5 code FAILED ({r.Key})!\n" +
                                                       $"{stdout}\n" +
                                                       $"{stderr}\n");
                    }

                    r.Value.Kill();

                    var currUndefs = Regex.Matches(stdout, @"UNDEF -> (.*), line (\d+)");
                    var currFails = Regex.Matches(stdout, @"FAILED -> (.*), line (\d+)");
                    var (invs, failed, msgs) = AggregateResults(job, r.Key, currUndefs.ToList(), currFails.ToList());
                    succeededInv.UnionWith(invs);
                    failedInv.UnionWith(failed);
                    failMessages.UnionWith(msgs);
                    // cache the results only when no invariant times out
                    if (currUndefs.Count == 0)
                    {
                        // add stdout to the database along with the corresponding checksum of the uclid query
                        using var stream = File.OpenRead(Path.Join(job.OutputDirectory.FullName, r.Key));
                        var newResult = new PVerifierCache
                        {
                            Reply = stdout,
                            Checksum = ComputeCheckSum(stream)
                        };
                        qCollection.Insert(newResult);
                    }

                    // find someone that hasn't run and isn't running and run it
                    var newTask = checklist.FirstOrDefault(x =>
                        x.Value == false && !tasks.ContainsKey(x.Key) && !newTasks.ContainsKey(x.Key)).Key;
                    if (newTask == null) continue;
                    var args = new[] { "-M", newTask };
                    newTasks.Add(newTask, Compiler.NonBlockingRun(job.OutputDirectory.FullName, "uclid", args));
                }

                if (newTasks.Count == 0)
                {
                    Thread.Sleep(500);
                }
                else
                {
                    newTasks.ToList().ForEach(x => tasks.Add(x.Key, x.Value));
                }

                numCompleted = checklist.Values.Sum(x => x ? 1 : 0);
                Console.Write($"\r🔍 Checked {numCompleted}/{checklist.Count} goals...");
            }

            Console.WriteLine();
        }

        succeededInv.ExceptWith(failedInv);
        job.Output.WriteInfo($"\n🎉 Verified {succeededInv.Count} invariants!");
        foreach (var inv in succeededInv)
        {
            if (!inv.IsDefault)
            {
                job.Output.WriteInfo($"✅ {inv.Name.Replace("_PGROUP_", ": ")}");
            }
            else
            {
                job.Output.WriteInfo($"✅ default P proof obligations");
            }

        }

        MarkProvenInvariants(succeededInv);
        ShowRemainings(job, failedInv, missingDefault);
        if (failMessages.Count > 0)
        {
            job.Output.WriteInfo($"❌ Failed to verify {failMessages.Count} {(failMessages.Count > 1 ? "properties" : "property")}!");
            foreach (var msg in failMessages)
            {
                job.Output.WriteError(msg);
            }
        }

        db.Dispose();
    }

    private void MarkProvenInvariants(HashSet<Invariant> succeededInv)
    {
        foreach (var inv in _invariantDependencies.Keys)
        {
            _invariantDependencies[inv].ExceptWith(succeededInv);
        }

        foreach (var inv in _invariantDependencies.Keys)
        {
            if (succeededInv.Contains(inv))
            {
                _provenInvariants.Add(inv);
            }
        }
    }

    private void ShowRemainings(ICompilerConfiguration job, HashSet<Invariant> failedInv, bool missingDefault)
    {
        HashSet<Invariant> remaining = [];
        foreach (var inv in _provenInvariants)
        {
            foreach (var dep in _invariantDependencies[inv])
            {
                if (!failedInv.Contains(dep) && !_provenInvariants.Contains(dep))
                {
                    remaining.Add(dep);
                }
            }
        }

        if (remaining.Count > 0 || missingDefault)
        {
            job.Output.WriteWarning("❓ Remaining Goals:");
            foreach (var inv in remaining)
            {
                job.Output.WriteWarning($"- {inv.Name.Replace("_PGROUP_", ": ")} at {GetLocation(inv)}");
            }

            if (missingDefault)
            {
                job.Output.WriteWarning($"- default P proof obligations");
            }
        }
    }

    private void ProcessFailureMessages(List<Match> collection, string[] query, string reason, List<string> failedInv,
        List<string> failMessages)
    {
        foreach (Match match in collection)
        {
            foreach (var feedback in match.Groups[1].Captures.Zip(match.Groups[2].Captures))
            {
                var line = query[int.Parse(feedback.Second.ToString()) - 1];
                var matchName = Regex.Match(line, @"// Failed to verify invariant (.*) at (.*)");
                if (matchName.Success)
                {
                    var invName = matchName.Groups[1].Value.Replace("_PGROUP_", ": ");
                    failedInv.Add(invName);
                    failMessages.Add($"{reason} {line.Split("// ").Last()}");
                }

                var matchDefault = Regex.Match(line,
                    @"(// Failed to verify that (.*) never receives (.*) in (.*)|// Failed to ensure unique action IDs at (.*)|// Failed to ensure increasing action IDs at (.*)|// Failed to ensure that received is a subset of sent at (.*))");
                if (matchDefault.Success)
                {
                    failedInv.Add("default");
                    failMessages.Add($"{reason} {line.Split("// ").Last()}");
                }

                var matchLoopInvs = Regex.Match(line,
                    @"// Failed to verify loop invariant at (.*)");
                if (matchLoopInvs.Success)
                {
                    var msg = $"{reason} {line.Split("// ").Last()}";
                    failMessages.Add(msg);
                    failedInv.Add("loop invariant @ " + matchLoopInvs.Groups[1].Value);
                }

                var assertFails = Regex.Match(line,
                    @"// Failed to verify assertion at (.*)");
                if (assertFails.Success)
                {
                    var parts = line.Split("// ");
                    var assertStmt = parts[0].Trim().Replace(";", "");
                    var locInfo = assertFails.Groups[1].Value;
                    var msg = $"{reason} {assertStmt} at {locInfo} failed";
                    failMessages.Add(msg);
                    failedInv.Add($"{assertStmt} @ " + locInfo);
                }
            }
        }
    }

    // returns invariants that were successfully verified and failure messages
    private (HashSet<Invariant>, HashSet<Invariant>, List<string>) AggregateResults(ICompilerConfiguration job,
        string filename, List<Match> undefs, List<Match> fails)
    {
        var cmd = _fileToProofCommands[filename];
        var query = File.ReadLines(job.OutputDirectory.FullName + "/" + filename).ToArray();
        List<string> failedInv = [];
        List<string> failMessages = [];
        ProcessFailureMessages(fails, query, "❌", failedInv, failMessages);
        ProcessFailureMessages(undefs, query, "❓", failedInv, failMessages);
        if (failedInv.Count == 0)
        {
            return ([.. cmd.Goals], [], []);
        }

        HashSet<Invariant> succeededInv = [];
        foreach (var inv in cmd.Goals)
        {
            if (!failedInv.Contains(inv.Name))
            {
                succeededInv.Add(inv);
            }
        }

        return (succeededInv, failedInv.Select(x =>
        {
            if (x == "default") {
                return _defaultInv;
            }
            _globalScope.Get(x, out Invariant i);
            return i;
        }).ToHashSet(), failMessages);
    }

    public IEnumerable<CompiledFile> GenerateCode(ICompilerConfiguration job, Scope globalScope)
    {
        _ctx = new CompilationContext(job);
        _optionsToDeclare = [];
        _chooseToDeclare = [];
        _specListenMap = new Dictionary<Event, List<string>>();
        _setCheckersToDeclare = [];
        _fileToProofCommands = [];
        _invariantDependencies = [];
        _provenInvariants = [];
        _proofCommandToFiles = [];
        _commands = [];
        _globalScope = globalScope;
        BuildDependencies(globalScope);
        PopulateSpecHandlers((from m in _globalScope.AllDecls.OfType<Machine>() where m.IsSpec select m).ToList());
        var filenamePrefix = $"{job.ProjectName}_";
        List<CompiledFile> files = [];
        // if there are no proof commands, then create two:
        // - one called default for checking that everything is handled and sanity checkings
        // - one with all the invariants as goals
        if (!globalScope.ProofCommands.Any())
        {
            var defaultProof = new ProofCommand("default", null)
            {
                Goals = [],
                Premises = globalScope.AllDecls.OfType<Invariant>().ToList(),
            };
            _commands.Add(defaultProof);
            _proofCommandToFiles.Add(defaultProof, []);
            files.AddRange(CompileToFile($"{filenamePrefix}default", defaultProof));

            var fullProof = new ProofCommand("full", null)
            {
                Goals = globalScope.AllDecls.OfType<Invariant>().ToList(),
                Premises = [],
            };
            _commands.Add(fullProof);
            _proofCommandToFiles.Add(fullProof, []);
            files.AddRange(CompileToFile($"{filenamePrefix}full", fullProof));
        }
        else
        {
            // otherwise, go through all the proof commands (or ones that are specified in the compiler config)
            bool emitCode (ProofCommand cmd) => job.TargetProofBlocks.Count == 0 ||
                                                    (cmd.ProofBlock != null && job.TargetProofBlocks.Contains(cmd.ProofBlock));
            foreach (var pbname in job.TargetProofBlocks)
            {
                if (!globalScope.Get(pbname, out ProofBlock pb))
                {
                    job.Output.WriteWarning($"Warning: proof block {pbname} not found. Skipping ...");
                }
            }
            foreach (var proofCmd in globalScope.ProofCommands.Where(emitCode))
            {
                // if one of them is the default, rename it to default so that we can generate the init, next, and invs in compileToFile
                // TODO: ensure that default can only happen on its own?
                if (proofCmd.Goals.Count == 1 && proofCmd.Goals[0].IsDefault)
                {
                    proofCmd.Name = "default";
                    _defaultInv = proofCmd.Goals[0];
                    _commands.Add(proofCmd);
                    _proofCommandToFiles.Add(proofCmd, []);
                    files.AddRange(CompileToFile($"{filenamePrefix}default", proofCmd));
                }
                else
                {
                    _proofCommandToFiles.Add(proofCmd, []);
                    files.AddRange(CompileToFile($"{filenamePrefix}{proofCmd.Name.Replace(",", "_").Replace(" ", "_")}",
                        proofCmd));
                    _commands.Add(proofCmd);
                }
            }
        }

        return files;
    }

    private void PopulateSpecHandlers(IEnumerable<Machine> specs)
    {
        foreach (var spec in specs)
        {
            var events = spec.Observes.Events;
            foreach (var e in events)
            {
                var procedureName = $"{SpecPrefix}{spec.Name}_{e.Name}";
                if (_specListenMap.ContainsKey(e))
                {
                    _specListenMap[e].Add(procedureName);
                }
                else
                {
                    _specListenMap.Add(e, [procedureName]);
                }
            }
        }
    }

    private CompiledFile GenerateCompiledFile(ProofCommand cmd, string name, Machine m, State s, Event e)
    {
        var filename = name;

        if (m != null)
        {
            filename += "_" + m.Name;
        }

        if (s != null)
        {
            filename += "_" + s.Name;
        }

        if (e != null)
        {
            filename += "_" + e.Name;
        }
        var md5 = MD5.Create();
        byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(filename));
        StringBuilder builder = new StringBuilder();
        foreach (byte b in bytes) {
            builder.Append(b.ToString("x2")); // Convert to hexadecimal string
        }
        filename = builder + ".ucl";

        var file = new CompiledFile(filename);
        _fileToProofCommands.Add(filename, cmd);
        _proofCommandToFiles[cmd].Add(filename);
        return file;
    }

    private List<CompiledFile> CompileToFile(string name, ProofCommand cmd)
    {
        // We essentially just want to call GenerateMain for every relevant bit of the proof command while filtering for checkOnly
        var machines = (from m in _globalScope.AllDecls.OfType<Machine>() where !m.IsSpec select m).ToList();
        var events = _globalScope.AllDecls.OfType<Event>().ToList();
        List<CompiledFile> files = [];

        if (cmd.Name == "default")
        {
            _src = GenerateCompiledFile(cmd, name, null, null, null);
            files.Add(_src);
            GenerateMain(null, null, null, cmd);
        }
        foreach (var m in machines)
        {
            if (_ctx.Job.CheckOnly == null || _ctx.Job.CheckOnly.Contains(m.Name))
            {
                foreach (var s in m.States)
                {
                    if (_ctx.Job.CheckOnly == null || _ctx.Job.CheckOnly.Contains(s.Name))
                    {
                        _src = GenerateCompiledFile(cmd, name, m, s, null);
                        files.Add(_src);
                        GenerateMain(m, s, null, cmd, cmd.Name == "default");
                    }

                    foreach (var e in events.Where(e => !e.IsNullEvent && !e.IsHaltEvent && s.HasHandler(e)))
                    {
                        if (_ctx.Job.CheckOnly == null || _ctx.Job.CheckOnly.Contains(e.Name))
                        {
                            _src = GenerateCompiledFile(cmd, name, m, s, e);
                            files.Add(_src);
                            GenerateMain(m, s, e, cmd, cmd.Name == "default");
                        }
                    }
                }
            }
        }

        Console.WriteLine($"Generated {files.Count} files for {name}");
        return files;
    }

    private void BuildDependencies(Scope globalScope)
    {   
        foreach (var cmd in globalScope.ProofCommands)
        {
            foreach (var goal in cmd.Goals)
            {
                foreach (var dep in cmd.Premises)
                {
                    if (goal == dep) continue;
                    if (!_invariantDependencies.ContainsKey(goal))
                    {
                        _invariantDependencies.Add(goal, []);
                    }

                    _invariantDependencies[goal].Add(dep);
                }
            }
        }
    }

    private string ShowInvariant(IPExpr e)
    {
        if (e is InvariantRefExpr inv) return inv.Invariant.Name;
        else return e.SourceLocation.GetText();
    }

    private void EmitLine(string str)
    {
        _ctx.WriteLine(_src.Stream, str);
    }

    // Prefixes to avoid name clashes and keywords
    private static string BuiltinPrefix => "P_";
    private static string UserPrefix => "User_";
    private static string EventPrefix => "Event_";
    private static string GotoPrefix => "PGoto_";
    private static string MachinePrefix => "PMachine_";
    private static string LocalPrefix => "PLocal_";
    private static string OptionPrefix => "POption_";
    private static string ChoosePrefix => "PChoose_";
    private static string CheckerPrefix => "PChecklist_";
    private static string SpecPrefix => "PSpec_";
    private static string InvariantPrefix => "PInv_";

    // P values that don't have a direct UCLID5 equivalent
    private static string PNull => $"{BuiltinPrefix}Null";
    private static string PNullDeclaration => $"type {PNull} = enum {{{PNull}}};";

    private static string DefaultMachineRef => $"{BuiltinPrefix}DefaultMachine";
    private static string DefaultMachineDeclaration => $"const {DefaultMachineRef}: {MachineRefT};";

    // P types that don't have a direct UCLID5 equivalent
    private static string StringT => $"{BuiltinPrefix}String";
    private static string StringTDeclaration => $"type {StringT};";
    private static string DefaultString => $"{BuiltinPrefix}DefaultString";
    private static string DefaultStringDeclaration => $"const {DefaultString}: {StringT};";

    /********************************
     * type StateADT = record {sent: [LabelADT]boolean, received: [LabelADT]boolean, machines: [MachineRefT]MachineStateADT};
     * var state: StateT;
     *******************************/
    private static string StateAdt => $"{BuiltinPrefix}StateAdt";
    private static string StateAdtSentSelector => $"{StateAdt}_Sent";
    private static string StateAdtReceivedSelector => $"{StateAdt}_Received";
    private static string StateAdtMachinesSelector => $"{StateAdt}_Machines";

    private static bool useLocalPrefix = false;

    private static string StateAdtConstruct(string sent, string received, string machines)
    {
        return
            $"const_record({StateAdtSentSelector} := {sent}, {StateAdtReceivedSelector} := {received}, {StateAdtMachinesSelector} := {machines})";
    }

    private static string StateAdtSelectSent(string state)
    {
        return $"{state}.{StateAdtSentSelector}";
    }

    private static string StateAdtSelectReceived(string state)
    {
        return $"{state}.{StateAdtReceivedSelector}";
    }

    private static string StateAdtSelectMachines(string state)
    {
        return $"{state}.{StateAdtMachinesSelector}";
    }

    private static string StateAdtDeclaration()
    {
        return
            $"type {StateAdt} = record {{{StateAdtSentSelector}: [{LabelAdt}]boolean, {StateAdtReceivedSelector}: [{LabelAdt}]boolean, {StateAdtMachinesSelector}: [{MachineRefT}]{MachineStateAdt}}};";
    }

    private static string InFlight(string state, string action)
    {
        return useLocalPrefix ? $"({LocalPrefix}sent[{action}] && !{StateAdtSelectReceived(state)}[{action}])" : $"({StateAdtSelectSent(state)}[{action}] && !{StateAdtSelectReceived(state)}[{action}])";
    }

    private static string StateVar => $"{BuiltinPrefix}State";
    private static string StateVarDeclaration => $"var {StateVar}: {StateAdt};";

    private static string Deref(string r)
    {
        return $"{StateAdtSelectMachines(StateVar)}[{r}]";
    }

    /********************************
     * type MachineRef;
     *
     * type MachineStateADT = record {stage: boolean; machine: MachineADT};
     *
     * // where Mi are the declared machines, Si their P state names, and MiFjl their fields
     * type MachineADT = | M0(M0_State: S0, M0F00, ..., M0F0n)
     *                   | ...
     *                   | Mk(Mk_State: Sk, MkFk0, ..., MkFkm)
     *******************************/
    private static string MachineRefT => $"{MachinePrefix}Ref_t";
    private static string MachineRefTDeclaration => $"type {MachineRefT};";
    private static string MachineStateAdt => $"{MachinePrefix}State_ADT";
    private static string MachineStateAdtStageSelector => $"{MachineStateAdt}_Stage";
    private static string MachineStateAdtMachineSelector => $"{MachineStateAdt}_Machine";

    private static string MachineStateAdtConstruct(string stage, string machine)
    {
        return
            $"const_record({MachineStateAdtStageSelector} := {stage}, {MachineStateAdtMachineSelector} := {machine})";
    }

    private static string MachineStateAdtSelectStage(string state)
    {
        return $"{state}.{MachineStateAdtStageSelector}";
    }

    private static string MachineStateAdtSelectMachine(string state)
    {
        return $"{state}.{MachineStateAdtMachineSelector}";
    }

    private static string MachineStateAdtDeclaration()
    {
        return
            $"type {MachineStateAdt} = record {{{MachineStateAdtStageSelector}: boolean, {MachineStateAdtMachineSelector}: {MachineAdt}}};";
    }

    private static string MachineAdt => $"{MachinePrefix}ADT";

    private string MachineAdtDeclaration(List<Machine> machines)
    {
        var sum = string.Join("\n\t\t| ", machines.Select(ProcessMachine));
        return $"datatype {MachineAdt} = \n\t\t| {sum};";

        string ProcessMachine(Machine m)
        {
            var fields = string.Join(", ",
                m.Fields.Select(f => $"{MachinePrefix}{m.Name}_{f.Name}: {TypeToString(f.Type)}"));
            if (m.Fields.Any()) fields = ", " + fields;

            return $"{MachinePrefix}{m.Name} ({MachinePrefix}{m.Name}_State: {MachinePrefix}{m.Name}_StateAdt{fields})";
        }
    }

    private static string MachineAdtConstructM(Machine m, List<string> args)
    {
        return $"{MachinePrefix}{m.Name}({string.Join(", ", args)})";
    }

    private static string MachineAdtSelectState(string instance, Machine m)
    {
        return $"{instance}.{MachinePrefix}{m.Name}_State";
    }

    private static string MachineStateAdtSelectState(string state, Machine m)
    {
        return MachineAdtSelectState(MachineStateAdtSelectMachine(state), m);
    }

    private static string MachineAdtSelectField(string instance, Machine m, Variable f)
    {
        return $"{instance}.{MachinePrefix}{m.Name}_{f.Name}";
    }

    private static string MachineStateAdtSelectField(string state, Machine m, Variable f)
    {
        return MachineAdtSelectField(MachineStateAdtSelectMachine(state), m, f);
    }

    private static string MachinePStateDeclaration(Machine m)
    {
        var states = string.Join(", ", m.States.Select(s => $"{MachinePrefix}{m.Name}_{s.Name}"));
        return $"type {MachinePrefix}{m.Name}_StateAdt = enum {{{states}}};";
    }

    private static string MachineAdtIsM(string instance, Machine machine)
    {
        return $"is_{MachinePrefix}{machine.Name}({instance})";
    }

    private static string MachineStateAdtIsM(string state, Machine machine)
    {
        return $"is_{MachinePrefix}{machine.Name}({MachineStateAdtSelectMachine(state)})";
    }

    private static string MachineStateAdtInS(string state, Machine m, State s)
    {
        return
            $"({MachineStateAdtIsM(state, m)} && {MachineStateAdtSelectState(state, m)} == {MachinePrefix}{m.Name}_{s.Name})";
    }


    private static string InStartPredicateDeclaration(List<Machine> machines)
    {
        var input = $"{LocalPrefix}state";
        var cases = machines.Select(ProcessMachine).ToList();
        var body = cases.Aggregate("false", (acc, pair) => $"if ({pair.Item1}) then ({pair.Item2})\n\t\telse ({acc})");
        return $"define {BuiltinPrefix}InStart({input}: {MachineStateAdt}) : boolean =\n\t\t{body};";

        (string, string) ProcessMachine(Machine m)
        {
            var machine = $"{MachineStateAdtSelectMachine(input)}";
            var state = $"{machine}.{MachinePrefix}{m.Name}_State";
            var start = $"{MachinePrefix}{m.Name}_{m.StartState.Name}";
            var check = $"{state} == {start}";
            var guard = MachineAdtIsM(machine, m);
            return (guard, check);
        }
    }

    private static string InEntryPredicateDeclaration()
    {
        var input = $"{LocalPrefix}state";
        return
            $"define {BuiltinPrefix}InEntry({input}: {MachineStateAdt}) : boolean = {MachineStateAdtSelectStage(input)};";
    }

    /********************************
     * type LabelAdt = record {target: MachineRef, action: EitherEventOrGotoAdt}
     *
     * datatype EitherEventOrGotoAdt = | EventLabel (event: EventAdt)
     *                                 | GotoLabel (goto: GotoAdt)
     *
     * // where Ei is an event, Pi is the payload of the event and Ti is the type of the payload
     * datatype EventAdt = | E0 (P0: T0)
     *                     | ...
     *                     | En (Pn: Tn)
     *
     * // where Si are as in MachineAdt and A0 are the arguments to the entry handler of the state
     * datatype GotoAdt = | M0 (M0_State: S0, A0: T0)
     *                    | ...
     *                    | Mn (Mn_State: Sn, An: Tn)
     *******************************/
    private static string LabelAdt => $"{BuiltinPrefix}Label";
    private static string LabelAdtTargetSelector => $"{LabelAdt}_Target";
    private static string LabelAdtActionSelector => $"{LabelAdt}_Action";
    private static string LabelAdtActionCountSelector => $"{LabelAdt}_ActionCount";

    private static string LabelAdtDeclaration()
    {
        return
            $"type {LabelAdt} = record {{{LabelAdtTargetSelector}: {MachineRefT}, {LabelAdtActionSelector}: {EventOrGotoAdt}, {LabelAdtActionCountSelector}: integer}};";
    }

    private void IncrementActionCount()
    {
        EmitLine($"{BuiltinPrefix}ActionCount = {BuiltinPrefix}ActionCount + 1;");
    }

    private static string LabelAdtConstruct(string target, string action)
    {
        return
            $"const_record({LabelAdtTargetSelector} := {target}, {LabelAdtActionSelector} := {action}, {LabelAdtActionCountSelector} := {BuiltinPrefix}ActionCount)";
    }

    private static string LabelAdtSelectTarget(string label)
    {
        return $"{label}.{LabelAdtTargetSelector}";
    }

    private static string LabelAdtSelectAction(string label)
    {
        return $"{label}.{LabelAdtActionSelector}";
    }

    private static string LabelAdtSelectActionCount(string label)
    {
        return $"{label}.{LabelAdtActionCountSelector}";
    }


    private static string EventOrGotoAdt => $"{BuiltinPrefix}EventOrGoto";
    private static string EventOrGotoAdtEventConstructor => $"{EventOrGotoAdt}_Event";
    private static string EventOrGotoAdtGotoConstructor => $"{EventOrGotoAdt}_Goto";
    private static string EventOrGotoAdtEventSelector => $"{EventOrGotoAdt}_Event_Event";
    private static string EventOrGotoAdtGotoSelector => $"{EventOrGotoAdt}_Goto_Goto";

    private static string EventOrGotoAdtDeclaration()
    {
        var e = $"| {EventOrGotoAdtEventConstructor}({EventOrGotoAdtEventSelector}: {EventAdt})";
        var g = $"| {EventOrGotoAdtGotoConstructor}({EventOrGotoAdtGotoSelector}: {GotoAdt})";
        return $"datatype {EventOrGotoAdt} = \n\t\t{e}\n\t\t{g};";
    }

    private static string EventOrGotoAdtConstructEvent(string e)
    {
        return $"{EventOrGotoAdtEventConstructor}({e})";
    }

    private string EventOrGotoAdtConstructEvent(Event ev, IPExpr arg)
    {
        var payload = arg is null ? "" : ExprToString(arg);
        var e = EventAdtConstruct(payload, ev);
        return $"{EventOrGotoAdtEventConstructor}({e})";
    }


    private static string EventOrGotoAdtConstructGoto(string g)
    {
        return $"{EventOrGotoAdtGotoConstructor}({g})";
    }

    private string EventOrGotoAdtConstructGoto(State state, IPExpr payload)
    {
        var g = GotoAdtConstruct(state, payload);
        return $"{EventOrGotoAdtGotoConstructor}({g})";
    }

    private static string EventOrGotoAdtSelectEvent(string eventOrGoto)
    {
        return $"{eventOrGoto}.{EventOrGotoAdtEventSelector}";
    }

    private static string EventOrGotoAdtSelectGoto(string eventOrGoto)
    {
        return $"{eventOrGoto}.{EventOrGotoAdtGotoSelector}";
    }

    private static string EventOrGotoAdtIsEvent(string eventOrGoto)
    {
        return $"is_{EventOrGotoAdtEventConstructor}({eventOrGoto})";
    }

    private static string EventOrGotoAdtIsGoto(string eventOrGoto)
    {
        return $"is_{EventOrGotoAdtGotoConstructor}({eventOrGoto})";
    }

    private static string EventAdt => $"{EventPrefix}Adt";

    private string EventAdtDeclaration(List<Event> events)
    {
        var declarationSum = string.Join("\n\t\t| ", events.Select(EventDeclarationCase));
        return $"datatype {EventAdt} = \n\t\t| {declarationSum};";

        string EventDeclarationCase(Event e)
        {
            var pt = e.PayloadType.IsSameTypeAs(PrimitiveType.Null)
                ? ""
                : $"{EventPrefix}{e.Name}_Payload: {TypeToString(e.PayloadType)}";
            return
                $"{EventPrefix}{e.Name} ({pt})";
        }
    }

    private static string EventAdtSelectPayload(string eadt, Event e)
    {
        return $"{eadt}.{EventPrefix}{e.Name}_Payload";
    }

    private static string EventAdtConstruct(string payload, Event e)
    {
        return $"{EventPrefix}{e.Name}({payload})";
    }

    private static string EventAdtIsE(string instance, Event e)
    {
        return $"is_{EventPrefix}{e.Name}({instance})";
    }

    private static string EventOrGotoAdtIsE(string instance, Event e)
    {
        var isEvent = EventOrGotoAdtIsEvent(instance);
        var selectEvent = EventOrGotoAdtSelectEvent(instance);
        var correctEvent = EventAdtIsE(selectEvent, e);
        return $"({isEvent} && {correctEvent})";
    }

    private static string LabelAdtIsE(string instance, Event e)
    {
        var action = LabelAdtSelectAction(instance);
        return EventOrGotoAdtIsE(action, e);
    }

    private static string LabelAdtSelectPayloadField(string instance, Event e, NamedTupleEntry field)
    {
        var action = LabelAdtSelectAction(instance);
        return $"{EventAdtSelectPayload(EventOrGotoAdtSelectEvent(action), e)}.{field.Name}";
    }

    private static string GotoAdt => $"{GotoPrefix}Adt";

    private string GotoAdtDeclaration(IEnumerable<Machine> machines)
    {
        List<(State, Variable)> gotos = [];
        foreach (var m in machines)
        foreach (var s in m.States)
        {
            var f = s.Entry;
            // get the arguments to the entry handler
            Variable a = null;
            if (f is not null && s.Entry.Signature.Parameters.Count > 0)
            {
                a = s.Entry.Signature.Parameters[0];
            }

            gotos.Add((s, a));
        }

        var sum = string.Join("\n\t\t| ", gotos.Select(ProcessGoto));

        return $"datatype {GotoAdt} = \n\t\t| {sum};";

        string ProcessGoto((State, Variable) g)
        {
            var prefix = $"{GotoPrefix}{g.Item1.OwningMachine.Name}_{g.Item1.Name}";
            if (g.Item2 is null)
            {
                return prefix + "()";
            }

            return prefix + $"({prefix}_{g.Item2.Name}: {TypeToString(g.Item2.Type)})";
        }
    }

    private string GotoAdtConstruct(State s, IPExpr p)
    {
        var payload = p is null ? "" : ExprToString(p);
        return $"{GotoPrefix}{s.OwningMachine.Name}_{s.Name}({payload})";
    }

    private string GotoAdtSelectParam(string instance, string param, State s)
    {
        return $"{instance}.{GotoPrefix}{s.OwningMachine.Name}_{s.Name}_{param}";
    }

    private static string GotoAdtIsS(string instance, State s)
    {
        return $"is_{GotoPrefix}{s.OwningMachine.Name}_{s.Name}({instance})";
    }

    private static string EventOrGotoAdtIsS(string instance, State s)
    {
        var isGoto = EventOrGotoAdtIsGoto(instance);
        var selectGoto = EventOrGotoAdtSelectGoto(instance);
        var correctGoto = GotoAdtIsS(selectGoto, s);
        return $"({isGoto} && {correctGoto})";
    }

    private static string LabelAdtIsS(string instance, State s)
    {
        var action = LabelAdtSelectAction(instance);
        return EventOrGotoAdtIsS(action, s);
    }


    /********************************
     * Spec machines are treated differently than regular machines. For example, there is only ever a single instance of
     * each spec machine.
     *
     * To handle spec machines,
     *  1) we create a global variable for each of their fields;
     *  2) keep track of what events the spec machines are listening to and the corresponding handleers;
     *  3) create a procedure per spec machine handler that operates on the variables from (1); and
     *  4) whenever a regular machine sends an event, we call the appropriate handler from (2).
     *******************************/
    private void SpecVariableDeclarations(List<Machine> specs)
    {
        foreach (var spec in specs)
        {
            EmitLine(
                $"type {SpecPrefix}{spec.Name}_StateAdt = enum {{{string.Join(", ", spec.States.Select(n => $"{SpecPrefix}{spec.Name}_{n.Name}"))}}};");
            EmitLine($"var {SpecPrefix}{spec.Name}_State: {SpecPrefix}{spec.Name}_StateAdt;");
            foreach (var f in spec.Fields)
            {
                EmitLine($"var {SpecPrefix}{spec.Name}_{f.Name}: {TypeToString(f.Type)};");
            }
        }
    }

    private void GenerateSpecProcedures(List<Machine> specs, List<Invariant> goals, bool generateSanityChecks = false)
    {
        foreach (var spec in specs)
        {
            foreach (var f in spec.Methods)
            {
                var ps = f.Signature.Parameters.Select(p => $"{GetLocalName(p)}: {TypeToString(p.Type)}");
                var line = _ctx.LocationResolver.GetLocation(f.SourceLocation).Line;
                var name = f.Name == "" ? $"{BuiltinPrefix}{f.Owner.Name}_f{line}" : f.Name;
                EmitLine($"procedure [inline] {name}({string.Join(", ", ps)})");
                if (!f.Signature.ReturnType.IsSameTypeAs(PrimitiveType.Null))
                {
                    EmitLine($"\treturns ({BuiltinPrefix}Return: {TypeToString(f.Signature.ReturnType)})");
                }

                EmitLine("{");

                // declare local variables corresponding to the global spec variables
                EmitLine($"var {LocalPrefix}state: {SpecPrefix}{spec.Name}_StateAdt;");
                EmitLine($"var {LocalPrefix}sent: [{LabelAdt}]boolean;");
                foreach (var v in spec.Fields)
                {
                    EmitLine($"var {GetLocalName(v)}: {TypeToString(v.Type)};");
                }

                // declare local variables for the method
                foreach (var v in f.LocalVariables) EmitLine($"var {GetLocalName(v)}: {TypeToString(v.Type)};");
                // foreach (var v in f.LocalVariables) EmitLine($"{GetLocalName(v)} = {DefaultValue(v.Type)};");

                // Set the local variables corresponding to the global spec variables to the correct starting value
                EmitLine($"{LocalPrefix}sent = {StateAdtSelectSent(StateVar)};");
                foreach (var v in spec.Fields)
                    EmitLine($"{GetLocalName(v)} = {SpecPrefix}{spec.Name}_{v.Name};");

                useLocalPrefix = true;
                GenerateStmt(f.Body, spec, goals, generateSanityChecks);
                useLocalPrefix = false;

                // update the global variables
                EmitLine($"{SpecPrefix}{spec.Name}_State = {LocalPrefix}state;");
                foreach (var v in spec.Fields)
                {
                    EmitLine($"{SpecPrefix}{spec.Name}_{v.Name} = {GetLocalName(v)};");
                }

                EmitLine("}\n");
            }
        }
    }

    private void GenerateSpecHandlers(List<Machine> specs)
    {
        foreach (var spec in specs)
        {
            var events = spec.Observes.Events;
            foreach (var e in events)
            {
                var procedureName = $"{SpecPrefix}{spec.Name}_{e.Name}";
                Trace.Assert(_specListenMap.ContainsKey(e) && _specListenMap[e].Contains(procedureName),
                    $"Procedure {procedureName} is not generated for Spec {spec.Name} that listens to {e.Name}");

                EmitLine($"procedure [inline] {procedureName}({SpecPrefix}Payload: {TypeToString(e.PayloadType)})");
                EmitLine("{");
                EmitLine("case");
                foreach (var state in spec.States)
                {
                    var handlers = state.AllEventHandlers.ToDictionary();
                    if (state.HasHandler(e))
                    {
                        var handler = handlers[e];
                        var precondition = $"{SpecPrefix}{spec.Name}_State == {SpecPrefix}{spec.Name}_{state.Name}";
                        EmitLine($"({precondition}) : {{");
                        switch (handler)
                        {
                            case EventDoAction eventDoAction:
                                var f = eventDoAction.Target;
                                var line = _ctx.LocationResolver.GetLocation(f.SourceLocation).Line;
                                var name = f.Name == "" ? $"{BuiltinPrefix}{f.Owner.Name}_f{line}" : f.Name;
                                var payload = f.Signature.Parameters.Count > 0 ? $"{SpecPrefix}Payload" : "";
                                EmitLine($"call {name}({payload});");
                                break;
                            default:
                                throw new NotSupportedException(
                                    $"Not supported handler ({handler}) at {GetLocation(handler)}");
                        }

                        EmitLine("}");
                    }
                }

                EmitLine("esac");
                EmitLine("}");
            }
        }
    }

    /********************************
     * Traverse the P AST and generate the UCLID5 code using the types and helpers defined above
     *******************************/
    private void GenerateMain(Machine machine, State state, Event @event, ProofCommand cmd, bool generateSanityChecks = false)
    {
        EmitLine("module main {");

        var machines = (from m in _globalScope.AllDecls.OfType<Machine>() where !m.IsSpec select m).ToList();
        var specs = (from m in _globalScope.AllDecls.OfType<Machine>() where m.IsSpec select m).ToList();
        var events = _globalScope.AllDecls.OfType<Event>().ToList();
        var axioms = _globalScope.AllDecls.OfType<Axiom>().ToList();
        var pures = _globalScope.AllDecls.OfType<Pure>().ToList();

        EmitLine(PNullDeclaration);
        EmitLine(DefaultMachineDeclaration);
        EmitLine(StringTDeclaration);
        EmitLine(DefaultStringDeclaration);
        EmitLine("");

        GenerateUserEnums(_globalScope.AllDecls.OfType<PEnum>());
        GenerateUserTypes(_globalScope.AllDecls.OfType<TypeDef>());
        EmitLine("");

        EmitLine(EventAdtDeclaration(events));
        EmitLine("");
        EmitLine(GotoAdtDeclaration(machines));
        EmitLine("");
        EmitLine(EventOrGotoAdtDeclaration());
        EmitLine(LabelAdtDeclaration());
        EmitLine($"var {BuiltinPrefix}ActionCount: integer;");
        EmitLine("");

        EmitLine(MachineRefTDeclaration);
        foreach (var m in machines)
        {
            EmitLine(MachinePStateDeclaration(m));
        }

        EmitLine(MachineAdtDeclaration(machines));
        EmitLine(MachineStateAdtDeclaration());
        EmitLine("");

        EmitLine(StateAdtDeclaration());
        EmitLine(StateVarDeclaration);
        EmitLine("");

        SpecVariableDeclarations(specs);
        EmitLine("");

        EmitLine(InStartPredicateDeclaration(machines));
        EmitLine(InEntryPredicateDeclaration());
        EmitLine("");

        // global functions
        GenerateGlobalProcedures(_globalScope.AllDecls.OfType<Function>(), cmd.Goals, generateSanityChecks);

        // Spec support
        GenerateSpecProcedures(specs, cmd.Goals, generateSanityChecks); // generate spec methods, called by spec handlers
        GenerateSpecHandlers(specs); // will populate _specListenMap, which is used when ever there is a send statement
        EmitLine("");

        foreach (var pure in pures)
        {
            var args = string.Join(", ",
                pure.Signature.Parameters.Select(p => $"{LocalPrefix}{p.Name}: {TypeToString(p.Type)}"));
            if (pure.Body is null)
            {
                EmitLine($"function {pure.Name}({args}): {TypeToString(pure.Signature.ReturnType)};");
            }
            else
            {
                EmitLine(
                    $"define {pure.Name}({args}): {TypeToString(pure.Signature.ReturnType)} = {ExprToString(pure.Body)};");
            }
        }

        EmitLine("");

        foreach (var inv in cmd.Premises)
        {
            EmitLine($"define {InvariantPrefix}{inv.Name}(): boolean = {ExprToString(inv.Body)};");
        }

        foreach (var inv in cmd.Goals)
        {
            if (!inv.IsDefault) {
                EmitLine($"define {InvariantPrefix}{inv.Name}(): boolean = {ExprToString(inv.Body)};");
            }
        }

        EmitLine("");

        foreach (var ax in axioms)
        {
            EmitLine($"axiom {ExprToString(ax.Body)};");
        }

        EmitLine("");

        // invariants to ensure unique action IDs
        EmitLine(
            $"define {InvariantPrefix}Unique_Actions(): boolean = forall (a1: {LabelAdt}, a2: {LabelAdt}) :: (a1 != a2 && {StateAdtSelectSent(StateVar)}[a1] && {StateAdtSelectSent(StateVar)}[a2]) ==> {LabelAdtSelectActionCount("a1")} != {LabelAdtSelectActionCount("a2")};");
        EmitLine($"invariant _{InvariantPrefix}Unique_Actions: {InvariantPrefix}Unique_Actions();");
        EmitLine(
            $"define {InvariantPrefix}Increasing_Action_Count(): boolean = forall (a: {LabelAdt}) :: {StateAdtSelectSent(StateVar)}[a] ==> {LabelAdtSelectActionCount("a")} < {BuiltinPrefix}ActionCount;");
        EmitLine(
            $"invariant _{InvariantPrefix}Increasing_Action_Count: {InvariantPrefix}Increasing_Action_Count();");
        // invariants to ensure received is a subset of sent
        EmitLine(
            $"define {InvariantPrefix}Received_Subset_Sent(): boolean = forall (a: {LabelAdt}) :: {StateAdtSelectReceived(StateVar)}[a] ==> {StateAdtSelectSent(StateVar)}[a];");
        EmitLine($"invariant _{InvariantPrefix}Received_Subset_Sent: {InvariantPrefix}Received_Subset_Sent();");
        EmitLine("");
        GenerateOptionTypes();
        EmitLine("");

        if (cmd.Name == "default" && machine == null && state == null && @event == null)
        {
            GenerateInitBlock(machines, specs, _globalScope.AllDecls.OfType<AssumeOnStart>());
            EmitLine("");
            GenerateNextBlock(machines, events);
            EmitLine("");

            foreach (var inv in cmd.Premises)
            {
                EmitLine($"invariant _{InvariantPrefix}{inv.Name}: {InvariantPrefix}{inv.Name}(); // Failed to verify invariant {inv.Name.Replace("_PGROUP_", ": ")} at base case");
            }

            EmitLine("");
            GenerateControlBlock(null, null, null, true);
        }
        else
        {
            // non-handler functions for handlers
            GenerateMachineProcedures(machine, cmd.Goals, generateSanityChecks); // generate machine methods, called by handlers below
            EmitLine("");

            // generate the handlers
            if (@event != null)
            {
                GenerateEventHandler(state, @event, cmd.Goals, cmd.Premises, generateSanityChecks);
            }
            else
            {
                GenerateEntryHandler(state, cmd.Goals, cmd.Premises, generateSanityChecks);
            }

            EmitLine("");

            // These have to be done at the end because we don't know what we need until we generate the rest of the code
            EmitLine("");
            GenerateCheckerVars();
            EmitLine("");
            GenerateChooseProcedures();
            EmitLine("");

            GenerateControlBlock(machine, state, @event, false);
        }

        // close the main module
        EmitLine("}");
    }

    private void GenerateUserEnums(IEnumerable<PEnum> enums)
    {
        foreach (var e in enums)
        {
            var variants = string.Join(", ", e.Values.Select(v => UserPrefix + v.Name));
            EmitLine($"type {UserPrefix}{e.Name} = enum {{{variants}}};");
        }
    }

    private void GenerateUserTypes(IEnumerable<TypeDef> types)
    {
        foreach (var t in types) EmitLine($"type {UserPrefix}{t.Name} = {TypeToString(t.Type)};");
    }

    private void GenerateInitBlock(List<Machine> machines, List<Machine> specs, IEnumerable<AssumeOnStart> starts)
    {
        var state = Deref("r");
        EmitLine("init {");
        EmitLine("// Every machine begins in their start state");
        EmitLine($"assume(forall (r: {MachineRefT}) :: {BuiltinPrefix}InStart({state}));");
        EmitLine("// Every machine begins with their entry flag set");
        EmitLine($"assume(forall (r: {MachineRefT}) :: {BuiltinPrefix}InEntry({state}));");
        EmitLine("// The buffer starts completely empty");
        EmitLine($"{StateAdtSelectSent(StateVar)} = const(false, [{LabelAdt}]boolean);");
        EmitLine($"{StateAdtSelectReceived(StateVar)} = const(false, [{LabelAdt}]boolean);");

        EmitLine("// User assumptions");
        foreach (var assumes in starts)
        {
            EmitLine($"assume ({ExprToString(assumes.Body)}); // {assumes.Name}");
        }

        // close the init block
        EmitLine("}");
    }

    private void GenerateNextBlock(List<Machine> machines, List<Event> events)
    {
        var currentLabel = $"{BuiltinPrefix}CurrentLabel";
        // pick a random label and handle it
        EmitLine("next {");
        EmitLine($"var {currentLabel}: {LabelAdt};");
        EmitLine($"if ({InFlight(StateVar, currentLabel)}) {{");
        EmitLine("case");
        foreach (var m in machines)
        {
            foreach (var s in m.States)
            {
                foreach (var e in events.Where(e => !e.IsNullEvent && !e.IsHaltEvent))
                {
                    if (!s.HasHandler(e))
                    {
                        if (_ctx.Job.CheckOnly is null || _ctx.Job.CheckOnly == m.Name ||
                            _ctx.Job.CheckOnly == s.Name || _ctx.Job.CheckOnly == e.Name)
                        {
                            EmitLine($"({EventGuard(m, s, e)}) : {{");
                            EmitLine(
                                $"assert false; // Failed to verify that {m.Name} never receives {e.Name} in {s.Name}");
                            EmitLine("}");
                        }
                    }
                }
            }
        }

        EmitLine("esac");
        EmitLine("}");
        // close the next block
        EmitLine("}");
        return;

        string EventGuard(Machine m, State s, Event e)
        {
            var correctMachine = MachineStateAdtIsM(Deref(LabelAdtSelectTarget(currentLabel)), m);
            var correctState = MachineStateAdtInS(Deref(LabelAdtSelectTarget(currentLabel)), m, s);
            var correctEvent = LabelAdtIsE(currentLabel, e);
            return string.Join(" && ", [correctMachine, correctState, correctEvent]);
        }
    }

    private void GenerateGlobalProcedures(IEnumerable<Function> functions, List<Invariant> goals,
        bool generateSanityChecks = false)
    {
        // TODO: these should be side-effect free and we should enforce that
        foreach (var f in functions)
        {
            var ps = f.Signature.Parameters.Select(p => $"{GetLocalName(p)}: {TypeToString(p.Type)}")
                .Prepend($"this: {MachineRefT}");

            if (f.Body is null)
            {
                EmitLine($"procedure [noinline] {f.Name}({string.Join(", ", ps)})");
            }
            else
            {
                EmitLine($"procedure [inline] {f.Name}({string.Join(", ", ps)})");
            }

            if (!f.Signature.ReturnType.IsSameTypeAs(PrimitiveType.Null))
            {
                var retName = f.ReturnVariable is null
                    ? $"{BuiltinPrefix}Return"
                    : $"{LocalPrefix}{f.ReturnVariable.Name}";
                EmitLine($"\treturns ({retName}: {TypeToString(f.Signature.ReturnType)})");
            }

            var i = 0;
            foreach (var req in f.Requires)
            {
                i += 1;
                EmitLine($"requires {ExprToString(req)}; // Violated precondition #{i} of {f.Name}");
            }

            foreach (var ensure in f.Ensures)
            {
                EmitLine($"ensures {ExprToString(ensure)};");
            }

            EmitLine("{");
            // declare local variables for the method
            foreach (var v in f.LocalVariables) EmitLine($"var {GetLocalName(v)}: {TypeToString(v.Type)};");
            GenerateStmt(f.Body, null, goals, generateSanityChecks);
            EmitLine("}\n");
        }
    }

    private void GenerateMachineProcedures(Machine m, List<Invariant> goals, bool generateSanityChecks = false)
    {
        foreach (var f in m.Methods)
        {
            var ps = f.Signature.Parameters.Select(p => $"{GetLocalName(p)}: {TypeToString(p.Type)}")
                .Prepend($"this: {MachineRefT}");
            var line = _ctx.LocationResolver.GetLocation(f.SourceLocation).Line;
            var name = f.Name == "" ? $"{BuiltinPrefix}{f.Owner.Name}_f{line}" : f.Name;
            EmitLine($"procedure [inline] {name}({string.Join(", ", ps)})");

            var currState = Deref("this");

            if (!f.Signature.ReturnType.IsSameTypeAs(PrimitiveType.Null))
            {
                EmitLine($"\treturns ({BuiltinPrefix}Return: {TypeToString(f.Signature.ReturnType)})");
            }

            EmitLine("{");

            // declare necessary local variables
            EmitLine($"var {LocalPrefix}state: {MachinePrefix}{m.Name}_StateAdt;");
            EmitLine($"var {LocalPrefix}stage: boolean;");
            EmitLine($"var {LocalPrefix}sent: [{LabelAdt}]boolean;");
            foreach (var v in m.Fields) EmitLine($"var {GetLocalName(v)}: {TypeToString(v.Type)};");
            foreach (var v in f.LocalVariables) EmitLine($"var {GetLocalName(v)}: {TypeToString(v.Type)};");

            // initialize all the local variables to the correct values
            EmitLine($"{LocalPrefix}state = {MachineStateAdtSelectState(currState, m)};");
            EmitLine($"{LocalPrefix}stage = false;"); // this can be set to true by a goto statement
            EmitLine($"{LocalPrefix}sent = {StateAdtSelectSent(StateVar)};");
            foreach (var v in m.Fields)
                EmitLine($"{GetLocalName(v)} = {MachineStateAdtSelectField(currState, m, v)};");
            // foreach (var v in f.LocalVariables) EmitLine($"{GetLocalName(v)} = {DefaultValue(v.Type)};");

            useLocalPrefix = true;
            GenerateStmt(f.Body, null, goals, generateSanityChecks);
            useLocalPrefix = false;

            var fields = m.Fields.Select(GetLocalName).Prepend($"{LocalPrefix}state").ToList();

            // make a new machine
            var newMachine = MachineAdtConstructM(m, fields);
            // make a new machine state
            var newMachineState = MachineStateAdtConstruct($"{LocalPrefix}stage", newMachine);
            // update the machine map
            EmitLine(
                $"{StateAdtSelectMachines(StateVar)} = {StateAdtSelectMachines(StateVar)}[this -> {newMachineState}];");
            // update the buffer
            EmitLine($"{StateAdtSelectSent(StateVar)} = {LocalPrefix}sent;");

            EmitLine("}\n");
        }
    }

    private void GenerateEntryHandler(State s, List<Invariant> goals, List<Invariant> requires,
        bool generateSanityChecks = false)
    {
        var label = $"{LocalPrefix}Label";
        var target = LabelAdtSelectTarget(label);
        var targetMachineState = Deref(target);
        var action = LabelAdtSelectAction(label);
        var g = EventOrGotoAdtSelectGoto(action);
        var received = StateAdtSelectReceived(StateVar);

        EmitLine($"procedure [noinline] {s.OwningMachine.Name}_{s.Name}({label}: {LabelAdt})");
        EmitLine($"\trequires {InFlight(StateVar, label)};");
        EmitLine($"\trequires {EventOrGotoAdtIsGoto(action)};");
        EmitLine($"\trequires {GotoAdtIsS(g, s)};");
        EmitLine($"\trequires {MachineStateAdtInS(targetMachineState, s.OwningMachine, s)};");

        EmitLine($"\trequires {InvariantPrefix}Unique_Actions();");
        EmitLine($"\trequires {InvariantPrefix}Increasing_Action_Count();");
        EmitLine($"\trequires {InvariantPrefix}Received_Subset_Sent();");

        if (generateSanityChecks)
        {
            EmitLine($"\tensures {InvariantPrefix}Unique_Actions(); // Failed to ensure unique action IDs at {GetLocation(s)}");
            EmitLine($"\tensures {InvariantPrefix}Increasing_Action_Count(); // Failed to ensure increasing action IDs at {GetLocation(s)}");
            EmitLine($"\tensures {InvariantPrefix}Received_Subset_Sent(); // Failed to ensure that received is a subset of sent at {GetLocation(s)}");
        }

        foreach (var reqs in requires)
        {
            EmitLine($"\trequires {InvariantPrefix}{reqs.Name}();");
        }

        foreach (var inv in goals)
        {
            if (!inv.IsDefault) {
                EmitLine($"\trequires {InvariantPrefix}{inv.Name}();");
                EmitLine(
                $"\tensures {InvariantPrefix}{inv.Name}(); // Failed to verify invariant {inv.Name.Replace("_PGROUP_", ": ")} at {GetLocation(s)}");
            }
        }

        EmitLine("{");

        if (s.Entry is null)
        {
            EmitLine($"var {LocalPrefix}stage: boolean;");
            EmitLine($"var {LocalPrefix}state: {MachinePrefix}{s.OwningMachine.Name}_StateAdt;");
            EmitLine($"var {LocalPrefix}received: [{LabelAdt}]boolean;");

            EmitLine($"{LocalPrefix}stage = false;");
            EmitLine($"{LocalPrefix}state = {MachineStateAdtSelectState(targetMachineState, s.OwningMachine)};");
            EmitLine($"{received} = {received}[{label} -> true];");
            EmitLine($"{LocalPrefix}received = {received};");

            var fields = s.OwningMachine.Fields
                .Select(v => MachineStateAdtSelectField(targetMachineState, s.OwningMachine, v))
                .Prepend($"{LocalPrefix}state").ToList();
            // make a new machine
            var newMachine = MachineAdtConstructM(s.OwningMachine, fields);
            // make a new machine state
            var newMachineState = MachineStateAdtConstruct($"{LocalPrefix}stage", newMachine);
            // update the machine map
            EmitLine(
                $"{StateAdtSelectMachines(StateVar)} = {StateAdtSelectMachines(StateVar)}[{target} -> {newMachineState}];");
            // update the buffer
            EmitLine($"{received} = {LocalPrefix}received;");
        }
        else
        {
            var f = s.Entry;
            var line = _ctx.LocationResolver.GetLocation(f.SourceLocation).Line;
            var name = f.Name == "" ? $"{BuiltinPrefix}{f.Owner.Name}_f{line}" : f.Name;
            var payload = f.Signature.Parameters.Count > 0
                ? $", {GotoAdtSelectParam(g, f.Signature.Parameters[0].Name, s)}"
                : "";
            EmitLine($"{received} = {received}[{label} -> true];");
            EmitLine($"call {name}({target}{payload});");
        }

        foreach (var reqs in requires)
        {
            EmitLine($"assume {InvariantPrefix}{reqs.Name}();");
        }
        
        EmitLine("}\n");
    }


    private void GenerateEventHandler(State s, Event ev, List<Invariant> goals, List<Invariant> requires,
        bool generateSanityChecks = false)
    {
        var label = $"{LocalPrefix}Label";
        EmitLine($"procedure [noinline] {s.OwningMachine.Name}_{s.Name}_{ev.Name}({label}: {LabelAdt})");

        var target = LabelAdtSelectTarget(label);
        var targetMachineState = Deref(target);
        var action = LabelAdtSelectAction(label);
        var e = EventOrGotoAdtSelectEvent(action);
        var received = StateAdtSelectReceived(StateVar);

        EmitLine($"\trequires {InFlight(StateVar, label)};");
        EmitLine($"\trequires {MachineStateAdtInS(targetMachineState, s.OwningMachine, s)};");
        EmitLine($"\trequires {EventOrGotoAdtIsEvent(action)};");
        EmitLine($"\trequires {EventAdtIsE(e, ev)};");

        EmitLine($"\trequires {InvariantPrefix}Unique_Actions();");
        EmitLine($"\trequires {InvariantPrefix}Increasing_Action_Count();");
        EmitLine($"\trequires {InvariantPrefix}Received_Subset_Sent();");

        var handler = s.AllEventHandlers.ToDictionary()[ev];

        if (generateSanityChecks)
        {
            EmitLine($"\tensures {InvariantPrefix}Unique_Actions(); // Failed to ensure unique action IDs at {GetLocation(handler)}");
            EmitLine($"\tensures {InvariantPrefix}Increasing_Action_Count(); // Failed to ensure increasing action IDs at {GetLocation(handler)}");
            EmitLine($"\tensures {InvariantPrefix}Received_Subset_Sent(); // Failed to ensure that received is a subset of sent at {GetLocation(handler)}");
        }        

        foreach (var reqs in requires)
        {
            EmitLine($"\trequires {InvariantPrefix}{reqs.Name}();");
        }

        foreach (var inv in goals)
        {
            if (!inv.IsDefault) {
                EmitLine($"\trequires {InvariantPrefix}{inv.Name}();");
                EmitLine(
                    $"\tensures {InvariantPrefix}{inv.Name}(); // Failed to verify invariant {inv.Name.Replace("_PGROUP_", ": ")} at {GetLocation(handler)}");
            }
        }

        switch (handler)
        {
            case EventDefer _:
                EmitLine("{");
                EmitLine("}\n");
                return;
            case EventDoAction eventDoAction:
                var f = eventDoAction.Target;
                var line = _ctx.LocationResolver.GetLocation(f.SourceLocation).Line;
                var name = f.Name == "" ? $"{BuiltinPrefix}{f.Owner.Name}_f{line}" : f.Name;
                var payload = f.Signature.Parameters.Count > 0 ? $", {EventAdtSelectPayload(e, ev)}" : "";
                EmitLine("{");
                EmitLine($"call {name}({target}{payload});");
                EmitLine($"{received} = {received}[{label} -> true];");
                foreach (var reqs in requires)
                {
                    EmitLine($"assume {InvariantPrefix}{reqs.Name}();");
                }
                EmitLine("}\n");
                return;
            case EventIgnore _:
                EmitLine("{");
                EmitLine("}\n");
                return;
            default:
                throw new NotSupportedException($"Not supported handler ({handler}) at {GetLocation(handler)}");
        }
    }


    private void GenerateControlBlock(Machine m, State s, Event e, Boolean handlerCheck)
    {
        // handlerCheck iff all the others are null
        Trace.Assert(handlerCheck == (m is null && s is null && e is null));

        EmitLine("control {");
        EmitLine($"set_solver_option(\":Timeout\", {_ctx.Job.Timeout});"); // timeout per query in seconds

        if (handlerCheck)
        {
            EmitLine("induction(1);");
        }
        else
        {
            if (e == null)
            {
                EmitLine($"verify({m.Name}_{s.Name});");
            }
            else
            {
                EmitLine($"verify({m.Name}_{s.Name}_{e.Name});");
            }
        }

        EmitLine("check;");
        EmitLine("print_results;");
        EmitLine("}");
    }

    private string DefaultValue(PLanguageType ty)
    {
        return ty switch
        {
            EnumType enumType => UserPrefix + enumType.EnumDecl.Values.First().Name,
            MapType mapType =>
                $"const({GetOptionName(mapType.ValueType)}_None(), {TypeToString(mapType)})",
            NamedTupleType ntt =>
                $"const_record({string.Join(", ", ntt.Fields.Select(f => $"{f.Name} := {DefaultValue(f.Type)}"))})",
            PermissionType _ => DefaultMachineRef,
            PrimitiveType pt when pt.Equals(PrimitiveType.Bool) => "false",
            PrimitiveType pt when pt.Equals(PrimitiveType.Int) => "0",
            PrimitiveType pt when pt.Equals(PrimitiveType.String) => DefaultString,
            PrimitiveType pt when pt.Equals(PrimitiveType.Machine) => DefaultMachineRef,
            SetType setType => $"const(false, {TypeToString(setType)})",
            TypeDefType tdType => DefaultValue(tdType.TypeDefDecl.Type),
            _ => throw new NotSupportedException($"Not supported default: {ty} ({ty.OriginalRepresentation})"),
        };
    }

    private void GenerateOptionTypes()
    {
        foreach (var ptype in _optionsToDeclare)
        {
            var opt = GetOptionName(ptype);
            EmitLine($"datatype {opt} = ");
            EmitLine($"\t| {opt}_Some ({opt}_Some_Value: {TypeToString(ptype)})");
            EmitLine($"\t| {opt}_None ();");
        }
    }

    private void GenerateChooseProcedures()
    {
        foreach (var choosePt in _chooseToDeclare)
        {
            var proc = GetChooseName(choosePt);
            EmitLine($"procedure [noinline] {proc}(inp: {TypeToString(choosePt)})");
            switch (choosePt)
            {
                case MapType mapType:
                    EmitLine($"\treturns (outp: {TypeToString(mapType.KeyType)})");
                    EmitLine($"\tensures {OptionIsSome(mapType.ValueType, "inp[outp]")};");
                    break;
                case SetType setType:
                    EmitLine($"\treturns (outp: {TypeToString(setType.ElementType)})");
                    EmitLine($"\tensures inp[outp];");
                    break;
                case PrimitiveType pt when pt.Equals(PrimitiveType.Int):
                    EmitLine($"\treturns (outp: {TypeToString(pt)})");
                    EmitLine($"\tensures 0 <= outp && outp <= inp;");
                    break;
                default:
                    throw new NotSupportedException(
                        $"Not supported choose type: {choosePt} ({choosePt.OriginalRepresentation})");
            }

            EmitLine("{");
            EmitLine("}\n");
        }
    }

    private string OptionConstructSome(PLanguageType t, string value)
    {
        return $"{GetOptionName(t)}_Some({value})";
    }

    private string OptionConstructNone(PLanguageType t)
    {
        return $"{GetOptionName(t)}_None()";
    }

    private string OptionIsSome(PLanguageType t, string instance)
    {
        return $"is_{GetOptionName(t)}_Some({instance})";
    }

    private string OptionIsNone(PLanguageType t, string instance)
    {
        return $"is_{GetOptionName(t)}_None({instance})";
    }

    private string OptionSelectValue(PLanguageType t, string instance)
    {
        return $"{instance}.{GetOptionName(t)}_Some_Value";
    }

    private void GenerateCheckerVars()
    {
        foreach (var ptype in _setCheckersToDeclare)
        {
            var name = GetCheckerName(ptype);
            EmitLine($"var {name}: {TypeToString(ptype)};");
        }
    }

    // specMachine is null iff we are not generating a statement for a spec machine
    private void GenerateStmt(IPStmt stmt, Machine specMachine, List<Invariant> goals,
        bool generateSanityChecks = false)
    {
        switch (stmt)
        {
            case CompoundStmt cstmt:
                foreach (var s in cstmt.Statements) GenerateStmt(s, specMachine, goals, generateSanityChecks);
                return;
            case AssignStmt { Value: FunCallExpr, Location: VariableAccessExpr } cstmt:
                var call = cstmt.Value as FunCallExpr;
                var avax = cstmt.Location as VariableAccessExpr;
                if (call == null) return;
                var v = ExprToString(avax);
                var f = call.Function.Name;
                var fargs = call.Arguments.Select(ExprToString);
                if (call.Function.Owner is not null)
                {
                    fargs = fargs.Prepend("this");
                }

                EmitLine($"call ({v}) = {f}({string.Join(", ", fargs.Prepend("this"))});");
                return;
            case AssignStmt { Value: ChooseExpr, Location: VariableAccessExpr } cstmt:
                var chooseExpr = (ChooseExpr)cstmt.Value;
                _chooseToDeclare.Add(chooseExpr.SubExpr.Type);
                var cvax = (VariableAccessExpr)cstmt.Location;
                var cv = ExprToString(cvax);
                var cf = GetChooseName(chooseExpr.SubExpr.Type);
                var arg = ExprToString(chooseExpr.SubExpr);
                EmitLine($"call ({cv}) = {cf}({arg});");
                return;
            case AssignStmt astmt:
                switch (astmt.Location)
                {
                    case VariableAccessExpr vax:
                        EmitLine($"{ExprToString(vax)} = {ExprToString(astmt.Value)};");
                        return;
                    case MapAccessExpr { MapExpr: VariableAccessExpr } max:
                        var map = ExprToString(max.MapExpr);
                        var index = ExprToString(max.IndexExpr);
                        var t = ((MapType)max.MapExpr.Type).ValueType;
                        EmitLine($"{map} = {map}[{index} -> {OptionConstructSome(t, ExprToString(astmt.Value))}];");
                        return;
                    case NamedTupleAccessExpr { SubExpr: VariableAccessExpr } tax:
                        var subExpr = ExprToString(tax.SubExpr);
                        var entry = tax.Entry.Name;
                        var field = tax.FieldName;
                        var fields = ((NamedTupleType)((TypeDefType)tax.SubExpr.Type).TypeDefDecl.Type).Fields;
                        var rhs = ExprToString(astmt.Value);
                        var build = string.Join(", ",
                            fields.Select(
                                f => f.Name == entry ? $"{entry} := {rhs}" : $"{f.Name} := {subExpr}.{f.Name}"));
                        EmitLine($"{subExpr} = const_record({build});");
                        return;
                }

                throw new NotSupportedException(
                    $"Not supported assignment expression: {astmt.Location} = {astmt.Value} ({GetLocation(astmt)})");
            case IfStmt ifstmt:
                var cond = (ifstmt.Condition) switch
                {
                    NondetExpr => "*",
                    _ => ExprToString(ifstmt.Condition),
                };
                EmitLine($"if ({cond}) {{");
                GenerateStmt(ifstmt.ThenBranch, specMachine, goals, generateSanityChecks);
                EmitLine("} else {");
                GenerateStmt(ifstmt.ElseBranch, specMachine, goals, generateSanityChecks);
                EmitLine("}");
                return;
            case AssertStmt astmt:
                EmitLine($"// {((StringExpr)astmt.Message).BaseString}");
                EmitLine(
                    $"assert({ExprToString(astmt.Assertion)}); // Failed to verify assertion at {GetLocation(astmt)}");
                return;
            case AssumeStmt astmt:
                EmitLine($"// {((StringExpr)astmt.Message).BaseString}");
                EmitLine($"assume({ExprToString(astmt.Assumption)});");
                return;
            case PrintStmt { Message: StringExpr } pstmt:
                EmitLine($"// {((StringExpr)pstmt.Message).BaseString}");
                return;
            case FunCallStmt fapp:
                EmitLine(
                    $"call {fapp.Function.Name}({string.Join(", ", fapp.ArgsList.Select(ExprToString).Prepend("this"))});");
                return;
            case AddStmt { Variable: VariableAccessExpr } astmt:
                // v += (y)
                var aset = ExprToString(astmt.Variable);
                var akey = ExprToString(astmt.Value);
                EmitLine($"{aset} = {aset}[{akey} -> true];");
                return;
            case AddStmt { Variable: MapAccessExpr } astmt
                when ((MapAccessExpr)astmt.Variable).MapExpr is VariableAccessExpr:
                // m[x] += (y)
                // m = m[x -> some((m[x].some)[y -> true])]
                var mapInP = ((MapAccessExpr)astmt.Variable);
                var mapTypeInP = (MapType)mapInP.MapExpr.Type;
                var mapM = ExprToString(mapInP.MapExpr);
                var locX = ExprToString(mapInP.IndexExpr);
                var valY = ExprToString(astmt.Value);
                var someMx = OptionSelectValue(mapTypeInP.ValueType, $"{mapM}[{locX}]");
                var someUpdated = OptionConstructSome(mapTypeInP.ValueType, $"{someMx}[{valY} -> true]");
                EmitLine($"{mapM} = {mapM}[{locX} -> {someUpdated}];");
                return;
            case RemoveStmt { Variable: VariableAccessExpr } rstmt:
                var rset = ExprToString(rstmt.Variable);
                var rkey = ExprToString(rstmt.Value);

                switch (rstmt.Variable.Type)
                {
                    case MapType mapType:
                        EmitLine($"{rset} = {rset}[{rkey} -> {OptionConstructNone(mapType.ValueType)}];");
                        return;
                    case SetType _:
                        EmitLine($"{rset} = {rset}[{rkey} -> false];");
                        return;
                    default:
                        throw new NotSupportedException(
                            $"Only support remove statements for sets and maps, got {rstmt.Variable.Type}");
                }
            case InsertStmt { Variable: VariableAccessExpr } istmt:
                var imap = ExprToString(istmt.Variable);
                var idx = ExprToString(istmt.Index);
                var value = OptionConstructSome(istmt.Value.Type, ExprToString(istmt.Value));
                EmitLine($"{imap} = {imap}[{idx} -> {value}];");
                return;
            case ForeachStmt fstmt:

                switch (fstmt.IterCollection)
                {
                    case KeysExpr keysExpr:
                        fstmt = new ForeachStmt(fstmt.SourceLocation, fstmt.Item, keysExpr.Expr, fstmt.Body,
                            fstmt.Invariants);
                        break;
                }

                var item = GetLocalName(fstmt.Item);
                var checker = GetCheckerName(fstmt.IterCollection.Type);
                var collection = ExprToString(fstmt.IterCollection);

                switch (fstmt.IterCollection.Type)
                {
                    case SetType setType:
                        // set the checker to default
                        EmitLine($"{checker} = {DefaultValue(setType)};");
                        // remember to declare it later
                        _setCheckersToDeclare.Add(setType);
                        // havoc the item
                        EmitLine($"havoc {item};");
                        EmitLine($"while ({checker} != {collection})");
                        foreach (var inv in goals)
                        {
                            if (!inv.IsDefault) {
                                EmitLine($"\tinvariant {InvariantPrefix}{inv.Name}(); // Failed to verify invariant {inv.Name.Replace("_PGROUP_", ": ")} at {GetLocation(fstmt)}");
                            }
                            
                        }

                        if (generateSanityChecks)
                        {
                            EmitLine($"\tinvariant {InvariantPrefix}Unique_Actions(); // Failed to ensure unique action IDs at {GetLocation(fstmt)}");
                            EmitLine($"\tinvariant {InvariantPrefix}Increasing_Action_Count(); // Failed to ensure increasing action IDs at {GetLocation(fstmt)}");
                            EmitLine($"\tinvariant {InvariantPrefix}Received_Subset_Sent(); // Failed to ensure that received is a subset of sent at {GetLocation(fstmt)}");
                        }
                        
                        // ensure uniqueness for the new ones too
                        EmitLine(
                                $"\tinvariant forall (a1: {LabelAdt}, a2: {LabelAdt}) :: (a1 != a2 && {LocalPrefix}sent[a1] && {LocalPrefix}sent[a2]) ==> {LabelAdtSelectActionCount("a1")} != {LabelAdtSelectActionCount("a2")};");
                        EmitLine(
                            $"\tinvariant forall (a: {LabelAdt}) :: {LocalPrefix}sent[a] ==> {LabelAdtSelectActionCount("a")} < {BuiltinPrefix}ActionCount;");
                        // ensure we only ever add sends
                        EmitLine(
                            $"\tinvariant forall (e: {LabelAdt}) :: {StateAdtSelectSent(StateVar)}[e] ==> {LocalPrefix}sent[e];");

                        // user given invariants
                        foreach (var inv in fstmt.Invariants)
                        {
                            EmitLine(
                                $"\tinvariant {ExprToString(inv)}; // Failed to verify loop invariant at {GetLocation(inv)}");
                        }

                        EmitLine("{");
                        // assume that the item is in the set but hasn't been visited
                        EmitLine($"if ({collection}[{item}] && !{checker}[{item}]) {{");
                        // the body of the loop
                        GenerateStmt(fstmt.Body, specMachine, goals, generateSanityChecks);
                        // update the checker
                        EmitLine($"{checker} = {checker}[{item} -> true];");
                        EmitLine("}");
                        // havoc the item
                        EmitLine($"havoc {item};");
                        EmitLine("}");
                        return;
                    case MapType mapType:
                        // set the checker to default
                        EmitLine($"{checker} = {DefaultValue(mapType)};");
                        // remember to declare it later
                        _setCheckersToDeclare.Add(mapType);
                        // havoc the item, in this case it is a key
                        EmitLine($"havoc {item};");
                        EmitLine($"while ({checker} != {collection})");
                        foreach (var inv in goals)
                        {
                            if (!inv.IsDefault) {
                                EmitLine($"\tinvariant {InvariantPrefix}{inv.Name}(); // Failed to verify invariant {inv.Name.Replace("_PGROUP_", ": ")} at {GetLocation(fstmt)}");
                            }
                        }

                        if (generateSanityChecks) {
                            EmitLine($"\tinvariant {InvariantPrefix}Unique_Actions(); // Failed to ensure unique action IDs at {GetLocation(fstmt)}");
                            EmitLine($"\tinvariant {InvariantPrefix}Increasing_Action_Count(); // Failed to ensure increasing action IDs at {GetLocation(fstmt)}");
                            EmitLine($"\tinvariant {InvariantPrefix}Received_Subset_Sent(); // Failed to ensure that received is a subset of sent at {GetLocation(fstmt)}");
                        }
                        // ensure uniqueness for the new ones too
                        EmitLine(
                            $"\tinvariant forall (a1: {LabelAdt}, a2: {LabelAdt}) :: (a1 != a2 && {LocalPrefix}sent[a1] && {LocalPrefix}sent[a2]) ==> {LabelAdtSelectActionCount("a1")} != {LabelAdtSelectActionCount("a2")};");
                        EmitLine(
                            $"\tinvariant forall (a: {LabelAdt}) :: {LocalPrefix}sent[a] ==> {LabelAdtSelectActionCount("a")} < {BuiltinPrefix}ActionCount;");

                        // ensure we only ever add sends
                        EmitLine(
                            $"\tinvariant forall (e: {LabelAdt}) :: {StateAdtSelectSent(StateVar)}[e] ==> {LocalPrefix}sent[e];");

                        // user given invariants
                        foreach (var inv in fstmt.Invariants)
                        {
                            EmitLine(
                                $"\tinvariant {ExprToString(inv)}; // Failed to verify loop invariant at {GetLocation(inv)}");
                        }

                        EmitLine("{");
                        // assume that the item is in the set but hasn't been visited
                        EmitLine(
                            $"if ({OptionIsSome(mapType.ValueType, $"{collection}[{item}]")} && {OptionIsNone(mapType.ValueType, $"{checker}[{item}]")}) {{");
                        // the body of the loop
                        GenerateStmt(fstmt.Body, specMachine, goals, generateSanityChecks);
                        // update the checker
                        EmitLine($"{checker} = {checker}[{item} -> {collection}[{item}]];");
                        EmitLine("}");
                        // havoc the item
                        EmitLine($"havoc {item};");
                        EmitLine("}");
                        return;
                    default:
                        throw new NotSupportedException(
                            $"Foreach over non-sets is not supported yet: {fstmt} ({GetLocation(fstmt)}");
                }
            case GotoStmt gstmt when specMachine is null:
                var gaction = EventOrGotoAdtConstructGoto(gstmt.State, gstmt.Payload);
                var glabel = LabelAdtConstruct("this", gaction);
                var glabels = StateAdtSelectSent(StateVar);
                var newState = $"{MachinePrefix}{gstmt.State.OwningMachine.Name}_{gstmt.State.Name}";
                EmitLine($"{glabels} = {glabels}[{glabel} -> true];");
                IncrementActionCount();
                EmitLine($"{LocalPrefix}state = {newState};");
                EmitLine($"{LocalPrefix}stage = true;");
                return;
            case GotoStmt gstmts: // when specMachine is not null
                EmitLine($"{LocalPrefix}state = {SpecPrefix}{specMachine.Name}_{gstmts.State.Name};");
                EmitLine($"{LocalPrefix}stage = true;");
                return;
            case SendStmt sstmt when specMachine is null:
                if (sstmt.Arguments.Count > 1)
                {
                    throw new NotSupportedException("We only support at most one argument to a send");
                }

                var ev = ((EventRefExpr)sstmt.Evt).Value;
                var saction = EventOrGotoAdtConstructEvent(ev, sstmt.Arguments.Any() ? sstmt.Arguments.First() : null);
                var slabel = LabelAdtConstruct(ExprToString(sstmt.MachineExpr), saction);
                var slabels = $"{LocalPrefix}sent";
                EmitLine($"{slabels} = {slabels}[{slabel} -> true];");
                IncrementActionCount();
                foreach (var procedureName in _specListenMap.GetValueOrDefault(ev, []))
                {
                    var argument = sstmt.Arguments.Count > 0 ? $"{ExprToString(sstmt.Arguments[0])}" : PNull;
                    EmitLine($"call {procedureName}({argument});");
                }

                return;
            case ReturnStmt rstmt:
                EmitLine($"{BuiltinPrefix}Return = {ExprToString(rstmt.ReturnValue)};");
                return;
            case null:
                return;
        }

        throw new NotSupportedException($"Not supported statement: {stmt} ({GetLocation(stmt)})");
    }

    private string ExprToString(IPExpr expr)
    {
        return expr switch
        {
            NamedTupleAccessExpr ntax => $"{ExprToString(ntax.SubExpr)}.{ntax.FieldName}",
            VariableAccessExpr vax => GetLocalName(vax.Variable),
            IntLiteralExpr i => i.Value.ToString(),
            BoolLiteralExpr b => b.Value.ToString().ToLower(),
            BinOpExpr bexpr =>
                $"({ExprToString(bexpr.Lhs)} {BinOpToString(bexpr.Operation)} {ExprToString(bexpr.Rhs)})",
            UnaryOpExpr uexpr => $"({UnaryOpToString(uexpr.Operation)} {ExprToString(uexpr.SubExpr)})",
            ThisRefExpr => "this",
            EnumElemRefExpr e => $"{UserPrefix}{e.Value.Name}",
            NamedTupleExpr t => NamedTupleExprHelper(t),
            StringExpr s =>
                $"\"{s.BaseString}\" {(s.Args.Count != 0 ? "%" : "")} {string.Join(", ", s.Args.Select(ExprToString))}",
            MapAccessExpr maex =>
                OptionSelectValue(((MapType)maex.MapExpr.Type).ValueType,
                    $"{ExprToString(maex.MapExpr)}[{ExprToString(maex.IndexExpr)}]"),
            ContainsExpr cexp when cexp.Collection.Type.Canonicalize() is MapType => OptionIsSome(
                ((MapType)cexp.Collection.Type).ValueType,
                $"{ExprToString(cexp.Collection)}[{ExprToString(cexp.Item)}]"),
            ContainsExpr cexp when cexp.Collection.Type.Canonicalize() is SetType =>
                $"{ExprToString(cexp.Collection)}[{ExprToString(cexp.Item)}]",
            DefaultExpr dexp => DefaultValue(dexp.Type),
            QuantExpr { Quant: QuantType.Forall } qexpr =>
                $"(forall ({BoundVars(qexpr.Bound)}) :: {Guard(qexpr.Bound, qexpr.Difference, true)}({ExprToString(qexpr.Body)}))",
            QuantExpr { Quant: QuantType.Exists } qexpr =>
                $"(exists ({BoundVars(qexpr.Bound)}) :: {Guard(qexpr.Bound, qexpr.Difference, false)}({ExprToString(qexpr.Body)}))",
            MachineAccessExpr max => MachineStateAdtSelectField(Deref(ExprToString(max.SubExpr)), max.Machine,
                max.Entry),
            SpecAccessExpr sax => $"{SpecPrefix}{sax.Spec.Name}_{sax.FieldName}",
            EventAccessExpr eax => LabelAdtSelectPayloadField(ExprToString(eax.SubExpr), eax.PEvent, eax.Entry),
            TestExpr { Kind: Machine m } texpr => MachineStateAdtIsM(Deref(ExprToString(texpr.Instance)),
                m), // must deref because or else we don't have an ADT!
            TestExpr { Kind: Event e } texpr => LabelAdtIsE(ExprToString(texpr.Instance), e),
            TestExpr { Kind: State s } texpr => MachineStateAdtInS(Deref(ExprToString(texpr.Instance)), s.OwningMachine,
                s), // must deref for ADT!
            PureCallExpr pexpr => $"{pexpr.Pure.Name}({string.Join(", ", pexpr.Arguments.Select(ExprToString))})",
            FlyingExpr fexpr => $"{InFlight(StateVar, ExprToString(fexpr.Instance))}",
            SentExpr sexpr => useLocalPrefix ? $"{LocalPrefix}sent[{ExprToString(sexpr.Instance)}]" : $"{StateAdtSelectSent(StateVar)}[{ExprToString(sexpr.Instance)}]",
            TargetsExpr texpr =>
                $"({LabelAdtSelectTarget(ExprToString(texpr.Instance))} == {ExprToString(texpr.Target)})",
            _ => throw new NotSupportedException($"Not supported expr ({expr}) at {GetLocation(expr)}")
            // _ => $"NotHandledExpr({expr})"
        };

        string BoundVars(List<Variable> bound)
        {
            return $"{string.Join(", ", bound.Select(b => $"{LocalPrefix}{b.Name}: {TypeToString(b.Type)}"))}";
        }

        string Guard(List<Variable> bound, bool difference, bool universal)
        {
            var impliesOrAnd = universal ? "==>" : "&&";

            List<string> bounds = [];
            foreach (var b in bound)
            {
                switch (b.Type)
                {
                    case PermissionType { Origin: Machine } pt:
                        bounds.Add(ExprToString(new TestExpr(b.SourceLocation,
                            new VariableAccessExpr(b.SourceLocation, b), pt.Origin)));
                        break;
                    case PermissionType { Origin: Interface } pt:
                        var name = pt.OriginalRepresentation;

                        if (_globalScope.Lookup(name, out Machine m))
                        {
                            bounds.Add(ExprToString(new TestExpr(b.SourceLocation,
                                new VariableAccessExpr(b.SourceLocation, b), m)));
                        }

                        break;
                    case PermissionType { Origin: NamedEventSet } pt:
                        var e = ((NamedEventSet)pt.Origin).Events.First();
                        bounds.Add(ExprToString(new TestExpr(b.SourceLocation,
                            new VariableAccessExpr(b.SourceLocation, b), e)));
                        if (difference)
                        {
                            bounds.Add($"{LocalPrefix}sent[{LocalPrefix}{b.Name}]");
                            bounds.Add($"!{StateAdtSelectSent(StateVar)}[{LocalPrefix}{b.Name}]");
                        }

                        break;
                    case PrimitiveType pt when pt.IsSameTypeAs(PrimitiveType.Event):
                        if (difference)
                        {
                            bounds.Add($"{LocalPrefix}sent[{LocalPrefix}{b.Name}]");
                            bounds.Add($"!{StateAdtSelectSent(StateVar)}[{LocalPrefix}{b.Name}]");
                        }

                        break;
                }
            }

            if (bounds.Count != 0)
            {
                return "(" + string.Join(" && ", bounds) + $") {impliesOrAnd} ";
            }

            return "";
        }
    }

    private string NamedTupleExprHelper(NamedTupleExpr t)
    {
        var ty = (NamedTupleType)t.Type;
        var names = ty.Fields.Select(f => f.Name);
        var values = t.TupleFields.Select(ExprToString);
        var args = string.Join(", ", names.Zip(values).Select(p => $"{p.First} := {p.Second}"));
        return $"const_record({args})";
    }

    private static string BinOpToString(BinOpType op)
    {
        return op switch
        {
            BinOpType.Add => "+",
            BinOpType.Sub => "-",
            BinOpType.Mul => "*",
            BinOpType.Div => "/",
            BinOpType.Mod => "%",
            BinOpType.Lt => "<",
            BinOpType.Le => "<=",
            BinOpType.Gt => ">",
            BinOpType.Ge => ">=",
            BinOpType.And => "&&",
            BinOpType.Or => "||",
            BinOpType.Eq => "==",
            BinOpType.Neq => "!=",
            BinOpType.Then => "==>",
            BinOpType.Iff => "==",
            _ => throw new NotImplementedException($"{op} is not implemented yet!")
        };
    }

    private static string UnaryOpToString(UnaryOpType op)
    {
        return op switch
        {
            UnaryOpType.Negate => "-",
            UnaryOpType.Not => "!",
            _ => throw new NotImplementedException($"{op} is not implemented yet!")
        };
    }

    private string TypeToString(PLanguageType t)
    {
        switch (t.Canonicalize())
        {
            case NamedTupleType ntt:
                var fields = string.Join(", ",
                    ntt.Fields.Select(nte => $"{nte.Name}: {TypeToString(nte.Type)}"));
                return $"record {{{fields}}}";
            case PrimitiveType pt when pt.Equals(PrimitiveType.Bool):
                return "boolean";
            case PrimitiveType pt when pt.Equals(PrimitiveType.Int):
                return "integer";
            case PrimitiveType pt when pt.Equals(PrimitiveType.String):
                return StringT;
            case PrimitiveType pt when pt.Equals(PrimitiveType.Null):
                return PNull;
            case PrimitiveType pt when pt.Equals(PrimitiveType.Machine):
                return MachineRefT;
            case PrimitiveType pt when pt.Equals(PrimitiveType.Event):
                return LabelAdt;
            case TypeDefType tdt:
                return $"{UserPrefix}{tdt.TypeDefDecl.Name}";
            case PermissionType { Origin: Machine } _:
                return MachineRefT;
            case PermissionType { Origin: Interface } _:
                return MachineRefT;
            case PermissionType { Origin: NamedEventSet } _:
                return LabelAdt;
            case EnumType et:
                return $"{UserPrefix}{et.EnumDecl.Name}";
            case SetType st:
                return $"[{TypeToString(st.ElementType)}]boolean";
            case MapType mt:
                _optionsToDeclare.Add(mt.ValueType.Canonicalize());
                return $"[{TypeToString(mt.KeyType)}]{GetOptionName(mt.ValueType)}";
        }

        throw new NotSupportedException($"Not supported type expression {t} ({t.OriginalRepresentation})");
    }

    private string GetLocalName(Variable v)
    {
        return $"{LocalPrefix}{v.Name}";
    }


    private string GetCheckerName(PLanguageType t)
    {
        var output = $"{CheckerPrefix}{TypeToString(t)}";
        return Regex.Replace(output, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
    }

    private string GetOptionName(PLanguageType t)
    {
        var output = $"{OptionPrefix}{TypeToString(t)}";
        return Regex.Replace(output, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
    }


    private string GetChooseName(PLanguageType t)
    {
        var output = $"{ChoosePrefix}{TypeToString(t)}";
        return Regex.Replace(output, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
    }

    private string GetLocation(IPAST node)
    {
        return _ctx.LocationResolver.GetLocation(node.SourceLocation).ToString();
    }
}