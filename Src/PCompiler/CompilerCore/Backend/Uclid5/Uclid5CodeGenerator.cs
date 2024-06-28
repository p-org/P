using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
    private HashSet<SetType> _setCheckersToDeclare;
    public bool HasCompilationStage => false;

    public IEnumerable<CompiledFile> GenerateCode(ICompilerConfiguration job, Scope globalScope)
    {
        _ctx = new CompilationContext(job);
        _src = new CompiledFile(_ctx.FileName);
        _optionsToDeclare = [];
        _setCheckersToDeclare = new HashSet<SetType>();
        GenerateMain(globalScope);
        return new List<CompiledFile> { _src };
    }

    private void EmitLine(string str)
    {
        _ctx.WriteLine(_src.Stream, str);
    }

    // Prefixes to avoid name clashes and keywords
    private static string BuiltinPrefix => "P_";
    private static string UserPrefix => "User_";
    private static string EventPrefix => "PEvent_";
    private static string GotoPrefix => "PGoto_";
    private static string MachinePrefix => "PMachine_";
    private static string LocalPrefix => "PLocal_";
    private static string OptionPrefix => "POption_";
    private static string CheckerPrefix => "PChecklist_";

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
     * type StateADT = record {buffer: [LabelADT]boolean, machines: [MachineRefT]MachineStateADT};
     * var state: StateT;
     *******************************/
    private static string StateAdt => $"{BuiltinPrefix}StateAdt";
    private static string StateAdtBufferSelector => $"{StateAdt}_Buffer";
    private static string StateAdtMachinesSelector => $"{StateAdt}_Machines";

    private static string StateAdtConstruct(string buffer, string machines)
    {
        return $"const_record({StateAdtBufferSelector} := {buffer}, {StateAdtMachinesSelector} := {machines})";
    }

    private static string StateAdtSelectBuffer(string state)
    {
        return $"{state}.{StateAdtBufferSelector}";
    }

    private static string StateAdtSelectMachines(string state)
    {
        return $"{state}.{StateAdtMachinesSelector}";
    }

    private static string StateAdtDeclaration()
    {
        return
            $"type {StateAdt} = record {{{StateAdtBufferSelector}: [{LabelAdt}]boolean, {StateAdtMachinesSelector}: [{MachineRefT}]{MachineStateAdt}}};";
    }

    private static string StateVar => $"{BuiltinPrefix}State";
    private static string StateVarDeclaration => $"var {StateVar}: {StateAdt};";

    private static string DerefFunctionDeclaration =>
        $"define {BuiltinPrefix}Deref({LocalPrefix}r: {MachineRefT}) : {MachineStateAdt} = {StateAdtSelectMachines(StateVar)}[{LocalPrefix}r];";

    private static string Deref(string r)
    {
        return $"{BuiltinPrefix}Deref({r})";
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
        return MachineStateAdtIsM(state, m) +
               $" && {MachineStateAdtSelectState(state, m)} == {MachinePrefix}{m.Name}_{s.Name}";
    }


    private static string InStartPredicateDeclaration(List<Machine> machines)
    {
        var input = $"{LocalPrefix}State";
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
        var input = $"{LocalPrefix}State";
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

    private static string LabelAdtDeclaration()
    {
        return
            $"type {LabelAdt} = record {{{LabelAdtTargetSelector}: {MachineRefT}, {LabelAdtActionSelector}: {EventOrGotoAdt}}};";
    }

    private static string LabelAdtConstruct(string target, string action)
    {
        return $"const_record({LabelAdtTargetSelector} := {target}, {LabelAdtActionSelector} := {action})";
    }

    private static string LabelAdtSelectTarget(string label)
    {
        return $"{label}.{LabelAdtTargetSelector}";
    }

    private static string LabelAdtSelectAction(string label)
    {
        return $"{label}.{LabelAdtActionSelector}";
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

    private string EventOrGotoAdtConstructEvent(PEvent ev, IPExpr arg)
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

    private string EventAdtDeclaration(List<PEvent> events)
    {
        var declarationSum = string.Join("\n\t\t| ", events.Select(EventDeclarationCase));
        return $"datatype {EventAdt} = \n\t\t| {declarationSum};";

        string EventDeclarationCase(PEvent e)
        {
            var pt = TypeToString(e.PayloadType);
            return
                $"{EventPrefix}{e.Name} ({EventPrefix}{e.Name}_Payload: {pt})";
        }
    }

    private static string EventAdtSelectPayload(string eadt, PEvent e)
    {
        return $"{eadt}.{EventPrefix}{e.Name}_Payload";
    }

    private static string EventAdtConstruct(string payload, PEvent e)
    {
        return $"{EventPrefix}{e.Name}({payload})";
    }

    private static string EventAdtIsE(string instance, PEvent e)
    {
        return $"is_{EventPrefix}{e.Name}({instance})";
    }

    private static string EventOrGotoAdtIsE(string instance, PEvent e)
    {
        var isEvent = EventOrGotoAdtIsEvent(instance);
        var selectEvent = EventOrGotoAdtSelectEvent(instance);
        var correctEvent = EventAdtIsE(selectEvent, e);
        return $"{isEvent} && {correctEvent}";
    }

    private static string LabelAdtIsE(string instance, PEvent e)
    {
        var action = LabelAdtSelectAction(instance);
        return EventOrGotoAdtIsE(action, e);
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
        return $"{isGoto} && {correctGoto}";
    }

    private static string LabelAdtIsS(string instance, State s)
    {
        var action = LabelAdtSelectAction(instance);
        return EventOrGotoAdtIsS(action, s);
    }

    /********************************
     * Traverse the P AST and generate the UCLID5 code using the types and helpers defined above
     *******************************/
    private void GenerateMain(Scope globalScope)
    {
        EmitLine("module main {");

        var machines = globalScope.AllDecls.OfType<Machine>().ToList();
        var events = globalScope.AllDecls.OfType<PEvent>().ToList();

        EmitLine(PNullDeclaration);
        EmitLine(DefaultMachineDeclaration);
        EmitLine(StringTDeclaration);
        EmitLine(DefaultStringDeclaration);
        EmitLine("");

        GenerateUserEnums(globalScope.AllDecls.OfType<PEnum>());
        GenerateUserTypes(globalScope.AllDecls.OfType<TypeDef>());
        EmitLine("");

        EmitLine(EventAdtDeclaration(events));
        EmitLine("");
        EmitLine(GotoAdtDeclaration(machines));
        EmitLine("");
        EmitLine(EventOrGotoAdtDeclaration());
        EmitLine(LabelAdtDeclaration());
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

        EmitLine(InStartPredicateDeclaration(machines));
        EmitLine(InEntryPredicateDeclaration());
        EmitLine(DerefFunctionDeclaration);
        EmitLine("");

        GenerateInitBlock(machines);
        EmitLine("");

        // pick a random label and handle it (with some guards to make sure we always handle gotos before events)
        GenerateNextBlock(machines, events);
        EmitLine("");

        // non-handler functions
        GenerateGlobalProcedures(globalScope.AllDecls.OfType<Function>());
        GenerateMachineProcedures(machines);
        EmitLine("");

        // generate the handlers
        foreach (var m in machines)
        {
            foreach (var s in m.States)
            {
                GenerateEntryHandler(s);
                foreach (var e in events.Where(e => !e.IsNullEvent && s.HasHandler(e)))
                {
                    GenerateEventHandler(s, e);
                }
            }
        }

        EmitLine("");

        // These have to be done at the end because we don't know what we need until we generate the rest of the code
        GenerateOptionTypes();
        EmitLine("");
        GenerateCheckerVars();
        EmitLine("");

        GenerateControlBlock(machines, events);

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

    private void GenerateInitBlock(List<Machine> machines)
    {
        var state = Deref("r");
        EmitLine("init {");
        EmitLine("// Every machine begins in their start state");
        EmitLine($"assume(forall (r: {MachineRefT}) :: {BuiltinPrefix}InStart({state}));");
        EmitLine("// Every machine begins with their entry flag set");
        EmitLine($"assume(forall (r: {MachineRefT}) :: {BuiltinPrefix}InEntry({state}));");
        EmitLine("// Every machine begins with fields in default");
        foreach (var m in machines)
        {
            foreach (var f in m.Fields)
            {
                EmitLine(
                    $"assume(forall (r: {MachineRefT}) :: {MachineStateAdtSelectField(state, m, f)} == {DefaultValue(f.Type)});");
            }
        }

        EmitLine("// The buffer starts completely empty");
        EmitLine($"{StateAdtSelectBuffer(StateVar)} = const(false, [{LabelAdt}]boolean);");
        // close the init block
        EmitLine("}");
    }

    private void GenerateNextBlock(List<Machine> machines, List<PEvent> events)
    {
        var currentLabel = $"{BuiltinPrefix}CurrentLabel";
        // pick a random label and handle it
        EmitLine("next {");
        EmitLine($"var {currentLabel}: {LabelAdt};");
        EmitLine($"if ({StateAdtSelectBuffer(StateVar)}[{currentLabel}]) {{");
        EmitLine("case");
        foreach (var m in machines)
        {
            foreach (var s in m.States)
            {
                if (s.Entry is not null)
                {
                    EmitLine($"({GotoGuard(m, s)}) : {{");
                    EmitLine($"call {m.Name}_{s.Name}({currentLabel});");
                    EmitLine("}");
                }

                foreach (var e in events.Where(e => !e.IsNullEvent))
                {
                    EmitLine($"({EventGuard(m, s, e)}) : {{");
                    if (s.HasHandler(e))
                    {
                        EmitLine($"call {m.Name}_{s.Name}_{e.Name}({currentLabel});");
                    }
                    else
                    {
                        EmitLine($"// There is no handler for {e.Name} in {s.Name} of {m.Name}");
                        EmitLine("assert false;");
                    }

                    EmitLine("}");
                }
            }
        }

        EmitLine("esac");
        EmitLine("}");
        // close the next block
        EmitLine("}");
        return;

        string EventGuard(Machine m, State s, PEvent e)
        {
            var correctMachine = MachineStateAdtIsM(Deref(LabelAdtSelectTarget(currentLabel)), m);
            var correctState = MachineStateAdtInS(Deref(LabelAdtSelectTarget(currentLabel)), m, s);
            var correctEvent = LabelAdtIsE(currentLabel, e);
            return string.Join(" && ", [correctMachine, correctState, correctEvent]);
        }

        string GotoGuard(Machine m, State s)
        {
            var correctMachine = MachineStateAdtIsM(Deref(LabelAdtSelectTarget(currentLabel)), m);
            var correctGoto = LabelAdtIsS(currentLabel, s);
            return $"{correctMachine} && {correctGoto}";
        }
    }

    private void GenerateGlobalProcedures(IEnumerable<Function> functions)
    {
        // TODO: these should be side-effect free and we should enforce that
        foreach (var f in functions)
        {
            var ps = f.Signature.Parameters.Select(p => $"{p.Name}: {TypeToString(p.Type)}");
            EmitLine($"procedure [noinline] {f.Name}({string.Join(", ", ps)})");
            if (!f.Signature.ReturnType.IsSameTypeAs(PrimitiveType.Null))
            {
                EmitLine($"\treturns ({BuiltinPrefix}Return: {TypeToString(f.Signature.ReturnType)})");
            }

            EmitLine("{");
            GenerateStmt(f.Body);
            EmitLine("}\n");
        }
    }

    private void GenerateMachineProcedures(List<Machine> machines)
    {
        foreach (var m in machines)
        {
            foreach (var f in m.Methods)
            {
                var ps = f.Signature.Parameters.Select(p => $"{GetLocalName(p)}: {TypeToString(p.Type)}")
                    .Prepend($"this: {MachineRefT}");
                var line = _ctx.LocationResolver.GetLocation(f.SourceLocation).Line;
                var name = f.Name == "" ? $"{BuiltinPrefix}{f.Owner.Name}_f{line}" : f.Name;
                EmitLine($"procedure [inline] {name}({string.Join(", ", ps)})");

                var currState = Deref("this");

                EmitLine($"\trequires {MachineStateAdtIsM(currState, m)};");
                EmitLine(
                    $"\tensures (forall (r1: {MachineRefT}) :: this != r1 ==> {StateAdtSelectMachines($"old({StateVar})")}[r1] == {Deref("r1")});");
                if (!f.Signature.ReturnType.IsSameTypeAs(PrimitiveType.Null))
                {
                    EmitLine($"\treturns ({BuiltinPrefix}Return: {TypeToString(f.Signature.ReturnType)})");
                }

                EmitLine("{");

                // declare necessary local variables
                EmitLine($"var {LocalPrefix}stage: boolean;");
                EmitLine($"var {LocalPrefix}state: {MachinePrefix}{m.Name}_StateAdt;");
                foreach (var v in m.Fields) EmitLine($"var {GetLocalName(v)}: {TypeToString(v.Type)};");
                foreach (var v in f.LocalVariables) EmitLine($"var {GetLocalName(v)}: {TypeToString(v.Type)};");

                // initialize all the local variables to the correct values
                EmitLine($"{LocalPrefix}stage = false;"); // this can be set to true by a send statement
                EmitLine($"{LocalPrefix}state = {MachineStateAdtSelectState(currState, m)};");
                foreach (var v in m.Fields)
                    EmitLine($"{GetLocalName(v)} = {MachineStateAdtSelectField(currState, m, v)};");
                foreach (var v in f.LocalVariables) EmitLine($"{GetLocalName(v)} = {DefaultValue(v.Type)};");

                GenerateStmt(f.Body);

                var fields = m.Fields.Select(GetLocalName).Prepend($"{LocalPrefix}state").ToList();

                // make a new machine
                var newMachine = MachineAdtConstructM(m, fields);
                // make a new machine state
                var newMachineState = MachineStateAdtConstruct($"{LocalPrefix}stage", newMachine);
                // update the machine map
                EmitLine(
                    $"{StateAdtSelectMachines(StateVar)} = {StateAdtSelectMachines(StateVar)}[this -> {newMachineState}];");

                EmitLine("}\n");
            }
        }
    }

    private void GenerateEntryHandler(State s)
    {
        if (s.Entry is null)
        {
            return;
        }

        var label = $"{LocalPrefix}Label";
        EmitLine($"procedure [noinline] {s.OwningMachine.Name}_{s.Name}({label}: {LabelAdt})");

        var target = LabelAdtSelectTarget(label);
        var targetMachineState = Deref(target);
        var action = LabelAdtSelectAction(label);
        var g = EventOrGotoAdtSelectGoto(action);
        var buffer = StateAdtSelectBuffer(StateVar);

        var f = s.Entry;
        var line = _ctx.LocationResolver.GetLocation(f.SourceLocation).Line;
        var name = f.Name == "" ? $"{BuiltinPrefix}{f.Owner.Name}_f{line}" : f.Name;

        EmitLine($"\trequires {StateAdtSelectBuffer(StateVar)}[{label}];");
        EmitLine($"\trequires {MachineStateAdtIsM(targetMachineState, s.OwningMachine)};");
        EmitLine($"\trequires {EventOrGotoAdtIsGoto(action)};");
        EmitLine($"\trequires {GotoAdtIsS(g, s)};");
        EmitLine($"\trequires {MachineStateAdtIsM(targetMachineState, s.OwningMachine)};");
        EmitLine(
            $"\tensures (forall (r1: {MachineRefT}) :: {target} != r1 ==> {StateAdtSelectMachines($"old({StateVar})")}[r1] == {Deref("r1")});");
        EmitLine($"\tensures !{buffer}[{label}];");

        var payload = f.Signature.Parameters.Count > 0 ? $", {GotoAdtSelectParam(g, f.Signature.Parameters[0].Name, s)}" : "";

        EmitLine("{");
        EmitLine($"{buffer} = {buffer}[{label} -> false];");
        EmitLine($"call {name}({target}{payload});");
        EmitLine("}\n");
    }


    private void GenerateEventHandler(State s, PEvent ev)
    {
        var label = $"{LocalPrefix}Label";
        EmitLine($"procedure [noinline] {s.OwningMachine.Name}_{s.Name}_{ev.Name}({label}: {LabelAdt})");

        var target = LabelAdtSelectTarget(label);
        var targetMachineState = Deref(target);
        var action = LabelAdtSelectAction(label);
        var e = EventOrGotoAdtSelectEvent(action);
        var buffer = StateAdtSelectBuffer(StateVar);

        EmitLine($"\trequires {StateAdtSelectBuffer(StateVar)}[{label}];");
        EmitLine($"\trequires {MachineStateAdtIsM(targetMachineState, s.OwningMachine)};");
        EmitLine($"\trequires {EventOrGotoAdtIsEvent(action)};");
        EmitLine($"\trequires {EventAdtIsE(e, ev)};");
        EmitLine($"\trequires {MachineStateAdtIsM(targetMachineState, s.OwningMachine)};");
        EmitLine(
            $"\tensures (forall (r1: {MachineRefT}) :: {target} != r1 ==> {StateAdtSelectMachines($"old({StateVar})")}[r1] == {Deref("r1")});");

        var handler = s.AllEventHandlers.ToDictionary()[ev];

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
                EmitLine($"\tensures !{buffer}[{label}];");
                var payload = f.Signature.Parameters.Count > 0 ? $", {EventAdtSelectPayload(e, ev)}" : "";
                EmitLine("{");
                EmitLine($"{buffer} = {buffer}[{label} -> false];");
                EmitLine($"call {name}({target}{payload});");
                EmitLine("}\n");
                return;
            case EventIgnore _:
                EmitLine("{");
                EmitLine("}\n");
                return;
            default:
                throw new NotSupportedException($"Not supported default: {handler}");
        }
    }


    private void GenerateControlBlock(List<Machine> machines, List<PEvent> events)
    {
        EmitLine("control {");
        EmitLine("set_solver_option(\":Timeout\", 1000);");
        EmitLine("induction(1);");

        foreach (var m in machines)
        {
            foreach (var s in m.States)
            {
                if (s.Entry is not null)
                {
                    EmitLine($"verify({m.Name}_{s.Name});");
                }

                foreach (var e in events.Where(e => !e.IsNullEvent && s.HasHandler(e)))
                {
                    EmitLine($"verify({m.Name}_{s.Name}_{e.Name});");
                }
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

    private void GenerateStmt(IPStmt stmt)
    {
        switch (stmt)
        {
            case CompoundStmt cstmt:
                foreach (var s in cstmt.Statements) GenerateStmt(s);

                return;
            case AssignStmt { Value: FunCallExpr } cstmt:
                var call = cstmt.Value as FunCallExpr;
                switch (cstmt.Location)
                {
                    case VariableAccessExpr vax:
                        if (call == null) return;
                        var v = GetLocalName(vax.Variable);
                        var f = call.Function.Name;
                        var fargs = call.Arguments.Select(ExprToString);
                        if (call.Function.Owner is not null)
                        {
                            fargs = fargs.Prepend("this");
                        }
                        EmitLine($"call ({v}) = {f}({string.Join(", ", fargs)});");

                        return;
                }

                throw new NotSupportedException(
                    $"Not supported assignment expression with call: {cstmt.Location} = {cstmt.Value} ({GetLocation(cstmt)})");
            case AssignStmt astmt:
                switch (astmt.Location)
                {
                    case VariableAccessExpr vax:
                        EmitLine($"{GetLocalName(vax.Variable)} = {ExprToString(astmt.Value)};");
                        return;
                    case MapAccessExpr max:
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
                GenerateStmt(ifstmt.ThenBranch);
                EmitLine("} else {");
                GenerateStmt(ifstmt.ElseBranch);
                EmitLine("}");
                return;
            case AssertStmt astmt:
                EmitLine($"// {((StringExpr)astmt.Message).BaseString}");
                EmitLine($"assert({ExprToString(astmt.Assertion)});");
                return;
            case PrintStmt { Message: StringExpr } pstmt:
                EmitLine($"// {((StringExpr)pstmt.Message).BaseString}");
                return;
            case FunCallStmt fapp:
                EmitLine(
                    $"call {fapp.Function.Name}({string.Join(", ", fapp.ArgsList.Select(ExprToString).Prepend("this"))});");
                return;
            case AddStmt astmt:
                var aset = ExprToString(astmt.Variable);
                var akey = ExprToString(astmt.Value);
                EmitLine($"{aset} = {aset}[{akey} -> true];");
                return;
            case RemoveStmt rstmt:
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
            case InsertStmt istmt:
                var imap = ExprToString(istmt.Variable);
                var idx = ExprToString(istmt.Index);
                var value = OptionConstructSome(istmt.Value.Type, ExprToString(istmt.Value));
                EmitLine($"{imap} = {imap}[{idx} -> {value}];");
                return;
            case WhileStmt wstmt:
                var wcond = ExprToString(wstmt.Condition);
                EmitLine($"while ({wcond}) {{");
                GenerateStmt(wstmt.Body);
                EmitLine("}");
                return;
            case ForeachStmt fstmt:
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
                        EmitLine($"while ({checker} != {collection}) {{");
                        // assume that the item is in the set but hasn't been visited
                        EmitLine($"assume ({collection}[{item}] && !{checker}[{item}]);");
                        // the body of the loop
                        GenerateStmt(fstmt.Body);
                        // update the checker
                        EmitLine($"{checker} = {checker}[{item} -> true];");
                        // havoc the item
                        EmitLine($"havoc {item};");
                        EmitLine("}");
                        return;
                    default:
                        throw new NotSupportedException(
                            $"Foreach over non-sets is not supported yet: {fstmt} ({GetLocation(fstmt)}");
                }
            case GotoStmt gstmt:
                var gaction = EventOrGotoAdtConstructGoto(gstmt.State, gstmt.Payload);
                var glabel = LabelAdtConstruct("this", gaction);
                var glabels = StateAdtSelectBuffer(StateVar);
                EmitLine($"{glabels} = {glabels}[{glabel} -> true];");
                return;
            case SendStmt sstmt:
                if (sstmt.Arguments.Count > 1)
                {
                    throw new NotSupportedException("We only support at most one argument to a send");
                }

                var ev = ((EventRefExpr)sstmt.Evt).Value;
                var saction = EventOrGotoAdtConstructEvent(ev, sstmt.Arguments[0]);
                var slabel = LabelAdtConstruct(ExprToString(sstmt.MachineExpr), saction);
                var slabels = StateAdtSelectBuffer(StateVar);
                EmitLine($"{slabels} = {slabels}[{slabel} -> true];");
                EmitLine($"{LocalPrefix}stage = true;");
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
                $"{ExprToString(bexpr.Lhs)} {BinOpToString(bexpr.Operation)} {ExprToString(bexpr.Rhs)}",
            UnaryOpExpr uexpr => $"{UnaryOpToString(uexpr.Operation)} {ExprToString(uexpr.SubExpr)}",
            ThisRefExpr => "this",
            EnumElemRefExpr e => $"{UserPrefix}{e.Value.Name}",
            NamedTupleExpr t => NamedTupleExprHelper(t),
            StringExpr s =>
                $"\"{s.BaseString}\" {(s.Args.Count != 0 ? "%" : "")} {string.Join(", ", s.Args.Select(ExprToString))}",
            MapAccessExpr maex =>
                OptionSelectValue(((MapType)maex.MapExpr.Type).ValueType,
                    $"{ExprToString(maex.MapExpr)}[{ExprToString(maex.IndexExpr)}]"),
            ContainsExpr cexp => cexp.Collection.Type switch
            {
                MapType mapType => OptionIsSome(mapType.ValueType,
                    $"{ExprToString(cexp.Collection)}[{ExprToString(cexp.Item)}]"),
                SetType _ => $"{ExprToString(cexp.Collection)}[{ExprToString(cexp.Item)}]",
                _ => throw new NotSupportedException(
                    $"Not supported expr: {expr} of {cexp.Type.OriginalRepresentation}")
            },
            DefaultExpr dexp => DefaultValue(dexp.Type),
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
                return PNull;
            case PrimitiveType pt when pt.Equals(PrimitiveType.Machine):
                return MachineRefT;
            case PrimitiveType pt when pt.Equals(PrimitiveType.Event):
                return EventAdt;
            case TypeDefType tdt:
                return $"{UserPrefix}{tdt.TypeDefDecl.Name}";
            case PermissionType _:
                return MachineRefT;
            case EnumType et:
                return $"{UserPrefix}{et.EnumDecl.Name}";
            case SetType st:
                return $"[{TypeToString(st.ElementType)}]boolean";
            case MapType mt:
                this._optionsToDeclare.Add(mt.ValueType);
                return $"[{TypeToString(mt.KeyType)}]{GetOptionName(mt.ValueType)}";
        }

        throw new NotSupportedException($"Not supported type expression: {t} ({t.OriginalRepresentation})");
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


    private string GetLocation(IPAST node)
    {
        return _ctx.LocationResolver.GetLocation(node.SourceLocation).ToString();
    }
}