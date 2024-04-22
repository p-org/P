using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.AST.Statements;
using Plang.Compiler.TypeChecker.AST.States;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.Uclid5 {
    public class Uclid5CodeGenerator : ICodeGenerator
    {
        public bool HasCompilationStage => false;
        public IEnumerable<CompiledFile> GenerateCode(ICompilerConfiguration job, Scope globalScope)
        {
            var context = new CompilationContext(job);
            var uclid5Source = GenerateSource(context, globalScope);
            return new List<CompiledFile> { uclid5Source };
        }
        
        private CompiledFile GenerateSource(CompilationContext context, Scope globalScope)
        {
            var source = new CompiledFile(context.FileName);
            
            // open the main module
            EmitLine("// The main module contains the entire P program");
            EmitLine($"module main {{");

            EmitLine("// Built-in types");
            EmitLine("type UPVerifier_MachineRef;");
            EmitLine("type UPVerifier_Null = enum { UPVerifier_Null };");
            EmitLine("type UPVerifier_String;");
            EmitLine("\n");
            
            // Add enum type definitions
            EmitLine("// Enumerated types");
            foreach (var e in globalScope.AllDecls.OfType<PEnum>())
            {
                DeclareEnum(e);
            }
            EmitLine("\n");
            
            // Add all other type definitions
            EmitLine("// Non-enum types");
            foreach (var t in globalScope.AllDecls.OfType<TypeDef>())
            {
                DeclareType(t);
            }
            EmitLine("\n");
            
            // Add event definitions
            EmitLine("// Events, their types, and helper functions");
            DeclareEvents(globalScope.AllDecls.OfType<PEvent>());
            EmitLine("\n");
            
            // Add machine definitions
            EmitLine("// Machines, their types, and helper functions");
            DeclareMachines(globalScope.AllDecls.OfType<Machine>());
            EmitLine("\n");
            
            // Declare state space
            EmitLine("// State space: machines, buffer, and next machine to step");
            EmitLine("var UPVerifier_Machines: [UPVerifier_MachineRef]UPVerifier_Machine;");
            EmitLine("var UPVerifier_MTurn: UPVerifier_MachineRef;");
            EmitLine("var UPVerifier_Buffer: [UPVerifier_Event]boolean;");
            EmitLine("var UPVerifier_ETurn: UPVerifier_Event;");
            EmitLine("\n");
            
            // Init captures the state of affairs before anything executes
            // Every machine is in their start state, every machine is in Entry, and the buffer is empty
            EmitLine("init {");
            EmitLine("// Every machine begins in their start state");
            EmitLine("assume(forall (r: UPVerifier.MachineRef) :: UPVerifier_Start(UPVerifier_Machines[r]));");
            EmitLine("// Every machine begins with their entry flag set");
            EmitLine("assume(forall (r: UPVerifier.MachineRef) :: UPVerifier_Entry(UPVerifier_Machines[r]));");
            EmitLine("// The buffer starts completely empty");
            EmitLine("UPVerifier_Buffer = const(false, [UPVerifier_Event]boolean);");
            // close the init block
            EmitLine("}");
            EmitLine("\n");
            
            // create all the event handler procedures
            foreach (var eh in globalScope.Machines.SelectMany(m => m.States.SelectMany(s => s.AllEventHandlers)))
            {
                DeclareEventHandler(eh.Value);
            }
            
            // Next picks a random machine and calls the appropriate procedure to step that machine
            EmitLine("next {");
            EmitLine("havoc UPVerifier_MTurn;");
            EmitLine("havoc UPVerifier_ETurn;");
            // close the next block
            EmitLine("}");
            EmitLine("\n");
            
            
            // close the main module
            EmitLine("}");
            
            return source;
            
            void EmitLine(String str)
            {
                context.WriteLine(source.Stream, str);
            }

            void DeclareEnum(PEnum e)
            {
                var variants = String.Join(", ", e.Values.Select(v => v.Name));
                EmitLine($"type {e.Name} = enum {{{variants}}};");
            }
            
            void DeclareType(TypeDef t)
            {
                EmitLine($"type {t.Name} = {ProcessType(t.Type)};");
            }

            String ProcessType(PLanguageType t)
            {
                switch (t)
                {
                    case NamedTupleType ntt:
                        var fields = String.Join(", ", ntt.Fields.Select(nte => $"{nte.Name}: {ProcessType(nte.Type)}"));
                        return $"record {{{fields}}}";
                    case PrimitiveType pt:
                        if (pt.Equals(PrimitiveType.Bool))
                        {
                            return "boolean";
                        }
                        if (pt.Equals(PrimitiveType.Int))
                        {
                            return "integer";
                        }
                        if (pt.Equals(PrimitiveType.String))
                        {
                            return "UPVerifier_String";
                        }
                        if (pt.Equals(PrimitiveType.Null))
                        {
                            return "UPVerifier_Null";
                        }
                        if (pt.Equals(PrimitiveType.Machine))
                        {
                            return "UPVerifier_MachineRef";
                        }
                        if (pt.Equals(PrimitiveType.Event))
                        {
                            return "UPVerifier_Event";
                        }
                        break;
                    case TypeDefType tdt:
                        return tdt.TypeDefDecl.Name;
                    case PermissionType pt:
                        return "UPVerifier_MachineRef";
                    case EnumType et:
                        return et.EnumDecl.Name;
                    case SetType st:
                        return $"[{ProcessType(st.ElementType)}]boolean";
                    case MapType mt:
                        return $"[{ProcessType(mt.KeyType)}]{ProcessType(mt.ValueType)}";
                }

                throw new NotSupportedException($"Not supported type expression: {t} ({t.OriginalRepresentation})");
                // return t.GetType().ToString();
            }
            void DeclareEvents(IEnumerable<PEvent> events)
            {
                var es = events.ToList();
                var sum = String.Join("\n\t\t| ", es.Select(ProcessEvent));
                EmitLine($"data UPVerifier_Event = \n\t\t| {sum}\n");

                string[] attributes = ["Source", "Target", "Payload"];
                foreach (var attribute in attributes)
                {
                    var cases = $"if (e is {es.First().Name}) then e.{es.First().Name}_{attribute}";
                    cases = es.Skip(1).SkipLast(1).Aggregate(cases, (current, e) => current + $"\n\t\telse if (e is {e.Name}) then e.{e.Name}_{attribute}");
                    cases += $"\n\t\telse e.{es.Last().Name}_{attribute}";
                    var outType = attribute == "Payload" ? "UPVerifier_Event" : "UPVerifier_MachineRef"; 
                    EmitLine($"function UPVerifier_{attribute} (e: UPVerifier_Event) : {outType} = \n\t\t{cases}");
                    if (attribute != "Payload")
                    {
                        EmitLine("");
                    }
                }
            }

            String ProcessEvent(PEvent e)
            {
                var payload = ProcessType(e.PayloadType);
                return $"{e.Name} ({e.Name}_Source: UPVerifier_MachineRef, {e.Name}_Target: UPVerifier_MachineRef, {e.Name}_Payload: {payload})";
            }
            
            void DeclareMachines(IEnumerable<Machine> machines)
            {
                var ms = machines.ToList();
                var sum = String.Join("\n\t\t| ", ms.Select(ProcessMachine));
                EmitLine($"data UPVerifier_Machine = \n\t\t| {sum}\n");
                
                var entryCases = $"if (m is {ms.First().Name}) then m.{ms.First().Name}_Entry";
                entryCases = ms.Skip(1).SkipLast(1).Aggregate(entryCases, (current, m) => current + $"\n\t\telse if (m is {m.Name}) then m.{m.Name}_Entry");
                entryCases += $"\n\t\telse m.{ms.Last().Name}_Entry";
                EmitLine($"function UPVerifier_Entry (m: UPVerifier_Machine) : boolean = \n\t\t{entryCases}\n");
                
                var startCases = $"if (m is {ms.First().Name}) then m.{ms.First().Name}_State == {ms.First().Name}_{GetStartState(ms.First())}";
                foreach (var m in ms.Skip(1).SkipLast(1))
                {
                    startCases += $"\n\t\telse if (m is {m.Name}) then m.{m.Name}_State == {m.Name}_{GetStartState(m)}";
                }
                startCases += $"\n\t\telse m.{ms.Last().Name}_State == {ms.Last().Name}_{GetStartState(ms.Last())}";
                EmitLine($"function UPVerifier_Start (m: UPVerifier_Machine) : boolean = \n\t\t{startCases}");
            }

            String GetStartState(Machine m)
            {
                return m.StartState.Name;
            }
            
            String ProcessMachine(Machine m)
            {
                var states = "enum {" + String.Join(", ", m.States.Select(s => $"{m.Name}_{s.Name}")) + "}";
                var fields = String.Join(", ", m.Fields.Select(f => $"{m.Name}_{f.Name}: {ProcessType(f.Type)}"));
                return $"{m.Name} ({m.Name}_Entry: boolean, {m.Name}_State: {states}, {fields})";
            }

            void DeclareEventHandler(IStateAction h)
            {
                switch (h)
                {
                    case EventDoAction action:
                        var e = action.Trigger;
                        var f = action.Target;
                        var m = f.Owner;
                        EmitLine($"// Handler for event {e.Name} in machine {m.Name}");
                        EmitLine($"procedure {m.Name}_handle_{e.Name} (r: UPVerifier_MachineRef, e: UPVerifier_Event)");
                        EmitLine("\tmodifies UPVerifier_Machines;");
                        EmitLine("\tmodifies UPVerifier_Buffer;");
                        EmitLine("\trequires UPVerifier_Buffer[e];");
                        EmitLine($"\trequires UPVerifier_Source(e) == r;");
                        EmitLine($"\trequires e is {e.Name};");
                        EmitLine($"\trequires UPVerifier_Machines[r] is {m.Name};");
                        EmitLine($"\tensures (forall (r1: UPVerifier_MachineRef) r != r1 ==> old(UPVerifier_Machines)[r1] == UPVerifier_Machines[r1]);");
                        EmitLine($"\tensures (forall (e1: UPVerifier_Event) e != e1 ==> (old(UPVerifier_Buffer)[e1] == UPVerifier_Buffer[e1] || (UPVerifier_Source(e1) == r && !old(UPVerifier_Buffer)[e1])));");
                        // open procedure
                        EmitLine("{");
                        foreach (var v in f.LocalVariables)
                        {
                            EmitLine($"var {v.Name}: {ProcessType(v.Type)};");
                        }
                        EmitLine("var m: UPVerifier_Machine;");
                        
                        if (f.Signature.Parameters.Count > 1)
                        {
                            throw new NotSupportedException($"Only one event handler argument supported: {h}");
                        } else if (f.Signature.Parameters.Count == 1)
                        {
                            var arg = f.Signature.Parameters.First();
                            EmitLine($"var {arg.Name}: {ProcessType(arg.Type)};");
                            EmitLine($"{arg.Name} = UPVerifier_Payload(e);");
                        }
                        EmitLine("UPVerifier_Buffer[e -> false];");
                        // make all modifications to m 
                        EmitLine("m = UPVerifier_Machines[r];");

                        ProcessStatement(f.Body);
                        
                        EmitLine("UPVerifier_Machines[r -> m];");
                        // close procedure
                        EmitLine("}\n");
                        return; 
                }
                // throw new NotSupportedException($"Not supported event handler: {h}");
                EmitLine($"// Not handled yet: {h}");
            }

            void ProcessStatement(IPStmt stmt)
            {
                switch (stmt)
                {
                    case CompoundStmt cstmt:
                        foreach (var s in cstmt.Statements)
                        {
                            ProcessStatement(s);
                        }
                        return;
                    case AssignStmt astmt:
                        var lhs = ProcessAssignmentLHS(astmt.Location);
                        var rhs = ProcessExpr(astmt.Value);
                        EmitLine($"// {lhs} = {rhs};");
                        return;
                    case IfStmt ifstmt:
                        var cond = ProcessExpr(ifstmt.Condition);
                        EmitLine($"if ({cond}) {{");
                        ProcessStatement(ifstmt.ThenBranch);
                        EmitLine("} else {");
                        ProcessStatement(ifstmt.ElseBranch);
                        EmitLine("}");
                        return;
                }
                EmitLine($"// Not handled yet: {stmt}");
            }

            String ProcessAssignmentLHS(IPExpr lhs)
            {
                switch (lhs)
                {
                    case VariableAccessExpr vax:
                        return vax.Variable.Name;
                }
                // throw new NotSupportedException($"Not supported lhs: {lhs}");
                return $"NotHandledLHS({lhs})";
            }

            String ProcessExpr(IPExpr expr)
            {
                switch (expr)
                {
                    case NamedTupleAccessExpr ntax:
                        return $"{ProcessExpr(ntax.SubExpr)}.{ntax.FieldName}";
                    case VariableAccessExpr vax:
                        return vax.Variable.Name;
                }
                
                // throw new NotSupportedException($"Not supported expr: {expr}");
                return $"NotHandledExpr({expr})";
            }
        }
    }
}