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
            this.EmitLine("type UPVerifier_MachineRef;");
            this.EmitLine("type UPVerifier_Null = enum { UPVerifier_Null };");
            this.EmitLine("type UPVerifier_String;");
            this.EmitLine("\n");
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
            this.EmitLine($"datatype UPVerifier_Event = \n\t\t| {sum}\n");

            string[] attributes = ["Source", "Target", "Payload"];
            foreach (var attribute in attributes)
            {
                var cases = $"if (e is {es.First().Name}) then e.{es.First().Name}_{attribute}";
                cases = es.Skip(1).SkipLast(1).Aggregate(cases,
                    (current, e) => current + $"\n\t\telse if (e is {e.Name}) then e.{e.Name}_{attribute}");
                cases += $"\n\t\telse e.{es.Last().Name}_{attribute}";
                var outType = attribute == "Payload" ? "UPVerifier_Event" : "UPVerifier_MachineRef";
                this.EmitLine($"function UPVerifier_{attribute} (e: UPVerifier_Event) : {outType} = \n\t\t{cases}");
                if (attribute != "Payload")
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
                    $"{e.Name} ({e.Name}_Source: UPVerifier_MachineRef, {e.Name}_Target: UPVerifier_MachineRef, {e.Name}_Payload: {payload})";
            }
        }

        private void GenerateMachineDefs(IEnumerable<Machine> machines)
        {
            this.EmitLine("// Machines, their types, and helper functions");
            var ms = machines.ToList();
            var sum = string.Join("\n\t\t| ", ms.Select(ProcessMachine));
            this.EmitLine($"datatype UPVerifier_Machine = \n\t\t| {sum}\n");

            var entryCases = $"if (m is {ms.First().Name}) then m.{ms.First().Name}_Entry";
            entryCases = ms.Skip(1).SkipLast(1).Aggregate(entryCases,
                (current, m) => current + $"\n\t\telse if (m is {m.Name}) then m.{m.Name}_Entry");
            entryCases += $"\n\t\telse m.{ms.Last().Name}_Entry";
            this.EmitLine($"function UPVerifier_Entry (m: UPVerifier_Machine) : boolean = \n\t\t{entryCases}\n");

            var startCases =
                $"if (m is {ms.First().Name}) then m.{ms.First().Name}_State == {ms.First().Name}_{GetStartState(ms.First())}";
            foreach (var m in ms.Skip(1).SkipLast(1))
            {
                startCases += $"\n\t\telse if (m is {m.Name}) then m.{m.Name}_State == {m.Name}_{GetStartState(m)}";
            }

            startCases += $"\n\t\telse m.{ms.Last().Name}_State == {ms.Last().Name}_{GetStartState(ms.Last())}";
            this.EmitLine($"function UPVerifier_Start (m: UPVerifier_Machine) : boolean = \n\t\t{startCases}");
            this.EmitLine("\n");
            return;

            string ProcessMachine(Machine m)
            {
                var states = "enum {" + string.Join(", ", m.States.Select(s => $"{m.Name}_{s.Name}")) + "}";
                var fields = string.Join(", ", m.Fields.Select(f => $"{m.Name}_{f.Name}: {TypeToString(f.Type)}"));
                return $"{m.Name} ({m.Name}_Entry: boolean, {m.Name}_State: {states}, {fields})";
            }
        }

        private void GenerateBuiltInVarDecls()
        {
            // Declare state space
            this.EmitLine("// State space: machines, buffer, and next machine to step");
            this.EmitLine("var UPVerifier_Machines: [UPVerifier_MachineRef]UPVerifier_Machine;");
            this.EmitLine("var UPVerifier_MTurn: UPVerifier_MachineRef;");
            this.EmitLine("var UPVerifier_Buffer: [UPVerifier_Event]boolean;");
            this.EmitLine("var UPVerifier_ETurn: UPVerifier_Event;");
            this.EmitLine("\n");
        }

        private void GenerateInitBlock()
        {
            // Init captures the state of affairs before anything executes
            // Every machine is in their start state, every machine is in Entry, and the buffer is empty
            this.EmitLine("init {");
            this.EmitLine("// Every machine begins in their start state");
            this.EmitLine("assume(forall (r: UPVerifier.MachineRef) :: UPVerifier_Start(UPVerifier_Machines[r]));");
            this.EmitLine("// Every machine begins with their entry flag set");
            this.EmitLine("assume(forall (r: UPVerifier.MachineRef) :: UPVerifier_Entry(UPVerifier_Machines[r]));");
            this.EmitLine("// The buffer starts completely empty");
            this.EmitLine("UPVerifier_Buffer = const(false, [UPVerifier_Event]boolean);");
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
                                    $"procedure {m.Name}_{s.Name}_handle_{e.Name} (r: UPVerifier_MachineRef, e: UPVerifier_Event)");
                                this.EmitLine("\tmodifies UPVerifier_Machines;");
                                this.EmitLine("\tmodifies UPVerifier_Buffer;");
                                this.EmitLine("\trequires UPVerifier_Buffer[e];");
                                this.EmitLine($"\trequires UPVerifier_Source(e) == r;");
                                this.EmitLine($"\trequires e is {e.Name};");
                                this.EmitLine($"\trequires UPVerifier_Machines[r] is {m.Name};");
                                this.EmitLine($"\trequires UPVerifier_Machines[r].{m.Name}_state is {s.Name};");
                                this.EmitLine($"\trequires !UPVerifier_Machines[r].{m.Name}_entry;");
                                this.EmitLine(
                                    $"\tensures (forall (r1: UPVerifier_MachineRef) r != r1 ==> old(UPVerifier_Machines)[r1] == UPVerifier_Machines[r1]);");
                                this.EmitLine(
                                    $"\tensures (forall (e1: UPVerifier_Event) e != e1 ==> (old(UPVerifier_Buffer)[e1] == UPVerifier_Buffer[e1] || (UPVerifier_Source(e1) == r && !old(UPVerifier_Buffer)[e1])));");
                                // open procedure
                                this.EmitLine("{");
                                foreach (var v in f.LocalVariables)
                                {
                                    this.EmitLine($"var {v.Name}: {TypeToString(v.Type)};");
                                }

                                this.EmitLine("var m: UPVerifier_Machine;");

                                switch (f.Signature.Parameters.Count)
                                {
                                    case > 1:
                                        throw new NotSupportedException(
                                            $"Only one event handler argument supported: {action}");
                                    case 1:
                                    {
                                        var arg = f.Signature.Parameters.First();
                                        this.EmitLine($"var {arg.Name}: {TypeToString(arg.Type)};");
                                        this.EmitLine($"{arg.Name} = UPVerifier_Payload(e);");
                                        break;
                                    }
                                }

                                this.EmitLine("UPVerifier_Buffer[e -> false];");
                                // make all modifications to m 
                                this.EmitLine("m = UPVerifier_Machines[r];");

                                GenerateStmt(f.Body);

                                this.EmitLine("UPVerifier_Machines[r -> m];");
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
                    this.EmitLine($"procedure {m.Name}_{s.Name}_handle_entry (r: UPVerifier_MachineRef)");
                    this.EmitLine("\tmodifies UPVerifier_Machines;");
                    this.EmitLine("\tmodifies UPVerifier_Buffer;");
                    this.EmitLine($"\trequires UPVerifier_Machines[r] is {m.Name};");
                    this.EmitLine($"\trequires UPVerifier_Machines[r].{m.Name}_state is {s.Name};");
                    this.EmitLine($"\trequires UPVerifier_Machines[r].{m.Name}_entry;");
                    this.EmitLine($"\tensures !UPVerifier_Machines[r].{m.Name}_entry;");
                    // open procedure
                    this.EmitLine("{");
                    foreach (var v in f.LocalVariables)
                    {
                        this.EmitLine($"var {v.Name}: {TypeToString(v.Type)};");
                    }

                    this.EmitLine("var m: UPVerifier_Machine;");

                    if (f.Signature.Parameters.Count > 0)
                    {
                        this.EmitLine($"// NotSupportedArguments: {f.Signature}");
                        // throw new NotSupportedException($"No entry handler arguments supported: {f}");
                    }

                    // make all modifications to m 
                    this.EmitLine("m = UPVerifier_Machines[r];");
                    this.EmitLine($"m.{m.Name}_entry = false;");

                    GenerateStmt(f.Body);

                    this.EmitLine("UPVerifier_Machines[r -> m];");
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
            this.EmitLine("havoc UPVerifier_MTurn;");
            this.EmitLine("havoc UPVerifier_ETurn;");
            // if UPVerifier_ETurn is a live event destined for the right place, then handle it
            this.EmitLine("if (UPVerifier_Buffer[UPVerifier_ETurn]) {");
            foreach (var m in machines)
            {
                foreach (var s in m.States)
                {
                    foreach (var h in s.AllEventHandlers)
                    {
                        this.EmitLine(
                            $"(UPVerifier_Machines[UPVerifier_MTurn] is {m.Name} && UPVerifier_Machines[UPVerifier_MTurn].{m.Name}_state is {s.Name} && !UPVerifier_Machines[UPVerifier_MTurn].{m.Name}_entry && UPVerifier_Target(UPVerifier_ETurn) == UPVerifier_MTurn && UPVerifier_ETurn is {h.Key.Name}) : {{");
                        this.EmitLine(
                            $"call {m.Name}_{s.Name}_handle_{h.Key.Name}(UPVerifier_MTurn, UPVerifier_MTurn);");
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
                        $"(UPVerifier_Machines[UPVerifier_MTurn] is {m.Name} && UPVerifier_Machines[UPVerifier_MTurn].{m.Name}_state is {s.Name}) && UPVerifier_Machines[UPVerifier_MTurn].{m.Name}_entry: {{");
                    this.EmitLine($"call {m.Name}_{s.Name}_handle_entry(UPVerifier_MTurn);");
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
                    this.EmitLine($"m.{m.Name}_state = {gstmt.State.Name};");
                    this.EmitLine($"m.{m.Name}_entry = true;");
                    return;
                case SendStmt sstmt when sstmt.Evt is EventRefExpr:
                    var eref = (EventRefExpr) sstmt.Evt;
                    var name = eref.Value.Name;
                    var payloadType = eref.Value.PayloadType.OriginalRepresentation;
                    var args = String.Join(", ", sstmt.Arguments.Select(ExprToString));
                    this.EmitLine(
                        $"UPVerifier_Buffer[{name}({name}_source = r, {name}_target = {ExprToString(sstmt.MachineExpr)}, {name}_payload = {payloadType}({args}))] = true;");
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
                    return "UPVerifier_String";
                case PrimitiveType pt when pt.Equals(PrimitiveType.Null):
                    return "UPVerifier_Null";
                case PrimitiveType pt when pt.Equals(PrimitiveType.Machine):
                    return "UPVerifier_MachineRef";
                case PrimitiveType pt when pt.Equals(PrimitiveType.Event):
                    return "UPVerifier_Event";
                case TypeDefType tdt:
                    return tdt.TypeDefDecl.Name;
                case PermissionType _:
                    return "UPVerifier_MachineRef";
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