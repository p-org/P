using System;
using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.AST.Statements;
using Plang.Compiler.TypeChecker.AST.States;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.Uclid5
{
    public class Uclid5CodeGenerator : ICodeGenerator
    {
        public bool HasCompilationStage => false;

        private CompiledFile _src;
        private CompilationContext _ctx;

        public IEnumerable<CompiledFile> GenerateCode(ICompilerConfiguration job, Scope globalScope)
        {
            this._ctx = new CompilationContext(job);
            this._src = new CompiledFile(this._ctx.FileName);
            GenerateMain(globalScope);
            return new List<CompiledFile> { this._src };
        }

        private void GenerateMain(Scope globalScope)
        {
            this.EmitLine("// The main module contains the entire P program");
            this.EmitLine($"module main {{");

            GenerateBuiltInTypeDefs();
            GenerateUserEnumDefs(globalScope.AllDecls.OfType<PEnum>());
            GenerateUserTypeDefs(globalScope.AllDecls.OfType<TypeDef>());
            GenerateEventDefs(globalScope.AllDecls.OfType<PEvent>());
            GenerateMachineDefs(globalScope.AllDecls.OfType<Machine>());
            GenerateBuiltInVarDecls();
            GenerateInitBlock();
            GenerateEntryProcedures(globalScope.Machines);
            GenerateHandlerProcedures(globalScope.Machines);
            GenerateNextBlock(globalScope.Machines);

            // close the main module
            this.EmitLine("}");
        }

        private void GenerateBuiltInTypeDefs()
        {
            this.EmitLine("// Built-in types");
            this.EmitLine($"type {RefT()};");
            this.EmitLine($"type {Null()} = enum {{ {Null()} }};");
            this.EmitLine($"type {StringT()};");
            this.EmitLine("\n");
        }

        private static string Prefix()
        {
            return "UPVerifier_";
        }
        private static string RefT()
        {
            return $"{Prefix()}MachineRef";
        }
        private static string MachineT()
        {
            return $"{Prefix()}Machine";
        }
        private static string Null()
        {
            return $"{Prefix()}Null";
        }
        private static string StringT()
        {
            return $"{Prefix()}String";
        }

        private static string EventT()
        {
            return $"{Prefix()}Event";
        }
        private static string Source()
        {
            return "Source";
        }
        private static string Target()
        {
            return "Target";
        }
        private static string Payload()
        {
            return "Payload";
        }
        private static string Entry()
        {
            return "Entry";
        }
        private static string Start()
        {
            return "Start";
        }
        private static string State()
        {
            return "State";
        }
        private static string CurrentMRef()
        {
            return $"{Prefix()}MTurn";
        }
        private static string CurrentEvent()
        {
            return $"{Prefix()}ETurn";
        }
        private static string Machines()
        {
            return $"{Prefix()}Machines";
        }
        private static string GetMachine(string r)
        {
            return $"{Machines()}[{r}]";
        }
        private static string UpdateMachine(string r, string m)
        {
            return $"{Machines()}[{r} -> {m}]";
        }
        private static string Buffer()
        {
            return $"{Prefix()}Buffer";
        }
        private static string LiveEvent(string e)
        {
            return $"{Buffer()}[{e}]";
        }
        private static string GetPayload(string arg)
        {
            return $"{Prefix()}{Payload()}({arg})";
        }
        private static string GetSource(string arg)
        {
            return $"{Prefix()}{Source()}({arg})";
        }
        private static string GetTarget(string arg)
        {
            return $"{Prefix()}{Target()}({arg})";
        }
        private static string InEntry(string arg)
        {
            return $"{Prefix()}{Entry()}({arg})";
        }
        private static string InStart(string arg)
        {
            return $"{Prefix()}{Start()}({arg})";
        }
        private static string IsInstance(string x, string constructor)
        {
            return $"is_{constructor}({x})";
        }
        private static string GetState(string m, string kind)
        {
            return $"{m}.{kind}_State";
        }

        private void GenerateUserEnumDefs(IEnumerable<PEnum> enums)
        {
            this.EmitLine("// User's enumerated types");
            foreach (var e in enums)
            {
                var variants = string.Join(", ", e.Values.Select(v => v.Name));
                this.EmitLine($"type {e.Name} = enum {{{variants}}};");
            }

            this.EmitLine("\n");
        }

        private void GenerateUserTypeDefs(IEnumerable<TypeDef> types)
        {
            this.EmitLine("// User's non-enumerated type types");
            foreach (var t in types)
            {
                this.EmitLine($"type {t.Name} = {TypeToString(t.Type)};");
            }

            this.EmitLine("\n");
        }

        private void GenerateEventDefs(IEnumerable<PEvent> events)
        {
            this.EmitLine("// Events, their types, and helper functions");
            var es = events.ToList();
            var sum = string.Join("\n\t\t| ", es.Select(ProcessEvent));
            this.EmitLine($"datatype {EventT()} = \n\t\t| {sum}\n");

            string[] attributes = [Source(), Target(), Payload()];
            foreach (var attribute in attributes)
            {
                var cases = $"if ({IsInstance("e", es.First().Name)}) then e.{es.First().Name}_{attribute}";
                cases = es.Skip(1).SkipLast(1).Aggregate(cases,
                    (current, e) => current + $"\n\t\telse if ({IsInstance("e", e.Name)}) then e.{e.Name}_{attribute}");
                cases += $"\n\t\telse e.{es.Last().Name}_{attribute}";
                var outType = attribute == Payload() ? EventT() : RefT();
                this.EmitLine($"define {Prefix()}{attribute} (e:$ {EventT()}) : {outType} = \n\t\t{cases}");
                if (attribute != Payload())
                {
                    this.EmitLine("");
                }
            }

            this.EmitLine("\n");
            return;

            string ProcessEvent(PEvent e)
            {
                var payload = TypeToString(e.PayloadType);
                return
                    $"{e.Name} ({e.Name}_{Source()}: {RefT()}, {e.Name}_{Target()}: {RefT()}, {e.Name}_{Payload()}: {payload})";
            }
        }

        private void GenerateMachineDefs(IEnumerable<Machine> machines)
        {
            this.EmitLine("// Machines, their types, and helper functions");
            var ms = machines.ToList();
            var sum = string.Join("\n\t\t| ", ms.Select(ProcessMachine));
            this.EmitLine($"datatype {MachineT()} = \n\t\t| {sum}\n");

            var entryCases = $"if ({IsInstance("m", ms.First().Name)}) then m.{ms.First().Name}_{Entry()}";
            entryCases = ms.Skip(1).SkipLast(1).Aggregate(entryCases,
                (current, m) => current + $"\n\t\telse if ({IsInstance("m", m.Name)}) then m.{m.Name}_{Entry()}");
            entryCases += $"\n\t\telse m.{ms.Last().Name}_{Entry()}";
            this.EmitLine($"define {Prefix()}{Entry()} (m: {MachineT()}) : boolean = \n\t\t{entryCases}\n");

            var startCases =
                $"if ({IsInstance("m", ms.First().Name)}) then m.{ms.First().Name}_{State()} == {ms.First().Name}_{GetStartState(ms.First())}";
            foreach (var m in ms.Skip(1).SkipLast(1))
            {
                startCases += $"\n\t\telse if ({IsInstance("m", m.Name)}) then m.{m.Name}_{State()} == {m.Name}_{GetStartState(m)}";
            }

            startCases += $"\n\t\telse m.{ms.Last().Name}_{State()} == {ms.Last().Name}_{GetStartState(ms.Last())}";
            this.EmitLine($"define {Prefix()}{Start()} (m: {MachineT()}) : boolean = \n\t\t{startCases}");
            this.EmitLine("\n");
            return;

            string ProcessMachine(Machine m)
            {
                var states = "enum {" + string.Join(", ", m.States.Select(s => $"{m.Name}_{s.Name}")) + "}";
                var fields = string.Join(", ", m.Fields.Select(f => $"{m.Name}_{f.Name}: {TypeToString(f.Type)}"));
                return $"{m.Name} ({m.Name}_{Entry()}: boolean, {m.Name}_{State()}: {states}, {fields})";
            }
        }

        private void GenerateBuiltInVarDecls()
        {
            // Declare state space
            this.EmitLine("// State space: machines, buffer, and next machine to step");
            this.EmitLine($"var {Machines()}: [{RefT()}]{MachineT()};");
            this.EmitLine($"var {CurrentMRef()}: {RefT()};");
            this.EmitLine($"var {Buffer()}: [{EventT()}]boolean;");
            this.EmitLine($"var {CurrentEvent()}: {EventT()};");
            this.EmitLine("\n");
        }

        private void GenerateInitBlock()
        {
            // Init captures the state of affairs before anything executes
            // Every machine is in their start state, every machine is in Entry, and the buffer is empty
            this.EmitLine("init {");
            this.EmitLine("// Every machine begins in their start state");
            this.EmitLine($"assume(forall (r: {RefT()}) :: {InStart(GetMachine("r"))});");
            this.EmitLine("// Every machine begins with their entry flag set");
            this.EmitLine($"assume(forall (r: {RefT()}) :: {InEntry(GetMachine("r"))});");
            this.EmitLine("// The buffer starts completely empty");
            this.EmitLine($"{Buffer()} = const(false, [{EventT()}]boolean);");
            // close the init block
            this.EmitLine("}");
            this.EmitLine("\n");
        }

        private void GenerateHandlerProcedures(IEnumerable<Machine> machines)
        {
            // create all the event handler procedures
            foreach (var m in machines)
            {
                foreach (var s in m.States)
                {
                    foreach (var eh in s.AllEventHandlers)
                    {
                        switch (eh.Value)
                        {
                            case EventDoAction action:
                                var e = action.Trigger;
                                var f = action.Target;
                                this.EmitLine($"// Handler for event {e.Name} in machine {m.Name}");
                                this.EmitLine(
                                    $"procedure {m.Name}_{s.Name}_handle_{e.Name} (r: {RefT()}, e: {EventT()})");
                                this.EmitLine($"\tmodifies {Machines()};");
                                this.EmitLine($"\tmodifies {Buffer()};");
                                this.EmitLine($"\trequires {LiveEvent("e")};");
                                this.EmitLine($"\trequires {GetSource("e")} == r;");
                                this.EmitLine($"\trequires {IsInstance("e", e.Name)};");
                                this.EmitLine($"\trequires {IsInstance(GetMachine("r"), m.Name)};");
                                this.EmitLine($"\trequires {IsInstance(GetState(GetMachine("r"), m.Name), s.Name)};");
                                this.EmitLine($"\trequires !{InEntry(GetMachine("r"))};");
                                this.EmitLine(
                                    $"\tensures (forall (r1: {RefT()}) r != r1 ==> old({Machines()})[r1] == {GetMachine("r1")});");
                                this.EmitLine(
                                    $"\tensures (forall (e1: {EventT()}) e != e1 ==> (old({Buffer()})[e1] == {LiveEvent("e1")} || ({GetSource("e1")} == r && !old({Buffer()})[e1])));");
                                // open procedure
                                this.EmitLine("{");
                                foreach (var v in f.LocalVariables)
                                {
                                    this.EmitLine($"var {v.Name}: {TypeToString(v.Type)};");
                                }
                                
                                foreach (var v in m.Fields)
                                {
                                    this.EmitLine($"var {v.Name}: {TypeToString(v.Type)};");
                                }
                                
                                this.EmitLine("var entry: boolean;");
                                this.EmitLine("var state: TODO;");

                                switch (f.Signature.Parameters.Count)
                                {
                                    case > 1:
                                        throw new NotSupportedException(
                                            $"Only one event handler argument supported: {action}");
                                    case 1:
                                    {
                                        var arg = f.Signature.Parameters.First();
                                        this.EmitLine($"var {arg.Name}: {TypeToString(arg.Type)};");
                                        this.EmitLine($"{arg.Name} = {GetPayload("e")};");
                                        break;
                                    }
                                }

                                this.EmitLine("UPVerifier_Buffer[e -> false];");

                                GenerateStmt(f.Body);

                                var newMachine = $"{m.Name}(entry, state, {string.Join(", ", m.Fields.Select(f => f.Name))})";
                                this.EmitLine($"{Machines()} = {UpdateMachine("r", newMachine)};");
                                // close procedure
                                this.EmitLine("}\n");
                                return;
                        }
                    }
                }
            }
        }

        private void GenerateEntryProcedures(IEnumerable<Machine> machines)
        {
            foreach (var m in machines)
            {
                foreach (var s in m.States)
                {
                    var f = s.Entry;
                    if (f is null) continue;

                    this.EmitLine($"// Handler for entry in machine {m.Name} at state {s.Name}");
                    this.EmitLine($"procedure {m.Name}_{s.Name}_handle_entry (r: {RefT()})");
                    this.EmitLine($"\tmodifies {Machines()};");
                    this.EmitLine($"\tmodifies {Buffer()};");
                    this.EmitLine($"\trequires {IsInstance(GetMachine("r"), m.Name)};");
                    this.EmitLine($"\trequires {IsInstance(GetState(GetMachine("r"),m.Name), s.Name)};");
                    this.EmitLine($"\trequires {InEntry(GetMachine("r"))};");
                    this.EmitLine($"\tensures !{InEntry(GetMachine("r"))};");
                    // open procedure
                    this.EmitLine("{");
                    foreach (var v in f.LocalVariables)
                    {
                        this.EmitLine($"var {v.Name}: {TypeToString(v.Type)};");
                    }

                    foreach (var v in m.Fields)
                    {
                        this.EmitLine($"var {v.Name}: {TypeToString(v.Type)};");
                    }
                    
                    this.EmitLine("var entry: boolean;");
                    this.EmitLine("var state: TODO;");

                    if (f.Signature.Parameters.Count > 0)
                    {
                        this.EmitLine($"// NotSupportedArguments: {f.Signature}");
                        // throw new NotSupportedException($"No entry handler arguments supported: {f}");
                    }

                    this.EmitLine($"entry = false;");

                    GenerateStmt(f.Body);

                    var newMachine = $"{m.Name}(entry, state, {string.Join(", ", m.Fields.Select(f => f.Name))})";
                    this.EmitLine($"{Machines()} = {UpdateMachine("r", newMachine)};");
                    // close procedure
                    this.EmitLine("}\n");
                }
            }
        }


        private void GenerateNextBlock(IEnumerable<Machine> ms)
        {
            var machines = ms.ToList();
            // Next picks a random machine and calls the appropriate procedure to step that machine
            this.EmitLine("next {");
            this.EmitLine($"havoc {CurrentMRef()};");
            this.EmitLine($"havoc {CurrentEvent()};");
            // if UPVerifier_ETurn is a live event destined for the right place, then handle it
            this.EmitLine($"if ({LiveEvent(CurrentEvent())}) {{");
            foreach (var m in machines)
            {
                foreach (var s in m.States)
                {
                    foreach (var h in s.AllEventHandlers)
                    {
                        this.EmitLine(
                            $"({IsInstance(GetMachine(CurrentMRef()), m.Name)} && {IsInstance(GetState(GetMachine(CurrentMRef()), m.Name), s.Name)} && !{InEntry(GetMachine(CurrentMRef()))} && {GetTarget(CurrentEvent())} == {CurrentMRef()} && {IsInstance(CurrentEvent(), h.Key.Name)}) : {{");
                        this.EmitLine(
                            $"call {m.Name}_{s.Name}_handle_{h.Key.Name}({CurrentEvent()}, {CurrentMRef()});");
                        this.EmitLine($"}}");
                    }
                }
            }

            this.EmitLine("} else {");
            // else do an entry
            this.EmitLine("case");
            foreach (var m in machines)
            {
                foreach (var s in m.States)
                {
                    if (s.Entry is null) continue;
                    this.EmitLine(
                        $"({IsInstance(GetMachine(CurrentMRef()), m.Name)} && {IsInstance(GetState(GetMachine(CurrentMRef()), m.Name), s.Name)} && {InEntry(GetMachine(CurrentMRef()))}): {{");
                    this.EmitLine($"call {m.Name}_{s.Name}_handle_entry({CurrentMRef()});");
                    this.EmitLine($"}}");
                }
            }

            this.EmitLine("esac");
            this.EmitLine("}");
            // close the next block
            this.EmitLine("}");
            this.EmitLine("\n");
        }


        private void GenerateStmt(IPStmt stmt)
        {
            switch (stmt)
            {
                case CompoundStmt cstmt:
                    foreach (var s in cstmt.Statements)
                    {
                        GenerateStmt(s);
                    }

                    return;
                case AssignStmt astmt:
                    var lhs = AssignmentLhsToString(astmt.Location);
                    var rhs = ExprToString(astmt.Value);
                    this.EmitLine($"{lhs} = {rhs};");
                    return;
                case IfStmt ifstmt:
                    var cond = ExprToString(ifstmt.Condition);
                    this.EmitLine($"if ({cond}) {{");
                    GenerateStmt(ifstmt.ThenBranch);
                    this.EmitLine("} else {");
                    GenerateStmt(ifstmt.ElseBranch);
                    this.EmitLine("}");
                    return;
                case GotoStmt gstmt:
                    var m = gstmt.State.OwningMachine;
                    this.EmitLine($"state = {gstmt.State.Name};");
                    this.EmitLine($"entry = true;");
                    return;
                case SendStmt { Evt: EventRefExpr } sstmt:
                    var eref = (EventRefExpr) sstmt.Evt;
                    var name = eref.Value.Name;
                    var payloadType = eref.Value.PayloadType.OriginalRepresentation;
                    var args = String.Join(", ", sstmt.Arguments.Select(ExprToString));
                    this.EmitLine(
                        $"{Buffer()} = {Buffer()}[{name}(r, {ExprToString(sstmt.MachineExpr)}, {payloadType}({args}))] = true;");
                    return;
                case AssertStmt astmt:
                    this.EmitLine($"assert({ExprToString(astmt.Assertion)});");
                    return;
            }
        }


        private static string TypeToString(PLanguageType t)
        {
            switch (t)
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
                    return StringT();
                case PrimitiveType pt when pt.Equals(PrimitiveType.Null):
                    return Null();
                case PrimitiveType pt when pt.Equals(PrimitiveType.Machine):
                    return RefT();
                case PrimitiveType pt when pt.Equals(PrimitiveType.Event):
                    return EventT();
                case TypeDefType tdt:
                    return tdt.TypeDefDecl.Name;
                case PermissionType _:
                    return RefT();
                case EnumType et:
                    return et.EnumDecl.Name;
                case SetType st:
                    return $"[{TypeToString(st.ElementType)}]boolean";
                case MapType mt:
                    return $"[{TypeToString(mt.KeyType)}]{TypeToString(mt.ValueType)}";
            }

            throw new NotSupportedException($"Not supported type expression: {t} ({t.OriginalRepresentation})");
        }

        private string AssignmentLhsToString(IPExpr lhs)
        {
            return lhs switch
            {
                VariableAccessExpr vax => vax.Variable.Name,
                // throw new NotSupportedException($"Not supported lhs: {lhs}");
                _ => $"NotHandledLhs({lhs})"
            };
        }

        private string ExprToString(IPExpr expr)
        {
            return expr switch
            {
                NamedTupleAccessExpr ntax => $"{ExprToString(ntax.SubExpr)}.{ntax.FieldName}",
                VariableAccessExpr vax => vax.Variable.Name,
                IntLiteralExpr i => i.Value.ToString(),
                BinOpExpr bexpr =>
                    $"{ExprToString(bexpr.Lhs)} {BinOpToString(bexpr.Operation)} {ExprToString(bexpr.Rhs)}",
                // throw new NotSupportedException($"Not supported expr: {expr}");
                _ => $"NotHandledExpr({expr})"
            };
        }

        private string BinOpToString(BinOpType op)
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
                _ => throw new NotImplementedException($"{op} is not implemented yet!")
            };
        }

        private static string GetStartState(Machine m)
        {
            return m.StartState.Name;
        }

        private void EmitLine(string str)
        {
            this._ctx.WriteLine(this._src.Stream, str);
        }
    }
}