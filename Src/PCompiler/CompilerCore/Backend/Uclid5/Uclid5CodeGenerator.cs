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

namespace Plang.Compiler.Backend.Uclid5;

public class Uclid5CodeGenerator : ICodeGenerator
{
    private CompilationContext _ctx;

    private CompiledFile _src;
    
    private HashSet<PLanguageType> _optionsToDeclare;

    private static string BuiltinPrefix => "UPVerifier_";
    private static string UserPrefix => "User_";
    private static string EventPrefix => "Event_";
    private static string MachinePrefix => "Machine_";
    private static string LocalPrefix => "Local_";
    private static string RefT => $"{BuiltinPrefix}MachineRef";
    private static string MachineT => $"{BuiltinPrefix}Machine";
    private static string Null => $"{BuiltinPrefix}Null";
    private static string StringT => $"{BuiltinPrefix}String";
    private static string EventT => $"{BuiltinPrefix}Event";
    private static string Source => "Source";
    private static string Target => "Target";
    private static string Payload => "Payload";
    private static string Entry => "Entry";
    private static string Start => "Start";
    private static string State => "State";
    private static string CurrentMRef => $"{BuiltinPrefix}MTurn";
    private static string CurrentEvent => $"{BuiltinPrefix}ETurn";
    private static string Machines => $"{BuiltinPrefix}Machines";
    private static string Buffer => $"{BuiltinPrefix}Buffer";
    private static string This => "this";
    private static string IncomingEvent => "curr";
    public bool HasCompilationStage => false;

    public IEnumerable<CompiledFile> GenerateCode(ICompilerConfiguration job, Scope globalScope)
    {
        _ctx = new CompilationContext(job);
        _src = new CompiledFile(_ctx.FileName);
        _optionsToDeclare = [];
        GenerateMain(globalScope);
        return new List<CompiledFile> { _src };
    }

    private void GenerateMain(Scope globalScope)
    {
        EmitLine("// The main module contains the entire P program");
        EmitLine("module main {");

        var machines = globalScope.AllDecls.OfType<Machine>().ToList();

        GenerateBuiltInTypeDefs();
        GenerateUserEnumDefs(globalScope.AllDecls.OfType<PEnum>());
        GenerateUserTypeDefs(globalScope.AllDecls.OfType<TypeDef>());
        GenerateEventDefs(globalScope.AllDecls.OfType<PEvent>());
        GenerateMachineDefs(machines);
        GenerateBuiltInVarDecls();
        GenerateInitBlock();
        GenerateEntryProcedures(machines);
        GenerateHandlerProcedures(machines);
        GenerateNextBlock(machines);
        GenerateOptionTypes();
        GenerateControlBlock(machines);

        // close the main module
        EmitLine("}");
    }

    private void GenerateBuiltInTypeDefs()
    {
        EmitLine("// Built-in types");
        EmitLine($"type {RefT};");
        EmitLine($"type {Null} = enum {{ {Null} }};");
        EmitLine($"type {StringT};");
        EmitLine("\n");
    }

    private static string GetUserName(string r)
    {
        return $"{UserPrefix}{r}";
    }

    private static string GetEventName(string r)
    {
        return $"{EventPrefix}{r}";
    }

    private static string GetMachineName(string r)
    {
        return $"{MachinePrefix}{r}";
    }

    private static string GetLocalName(string r)
    {
        return $"{LocalPrefix}{r}";
    }

    private static string GetOptionTypeName(string r)
    {
        return $"Option_{r}";
    }

    private static string ConstructOptionSome(string t, string r)
    {
        return $"{GetOptionTypeName(t)}_Some({r})";
    }
    private static string SelectOptionValue(string t, string r)
    {
        return $"{r}.{GetOptionTypeName(t)}_Some_Value";
    }
    
    private static string IsOptionSome(string t, string r)
    {
        return $"is_{GetOptionTypeName(t)}_Some({r})";
    }

    private static string GetMachine(string r)
    {
        return $"{Machines}[{r}]";
    }

    private static string UpdateMachine(string r, string m)
    {
        return $"{Machines}[{r} -> {m}]";
    }

    private static string UpdateBuffer(string r, bool t)
    {
        return $"{Buffer}[{r} -> {t.ToString().ToLower()}]";
    }

    private static string LiveEvent(string e)
    {
        return $"{Buffer}[{e}]";
    }

    private static string GetSource(string arg)
    {
        return $"{BuiltinPrefix}{Source}({arg})";
    }

    private static string GetTarget(string arg)
    {
        return $"{BuiltinPrefix}{Target}({arg})";
    }

    private static string InEntry(string arg)
    {
        return $"{BuiltinPrefix}{Entry}({arg})";
    }

    private static string InStart(string arg)
    {
        return $"{BuiltinPrefix}{Start}({arg})";
    }

    private static string IsEventInstance(string x, PEvent e)
    {
        return $"is_{GetEventName(e.Name)}({x})";
    }

    private static string IsMachineInstance(string x, Machine m)
    {
        return $"is_{GetMachineName(m.Name)}({x})";
    }

    private static string IsMachineStateInstance(string x, Machine m, State s)
    {
        return $"is_{GetMachineName(m.Name)}_{s.Name}({x}.{GetMachineName(m.Name)}_State)";
    }

    private static string MachineStateT(Machine m)
    {
        return $"{GetMachineName(m.Name)}_StateT";
    }

    private static string EventHandlerName(string m, string s, string e)
    {
        return $"{m}_{s}_handle_{e}";
    }

    private static string EntryHandlerName(string m, string s)
    {
        return $"{m}_{s}_handle_entry";
    }

    private void GenerateUserEnumDefs(IEnumerable<PEnum> enums)
    {
        EmitLine("// User's enumerated types");
        foreach (var e in enums)
        {
            var variants = string.Join(", ", e.Values.Select(v => GetUserName(v.Name)));
            EmitLine($"type {GetUserName(e.Name)} = enum {{{variants}}};");
        }

        EmitLine("\n");
    }

    private void GenerateUserTypeDefs(IEnumerable<TypeDef> types)
    {
        EmitLine("// User's non-enumerated type types");
        foreach (var t in types) EmitLine($"type {GetUserName(t.Name)} = {TypeToString(t.Type)};");

        EmitLine("\n");
    }

    private void GenerateEventDefs(IEnumerable<PEvent> events)
    {
        EmitLine("// Events, their types, and helper functions");
        var es = events.ToList();
        var sum = string.Join("\n\t\t| ", es.Select(ProcessEvent));
        EmitLine($"datatype {EventT} = \n\t\t| {sum};\n");

        string[] attributes = [Source, Target];
        foreach (var attribute in attributes)
        {
            var cases = $"if ({IsEventInstance("e", es.First())}) then e.{GetEventName(es.First().Name)}_{attribute}";
            cases = es.Skip(1).SkipLast(1).Aggregate(cases,
                (current, e) =>
                    current + $"\n\t\telse if ({IsEventInstance("e", e)}) then e.{GetEventName(e.Name)}_{attribute}");
            cases += $"\n\t\telse e.{GetEventName(es.Last().Name)}_{attribute}";
            EmitLine($"define {BuiltinPrefix}{attribute} (e: {EventT}) : {RefT} = \n\t\t{cases};");
            EmitLine("");
        }

        EmitLine("");
        return;

        string ProcessEvent(PEvent e)
        {
            var payload = TypeToString(e.PayloadType);
            return
                $"{GetEventName(e.Name)} ({GetEventName(e.Name)}_{Source}: {RefT}, {GetEventName(e.Name)}_{Target}: {RefT}, {GetEventName(e.Name)}_{Payload}: {payload})";
        }
    }

    private void GenerateMachineDefs(IEnumerable<Machine> machines)
    {
        EmitLine("// Machines, their types, and helper functions");
        var ms = machines.ToList();
        var sum = string.Join("\n\t\t| ", ms.Select(ProcessMachine));
        EmitLine($"datatype {MachineT} = \n\t\t| {sum};\n");

        var entryCases = $"if ({IsMachineInstance("m", ms.First())}) then m.{GetMachineName(ms.First().Name)}_{Entry}";
        entryCases = ms.Skip(1).SkipLast(1).Aggregate(entryCases,
            (current, m) =>
                current + $"\n\t\telse if ({IsMachineInstance("m", m)}) then m.{GetMachineName(m.Name)}_{Entry}");
        entryCases += $"\n\t\telse m.{GetMachineName(ms.Last().Name)}_{Entry}";
        EmitLine($"define {BuiltinPrefix}{Entry} (m: {MachineT}) : boolean = \n\t\t{entryCases};\n");

        var startCases =
            $"if ({IsMachineInstance("m", ms.First())}) then m.{GetMachineName(ms.First().Name)}_{State} == {GetMachineName(ms.First().Name)}_{GetStartState(ms.First())}()";
        foreach (var m in ms.Skip(1).SkipLast(1))
            startCases +=
                $"\n\t\telse if ({IsMachineInstance("m", m)}) then m.{GetMachineName(m.Name)}_{State} == {GetMachineName(m.Name)}_{GetStartState(m)}()";

        startCases +=
            $"\n\t\telse m.{GetMachineName(ms.Last().Name)}_{State} == {GetMachineName(ms.Last().Name)}_{GetStartState(ms.Last())}()";
        EmitLine($"define {BuiltinPrefix}{Start} (m: {MachineT}) : boolean = \n\t\t{startCases};");
        EmitLine("\n");
        return;

        string ProcessMachine(Machine m)
        {
            var states = string.Join(" | ", m.States.Select(s => $"{GetMachineName(m.Name)}_{s.Name}()"));
            var statesName = $"{MachineStateT(m)}";
            EmitLine($"datatype {statesName} = {states};");
            var fields = string.Join(", ",
                m.Fields.Select(f => $"{GetMachineName(m.Name)}_{f.Name}: {TypeToString(f.Type)}"));
            if (m.Fields.Any()) fields = ", " + fields;

            return
                $"{GetMachineName(m.Name)} ({GetMachineName(m.Name)}_{Entry}: boolean, {GetMachineName(m.Name)}_{State}: {statesName}{fields})";
        }
    }

    private void GenerateBuiltInVarDecls()
    {
        // Declare state space
        EmitLine("// State space: machines, buffer, and next machine to step");
        EmitLine($"var {Machines}: [{RefT}]{MachineT};");
        EmitLine($"var {CurrentMRef}: {RefT};");
        EmitLine($"var {Buffer}: [{EventT}]boolean;");
        EmitLine($"var {CurrentEvent}: {EventT};");
        EmitLine("\n");
    }

    private void GenerateInitBlock()
    {
        // Init captures the state of affairs before anything executes
        // Every machine is in their start state, every machine is in Entry, and the buffer is empty
        EmitLine("init {");
        EmitLine("// Every machine begins in their start state");
        EmitLine($"assume(forall (r: {RefT}) :: {InStart(GetMachine("r"))});");
        EmitLine("// Every machine begins with their entry flag set");
        EmitLine($"assume(forall (r: {RefT}) :: {InEntry(GetMachine("r"))});");
        EmitLine("// The buffer starts completely empty");
        EmitLine($"{Buffer} = const(false, [{EventT}]boolean);");
        // close the init block
        EmitLine("}");
        EmitLine("\n");
    }

    private void GenerateGenericHandlerSpec(Machine m, State s)
    {
        EmitLine($"\tmodifies {Machines};");
        EmitLine($"\tmodifies {Buffer};");
        EmitLine($"\trequires {IsMachineInstance(GetMachine(This), m)};");
        EmitLine($"\trequires {IsMachineStateInstance(GetMachine(This), m, s)};");
        EmitLine(
            $"\tensures (forall (r1: {RefT}) :: {This} != r1 ==> old({Machines})[r1] == {GetMachine("r1")});");
    }

    private void GenerateGenericHandlerVars(Machine m, Function f)
    {
        foreach (var v in m.Fields) EmitLine($"var {GetLocalName(v.Name)}: {TypeToString(v.Type)};");

        EmitLine("var entry: boolean;");
        EmitLine($"var state: {MachineStateT(m)};");

        foreach (var v in f.LocalVariables) EmitLine($"var {GetLocalName(v.Name)}: {TypeToString(v.Type)};");

        foreach (var v in f.Signature.Parameters) EmitLine($"var {GetLocalName(v.Name)}: {TypeToString(v.Type)};");
    }

    private void GenerateGenericHandlerPost(Machine m)
    {
        var fields = string.Join(", ", m.Fields.Select(variable => GetLocalName(variable.Name)));
        if (fields.Any()) fields = ", " + fields;

        var newMachine = $"{GetMachineName(m.Name)}(entry, state{fields})";
        EmitLine($"{Machines} = {UpdateMachine(This, newMachine)};");
    }

    private void GenerateHandlerProcedures(IEnumerable<Machine> machines)
    {
        // create all the event handler procedures
        foreach (var m in machines)
        foreach (var s in m.States)
        foreach (var eh in s.AllEventHandlers)
            switch (eh.Value)
            {
                case EventDefer eventDefer:
                    throw new NotSupportedException($"Not supported handler {eventDefer})");
                case EventDoAction action:
                    var e = action.Trigger;
                    var f = action.Target;
                    EmitLine($"// Handler for event {e.Name} in machine {m.Name}");
                    EmitLine(
                        $"procedure {EventHandlerName(m.Name, s.Name, e.Name)} ({This}: {RefT}, {IncomingEvent}: {EventT})");
                    GenerateGenericHandlerSpec(m, s);
                    EmitLine($"\trequires {LiveEvent(IncomingEvent)};");
                    EmitLine($"\trequires {GetSource(IncomingEvent)} == {This};");
                    EmitLine($"\trequires {IsEventInstance(IncomingEvent, e)};");
                    EmitLine($"\trequires !{InEntry(GetMachine(This))};");
                    // open procedure
                    EmitLine("{");
                    GenerateGenericHandlerVars(m, f);
                    // find the variable that corresponds to the payload and assume that it is equal to the payload of the event coming in
                    foreach (var v in f.Signature.Parameters)
                        if (v.Type.Equals(e.PayloadType))
                            EmitLine(
                                $"assume({GetLocalName(v.Name)} == {IncomingEvent}.{GetEventName(e.Name)}_{Payload});");

                    EmitLine($"{Buffer} = {UpdateBuffer(IncomingEvent, false)};");
                    GenerateStmt(f.Body);
                    GenerateGenericHandlerPost(m);
                    // close procedure
                    EmitLine("}\n");
                    break;
                case EventGotoState eventGotoState:
                    throw new NotSupportedException($"Not supported handler {eventGotoState})");
                case EventIgnore eventIgnore:
                    throw new NotSupportedException($"Not supported handler {eventIgnore})");
            }
    }

    private void GenerateEntryProcedures(IEnumerable<Machine> machines)
    {
        foreach (var m in machines)
        foreach (var s in m.States)
        {
            var f = s.Entry;
            if (f is null) continue;

            EmitLine($"// Handler for entry in machine {m.Name} at state {s.Name}");
            EmitLine($"procedure {EntryHandlerName(m.Name, s.Name)}({This}: {RefT})");
            GenerateGenericHandlerSpec(m, s);
            // open procedure
            EmitLine("{");
            GenerateGenericHandlerVars(m, f);
            EmitLine("entry = false;");
            GenerateStmt(f.Body);
            GenerateGenericHandlerPost(m);
            // close procedure
            EmitLine("}\n");
        }
    }


    private void GenerateNextBlock(IEnumerable<Machine> ms)
    {
        var machines = ms.ToList();
        // Next picks a random machine and calls the appropriate procedure to step that machine
        EmitLine("next {");
        EmitLine($"havoc {CurrentMRef};");
        EmitLine($"havoc {CurrentEvent};");
        // if UPVerifier_ETurn is a live event destined for the right place, then handle it
        EmitLine($"if ({LiveEvent(CurrentEvent)}) {{");
        EmitLine("case");
        foreach (var m in machines)
        foreach (var s in m.States)
        foreach (var h in s.AllEventHandlers)
        {
            EmitLine(
                $"({IsMachineInstance(GetMachine(CurrentMRef), m)} && {IsMachineStateInstance(GetMachine(CurrentMRef), m, s)} && !{InEntry(GetMachine(CurrentMRef))} && {GetTarget(CurrentEvent)} == {CurrentMRef} && {IsEventInstance(CurrentEvent, h.Key)}) : {{");
            EmitLine(
                $"call {EventHandlerName(m.Name, s.Name, h.Key.Name)}({CurrentMRef}, {CurrentEvent});");
            EmitLine("}");
        }

        EmitLine("esac");

        EmitLine("} else {");
        // else do an entry
        EmitLine("case");
        foreach (var m in machines)
        foreach (var s in m.States)
        {
            if (s.Entry is null) continue;
            EmitLine(
                $"({IsMachineInstance(GetMachine(CurrentMRef), m)} && {IsMachineStateInstance(GetMachine(CurrentMRef), m, s)} && {InEntry(GetMachine(CurrentMRef))}): {{");
            EmitLine($"call {EntryHandlerName(m.Name, s.Name)}({CurrentMRef});");
            EmitLine("}");
        }

        EmitLine("esac");
        EmitLine("}");
        // close the next block
        EmitLine("}");
        EmitLine("\n");
    }


    private void GenerateStmt(IPStmt stmt)
    {
        switch (stmt)
        {
            case CompoundStmt cstmt:
                foreach (var s in cstmt.Statements) GenerateStmt(s);

                return;
            case AssignStmt astmt:
                switch (astmt.Location)
                {
                    case VariableAccessExpr vax:
                        EmitLine($"{GetLocalName(vax.Variable.Name)} = {ExprToString(astmt.Value)};");
                        return;
                    case MapAccessExpr max:
                        var map = ExprToString(max.MapExpr);
                        var index = ExprToString(max.IndexExpr);
                        var valueType = TypeToString(((MapType)max.MapExpr.Type).ValueType);
                        EmitLine($"{map} = {map}[{index} -> {ConstructOptionSome(valueType, ExprToString(astmt.Value))}];");
                        return;
                }

                throw new NotSupportedException(
                    $"Not supported assignment expression: {astmt} ({astmt.SourceLocation})");
            case IfStmt ifstmt:
                var cond = ExprToString(ifstmt.Condition);
                EmitLine($"if ({cond}) {{");
                GenerateStmt(ifstmt.ThenBranch);
                EmitLine("} else {");
                GenerateStmt(ifstmt.ElseBranch);
                EmitLine("}");
                return;
            case GotoStmt gstmt:
                EmitLine($"state = {GetMachineName(gstmt.State.OwningMachine.Name)}_{gstmt.State.Name}();");
                EmitLine("entry = true;");
                return;
            case SendStmt { Evt: EventRefExpr } sstmt:
                var eref = (EventRefExpr)sstmt.Evt;
                var name = GetEventName(eref.Value.Name);
                var args = string.Join(", ", sstmt.Arguments.Select(ExprToString));
                EmitLine(
                    $"{Buffer} = {Buffer}[{name}({This}, {ExprToString(sstmt.MachineExpr)}, {args}) -> true];");
                return;
            case AssertStmt astmt:
                EmitLine($"// print({ExprToString(astmt.Message)});");
                EmitLine($"assert({ExprToString(astmt.Assertion)});");
                return;
            case PrintStmt { Message: StringExpr } pstmt:
                EmitLine($"// print({ExprToString(pstmt.Message)});");
                return;
        }
        // this.EmitLine($"NotHandledStmt({stmt});");
    }


    private string TypeToString(PLanguageType t)
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
                return StringT;
            case PrimitiveType pt when pt.Equals(PrimitiveType.Null):
                return Null;
            case PrimitiveType pt when pt.Equals(PrimitiveType.Machine):
                return RefT;
            case PrimitiveType pt when pt.Equals(PrimitiveType.Event):
                return EventT;
            case TypeDefType tdt:
                return GetUserName(tdt.TypeDefDecl.Name);
            case PermissionType _:
                return RefT;
            case EnumType et:
                return GetUserName(et.EnumDecl.Name);
            case SetType st:
                return $"[{TypeToString(st.ElementType)}]boolean";
            case MapType mt:
                this._optionsToDeclare.Add(mt.ValueType);
                return $"[{TypeToString(mt.KeyType)}]{GetOptionTypeName(TypeToString(mt.ValueType))}";
        }

        throw new NotSupportedException($"Not supported type expression: {t} ({t.OriginalRepresentation})");
    }

    private string ExprToString(IPExpr expr)
    {
        return expr switch
        {
            NamedTupleAccessExpr ntax => $"{ExprToString(ntax.SubExpr)}.{ntax.FieldName}",
            VariableAccessExpr vax => GetLocalName(vax.Variable.Name),
            IntLiteralExpr i => i.Value.ToString(),
            BinOpExpr bexpr =>
                $"{ExprToString(bexpr.Lhs)} {BinOpToString(bexpr.Operation)} {ExprToString(bexpr.Rhs)}",
            ThisRefExpr => This,
            EnumElemRefExpr e => GetUserName(e.Value.Name),
            NamedTupleExpr t => NamedTupleExprHelper(t),
            StringExpr s =>
                $"\"{s.BaseString}\" {(s.Args.Count != 0 ? "%" : "")} {string.Join(", ", s.Args.Select(ExprToString))}",
            MapAccessExpr maex => 
                SelectOptionValue(TypeToString(((MapType)maex.MapExpr.Type).ValueType), $"{ExprToString(maex.MapExpr)}[{ExprToString(maex.IndexExpr)}]"),
            ContainsExpr cexp => cexp.Collection.Type switch
            {
                MapType mapType => IsOptionSome(TypeToString(mapType.ValueType), $"{ExprToString(cexp.Collection)}[{ExprToString(cexp.Item)}]"),
                SetType _ => $"{ExprToString(cexp.Collection)}[{ExprToString(cexp.Item)}]",
                _ => throw new NotSupportedException($"Not supported expr: {expr} of {cexp.Type.OriginalRepresentation}")
            },
            _ => throw new NotSupportedException($"Not supported expr: {expr}")
            // _ => $"NotHandledExpr({expr})"
        };
    }

    private string NamedTupleExprHelper(NamedTupleExpr t)
    {
        var ty = (NamedTupleType)t.Type;
        var names = ty.Fields.Select(f => f.Name);
        var values = t.TupleFields.Select(ExprToString);
        var args = string.Join(", ", names.Zip(values).Select(p => $"{p.First} := {p.Second}"));
        return $"const_record({args})";
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

    private void GenerateOptionTypes()
    {
        foreach (var ptype in _optionsToDeclare)
        {
            var t = TypeToString(ptype);
            var opt = GetOptionTypeName(t);
            EmitLine($"datatype {opt} = ");
            EmitLine($"\t| {opt}_Some ({opt}_Some_Value: {t})");
            EmitLine($"\t| {opt}_None ();");
        }
        EmitLine("\n");
    }
    
    private void GenerateControlBlock(IEnumerable<Machine> ms)
    {
        var machines = ms.ToList();

        EmitLine("control {");
        EmitLine("bmc(1);");

        foreach (var m in machines)
        foreach (var s in m.States)
        {
            if (s.Entry is null) continue;
            EmitLine($"verify({EntryHandlerName(m.Name, s.Name)});");
            foreach (var h in s.AllEventHandlers)
            {
                EmitLine($"verify({EventHandlerName(m.Name, s.Name, h.Key.Name)});");
            }
        }

        EmitLine("check;");
        EmitLine("print_results;");
        EmitLine("}");
    }

    private static string GetStartState(Machine m)
    {
        return m.StartState.Name;
    }

    private void EmitLine(string str)
    {
        _ctx.WriteLine(_src.Stream, str);
    }
}