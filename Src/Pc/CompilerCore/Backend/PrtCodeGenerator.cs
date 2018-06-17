using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Pc.Backend.ASTExt;
using Microsoft.Pc.TypeChecker;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.Expressions;
using Microsoft.Pc.TypeChecker.AST.Statements;
using Microsoft.Pc.TypeChecker.AST.States;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.Backend
{
    public partial class PrtCodeGenerator : ICodeGenerator
    {
        private const string FunCallArgsArrayName = "FUNARGS";
        private const string FunCallRetValName = "FUNRETVAL";
        private const string FunResultValName = "retval";

        private static readonly Dictionary<Type, string> DeclNameParts = new Dictionary<Type, string>
        {
            {typeof(EnumElem), "ENUMELEM"},
            {typeof(Function), "FUNCTION"},
            {typeof(Implementation), "IMPL"},
            {typeof(Interface), "I"},
            {typeof(Machine), "MACHINE"},
            {typeof(NamedEventSet), "EVENTSET"},
            {typeof(NamedModule), "MODULE"},
            {typeof(PEnum), "ENUM"},
            {typeof(PEvent), "EVENT"},
            {typeof(RefinementTest), "REFINEMENT_TEST"},
            {typeof(SafetyTest), "SAFETY_TEST"},
            {typeof(State), "STATE"},
            {typeof(StateGroup), "STATEGROUP"},
            {typeof(TypeDef), "TYPEDEF"},
            {typeof(Variable), "VAR"}
        };

        public IReadOnlyList<CompiledFile> GenerateCode(ITranslationErrorHandler handler, ICompilerOutput log, string projectName,
                                                        Scope globalScope)
        {
            // Manages job-specific state so that jobs can be parallelized
            var context = new CompilationContext(projectName);

            // Create header and source files.
            var cHeader = new CompiledFile(context.HeaderFileName);
            var cSource = new CompiledFile(context.SourceFileName);

            // Write the header file
            WriteGenericHeader(context, cHeader.Stream);

            foreach (IPDecl decl in globalScope.AllDecls)
            {
                WriteForwardDeclaration(context, cHeader.Stream, decl);
            }

            WriteGenericFooter(context, cHeader.Stream);

            // Write includes and common macros, if any
            WriteSourcePrologue(context, cSource.Stream);

            // Write the machine and function bodies into temporary buffer
            var bodyWriter = new StringWriter();
            foreach (IPDecl decl in globalScope.AllDecls)
            {
                WriteSourceDecl(context, decl, bodyWriter);
            }

            // Write all the type definitions and function implementation prototypes
            context.WriteLine(cSource.Stream, "// Type universe for program:");
            foreach (PLanguageType type in context.UsedTypes)
            {
                WriteTypeDefinition(context, type, cSource.Stream);
            }

            context.WriteLine(cSource.Stream);

            context.WriteLine(cSource.Stream, "// Function implementation prototypes:");
            foreach (Function function in AllMethods(globalScope))
            {
                string functionName = context.Names.GetNameForFunctionImpl(function);
                context.WriteLine(cSource.Stream, $"PRT_VALUE* {functionName}(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);");
                context.WriteLine(cSource.Stream, $"extern PRT_FUNDECL {GetPrtNameForDecl(context, function)};");
                context.WriteLine(cSource.Stream);
            }

            context.WriteLine(cSource.Stream);

            // Append all the function bodies and declaration structs (which have forward declarations in the header)
            cSource.Stream.GetStringBuilder().Append(bodyWriter);

            // Finally, write the overall program decl
            WriteProgramDecl(context, globalScope, cSource.Stream);

            // All done! Return the compiled files.
            return new List<CompiledFile> {cHeader, cSource};
        }

        private IList<T> ToOrderedListByPermutation<T>(IEnumerable<T> enumerable, Func<T, int> perm)
        {
            var items = enumerable.ToList();
            IList<T> inOrder = new T[items.Count];
            foreach (T item in items)
            {
                inOrder[perm(item)] = item;
            }
            return inOrder;
        }

        private void WriteProgramDecl(CompilationContext context, Scope globalScope, TextWriter output)
        {
            // generate event array
            var eventsList = globalScope.Events.Where(e => !e.IsBuiltIn);
            globalScope.Get("null", out PEvent nullEvent);
            globalScope.Get("halt", out PEvent haltEvent);
            eventsList = new[] {nullEvent, haltEvent}.Concat(eventsList);

            var eventArrayName = context.Names.GetTemporaryName("ALL_EVENTS");
            var eventArrayBody = string.Join(", ", eventsList.Select(ev => "&" + GetPrtNameForDecl(context, ev)));
            eventArrayBody = string.IsNullOrEmpty(eventArrayBody) ? "NULL" : eventArrayBody;
            context.WriteLine(output, $"PRT_EVENTDECL* {eventArrayName}[] = {{ {eventArrayBody} }};");

            // generate machine array
            var machineArrayName = context.Names.GetTemporaryName("ALL_MACHINES");
            var machineArrayBody = string.Join(", ", ToOrderedListByPermutation(globalScope.Machines, context.GetNumberForMachine)
                                                                .Select(ev => "&" + GetPrtNameForDecl(context, ev)));
            machineArrayBody = string.IsNullOrEmpty(machineArrayBody) ? "NULL" : machineArrayBody;
            context.WriteLine(output, $"PRT_MACHINEDECL* {machineArrayName}[] = {{ {machineArrayBody} }};");

            // generate interface array
            var interfaceArrayName = context.Names.GetTemporaryName("ALL_INTERFACES");
            var interfaceArrayBody = string.Join(", ", ToOrderedListByPermutation(globalScope.Interfaces, context.GetNumberForInterface)
                                                                  .Select(ev => "&" + GetPrtNameForDecl(context, ev)));
            interfaceArrayBody = string.IsNullOrEmpty(interfaceArrayBody) ? "NULL" : interfaceArrayBody;
            context.WriteLine(output, $"PRT_INTERFACEDECL* {interfaceArrayName}[] = {{ {interfaceArrayBody} }};");

            // generate functions array
            var funcArrayName = context.Names.GetTemporaryName("ALL_FUNCTIONS");
            var funcArrayBody = string.Join(", ", globalScope.Functions.Select(ev => "&" + GetPrtNameForDecl(context, ev)));
            funcArrayBody = string.IsNullOrEmpty(funcArrayBody) ? "NULL" : funcArrayBody;
            context.WriteLine(output, $"PRT_FUNDECL* {funcArrayName}[] = {{ {funcArrayBody} }};");

            foreach (Implementation impl in globalScope.Implementations)
            {
                var linkMap = impl.ModExpr.ModuleInfo.LinkMap;
                
                int[][] trueLinkMap = ResolveLinkMap(globalScope, context, linkMap);
                string[] mapNames = Enumerable.Repeat("NULL", trueLinkMap.Length).ToArray();
                for (var i = 0; i < trueLinkMap.Length; i++)
                {
                    var iMap = trueLinkMap[i];
                    if (iMap != null)
                    {
                        var mapTmpName = context.Names.GetTemporaryName($"{impl.Name}_LME_{i}");
                        mapNames[i] = mapTmpName;
                        context.WriteLine(output, $"int {mapTmpName}[] = {{ {string.Join(",", iMap)} }};");
                    }
                }
                var linkMapName = context.Names.GetTemporaryName($"{impl.Name}_LINKMAP");
                context.WriteLine(output, $"int* {linkMapName}[] = {{ {string.Join(", ", mapNames)} }};");

                var machineDefMap = impl.ModExpr.ModuleInfo.InterfaceDef;
                var machineDefMapName = context.Names.GetTemporaryName($"{impl.Name}_DEFMAP");
                int[] realMachineDefMap = Enumerable.Repeat(-1, trueLinkMap.Length).ToArray();
                foreach (var linking in machineDefMap)
                {
                    realMachineDefMap[context.GetNumberForInterface(linking.Key)] = context.GetNumberForMachine(linking.Value);
                }
                context.WriteLine(output, $"int {machineDefMapName}[] = {{ {string.Join(",", realMachineDefMap)} }};");

                context.WriteLine(output, $"PRT_PROGRAMDECL {GetPrtNameForDecl(context, impl)} = {{");
                context.WriteLine(output, $"{globalScope.Events.Count()}U,");
                context.WriteLine(output, $"{globalScope.Machines.Count()}U,");
                context.WriteLine(output, $"{globalScope.Interfaces.Count()}U,");
                context.WriteLine(output, $"{globalScope.Functions.Count()}U,");
                context.WriteLine(output, "0U,"); // TODO: foreign types
                context.WriteLine(output, $"{eventArrayName},");
                context.WriteLine(output, $"{machineArrayName},");
                context.WriteLine(output, $"{interfaceArrayName},");
                context.WriteLine(output, $"{funcArrayName},");
                context.WriteLine(output, $"NULL,"); // TODO: foreign types
                context.WriteLine(output, $"{linkMapName},");
                context.WriteLine(output, $"{machineDefMapName}");
                context.WriteLine(output, "};");
            }
        }

        private static int[][] ResolveLinkMap(Scope globalScope, CompilationContext context,
                                       IDictionary<Interface, IDictionary<Interface, Interface>> linkMap)
        {
            var nInterfaces = globalScope.Interfaces.Count();
            var maps = new int[nInterfaces][];
            foreach (var keyValuePair in linkMap)
            {
                var firstInterfaceIndex = context.GetNumberForInterface(keyValuePair.Key);
                Debug.Assert(maps[firstInterfaceIndex] == null);
                maps[firstInterfaceIndex] = Enumerable.Repeat(-1, nInterfaces).ToArray();
                
                foreach (var finalMapping in keyValuePair.Value)
                {
                    var secondInterfaceIndex = context.GetNumberForInterface(finalMapping.Key);
                    var finalInterfaceIndex = context.GetNumberForInterface(finalMapping.Value);
                    maps[firstInterfaceIndex][secondInterfaceIndex] = finalInterfaceIndex;
                }
            }

            return maps;
        }

        private IEnumerable<Function> AllMethods(Scope scope)
        {
            foreach (Function function in scope.Functions)
            {
                yield return function;
            }

            foreach (Machine machine in scope.Machines)
            {
                // includes anonymous functions
                foreach (Function method in machine.Methods)
                {
                    yield return method;
                }
            }
        }

        private void WriteSourceDecl(CompilationContext context, IPDecl decl, TextWriter output)
        {
            string declName = GetPrtNameForDecl(context, decl);
            switch (decl)
            {
                case EnumElem _:
                    // Member of a type. Instantitated by usage.
                    return;
                case PEnum _:
                    // Declares a type. Instantitated by usage.
                    return;
                case Function function:
                    string functionImplName = context.Names.GetNameForFunctionImpl(function);
                    
                    context.WriteLine(output, $"PRT_VALUE* {functionImplName}(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)");
                    context.WriteLine(output, "{");
                    WriteFunctionBody(context, function, output);
                    context.WriteLine(output, "}");
                    context.WriteLine(output);
                    context.WriteLine(output, $"PRT_FUNDECL {declName} =");
                    context.WriteLine(output, "{");
                    context.WriteLine(output, $"\"{function.Name}\","); // name of function in original program
                    context.WriteLine(output, $"&{functionImplName},"); // pointer to implementation
                    context.WriteLine(output, "NULL"); // payload type for anonymous functions: always NULL.
                    context.WriteLine(output, "};");
                    context.WriteLine(output);
                    break;
                case Implementation _:
                    // does not produce a struct definition - aside from ProgramDecl
                    return;
                case Interface @interface:
                    string ifaceRecvSetName;
                    if (@interface.ReceivableEvents is NamedEventSet set)
                    {
                        ifaceRecvSetName = GetPrtNameForDecl(context, set);
                    }
                    else
                    {
                        var interfaceEventSet = new NamedEventSet(@interface.Name + "_RECV", @interface.SourceLocation);
                        interfaceEventSet.AddEvents(@interface.ReceivableEvents.Events);
                        WriteSourceDecl(context, interfaceEventSet, output);
                        ifaceRecvSetName = GetPrtNameForDecl(context, interfaceEventSet);
                    }
                    
                    context.WriteLine(output, $"PRT_INTERFACEDECL {declName} =");
                    context.WriteLine(output, "{");
                    context.WriteLine(output, $"{context.GetNumberForInterface(@interface)}U,");
                    context.WriteLine(output, $"\"{@interface.Name}\",");
                    context.WriteLine(output, $"&{context.Names.GetNameForType(@interface.PayloadType)},");
                    context.WriteLine(output, $"&{ifaceRecvSetName}");
                    context.WriteLine(output, "};");
                    break;
                case Machine machine:
                    var machineFields = machine.Fields.ToList();
                    var fieldArrayName = "NULL";
                    if (machineFields.Any())
                    {
                        fieldArrayName = context.Names.GetTemporaryName($"{machine.Name}_VARS");
                        context.WriteLine(output, $"PRT_VARDECL {fieldArrayName}[] = {{");
                        for (var i = 0; i < machineFields.Count; i++)
                        {
                            var field = machineFields[i];
                            var sep = i == machineFields.Count - 1 ? "" : ",";
                            context.WriteLine(output, $"{{ \"{field.Name}\", &{context.Names.GetNameForType(field.Type)} }}{sep}");
                        }

                        context.WriteLine(output, "};");
                        context.WriteLine(output);
                    }

                    var machineStates = machine.AllStates().ToList();
                    var machineStatesInOrder = new State[machineStates.Count];
                    foreach (var state in machineStates)
                    {
                        WriteSourceDecl(context, state, output);
                        machineStatesInOrder[context.GetNumberForState(state)] = state;
                    }

                    var stateArrayName = context.Names.GetTemporaryName($"{machine.Name}_STATES");
                    var stateArrayBody = string.Join(", ", machineStatesInOrder.Select(st => GetPrtNameForDecl(context, st)));
                    context.WriteLine(output, $"PRT_STATEDECL {stateArrayName}[] = {{ {stateArrayBody} }};");
                    context.WriteLine(output);
                    
                    var machineMethods = machine.Methods.ToList();
                    foreach (Function machineMethod in machineMethods)
                    {
                        WriteSourceDecl(context, machineMethod, output);
                    }

                    var methodArrayName = "NULL";
                    if (machineMethods.Any())
                    {
                        methodArrayName = context.Names.GetTemporaryName($"{machine.Name}_METHODS");
                        var methodArrayBody = string.Join(", ", machineMethods.Select(m => $"&{GetPrtNameForDecl(context, m)}"));
                        context.WriteLine(output, $"PRT_FUNDECL* {methodArrayName}[] = {{ {methodArrayBody} }};");
                        context.WriteLine(output);
                    }

                    string machineRecvSetName = GetReceivesNameOrMkTemp(machine, context, output);
                    string machineSendSetName = GetSendsNameOrMkTemp(machine, context, output);
                    string machineCreatesName = "NULL";
                    if (machine.Creates.Interfaces.Any())
                    {
                        var createsInterfaces = machine.Creates.Interfaces.ToList();
                        machineCreatesName = context.Names.GetTemporaryName($"{machine.Name}_CREATES");
                        var createsArrayName = context.Names.GetTemporaryName($"{machine.Name}_CREATES_ARR");
                        var createsArrayBody = string.Join(", ", createsInterfaces.Select(context.GetNumberForInterface));
                        context.WriteLine(output, $"PRT_UINT32 {createsArrayName}[] = {{ {createsArrayBody} }};");
                        context.WriteLine(output, $"PRT_INTERFACESETDECL {machineCreatesName} = {{ {createsInterfaces.Count}, {createsArrayName} }};");
                        machineCreatesName = "&" + machineCreatesName;
                    }

                    var maxQueueSize = machine.Assert ?? uint.MaxValue;
                    context.WriteLine(output, $"PRT_MACHINEDECL {declName} = ");
                    context.WriteLine(output, "{");
                    context.WriteLine(output, $"{context.GetNumberForMachine(machine)}U,");
                    context.WriteLine(output, $"\"{machine.Name}\",");
                    context.WriteLine(output, $"&{machineRecvSetName},");
                    context.WriteLine(output, $"&{machineSendSetName},");
                    context.WriteLine(output, $"{machineCreatesName},");
                    context.WriteLine(output, $"{machineFields.Count}U,");
                    context.WriteLine(output, $"{machineStatesInOrder.Length}U,");
                    context.WriteLine(output, $"{machineMethods.Count}U,");
                    context.WriteLine(output, $"{maxQueueSize}U,");
                    context.WriteLine(output, $"{context.GetNumberForState(machine.StartState)}U,");
                    context.WriteLine(output, $"{fieldArrayName},");
                    context.WriteLine(output, $"{stateArrayName},");
                    context.WriteLine(output, $"{methodArrayName}");
                    context.WriteLine(output, "};");

                    break;
                case NamedEventSet namedEventSet:
                    string innerSetName = context.Names.GetTemporaryName(namedEventSet.Name + "_INNER");
                    var eventDeclNames = namedEventSet.Events.Select(x => "&" + GetPrtNameForDecl(context, x)).ToList();
                    string eventDeclArrBody = string.Join(", ", eventDeclNames);
                    eventDeclArrBody = string.IsNullOrEmpty(eventDeclArrBody) ? "NULL" : eventDeclArrBody;
                    context.WriteLine(output, $"PRT_EVENTDECL* {innerSetName}[] = {{ {eventDeclArrBody} }};");
                    context.WriteLine(output, $"PRT_EVENTSETDECL {declName} =");
                    context.WriteLine(output, "{");
                    context.WriteLine(output, $"{eventDeclNames.Count}U,");
                    context.WriteLine(output, $"{innerSetName},");
                    context.WriteLine(output, "NULL");
                    context.WriteLine(output, "};");
                    break;
                case NamedModule _:
                    // does not produce a struct definition
                    return;
                case PEvent pEvent when pEvent.IsBuiltIn:
                    return;
                case PEvent pEvent when !pEvent.IsBuiltIn:
                    long eventBound = Math.Min(pEvent.Assert == -1 ? uint.MaxValue : (uint) pEvent.Assert,
                                               pEvent.Assume == -1 ? uint.MaxValue : (uint) pEvent.Assume);

                    context.WriteLine(output, $"PRT_EVENTDECL {declName} = ");
                    context.WriteLine(output, "{");
                    context.WriteLine(output, "{ PRT_VALUE_KIND_EVENT, 0U },");
                    context.WriteLine(output, $"\"{pEvent.Name}\",");
                    context.WriteLine(output, $"{eventBound}U,");
                    context.WriteLine(output, $"&{context.Names.GetNameForType(pEvent.PayloadType)}");
                    context.WriteLine(output, "};");
                    break;
                case RefinementTest _:
                    // does not produce a struct definition
                    return;
                case SafetyTest _:
                    // does not produce a struct definition
                    return;
                case TypeDef typeDef:
                    context.WriteLine(output, $"PRT_TYPE* {declName} = &{context.Names.GetNameForType(typeDef.Type)};");
                    return;
                case Variable _:
                    // does not produce a struct definition
                    // handled by MachineDecl.
                    return;
                case State state:
                    var stateEntryFunName = state.Entry == null ? "&_P_NO_OP" : $"&{GetPrtNameForDecl(context, state.Entry)}";
                    var stateExitFunName = state.Exit == null ? "&_P_NO_OP" : $"&{GetPrtNameForDecl(context, state.Exit)}";

                    var stateIndex = context.GetNumberForState(state);
                    var stateData = BuildActionSets(context, state);

                    WriteSourceDecl(context, stateData.DefersSet, output);
                    WriteSourceDecl(context, stateData.TransSet, output);
                    WriteSourceDecl(context, stateData.DosSet, output);

                    var transArrName = "NULL";
                    if(stateData.Trans.Count != 0)
                    {
                        transArrName = context.Names.GetTemporaryName("TRANS");
                        context.WriteLine(output, $"PRT_TRANSDECL {transArrName}[] =");
                        context.WriteLine(output, "{");
                        for (var i = 0; i < stateData.Trans.Count; i++)
                        {
                            (PEvent triggerEvent, int destIndex, string transFunRef) = stateData.Trans[i];
                            string triggerName = GetPrtNameForDecl(context, triggerEvent);
                            var comma = i == stateData.Trans.Count - 1 ? "" : ",";
                            context.WriteLine(output, $"{{ {stateIndex}, &{triggerName}, {destIndex}, {transFunRef} }}{comma}");
                        }
                        context.WriteLine(output, "};");
                        context.WriteLine(output);
                    }
                    

                    var dosArrName = "NULL";
                    if (stateData.Dos.Count != 0)
                    { 
                        dosArrName = context.Names.GetTemporaryName("DOS");
                        context.WriteLine(output, $"PRT_DODECL {dosArrName}[] =");
                        context.WriteLine(output, "{");
                        for (var i = 0; i < stateData.Dos.Count; i++)
                        {
                            (PEvent triggerEvent, Function transFun) = stateData.Dos[i];
                            string triggerName = GetPrtNameForDecl(context, triggerEvent);
                            var comma = i == stateData.Trans.Count - 1 ? "" : ",";
                            var funName = transFun != null ? GetPrtNameForDecl(context, transFun) : "_P_NO_OP";
                            context.WriteLine(output, $"{{ {stateIndex}, &{triggerName}, &{funName} }}{comma}");
                        }
                        context.WriteLine(output, "};");
                        context.WriteLine(output);
                    }
                    
                    context.WriteLine(output, $"#define {declName} \\");
                    context.WriteLine(output, "{ \\");
                    context.WriteLine(output, $"\"{state.QualifiedName}\", \\");
                    context.WriteLine(output, $"{stateData.Trans.Count}U, \\");
                    context.WriteLine(output, $"{stateData.Dos.Count}U, \\");
                    context.WriteLine(output, $"&{GetPrtNameForDecl(context, stateData.DefersSet)}, \\");
                    context.WriteLine(output, $"&{GetPrtNameForDecl(context, stateData.TransSet)}, \\");
                    context.WriteLine(output, $"&{GetPrtNameForDecl(context, stateData.DosSet)}, \\");
                    context.WriteLine(output, $"{transArrName}, \\");
                    context.WriteLine(output, $"{dosArrName}, \\");
                    context.WriteLine(output, $"{stateEntryFunName}, \\");
                    context.WriteLine(output, $"{stateExitFunName}, \\");
                    context.WriteLine(output, "}");
                    break;
                case StateGroup stateGroup:
                    foreach (var state in stateGroup.States)
                    {
                        WriteSourceDecl(context, state, output);
                    }
                    foreach (var subGroup in stateGroup.Groups)
                    {
                        WriteSourceDecl(context, subGroup, output);
                    }
                    break;
            }

            context.WriteLine(output);
        }

        private string GetReceivesNameOrMkTemp(Machine machine, CompilationContext context, TextWriter output)
        {
            string eventSetName;
            if (machine.Receives is NamedEventSet mRecvSet)
            {
                eventSetName = GetPrtNameForDecl(context, mRecvSet);
            }
            else
            {
                var machineTempRecvSet = new NamedEventSet(machine.Name + "_RECV", machine.SourceLocation);
                machineTempRecvSet.AddEvents(machine.Receives.Events);
                WriteSourceDecl(context, machineTempRecvSet, output);
                eventSetName = GetPrtNameForDecl(context, machineTempRecvSet);
            }

            return eventSetName;
        }

        private string GetSendsNameOrMkTemp(Machine machine, CompilationContext context, TextWriter output)
        {
            string eventSetName;
            if (machine.Sends is NamedEventSet mSendSet)
            {
                eventSetName = GetPrtNameForDecl(context, mSendSet);
            }
            else
            {
                var machineTempSendSet = new NamedEventSet(machine.Name + "_SEND", machine.SourceLocation);
                machineTempSendSet.AddEvents(machine.Sends.Events);
                WriteSourceDecl(context, machineTempSendSet, output);
                eventSetName = GetPrtNameForDecl(context, machineTempSendSet);
            }

            return eventSetName;
        }

        private class StateActionResults
        {
            public NamedEventSet DefersSet { get; }
            public NamedEventSet TransSet { get; }
            public NamedEventSet DosSet { get; }

            public List<(PEvent, Function)> Dos { get; }
            public List<(PEvent, int, string)> Trans { get; }

            public StateActionResults(NamedEventSet defersSet, NamedEventSet transSet, NamedEventSet dosSet, List<(PEvent, Function)> dos, List<(PEvent, int, string)> trans)
            {
                DefersSet = defersSet;
                TransSet = transSet;
                DosSet = dosSet;
                Dos = dos;
                Trans = trans;
            }
        }

        private static StateActionResults BuildActionSets(CompilationContext context, State state)
        {
            var defersSet = new NamedEventSet(state.Name + "_DEFERS", state.SourceLocation);
            var transSet = new NamedEventSet(state.Name + "_TRANS", state.SourceLocation);
            var dosSet = new NamedEventSet(state.Name + "_DOS", state.SourceLocation);

            var dos = new List<(PEvent, Function)>();
            var trans = new List<(PEvent, int, string)>();

            foreach (var eventActionPair in state.AllEventHandlers)
            {
                PEvent pEvent = eventActionPair.Key;
                switch (eventActionPair.Value)
                {
                    case EventDefer _:
                        defersSet.AddEvent(pEvent);
                        break;
                    case EventDoAction eventDoAction:
                        dosSet.AddEvent(pEvent);
                        dos.Add((pEvent, eventDoAction.Target));
                        break;
                    case EventGotoState eventGotoState:
                        transSet.AddEvent(pEvent);
                        var transFunName = eventGotoState.TransitionFunction == null
                                               ? "_P_NO_OP"
                                               : GetPrtNameForDecl(context, eventGotoState.TransitionFunction);
                        trans.Add((pEvent, context.GetNumberForState(eventGotoState.Target), "&" + transFunName));
                        break;
                    case EventIgnore _:
                        dosSet.AddEvent(pEvent);
                        dos.Add((pEvent, null));
                        break;
                    case EventPushState eventPushState:
                        transSet.AddEvent(pEvent);
                        trans.Add((pEvent, context.GetNumberForState(eventPushState.Target), "NULL"));
                        break;
                }
            }

            return new StateActionResults(defersSet,transSet,dosSet,dos,trans);
        }

        private void WriteFunctionBody(CompilationContext context, Function function, TextWriter output)
        {
            for (var i = 0; i < function.Signature.Parameters.Count; i++)
            {
                Variable argument = function.Signature.Parameters[i];
                string varName = GetPrtNameForDecl(context, argument);
                context.WriteLine(output, $"PRT_VALUE** {varName} = argRefs[{i}];");
            }

            foreach (Variable localVariable in function.LocalVariables)
            {
                string varName = GetPrtNameForDecl(context, localVariable);
                string varTypeName = context.Names.GetNameForType(localVariable.Type);
                // TODO: optimize away PrtMkDefaultValue if dataflow shows no usages before assignments.
                context.WriteLine(output, $"PRT_VALUE* {varName} = PrtMkDefaultValue(&{varTypeName});");
            }

            context.WriteLine(output, $"PRT_VALUE* {FunCallRetValName} = NULL;");
            // TODO: figure out how many args are actually necessary based on function calls.
            context.WriteLine(output, $"PRT_VALUE** {FunCallArgsArrayName}[32];");
            context.WriteLine(output, "PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;");
            if (function.Signature.ReturnType.IsSameTypeAs(PrimitiveType.Null))
            {
                context.WriteLine(output, $"PRT_VALUE* {FunResultValName} = NULL;");
            }
            else
            {
                string nameForReturnType = context.Names.GetNameForType(function.Signature.ReturnType);
                context.WriteLine(output, $"PRT_VALUE* {FunResultValName} = PrtMkDefaultValue(&{nameForReturnType});");
            }

            context.WriteLine(output);

            var bodyWriter = new StringWriter();

            // skip unnecessary nesting level.
            if (function.Body is CompoundStmt body)
            {
                foreach (IPStmt stmt in body.Statements)
                {
                    WriteStmt(context, function, stmt, bodyWriter);
                }
            }
            else
            {
                WriteStmt(context, function, function.Body, bodyWriter);
            }

            bodyWriter.WriteLine("p_return:");
            foreach (Variable localVariable in function.LocalVariables)
            {
                string varName = GetPrtNameForDecl(context, localVariable);
                context.WriteLine(bodyWriter, $"PrtFreeValue({varName}); {varName} = NULL;");
            }

            context.WriteLine(bodyWriter, $"return {FunResultValName};");

            // Write gathered literals to the prologue
            foreach (var literal in context.GetRegisteredIntLiterals(function))
            {
                context.WriteLine(output, $"PRT_VALUE {literal.Value} = {{ PRT_VALUE_KIND_INT, {{ .nt = {literal.Key} }} }};");
            }
            foreach (var literal in context.GetRegisteredFloatLiterals(function))
            {
                context.WriteLine(output, $"PRT_VALUE {literal.Value} = {{ PRT_VALUE_KIND_FLOAT, {{ .ft = {literal.Key} }} }};");
            }
            foreach (var literal in context.GetRegisteredBoolLiterals(function))
            {
                context.WriteLine(output, $"PRT_VALUE {literal.Value} = {{ PRT_VALUE_KIND_BOOL, {{ .bl = {(literal.Key ? "PRT_TRUE" : "PRT_FALSE")} }} }};");
            }

            output.Write(bodyWriter);
        }

        private void WriteStmt(CompilationContext context, Function function, IPStmt stmt, TextWriter output)
        {
            context.WriteLine(output, $"// {stmt.GetType().Name}");
            switch (stmt)
            {
                case AnnounceStmt announceStmt:
                    break;
                case AssertStmt assertStmt:
                    context.Write(output, "PrtAssert(PrtPrimGetBool(");
                    WriteExpr(context, function, assertStmt.Assertion, output);
                    context.WriteLine(output, $"), \"{assertStmt.Message}\");");
                    break;
                case AssignStmt assignStmt:
                    // Free old value
                    context.Write(output, "PrtFreeValue(");
                    WriteExpr(context, function, assignStmt.Variable, output);
                    context.WriteLine(output, ");");

                    // Assign new value
                    WriteExpr(context, function, assignStmt.Variable, output);
                    context.Write(output, " = ");
                    WriteExpr(context, function, assignStmt.Value, output);
                    context.WriteLine(output, ";");
                    break;
                case CompoundStmt compoundStmt:
                    context.WriteLine(output, "{");
                    foreach (IPStmt pStmt in compoundStmt.Statements)
                    {
                        WriteStmt(context, function, pStmt, output);
                    }

                    context.WriteLine(output, "}");
                    break;
                case CtorStmt ctorStmt:
                    context.Write(output, $"PrtMkInterface(context, {context.GetNumberForInterface(ctorStmt.Interface)}, {ctorStmt.Arguments.Count}");
                    foreach (IPExpr pExpr in ctorStmt.Arguments)
                    {
                        Debug.Assert(pExpr is VariableAccessExpr);
                        var argVar = (VariableAccessExpr) pExpr;
                        context.Write(output, $", &{GetPrtNameForDecl(context, argVar.Variable)}");
                    }
                    context.WriteLine(output, ");");
                    break;
                case FunCallStmt funCallStmt:
                    string funImplName = context.Names.GetNameForFunctionImpl(funCallStmt.Fun);
                    var funArgs = funCallStmt.ArgsList.Cast<ILinearRef>().ToList();

                    // Put all the arguments in the args array
                    foreach (var arg in funArgs.Select((arg, i) => new {arg.Variable, i}))
                    {
                        context.WriteLine(output, $"{FunCallArgsArrayName}[{arg.i}] = &{GetPrtNameForDecl(context, arg.Variable)};");
                    }

                    // Call the function and immediately free the value
                    context.WriteLine(output, $"PrtFreeValue({funImplName}(context, {FunCallArgsArrayName}));");

                    // Free and set to null all the moved arguments
                    var toFree = funArgs.Where(arg => arg.LinearType.Equals(LinearType.Move))
                                        .Select(arg => GetPrtNameForDecl(context, arg.Variable));
                    foreach (string argName in toFree)
                    {
                        context.WriteLine(output, $"PrtFreeValue({argName});");
                        context.WriteLine(output, $"{argName} = NULL;");
                    }

                    break;
                case GotoStmt gotoStmt:
                    var destStateIndex = context.GetNumberForState(gotoStmt.State);
                    context.WriteLine(output, $"PrtGoto(context, {destStateIndex}U, ");
                    if (gotoStmt.Payload != null)
                    {
                        Debug.Assert(gotoStmt.Payload is VariableAccessExpr);
                        var gotoArg = (VariableAccessExpr) gotoStmt.Payload;
                        context.Write(output, $"1, &{GetPrtNameForDecl(context, gotoArg.Variable)}");
                    }
                    else
                    {
                        context.Write(output, "0");
                    }
                    context.WriteLine(output, ");");
                    break;
                case IfStmt ifStmt:
                    context.Write(output, "if (PrtPrimGetBool(");
                    WriteExpr(context, function, ifStmt.Condition, output);
                    context.WriteLine(output, "))");
                    WriteStmt(context, function, ifStmt.ThenBranch, output);
                    if (ifStmt.ElseBranch != null)
                    {
                        context.WriteLine(output, "else");
                        WriteStmt(context, function, ifStmt.ElseBranch, output);
                    }

                    break;
                case InsertStmt insertStmt:
                    break;
                case MoveAssignStmt moveAssignStmt:
                    break;
                case NoStmt _:
                    return;
                case PopStmt _:
                    context.WriteLine(output, "PrtFreeTriggerPayload(p_this);");
                    context.WriteLine(output, "PrtPop(context);");
                    context.WriteLine(output, "goto p_return;");
                    break;
                case PrintStmt printStmt:
                    WritePrintStmt(context, output, printStmt, function);
                    break;
                case RaiseStmt raiseStmt:
                    context.WriteLine(output, "PrtFreeTriggerPayload(p_this);");
                    context.Write(output, "PrtRaise(p_this, ");
                    WriteExpr(context, function, raiseStmt.PEvent, output);
                    context.Write(output, $", {raiseStmt.Payload.Count}");
                    foreach (IPExpr pExpr in raiseStmt.Payload)
                    {
                        Debug.Assert(pExpr is VariableAccessExpr);
                        var argVar = (VariableAccessExpr) pExpr;
                        context.Write(output, $", &{GetPrtNameForDecl(context, argVar.Variable)}");
                    }
                    context.WriteLine(output, ");");

                    Debug.Assert(raiseStmt.PEvent is VariableAccessExpr);
                    var raiseEventVar = (VariableAccessExpr) raiseStmt.PEvent;
                    context.WriteLine(output, $"{GetPrtNameForDecl(context, raiseEventVar.Variable)} = NULL;");
                    context.WriteLine(output, "goto p_return;");
                    break;
                case ReceiveStmt receiveStmt:
                    break;
                case RemoveStmt removeStmt:
                    break;
                case ReturnStmt returnStmt:
                    if (returnStmt.ReturnValue != null)
                    {
                        context.WriteLine(output, $"PrtFreeValue({FunResultValName});");
                        context.Write(output, $"{FunResultValName} = ");
                        WriteExpr(context, function, returnStmt.ReturnValue, output);
                        context.WriteLine(output, ";");
                    }

                    context.WriteLine(output, "goto p_return;");
                    break;
                case SendStmt sendStmt:
                    context.Write(output, $"PrtSendInternal(context, PrtGetMachine(context->process, ");
                    WriteExpr(context, function, sendStmt.MachineExpr, output);
                    context.Write(output, "), ");
                    WriteExpr(context, function, sendStmt.Evt, output);
                    context.Write(output, $", {sendStmt.ArgsList.Count}");
                    foreach (IPExpr sendArgExpr in sendStmt.ArgsList)
                    {
                        Debug.Assert(sendArgExpr is IVariableRef);
                        var argVar = (IVariableRef) sendArgExpr;
                        context.Write(output, $", &{GetPrtNameForDecl(context, argVar.Variable)}");
                    }
                    context.WriteLine(output, ");");

                    Debug.Assert(sendStmt.Evt is VariableAccessExpr);
                    var sendEventVar = (VariableAccessExpr) sendStmt.Evt;
                    context.WriteLine(output, $"{GetPrtNameForDecl(context, sendEventVar.Variable)} = NULL;");
                    break;
                case SwapAssignStmt swapAssignStmt:
                    break;
                case WhileStmt whileStmt:
                    context.Write(output, "while (PrtPrimGetBool(");
                    WriteExpr(context, function, whileStmt.Condition, output);
                    context.WriteLine(output, "))");
                    WriteStmt(context, function, whileStmt.Body, output);
                    break;
            }

            context.WriteLine(output);
        }

        private void WritePrintStmt(CompilationContext context, TextWriter output, PrintStmt printStmt1, Function function)
        {
            // format is {str0, n1, str1, n2, ..., nK, strK}
            var printMessageParts = ParsePrintMessage(printStmt1.Message);

            // Optimize for simple case.
            if (printMessageParts.Length == 1)
            {
                context.Write(output, "PrtPrintf(\"");
                context.Write(output, (string) printMessageParts[0]);
                context.WriteLine(output, "\");");
                return;
            }

            // Otherwise build full parameter pack...
            int k = (printMessageParts.Length - 1) / 2;
            context.Write(output, "PrtFormatPrintf(\"");
            context.Write(output, (string) printMessageParts[0]);
            context.Write(output, "\", ");
            context.Write(output, printStmt1.Args.Count.ToString());
            foreach (IPExpr printArg in printStmt1.Args)
            {
                context.Write(output, ", ");
                WriteExpr(context, function, printArg, output);
            }

            context.Write(output, ", ");
            context.Write(output, k.ToString());
            for (var i = 0; i < k; i++)
            {
                var n = (int) printMessageParts[1 + 2 * i];
                var s = (string) printMessageParts[1 + 2 * i + 1];
                context.Write(output, ", ");
                context.Write(output, n.ToString());
                context.Write(output, ", \"");
                context.Write(output, s);
                context.Write(output, "\"");
            }

            context.WriteLine(output, ");");
        }

        public static object[] ParsePrintMessage(string message)
        {
            var parts = new List<object>();
            var sb = new StringBuilder();
            for (var i = 0; i < message.Length; i++)
            {
                if (message[i] == '{')
                {
                    if (i + 1 == message.Length)
                    {
                        throw new ArgumentException("unmatched opening brace", nameof(message));
                    }

                    if (message[i + 1] == '{')
                    {
                        i++;
                        sb.Append(message[i]);
                    }
                    else if (char.IsDigit(message[i + 1]))
                    {
                        parts.Add(sb.ToString());
                        sb.Clear();

                        var position = 0;
                        while (++i < message.Length && '0' <= message[i] && message[i] <= '9')
                        {
                            position = 10 * position + (message[i] - '0');
                        }

                        if (i == message.Length || message[i] != '}')
                        {
                            throw new ArgumentException("unmatched opening brace in position expression", nameof(message));
                        }

                        parts.Add(position);
                    }
                    else
                    {
                        throw new ArgumentException("opening brace not followed by digits", nameof(message));
                    }
                }
                else if (message[i] == '}')
                {
                    if (i + 1 == message.Length || message[i + 1] != '}')
                    {
                        throw new ArgumentException("unmatched closing brace", nameof(message));
                    }

                    sb.Append(message[i]);
                    i++;
                }
                else
                {
                    sb.Append(message[i]);
                }
            }

            parts.Add(sb.ToString());
            return parts.ToArray();
        }

        private void WriteExpr(CompilationContext context, Function function, IPExpr expr, TextWriter output)
        {
            switch (expr)
            {
                case CloneExpr cloneExpr:
                    context.Write(output, "PrtCloneValue(");
                    WriteExpr(context, function, cloneExpr.Term, output);
                    context.Write(output, ")");
                    break;
                case BinOpExpr binOpExpr:
                    IPExpr binOpLhs = binOpExpr.Lhs;
                    IPExpr binOpRhs = binOpExpr.Rhs;
                    BinOpType binOpType = binOpExpr.Operation;

                    // TODO: if getting a literal, replace with literal.
                    if (binOpType == BinOpType.Eq || binOpType == BinOpType.Neq)
                    {
                        string negate = binOpType == BinOpType.Eq ? "" : "!";
                        context.Write(output, $"PrtMkBoolValue({negate}PrtIsEqualValue(");
                        WriteExpr(context, function, binOpLhs, output);
                        context.Write(output, ", ");
                        WriteExpr(context, function, binOpRhs, output);
                        context.Write(output, "))");
                    }
                    else
                    {
                        var (binOpGetter, _) = GetTypeStructureFuns(binOpLhs.Type);
                        var (_, binOpBuilder) = GetTypeStructureFuns(binOpExpr.Type);

                        context.Write(output, $"{binOpBuilder}(");

                        context.Write(output, $"{binOpGetter}(");
                        WriteExpr(context, function, binOpLhs, output);
                        context.Write(output, ")");

                        context.Write(output, $" {BinOpToStr(binOpType)} ");

                        context.Write(output, $"{binOpGetter}(");
                        WriteExpr(context, function, binOpRhs, output);
                        context.Write(output, ")");

                        context.Write(output, ")");
                    }

                    break;
                case BoolLiteralExpr boolLiteralExpr:
                    string boolLiteralName = context.RegisterLiteral(function, boolLiteralExpr.Value);
                    context.Write(output, $"(&{boolLiteralName})");
                    break;
                case CastExpr castExpr:
                    break;
                case CoerceExpr coerceExpr:
                    string coerceCtor;
                    switch (coerceExpr.NewType)
                    {
                        case PrimitiveType primitiveType when PrimitiveType.Int.IsSameTypeAs(primitiveType):
                            coerceCtor = "PrtMkIntValue";
                            break;
                        case PrimitiveType primitiveType when PrimitiveType.Float.IsSameTypeAs(primitiveType):
                            coerceCtor = "PrtMkFloatValue";
                            break;
                        default:
                            throw new ArgumentException(nameof(coerceExpr.NewType));
                    }

                    string coerceUnpack;
                    switch (coerceExpr.SubExpr.Type)
                    {
                        case PrimitiveType primitiveType when PrimitiveType.Int.IsSameTypeAs(primitiveType):
                            coerceUnpack = "PrtPrimGetInt";
                            break;
                        case PrimitiveType primitiveType when PrimitiveType.Float.IsSameTypeAs(primitiveType):
                            coerceUnpack = "PrtPrimGetFloat";
                            break;
                        default:
                            throw new ArgumentException(nameof(coerceExpr.SubExpr));
                    }

                    context.Write(output, $"{coerceCtor}({coerceUnpack}(");
                    WriteExpr(context, function, coerceExpr.SubExpr, output);
                    context.Write(output, "))");
                    break;
                case ContainsKeyExpr containsKeyExpr:
                    break;
                case CtorExpr ctorExpr:
                    context.Write(output, $"PrtCloneValue(PrtMkInterface(context, {context.GetNumberForInterface(ctorExpr.Interface)}, {ctorExpr.Arguments.Count}");
                    foreach (IPExpr pExpr in ctorExpr.Arguments)
                    {
                        Debug.Assert(pExpr is VariableAccessExpr);
                        var argVar = (VariableAccessExpr) pExpr;
                        context.Write(output, $", &{GetPrtNameForDecl(context, argVar.Variable)}");
                    }
                    context.Write(output, ")->id)");
                    break;
                case DefaultExpr defaultExpr:
                    string nameForDefaultType = context.Names.GetNameForType(defaultExpr.Type);
                    context.Write(output, $"PrtMkDefaultValue({nameForDefaultType})");
                    break;
                case EnumElemRefExpr enumElemRefExpr:
                    context.Write(output, GetPrtNameForDecl(context, enumElemRefExpr.Value));
                    break;
                case EventRefExpr eventRefExpr:
                    context.Write(output, $"(&{GetPrtNameForDecl(context, eventRefExpr.Value)}.value)");
                    break;
                case FairNondetExpr _:
                    context.Write(output, "(PrtMkNondetBoolValue())");
                    break;
                case FloatLiteralExpr floatLiteralExpr:
                    string floaLiteralName = context.RegisterLiteral(function, floatLiteralExpr.Value);
                    context.Write(output, $"(&{floaLiteralName})");
                    break;
                case FunCallExpr funCallExpr:
                    string funImplName = context.Names.GetNameForFunctionImpl(funCallExpr.Function);
                    var funArgs = funCallExpr.Arguments.Cast<ILinearRef>().ToList();
                    var argSetup = funArgs.Select((arg, i) => $"({FunCallArgsArrayName}[{i}] = &{GetPrtNameForDecl(context, arg.Variable)})");
                    var funCall = new[] {$"({FunCallRetValName} = {funImplName}(context, {FunCallArgsArrayName}))"};
                    var argsFree = funArgs.Where(arg => arg.LinearType.Equals(LinearType.Move))
                                          .Select(arg => GetPrtNameForDecl(context, arg.Variable))
                                          .Select(varName => $"(PrtFreeValue({varName}), {varName} = NULL)");
                    var resRetrieve = new[] {$"({FunCallRetValName})"};
                    var fullCall = string.Join(", ", argSetup.Concat(funCall).Concat(argsFree).Concat(resRetrieve));
                    context.Write(output, $"({fullCall})");
                    break;
                case IntLiteralExpr intLiteralExpr:
                    string intLiteralName = context.RegisterLiteral(function, intLiteralExpr.Value);
                    context.Write(output, $"(&{intLiteralName})");
                    break;
                case KeysExpr keysExpr:
                    break;
                case LinearAccessRefExpr linearAccessRefExpr:
                    break;
                case MapAccessExpr mapAccessExpr:
                    break;
                case NamedTupleAccessExpr namedTupleAccessExpr:
                    break;
                case NamedTupleExpr namedTupleExpr:
                    break;
                case NondetExpr _:
                    context.Write(output, "(PrtMkNondetBoolValue())");
                    break;
                case NullLiteralExpr nullLiteralExpr:
                    context.Write(output, "PrtMkNullValue()");
                    break;
                case SeqAccessExpr seqAccessExpr:
                    break;
                case SizeofExpr sizeofExpr:
                    break;
                case ThisRefExpr thisRefExpr:
                    context.Write(output, "p_this->id");
                    break;
                case TupleAccessExpr tupleAccessExpr:
                    context.Write(output, "PrtTupleGet(");
                    WriteExpr(context, function, tupleAccessExpr.SubExpr, output);
                    context.Write(output, $", {tupleAccessExpr.FieldNo})");
                    break;
                case UnaryOpExpr unaryOpExpr:
                    var (unOpGetter, unOpBuilder) = GetTypeStructureFuns(unaryOpExpr.Type);
                    context.Write(output, $"{unOpBuilder}(");

                    context.Write(output, UnOpToStr(unaryOpExpr.Operation));
                    context.Write(output, $"{unOpGetter}(");
                    WriteExpr(context, function, unaryOpExpr.SubExpr, output);
                    context.Write(output, ")");

                    context.Write(output, ")");
                    break;
                case UnnamedTupleExpr unnamedTupleExpr:
                    var utArgs = (IReadOnlyList<IVariableRef>)unnamedTupleExpr.TupleFields;
                    var utTypeName = context.Names.GetNameForType(unnamedTupleExpr.Type);
                    var tupleBody = string.Join(", ", utArgs.Select(v => $"&{GetPrtNameForDecl(context, v.Variable)}"));
                    context.Write(output, $"(PrtMkTuple(&{utTypeName}, {tupleBody}))");
                    break;
                case ValuesExpr valuesExpr:
                    break;
                case VariableAccessExpr variableAccessExpr:
                    VariableRole variableRole = variableAccessExpr.Variable.Role;

                    if (variableRole.HasFlag(VariableRole.Param))
                    {
                        // dereference, since params are passed by reference.
                        context.Write(output, "*");
                        context.Write(output, GetPrtNameForDecl(context, variableAccessExpr.Variable));
                    }

                    if (variableRole.HasFlag(VariableRole.Field))
                    {
                        // TODO: is this always correct? I think the iterator ordering of a List should be consistent...
                        var varIdx = function.Owner.Fields.ToList().IndexOf(variableAccessExpr.Variable);
                        context.Write(output, $"p_this->varValues[{varIdx}]");
                    }

                    if (variableRole.HasFlag(VariableRole.Temp) || variableRole.HasFlag(VariableRole.Local))
                    {
                        context.Write(output, GetPrtNameForDecl(context, variableAccessExpr.Variable));
                    }

                    break;
            }
        }

        private string UnOpToStr(UnaryOpType operation)
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

        private static (string, string) GetTypeStructureFuns(PLanguageType type)
        {
            string binOpGetter;
            string binOpBuilder;
            if (type.IsSameTypeAs(PrimitiveType.Int) || type.TypeKind.Equals(TypeKind.Enum))
            {
                binOpGetter = "PrtPrimGetInt";
                binOpBuilder = "PrtMkIntValue";
            }
            else if (type.IsSameTypeAs(PrimitiveType.Bool))
            {
                binOpGetter = "PrtPrimGetBool";
                binOpBuilder = "PrtMkBoolValue";
            }
            else if (type.IsSameTypeAs(PrimitiveType.Float))
            {
                binOpGetter = "PrtPrimGetFloat";
                binOpBuilder = "PrtMkFloatValue";
            }
            else
            {
                throw new ArgumentException($"cannot destructure type {type.CanonicalRepresentation}", nameof(type));
            }

            return (binOpGetter, binOpBuilder);
        }

        private string BinOpToStr(BinOpType binOpType)
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

        private string WriteTypeDefinition(CompilationContext context, PLanguageType type, TextWriter output)
        {
            type = type.Canonicalize();

            string typeGenName = context.Names.GetNameForType(type);

            if (context.WrittenTypes.Contains(type))
            {
                return typeGenName;
            }

            switch (type)
            {
                case BoundedType boundedType:
                    context.WriteLine(output, $"// TODO: implement types like {boundedType.CanonicalRepresentation}");
                    break;
                case EnumType _:
                    context.WriteLine(output, $"static PRT_TYPE {typeGenName} = {{ PRT_KIND_INT, {{ NULL }} }};");
                    break;
                case ForeignType foreignType:
                    context.WriteLine(output, $"// TODO: implement types like {foreignType.CanonicalRepresentation}");
                    break;
                case MapType mapType:
                    string mapKeyTypeName = WriteTypeDefinition(context, mapType.KeyType, output);
                    string mapValueTypeName = WriteTypeDefinition(context, mapType.ValueType, output);
                    string mapTypeDeclName = context.Names.GetTemporaryName("MAPTYPE");
                    context.WriteLine(output, $"static PRT_MAPTYPE {mapTypeDeclName} = {{ &{mapKeyTypeName}, &{mapValueTypeName} }};");
                    context.WriteLine(output, $"static PRT_TYPE {typeGenName} = {{ PRT_KIND_MAP, {{ .map = &{mapTypeDeclName} }} }};");
                    break;
                case NamedTupleType namedTupleType:
                    string ntNamesArrayName = context.Names.GetTemporaryName("NMDTUP_N");
                    string ntTypesArrayName = context.Names.GetTemporaryName("NMDTUP_T");
                    string ntStructName = context.Names.GetTemporaryName("NMDTUP");
                    var typeDeclNames = namedTupleType.Types.Select(t => WriteTypeDefinition(context, t, output));
                    context.WriteLine(
                        output,
                        $"static PRT_STRING {ntNamesArrayName}[] = {{ {string.Join(", ", namedTupleType.Names.Select(name => "\"" + name + "\""))} }};");
                    context.WriteLine(
                        output,
                        $"static PRT_TYPE* {ntTypesArrayName}[] = {{ {string.Join(", ", typeDeclNames.Select(name => "&" + name))} }};");
                    context.WriteLine(
                        output,
                        $"static PRT_NMDTUPTYPE {ntStructName} = {{ {namedTupleType.Types.Count}U, {ntNamesArrayName}, {ntTypesArrayName} }};");
                    context.WriteLine(output, $"static PRT_TYPE {typeGenName} = {{ PRT_KIND_NMDTUP, {{ .nmTuple = &{ntStructName} }} }};");
                    break;
                case PermissionType _:
                    // TODO: implement full permission types in runtime
                    context.WriteLine(output, "// TODO: implement full permission types in runtime");
                    context.WriteLine(output, $"static PRT_TYPE {typeGenName} = {{ PRT_KIND_MACHINE, {{ NULL }} }};");
                    break;
                case PrimitiveType primitiveType when Equals(primitiveType, PrimitiveType.Null):
                    context.WriteLine(output, $"static PRT_TYPE {typeGenName} = {{ PRT_KIND_NULL, {{ NULL }} }};");
                    break;
                case PrimitiveType primitiveType when Equals(primitiveType, PrimitiveType.Int):
                    context.WriteLine(output, $"static PRT_TYPE {typeGenName} = {{ PRT_KIND_INT, {{ NULL }} }};");
                    break;
                case PrimitiveType primitiveType when Equals(primitiveType, PrimitiveType.Float):
                    context.WriteLine(output, $"static PRT_TYPE {typeGenName} = {{ PRT_KIND_FLOAT, {{ NULL }} }};");
                    break;
                case PrimitiveType primitiveType when Equals(primitiveType, PrimitiveType.Bool):
                    context.WriteLine(output, $"static PRT_TYPE {typeGenName} = {{ PRT_KIND_BOOL, {{ NULL }} }};");
                    break;
                case PrimitiveType primitiveType when Equals(primitiveType, PrimitiveType.Machine):
                    context.WriteLine(output, $"static PRT_TYPE {typeGenName} = {{ PRT_KIND_MACHINE, {{ NULL }} }};");
                    break;
                case PrimitiveType primitiveType when Equals(primitiveType, PrimitiveType.Event):
                    context.WriteLine(output, $"static PRT_TYPE {typeGenName} = {{ PRT_KIND_EVENT, {{ NULL }} }};");
                    break;
                case PrimitiveType primitiveType:
                    context.WriteLine(output, $"// TODO: implement types like {primitiveType.CanonicalRepresentation}");
                    break;
                case SequenceType sequenceType:
                    string seqElementTypeName = WriteTypeDefinition(context, sequenceType.ElementType, output);
                    string seqTypeDeclName = context.Names.GetTemporaryName("SEQTYPE");
                    context.WriteLine(output, $"static PRT_SEQTYPE {seqTypeDeclName} = {{ &{seqElementTypeName} }};");
                    context.WriteLine(output, $"static PRT_TYPE {typeGenName} = {{ PRT_KIND_SEQ, {{ .seq = &{seqTypeDeclName} }} }};");
                    break;
                case TupleType tupleType:
                    string tupTypesArrayName = context.Names.GetTemporaryName("TUP_T");
                    string tupStructName = context.Names.GetTemporaryName("TUP");
                    var tupTypeDeclNames = tupleType.Types.Select(t => WriteTypeDefinition(context, t, output));
                    context.WriteLine(output, $"static PRT_TYPE* {tupTypesArrayName}[] = {{ {string.Join(", ", tupTypeDeclNames.Select(n => "&" + n))} }};");
                    context.WriteLine(output, $"static PRT_TUPTYPE {tupStructName} = {{ {tupleType.Types.Count}U, {tupTypesArrayName} }};");
                    context.WriteLine(output, $"static PRT_TYPE {typeGenName} = {{ PRT_KIND_TUPLE, {{ .tuple = &{tupStructName} }} }};");
                    break;
                case TypeDefType _:
                    throw new ArgumentException("typedefs shouldn't be possible after canonicalization", nameof(type));
            }

            context.WrittenTypes.Add(type);
            return typeGenName;
        }

        private void WriteSourcePrologue(CompilationContext context, TextWriter output)
        {
            context.WriteLine(output, $"#include \"{context.HeaderFileName}\"");
            context.WriteLine(output);
        }

        private static string GetPrtNameForDecl(CompilationContext context, IPDecl decl)
        {
            if (decl == null)
            {
                throw new ArgumentNullException(nameof(decl));
            }

            var computedPrefix = "";
            switch (decl)
            {
                case EnumElem enumElem:
                    computedPrefix = $"{enumElem.ParentEnum.Name}_";
                    break;
                case PEvent pEvent:
                    if (pEvent.IsNullEvent)
                    {
                        return "_P_EVENT_NULL_STRUCT";
                    }

                    if (pEvent.IsHaltEvent)
                    {
                        return "_P_EVENT_HALT_STRUCT";
                    }

                    break;
                case Implementation impl:
                    return $"P_GEND_IMPL_{impl.Name}";
            }

            if (DeclNameParts.TryGetValue(decl.GetType(), out string prefix))
            {
                prefix += "_";
            }
            else
            {
                prefix = "";
            }

            return context.Names.GetNameForNode(decl, prefix + computedPrefix);
        }

        private class CompilationContext
        {
            private bool lineHasBeenIndented;
            private readonly Dictionary<Interface, int> interfaceNumbering = new Dictionary<Interface, int>();
            private readonly Dictionary<Machine, int> machineNumbering = new Dictionary<Machine, int>();
            private readonly Dictionary<Machine, Dictionary<State, int>> stateNumbering = new Dictionary<Machine, Dictionary<State, int>>();

            public CompilationContext(string projectName)
            {
                ProjectName = projectName;
                HeaderFileName = $"{projectName}.h";
                SourceFileName = $"{projectName}.c";
                Names = new NameManager($"P_{projectName.ToUpperInvariant()}_");
                registeredInts = new ValueInternmentManager<int>(Names);
                registeredFloats = new ValueInternmentManager<double>(Names);
                registeredBools = new ValueInternmentManager<bool>(Names);
            }

            public string ProjectName { get; }
            public string HeaderFileName { get; }
            public string SourceFileName { get; }
            public NameManager Names { get; }
            public IEnumerable<PLanguageType> UsedTypes => Names.UsedTypes;
            public HashSet<PLanguageType> WrittenTypes { get; } = new HashSet<PLanguageType>();
            public int IndentationLevel { get; set; }

            public int GetNumberForInterface(Interface pInterface)
            {
                if (interfaceNumbering.TryGetValue(pInterface, out int name))
                {
                    return name;
                }

                name = interfaceNumbering.Count;
                interfaceNumbering.Add(pInterface, name);
                return name;
            }

            public int GetNumberForMachine(Machine machine)
            {
                if (machineNumbering.TryGetValue(machine, out int name))
                {
                    return name;
                }

                name = machineNumbering.Count;
                machineNumbering.Add(machine, name);
                return name;
            }

            public int GetNumberForState(State state)
            {
                var machine = state.OwningMachine;
                if (!stateNumbering.TryGetValue(machine, out var internalNumbering))
                {
                    internalNumbering = new Dictionary<State, int>();
                    stateNumbering.Add(machine, internalNumbering);
                }

                if (internalNumbering.TryGetValue(state, out var name))
                {
                    return name;
                }

                name = internalNumbering.Count;
                internalNumbering.Add(state, name);
                return name;
            }

            public void WriteLine(TextWriter output, string format = "")
            {
                // Unindent for every } at the beginning of the line, save the index 
                // of one past the last leading }.
                int i;
                for (i = 0; i < format.Length; i++)
                {
                    if (format[i] == '}')
                    {
                        IndentationLevel--;
                    }
                    else if (!char.IsWhiteSpace(format[i]))
                    {
                        break;
                    }
                }

                // Do not indent preprocessor lines.
                var indentation = new string(' ', 4 * IndentationLevel);
                if (format.StartsWith("#") || lineHasBeenIndented)
                {
                    indentation = "";
                }

                output.WriteLine(indentation + format);
                lineHasBeenIndented = false;

                // Compute indentation for future lines starting from after last leading }.
                for (; i < format.Length; i++)
                {
                    if (format[i] == '{')
                    {
                        IndentationLevel++;
                    }
                    else if (format[i] == '}')
                    {
                        IndentationLevel--;
                    }
                }
            }

            public void Write(TextWriter output, string format)
            {
                // Unindent for every } at the beginning of the line, save the index 
                // of one past the last leading }.
                int i;
                for (i = 0; i < format.Length; i++)
                {
                    if (format[i] == '}')
                    {
                        IndentationLevel--;
                    }
                    else if (!char.IsWhiteSpace(format[i]))
                    {
                        break;
                    }
                }

                // Do not indent preprocessor lines.
                var indentation = new string(' ', 4 * IndentationLevel);
                if (format.StartsWith("#") || lineHasBeenIndented)
                {
                    indentation = "";
                }

                output.Write(indentation + format);
                lineHasBeenIndented = true;

                // Compute indentation for future lines starting from after last leading }.
                for (; i < format.Length; i++)
                {
                    if (format[i] == '{')
                    {
                        IndentationLevel++;
                    }
                    else if (format[i] == '}')
                    {
                        IndentationLevel--;
                    }
                }
            }
            
            private readonly ValueInternmentManager<int> registeredInts;
            private readonly ValueInternmentManager<double> registeredFloats;
            private readonly ValueInternmentManager<bool> registeredBools;

            public string RegisterLiteral(Function function, int value)
            {
                return registeredInts.RegisterValue(function, value);
            }

            public IEnumerable<KeyValuePair<int, string>> GetRegisteredIntLiterals(Function function)
            {
                return registeredInts.GetValues(function);
            }

            internal string RegisterLiteral(Function function, double value)
            {
                return registeredFloats.RegisterValue(function, value);
            }

            public IEnumerable<KeyValuePair<double, string>> GetRegisteredFloatLiterals(Function function)
            {
                return registeredFloats.GetValues(function);
            }

            public string RegisterLiteral(Function function, bool value)
            {
                return registeredBools.RegisterValue(function, value);
            }

            public IEnumerable<KeyValuePair<bool, string>> GetRegisteredBoolLiterals(Function function)
            {
                return registeredBools.GetValues(function);
            }
        }

        #region Header writing routines

        private static void WriteGenericHeader(CompilationContext context, TextWriter output)
        {
            string includeGuardMacro = $"P_{Regex.Replace(context.ProjectName.ToUpperInvariant(), @"\s+", "")}_H_";
            context.WriteLine(output, "#pragma once");
            context.WriteLine(output, $"#ifndef {includeGuardMacro}");
            context.WriteLine(output, $"#define {includeGuardMacro}");
            context.WriteLine(output, "#include \"Prt.h\"");
            context.WriteLine(output);
            context.WriteLine(output, "#ifdef __cplusplus");
            context.WriteLine(output, "extern \"C\" {");
            context.WriteLine(output, "#endif");
            context.WriteLine(output);
        }

        private static void WriteForwardDeclaration(CompilationContext context, TextWriter output, IPDecl decl)
        {
            string declName = GetPrtNameForDecl(context, decl);
            switch (decl)
            {
                case EnumElem enumElem:
                    // TODO: do we write enum elements here?
                    break;
                case Function _:
                    context.WriteLine(output, $"extern PRT_FUNDECL {declName};");
                    break;
                case Implementation _:
                    context.WriteLine(output, $"extern PRT_PROGRAMDECL {declName};");
                    break;
                case Interface _:
                    context.WriteLine(output, $"extern PRT_INTERFACEDECL {declName};");
                    break;
                case Machine _:
                    context.WriteLine(output, $"extern PRT_MACHINEDECL {declName};");
                    break;
                case NamedEventSet namedEventSet:
                    context.WriteLine(output, $"// DECL(NamedEventSet, {decl.Name}) => {declName}");
                    break;
                case NamedModule namedModule:
                    context.WriteLine(output, $"// DECL(NamedModule, {decl.Name}) => {declName}");
                    break;
                case PEnum pEnum:
                    context.WriteLine(
                        output,
                        $"typedef enum {declName} {{ {string.Join(", ", pEnum.Values.Select(val => $"{GetPrtNameForDecl(context, val)} = {val.Value}"))} }} {declName};");
                    context.WriteLine(output);
                    break;
                case PEvent pEvent:
                    if (!pEvent.IsBuiltIn)
                    {
                        context.WriteLine(output, $"extern PRT_EVENTDECL {declName};");
                    }

                    break;
                case RefinementTest refinementTest:
                    context.WriteLine(output, $"// DECL(RefinementTest, {decl.Name}) => {declName}");
                    break;
                case SafetyTest safetyTest:
                    context.WriteLine(output, $"// DECL(SafetyTest, {decl.Name}) => {declName}");
                    break;
                case TypeDef typeDef:
                    context.WriteLine(output, $"extern PRT_TYPE* {declName};");
                    break;
                case Variable variable:
                    throw new ArgumentException("can't have global P variables", nameof(decl));
                case State state:
                    context.WriteLine(output, $"// DECL(State, {decl.Name}) => {declName}");
                    break;
                case StateGroup stateGroup:
                    context.WriteLine(output, $"// DECL(StateGroup, {decl.Name}) => {declName}");
                    break;
            }
        }

        private static void WriteGenericFooter(CompilationContext context, TextWriter output)
        {
            string includeGuardMacro = $"P_{Regex.Replace(context.ProjectName.ToUpperInvariant(), @"\s+", "")}_H_";
            context.WriteLine(output);
            context.WriteLine(output, "#ifdef __cplusplus");
            context.WriteLine(output, "}");
            context.WriteLine(output, "#endif");
            context.WriteLine(output);
            context.WriteLine(output, $"#endif // {includeGuardMacro}");
        }

        #endregion
    }
}
