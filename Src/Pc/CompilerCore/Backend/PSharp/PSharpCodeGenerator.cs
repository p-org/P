using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Plang.Compiler.Backend.ASTExt;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.AST.Statements;
using Plang.Compiler.TypeChecker.AST.States;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.PSharp
{
    public class PSharpCodeGenerator : ICodeGenerator
    {
        public IEnumerable<CompiledFile> GenerateCode(ICompilationJob job, Scope globalScope)
        {
            var context = new CompilationContext(job);
            var csharpSource = GenerateSource(context, globalScope);
            return new List<CompiledFile> {csharpSource};
        }

        private CompiledFile GenerateSource(CompilationContext context, Scope globalScope)
        {
            var source = new CompiledFile(context.FileName);

            WriteSourcePrologue(context, source.Stream);

            // write the top level declarations
            foreach (var decl in globalScope.AllDecls) WriteDecl(context, source.Stream, decl);

            // write the interface declarations 
            WriteInitializeInterfaces(context, source.Stream, globalScope.Interfaces);

            // write the enum declarations
            WriteInitializeEnums(context, source.Stream, globalScope.Enums);

            WriteSourceEpilogue(context, source.Stream);

            return source;
        }

        private void WriteInitializeInterfaces(CompilationContext context, StringWriter output,
            IEnumerable<Interface> interfaces)
        {
            //create the interface declarations
            var ifaces = interfaces.ToList();
            foreach (var iface in ifaces)
            {
                context.WriteLine(output, $"public class {context.Names.GetNameForDecl(iface)} : PMachineValue {{");
                context.WriteLine(output,
                    $"public {context.Names.GetNameForDecl(iface)} (MachineId machine, List<string> permissions) : base(machine, permissions) {{ }}");
                context.WriteLine(output, "}");
                context.WriteLine(output);
            }

            //initialize the interfaces
            context.WriteLine(output, "public partial class PHelper {");
            context.WriteLine(output, "public static void InitializeInterfaces() {");
            context.WriteLine(output, "PInterfaces.Clear();");
            foreach (var iface in ifaces)
            {
                context.Write(output, $"PInterfaces.AddInterface(nameof({context.Names.GetNameForDecl(iface)})");
                foreach (var ev in iface.ReceivableEvents.Events)
                {
                    context.Write(output, ", ");
                    context.Write(output, $"nameof({context.Names.GetNameForDecl(ev)})");
                }

                context.WriteLine(output, ");");
            }

            context.WriteLine(output, "}");
            context.WriteLine(output, "}");
            context.WriteLine(output);
        }

        private void WriteSourcePrologue(CompilationContext context, StringWriter output)
        {
            context.WriteLine(output, "using Microsoft.PSharp;");
            context.WriteLine(output, "using System;");
            context.WriteLine(output, "using System.Runtime;");
            context.WriteLine(output, "using System.Collections.Generic;");
            context.WriteLine(output, "using System.Linq;");
            context.WriteLine(output, "using System.IO;");
            context.WriteLine(output, "using Plang.PrtSharp;");
            context.WriteLine(output, "using Plang.PrtSharp.Values;");
            context.WriteLine(output, "using Plang.PrtSharp.Exceptions;");
            context.WriteLine(output, "using System.Threading;");
            context.WriteLine(output, "using System.Threading.Tasks;");
            context.WriteLine(output);
            context.WriteLine(output, "#pragma warning disable 162, 219, 414");
            context.WriteLine(output, $"namespace {context.ProjectName}");
            context.WriteLine(output, "{");
            context.WriteLine(output, $"public static partial class {context.GlobalFunctionClassName} {{}}");
        }

        private void WriteSourceEpilogue(CompilationContext context, StringWriter output)
        {
            context.WriteLine(output, "}");
            context.WriteLine(output, "#pragma warning restore 162, 219, 414");
        }

        private void WriteDecl(CompilationContext context, StringWriter output, IPDecl decl)
        {
            string declName;
            switch (decl)
            {
                case Function function:
                    context.WriteLine(output, $"public static partial class {context.GlobalFunctionClassName}");
                    context.WriteLine(output, "{");
                    WriteFunction(context, output, function);
                    context.WriteLine(output, "}");
                    break;
                case PEvent pEvent:
                    if (!pEvent.IsBuiltIn) WriteEvent(context, output, pEvent);

                    break;
                case Machine machine:
                    if (machine.IsSpec)
                        WriteMonitor(context, output, machine);
                    else
                        WriteMachine(context, output, machine);

                    break;
                case PEnum _:
                    break;
                case TypeDef typeDef:
                    var foreignType = typeDef.Type as ForeignType;
                    if (foreignType != null) WriteForeignType(context, output, foreignType);

                    break;
                case Implementation impl:
                    WriteImplementationDecl(context, output, impl);
                    break;
                case SafetyTest safety:
                    WriteSafetyTestDecl(context, output, safety);
                    break;
                case Interface _:
                    break;
                case EnumElem _:
                    break;
                default:
                    declName = context.Names.GetNameForDecl(decl);
                    context.WriteLine(output, $"// TODO: {decl.GetType().Name} {declName}");
                    break;
            }
        }

        private void WriteMonitor(CompilationContext context, StringWriter output, Machine machine)
        {
            var declName = context.Names.GetNameForDecl(machine);
            context.WriteLine(output, $"internal partial class {declName} : PMonitor");
            context.WriteLine(output, "{");

            foreach (var field in machine.Fields)
                context.WriteLine(output,
                    $"private {GetCSharpType(field.Type)} {context.Names.GetNameForDecl(field)} = {GetDefaultValue(field.Type)};");

            WriteMonitorConstructor(context, output, machine);

            foreach (var method in machine.Methods) WriteFunction(context, output, method);

            foreach (var state in machine.States) WriteState(context, output, state);

            context.WriteLine(output, "}");
        }

        private void WriteMonitorConstructor(CompilationContext context, StringWriter output, Machine machine)
        {
            var declName = context.Names.GetNameForDecl(machine);
            context.WriteLine(output, $"static {declName}() {{");
            foreach (var sEvent in machine.Observes.Events)
                context.WriteLine(output, $"observes.Add(nameof({context.Names.GetNameForDecl(sEvent)}));");

            context.WriteLine(output, "}");
            context.WriteLine(output);
        }

        private static void WriteForeignType(CompilationContext context, StringWriter output, ForeignType foreignType)
        {
            var declName = foreignType.CanonicalRepresentation;
            context.WriteLine(output, $"public partial class {declName} : IPrtValue");
            context.WriteLine(output, "{");
            context.WriteLine(output, "}");
        }

        private void WriteSafetyTestDecl(CompilationContext context, StringWriter output, SafetyTest safety)
        {
            context.WriteLine(output, $"public class {context.Names.GetNameForDecl(safety)} {{");
            WriteInitializeLinkMap(context, output, safety.ModExpr.ModuleInfo.LinkMap);
            WriteInitializeInterfaceDefMap(context, output, safety.ModExpr.ModuleInfo.InterfaceDef);
            WriteInitializeMonitorObserves(context, output, safety.ModExpr.ModuleInfo.MonitorMap.Keys);
            WriteInitializeMonitorMap(context, output, safety.ModExpr.ModuleInfo.MonitorMap);
            WriteTestFunction(context, output, safety.Main);
            context.WriteLine(output, "}");
        }

        private void WriteImplementationDecl(CompilationContext context, StringWriter output, Implementation impl)
        {
            context.WriteLine(output, $"public class {context.Names.GetNameForDecl(impl)} {{");
            WriteInitializeLinkMap(context, output, impl.ModExpr.ModuleInfo.LinkMap);
            WriteInitializeInterfaceDefMap(context, output, impl.ModExpr.ModuleInfo.InterfaceDef);
            WriteInitializeMonitorObserves(context, output, impl.ModExpr.ModuleInfo.MonitorMap.Keys);
            WriteInitializeMonitorMap(context, output, impl.ModExpr.ModuleInfo.MonitorMap);
            WriteTestFunction(context, output, impl.Main);
            context.WriteLine(output, "}");
        }

        private void WriteInitializeMonitorObserves(CompilationContext context, StringWriter output,
            ICollection<Machine> monitors)
        {
            context.WriteLine(output, "public static void InitializeMonitorObserves() {");
            context.WriteLine(output, "PModule.monitorObserves.Clear();");
            foreach (var monitor in monitors)
            {
                context.WriteLine(output,
                    $"PModule.monitorObserves[nameof({context.Names.GetNameForDecl(monitor)})] = new List<string>();");
                foreach (var ev in monitor.Observes.Events)
                    context.WriteLine(output,
                        $"PModule.monitorObserves[nameof({context.Names.GetNameForDecl(monitor)})].Add(nameof({context.Names.GetNameForDecl(ev)}));");
            }

            context.WriteLine(output, "}");
            context.WriteLine(output);
        }

        private void WriteInitializeEnums(CompilationContext context, StringWriter output, IEnumerable<PEnum> enums)
        {
            //initialize the interfaces
            context.WriteLine(output, "public partial class PHelper {");
            context.WriteLine(output, "public static void InitializeEnums() {");
            context.WriteLine(output, "PrtEnum.Clear();");
            foreach (var enumDecl in enums)
            {
                var enumElemNames =
                    $"new [] {{{string.Join(",", enumDecl.Values.Select(e => $"\"{context.Names.GetNameForDecl(e)}\""))}}}";
                var enumElemValues = $"new [] {{{string.Join(",", enumDecl.Values.Select(e => e.Value))}}}";
                context.WriteLine(output, $"PrtEnum.AddEnumElements({enumElemNames}, {enumElemValues});");
            }

            context.WriteLine(output, "}");
            context.WriteLine(output, "}");
            context.WriteLine(output);
        }

        private void WriteTestFunction(CompilationContext context, StringWriter output, string main)
        {
            context.WriteLine(output);
            context.WriteLine(output, "[Microsoft.PSharp.Test]");
            context.WriteLine(output, "public static void Execute(PSharpRuntime runtime) {");
            context.WriteLine(output, "runtime.SetLogger(new PLogger());");
            context.WriteLine(output, "PModule.runtime = runtime;");
            context.WriteLine(output, "PHelper.InitializeInterfaces();");
            context.WriteLine(output, "PHelper.InitializeEnums();");
            context.WriteLine(output, "InitializeLinkMap();");
            context.WriteLine(output, "InitializeInterfaceDefMap();");
            context.WriteLine(output, "InitializeMonitorMap(runtime);");
            context.WriteLine(output, "InitializeMonitorObserves();");
            context.WriteLine(output,
                $"runtime.CreateMachine(typeof(_GodMachine), new _GodMachine.Config(typeof({main})));");
            context.WriteLine(output, "}");
        }

        private void WriteInitializeMonitorMap(CompilationContext context, StringWriter output,
            IDictionary<Machine, IEnumerable<Interface>> monitorMap)
        {
            // compute the reverse map
            var machineMap = new Dictionary<string, List<Machine>>();
            foreach (var monitorToInterface in monitorMap)
            foreach (var iface in monitorToInterface.Value)
            {
                if (!machineMap.ContainsKey(iface.Name)) machineMap[iface.Name] = new List<Machine>();

                machineMap[iface.Name].Add(monitorToInterface.Key);
            }

            context.WriteLine(output, "public static void InitializeMonitorMap(PSharpRuntime runtime) {");
            context.WriteLine(output, "PModule.monitorMap.Clear();");
            foreach (var machine in machineMap)
            {
                context.WriteLine(output, $"PModule.monitorMap[\"{machine.Key}\"] = new List<Type>();");
                foreach (var monitor in machine.Value)
                    context.WriteLine(output,
                        $"PModule.monitorMap[\"{machine.Key}\"].Add(typeof({context.Names.GetNameForDecl(monitor)}));");
            }

            foreach (var monitor in monitorMap.Keys)
                context.WriteLine(output, $"runtime.RegisterMonitor(typeof({context.Names.GetNameForDecl(monitor)}));");

            context.WriteLine(output, "}");
            context.WriteLine(output);
        }


        private void WriteInitializeInterfaceDefMap(CompilationContext context, StringWriter output,
            IDictionary<Interface, Machine> interfaceDef)
        {
            context.WriteLine(output, "public static void InitializeInterfaceDefMap() {");
            context.WriteLine(output, "PModule.interfaceDefinitionMap.Clear();");
            foreach (var map in interfaceDef)
                context.WriteLine(output,
                    $"PModule.interfaceDefinitionMap.Add(nameof({context.Names.GetNameForDecl(map.Key)}), typeof({context.Names.GetNameForDecl(map.Value)}));");

            context.WriteLine(output, "}");
            context.WriteLine(output);
        }

        private void WriteInitializeLinkMap(CompilationContext context, StringWriter output,
            IDictionary<Interface, IDictionary<Interface, Interface>> linkMap)
        {
            context.WriteLine(output, "public static void InitializeLinkMap() {");
            context.WriteLine(output, "PModule.linkMap.Clear();");
            foreach (var creatorInterface in linkMap)
            {
                context.WriteLine(output,
                    $"PModule.linkMap[nameof({context.Names.GetNameForDecl(creatorInterface.Key)})] = new Dictionary<string, string>();");
                foreach (var clinkMap in creatorInterface.Value)
                    context.WriteLine(output,
                        $"PModule.linkMap[nameof({context.Names.GetNameForDecl(creatorInterface.Key)})].Add(nameof({context.Names.GetNameForDecl(clinkMap.Key)}), nameof({context.Names.GetNameForDecl(clinkMap.Value)}));");
            }

            context.WriteLine(output, "}");
            context.WriteLine(output);
        }

        private void WriteEvent(CompilationContext context, StringWriter output, PEvent pEvent)
        {
            var declName = context.Names.GetNameForDecl(pEvent);

            // initialize the payload type
            var payloadType = GetCSharpType(pEvent.PayloadType, true);
            context.WriteLine(output, $"internal partial class {declName} : PEvent");
            context.WriteLine(output, "{");
            context.WriteLine(output,
                $"static {declName}() {{ AssertVal = {pEvent.Assert}; AssumeVal = {pEvent.Assume};}}");
            context.WriteLine(output, $"public {declName}() : base() {{}}");
            context.WriteLine(output, $"public {declName} ({payloadType} payload): base(payload)" + "{ }");
            context.WriteLine(output, $"public override IPrtValue Clone() {{ return new {declName}();}}");
            context.WriteLine(output, "}");
        }

        private void WriteMachine(CompilationContext context, StringWriter output, Machine machine)
        {
            var declName = context.Names.GetNameForDecl(machine);
            context.WriteLine(output, $"internal partial class {declName} : PMachine");
            context.WriteLine(output, "{");

            foreach (var field in machine.Fields)
                context.WriteLine(output,
                    $"private {GetCSharpType(field.Type)} {context.Names.GetNameForDecl(field)} = {GetDefaultValue(field.Type)};");

            //create the constructor event
            var cTorType = GetCSharpType(machine.PayloadType, true);
            context.Write(output, "public class ConstructorEvent : PEvent");
            context.Write(output, "{");
            context.Write(output, $"public ConstructorEvent({cTorType} val) : base(val) {{ }}");
            context.WriteLine(output, "}");
            context.WriteLine(output);

            context.WriteLine(output,
                $"protected override Event GetConstructorEvent(IPrtValue value) {{ return new ConstructorEvent(({cTorType})value); }}");

            // create the constructor to initialize the sends, creates and receives list
            WriteMachineConstructor(context, output, machine);

            foreach (var method in machine.Methods) WriteFunction(context, output, method);

            foreach (var state in machine.States) WriteState(context, output, state);

            context.WriteLine(output, "}");
        }

        private static void WriteMachineConstructor(CompilationContext context, StringWriter output, Machine machine)
        {
            var declName = context.Names.GetNameForDecl(machine);
            context.WriteLine(output, $"public {declName}() {{");
            foreach (var sEvent in machine.Sends.Events)
                context.WriteLine(output, $"this.sends.Add(nameof({context.Names.GetNameForDecl(sEvent)}));");

            foreach (var rEvent in machine.Receives.Events)
                context.WriteLine(output, $"this.receives.Add(nameof({context.Names.GetNameForDecl(rEvent)}));");

            foreach (var iCreate in machine.Creates.Interfaces)
                context.WriteLine(output, $"this.creates.Add(nameof({context.Names.GetNameForDecl(iCreate)}));");

            context.WriteLine(output, "}");
            context.WriteLine(output);
        }

        private void WriteState(CompilationContext context, StringWriter output, State state)
        {
            if (state.IsStart && !state.OwningMachine.IsSpec)
            {
                context.WriteLine(output, "[Start]");
                context.WriteLine(output, "[OnEntry(nameof(InitializeParametersFunction))]");
                context.WriteLine(output,
                    $"[OnEventGotoState(typeof(ConstructorEvent), typeof({context.Names.GetNameForDecl(state)}))]");
                context.WriteLine(output, "class __InitState__ : MachineState { }");
                context.WriteLine(output);
            }

            if (state.IsStart && state.OwningMachine.IsSpec) context.WriteLine(output, "[Start]");

            if (state.OwningMachine.IsSpec)
            {
                if (state.Temperature == StateTemperature.Cold)
                    context.WriteLine(output, "[Cold]");
                else if (state.Temperature == StateTemperature.Hot) context.WriteLine(output, "[Hot]");
            }

            if (state.Entry != null)
                context.WriteLine(output, $"[OnEntry(nameof({context.Names.GetNameForDecl(state.Entry)}))]");

            var deferredEvents = new List<string>();
            var ignoredEvents = new List<string>();
            foreach (var eventHandler in state.AllEventHandlers)
            {
                var pEvent = eventHandler.Key;
                var stateAction = eventHandler.Value;
                switch (stateAction)
                {
                    case EventDefer _:
                        deferredEvents.Add($"typeof({context.Names.GetNameForDecl(pEvent)})");
                        break;
                    case EventDoAction eventDoAction:
                        context.WriteLine(
                            output,
                            $"[OnEventDoAction(typeof({context.Names.GetNameForDecl(pEvent)}), nameof({context.Names.GetNameForDecl(eventDoAction.Target)}))]");
                        break;
                    case EventGotoState eventGotoState when eventGotoState.TransitionFunction == null:
                        context.WriteLine(
                            output,
                            $"[OnEventGotoState(typeof({context.Names.GetNameForDecl(pEvent)}), typeof({context.Names.GetNameForDecl(eventGotoState.Target)}))]");
                        break;
                    case EventGotoState eventGotoState when eventGotoState.TransitionFunction != null:
                        context.WriteLine(
                            output,
                            $"[OnEventGotoState(typeof({context.Names.GetNameForDecl(pEvent)}), typeof({context.Names.GetNameForDecl(eventGotoState.Target)}), nameof({context.Names.GetNameForDecl(eventGotoState.TransitionFunction)}))]");
                        break;
                    case EventIgnore _:
                        ignoredEvents.Add($"typeof({context.Names.GetNameForDecl(pEvent)})");
                        break;
                    case EventPushState eventPushState:
                        context.WriteLine(
                            output,
                            $"[OnEventPushState(typeof({context.Names.GetNameForDecl(pEvent)}), typeof({context.Names.GetNameForDecl(eventPushState.Target)}))]");
                        break;
                }
            }

            if (deferredEvents.Count > 0)
                context.WriteLine(output, $"[DeferEvents({string.Join(", ", deferredEvents.AsEnumerable())})]");

            if (ignoredEvents.Count > 0)
                context.WriteLine(output, $"[IgnoreEvents({string.Join(", ", ignoredEvents.AsEnumerable())})]");

            if (state.Exit != null)
                context.WriteLine(output, $"[OnExit(nameof({context.Names.GetNameForDecl(state.Exit)}))]");

            var stateType = state.OwningMachine.IsSpec ? "MonitorState" : "MachineState";
            context.WriteLine(output, $"class {context.Names.GetNameForDecl(state)} : {stateType}");
            context.WriteLine(output, "{");
            context.WriteLine(output, "}");
        }

        private void WriteFunction(CompilationContext context, StringWriter output, Function function)
        {
            if (function.IsForeign) return;

            var isStatic = function.Owner == null;
            var isAsync = function.CanReceive == true;
            var signature = function.Signature;

            var staticKeyword = isStatic ? "static " : "";
            var asyncKeyword = isAsync ? "async " : "";
            var returnType = GetCSharpType(signature.ReturnType);

            if (isAsync) returnType = returnType == "void" ? "Task" : $"Task<{returnType}>";

            var functionName = context.Names.GetNameForDecl(function);
            var functionParameters = "";
            if (!function.IsAnon)
                functionParameters = string.Join(
                    ", ",
                    signature.Parameters.Select(param =>
                        $"{GetCSharpType(param.Type)} {context.Names.GetNameForDecl(param)}"));

            if (isStatic)
            {
                var seperator = functionParameters == "" ? "" : ", ";
                functionParameters += string.Concat(seperator, "PMachine currentMachine");
            }

            context.WriteLine(output,
                $"public {staticKeyword}{asyncKeyword}{returnType} {functionName}({functionParameters})");
            WriteFunctionBody(context, output, function);
        }

        private void WriteFunctionBody(CompilationContext context, StringWriter output, Function function)
        {
            context.WriteLine(output, "{");

            //add the declaration of currentMachine
            if (function.Owner != null)
                context.WriteLine(output, $"{context.Names.GetNameForDecl(function.Owner)} currentMachine = this;");

            if (function.IsAnon)
                if (function.Signature.Parameters.Any())
                {
                    var param = function.Signature.Parameters.First();
                    context.WriteLine(output,
                        $"{GetCSharpType(param.Type)} {context.Names.GetNameForDecl(param)} = ({GetCSharpType(param.Type)})(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);");
                    context.WriteLine(output, "this.gotoPayload = null;");
                }

            foreach (var local in function.LocalVariables)
            {
                var type = local.Type;
                context.WriteLine(output,
                    $"{GetCSharpType(type, true)} {context.Names.GetNameForDecl(local)} = {GetDefaultValue(type)};");
            }

            foreach (var bodyStatement in function.Body.Statements) WriteStmt(context, output, bodyStatement);

            context.WriteLine(output, "}");
        }

        private void WriteStmt(CompilationContext context, StringWriter output, IPStmt stmt)
        {
            switch (stmt)
            {
                case AnnounceStmt announceStmt:
                    context.Write(output, "currentMachine.Announce((Event)");
                    WriteExpr(context, output, announceStmt.PEvent);
                    if (announceStmt.Payload != null)
                    {
                        context.Write(output, ", ");
                        WriteExpr(context, output, announceStmt.Payload);
                    }

                    context.WriteLine(output, ");");
                    break;
                case AssertStmt assertStmt:
                    context.Write(output, "currentMachine.Assert(");
                    WriteExpr(context, output, assertStmt.Assertion);
                    context.Write(output, ",");
                    context.Write(output, $"\"{assertStmt.Message}\"");
                    context.WriteLine(output, ");");
                    //last statement
                    if (FunctionValidator.SurelyReturns(assertStmt))
                        context.WriteLine(output, "throw new PUnreachableCodeException();");

                    break;
                case AssignStmt assignStmt:
                    var needCtorAdapter = !assignStmt.Value.Type.IsSameTypeAs(assignStmt.Location.Type)
                                          && !PrimitiveType.Null.IsSameTypeAs(assignStmt.Value.Type)
                                          && !PrimitiveType.Any.IsSameTypeAs(assignStmt.Location.Type);
                    WriteLValue(context, output, assignStmt.Location);
                    context.Write(output, $" = ({GetCSharpType(assignStmt.Location.Type)})(");
                    if (needCtorAdapter)
                        context.Write(output, $"new {GetCSharpType(assignStmt.Location.Type)}(");

                    WriteExpr(context, output, assignStmt.Value);
                    if (needCtorAdapter)
                    {
                        if (assignStmt.Location.Type.Canonicalize() is SequenceType seqType)
                            context.Write(output, $".Cast<{GetCSharpType(seqType.ElementType)}>()");

                        context.Write(output, ")");
                    }

                    context.WriteLine(output, ");");
                    break;
                case CompoundStmt compoundStmt:
                    context.WriteLine(output, "{");
                    foreach (var subStmt in compoundStmt.Statements) WriteStmt(context, output, subStmt);

                    context.WriteLine(output, "}");
                    break;
                case CtorStmt ctorStmt:
                    context.Write(output,
                        $"currentMachine.CreateInterface<{context.Names.GetNameForDecl(ctorStmt.Interface)}>(");
                    context.Write(output, "currentMachine");
                    if (ctorStmt.Arguments.Any())
                    {
                        context.Write(output, ", ");
                        if (ctorStmt.Arguments.Count > 1)
                        {
                            //create tuple from rvaluelist
                            context.Write(output, "new PrtTuple(");
                            var septor = "";
                            foreach (var ctorExprArgument in ctorStmt.Arguments)
                            {
                                context.Write(output, septor);
                                WriteExpr(context, output, ctorExprArgument);
                                septor = ",";
                            }

                            context.Write(output, ")");
                        }
                        else
                        {
                            WriteExpr(context, output, ctorStmt.Arguments.First());
                        }
                    }

                    context.WriteLine(output, ");");
                    break;
                case FunCallStmt funCallStmt:
                    var isStatic = funCallStmt.Function.Owner == null;
                    var awaitMethod = funCallStmt.Function.CanReceive == true ? "await " : "";
                    var globalFunctionClass = isStatic ? $"{context.GlobalFunctionClassName}." : "";
                    context.Write(output,
                        $"{awaitMethod}{globalFunctionClass}{context.Names.GetNameForDecl(funCallStmt.Function)}(");
                    var separator = "";

                    foreach (var param in funCallStmt.ArgsList)
                    {
                        context.Write(output, separator);
                        WriteExpr(context, output, param);
                        separator = ", ";
                    }

                    if (isStatic) context.Write(output, separator + "this");

                    context.WriteLine(output, ");");
                    break;
                case GotoStmt gotoStmt:
                    context.Write(output, $"currentMachine.GotoState<{gotoStmt.State.QualifiedName}>(");
                    if (gotoStmt.Payload != null) WriteExpr(context, output, gotoStmt.Payload);

                    context.WriteLine(output, ");");
                    //last statement
                    context.WriteLine(output, "throw new PUnreachableCodeException();");
                    break;
                case IfStmt ifStmt:
                    context.Write(output, "if (");
                    WriteExpr(context, output, ifStmt.Condition);
                    context.WriteLine(output, ")");
                    WriteStmt(context, output, ifStmt.ThenBranch);
                    if (ifStmt.ElseBranch != null && ifStmt.ElseBranch.Statements.Any())
                    {
                        context.WriteLine(output, "else");
                        WriteStmt(context, output, ifStmt.ElseBranch);
                    }

                    break;
                case InsertStmt insertStmt:
                    var isMap = PLanguageType.TypeIsOfKind(insertStmt.Variable.Type, TypeKind.Map);
                    var castOp = isMap ? "(PrtMap)" : "(PrtSeq)";
                    context.Write(output, $"({castOp}");
                    WriteExpr(context, output, insertStmt.Variable);
                    if (isMap)
                        context.Write(output, ").Add(");
                    else
                        context.Write(output, ").Insert(");

                    WriteExpr(context, output, insertStmt.Index);
                    context.Write(output, ", ");
                    WriteExpr(context, output, insertStmt.Value);
                    context.WriteLine(output, ");");
                    break;
                case MoveAssignStmt moveAssignStmt:
                    var upCast = "";
                    if (!moveAssignStmt.FromVariable.Type.IsSameTypeAs(moveAssignStmt.ToLocation.Type))
                    {
                        upCast = $"({GetCSharpType(moveAssignStmt.ToLocation.Type)})";
                        if (PLanguageType.TypeIsOfKind(moveAssignStmt.FromVariable.Type, TypeKind.Enum))
                            upCast = $"{upCast}(long)";
                    }

                    WriteLValue(context, output, moveAssignStmt.ToLocation);
                    context.WriteLine(output,
                        $" = {upCast}{context.Names.GetNameForDecl(moveAssignStmt.FromVariable)};");
                    break;
                case NoStmt _:
                    break;
                case PopStmt _:
                    context.WriteLine(output, "currentMachine.PopState();");
                    //last statement
                    context.WriteLine(output, "throw new PUnreachableCodeException();");
                    break;
                case PrintStmt printStmt:
                    context.Write(output, $"PModule.runtime.Logger.WriteLine(\"{printStmt.Message}\"");
                    foreach (var printArg in printStmt.Args)
                    {
                        context.Write(output, ", ");
                        WriteExpr(context, output, printArg);
                    }

                    context.WriteLine(output, ");");
                    break;
                case RaiseStmt raiseStmt:
                    context.Write(output, "currentMachine.RaiseEvent((Event)");
                    WriteExpr(context, output, raiseStmt.PEvent);
                    if (raiseStmt.Payload.Any())
                    {
                        context.Write(output, ", ");
                        WriteExpr(context, output, raiseStmt.Payload.First());
                    }

                    context.WriteLine(output, ");");
                    //last statement
                    context.WriteLine(output, "throw new PUnreachableCodeException();");
                    break;
                case ReceiveStmt receiveStmt:
                    var eventName = context.Names.GetTemporaryName("recvEvent");
                    var eventTypeNames = receiveStmt.Cases.Keys.Select(evt => context.Names.GetNameForDecl(evt))
                        .ToArray();
                    var recvArgs = string.Join(", ", eventTypeNames.Select(name => $"typeof({name})"));
                    context.WriteLine(output, $"var {eventName} = await currentMachine.ReceiveEvent({recvArgs});");
                    context.WriteLine(output, $"switch ({eventName}) {{");
                    foreach (var recvCase in receiveStmt.Cases)
                    {
                        var caseName = context.Names.GetTemporaryName("evt");
                        context.WriteLine(output, $"case {context.Names.GetNameForDecl(recvCase.Key)} {caseName}: {{");
                        if (recvCase.Value.Signature.Parameters.FirstOrDefault() is Variable caseArg)
                            context.WriteLine(output,
                                $"{GetCSharpType(caseArg.Type)} {context.Names.GetNameForDecl(caseArg)} = ({GetCSharpType(caseArg.Type)})({caseName}.Payload);");

                        foreach (var local in recvCase.Value.LocalVariables)
                        {
                            var type = local.Type;
                            context.WriteLine(output,
                                $"{GetCSharpType(type, true)} {context.Names.GetNameForDecl(local)} = {GetDefaultValue(type)};");
                        }

                        foreach (var caseStmt in recvCase.Value.Body.Statements) WriteStmt(context, output, caseStmt);

                        context.WriteLine(output, "} break;");
                    }

                    context.WriteLine(output, "}");
                    break;
                case RemoveStmt removeStmt:
                {
                    var castOperation = PLanguageType.TypeIsOfKind(removeStmt.Variable.Type, TypeKind.Map)
                        ? "(PrtMap)"
                        : "(PrtSeq)";
                    context.Write(output, $"({castOperation}");
                    switch (removeStmt.Variable.Type.Canonicalize())
                    {
                        case MapType _:
                            WriteExpr(context, output, removeStmt.Variable);
                            context.Write(output, ").Remove(");
                            WriteExpr(context, output, removeStmt.Value);
                            context.WriteLine(output, ");");
                            break;
                        case SequenceType _:
                            WriteExpr(context, output, removeStmt.Variable);
                            context.Write(output, ").RemoveAt(");
                            WriteExpr(context, output, removeStmt.Value);
                            context.WriteLine(output, ");");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(
                                $"Remove cannot be applied to type {removeStmt.Variable.Type.OriginalRepresentation}");
                    }
                }
                    break;
                case ReturnStmt returnStmt:
                    context.Write(output, "return ");
                    if (returnStmt.ReturnValue != null) WriteExpr(context, output, returnStmt.ReturnValue);

                    context.WriteLine(output, ";");
                    break;
                case SendStmt sendStmt:
                    context.Write(output, "currentMachine.SendEvent(");
                    WriteExpr(context, output, sendStmt.MachineExpr);
                    context.Write(output, ", (Event)");
                    WriteExpr(context, output, sendStmt.Evt);

                    if (sendStmt.Arguments.Any())
                    {
                        context.Write(output, ", ");
                        if (sendStmt.Arguments.Count > 1)
                        {
                            //create tuple from rvaluelist
                            var argTypes = string.Join(",",
                                sendStmt.Arguments.Select(a => GetCSharpType(a.Type)));
                            var tupleType = $"PrtTuple";
                            context.Write(output, $"new {tupleType}(");
                            var septor = "";
                            foreach (var ctorExprArgument in sendStmt.Arguments)
                            {
                                context.Write(output, septor);
                                WriteExpr(context, output, ctorExprArgument);
                                septor = ",";
                            }

                            context.Write(output, ")");
                        }
                        else
                        {
                            WriteExpr(context, output, sendStmt.Arguments.First());
                        }
                    }

                    context.WriteLine(output, ");");
                    break;
                case SwapAssignStmt _:
                    throw new NotImplementedException("swap assignments");
                case WhileStmt whileStmt:
                    context.Write(output, "while (");
                    WriteExpr(context, output, whileStmt.Condition);
                    context.WriteLine(output, ")");
                    WriteStmt(context, output, whileStmt.Body);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stmt));
            }
        }

        private void WriteLValue(CompilationContext context, StringWriter output, IPExpr lvalue)
        {
            switch (lvalue)
            {
                case MapAccessExpr mapAccessExpr:
                    context.Write(output, "((PrtMap)");
                    WriteLValue(context, output, mapAccessExpr.MapExpr);
                    context.Write(output, ")[");
                    WriteExpr(context, output, mapAccessExpr.IndexExpr);
                    context.Write(output, "]");
                    break;
                case NamedTupleAccessExpr namedTupleAccessExpr:
                    context.Write(output, "((PrtNamedTuple)");
                    WriteExpr(context, output, namedTupleAccessExpr.SubExpr);
                    context.Write(output, $")[\"{namedTupleAccessExpr.FieldName}\"]");
                    break;
                case SeqAccessExpr seqAccessExpr:
                    context.Write(output, "((PrtSeq)");
                    WriteLValue(context, output, seqAccessExpr.SeqExpr);
                    context.Write(output, ")[");
                    WriteExpr(context, output, seqAccessExpr.IndexExpr);
                    context.Write(output, "]");
                    break;
                case TupleAccessExpr tupleAccessExpr:
                    context.Write(output, "((PrtTuple)");
                    WriteExpr(context, output, tupleAccessExpr.SubExpr);
                    context.Write(output, $")[{tupleAccessExpr.FieldNo}]");
                    break;
                case VariableAccessExpr variableAccessExpr:
                    context.Write(output, context.Names.GetNameForDecl(variableAccessExpr.Variable));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lvalue));
            }
        }

        private void WriteExpr(CompilationContext context, StringWriter output, IPExpr pExpr)
        {
            switch (pExpr)
            {
                case CloneExpr cloneExpr:
                    WriteClone(context, output, cloneExpr.Term);
                    break;
                case BinOpExpr binOpExpr:
                    //handle eq and noteq differently
                    if (binOpExpr.Operation == BinOpType.Eq || binOpExpr.Operation == BinOpType.Neq)
                    {
                        var negate = binOpExpr.Operation == BinOpType.Neq ? "!" : "";
                        context.Write(output, $"({negate}PrtValues.SafeEquals(");
                        if (PLanguageType.TypeIsOfKind(binOpExpr.Lhs.Type, TypeKind.Enum))
                        {
                            context.Write(output, "PrtValues.Box((long) ");
                            WriteExpr(context, output, binOpExpr.Lhs);
                            context.Write(output, "),");
                        }
                        else
                        {
                            WriteExpr(context, output, binOpExpr.Lhs);
                            context.Write(output, ",");
                        }


                        if (PLanguageType.TypeIsOfKind(binOpExpr.Rhs.Type, TypeKind.Enum))
                        {
                            context.Write(output, "PrtValues.Box((long) ");
                            WriteExpr(context, output, binOpExpr.Rhs);
                            context.Write(output, ")");
                        }
                        else
                        {
                            WriteExpr(context, output, binOpExpr.Rhs);
                        }

                        context.Write(output, "))");
                    }
                    else
                    {
                        context.Write(output, "(");
                        if (PLanguageType.TypeIsOfKind(binOpExpr.Lhs.Type, TypeKind.Enum))
                            context.Write(output, "(long)");

                        WriteExpr(context, output, binOpExpr.Lhs);
                        context.Write(output, $") {BinOpToStr(binOpExpr.Operation)} (");
                        if (PLanguageType.TypeIsOfKind(binOpExpr.Rhs.Type, TypeKind.Enum))
                            context.Write(output, "(long)");

                        WriteExpr(context, output, binOpExpr.Rhs);
                        context.Write(output, ")");
                    }

                    break;
                case BoolLiteralExpr boolLiteralExpr:
                    context.Write(output, $"((PrtBool){(boolLiteralExpr.Value ? "true" : "false")})");
                    break;
                case CastExpr castExpr:
                    context.Write(output, $"(({GetCSharpType(castExpr.Type)})");
                    WriteExpr(context, output, castExpr.SubExpr);
                    context.Write(output, ")");
                    break;
                case CoerceExpr coerceExpr:
                    switch (coerceExpr.Type.Canonicalize())
                    {
                        case PrimitiveType oldType when oldType.IsSameTypeAs(PrimitiveType.Float):
                        case PrimitiveType oldType1 when oldType1.IsSameTypeAs(PrimitiveType.Int):
                            context.Write(output, "(");
                            WriteExpr(context, output, coerceExpr.SubExpr);
                            context.Write(output, ")");
                            break;
                        case PermissionType _:
                            context.Write(output, "(PInterfaces.IsCoercionAllowed(");
                            WriteExpr(context, output, coerceExpr.SubExpr);
                            context.Write(output, ", ");
                            context.Write(output, $"\"I_{coerceExpr.NewType.CanonicalRepresentation}\") ?");
                            context.Write(output, "new PMachineValue(");
                            context.Write(output, "(");
                            WriteExpr(context, output, coerceExpr.SubExpr);
                            context.Write(output, ").Id, ");
                            context.Write(output,
                                $"PInterfaces.GetPermissions(\"I_{coerceExpr.NewType.CanonicalRepresentation}\")) : null)");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(
                                @"unexpected coercion operation to:" + coerceExpr.Type.CanonicalRepresentation);
                    }

                    break;
                case ContainsKeyExpr containsKeyExpr:
                    context.Write(output, "((PrtBool)(");
                    WriteExpr(context, output, containsKeyExpr.Map);
                    context.Write(output, ").ContainsKey(");
                    WriteExpr(context, output, containsKeyExpr.Key);
                    context.Write(output, "))");
                    break;
                case CtorExpr ctorExpr:
                    context.Write(output,
                        $"currentMachine.CreateInterface<{context.Names.GetNameForDecl(ctorExpr.Interface)}>( ");
                    context.Write(output, "currentMachine");
                    if (ctorExpr.Arguments.Any())
                    {
                        context.Write(output, ", ");
                        if (ctorExpr.Arguments.Count > 1)
                        {
                            //create tuple from rvaluelist
                            context.Write(output, "new PrtTuple(");
                            var septor = "";
                            foreach (var ctorExprArgument in ctorExpr.Arguments)
                            {
                                context.Write(output, septor);
                                WriteExpr(context, output, ctorExprArgument);
                                septor = ",";
                            }

                            context.Write(output, ")");
                        }
                        else
                        {
                            WriteExpr(context, output, ctorExpr.Arguments.First());
                        }
                    }

                    context.Write(output, ")");
                    break;
                case DefaultExpr defaultExpr:
                    context.Write(output, GetDefaultValue(defaultExpr.Type));
                    break;
                case EnumElemRefExpr enumElemRefExpr:
                    var enumElem = enumElemRefExpr.Value;
                    context.Write(output, $"(PrtEnum.Get(\"{context.Names.GetNameForDecl(enumElem)}\"))");
                    break;
                case EventRefExpr eventRefExpr:
                    var eventName = context.Names.GetNameForDecl(eventRefExpr.Value);
                    switch (eventName)
                    {
                        case "Halt":
                            context.Write(output, "new PHalt()");
                            break;
                        case "Default":
                            context.Write(output, "new Default()");
                            break;
                        default:
                            var payloadExpr = GetDefaultValue(eventRefExpr.Value.PayloadType);
                            context.Write(output, $"new {eventName}({payloadExpr})");
                            break;
                    }

                    break;
                case FairNondetExpr _:
                    context.Write(output, "((PrtBool)currentMachine.FairRandom())");
                    break;
                case FloatLiteralExpr floatLiteralExpr:
                    context.Write(output, $"((PrtFloat){floatLiteralExpr.Value})");
                    break;
                case FunCallExpr funCallExpr:
                    var isStatic = funCallExpr.Function.Owner == null;
                    var awaitMethod = funCallExpr.Function.CanReceive == true ? "await " : "";
                    var globalFunctionClass = isStatic ? $"{context.GlobalFunctionClassName}." : "";
                    context.Write(output,
                        $"{awaitMethod}{globalFunctionClass}{context.Names.GetNameForDecl(funCallExpr.Function)}(");
                    var separator = "";

                    foreach (var param in funCallExpr.Arguments)
                    {
                        context.Write(output, separator);
                        WriteExpr(context, output, param);
                        separator = ", ";
                    }

                    if (isStatic) context.Write(output, separator + "this");

                    context.Write(output, ")");
                    break;
                case IntLiteralExpr intLiteralExpr:
                    context.Write(output, $"((PrtInt){intLiteralExpr.Value})");
                    break;
                case KeysExpr keysExpr:
                    context.Write(output, "(");
                    WriteExpr(context, output, keysExpr.Expr);
                    context.Write(output, ").CloneKeys()");
                    break;
                case LinearAccessRefExpr linearAccessRefExpr:
                    var swapKeyword = linearAccessRefExpr.LinearType.Equals(LinearType.Swap) ? "ref " : "";
                    context.Write(output, $"{swapKeyword}{context.Names.GetNameForDecl(linearAccessRefExpr.Variable)}");
                    break;
                case NamedTupleExpr namedTupleExpr:
                    var fieldNamesArray = string.Join(",",
                        ((NamedTupleType) namedTupleExpr.Type).Names.Select(n => $"\"{n}\""));
                    fieldNamesArray = $"new string[]{{{fieldNamesArray}}}";
                    context.Write(output, $"(new {GetCSharpType(namedTupleExpr.Type)}({fieldNamesArray}, ");
                    for (var i = 0; i < namedTupleExpr.TupleFields.Count; i++)
                    {
                        if (i > 0) context.Write(output, ", ");

                        WriteExpr(context, output, namedTupleExpr.TupleFields[i]);
                    }

                    context.Write(output, "))");
                    break;
                case NondetExpr _:
                    context.Write(output, "((PrtBool)currentMachine.Random())");
                    break;
                case NullLiteralExpr _:
                    context.Write(output, "null");
                    break;
                case SizeofExpr sizeofExpr:
                    context.Write(output, "((PrtInt)(");
                    WriteExpr(context, output, sizeofExpr.Expr);
                    context.Write(output, ").Count)");
                    break;
                case ThisRefExpr _:
                    context.Write(output, "currentMachine.self");
                    break;
                case UnaryOpExpr unaryOpExpr:
                    context.Write(output, $"{UnOpToStr(unaryOpExpr.Operation)}(");
                    WriteExpr(context, output, unaryOpExpr.SubExpr);
                    context.Write(output, ")");
                    break;
                case UnnamedTupleExpr unnamedTupleExpr:
                    context.Write(output, $"new {GetCSharpType(unnamedTupleExpr.Type)}(");
                    var sep = "";
                    foreach (var field in unnamedTupleExpr.TupleFields)
                    {
                        context.Write(output, sep);
                        WriteExpr(context, output, field);
                        sep = ", ";
                    }

                    context.Write(output, ")");
                    break;
                case ValuesExpr valuesExpr:
                    context.Write(output, "(");
                    WriteExpr(context, output, valuesExpr.Expr);
                    context.Write(output, ").CloneValues()");
                    break;
                case MapAccessExpr _:
                case NamedTupleAccessExpr _:
                case SeqAccessExpr _:
                case TupleAccessExpr _:
                case VariableAccessExpr _:
                    WriteLValue(context, output, pExpr);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pExpr), $"type was {pExpr?.GetType().FullName}");
            }
        }

        private void WriteClone(CompilationContext context, StringWriter output, IExprTerm cloneExprTerm)
        {
            if (!(cloneExprTerm is IVariableRef variableRef))
            {
                WriteExpr(context, output, cloneExprTerm);
                return;
            }

            var varName = context.Names.GetNameForDecl(variableRef.Variable);
            context.Write(output, $"(({GetCSharpType(variableRef.Type)})((IPrtValue){varName})?.Clone())");
        }

        private string GetCSharpType(PLanguageType type, bool isVar = false)
        {
            switch (type.Canonicalize())
            {
                case DataType _:
                    return "IPrtValue";
                case EnumType _:
                    return "PrtInt";
                case ForeignType _:
                    return type.CanonicalRepresentation;
                case MapType _:
                    return "PrtMap";
                case NamedTupleType _:
                    return "PrtNamedTuple";
                case PermissionType _:
                    return "PMachineValue";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Any):
                    return "IPrtValue";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Bool):
                    return "PrtBool";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Int):
                    return "PrtInt";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Float):
                    return "PrtFloat";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Event):
                    return "PEvent";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Machine):
                    return "PMachineValue";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Null):
                    return isVar ? "IPrtValue" : "void";
                case SequenceType _:
                    return "PrtSeq";
                case TupleType _:
                    return "PrtTuple";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        private string GetDefaultValue(PLanguageType returnType)
        {
            switch (returnType.Canonicalize())
            {
                case EnumType _:
                    return "((PrtInt)0)";
                case MapType mapType:
                    return $"new {GetCSharpType(mapType)}()";
                case SequenceType sequenceType:
                    return $"new {GetCSharpType(sequenceType)}()";
                case NamedTupleType namedTupleType:
                    var fieldNamesArray = string.Join(",", namedTupleType.Names.Select(n => $"\"{n}\""));
                    fieldNamesArray = $"new string[]{{{fieldNamesArray}}}";
                    var fieldDefaults =
                        string.Join(", ", namedTupleType.Types.Select(t => GetDefaultValue(t)));
                    return $"(new {GetCSharpType(namedTupleType)}({fieldNamesArray},{fieldDefaults}))";
                case TupleType tupleType:
                    var defaultTupleValues =
                        string.Join(", ", tupleType.Types.Select(t => GetDefaultValue(t)));
                    return $"(new {GetCSharpType(tupleType)}({defaultTupleValues}))";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Bool):
                    return "((PrtBool)false)";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Int):
                    return "((PrtInt)0)";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Float):
                    return "((PrtFloat)0.0)";
                case PrimitiveType eventType when eventType.IsSameTypeAs(PrimitiveType.Event):
                case PermissionType _:
                case PrimitiveType anyType when anyType.IsSameTypeAs(PrimitiveType.Any):
                case PrimitiveType machineType when machineType.IsSameTypeAs(PrimitiveType.Machine):
                case ForeignType _:
                case PrimitiveType nullType when nullType.IsSameTypeAs(PrimitiveType.Null):
                case DataType _:
                    return "null";
                case null:
                    return "";
                default:
                    throw new ArgumentOutOfRangeException(nameof(returnType));
            }
        }

        private static string UnOpToStr(UnaryOpType operation)
        {
            switch (operation)
            {
                case UnaryOpType.Negate:
                    return "-";
                case UnaryOpType.Not:
                    return "!";
                default:
                    throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
            }
        }

        private static string BinOpToStr(BinOpType binOpType)
        {
            switch (binOpType)
            {
                case BinOpType.Add:
                    return "+";
                case BinOpType.Sub:
                    return "-";
                case BinOpType.Mul:
                    return "*";
                case BinOpType.Div:
                    return "/";
                case BinOpType.Lt:
                    return "<";
                case BinOpType.Le:
                    return "<=";
                case BinOpType.Gt:
                    return ">";
                case BinOpType.Ge:
                    return ">=";
                case BinOpType.And:
                    return "&&";
                case BinOpType.Or:
                    return "||";
                default:
                    throw new ArgumentOutOfRangeException(nameof(binOpType), binOpType, null);
            }
        }
    }
}