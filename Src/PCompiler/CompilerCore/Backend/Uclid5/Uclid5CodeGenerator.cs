using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
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
            EmitLine("var UPVerifier_Buffer: [UPVerifier_Event]boolean;");
            EmitLine("var UPVerifier_Turn: UPVerifier_MachineRef;");
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
            
            // Next picks a random machine and calls the appropriate procedure to step that machine
            EmitLine("next {");
            EmitLine("havoc UPVerifier_Turn;");
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

                throw new NotSupportedException($"Not supported type expression: {t.ToString()}");
                // return t.GetType().ToString();
            }
            void DeclareEvents(IEnumerable<PEvent> es)
            {
                var sum = String.Join("\n\t\t| ", es.Select(e => ProcessEvent(e)));
                EmitLine($"data UPVerifier_Event = \n\t\t| {sum}\n");

                string[] attributes = ["Source", "Target", "Payload"];
                foreach (var attribute in attributes)
                {
                    var cases = $"if (e is {es.First().Name}) then e.{es.First().Name}_{attribute}";
                    foreach (var e in es.Skip(1).SkipLast(1))
                    {
                        cases += $"\n\t\telse if (e is {e.Name}) then e.{e.Name}_{attribute}";
                    }
                    cases += $"\n\t\telse e.{es.Last().Name}_{attribute}";
                    var outtype = attribute == "Payload" ? "UPVerifier_Event" : "UPVerifier_MachineRef"; 
                    EmitLine($"function UPVerifier_{attribute} (e: UPVerifier_Event) : {outtype} = \n\t\t{cases}");
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
            
            void DeclareMachines(IEnumerable<Machine> ms)
            {
                var sum = String.Join("\n\t\t| ", ms.Select(m => ProcessMachine(m)));
                EmitLine($"data UPVerifier_Machine = \n\t\t| {sum}\n");
                
                var entry_cases = $"if (m is {ms.First().Name}) then m.{ms.First().Name}_Entry";
                foreach (var m in ms.Skip(1).SkipLast(1))
                {
                    entry_cases += $"\n\t\telse if (m is {m.Name}) then m.{m.Name}_Entry";
                }
                entry_cases += $"\n\t\telse m.{ms.Last().Name}_Entry";
                EmitLine($"function UPVerifier_Entry (m: UPVerifier_Machine) : boolean = \n\t\t{entry_cases}\n");
                
                var start_cases = $"if (m is {ms.First().Name}) then m.{ms.First().Name}_State == {GetStartState(ms.First())}";
                foreach (var m in ms.Skip(1).SkipLast(1))
                {
                    start_cases += $"\n\t\telse if (m is {m.Name}) then m.{m.Name}_State == {GetStartState(m)}";
                }
                start_cases += $"\n\t\telse m.{ms.Last().Name}_State == {GetStartState(ms.Last())}";
                EmitLine($"function UPVerifier_Start (m: UPVerifier_Machine) : boolean = \n\t\t{start_cases}");
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
        }
    }
}