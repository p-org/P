using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Pc.Backend.ASTExt;
using Microsoft.Pc.TypeChecker;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.Expressions;
using Microsoft.Pc.TypeChecker.AST.Statements;
using Microsoft.Pc.TypeChecker.AST.States;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.Backend.Prt
{
    public class PrtCodeGenerator : ICodeGenerator
    {
        private const string FunCallArgsArrayName = "_P_GEN_funargs";
        private const string FunCallRetValName = "_P_GEN_funval";
        private const string FunResultValName = "_P_GEN_retval";
        private const string FunNullStaticName = "_P_GEN_null";

        public IEnumerable<CompiledFile> GenerateCode(ICompilationJob job, Scope globalScope)
        {
            var context = new CompilationContext(job);
            CompiledFile cHeader = GenerateHeaderFile(context, globalScope);
            CompiledFile cSource = GenerateSourceFile(context, globalScope);
            return new List<CompiledFile> {cHeader, cSource};
        }

        #region Top-level generation methods

        private static CompiledFile GenerateHeaderFile(CompilationContext context, Scope globalScope)
        {
            var cHeader = new CompiledFile(context.HeaderFileName);

            WriteHeaderPrologue(context, cHeader.Stream);
            foreach (IPDecl decl in globalScope.AllDecls)
            {
                WriteExternDeclaration(context, cHeader.Stream, decl);
            }

            WriteHeaderEpilogue(context, cHeader.Stream);
            return cHeader;
        }

        private static CompiledFile GenerateSourceFile(CompilationContext context, Scope globalScope)
        {
            var cSource = new CompiledFile(context.SourceFileName);
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
            foreach (PLanguageType type in context.UsedTypes.ToArray())
            {
                WriteTypeDefinition(context, type, cSource.Stream);
            }

            context.WriteLine(cSource.Stream);

            context.WriteLine(cSource.Stream, "// Function implementation prototypes:");
            foreach (Function function in globalScope.GetAllMethods())
            {
                string functionName = context.Names.GetNameForFunctionImpl(function);
                context.WriteLine(cSource.Stream, $"PRT_VALUE* {functionName}(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);");
                if (!function.IsForeign)
                {
                    context.WriteLine(cSource.Stream, $"extern PRT_FUNDECL {context.Names.GetNameForDecl(function)};");
                }
                context.WriteLine(cSource.Stream);
            }

            context.WriteLine(cSource.Stream);

            // Append all the function bodies and declaration structs (which have forward declarations in the header)
            cSource.Stream.GetStringBuilder().Append(bodyWriter);

            // Finally, write the overall program decl
            WriteProgramDecl(context, globalScope, cSource.Stream);
            return cSource;
        }

        #endregion

        #region Declaration level methods

        private static void WriteSourcePrologue(CompilationContext context, TextWriter output)
        {
            context.WriteLine(output, $"#include \"{context.HeaderFileName}\"");
            context.WriteLine(output);
        }

        private static void WriteSourceDecl(CompilationContext context, IPDecl decl, TextWriter output)
        {
            string declName = context.Names.GetNameForDecl(decl);
            var declLocation = context.LocationResolver.GetLocation(decl);
            switch (decl)
            {
                case EnumElem _:
                    // Member of a type. Instantitated by usage.
                    return;
                case PEnum _:
                    // Declares a type. Instantitated by usage.
                    return;
                case Function function:
                    if (!function.IsForeign)
                    {
                        WriteNormalFunction(context, output, function);
                    }
                    break;
                case Implementation _:
                    // does not produce a struct definition - aside from ProgramDecl
                    return;
                case Interface @interface:
                    string ifaceRecvSetName;
                    if (@interface.ReceivableEvents is NamedEventSet set)
                    {
                        ifaceRecvSetName = context.Names.GetNameForDecl(set);
                    }
                    else
                    {
                        var interfaceEventSet = new NamedEventSet(@interface.Name + "_RECV", @interface.SourceLocation);
                        interfaceEventSet.AddEvents(@interface.ReceivableEvents.Events);
                        WriteSourceDecl(context, interfaceEventSet, output);
                        ifaceRecvSetName = context.Names.GetNameForDecl(interfaceEventSet);
                    }

                    context.WriteLine(output, $"#line {declLocation.Line} \"{declLocation.File.Name}\"");
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
                            Variable field = machineFields[i];
                            string sep = i == machineFields.Count - 1 ? "" : ",";
                            context.WriteLine(output, $"{{ \"{field.Name}\", &{context.Names.GetNameForType(field.Type)} }}{sep}");
                        }

                        context.WriteLine(output, "};");
                        context.WriteLine(output);
                    }

                    var machineStates = machine.AllStates().ToList();
                    var machineStatesInOrder = new State[machineStates.Count];
                    foreach (State state in machineStates)
                    {
                        WriteSourceDecl(context, state, output);
                        machineStatesInOrder[context.GetNumberForState(state)] = state;
                    }

                    string stateArrayName = context.Names.GetTemporaryName($"{machine.Name}_STATES");
                    string stateArrayBody =
                        string.Join(", ", machineStatesInOrder.Select(st => context.Names.GetNameForDecl(st)));
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
                        string methodArrayBody =
                            string.Join(", ", machineMethods.Select(m => $"&{context.Names.GetNameForDecl(m)}"));
                        context.WriteLine(output, $"PRT_FUNDECL* {methodArrayName}[] = {{ {methodArrayBody} }};");
                        context.WriteLine(output);
                    }

                    string machineRecvSetName = GetReceivesNameOrMkTemp(machine, context, output);
                    string machineSendSetName = GetSendsNameOrMkTemp(machine, context, output);
                    var machineCreatesName = "NULL";
                    if (machine.Creates.Interfaces.Any())
                    {
                        var createsInterfaces = machine.Creates.Interfaces.ToList();
                        machineCreatesName = context.Names.GetTemporaryName($"{machine.Name}_CREATES");
                        string createsArrayName = context.Names.GetTemporaryName($"{machine.Name}_CREATES_ARR");
                        string createsArrayBody = string.Join(", ", createsInterfaces.Select(context.GetNumberForInterface));
                        context.WriteLine(output, $"PRT_UINT32 {createsArrayName}[] = {{ {createsArrayBody} }};");
                        context.WriteLine(
                            output, $"PRT_INTERFACESETDECL {machineCreatesName} = {{ {createsInterfaces.Count}, {createsArrayName} }};");
                        machineCreatesName = "&" + machineCreatesName;
                    }

                    uint maxQueueSize = machine.Assert ?? uint.MaxValue;
                    context.WriteLine(output, $"#line {declLocation.Line} \"{declLocation.File.Name}\"");
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
                    var eventDeclNames = namedEventSet.Events.Select(x => "&" + context.Names.GetNameForDecl(x)).ToList();
                    string eventDeclArrBody = string.Join(", ", eventDeclNames);
                    eventDeclArrBody = string.IsNullOrEmpty(eventDeclArrBody) ? "NULL" : eventDeclArrBody;
                    context.WriteLine(output, $"#line {declLocation.Line} \"{declLocation.File.Name}\"");
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

                    context.WriteLine(output, $"#line {declLocation.Line} \"{declLocation.File.Name}\"");
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
                    context.WriteLine(output, $"#line {declLocation.Line} \"{declLocation.File.Name}\"");
                    context.WriteLine(output, $"PRT_TYPE* {declName} = &{context.Names.GetNameForType(typeDef.Type)};");
                    return;
                case Variable _:
                    // does not produce a struct definition
                    // handled by MachineDecl.
                    return;
                case State state:
                    string stateEntryFunName = state.Entry == null
                        ? "&_P_NO_OP"
                        : $"&{context.Names.GetNameForDecl(state.Entry)}";
                    string stateExitFunName =
                        state.Exit == null ? "&_P_NO_OP" : $"&{context.Names.GetNameForDecl(state.Exit)}";

                    int stateIndex = context.GetNumberForState(state);
                    PrtTranslationUtils.StateActionResults stateData = PrtTranslationUtils.BuildActionSets(context, state);

                    WriteSourceDecl(context, stateData.DefersSet, output);
                    WriteSourceDecl(context, stateData.TransSet, output);
                    WriteSourceDecl(context, stateData.DosSet, output);

                    var transArrName = "NULL";
                    if (stateData.Trans.Count != 0)
                    {
                        transArrName = context.Names.GetTemporaryName("TRANS");
                        context.WriteLine(output, $"PRT_TRANSDECL {transArrName}[] =");
                        context.WriteLine(output, "{");
                        for (var i = 0; i < stateData.Trans.Count; i++)
                        {
                            (PEvent triggerEvent, int destIndex, string transFunRef) = stateData.Trans[i];
                            string triggerName = context.Names.GetNameForDecl(triggerEvent);
                            string comma = i == stateData.Trans.Count - 1 ? "" : ",";
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
                            (PEvent triggerEvent, Function doFun) = stateData.Dos[i];
                            string triggerName = context.Names.GetNameForDecl(triggerEvent);
                            string comma = i == stateData.Dos.Count - 1 ? "" : ",";
                            string funName = doFun != null ? $"&{context.Names.GetNameForDecl(doFun)}" : "NULL";
                            context.WriteLine(output, $"{{ {stateIndex}, &{triggerName}, {funName} }}{comma}");
                        }

                        context.WriteLine(output, "};");
                        context.WriteLine(output);
                    }

                    context.WriteLine(output, $"#line {declLocation.Line} \"{declLocation.File.Name}\"");
                    context.WriteLine(output, $"#define {declName} \\");
                    context.WriteLine(output, "{ \\");
                    context.WriteLine(output, $"\"{state.QualifiedName}\", \\");
                    context.WriteLine(output, $"{stateData.Trans.Count}U, \\");
                    context.WriteLine(output, $"{stateData.Dos.Count}U, \\");
                    context.WriteLine(output, $"&{context.Names.GetNameForDecl(stateData.DefersSet)}, \\");
                    context.WriteLine(output, $"&{context.Names.GetNameForDecl(stateData.TransSet)}, \\");
                    context.WriteLine(output, $"&{context.Names.GetNameForDecl(stateData.DosSet)}, \\");
                    context.WriteLine(output, $"{transArrName}, \\");
                    context.WriteLine(output, $"{dosArrName}, \\");
                    context.WriteLine(output, $"{stateEntryFunName}, \\");
                    context.WriteLine(output, $"{stateExitFunName}, \\");
                    context.WriteLine(output, "}");
                    break;
                case StateGroup stateGroup:
                    foreach (State state in stateGroup.States)
                    {
                        WriteSourceDecl(context, state, output);
                    }

                    foreach (StateGroup subGroup in stateGroup.Groups)
                    {
                        WriteSourceDecl(context, subGroup, output);
                    }

                    break;
            }

            context.WriteLine(output);
        }

        private static void WriteNormalFunction(CompilationContext context, TextWriter output, Function function)
        {
            string declName = context.Names.GetNameForDecl(function);
            var declLocation = context.LocationResolver.GetLocation(function);

            string functionImplName = context.Names.GetNameForFunctionImpl(function);
            bool isAnon = string.IsNullOrEmpty(function.Name);
            string functionName = isAnon ? "NULL" : $"\"{function.Name}\"";
            var signature = function.Signature.ParameterTypes.ToList();
            Debug.Assert((isAnon && signature.Count <= 1) || !isAnon);
            string payloadType = isAnon && signature.Count == 1 ? $"&{context.Names.GetNameForType(signature[0])}" : "NULL";

            context.WriteLine(output, $"#line {declLocation.Line} \"{declLocation.File.Name}\"");
            context.WriteLine(output, $"PRT_VALUE* {functionImplName}(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)");
            context.WriteLine(output, "{");
            WriteFunctionBody(context, function, output);
            context.WriteLine(output, "}");
            context.WriteLine(output);
            context.WriteLine(output, $"PRT_FUNDECL {declName} =");
            context.WriteLine(output, "{");
            context.WriteLine(output, $"{functionName},"); // name of function in original program, NULL if anon
            context.WriteLine(output, $"&{functionImplName},"); // pointer to implementation
            context.WriteLine(output, $"{payloadType}"); // payload type for anonymous functions
            context.WriteLine(output, "};");
            context.WriteLine(output);
        }

        private static string WriteTypeDefinition(CompilationContext context, PLanguageType type, TextWriter output)
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
                    string foreignTypeName = foreignType.CanonicalRepresentation;

                    context.WriteLine(output, $"extern PRT_UINT64 PRT_FOREIGN_MKDEF_{foreignTypeName}_IMPL(void);");
                    context.WriteLine(output, $"extern PRT_UINT64 PRT_FOREIGN_CLONE_{foreignTypeName}_IMPL(PRT_UINT64);");
                    context.WriteLine(output, $"extern void PRT_FOREIGN_FREE_{foreignTypeName}_IMPL(PRT_UINT64);");
                    context.WriteLine(output, $"extern PRT_UINT32 PRT_FOREIGN_GETHASHCODE_{foreignTypeName}_IMPL(PRT_UINT64);");
                    context.WriteLine(output, $"extern PRT_BOOLEAN PRT_FOREIGN_ISEQUAL_{foreignTypeName}_IMPL(PRT_UINT64, PRT_UINT64);");
                    context.WriteLine(output, $"extern PRT_STRING PRT_FOREIGN_TOSTRING_{foreignTypeName}_IMPL(PRT_UINT64);");

                    string foreignTypeDeclName = context.Names.GetNameForForeignTypeDecl(foreignType);
                    context.WriteLine(output, $"static PRT_FOREIGNTYPEDECL {foreignTypeDeclName} = {{");
                    context.WriteLine(output, "0U,");
                    context.WriteLine(output, $"\"{foreignTypeName}\",");
                    context.WriteLine(output, $"PRT_FOREIGN_MKDEF_{foreignTypeName}_IMPL,");
                    context.WriteLine(output, $"PRT_FOREIGN_CLONE_{foreignTypeName}_IMPL,");
                    context.WriteLine(output, $"PRT_FOREIGN_FREE_{foreignTypeName}_IMPL,");
                    context.WriteLine(output, $"PRT_FOREIGN_GETHASHCODE_{foreignTypeName}_IMPL,");
                    context.WriteLine(output, $"PRT_FOREIGN_ISEQUAL_{foreignTypeName}_IMPL,");
                    context.WriteLine(output, $"PRT_FOREIGN_TOSTRING_{foreignTypeName}_IMPL,");
                    context.WriteLine(output, "};");
                    context.WriteLine(output, $"PRT_TYPE {typeGenName} = {{ PRT_KIND_FOREIGN, {{ .foreignType = &{foreignTypeDeclName} }} }};");
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
                case PrimitiveType primitiveType when Equals(primitiveType, PrimitiveType.Any):
                    context.WriteLine(output, $"static PRT_TYPE {typeGenName} = {{ PRT_KIND_ANY, {{ NULL }} }};");
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
                    context.WriteLine(
                        output,
                        $"static PRT_TYPE* {tupTypesArrayName}[] = {{ {string.Join(", ", tupTypeDeclNames.Select(n => "&" + n))} }};");
                    context.WriteLine(output, $"static PRT_TUPTYPE {tupStructName} = {{ {tupleType.Types.Count}U, {tupTypesArrayName} }};");
                    context.WriteLine(output, $"static PRT_TYPE {typeGenName} = {{ PRT_KIND_TUPLE, {{ .tuple = &{tupStructName} }} }};");
                    break;
                case TypeDefType _:
                    throw new ArgumentException("typedefs shouldn't be possible after canonicalization", nameof(type));
            }

            context.WrittenTypes.Add(type);
            return typeGenName;
        }

        private static void WriteProgramDecl(CompilationContext context, Scope globalScope, TextWriter output)
        {
            // generate event array
            var eventsList = globalScope.Events.Where(e => !e.IsBuiltIn);
            globalScope.Get("null", out PEvent nullEvent);
            globalScope.Get("halt", out PEvent haltEvent);
            eventsList = new[] {nullEvent, haltEvent}.Concat(eventsList);

            string eventArrayName = context.Names.GetTemporaryName("ALL_EVENTS");
            string eventArrayBody = string.Join(", ", eventsList.Select(ev => "&" + context.Names.GetNameForDecl(ev)));
            eventArrayBody = string.IsNullOrEmpty(eventArrayBody) ? "NULL" : eventArrayBody;
            context.WriteLine(output, $"PRT_EVENTDECL* {eventArrayName}[] = {{ {eventArrayBody} }};");

            // generate machine array
            string machineArrayName = context.Names.GetTemporaryName("ALL_MACHINES");
            string machineArrayBody = string.Join(", ", PrtTranslationUtils
                                                        .ToOrderedListByPermutation(globalScope.Machines, context.GetNumberForMachine)
                                                        .Select(ev => "&" + context.Names.GetNameForDecl(ev)));
            machineArrayBody = string.IsNullOrEmpty(machineArrayBody) ? "NULL" : machineArrayBody;
            context.WriteLine(output, $"PRT_MACHINEDECL* {machineArrayName}[] = {{ {machineArrayBody} }};");

            // generate interface array
            string interfaceArrayName = context.Names.GetTemporaryName("ALL_INTERFACES");
            string interfaceArrayBody = string.Join(", ", PrtTranslationUtils
                                                          .ToOrderedListByPermutation(globalScope.Interfaces, context.GetNumberForInterface)
                                                          .Select(ev => "&" + context.Names.GetNameForDecl(ev)));
            interfaceArrayBody = string.IsNullOrEmpty(interfaceArrayBody) ? "NULL" : interfaceArrayBody;
            context.WriteLine(output, $"PRT_INTERFACEDECL* {interfaceArrayName}[] = {{ {interfaceArrayBody} }};");

            // generate functions array
            var allFunctions = globalScope.Functions.Where(f => !f.IsForeign).ToList();
            string funcArrayName = context.Names.GetTemporaryName("ALL_FUNCTIONS");
            string funcArrayBody =
                string.Join(", ", allFunctions.Select(ev => "&" + context.Names.GetNameForDecl(ev)));
            funcArrayBody = string.IsNullOrEmpty(funcArrayBody) ? "NULL" : funcArrayBody;
            context.WriteLine(output, $"PRT_FUNDECL* {funcArrayName}[] = {{ {funcArrayBody} }};");

            // generate foreign types array
            var foreignTypes = context.WrittenTypes.Where(t => t is ForeignType).Cast<ForeignType>().ToList();
            string foreignTypesArrayName = context.Names.GetTemporaryName("ALL_FOREIGN_TYPES");
            string foreignTypesArrayBody = string.Join(", ",
                foreignTypes
                    .Select(t => $"&{context.Names.GetNameForForeignTypeDecl(t)}"));
            foreignTypesArrayBody = string.IsNullOrEmpty(foreignTypesArrayBody) ? "NULL" : foreignTypesArrayBody;
            context.WriteLine(output, $"PRT_FOREIGNTYPEDECL* {foreignTypesArrayName}[] = {{ {foreignTypesArrayBody} }};");

            foreach (Implementation impl in globalScope.Implementations)
            {
                var linkMap = impl.ModExpr.ModuleInfo.LinkMap;

                var trueLinkMap = ResolveLinkMap(globalScope, context, linkMap);
                var mapNames = Enumerable.Repeat("NULL", trueLinkMap.Length).ToArray();
                for (var i = 0; i < trueLinkMap.Length; i++)
                {
                    var iMap = trueLinkMap[i];
                    if (iMap != null)
                    {
                        string mapTmpName = context.Names.GetTemporaryName($"{impl.Name}_LME_{i}");
                        mapNames[i] = mapTmpName;
                        context.WriteLine(output, $"int {mapTmpName}[] = {{ {string.Join(",", iMap)} }};");
                    }
                }

                string linkMapName = context.Names.GetTemporaryName($"{impl.Name}_LINKMAP");
                context.WriteLine(output, $"int* {linkMapName}[] = {{ {string.Join(", ", mapNames)} }};");

                var machineDefMap = impl.ModExpr.ModuleInfo.InterfaceDef;
                string machineDefMapName = context.Names.GetTemporaryName($"{impl.Name}_DEFMAP");
                var realMachineDefMap = Enumerable.Repeat(-1, trueLinkMap.Length).ToArray();
                foreach (var linking in machineDefMap)
                {
                    realMachineDefMap[context.GetNumberForInterface(linking.Key)] = context.GetNumberForMachine(linking.Value);
                }

                context.WriteLine(output, $"int {machineDefMapName}[] = {{ {string.Join(",", realMachineDefMap)} }};");

                context.WriteLine(output, $"PRT_PROGRAMDECL {context.Names.GetNameForDecl(impl)} = {{");
                context.WriteLine(output, $"{globalScope.Events.Count()}U,");
                context.WriteLine(output, $"{globalScope.Machines.Count()}U,");
                context.WriteLine(output, $"{globalScope.Interfaces.Count()}U,");
                context.WriteLine(output, $"{allFunctions.Count}U,");
                context.WriteLine(output, $"{foreignTypes.Count}U,");
                context.WriteLine(output, $"{eventArrayName},");
                context.WriteLine(output, $"{machineArrayName},");
                context.WriteLine(output, $"{interfaceArrayName},");
                context.WriteLine(output, $"{funcArrayName},");
                context.WriteLine(output, $"{foreignTypesArrayName},");
                context.WriteLine(output, $"{linkMapName},");
                context.WriteLine(output, $"{machineDefMapName}");
                context.WriteLine(output, "};");
            }
        }

        private static int[][] ResolveLinkMap(Scope globalScope, CompilationContext context,
                                              IDictionary<Interface, IDictionary<Interface, Interface>> linkMap)
        {
            int nInterfaces = globalScope.Interfaces.Count();
            var maps = new int[nInterfaces][];
            foreach (var keyValuePair in linkMap)
            {
                int firstInterfaceIndex = context.GetNumberForInterface(keyValuePair.Key);
                Debug.Assert(maps[firstInterfaceIndex] == null);
                maps[firstInterfaceIndex] = Enumerable.Repeat(-1, nInterfaces).ToArray();

                foreach (var finalMapping in keyValuePair.Value)
                {
                    int secondInterfaceIndex = context.GetNumberForInterface(finalMapping.Key);
                    int finalInterfaceIndex = context.GetNumberForInterface(finalMapping.Value);
                    maps[firstInterfaceIndex][secondInterfaceIndex] = finalInterfaceIndex;
                }
            }

            return maps;
        }

        private static string GetReceivesNameOrMkTemp(Machine machine, CompilationContext context, TextWriter output)
        {
            string eventSetName;
            if (machine.Receives is NamedEventSet mRecvSet)
            {
                eventSetName = context.Names.GetNameForDecl(mRecvSet);
            }
            else
            {
                var machineTempRecvSet = new NamedEventSet(machine.Name + "_RECV", machine.SourceLocation);
                machineTempRecvSet.AddEvents(machine.Receives.Events);
                WriteSourceDecl(context, machineTempRecvSet, output);
                eventSetName = context.Names.GetNameForDecl(machineTempRecvSet);
            }

            return eventSetName;
        }

        private static string GetSendsNameOrMkTemp(Machine machine, CompilationContext context, TextWriter output)
        {
            string eventSetName;
            if (machine.Sends is NamedEventSet mSendSet)
            {
                eventSetName = context.Names.GetNameForDecl(mSendSet);
            }
            else
            {
                var machineTempSendSet = new NamedEventSet(machine.Name + "_SEND", machine.SourceLocation);
                machineTempSendSet.AddEvents(machine.Sends.Events);
                WriteSourceDecl(context, machineTempSendSet, output);
                eventSetName = context.Names.GetNameForDecl(machineTempSendSet);
            }

            return eventSetName;
        }

        #endregion

        #region Function body methods

        private static void WriteFunctionBody(CompilationContext context, Function function, TextWriter output)
        {
            var funLocation = context.LocationResolver.GetLocation(function);
            context.WriteLine(output, $"#line {funLocation.Line} \"{funLocation.File.Name}\"");

            for (var i = 0; i < function.Signature.Parameters.Count; i++)
            {
                Variable argument = function.Signature.Parameters[i];
                string varName = context.Names.GetNameForDecl(argument);
                context.WriteLine(output, $"PRT_VALUE** {varName} = argRefs[{i}];");
            }

            foreach (Variable localVariable in function.LocalVariables)
            {
                string varName = context.Names.GetNameForDecl(localVariable);
                string varTypeName = context.Names.GetNameForType(localVariable.Type);
                // TODO: optimize away PrtMkDefaultValue if liveness shows no usages before assignments.
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

            // Write the body into a temporary buffer so that forward declarations can be found and added
            var bodyWriter = new StringWriter();
            var bodyLocation = context.LocationResolver.GetLocation(function.Body);
            context.WriteLine(bodyWriter, $"#line {bodyLocation.Line} \"{bodyLocation.File.Name}\"");

            foreach (IPStmt stmt in function.Body.Statements)
            {
                WriteStmt(context, function, stmt, bodyWriter);
            }
            
            bodyWriter.WriteLine("p_return:");
            foreach (Variable localVariable in function.LocalVariables)
            {
                string varName = context.Names.GetNameForDecl(localVariable);
                context.WriteLine(bodyWriter, $"PrtFreeValue({varName}); {varName} = NULL;");
            }

            context.WriteLine(bodyWriter, $"return {FunResultValName};");

            // Write gathered literals to the prologue
            context.WriteLine(output, $"PRT_VALUE {FunNullStaticName} = {{ PRT_VALUE_KIND_NULL, {{ .ev = PRT_SPECIAL_EVENT_NULL }} }};");

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
                context.WriteLine(
                    output,
                    $"PRT_VALUE {literal.Value} = {{ PRT_VALUE_KIND_BOOL, {{ .bl = {(literal.Key ? "PRT_TRUE" : "PRT_FALSE")} }} }};");
            }

            output.Write(bodyWriter);
        }

        private static string GetVariableReference(CompilationContext context, Function function, IVariableRef variableRef)
        {
            return $"&({GetVariablePointer(context, function, variableRef.Variable)})";
        }

        private static void WriteStmt(CompilationContext context, Function function, IPStmt stmt, TextWriter output)
        {
            var stmtLocation = context.LocationResolver.GetLocation(stmt);
            context.WriteLine(output, $"#line {stmtLocation.Line} \"{stmtLocation.File.Name}\"");
            switch (stmt)
            {
                case AnnounceStmt _:
                    // TODO: Ankush needs to implement this in Prt
                    break;
                case AssertStmt assertStmt:
                    context.Write(output, "PrtAssert(PrtPrimGetBool(");
                    WriteExpr(context, output, function, assertStmt.Assertion);
                    context.WriteLine(output, $"), \"{assertStmt.Message}\");");
                    break;
                case AssignStmt assignStmt:
                    // Lookup lvalue
                    string lvalName = context.Names.GetTemporaryName("LVALUE");
                    context.Write(output, $"PRT_VALUE** {lvalName} = &(");
                    WriteLValue(context, output, function, assignStmt.Location);
                    context.WriteLine(output, ");");

                    // Free old value
                    context.WriteLine(output, $"PrtFreeValue(*{lvalName});");

                    // Assign new value
                    context.Write(output, $"*{lvalName} = ");
                    WriteExpr(context, output, function, assignStmt.Value);
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
                    context.Write(
                        output, $"PrtMkInterface(context, {context.GetNumberForInterface(ctorStmt.Interface)}, {ctorStmt.Arguments.Count}");
                    foreach (IPExpr pExpr in ctorStmt.Arguments)
                    {
                        Debug.Assert(pExpr is IVariableRef);
                        var argVar = (IVariableRef) pExpr;
                        context.Write(output, $", {GetVariableReference(context, function, argVar)}");
                    }

                    context.WriteLine(output, ");");
                    break;
                case FunCallStmt funCallStmt:
                    string funImplName = context.Names.GetNameForFunctionImpl(funCallStmt.Fun);
                    var funArgs = funCallStmt.ArgsList.Cast<ILinearRef>().ToList();

                    // Put all the arguments in the args array
                    foreach (var arg in funArgs.Select((arg, i) => new {arg, i}))
                    {
                        context.WriteLine(
                            output, $"{FunCallArgsArrayName}[{arg.i}] = {GetVariableReference(context, function, arg.arg)};");
                    }

                    // Call the function and immediately free the value
                    context.WriteLine(output, $"PrtFreeValue({funImplName}(context, {FunCallArgsArrayName}));");

                    // Free and set to null all the moved arguments
                    var toFree = funArgs.Where(arg => arg.LinearType.Equals(LinearType.Move))
                                        .Select(arg => GetVariablePointer(context, function, arg.Variable));
                    foreach (string argName in toFree)
                    {
                        context.WriteLine(output, $"PrtFreeValue({argName});");
                        context.WriteLine(output, $"{argName} = NULL;");
                    }

                    break;
                case GotoStmt gotoStmt:
                    context.WriteLine(output, "PrtFreeTriggerPayload(p_this);");

                    int destStateIndex = context.GetNumberForState(gotoStmt.State);
                    context.Write(output, $"PrtGoto(p_this, {destStateIndex}U, ");
                    if (gotoStmt.Payload != null)
                    {
                        Debug.Assert(gotoStmt.Payload is IVariableRef);
                        var gotoArg = (IVariableRef) gotoStmt.Payload;
                        context.Write(output, $"1, {GetVariableReference(context, function, gotoArg)}");
                    }
                    else
                    {
                        context.Write(output, "0");
                    }

                    context.WriteLine(output, ");");
                    break;
                case IfStmt ifStmt:
                    context.Write(output, "if (PrtPrimGetBool(");
                    WriteExpr(context, output, function, ifStmt.Condition);
                    context.WriteLine(output, "))");
                    WriteStmt(context, function, ifStmt.ThenBranch, output);
                    if (ifStmt.ElseBranch != null)
                    {
                        context.WriteLine(output, "else");
                        WriteStmt(context, function, ifStmt.ElseBranch, output);
                    }

                    break;
                case InsertStmt insertStmt:
                    context.Write(output, "PrtSeqInsertEx(");
                    WriteLValue(context, output, function, insertStmt.Variable);
                    context.Write(output, ", ");
                    WriteExpr(context, output, function, insertStmt.Index);
                    context.Write(output, ", ");
                    Debug.Assert(insertStmt.Value is IVariableRef);
                    WriteExpr(context, output, function, insertStmt.Value);
                    context.WriteLine(output, ", PRT_FALSE);");
                    var insertValueVar = (IVariableRef) insertStmt.Value;
                    context.WriteLine(output, $"*({GetVariableReference(context, function, insertValueVar)}) = NULL;");
                    break;
                case MoveAssignStmt moveAssignStmt:
                    context.WriteLine(output, "{");
                    string movedVarName = GetVariablePointer(context, function, moveAssignStmt.FromVariable);

                    // Get reference to old value
                    string movedLValue = context.Names.GetTemporaryName("LVALUE");
                    context.Write(output, $"PRT_VALUE** {movedLValue} = &(");
                    WriteLValue(context, output, function, moveAssignStmt.ToLocation);
                    context.WriteLine(output, ");");

                    // Free old value
                    context.WriteLine(output, $"PrtFreeValue(*{movedLValue});");
                    
                    // Move variable to lvalue location
                    context.WriteLine(output, $"*{movedLValue} = {movedVarName};");

                    // Null out old variable
                    context.WriteLine(output, $"{movedVarName} = NULL;");

                    context.WriteLine(output, "}");
                    break;
                case NoStmt _:
                    return;
                case PopStmt _:
                    context.WriteLine(output, "PrtFreeTriggerPayload(p_this);");
                    context.WriteLine(output, "PrtPop(p_this);");
                    context.WriteLine(output, "goto p_return;");
                    break;
                case PrintStmt printStmt:
                    WritePrintStmt(context, output, printStmt, function);
                    break;
                case RaiseStmt raiseStmt:
                    context.WriteLine(output, "PrtFreeTriggerPayload(p_this);");
                    context.Write(output, "PrtRaise(p_this, ");
                    WriteExpr(context, output, function, raiseStmt.PEvent);
                    context.Write(output, $", {raiseStmt.Payload.Count}");
                    foreach (IPExpr pExpr in raiseStmt.Payload)
                    {
                        Debug.Assert(pExpr is IVariableRef);
                        var argVar = (IVariableRef) pExpr;
                        context.Write(output, $", {GetVariableReference(context, function, argVar)}");
                    }

                    context.WriteLine(output, ");");

                    Debug.Assert(raiseStmt.PEvent is IVariableRef);
                    var raiseEventVar = (IVariableRef) raiseStmt.PEvent;
                    context.WriteLine(output, $"*({GetVariableReference(context, function, raiseEventVar)}) = NULL;");
                    context.WriteLine(output, "goto p_return;");
                    break;
                case ReceiveStmt receiveStmt:
                    // TODO: implement. Daan's the man!
                    context.WriteLine(output, "PrtAssert(PRT_FALSE, \"receive not yet implemented!\");");
                    /*
                     * Ideal template:
                     * PRT_VALUE* payload = NULL; int allowedEventIds[] = { ... };
                     * int eventId = PrtReceiveAsync(context, allowedEventIds, &payload);
                     * switch(eventId) {
                     *     case X: {
                     *         PRT_VALUE* actual_payload_var_name = payload;
                     *         // statements
                     *     } break;
                     *     // more cases
                     *     default:
                     *         PrtAssert(false, "issue in receive");
                     *         break;
                     * }
                     */
                    break;
                case RemoveStmt removeStmt:
                    context.Write(output, "PrtRemoveByKey(");
                    WriteLValue(context, output, function, removeStmt.Variable);
                    context.Write(output, ", ");
                    WriteExpr(context, output, function, removeStmt.Value);
                    context.WriteLine(output, ");");
                    break;
                case ReturnStmt returnStmt:
                    if (returnStmt.ReturnValue != null)
                    {
                        context.WriteLine(output, $"PrtFreeValue({FunResultValName});");
                        context.Write(output, $"{FunResultValName} = ");
                        WriteExpr(context, output, function, returnStmt.ReturnValue);
                        context.WriteLine(output, ";");
                    }

                    context.WriteLine(output, "goto p_return;");
                    break;
                case SendStmt sendStmt:
                    context.Write(output, "PrtSendInternal(context, PrtGetMachine(context->process, ");
                    WriteExpr(context, output, function, sendStmt.MachineExpr);
                    context.Write(output, "), ");
                    WriteExpr(context, output, function, sendStmt.Evt);
                    context.Write(output, $", {sendStmt.ArgsList.Count}");
                    foreach (IPExpr sendArgExpr in sendStmt.ArgsList)
                    {
                        Debug.Assert(sendArgExpr is IVariableRef);
                        var argVar = (IVariableRef) sendArgExpr;
                        context.Write(output, $", {GetVariableReference(context, function, argVar)}");
                    }

                    context.WriteLine(output, ");");

                    Debug.Assert(sendStmt.Evt is IVariableRef);
                    var sendEventVar = (IVariableRef) sendStmt.Evt;
                    context.WriteLine(output, $"*({GetVariableReference(context, function, sendEventVar)}) = NULL;");
                    break;
                case SwapAssignStmt swapAssignStmt:
                    context.WriteLine(output, "{");
                    string lvName = context.Names.GetTemporaryName("LVALUE");
                    string tmpName = context.Names.GetTemporaryName("SWAP");

                    // Can only swap local variables, so this is safe. Otherwise would have to call a second WriteLValue
                    string swappedName = GetVariablePointer(context, function, swapAssignStmt.OldLocation);

                    // Save l-value
                    context.Write(output, $"PRT_VALUE** {lvName} = &(");
                    WriteLValue(context, output, function, swapAssignStmt.NewLocation);
                    context.WriteLine(output, ");");
                    context.WriteLine(output, $"PRT_VALUE* {tmpName} = *{lvName};");

                    // Overwrite l-value with var
                    context.WriteLine(output, $"*{lvName} = {swappedName};");

                    // Complete the swap
                    context.WriteLine(output, $"{swappedName} = {tmpName};");
                    context.WriteLine(output, "}");
                    break;
                case WhileStmt whileStmt:
                    context.Write(output, "while (PrtPrimGetBool(");
                    WriteExpr(context, output, function, whileStmt.Condition);
                    context.WriteLine(output, "))");
                    WriteStmt(context, function, whileStmt.Body, output);
                    break;
            }

            context.WriteLine(output);
        }

        private static void WritePrintStmt(CompilationContext context, TextWriter output, PrintStmt printStmt, Function function)
        {
            // format is {str0, n1, str1, n2, ..., nK, strK}
            var printMessageParts = PrtTranslationUtils.ParsePrintMessage(printStmt.Message);

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
            context.Write(output, printStmt.Args.Count.ToString());
            foreach (IPExpr printArg in printStmt.Args)
            {
                context.Write(output, ", ");
                WriteExpr(context, output, function, printArg);
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

        private static void WriteLValue(CompilationContext context, TextWriter output, Function function, IPExpr expr)
        {
            switch (expr)
            {
                case MapAccessExpr mapAccessExpr:
                    // TODO: optimize out key copy when possible
                    context.Write(output, "*(PrtMapGetLValue(");
                    WriteLValue(context, output, function, mapAccessExpr.MapExpr);
                    context.Write(output, ", ");
                    WriteExpr(context, output, function, mapAccessExpr.IndexExpr);
                    context.Write(output, $", PRT_TRUE, &{context.Names.GetNameForType(mapAccessExpr.MapExpr.Type)}))");
                    break;
                case NamedTupleAccessExpr namedTupleAccessExpr:
                    context.Write(output, "*(PrtTupleGetLValue(");
                    WriteLValue(context, output, function, namedTupleAccessExpr.SubExpr);
                    context.Write(output, $", {namedTupleAccessExpr.Entry.FieldNo}))");
                    break;
                case SeqAccessExpr seqAccessExpr:
                    context.Write(output, "*(PrtSeqGetLValue(");
                    WriteLValue(context, output, function, seqAccessExpr.SeqExpr);
                    context.Write(output, ", ");
                    WriteExpr(context, output, function, seqAccessExpr.IndexExpr);
                    context.Write(output, "))");
                    break;
                case TupleAccessExpr tupleAccessExpr:
                    context.Write(output, "*(PrtTupleGetLValue(");
                    WriteLValue(context, output, function, tupleAccessExpr.SubExpr);
                    context.Write(output, $", {tupleAccessExpr.FieldNo}))");
                    break;
                case VariableAccessExpr variableAccessExpr:
                    WriteVariableAccess(context, output, function, variableAccessExpr.Variable);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(expr));
            }
        }

        private static void WriteExpr(CompilationContext context, TextWriter output, Function function, IPExpr expr)
        {
            switch (expr)
            {
                case CloneExpr cloneExpr:
                    context.Write(output, "PrtCloneValue(");
                    WriteExpr(context, output, function, cloneExpr.Term);
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
                        WriteExpr(context, output, function, binOpLhs);
                        context.Write(output, ", ");
                        WriteExpr(context, output, function, binOpRhs);
                        context.Write(output, "))");
                    }
                    else
                    {
                        (string binOpGetter, _) = GetTypeStructureFuns(binOpLhs.Type);
                        (_, string binOpBuilder) = GetTypeStructureFuns(binOpExpr.Type);

                        context.Write(output, $"{binOpBuilder}(");

                        context.Write(output, $"{binOpGetter}(");
                        WriteExpr(context, output, function, binOpLhs);
                        context.Write(output, ")");

                        context.Write(output, $" {BinOpToStr(binOpType)} ");

                        context.Write(output, $"{binOpGetter}(");
                        WriteExpr(context, output, function, binOpRhs);
                        context.Write(output, ")");

                        context.Write(output, ")");
                    }

                    break;
                case BoolLiteralExpr boolLiteralExpr:
                    string boolLiteralName = context.RegisterLiteral(function, boolLiteralExpr.Value);
                    context.Write(output, $"(&{boolLiteralName})");
                    break;
                case CastExpr castExpr:
                    string castTypeName = context.Names.GetNameForType(castExpr.Type);
                    context.Write(output, "PrtCloneValue(PrtCastValue(");
                    WriteExpr(context, output, function, castExpr.SubExpr);
                    context.Write(output, $", &{castTypeName}))");
                    break;
                case CoerceExpr coerceExpr:
                    string coerceCtor;
                    switch (coerceExpr.NewType)
                    {
                        case PrimitiveType primitiveType when PrimitiveType.Int.IsSameTypeAs(primitiveType):
                        case PLanguageType type when PLanguageType.TypeIsOfKind(type, TypeKind.Enum):
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
                        case PLanguageType type when PLanguageType.TypeIsOfKind(type, TypeKind.Enum):
                            coerceUnpack = "PrtPrimGetInt";
                            break;
                        case PrimitiveType primitiveType when PrimitiveType.Float.IsSameTypeAs(primitiveType):
                            coerceUnpack = "PrtPrimGetFloat";
                            break;
                        default:
                            throw new ArgumentException(nameof(coerceExpr.SubExpr));
                    }

                    context.Write(output, $"{coerceCtor}({coerceUnpack}(");
                    WriteExpr(context, output, function, coerceExpr.SubExpr);
                    context.Write(output, "))");
                    break;
                case ContainsKeyExpr containsKeyExpr:
                    context.Write(output, "PrtMkBoolValue(PrtMapExists(");
                    WriteExpr(context, output, function, containsKeyExpr.Map);
                    context.Write(output, ", ");
                    WriteExpr(context, output, function, containsKeyExpr.Key);
                    context.Write(output, "));");
                    break;
                case CtorExpr ctorExpr:
                    context.Write(
                        output,
                        $"PrtCloneValue(PrtMkInterface(context, {context.GetNumberForInterface(ctorExpr.Interface)}, {ctorExpr.Arguments.Count}");
                    foreach (IPExpr pExpr in ctorExpr.Arguments)
                    {
                        Debug.Assert(pExpr is IVariableRef);
                        var argVar = (IVariableRef) pExpr;
                        context.Write(output, $", {GetVariableReference(context, function, argVar)}");
                    }

                    context.Write(output, ")->id)");
                    break;
                case DefaultExpr defaultExpr:
                    string nameForDefaultType = context.Names.GetNameForType(defaultExpr.Type);
                    context.Write(output, $"PrtMkDefaultValue(&{nameForDefaultType})");
                    break;
                case EnumElemRefExpr enumElemRefExpr:
                    string enumLiteralName = context.RegisterLiteral(function, enumElemRefExpr.Value.Value);
                    context.Write(output, $"(&{enumLiteralName})");
                    break;
                case EventRefExpr eventRefExpr:
                    context.Write(output, $"(&{context.Names.GetNameForDecl(eventRefExpr.Value)}.value)");
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
                    var argSetup = funArgs.Select((arg, i) => $"({FunCallArgsArrayName}[{i}] = {GetVariableReference(context, function, arg)})");
                    var funCall = new[] {$"({FunCallRetValName} = {funImplName}(context, {FunCallArgsArrayName}))"};
                    var argsFree = funArgs.Where(arg => arg.LinearType.Equals(LinearType.Move))
                                          .Select(arg => GetVariablePointer(context, function, arg.Variable))
                                          .Select(varName => $"(PrtFreeValue({varName}), {varName} = NULL)");
                    var resRetrieve = new[] {$"({FunCallRetValName})"};
                    string fullCall = string.Join(", ", argSetup.Concat(funCall).Concat(argsFree).Concat(resRetrieve));
                    context.Write(output, $"({fullCall})");
                    break;
                case IntLiteralExpr intLiteralExpr:
                    string intLiteralName = context.RegisterLiteral(function, intLiteralExpr.Value);
                    context.Write(output, $"(&{intLiteralName})");
                    break;
                case KeysExpr keysExpr:
                    context.Write(output, "PrtMapGetKeys(");
                    WriteExpr(context, output, function, keysExpr.Expr);
                    context.Write(output, ")");
                    break;
                case LinearAccessRefExpr linearAccessRefExpr:
                    // TODO: what's special about linear refs here?
                    WriteVariableAccess(context, output, function, linearAccessRefExpr.Variable);
                    break;
                case MapAccessExpr mapAccessExpr:
                    context.Write(output, "PrtMapGet(");
                    WriteExpr(context, output, function, mapAccessExpr.MapExpr);
                    context.Write(output, ", ");
                    WriteExpr(context, output, function, mapAccessExpr.IndexExpr);
                    context.Write(output, ")");
                    break;
                case NamedTupleAccessExpr namedTupleAccessExpr:
                    context.Write(output, "PrtTupleGet(");
                    WriteExpr(context, output, function, namedTupleAccessExpr.SubExpr);
                    context.Write(output, $", {namedTupleAccessExpr.Entry.FieldNo})");
                    break;
                case NamedTupleExpr namedTupleExpr:
                    var ntArgs = (IReadOnlyList<IVariableRef>) namedTupleExpr.TupleFields;
                    string ntTypeName = context.Names.GetNameForType(namedTupleExpr.Type);
                    string namedTupleBody =
                        string.Join(", ", ntArgs.Select(v => GetVariableReference(context, function, v)));
                    context.Write(output, $"(PrtMkTuple(&{ntTypeName}, {namedTupleBody}))");
                    break;
                case NondetExpr _:
                    context.Write(output, "(PrtMkNondetBoolValue())");
                    break;
                case NullLiteralExpr _:
                    context.Write(output, $"(&{FunNullStaticName})");
                    break;
                case SeqAccessExpr seqAccessExpr:
                    context.Write(output, "PrtSeqGet(");
                    WriteExpr(context, output, function, seqAccessExpr.SeqExpr);
                    context.Write(output, ", ");
                    WriteExpr(context, output, function, seqAccessExpr.IndexExpr);
                    context.Write(output, ")");
                    break;
                case SizeofExpr sizeofExpr:
                    var sizeofFun = PLanguageType.TypeIsOfKind(sizeofExpr.Expr.Type, TypeKind.Map) ? "PrtMapSizeOf" : "PrtSeqSizeOf";
                    context.Write(output, $"PrtMkIntValue({sizeofFun}(");
                    WriteExpr(context, output, function, sizeofExpr.Expr);
                    context.Write(output, "))");
                    break;
                case ThisRefExpr _:
                    context.Write(output, "(p_this->id)");
                    break;
                case TupleAccessExpr tupleAccessExpr:
                    context.Write(output, "PrtTupleGet(");
                    WriteExpr(context, output, function, tupleAccessExpr.SubExpr);
                    context.Write(output, $", {tupleAccessExpr.FieldNo})");
                    break;
                case UnaryOpExpr unaryOpExpr:
                    (string unOpGetter, string unOpBuilder) = GetTypeStructureFuns(unaryOpExpr.Type);
                    context.Write(output, $"{unOpBuilder}(");

                    context.Write(output, UnOpToStr(unaryOpExpr.Operation));
                    context.Write(output, $"{unOpGetter}(");
                    WriteExpr(context, output, function, unaryOpExpr.SubExpr);
                    context.Write(output, ")");

                    context.Write(output, ")");
                    break;
                case UnnamedTupleExpr unnamedTupleExpr:
                    var utArgs = (IReadOnlyList<IVariableRef>) unnamedTupleExpr.TupleFields;
                    string utTypeName = context.Names.GetNameForType(unnamedTupleExpr.Type);
                    string tupleBody =
                        string.Join(", ", utArgs.Select(v => GetVariableReference(context, function, v)));
                    context.Write(output, $"(PrtMkTuple(&{utTypeName}, {tupleBody}))");
                    break;
                case ValuesExpr valuesExpr:
                    context.Write(output, "PrtMapGetValues(");
                    WriteExpr(context, output, function, valuesExpr.Expr);
                    context.Write(output, ")");
                    break;
                case VariableAccessExpr variableAccessExpr:
                    WriteVariableAccess(context, output, function, variableAccessExpr.Variable);
                    break;
            }
        }

        private static string GetVariablePointer(CompilationContext context, Function function, Variable variable)
        {
            if (variable.Role.HasFlag(VariableRole.Param))
            {
                // dereference, since params are passed by reference.
                return $"*{context.Names.GetNameForDecl(variable)}";
            }
            
            if (variable.Role.HasFlag(VariableRole.Field))
            {
                // TODO: is this always correct? I think the iterator ordering of a List should be consistent...
                int varIdx = function.Owner.Fields.ToList().IndexOf(variable);
                return $"p_this->varValues[{varIdx}]";
            }

            if (variable.Role.HasFlag(VariableRole.Temp) || variable.Role.HasFlag(VariableRole.Local))
            {
                return context.Names.GetNameForDecl(variable);
            }

            throw new ArgumentOutOfRangeException(nameof(variable));
        }

        private static void WriteVariableAccess(CompilationContext context, TextWriter output, Function function, Variable variable)
        {
            context.Write(output, GetVariablePointer(context, function, variable));
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

        #endregion

        #region Header methods

        private static void WriteHeaderPrologue(CompilationContext context, TextWriter output)
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

        private static void WriteExternDeclaration(CompilationContext context, TextWriter output, IPDecl decl)
        {
            string declName = context.Names.GetNameForDecl(decl);
            switch (decl)
            {
                case EnumElem _:
                    break;
                case Function function when !function.IsForeign:
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
                    string enumBody = string.Join(", ", pEnum.Values.Select(val => $"{context.Names.GetNameForDecl(val)} = {val.Value}"));
                    context.WriteLine(output, $"typedef enum {declName} {{ {enumBody} }} {declName};");
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
                case Variable _:
                    throw new ArgumentOutOfRangeException(nameof(decl), "can't have global P variables");
                case State state:
                    context.WriteLine(output, $"// DECL(State, {decl.Name}) => {declName}");
                    break;
                case StateGroup stateGroup:
                    context.WriteLine(output, $"// DECL(StateGroup, {decl.Name}) => {declName}");
                    break;
            }
        }

        private static void WriteHeaderEpilogue(CompilationContext context, TextWriter output)
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
