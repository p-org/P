using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Pc.Backend.ASTExt;
using Microsoft.Pc.TypeChecker;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.Expressions;
using Microsoft.Pc.TypeChecker.AST.Statements;
using Microsoft.Pc.TypeChecker.AST.States;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.Backend.PSharp
{
    public class PSharpCodeGenerator : ICodeGenerator
    {
        public IEnumerable<CompiledFile> GenerateCode(ICompilationJob job, Scope globalScope)
        {
            var context = new CompilationContext(job);
            CompiledFile csharpSource = GenerateSource(context, globalScope);
            return new List<CompiledFile> {csharpSource};
        }

        private CompiledFile GenerateSource(CompilationContext context, Scope globalScope)
        {
            var source = new CompiledFile(context.FileName);

            WriteSourcePrologue(context, source.Stream);

            // write the top level declarations
            foreach (IPDecl decl in globalScope.AllDecls)
            {
                WriteDecl(context, source.Stream, decl);
            }
            
            // write the interface declarations 
            WriteInitializeInterfaces(context, source.Stream, globalScope.Interfaces);

            // TODO: generate tuple type classes.

            WriteSourceEpilogue(context, source.Stream);

            return source;
        }

        private void WriteInitializeInterfaces(CompilationContext context, StringWriter output, IEnumerable<Interface> interfaces)
        {
            context.WriteLine(output, "public partial class PHelper {");
            context.WriteLine(output, "public static void InitializeInterfaces() {");
            foreach (var iface in interfaces)
            {
                context.Write(output, $"PInterfaces.AddInterface(\"{iface.Name}\"");
                foreach (PEvent ev in iface.ReceivableEvents.Events)
                {
                    context.Write(output, ", ");
                    context.Write(output, $"\"{ev.Name}\"");
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
            context.WriteLine(output, "using PrtSharp;");
            context.WriteLine(output, "using PrtSharp.Values;");
            context.WriteLine(output, "using System.Threading;");
            context.WriteLine(output, "using System.Threading.Tasks;");
            context.WriteLine(output);
            context.WriteLine(output, $"namespace {context.ProjectName}");
            context.WriteLine(output, "{");
            context.WriteLine(output, $"public static partial class {context.GlobalFunctionClassName} {{}}");
        }

        private void WriteSourceEpilogue(CompilationContext context, StringWriter output)
        {
            context.WriteLine(output, "}");
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
                    if (!pEvent.IsBuiltIn)
                    {
                        WriteEvent(context, output, pEvent);
                    }
                    break;
                case Machine machine:
                    if (machine.IsSpec)
                    {
                        WriteMonitor(context, output, machine);
                    }
                    else
                    {
                        WriteMachine(context, output, machine);
                    }
                    break;
                case PEnum pEnum:
                    WriteEnum(context, output, pEnum);
                    break;
                case Implementation impl:
                    WriteImplementationDecl(context, output, impl);
                    break;
                case SafetyTest safety:
                    WriteSafetyTestDecl(context, output, safety);
                    break;
                case Interface pInterface:
                    break;
                default:
                    declName = context.Names.GetNameForDecl(decl);
                    context.WriteLine(output, $"// TODO: {decl.GetType().Name} {declName}");
                    break;
            }
        }

        private void WriteMonitor(CompilationContext context, StringWriter output, Machine machine)
        {
            string declName = context.Names.GetNameForDecl(machine);
            context.WriteLine(output, $"internal class {declName} : PMonitor");
            context.WriteLine(output, "{");

            foreach (Variable field in machine.Fields)
            {
                context.WriteLine(output, $"private {GetCSharpType(context, field.Type)} {context.Names.GetNameForDecl(field)} = {GetDefaultValue(context, field.Type)};");
            }

            foreach (Function method in machine.Methods)
            {
                WriteFunction(context, output, method);
            }

            foreach (State state in machine.States)
            {
                WriteState(context, output, state);
            }
            context.WriteLine(output, "}");
        }

        private void WriteMonitorContructor(CompilationContext context, StringWriter output, Machine machine)
        {
            string declName = context.Names.GetNameForDecl(machine);
            context.WriteLine(output, $"static {declName}() {{");
            foreach (var sEvent in machine.Observes.Events)
            {
                context.WriteLine(output, $"observes.Add(\"{sEvent.Name}\");");
            }
            context.WriteLine(output, "}");
            context.WriteLine(output);
        }

        private static void WriteEnum(CompilationContext context, StringWriter output, PEnum pEnum)
        {
            var declName = context.Names.GetNameForDecl(pEnum);
            context.WriteLine(output, $"public enum {declName}");
            context.WriteLine(output, "{");
            foreach (EnumElem enumElem in pEnum.Values)
            {
                context.WriteLine(output, $"{enumElem.Name} = {enumElem.Value},");
            }

            context.WriteLine(output, "}");
        }

        private void WriteSafetyTestDecl(CompilationContext context, StringWriter output, SafetyTest safety)
        {
            context.WriteLine(output, $"public class {safety.Name} {{");
            WriteInitializeLinkMap(context, output, safety.ModExpr.ModuleInfo.LinkMap);
            WriteInitializeInterfaceDefMap(context, output, safety.ModExpr.ModuleInfo.InterfaceDef);
            WriteInitializeMonitorObserves(context, output, safety.ModExpr.ModuleInfo.MonitorMap.Keys);
            WriteInitializeMonitorMap(context, output, safety.ModExpr.ModuleInfo.MonitorMap);
            WriteTestFunction(context, output, safety.Main);
            context.WriteLine(output, "}");
        }

        private void WriteImplementationDecl(CompilationContext context, StringWriter output, Implementation impl)
        {
            context.WriteLine(output, $"public class {impl.Name} {{");
            WriteInitializeLinkMap(context, output, impl.ModExpr.ModuleInfo.LinkMap);
            WriteInitializeInterfaceDefMap(context, output, impl.ModExpr.ModuleInfo.InterfaceDef);
            WriteInitializeMonitorObserves(context, output, impl.ModExpr.ModuleInfo.MonitorMap.Keys);
            WriteInitializeMonitorMap(context, output, impl.ModExpr.ModuleInfo.MonitorMap);
            WriteTestFunction(context, output, impl.Main);
            context.WriteLine(output, "}");
        }

        private void WriteInitializeMonitorObserves(CompilationContext context, StringWriter output, ICollection<Machine> monitors)
        {
            context.WriteLine(output, "public static void InitializeMonitorObserves() {");

            foreach (var monitor in monitors)
            {
                context.WriteLine(output, $"PModule.monitorObserves[\"{monitor.Name}\"] = new List<string>();");
                foreach (var ev in monitor.Observes.Events)
                {
                    context.WriteLine(output, $"PModule.monitorObserves[\"{monitor.Name}\"].Add(\"{ev.Name}\");");
                }
            }

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
            context.WriteLine(output, "InitializeLinkMap();");
            context.WriteLine(output, "InitializeInterfaceDefMap();");
            context.WriteLine(output, "InitializeMonitorMap(runtime);");
            context.WriteLine(output, "InitializeMonitorObserves();");
            context.WriteLine(output, $"runtime.CreateMachine(typeof(_GodMachine), new _GodMachine.Config(typeof({main})));");
            context.WriteLine(output, "}");
        }

        private void WriteInitializeMonitorMap(CompilationContext context, StringWriter output, IDictionary<Machine, IEnumerable<Interface>> monitorMap)
        {
            // compute the reverse map
            var machineMap = new Dictionary<string, List<Machine>>();
            foreach (var monitor in monitorMap)
            {
                foreach (var machine in monitor.Value)
                {
                    if (!machineMap.ContainsKey(machine.Name))
                    {
                        machineMap[machine.Name] = new List<Machine>();
                    }
                    machineMap[machine.Name].Add(monitor.Key);
                }
            }

            context.WriteLine(output, "public static void InitializeMonitorMap(PSharpRuntime runtime) {");

            foreach (var machine in machineMap)
            {
                context.WriteLine(output, $"PModule.monitorMap[\"{machine.Key}\"] = new List<Type>();");
                foreach (var monitor in machine.Value)
                {
                    context.WriteLine(output, $"PModule.monitorMap[\"{machine.Key}\"].Add(typeof({context.Names.GetNameForDecl(monitor)});");
                }
            }
            foreach (var monitor in monitorMap.Keys)
            {
                context.WriteLine(output, $"runtime.RegisterMonitor(typeof({context.Names.GetNameForDecl(monitor)}));");
            }
            context.WriteLine(output, "}");
            context.WriteLine(output);
        }

        
        private void WriteInitializeInterfaceDefMap(CompilationContext context, StringWriter output, IDictionary<Interface, Machine> interfaceDef)
        {
            context.WriteLine(output, "public static void InitializeInterfaceDefMap() {");
            foreach (var map in interfaceDef)
            {
                context.WriteLine(output, $"PModule.interfaceDefinitionMap.Add(\"{map.Key.Name}\", typeof({context.Names.GetNameForDecl(map.Value)}));");
            }
            context.WriteLine(output, "}");
            context.WriteLine(output);
        }

        private void WriteInitializeLinkMap(CompilationContext context, StringWriter output, IDictionary<Interface, IDictionary<Interface, Interface>> linkMap)
        {
            
            context.WriteLine(output, "public static void InitializeLinkMap() {");
            foreach (var creatorInterface in linkMap)
            {
                context.WriteLine(output, $"PModule.linkMap[\"{creatorInterface.Key.Name}\"] = new Dictionary<string, string>();");
                foreach (var clinkMap in creatorInterface.Value)
                {
                    context.WriteLine(output, $"PModule.linkMap[\"{creatorInterface.Key.Name}\"].Add(\"{clinkMap.Key.Name}\", \"{clinkMap.Value.Name}\");");
                }
            }
            context.WriteLine(output, "}");
            context.WriteLine(output);
        }

        private void WriteEvent(CompilationContext context, StringWriter output, PEvent pEvent)
        {
            string declName = context.Names.GetNameForDecl(pEvent);
            
            // initialize the payload type
            string payloadType = pEvent.PayloadType.IsSameTypeAs(PrimitiveType.Null)? "object": GetCSharpType(context, pEvent.PayloadType);
            context.WriteLine(output, $"internal class {declName} : PEvent<{payloadType}>");
            context.WriteLine(output, "{");
            context.WriteLine(output, $"static {declName}() {{ AssertVal = {pEvent.Assert}; AssumeVal = {pEvent.Assume};}}");
            context.WriteLine(output, $"public {pEvent.Name}() : base() {{}}");
            context.WriteLine(output, $"public {pEvent.Name} ({payloadType} payload): base(payload)" + "{ }");
            context.WriteLine(output, "}");
        }

        private void WriteMachine(CompilationContext context, StringWriter output, Machine machine)
        {
            string declName = context.Names.GetNameForDecl(machine);
            context.WriteLine(output, $"internal class {declName} : PMachine");
            context.WriteLine(output, "{");

            foreach (Variable field in machine.Fields)
            {
                context.WriteLine(output, $"private {GetCSharpType(context, field.Type)} {context.Names.GetNameForDecl(field)} = {GetDefaultValue(context, field.Type)};");
            }

            // create the constructor to initialize the sends, creates and receives list
            WriteMachineContructor(context, output, machine);

            foreach (Function method in machine.Methods)
            {
                WriteFunction(context, output, method);
            }

            foreach (State state in machine.States)
            {
                WriteState(context, output, state);
            }
            context.WriteLine(output, "}");
        }

        private static void WriteMachineContructor(CompilationContext context, StringWriter output, Machine machine)
        {
            string declName = context.Names.GetNameForDecl(machine);
            context.WriteLine(output, $"public {declName}() {{");
            foreach (var sEvent in machine.Sends.Events)
            {
                context.WriteLine(output, $"this.sends.Add(\"{sEvent.Name}\");");
            }
            foreach (var rEvent in machine.Receives.Events)
            {
                context.WriteLine(output, $"this.receives.Add(\"{rEvent.Name}\");");
            }
            foreach (var iCreate in machine.Creates.Interfaces)
            {
                context.WriteLine(output, $"this.creates.Add(\"{iCreate.Name}\");");
            }
            context.WriteLine(output, "}");
            context.WriteLine(output);
        }

        private static void WriteState(CompilationContext context, StringWriter output, State state)
        {
            if (state.IsStart && !state.OwningMachine.IsSpec)
            {
                context.WriteLine(output, "[Start]");
                context.WriteLine(output, "[OnEntry(nameof(InitializeParametersFunction))]");
                context.WriteLine(output, $"[OnEventGotoState(typeof(ContructorEvent), typeof({context.Names.GetNameForDecl(state)}))]");
                context.WriteLine(output, "class __InitState__ : MachineState { }");
                context.WriteLine(output);
            }
            if (state.IsStart && state.OwningMachine.IsSpec)
            {
                context.WriteLine(output, "[Start]");
            }

            if (state.OwningMachine.IsSpec)
            {
                if (state.Temperature == StateTemperature.Cold)
                {
                    context.WriteLine(output, $"[Cold]");
                }
                else if (state.Temperature == StateTemperature.Hot)
                {
                    context.WriteLine(output, $"[Hot]");
                }
            }

            if (state.Entry != null)
            {
                context.WriteLine(output, $"[OnEntry(nameof({context.Names.GetNameForDecl(state.Entry)}))]");
            }

            var deferredEvents = new List<string>();
            var ignoredEvents = new List<string>();
            foreach (var eventHandler in state.AllEventHandlers)
            {
                PEvent pEvent = eventHandler.Key;
                IStateAction stateAction = eventHandler.Value;
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
            {
                context.WriteLine(output, $"[DeferEvents({string.Join(", ", deferredEvents.AsEnumerable())})]");
            }

            if (ignoredEvents.Count > 0)
            {
                context.WriteLine(output, $"[IgnoreEvents({string.Join(", ", ignoredEvents.AsEnumerable())})]");
            }

            if (state.Exit != null)
            {
                context.WriteLine(output, $"[OnExit(nameof({context.Names.GetNameForDecl(state.Exit)}))]");
            }

            var stateType = state.OwningMachine.IsSpec ? "MonitorState" : "MachineState";
            context.WriteLine(output, $"class {context.Names.GetNameForDecl(state)} : {stateType}");
            context.WriteLine(output, "{");
            context.WriteLine(output, "}");
        }
        
        private void WriteFunction(CompilationContext context, StringWriter output, Function function)
        {
            bool isStatic = function.Owner == null;
            bool isAsync = function.CanReceive == true;
            FunctionSignature signature = function.Signature;

            string staticKeyword = isStatic ? "static " : "";
            string asyncKeyword = isAsync ? "async " : "";
            string returnType = GetCSharpType(context, signature.ReturnType);

            if (isAsync)
            {
                returnType = returnType == "void" ? "Task" : $"Task<{returnType}>";
            }

            string functionName = context.Names.GetNameForDecl(function);
            string functionParameters = "";
            if (!function.IsAnon)
            {
                functionParameters = string.Join(
                    ", ",
                    signature.Parameters.Select(param => $"{GetCSharpType(context, param.Type)} {context.Names.GetNameForDecl(param)}"));
            }

            if (isStatic)
            {
                var seperator = functionParameters == "" ? "": ", ";
                functionParameters += string.Concat(seperator, "PMachine currentMachine");
            }

            context.WriteLine(output, $"public {staticKeyword}{asyncKeyword}{returnType} {functionName}({functionParameters})");
            WriteFunctionBody(context, output, function);
        }

        private void WriteFunctionBody(CompilationContext context, StringWriter output, Function function)
        { 
            context.WriteLine(output, "{");

            //add the declaration of currentMachine
            if (function.Owner != null)
            {
                context.WriteLine(output, $"{context.Names.GetNameForDecl(function.Owner)} currentMachine = this;");
            }
            if (function.IsAnon)
            {
                if (function.Signature.Parameters.Any())
                {
                    var param = function.Signature.Parameters.First();
                    context.WriteLine(output, $"{GetCSharpType(context, param.Type)} {context.Names.GetNameForDecl(param)} = (currentMachine.ReceivedEvent as PEvent<{GetCSharpType(context, param.Type)}>).Payload;");
                }
            }
            foreach (Variable local in function.LocalVariables)
            {
                PLanguageType type = local.Type;
                context.WriteLine(output, $"{GetCSharpType(context, type)} {context.Names.GetNameForDecl(local)} = {GetDefaultValue(context, type)};");
            }

            

            foreach (IPStmt bodyStatement in function.Body.Statements)
            {
                WriteStmt(context, output, bodyStatement);
            }
            context.WriteLine(output, "}");
        }

   
        private void WriteStmt(CompilationContext context, StringWriter output, IPStmt stmt)
        {
            switch (stmt)
            {
                case AnnounceStmt announceStmt:
                    context.Write(output, "currentMachine.Announce(");
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
                    {
                        context.WriteLine(output, "throw new PUnreachableCodeException();");
                    }
                    break;
                case AssignStmt assignStmt:
                    WriteLValue(context, output, assignStmt.Location);
                    context.Write(output, " = ");
                    WriteExpr(context, output, assignStmt.Value);
                    context.WriteLine(output, ";");
                    break;
                case CompoundStmt compoundStmt:
                    context.WriteLine(output, "{");
                    foreach (IPStmt subStmt in compoundStmt.Statements)
                    {
                        WriteStmt(context, output, subStmt);
                    }

                    context.WriteLine(output, "}");
                    break;
                case CtorStmt ctorStmt:
                    context.Write(output, "currentMachine.CreateInterface(");
                    context.Write(output, "currentMachine, ");
                    context.Write(output, $"\"{ctorStmt.Interface.Name}\"");
                    if (ctorStmt.Arguments.Any())
                    {
                        context.Write(output, $", ");
                        WriteExpr(context, output, ctorStmt.Arguments.First());
                    }
                    context.WriteLine(output, ");");
                    break;
                case FunCallStmt funCallStmt:
                    var isStatic = funCallStmt.Function.Owner == null;
                    var awaitMethod = funCallStmt.Function.CanReceive == true ? "await " : "";
                    var globalFunctionClass = isStatic? $"{context.GlobalFunctionClassName}." : "";
                    context.Write(output, $"{awaitMethod}{globalFunctionClass}{context.Names.GetNameForDecl(funCallStmt.Function)}(");
                    var separator = "";

                    
                    foreach (var param in funCallStmt.ArgsList)
                    {
                        context.Write(output, separator);
                        WriteExpr(context, output, param);
                        separator = ", ";
                    }

                    if (isStatic)
                    {
                        context.Write(output, separator+"this");
                    }

                    context.WriteLine(output, ");");
                    break;
                case GotoStmt gotoStmt:
                    context.Write(output, $"currentMachine.GotoState<{gotoStmt.State.QualifiedName}>(");
                    WriteExpr(context, output, gotoStmt.Payload);
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
                    throw new NotImplementedException();
                case MoveAssignStmt moveAssignStmt:
                    WriteLValue(context, output, moveAssignStmt.ToLocation);
                    context.WriteLine(output, $" = {context.Names.GetNameForDecl(moveAssignStmt.FromVariable)};");
                    break;
                case NoStmt _:
                    break;
                case PopStmt popStmt:
                    context.WriteLine(output, $"currentMachine.PopState();");
                    //last statement
                    context.WriteLine(output, "throw new PUnreachableCodeException();");
                    break;
                case PrintStmt printStmt:
                    context.Write(output, $"PModule.runtime.Logger.WriteLine(\"{printStmt.Message}\"");
                    foreach (IPExpr printArg in printStmt.Args)
                    {
                        context.Write(output, ", ");
                        WriteExpr(context, output, printArg);
                    }

                    context.WriteLine(output, ");");
                    break;
                case RaiseStmt raiseStmt:
                    context.Write(output, "currentMachine.RaiseEvent(");
                    context.Write(output, "currentMachine, ");
                    WriteExpr(context, output, raiseStmt.PEvent);
                    if (raiseStmt.Payload.Any())
                    {
                        context.Write(output, $", ");
                        WriteExpr(context, output, raiseStmt.Payload.First());
                    }
                    context.WriteLine(output, $");");
                    //last statement
                    context.WriteLine(output, "throw new PUnreachableCodeException();");
                    break;
                case ReceiveStmt receiveStmt:
                    string eventName = context.Names.GetTemporaryName("recvEvent");
                    string[] eventTypeNames = receiveStmt.Cases.Keys.Select(evt => context.Names.GetNameForDecl(evt)).ToArray();
                    string recvArgs = string.Join(", ", eventTypeNames.Select(name => $"typeof({name})"));
                    context.WriteLine(output, $"var {eventName} = await currentMachine.ReceiveEvent({recvArgs});");
                    context.WriteLine(output, $"switch ({eventName}) {{");
                    foreach (var recvCase in receiveStmt.Cases)
                    {
                        string caseName = context.Names.GetTemporaryName("evt");
                        context.WriteLine(output, $"case {context.Names.GetNameForDecl(recvCase.Key)} {caseName}: {{");
                        
                        context.WriteLine(output, "} break;");
                        throw new NotImplementedException();
                    }
                    context.WriteLine(output, "}");
                    break;
                case RemoveStmt removeStmt:
                    throw new NotImplementedException();
                    break;
                case ReturnStmt returnStmt:
                    context.Write(output, "return ");
                    WriteExpr(context, output, returnStmt.ReturnValue);
                    context.WriteLine(output, ";");
                    break;
                case SendStmt sendStmt:
                    context.Write(output, "currentMachine.SendEvent(");
                    context.Write(output, "currentMachine, ");
                    WriteExpr(context, output, sendStmt.MachineExpr);
                    context.Write(output, $",");
                    WriteExpr(context, output, sendStmt.Evt);

                    if (sendStmt.ArgsList.Any())
                    {
                        context.Write(output, $", ");
                        WriteExpr(context, output, sendStmt.ArgsList.First());
                    }

                    context.WriteLine(output, $");");
                    break;
                case SwapAssignStmt swapAssignStmt:
                    throw new NotImplementedException();
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
                    context.Write(output, "(");
                    WriteLValue(context, output, mapAccessExpr.MapExpr);
                    context.Write(output, ")[");
                    WriteExpr(context, output, mapAccessExpr.IndexExpr);
                    context.Write(output, "]");
                    break;
                case NamedTupleAccessExpr namedTupleAccessExpr:
                    throw new NotImplementedException();
                case SeqAccessExpr seqAccessExpr:
                    context.Write(output, "(");
                    WriteLValue(context, output, seqAccessExpr.SeqExpr);
                    context.Write(output, ")[");
                    WriteExpr(context, output, seqAccessExpr.IndexExpr);
                    context.Write(output, "]");
                    break;
                case TupleAccessExpr tupleAccessExpr:
                    throw new NotImplementedException();
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
                    context.Write(output, "(");
                    WriteExpr(context, output, binOpExpr.Lhs);
                    context.Write(output, $") {BinOpToStr(binOpExpr.Operation)} (");
                    WriteExpr(context, output, binOpExpr.Rhs);
                    context.Write(output, ")");
                    break;
                case BoolLiteralExpr boolLiteralExpr:
                    context.Write(output, boolLiteralExpr.Value ? "true" : "false");
                    break;
                case CastExpr castExpr:
                    throw new NotImplementedException();
                case CoerceExpr coerceExpr:
                    switch (coerceExpr.Type.Canonicalize())
                    {
                        case PrimitiveType oldType when oldType.IsSameTypeAs(PrimitiveType.Float):
                        case PrimitiveType oldType1 when oldType1.IsSameTypeAs(PrimitiveType.Int):
                            context.Write(output, $"(({GetCSharpType(context, coerceExpr.NewType)}) ");
                            WriteExpr(context, output, coerceExpr.SubExpr);
                            context.Write(output, ")");
                            break;
                        case PermissionType _:
                            context.Write(output, $"new PMachineValue(");
                            context.Write(output, "(");
                            WriteExpr(context, output, coerceExpr.SubExpr);
                            context.Write(output, ").Id, ");
                            context.Write(output, $"PInterfaces.GetPermissions(\"{coerceExpr.NewType.CanonicalRepresentation}\"))");
                            break;
                    }
                    throw new NotImplementedException();
                case ContainsKeyExpr containsKeyExpr:
                    context.Write(output, "(");
                    WriteExpr(context, output, containsKeyExpr.Map);
                    context.Write(output, ").ContainsKey(");
                    WriteExpr(context, output, containsKeyExpr.Key);
                    context.Write(output, ")");
                    break;
                case CtorExpr ctorExpr:
                    context.Write(output, "currentMachine.CreateInterface( ");
                    context.Write(output, "currentMachine, ");
                    context.Write(output, $"\"{ctorExpr.Interface.Name}\"");
                    if (ctorExpr.Arguments.Any())
                    {
                        context.Write(output, $", ");
                        WriteExpr(context, output, ctorExpr.Arguments.First());
                    }
                    context.Write(output, ")");
                    break;
                case DefaultExpr defaultExpr:
                    context.Write(output, GetDefaultValue(context, defaultExpr.Type));
                    break;
                case EnumElemRefExpr enumElemRefExpr:
                    EnumElem enumElem = enumElemRefExpr.Value;
                    context.Write(output, $"{context.Names.GetNameForDecl(enumElem.ParentEnum)}.{context.Names.GetNameForDecl(enumElem)}");
                    break;
                case EventRefExpr eventRefExpr:
                    var eventName = context.Names.GetNameForDecl(eventRefExpr.Value);
                    string payloadExpr = "";
                    if (!(eventName == "Halt" || eventName == "Default"))
                    {
                        payloadExpr = GetDefaultValue(context, eventRefExpr.Value.PayloadType);
                    }
                    context.Write(output, $"new {eventName}({payloadExpr})");
                    break;
                case FairNondetExpr _:
                    context.Write(output, "currentMachine.FairRandom()");
                    break;
                case FloatLiteralExpr floatLiteralExpr:
                    context.Write(output, $"{floatLiteralExpr.Value}");
                    break;
                case FunCallExpr funCallExpr:
                    var isStatic = funCallExpr.Function.Owner == null;
                    var awaitMethod = funCallExpr.Function.CanReceive == true ? "await " : "";
                    var globalFunctionClass = isStatic ? $"{context.GlobalFunctionClassName}." : "";
                    context.Write(output, $"{awaitMethod}{globalFunctionClass}{context.Names.GetNameForDecl(funCallExpr.Function)}(");
                    var separator = "";


                    foreach (var param in funCallExpr.Arguments)
                    {
                        context.Write(output, separator);
                        WriteExpr(context, output, param);
                        separator = ", ";
                    }

                    if (isStatic)
                    {
                        context.Write(output, separator + "this");
                    }
                    context.Write(output, ")");
                    break;
                case IntLiteralExpr intLiteralExpr:
                    context.Write(output, $"{intLiteralExpr.Value}");
                    break;
                case KeysExpr keysExpr:
                    context.Write(output, "(");
                    WriteExpr(context, output, keysExpr.Expr);
                    context.Write(output, ").Keys.ToList()");
                    break;
                case LinearAccessRefExpr linearAccessRefExpr:
                    string swapKeyword = linearAccessRefExpr.LinearType.Equals(LinearType.Swap) ? "ref " : "";
                    context.Write(output, $"{swapKeyword}{context.Names.GetNameForDecl(linearAccessRefExpr.Variable)}");
                    break;
                case NamedTupleExpr namedTupleExpr:
                    throw new NotImplementedException();
                case NondetExpr _:
                    context.Write(output, "currentMachine.Random()");
                    break;
                case NullLiteralExpr _:
                    context.Write(output, "null");
                    break;
                case SizeofExpr sizeofExpr:
                    context.Write(output, "(");
                    WriteExpr(context, output, sizeofExpr.Expr);
                    context.Write(output, ").Count");
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
                    throw new NotImplementedException();
                case ValuesExpr valuesExpr:
                    context.Write(output, "(");
                    WriteExpr(context, output, valuesExpr.Expr);
                    context.Write(output, ").Values.ToList()");
                    break;
                case MapAccessExpr _:
                case NamedTupleAccessExpr _:
                case SeqAccessExpr _:
                case TupleAccessExpr _:
                case VariableAccessExpr _:
                    WriteLValue(context, output, pExpr);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pExpr));
            }
        }

        private void WriteClone(CompilationContext context, StringWriter output, IExprTerm cloneExprTerm)
        {
            if (!(cloneExprTerm is IVariableRef variableRef))
            {
                WriteExpr(context, output, cloneExprTerm);
                return;
            }
            
            var variable = variableRef.Variable;
            context.Write(output, RenderClone(context, variable.Type, context.Names.GetNameForDecl(variable)));
        }

        private string RenderClone(CompilationContext context, PLanguageType cloneType, string termName)
        {
            switch (cloneType.Canonicalize())
            {
                case SequenceType seq:
                    var elem = context.Names.GetTemporaryName("elem");
                    return $"({termName}).ConvertAll({elem} => {RenderClone(context, seq.ElementType, elem)})";
                case MapType map:
                    var key = context.Names.GetTemporaryName("k");
                    var val = context.Names.GetTemporaryName("v");
                    return $"({termName}).ToDictionary({key} => {RenderClone(context, map.KeyType, key + ".Key")}, {val} => {RenderClone(context, map.ValueType, val + ".Value")})";
                case NamedTupleType type:
                    throw new NotImplementedException("named tuple types");
                case PermissionType type:
                    throw new NotImplementedException("permission types");
                case PrimitiveType type when type.IsSameTypeAs(PrimitiveType.Int):
                    return termName;
                case PrimitiveType type when type.IsSameTypeAs(PrimitiveType.Float):
                    return termName;
                case PrimitiveType type when type.IsSameTypeAs(PrimitiveType.Bool):
                    return termName;
                case PrimitiveType type when type.IsSameTypeAs(PrimitiveType.Machine):
                    return termName;
                case PrimitiveType type when type.IsSameTypeAs(PrimitiveType.Event):
                    return GetDefaultValue(context, type);
                case TupleType type:
                    throw new NotImplementedException("tuple types");
                default:
                    throw new NotImplementedException($"Cloning {cloneType.OriginalRepresentation}");
            }
        }

        private string GetCSharpType(CompilationContext context, PLanguageType type)
        {
            switch (type.Canonicalize())
            {
                case DataType _:
                    return "object";
                case EnumType enumType:
                    return context.Names.GetNameForDecl(enumType.EnumDecl);
                case ForeignType _:
                    throw new NotImplementedException();
                case MapType mapType:
                    return $"Dictionary<{GetCSharpType(context, mapType.KeyType)}, {GetCSharpType(context, mapType.ValueType)}>";
                case NamedTupleType _:
                    throw new NotImplementedException();
                case PermissionType _:
                    return "PMachineValue";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Any):
                    return "object";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Bool):
                    return "bool";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Int):
                    return "int";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Float):
                    return "double";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Event):
                    return "Event";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Machine):
                    return "PMachineValue";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Null):
                    return "void";
                case SequenceType sequenceType:
                    return $"List<{GetCSharpType(context, sequenceType.ElementType)}>";
                case TupleType _:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        private string GetDefaultValue(CompilationContext context, PLanguageType returnType)
        {
            switch (returnType.Canonicalize())
            {
                case EnumType enumType:
                    return $"({context.Names.GetNameForDecl(enumType.EnumDecl)})(0)";
                case MapType mapType:
                    return $"new {GetCSharpType(context, mapType)}()";
                case SequenceType sequenceType:
                    return $"new {GetCSharpType(context, sequenceType)}()";
                case NamedTupleType _:
                    throw new NotImplementedException();
                case TupleType _:
                    throw new NotImplementedException();
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Bool):
                    return "false";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Int):
                    return "0";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Float):
                    return "0.0";
                case PermissionType _:
                case PrimitiveType anyType when anyType.IsSameTypeAs(PrimitiveType.Any):
                case PrimitiveType eventType when eventType.IsSameTypeAs(PrimitiveType.Event):
                case PrimitiveType nullType when nullType.IsSameTypeAs(PrimitiveType.Null):
                case PrimitiveType machineType when machineType.IsSameTypeAs(PrimitiveType.Machine):
                case ForeignType _:
                case DataType _:
                    return "null";
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
                case BinOpType.Eq:
                    return "==";
                case BinOpType.Neq:
                    return "!=";
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
