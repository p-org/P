using Plang.Compiler.Backend.ASTExt;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.AST.Statements;
using Plang.Compiler.TypeChecker.AST.States;
using Plang.Compiler.TypeChecker.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Plang.Compiler.Backend.Prt
{
    public class PrtCodeGenerator : ICodeGenerator
    {
        public IEnumerable<CompiledFile> GenerateCode(ICompilationJob job, Scope globalScope)
        {
            return PrtCodeGeneratorImpl.GenerateCode(job, globalScope);
        }
    }

    internal class PrtCodeGeneratorImpl
    {
        private const string FunCallArgsArrayName = "_P_GEN_funargs";
        private const string FunCallRetValName = "_P_GEN_funval";
        private const string FunResultValName = "_P_GEN_retval";
        private const string FunNullStaticName = "_P_GEN_null";
        private readonly CompilationContext context;

        private PrtCodeGeneratorImpl(CompilationContext context)
        {
            this.context = context;
        }

        public static IEnumerable<CompiledFile> GenerateCode(ICompilationJob job, Scope globalScope)
        {
            CompilationContext context = new CompilationContext(job);
            PrtCodeGeneratorImpl generator = new PrtCodeGeneratorImpl(context);
            CompiledFile cHeader = generator.GenerateHeaderFile(globalScope);
            CompiledFile cSource = generator.GenerateSourceFile(globalScope);
            return new List<CompiledFile> { cHeader, cSource };
        }

        #region Top-level generation methods

        private CompiledFile GenerateHeaderFile(Scope globalScope)
        {
            CompiledFile cHeader = new CompiledFile(context.HeaderFileName);

            WriteHeaderPrologue(cHeader.Stream);
            foreach (IPDecl decl in globalScope.AllDecls)
            {
                WriteExternDeclaration(cHeader.Stream, decl);
            }

            WriteHeaderEpilogue(cHeader.Stream);
            return cHeader;
        }

        private CompiledFile GenerateSourceFile(Scope globalScope)
        {
            CompiledFile cSource = new CompiledFile(context.SourceFileName);
            // Write includes and common macros, if any
            WriteSourcePrologue(cSource.Stream);

            // Write the machine and function bodies into temporary buffer
            StringWriter bodyWriter = new StringWriter();
            foreach (IPDecl decl in globalScope.AllDecls)
            {
                WriteSourceDecl(bodyWriter, decl);
            }

            // Write all the type definitions and function implementation prototypes
            context.WriteLine(cSource.Stream, "// Type universe for program:");
            foreach (PLanguageType type in context.UsedTypes.ToArray())
            {
                WriteTypeDefinition(cSource.Stream, type);
            }

            context.WriteLine(cSource.Stream);

            context.WriteLine(cSource.Stream, "// Function implementation prototypes:");
            foreach (Function function in globalScope.GetAllMethods())
            {
                string functionName = context.Names.GetNameForFunctionImpl(function);
                context.WriteLine(cSource.Stream,
                    $"PRT_VALUE* {functionName}(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);");
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
            WriteProgramDecl(cSource.Stream, globalScope);
            return cSource;
        }

        private void TraceSourceLine(TextWriter output, SourceLocation location)
        {
            if (context.Job.GenerateSourceMaps)
            {
                context.WriteLine(output, $"#line {location.Line} \"{location.File.Name}\"");
            }
        }

        #endregion Top-level generation methods

        #region Declaration level methods

        private void WriteSourcePrologue(TextWriter output)
        {
            context.WriteLine(output, $"#include \"{context.HeaderFileName}\"");
            context.WriteLine(output);
        }

        private void WriteSourceDecl(TextWriter output, IPDecl decl)
        {
            string declName = context.Names.GetNameForDecl(decl);
            SourceLocation declLocation = context.LocationResolver.GetLocation(decl);
            switch (decl)
            {
                case EnumElem _:
                    // Member of a type. Instantiated by usage.
                    return;

                case PEnum _:
                    // Declares a type. Instantiated by usage.
                    return;

                case Function function:
                    WriteFunction(output, function);
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
                        NamedEventSet interfaceEventSet = new NamedEventSet(@interface.Name + "_RECV", @interface.SourceLocation);
                        interfaceEventSet.AddEvents(@interface.ReceivableEvents.Events);
                        WriteSourceDecl(output, interfaceEventSet);
                        ifaceRecvSetName = context.Names.GetNameForDecl(interfaceEventSet);
                    }

                    TraceSourceLine(output, declLocation);
                    context.WriteLine(output, $"PRT_INTERFACEDECL {declName} =");
                    context.WriteLine(output, "{");
                    context.WriteLine(output, $"{context.GetDeclNumber(@interface)}U,");
                    context.WriteLine(output, $"\"{@interface.Name}\",");
                    context.WriteLine(output, $"&{context.Names.GetNameForType(@interface.PayloadType)},");
                    context.WriteLine(output, $"&{ifaceRecvSetName}");
                    context.WriteLine(output, "};");
                    break;

                case Machine machine:
                    List<Variable> machineFields = machine.Fields.ToList();
                    string fieldArrayName = "NULL";
                    if (machineFields.Any())
                    {
                        fieldArrayName = context.Names.GetTemporaryName($"{machine.Name}_VARS");
                        context.WriteLine(output, $"PRT_VARDECL {fieldArrayName}[] = {{");
                        for (int i = 0; i < machineFields.Count; i++)
                        {
                            Variable field = machineFields[i];
                            string sep = i == machineFields.Count - 1 ? "" : ",";
                            context.WriteLine(output,
                                $"{{ \"{field.Name}\", &{context.Names.GetNameForType(field.Type)} }}{sep}");
                        }

                        context.WriteLine(output, "};");
                        context.WriteLine(output);
                    }

                    List<State> machineStates = machine.AllStates().ToList();
                    State[] machineStatesInOrder = new State[machineStates.Count];
                    foreach (State state in machineStates)
                    {
                        WriteSourceDecl(output, state);
                        machineStatesInOrder[context.GetDeclNumber(state)] = state;
                    }

                    string stateArrayName = context.Names.GetTemporaryName($"{machine.Name}_STATES");
                    string stateArrayBody =
                        string.Join(", ", machineStatesInOrder.Select(st => context.Names.GetNameForDecl(st)));
                    context.WriteLine(output, $"PRT_STATEDECL {stateArrayName}[] = {{ {stateArrayBody} }};");
                    context.WriteLine(output);

                    List<Function> machineMethods = machine.Methods.ToList();
                    foreach (Function machineMethod in machineMethods)
                    {
                        WriteSourceDecl(output, machineMethod);
                    }

                    string methodArrayName = "NULL";
                    if (machineMethods.Any())
                    {
                        methodArrayName = context.Names.GetTemporaryName($"{machine.Name}_METHODS");
                        string methodArrayBody =
                            string.Join(", ", machineMethods.Select(m => $"&{context.Names.GetNameForDecl(m)}"));
                        context.WriteLine(output, $"PRT_FUNDECL* {methodArrayName}[] = {{ {methodArrayBody} }};");
                        context.WriteLine(output);
                    }

                    string machineRecvSetName = GetReceivesNameOrMkTemp(output, machine);
                    string machineSendSetName = GetSendsNameOrMkTemp(output, machine);
                    string machineCreatesName = "NULL";
                    if (machine.Creates.Interfaces.Any())
                    {
                        List<Interface> createsInterfaces = machine.Creates.Interfaces.ToList();
                        machineCreatesName = context.Names.GetTemporaryName($"{machine.Name}_CREATES");
                        string createsArrayName = context.Names.GetTemporaryName($"{machine.Name}_CREATES_ARR");
                        string createsArrayBody = string.Join(", ", createsInterfaces.Select(context.GetDeclNumber));
                        context.WriteLine(output, $"PRT_UINT32 {createsArrayName}[] = {{ {createsArrayBody} }};");
                        context.WriteLine(
                            output,
                            $"PRT_INTERFACESETDECL {machineCreatesName} = {{ {createsInterfaces.Count}, {createsArrayName} }};");
                        machineCreatesName = "&" + machineCreatesName;
                    }

                    uint maxQueueSize = machine.Assert ?? uint.MaxValue;
                    TraceSourceLine(output, declLocation);
                    context.WriteLine(output, $"PRT_MACHINEDECL {declName} = ");
                    context.WriteLine(output, "{");
                    context.WriteLine(output, $"{context.GetDeclNumber(machine)}U,");
                    context.WriteLine(output, $"\"{machine.Name}\",");
                    context.WriteLine(output, $"&{machineRecvSetName},");
                    context.WriteLine(output, $"&{machineSendSetName},");
                    context.WriteLine(output, $"{machineCreatesName},");
                    context.WriteLine(output, $"{machineFields.Count}U,");
                    context.WriteLine(output, $"{machineStatesInOrder.Length}U,");
                    context.WriteLine(output, $"{machineMethods.Count}U,");
                    context.WriteLine(output, $"{maxQueueSize}U,");
                    context.WriteLine(output, $"{context.GetDeclNumber(machine.StartState)}U,");
                    context.WriteLine(output, $"{fieldArrayName},");
                    context.WriteLine(output, $"{stateArrayName},");
                    context.WriteLine(output, $"{methodArrayName}");
                    context.WriteLine(output, "};");

                    break;

                case NamedEventSet namedEventSet:
                    string innerSetName = context.Names.GetTemporaryName(namedEventSet.Name + "_INNER");
                    List<string> eventDeclNames = namedEventSet.Events.Select(x => "&" + context.Names.GetNameForDecl(x))
                        .ToList();
                    string eventDeclArrBody = string.Join(", ", eventDeclNames);
                    eventDeclArrBody = string.IsNullOrEmpty(eventDeclArrBody) ? "NULL" : eventDeclArrBody;
                    TraceSourceLine(output, declLocation);
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
                    long eventBound = Math.Min(pEvent.Assert == -1 ? uint.MaxValue : (uint)pEvent.Assert,
                        pEvent.Assume == -1 ? uint.MaxValue : (uint)pEvent.Assume);

                    TraceSourceLine(output, declLocation);
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
                    TraceSourceLine(output, declLocation);
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

                    int stateIndex = context.GetDeclNumber(state);
                    PrtTranslationUtils.StateActionResults stateData =
                        PrtTranslationUtils.BuildActionSets(context, state);

                    WriteSourceDecl(output, stateData.DefersSet);
                    WriteSourceDecl(output, stateData.TransSet);
                    WriteSourceDecl(output, stateData.DosSet);

                    string transArrName = "NULL";
                    if (stateData.Trans.Count != 0)
                    {
                        transArrName = context.Names.GetTemporaryName("TRANS");
                        context.WriteLine(output, $"PRT_TRANSDECL {transArrName}[] =");
                        context.WriteLine(output, "{");
                        for (int i = 0; i < stateData.Trans.Count; i++)
                        {
                            (PEvent triggerEvent, int destIndex, string transFunRef) = stateData.Trans[i];
                            string triggerName = context.Names.GetNameForDecl(triggerEvent);
                            string comma = i == stateData.Trans.Count - 1 ? "" : ",";
                            context.WriteLine(output,
                                $"{{ {stateIndex}, &{triggerName}, {destIndex}, {transFunRef} }}{comma}");
                        }

                        context.WriteLine(output, "};");
                        context.WriteLine(output);
                    }

                    string dosArrName = "NULL";
                    if (stateData.Dos.Count != 0)
                    {
                        dosArrName = context.Names.GetTemporaryName("DOS");
                        context.WriteLine(output, $"PRT_DODECL {dosArrName}[] =");
                        context.WriteLine(output, "{");
                        for (int i = 0; i < stateData.Dos.Count; i++)
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

                    TraceSourceLine(output, declLocation);
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
                        WriteSourceDecl(output, state);
                    }

                    foreach (StateGroup subGroup in stateGroup.Groups)
                    {
                        WriteSourceDecl(output, subGroup);
                    }

                    break;
            }

            context.WriteLine(output);
        }

        private void WriteFunction(TextWriter output, Function function)
        {
            string declName = context.Names.GetNameForDecl(function);
            SourceLocation declLocation = context.LocationResolver.GetLocation(function);

            string functionImplName = context.Names.GetNameForFunctionImpl(function);
            bool isAnon = string.IsNullOrEmpty(function.Name);
            string functionName = isAnon ? "NULL" : $"\"{function.Name}\"";
            List<PLanguageType> signature = function.Signature.ParameterTypes.ToList();
            Debug.Assert(isAnon && signature.Count <= 1 || !isAnon);
            string payloadType = isAnon && signature.Count == 1
                ? $"&{context.Names.GetNameForType(signature[0])}"
                : "NULL";

            if (!function.IsForeign)
            {
                TraceSourceLine(output, declLocation);
                context.WriteLine(output,
                    $"PRT_VALUE* {functionImplName}(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)");
                context.WriteLine(output, "{");
                WriteFunctionBody(output, function);
                context.WriteLine(output, "}");
                context.WriteLine(output);
            }

            context.WriteLine(output, $"PRT_FUNDECL {declName} =");
            context.WriteLine(output, "{");
            context.WriteLine(output, $"{functionName},"); // name of function in original program, NULL if anon
            context.WriteLine(output, $"&{functionImplName},"); // pointer to implementation
            context.WriteLine(output, $"{payloadType}"); // payload type for anonymous functions
            context.WriteLine(output, "};");
            context.WriteLine(output);
        }

        private string WriteTypeDefinition(TextWriter output, PLanguageType type)
        {
            type = type.Canonicalize();

            string typeGenName = context.Names.GetNameForType(type);

            if (context.WrittenTypes.Contains(type))
            {
                return typeGenName;
            }

            switch (type)
            {
                case DataType boundedType:
                    context.WriteLine(output, $"static PRT_TYPE {typeGenName} = {{ PRT_KIND_ANY, {{ NULL }} }};");
                    break;

                case EnumType _:
                    context.WriteLine(output, $"static PRT_TYPE {typeGenName} = {{ PRT_KIND_INT, {{ NULL }} }};");
                    break;

                case ForeignType foreignType:
                    string foreignTypeName = foreignType.CanonicalRepresentation;

                    context.WriteLine(output, $"extern PRT_UINT64 P_MKDEF_{foreignTypeName}_IMPL(void);");
                    context.WriteLine(output, $"extern PRT_UINT64 P_CLONE_{foreignTypeName}_IMPL(PRT_UINT64);");
                    context.WriteLine(output, $"extern void P_FREE_{foreignTypeName}_IMPL(PRT_UINT64);");
                    context.WriteLine(output, $"extern PRT_UINT32 P_GETHASHCODE_{foreignTypeName}_IMPL(PRT_UINT64);");
                    context.WriteLine(output,
                        $"extern PRT_BOOLEAN P_ISEQUAL_{foreignTypeName}_IMPL(PRT_UINT64, PRT_UINT64);");
                    context.WriteLine(output, $"extern PRT_STRING P_TOSTRING_{foreignTypeName}_IMPL(PRT_UINT64);");

                    string foreignTypeDeclName = context.Names.GetNameForForeignTypeDecl(foreignType);
                    context.WriteLine(output, $"static PRT_FOREIGNTYPEDECL {foreignTypeDeclName} = {{");
                    context.WriteLine(output, "0U,");
                    context.WriteLine(output, $"\"{foreignTypeName}\",");
                    context.WriteLine(output, $"P_MKDEF_{foreignTypeName}_IMPL,");
                    context.WriteLine(output, $"P_CLONE_{foreignTypeName}_IMPL,");
                    context.WriteLine(output, $"P_FREE_{foreignTypeName}_IMPL,");
                    context.WriteLine(output, $"P_GETHASHCODE_{foreignTypeName}_IMPL,");
                    context.WriteLine(output, $"P_ISEQUAL_{foreignTypeName}_IMPL,");
                    context.WriteLine(output, $"P_TOSTRING_{foreignTypeName}_IMPL,");
                    context.WriteLine(output, "};");
                    context.WriteLine(output,
                        $"PRT_TYPE {typeGenName} = {{ PRT_KIND_FOREIGN, {{ .foreignType = &{foreignTypeDeclName} }} }};");
                    break;

                case MapType mapType:
                    string mapKeyTypeName = WriteTypeDefinition(output, mapType.KeyType);
                    string mapValueTypeName = WriteTypeDefinition(output, mapType.ValueType);
                    string mapTypeDeclName = context.Names.GetTemporaryName("MAPTYPE");
                    context.WriteLine(output,
                        $"static PRT_MAPTYPE {mapTypeDeclName} = {{ &{mapKeyTypeName}, &{mapValueTypeName} }};");
                    context.WriteLine(output,
                        $"static PRT_TYPE {typeGenName} = {{ PRT_KIND_MAP, {{ .map = &{mapTypeDeclName} }} }};");
                    break;

                case SetType setType:
                    var setElementTypeName = WriteTypeDefinition(output, setType.ElementType);
                    var setTypeDeclName = context.Names.GetTemporaryName("SETTYPE");
                    context.WriteLine(output,
                        $"static PRT_SETTYPE {setTypeDeclName} = {{ &{setElementTypeName}}};");
                    context.WriteLine(output,
                        $"static PRT_TYPE {typeGenName} = {{ PRT_KIND_SET, {{ .set = &{setTypeDeclName} }} }};");
                    break;

                case NamedTupleType namedTupleType:
                    string ntNamesArrayName = context.Names.GetTemporaryName("NMDTUP_N");
                    string ntTypesArrayName = context.Names.GetTemporaryName("NMDTUP_T");
                    string ntStructName = context.Names.GetTemporaryName("NMDTUP");
                    IEnumerable<string> typeDeclNames = namedTupleType.Types.Select(t => WriteTypeDefinition(output, t));
                    context.WriteLine(
                        output,
                        $"static PRT_STRING {ntNamesArrayName}[] = {{ {string.Join(", ", namedTupleType.Names.Select(name => "\"" + name + "\""))} }};");
                    context.WriteLine(
                        output,
                        $"static PRT_TYPE* {ntTypesArrayName}[] = {{ {string.Join(", ", typeDeclNames.Select(name => "&" + name))} }};");
                    context.WriteLine(
                        output,
                        $"static PRT_NMDTUPTYPE {ntStructName} = {{ {namedTupleType.Types.Count}U, {ntNamesArrayName}, {ntTypesArrayName} }};");
                    context.WriteLine(output,
                        $"static PRT_TYPE {typeGenName} = {{ PRT_KIND_NMDTUP, {{ .nmTuple = &{ntStructName} }} }};");
                    break;

                case PermissionType _:
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

                case PrimitiveType primitiveType when Equals(primitiveType, PrimitiveType.String):
                    context.WriteLine(output, $"static PRT_TYPE {typeGenName} = {{ PRT_KIND_STRING, {{ NULL }} }};");
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
                    string seqElementTypeName = WriteTypeDefinition(output, sequenceType.ElementType);
                    string seqTypeDeclName = context.Names.GetTemporaryName("SEQTYPE");
                    context.WriteLine(output, $"static PRT_SEQTYPE {seqTypeDeclName} = {{ &{seqElementTypeName} }};");
                    context.WriteLine(output,
                        $"static PRT_TYPE {typeGenName} = {{ PRT_KIND_SEQ, {{ .seq = &{seqTypeDeclName} }} }};");
                    break;

                case TupleType tupleType:
                    string tupTypesArrayName = context.Names.GetTemporaryName("TUP_T");
                    string tupStructName = context.Names.GetTemporaryName("TUP");
                    IEnumerable<string> tupTypeDeclNames = tupleType.Types.Select(t => WriteTypeDefinition(output, t));
                    context.WriteLine(
                        output,
                        $"static PRT_TYPE* {tupTypesArrayName}[] = {{ {string.Join(", ", tupTypeDeclNames.Select(n => "&" + n))} }};");
                    context.WriteLine(output,
                        $"static PRT_TUPTYPE {tupStructName} = {{ {tupleType.Types.Count}U, {tupTypesArrayName} }};");
                    context.WriteLine(output,
                        $"static PRT_TYPE {typeGenName} = {{ PRT_KIND_TUPLE, {{ .tuple = &{tupStructName} }} }};");
                    break;

                case TypeDefType _:
                    Debug.Fail("typedefs shouldn't be possible after canonicalization");
                    throw new ArgumentOutOfRangeException(nameof(type),
                        "typedefs shouldn't be possible after canonicalization");
            }

            context.WrittenTypes.Add(type);
            return typeGenName;
        }

        private void WriteProgramDecl(TextWriter output, Scope globalScope)
        {
            // generate event array
            string eventArrayName = context.Names.GetTemporaryName("ALL_EVENTS");
            IList<PEvent> eventsInOrder =
                PrtTranslationUtils.ToOrderedListByPermutation(globalScope.Events, context.GetDeclNumber);
            string eventArrayBody = string.Join(", ", eventsInOrder.Select(ev => "&" + context.Names.GetNameForDecl(ev)));
            eventArrayBody = string.IsNullOrEmpty(eventArrayBody) ? "NULL" : eventArrayBody;
            context.WriteLine(output, $"PRT_EVENTDECL* {eventArrayName}[] = {{ {eventArrayBody} }};");

            // generate machine array
            string machineArrayName = context.Names.GetTemporaryName("ALL_MACHINES");
            string machineArrayBody = string.Join(", ", PrtTranslationUtils
                .ToOrderedListByPermutation(globalScope.Machines, context.GetDeclNumber)
                .Select(ev => "&" + context.Names.GetNameForDecl(ev)));
            machineArrayBody = string.IsNullOrEmpty(machineArrayBody) ? "NULL" : machineArrayBody;
            context.WriteLine(output, $"PRT_MACHINEDECL* {machineArrayName}[] = {{ {machineArrayBody} }};");

            // generate interface array
            string interfaceArrayName = context.Names.GetTemporaryName("ALL_INTERFACES");
            string interfaceArrayBody = string.Join(", ", PrtTranslationUtils
                .ToOrderedListByPermutation(globalScope.Interfaces, context.GetDeclNumber)
                .Select(ev => "&" + context.Names.GetNameForDecl(ev)));
            interfaceArrayBody = string.IsNullOrEmpty(interfaceArrayBody) ? "NULL" : interfaceArrayBody;
            context.WriteLine(output, $"PRT_INTERFACEDECL* {interfaceArrayName}[] = {{ {interfaceArrayBody} }};");

            // generate functions array
            List<Function> allFunctions = globalScope.Functions.Where(f => !f.IsForeign).ToList();
            string funcArrayName = context.Names.GetTemporaryName("ALL_FUNCTIONS");
            string funcArrayBody =
                string.Join(", ", allFunctions.Select(ev => "&" + context.Names.GetNameForDecl(ev)));
            funcArrayBody = string.IsNullOrEmpty(funcArrayBody) ? "NULL" : funcArrayBody;
            context.WriteLine(output, $"PRT_FUNDECL* {funcArrayName}[] = {{ {funcArrayBody} }};");

            // generate foreign types array
            List<ForeignType> foreignTypes = context.WrittenTypes.Where(t => t is ForeignType).Cast<ForeignType>().ToList();
            string foreignTypesArrayName = context.Names.GetTemporaryName("ALL_FOREIGN_TYPES");
            string foreignTypesArrayBody = string.Join(", ",
                foreignTypes
                    .Select(t => $"&{context.Names.GetNameForForeignTypeDecl(t)}"));
            foreignTypesArrayBody = string.IsNullOrEmpty(foreignTypesArrayBody) ? "NULL" : foreignTypesArrayBody;
            context.WriteLine(output,
                $"PRT_FOREIGNTYPEDECL* {foreignTypesArrayName}[] = {{ {foreignTypesArrayBody} }};");

            foreach (Implementation impl in globalScope.Implementations)
            {
                IDictionary<Interface, IDictionary<Interface, Interface>> linkMap = impl.ModExpr.ModuleInfo.LinkMap;

                int[][] trueLinkMap = ResolveLinkMap(globalScope, linkMap);
                string[] mapNames = Enumerable.Repeat("NULL", trueLinkMap.Length).ToArray();
                for (int i = 0; i < trueLinkMap.Length; i++)
                {
                    int[] iMap = trueLinkMap[i];
                    if (iMap != null)
                    {
                        string mapTmpName = context.Names.GetTemporaryName($"{impl.Name}_LME_{i}");
                        mapNames[i] = mapTmpName;
                        context.WriteLine(output, $"int {mapTmpName}[] = {{ {string.Join(",", iMap)} }};");
                    }
                }

                string linkMapName = context.Names.GetTemporaryName($"{impl.Name}_LINKMAP");
                context.WriteLine(output, $"int* {linkMapName}[] = {{ {string.Join(", ", mapNames)} }};");

                IDictionary<Interface, Machine> machineDefMap = impl.ModExpr.ModuleInfo.InterfaceDef;
                string machineDefMapName = context.Names.GetTemporaryName($"{impl.Name}_DEFMAP");
                int[] realMachineDefMap = Enumerable.Repeat(-1, trueLinkMap.Length).ToArray();
                foreach (KeyValuePair<Interface, Machine> linking in machineDefMap)
                {
                    realMachineDefMap[context.GetDeclNumber(linking.Key)] = context.GetDeclNumber(linking.Value);
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

        private int[][] ResolveLinkMap(Scope globalScope,
            IDictionary<Interface, IDictionary<Interface, Interface>> linkMap)
        {
            int nInterfaces = globalScope.Interfaces.Count();
            int[][] maps = new int[nInterfaces][];
            foreach (KeyValuePair<Interface, IDictionary<Interface, Interface>> keyValuePair in linkMap)
            {
                int firstInterfaceIndex = context.GetDeclNumber(keyValuePair.Key);
                Debug.Assert(maps[firstInterfaceIndex] == null);
                maps[firstInterfaceIndex] = Enumerable.Repeat(-1, nInterfaces).ToArray();

                foreach (KeyValuePair<Interface, Interface> finalMapping in keyValuePair.Value)
                {
                    int secondInterfaceIndex = context.GetDeclNumber(finalMapping.Key);
                    int finalInterfaceIndex = context.GetDeclNumber(finalMapping.Value);
                    maps[firstInterfaceIndex][secondInterfaceIndex] = finalInterfaceIndex;
                }
            }

            return maps;
        }

        private string GetReceivesNameOrMkTemp(TextWriter output, Machine machine)
        {
            string eventSetName;
            if (machine.Receives is NamedEventSet mRecvSet)
            {
                eventSetName = context.Names.GetNameForDecl(mRecvSet);
            }
            else
            {
                NamedEventSet machineTempRecvSet = new NamedEventSet(machine.Name + "_RECV", machine.SourceLocation);
                machineTempRecvSet.AddEvents(machine.Receives.Events);
                WriteSourceDecl(output, machineTempRecvSet);
                eventSetName = context.Names.GetNameForDecl(machineTempRecvSet);
            }

            return eventSetName;
        }

        private string GetSendsNameOrMkTemp(TextWriter output, Machine machine)
        {
            string eventSetName;
            if (machine.Sends is NamedEventSet mSendSet)
            {
                eventSetName = context.Names.GetNameForDecl(mSendSet);
            }
            else
            {
                NamedEventSet machineTempSendSet = new NamedEventSet(machine.Name + "_SEND", machine.SourceLocation);
                machineTempSendSet.AddEvents(machine.Sends.Events);
                WriteSourceDecl(output, machineTempSendSet);
                eventSetName = context.Names.GetNameForDecl(machineTempSendSet);
            }

            return eventSetName;
        }

        #endregion Declaration level methods

        #region Function body methods

        private void WriteFunctionBody(TextWriter output, Function function)
        {
            SourceLocation funLocation = context.LocationResolver.GetLocation(function);
            TraceSourceLine(output, funLocation);

            // TODO: figure out how many args are actually necessary based on function calls.
            context.WriteLine(output, $"PRT_VALUE* {FunCallRetValName} = NULL;");
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

            for (int i = 0; i < function.Signature.Parameters.Count; i++)
            {
                Variable argument = function.Signature.Parameters[i];
                string varName = context.Names.GetNameForDecl(argument);
                context.WriteLine(output, $"PRT_VALUE** {varName} = argRefs[{i}];");
            }

            WriteFunctionStatements(output, function, "p_return");
            context.WriteLine(output, $"return {FunResultValName};");
        }

        private void WriteFunctionStatements(TextWriter output, Function function, string returnLabelHint)
        {
            string returnLabel = context.Names.GetReturnLabel(function, returnLabelHint);

            // Write the body into a temporary buffer so that forward declarations can be found and added
            StringWriter bodyWriter = new StringWriter();
            SourceLocation bodyLocation = context.LocationResolver.GetLocation(function.Body);
            TraceSourceLine(bodyWriter, bodyLocation);

            foreach (IPStmt stmt in function.Body.Statements)
            {
                WriteStmt(bodyWriter, function, stmt);
            }

            bodyWriter.WriteLine($"{returnLabel}: ;");
            foreach (Variable localVariable in function.LocalVariables)
            {
                string varName = context.Names.GetNameForDecl(localVariable);
                context.WriteLine(bodyWriter, $"PrtFreeValue({varName}); {varName} = NULL;");
            }

            // Write local variable declarations to the prologue
            foreach (Variable localVariable in function.LocalVariables)
            {
                SourceLocation varLocation = context.LocationResolver.GetLocation(localVariable);
                TraceSourceLine(output, varLocation);

                string varName = context.Names.GetNameForDecl(localVariable);
                if (localVariable.Role.HasFlag(VariableRole.Temp))
                {
                    // temporaries are never read before being written.
                    context.WriteLine(output, $"PRT_VALUE* {varName} = NULL;");
                }
                else
                {
                    // TODO: optimize away PrtMkDefaultValue if liveness shows no usages before assignments.
                    string varTypeName = context.Names.GetNameForType(localVariable.Type);
                    context.WriteLine(output, $"PRT_VALUE* {varName} = PrtMkDefaultValue(&{varTypeName});");
                }
            }

            if (function.LocalVariables.Any())
            {
                context.WriteLine(output);
            }

            // Write gathered literals to the prologue
            context.WriteLine(output,
                $"PRT_VALUE {FunNullStaticName} = {{ PRT_VALUE_KIND_NULL, {{ .ev = PRT_SPECIAL_EVENT_NULL }} }};");

            WriteRegisteredLiterals(output, function);

            output.Write(bodyWriter);
        }

        private void WriteRegisteredLiterals(TextWriter output, Function function)
        {
            foreach (KeyValuePair<int, string> literal in context.GetRegisteredIntLiterals(function))
            {
                context.WriteLine(output,
                    $"PRT_VALUE {literal.Value} = {{ PRT_VALUE_KIND_INT, {{ .nt = {literal.Key} }} }};");
            }

            foreach (KeyValuePair<double, string> literal in context.GetRegisteredFloatLiterals(function))
            {
                context.WriteLine(output,
                    $"PRT_VALUE {literal.Value} = {{ PRT_VALUE_KIND_FLOAT, {{ .ft = {literal.Key} }} }};");
            }

            foreach (KeyValuePair<bool, string> literal in context.GetRegisteredBoolLiterals(function))
            {
                context.WriteLine(
                    output,
                    $"PRT_VALUE {literal.Value} = {{ PRT_VALUE_KIND_BOOL, {{ .bl = {(literal.Key ? "PRT_TRUE" : "PRT_FALSE")} }} }};");
            }

            foreach (KeyValuePair<string, string> literal in context.GetRegisteredStringLiterals(function))
            {
                context.WriteLine(
                    output,
                    $"PRT_VALUE {literal.Value} = {{ PRT_VALUE_KIND_STRING, {{ .str = {literal.Key} }} }};");
            }
        }

        private string GetVariableReference(Function function, IVariableRef variableRef)
        {
            return $"&({GetVariablePointer(function, variableRef.Variable)})";
        }

        private void WriteCleanupCheck(TextWriter output, Function function)
        {
            Function topFun = function;
            while (topFun.ParentFunction != null)
            {
                topFun = topFun.ParentFunction;
            }

            context.WriteLine(output,
                "if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {");
            context.WriteLine(output, $"goto {context.Names.GetReturnLabel(topFun)};");
            context.WriteLine(output, "}");

            context.WriteLine(output, "if (p_this->isHalted == PRT_TRUE) {");
            context.WriteLine(output, $"PrtFreeValue({FunResultValName});");
            context.WriteLine(output, $"{FunResultValName} = NULL;");
            context.WriteLine(output, $"goto {context.Names.GetReturnLabel(topFun)};");
            context.WriteLine(output, "}");
        }

        private void WriteStmt(TextWriter output, Function function, IPStmt stmt)
        {
            SourceLocation stmtLocation = context.LocationResolver.GetLocation(stmt);
            TraceSourceLine(output, stmtLocation);
            switch (stmt)
            {
                case AnnounceStmt _:
                    // TODO: Ankush needs to implement this in Prt
                    break;

                case AddStmt addStmt:
                    context.Write(output, "PrtSetAddEx(");
                    WriteLValue(output, function, addStmt.Variable);
                    context.Write(output, ", ");
                    Debug.Assert(addStmt.Value is IVariableRef);
                    WriteExpr(output, function, addStmt.Value);
                    context.WriteLine(output, ", PRT_FALSE);");
                    var addValueVar = (IVariableRef)addStmt.Value;
                    context.WriteLine(output, $"*({GetVariableReference(function, addValueVar)}) = NULL;");
                    break;

                case AssertStmt assertStmt:
                    context.Write(output, "PrtAssert(PrtPrimGetBool(");
                    WriteExpr(output, function, assertStmt.Assertion);
                    context.WriteLine(output, $"), \"{assertStmt.Message}\");");
                    break;

                case AssignStmt assignStmt:
                    // Lookup lvalue
                    string lvalName = context.Names.GetTemporaryName("LVALUE");
                    context.Write(output, $"PRT_VALUE** {lvalName} = &(");
                    WriteLValue(output, function, assignStmt.Location);
                    context.WriteLine(output, ");");

                    // Free old value
                    context.WriteLine(output, $"PrtFreeValue(*{lvalName});");

                    // Assign new value
                    context.Write(output, $"*{lvalName} = ");
                    WriteExpr(output, function, assignStmt.Value);
                    context.WriteLine(output, ";");

                    // If we called external code, check for cleanup.
                    if (assignStmt.Value is FunCallExpr || assignStmt.Value is CtorExpr)
                    {
                        WriteCleanupCheck(output, function);
                    }

                    break;

                case StringAssignStmt stringAssignStmt:
                    // Lookup lvalue
                    lvalName = context.Names.GetTemporaryName("LVALUE");
                    context.Write(output, $"PRT_VALUE** {lvalName} = &(");
                    WriteLValue(output, function, stringAssignStmt.Location);
                    context.WriteLine(output, ");");

                    // Free old value
                    context.WriteLine(output, $"PrtFreeValue(*{lvalName});");

                    // Assign new value
                    context.Write(output, $"*{lvalName} = ");
                    WriteStringAssignStmt(output, stringAssignStmt, function);
                    break;

                case CompoundStmt compoundStmt:
                    context.WriteLine(output, "{");
                    foreach (IPStmt pStmt in compoundStmt.Statements)
                    {
                        WriteStmt(output, function, pStmt);
                    }

                    context.WriteLine(output, "}");
                    break;

                case CtorStmt ctorStmt:
                    context.Write(
                        output,
                        $"PrtMkInterface(context, {context.GetDeclNumber(ctorStmt.Interface)}, {ctorStmt.Arguments.Count}");
                    foreach (IPExpr pExpr in ctorStmt.Arguments)
                    {
                        Debug.Assert(pExpr is IVariableRef);
                        IVariableRef argVar = (IVariableRef)pExpr;
                        context.Write(output, $", {GetVariableReference(function, argVar)}");
                    }

                    context.WriteLine(output, ");");
                    WriteCleanupCheck(output, function);
                    break;

                case FunCallStmt funCallStmt:
                    string funImplName = context.Names.GetNameForFunctionImpl(funCallStmt.Function);
                    List<ILinearRef> funArgs = funCallStmt.ArgsList.Cast<ILinearRef>().ToList();

                    // Put all the arguments in the args array
                    foreach (var arg in funArgs.Select((arg, i) => new { arg, i }))
                    {
                        context.WriteLine(
                            output, $"{FunCallArgsArrayName}[{arg.i}] = {GetVariableReference(function, arg.arg)};");
                    }

                    // Call the function and immediately free the value
                    context.WriteLine(output, $"PrtFreeValue({funImplName}(context, {FunCallArgsArrayName}));");

                    // Free and set to null all the moved arguments
                    IEnumerable<string> toFree = funArgs.Where(arg => arg.LinearType.Equals(LinearType.Move))
                        .Select(arg => GetVariablePointer(function, arg.Variable));
                    foreach (string argName in toFree)
                    {
                        context.WriteLine(output, $"PrtFreeValue({argName});");
                        context.WriteLine(output, $"{argName} = NULL;");
                    }

                    WriteCleanupCheck(output, function);
                    break;

                case GotoStmt gotoStmt:
                    int destStateIndex = context.GetDeclNumber(gotoStmt.State);
                    context.Write(output, $"PrtGoto(p_this, {destStateIndex}U, ");
                    if (gotoStmt.Payload != null)
                    {
                        Debug.Assert(gotoStmt.Payload is IVariableRef);
                        IVariableRef gotoArg = (IVariableRef)gotoStmt.Payload;
                        context.Write(output, $"1, {GetVariableReference(function, gotoArg)}");
                    }
                    else
                    {
                        context.Write(output, "0");
                    }

                    context.WriteLine(output, ");");
                    break;

                case IfStmt ifStmt:
                    context.Write(output, "if (PrtPrimGetBool(");
                    WriteExpr(output, function, ifStmt.Condition);
                    context.WriteLine(output, "))");
                    WriteStmt(output, function, ifStmt.ThenBranch);
                    if (ifStmt.ElseBranch != null)
                    {
                        context.WriteLine(output, "else");
                        WriteStmt(output, function, ifStmt.ElseBranch);
                    }

                    break;

                case InsertStmt insertStmt:
                    context.Write(output, "PrtSeqInsertEx(");
                    WriteLValue(output, function, insertStmt.Variable);
                    context.Write(output, ", ");
                    WriteExpr(output, function, insertStmt.Index);
                    context.Write(output, ", ");
                    Debug.Assert(insertStmt.Value is IVariableRef);
                    WriteExpr(output, function, insertStmt.Value);
                    context.WriteLine(output, ", PRT_FALSE);");
                    IVariableRef insertValueVar = (IVariableRef)insertStmt.Value;
                    context.WriteLine(output, $"*({GetVariableReference(function, insertValueVar)}) = NULL;");
                    break;

                case MoveAssignStmt moveAssignStmt:
                    context.WriteLine(output, "{");
                    string movedVarName = GetVariablePointer(function, moveAssignStmt.FromVariable);

                    // Get reference to old value
                    string movedLValue = context.Names.GetTemporaryName("LVALUE");
                    context.Write(output, $"PRT_VALUE** {movedLValue} = &(");
                    WriteLValue(output, function, moveAssignStmt.ToLocation);
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
                    context.WriteLine(output, "PrtPop(p_this);");
                    context.WriteLine(output, $"goto {context.Names.GetReturnLabel(function)};");
                    break;

                case PrintStmt printStmt:
                    WritePrintStmt(output, printStmt, function);
                    break;

                case RaiseStmt raiseStmt:
                    context.Write(output, "PrtRaise(p_this, ");
                    WriteExpr(output, function, raiseStmt.PEvent);
                    context.Write(output, $", {raiseStmt.Payload.Count}");
                    foreach (IPExpr pExpr in raiseStmt.Payload)
                    {
                        Debug.Assert(pExpr is IVariableRef);
                        IVariableRef argVar = (IVariableRef)pExpr;
                        context.Write(output, $", {GetVariableReference(function, argVar)}");
                    }

                    context.WriteLine(output, ");");

                    Debug.Assert(raiseStmt.PEvent is IVariableRef);
                    IVariableRef raiseEventVar = (IVariableRef)raiseStmt.PEvent;
                    context.WriteLine(output, $"*({GetVariableReference(function, raiseEventVar)}) = NULL;");

                    Function raiseReturnTarget = function;
                    while (raiseReturnTarget.ParentFunction != null)
                    {
                        raiseReturnTarget = raiseReturnTarget.ParentFunction;
                    }

                    context.WriteLine(output, $"goto {context.Names.GetReturnLabel(raiseReturnTarget)};");
                    break;

                case ReceiveStmt receiveStmt:
                    // context.Job.Output.WriteMessage("Receive is not yet stable!", SeverityKind.Warning);

                    string allowedEventIdsName = context.Names.GetTemporaryName("allowedEventIds");
                    List<int> receiveEventIds = receiveStmt.Cases.Keys.Select(context.GetDeclNumber).ToList();
                    string allowedEventIdsValue = string.Join(", ", receiveEventIds);

                    Variable payloadVariable = new Variable(context.Names.GetTemporaryName("payload"),
                        receiveStmt.SourceLocation, VariableRole.Temp);
                    function.AddLocalVariable(payloadVariable);

                    string payloadName = context.Names.GetNameForDecl(payloadVariable);
                    string eventIdName = context.Names.GetTemporaryName("eventId");

                    // Set up call to PrtReceiveAsync
                    context.WriteLine(output, $"PRT_UINT32 {allowedEventIdsName}[] = {{ {allowedEventIdsValue} }};");
                    context.WriteLine(output, $"PrtFreeValue({payloadName}); {payloadName} = NULL;");
                    context.WriteLine(output,
                        $"PRT_UINT32 {eventIdName} = PrtReceiveAsync({receiveEventIds.Count}U, {allowedEventIdsName}, &{payloadName});");

                    // Might wake up with the cleanup flag set.
                    WriteCleanupCheck(output, function);

                    // Write each case as a switch
                    context.WriteLine(output, $"switch ({eventIdName}) {{");
                    foreach (KeyValuePair<PEvent, Function> receiveCase in receiveStmt.Cases)
                    {
                        PEvent caseEvent = receiveCase.Key;
                        Function caseFunction = receiveCase.Value;
                        context.WriteLine(output, $"case {context.GetDeclNumber(caseEvent)}: {{");

                        Debug.Assert(caseFunction.Signature.Parameters.Count <= 1);

                        if (caseFunction.Signature.Parameters.Any())
                        {
                            string realPayloadName = context.Names.GetNameForDecl(caseFunction.Signature.Parameters[0]);
                            context.WriteLine(output, $"PRT_VALUE** {realPayloadName} = &{payloadName};");
                        }

                        // Write case body into temporary buffer so that we can prepend literals
                        StringWriter caseWriter = new StringWriter();
                        foreach (IPStmt caseStmt in caseFunction.Body.Statements)
                        {
                            WriteStmt(caseWriter, caseFunction, caseStmt);
                        }

                        context.WriteLine(caseWriter, $"{context.Names.GetReturnLabel(caseFunction)}: ;");

                        // Register local variables with containing function
                        function.AddLocalVariables(caseFunction.LocalVariables);

                        // Write the new literals that are found in the case
                        WriteRegisteredLiterals(output, caseFunction);

                        // Paste temporary buffer into output.
                        context.Write(output, caseWriter.ToString());
                        context.WriteLine(output, "} break;");
                    }

                    // Default case -- catch erroneous resumptions
                    context.WriteLine(output, "default: {");
                    context.WriteLine(output, "PrtAssert(PRT_FALSE, \"receive returned unhandled event\");");
                    context.WriteLine(output, "} break;");
                    context.WriteLine(output, "}");

                    // Clean up the payload now since it loses scope in P source.
                    context.WriteLine(output, $"PrtFreeValue({payloadName}); {payloadName} = NULL;");
                    break;

                case RemoveStmt removeStmt:
                    context.Write(output, "PrtRemoveByKey(");
                    WriteLValue(output, function, removeStmt.Variable);
                    context.Write(output, ", ");
                    WriteExpr(output, function, removeStmt.Value);
                    context.WriteLine(output, ");");
                    break;

                case ReturnStmt returnStmt:
                    if (returnStmt.ReturnValue != null)
                    {
                        context.WriteLine(output, $"PrtFreeValue({FunResultValName});");
                        context.Write(output, $"{FunResultValName} = ");
                        WriteExpr(output, function, returnStmt.ReturnValue);
                        context.WriteLine(output, ";");
                    }

                    context.WriteLine(output, $"goto {context.Names.GetReturnLabel(function)};");
                    break;

                case BreakStmt breakStmt:
                    context.WriteLine(output, "break;");
                    break;

                case ContinueStmt continueStmt:
                    context.WriteLine(output, "continue;");
                    break;

                case SendStmt sendStmt:
                    context.Write(output, "PrtSendInternal(context, PrtGetMachine(context->process, ");
                    WriteExpr(output, function, sendStmt.MachineExpr);
                    context.Write(output, "), ");
                    WriteExpr(output, function, sendStmt.Evt);
                    context.Write(output, $", {sendStmt.Arguments.Count}");
                    foreach (IPExpr sendArgExpr in sendStmt.Arguments)
                    {
                        Debug.Assert(sendArgExpr is IVariableRef);
                        IVariableRef argVar = (IVariableRef)sendArgExpr;
                        context.Write(output, $", {GetVariableReference(function, argVar)}");
                    }

                    context.WriteLine(output, ");");

                    Debug.Assert(sendStmt.Evt is IVariableRef);
                    IVariableRef sendEventVar = (IVariableRef)sendStmt.Evt;
                    context.WriteLine(output, $"*({GetVariableReference(function, sendEventVar)}) = NULL;");

                    // Send can immediately schedule work on another machine. It does this via a recursive call to PrtScheduleWork,
                    // which is almost certainly the Wrong Thing To Do (tm).
                    // TODO: fix the underlying problem and remove this check.
                    WriteCleanupCheck(output, function);
                    break;

                case SwapAssignStmt swapAssignStmt:
                    context.WriteLine(output, "{");
                    string lvName = context.Names.GetTemporaryName("LVALUE");
                    string tmpName = context.Names.GetTemporaryName("SWAP");

                    // Can only swap local variables, so this is safe. Otherwise would have to call a second WriteLValue
                    string swappedName = GetVariablePointer(function, swapAssignStmt.OldLocation);

                    // Save l-value
                    context.Write(output, $"PRT_VALUE** {lvName} = &(");
                    WriteLValue(output, function, swapAssignStmt.NewLocation);
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
                    WriteExpr(output, function, whileStmt.Condition);
                    context.WriteLine(output, "))");
                    WriteStmt(output, function, whileStmt.Body);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(stmt));
            }

            context.WriteLine(output);
        }

        private void WriteStringAssignStmt(TextWriter output, StringAssignStmt stringAssignStmt, Function function)
        {
            // format is {str0, n1, str1, n2, ..., nK, strK}
            object[] assignBaseParts = PrtTranslationUtils.ParsePrintMessage(stringAssignStmt.BaseString);
   
            // Build parameter pack
            int k = (assignBaseParts.Length - 1) / 2;
            context.Write(output, "PrtMkStringValue(PrtFormatString(\"");
            context.Write(output, (string)assignBaseParts[0]);
            context.Write(output, "\", ");
            context.Write(output, stringAssignStmt.Args.Count.ToString());
            foreach (IPExpr printArg in stringAssignStmt.Args)
            {
                context.Write(output, ", ");
                WriteExpr(output, function, printArg);
            }

            context.Write(output, ", ");
            context.Write(output, k.ToString());
            for (int i = 0; i < k; i++)
            {
                int n = (int)assignBaseParts[1 + 2 * i];
                string s = (string)assignBaseParts[1 + 2 * i + 1];
                context.Write(output, ", ");
                context.Write(output, n.ToString());
                context.Write(output, ", \"");
                context.Write(output, s);
                context.Write(output, "\"");
            }

            context.WriteLine(output, "));");
        }

        private void WritePrintStmt(TextWriter output, PrintStmt printStmt, Function function)
        {
            // format is {str0, n1, str1, n2, ..., nK, strK}
            object[] printMessageParts = PrtTranslationUtils.ParsePrintMessage(printStmt.Message);

            // Optimize for simple case.
            if (printMessageParts.Length == 1)
            {
                context.Write(output, "PrtPrintf(\"");
                context.Write(output, (string)printMessageParts[0]);
                context.WriteLine(output, "\");");
                return;
            }

            // Otherwise build full parameter pack...
            int k = (printMessageParts.Length - 1) / 2;
            context.Write(output, "PrtFormatPrintf(\"");
            context.Write(output, (string)printMessageParts[0]);
            context.Write(output, "\", ");
            context.Write(output, printStmt.Args.Count.ToString());
            foreach (IPExpr printArg in printStmt.Args)
            {
                context.Write(output, ", ");
                WriteExpr(output, function, printArg);
            }

            context.Write(output, ", ");
            context.Write(output, k.ToString());
            for (int i = 0; i < k; i++)
            {
                int n = (int)printMessageParts[1 + 2 * i];
                string s = (string)printMessageParts[1 + 2 * i + 1];
                context.Write(output, ", ");
                context.Write(output, n.ToString());
                context.Write(output, ", \"");
                context.Write(output, s);
                context.Write(output, "\"");
            }

            context.WriteLine(output, ");");
        }

        private void WriteLValue(TextWriter output, Function function, IPExpr expr)
        {
            switch (expr)
            {
                case MapAccessExpr mapAccessExpr:
                    // TODO: optimize out key copy when possible
                    context.Write(output, "*(PrtMapGetLValue(");
                    WriteLValue(output, function, mapAccessExpr.MapExpr);
                    context.Write(output, ", ");
                    WriteExpr(output, function, mapAccessExpr.IndexExpr);
                    context.Write(output, $", PRT_TRUE, &{context.Names.GetNameForType(mapAccessExpr.MapExpr.Type)}))");
                    break;

                case NamedTupleAccessExpr namedTupleAccessExpr:
                    context.Write(output, "*(PrtTupleGetLValue(");
                    WriteLValue(output, function, namedTupleAccessExpr.SubExpr);
                    context.Write(output, $", {namedTupleAccessExpr.Entry.FieldNo}))");
                    break;

                case SeqAccessExpr seqAccessExpr:
                    context.Write(output, "*(PrtSeqGetLValue(");
                    WriteLValue(output, function, seqAccessExpr.SeqExpr);
                    context.Write(output, ", ");
                    WriteExpr(output, function, seqAccessExpr.IndexExpr);
                    context.Write(output, "))");
                    break;

                case TupleAccessExpr tupleAccessExpr:
                    context.Write(output, "*(PrtTupleGetLValue(");
                    WriteLValue(output, function, tupleAccessExpr.SubExpr);
                    context.Write(output, $", {tupleAccessExpr.FieldNo}))");
                    break;

                case VariableAccessExpr variableAccessExpr:
                    WriteVariableAccess(output, function, variableAccessExpr.Variable);
                    break;

                default:
                    throw context.Handler.InternalError(expr.SourceLocation,
                        new ArgumentOutOfRangeException(nameof(expr)));
            }
        }

        private void WriteExpr(TextWriter output, Function function, IPExpr expr)
        {
            switch (expr)
            {
                case CloneExpr cloneExpr:
                    context.Write(output, "PrtCloneValue(");
                    WriteExpr(output, function, cloneExpr.Term);
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
                        WriteExpr(output, function, binOpLhs);
                        context.Write(output, ", ");
                        WriteExpr(output, function, binOpRhs);
                        context.Write(output, "))");
                    }
                    // String Concatenation replaces + with strcat
                    else if (PrimitiveType.String.IsSameTypeAs(binOpLhs.Type) &&
                        PrimitiveType.String.IsSameTypeAs(binOpRhs.Type) &&
                        binOpType == BinOpType.Add)
                    {
                        context.Write(output, $"PrtStringConcat(");
                        WriteExpr(output, function, binOpLhs);
                        context.Write(output, ",");
                        WriteExpr(output, function, binOpRhs);
                        context.Write(output, ")");
                    }
                    else
                    {
                        (string binOpGetter, string _) = GetTypeStructureFuns(binOpLhs.Type);
                        (string _, string binOpBuilder) = GetTypeStructureFuns(binOpExpr.Type);

                        context.Write(output, $"{binOpBuilder}(");

                        context.Write(output, $"{binOpGetter}(");
                        WriteExpr(output, function, binOpLhs);
                        context.Write(output, ")");

                        context.Write(output, $" {BinOpToStr(binOpType)} ");

                        context.Write(output, $"{binOpGetter}(");
                        WriteExpr(output, function, binOpRhs);
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
                    WriteExpr(output, function, castExpr.SubExpr);
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

                        case PermissionType permissionType:
                            coerceCtor = "PrtMkMachineValue";
                            break;

                        default:
                            throw context.Handler.InternalError(coerceExpr.SourceLocation,
                                new ArgumentOutOfRangeException(nameof(coerceExpr.NewType)));
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

                        case PrimitiveType primitiveType when PrimitiveType.Machine.IsSameTypeAs(primitiveType):
                        case PermissionType _:
                            coerceUnpack = "PrtPrimGetMachine";
                            break;

                        default:
                            throw context.Handler.InternalError(coerceExpr.SourceLocation,
                                new ArgumentOutOfRangeException(nameof(coerceExpr.SubExpr.Type)));
                    }

                    context.Write(output, $"{coerceCtor}({coerceUnpack}(");
                    WriteExpr(output, function, coerceExpr.SubExpr);
                    context.Write(output, "))");
                    break;

                case ContainsExpr containsKeyExpr:
                    var isMap = PLanguageType.TypeIsOfKind(containsKeyExpr.Collection.Type, TypeKind.Map);
                    var isSeq = PLanguageType.TypeIsOfKind(containsKeyExpr.Collection.Type, TypeKind.Sequence);
                    var isSet = PLanguageType.TypeIsOfKind(containsKeyExpr.Collection.Type, TypeKind.Set);
                    if (isMap)
                    {
                        context.Write(output, "PrtMkBoolValue(PrtMapExists(");
                    }
                    else if (isSeq)
                    {
                        context.Write(output, "PrtMkBoolValue(PrtSeqExists(");
                    }
                    else if (isSet) {
                        context.Write(output, "PrtMkBoolValue(PrtSetExists(");
                    } else {
                        throw new InvalidOperationException("Unsupported operation for non-map or sequence type");
                    }
                    WriteExpr(output, function, containsKeyExpr.Collection);
                    context.Write(output, ", ");
                    WriteExpr(output, function, containsKeyExpr.Item);
                    context.Write(output, "));");
                    break;

                case CtorExpr ctorExpr:
                    context.Write(
                        output,
                        $"PrtCloneValue(PrtMkInterface(context, {context.GetDeclNumber(ctorExpr.Interface)}, {ctorExpr.Arguments.Count}");
                    foreach (IPExpr pExpr in ctorExpr.Arguments)
                    {
                        Debug.Assert(pExpr is IVariableRef);
                        IVariableRef argVar = (IVariableRef)pExpr;
                        context.Write(output, $", {GetVariableReference(function, argVar)}");
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
                    string floatLiteralName = context.RegisterLiteral(function, floatLiteralExpr.Value);
                    context.Write(output, $"(&{floatLiteralName})");
                    break;

                case FunCallExpr funCallExpr:
                    string funImplName = context.Names.GetNameForFunctionImpl(funCallExpr.Function);
                    List<ILinearRef> funArgs = funCallExpr.Arguments.Cast<ILinearRef>().ToList();
                    IEnumerable<string> argSetup = funArgs.Select((arg, i) =>
                        $"({FunCallArgsArrayName}[{i}] = {GetVariableReference(function, arg)})");
                    string[] funCall = new[] { $"({FunCallRetValName} = {funImplName}(context, {FunCallArgsArrayName}))" };
                    IEnumerable<string> argsFree = funArgs.Where(arg => arg.LinearType.Equals(LinearType.Move))
                        .Select(arg => GetVariablePointer(function, arg.Variable))
                        .Select(varName => $"(PrtFreeValue({varName}), {varName} = NULL)");
                    string[] resRetrieve = new[] { $"({FunCallRetValName})" };
                    string fullCall = string.Join(", ", argSetup.Concat(funCall).Concat(argsFree).Concat(resRetrieve));
                    context.Write(output, $"({fullCall})");
                    break;

                case IntLiteralExpr intLiteralExpr:
                    string intLiteralName = context.RegisterLiteral(function, intLiteralExpr.Value);
                    context.Write(output, $"(&{intLiteralName})");
                    break;

                case KeysExpr keysExpr:
                    context.Write(output, "PrtMapGetKeys(");
                    WriteExpr(output, function, keysExpr.Expr);
                    context.Write(output, ")");
                    break;

                case LinearAccessRefExpr linearAccessRefExpr:
                    // TODO: what's special about linear refs here?
                    WriteVariableAccess(output, function, linearAccessRefExpr.Variable);
                    break;

                case MapAccessExpr mapAccessExpr:
                    context.Write(output, "PrtMapGet(");
                    WriteExpr(output, function, mapAccessExpr.MapExpr);
                    context.Write(output, ", ");
                    WriteExpr(output, function, mapAccessExpr.IndexExpr);
                    context.Write(output, ")");
                    break;

                case NamedTupleAccessExpr namedTupleAccessExpr:
                    context.Write(output, "PrtTupleGet(");
                    WriteExpr(output, function, namedTupleAccessExpr.SubExpr);
                    context.Write(output, $", {namedTupleAccessExpr.Entry.FieldNo})");
                    break;

                case NamedTupleExpr namedTupleExpr:
                    IReadOnlyList<IVariableRef> ntArgs = (IReadOnlyList<IVariableRef>)namedTupleExpr.TupleFields;
                    string ntTypeName = context.Names.GetNameForType(namedTupleExpr.Type);
                    string namedTupleBody =
                        string.Join(", ", ntArgs.Select(v => GetVariableReference(function, v)));
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
                    WriteExpr(output, function, seqAccessExpr.SeqExpr);
                    context.Write(output, ", ");
                    WriteExpr(output, function, seqAccessExpr.IndexExpr);
                    context.Write(output, ")");
                    break;

                case StringLiteralExpr stringLiteralExpr:
                    string stringLiteralName = context.RegisterLiteral(function, stringLiteralExpr.Value);
                    context.Write(output, $"(&{stringLiteralName})");
                    break;

                case SizeofExpr sizeofExpr:
                    string sizeofFun = PLanguageType.TypeIsOfKind(sizeofExpr.Expr.Type, TypeKind.Map)
                        ? "PrtMapSizeOf"
                        : PLanguageType.TypeIsOfKind(sizeofExpr.Expr.Type, TypeKind.Sequence)
                        ? "PrtSeqSizeOf"
                        : "PrtSetSizeOf";
                    context.Write(output, $"PrtMkIntValue({sizeofFun}(");
                    WriteExpr(output, function, sizeofExpr.Expr);
                    context.Write(output, "))");
                    break;

                case ThisRefExpr _:
                    context.Write(output, "(p_this->id)");
                    break;

                case TupleAccessExpr tupleAccessExpr:
                    context.Write(output, "PrtTupleGet(");
                    WriteExpr(output, function, tupleAccessExpr.SubExpr);
                    context.Write(output, $", {tupleAccessExpr.FieldNo})");
                    break;

                case UnaryOpExpr unaryOpExpr:
                    (string unOpGetter, string unOpBuilder) = GetTypeStructureFuns(unaryOpExpr.Type);
                    context.Write(output, $"{unOpBuilder}(");

                    context.Write(output, UnOpToStr(unaryOpExpr.Operation));
                    context.Write(output, $"{unOpGetter}(");
                    WriteExpr(output, function, unaryOpExpr.SubExpr);
                    context.Write(output, ")");

                    context.Write(output, ")");
                    break;

                case UnnamedTupleExpr unnamedTupleExpr:
                    IReadOnlyList<IVariableRef> utArgs = (IReadOnlyList<IVariableRef>)unnamedTupleExpr.TupleFields;
                    string utTypeName = context.Names.GetNameForType(unnamedTupleExpr.Type);
                    string tupleBody =
                        string.Join(", ", utArgs.Select(v => GetVariableReference(function, v)));
                    context.Write(output, $"(PrtMkTuple(&{utTypeName}, {tupleBody}))");
                    break;

                case ValuesExpr valuesExpr:
                    context.Write(output, "PrtMapGetValues(");
                    WriteExpr(output, function, valuesExpr.Expr);
                    context.Write(output, ")");
                    break;

                case VariableAccessExpr variableAccessExpr:
                    WriteVariableAccess(output, function, variableAccessExpr.Variable);
                    break;
            }
        }

        private string GetVariablePointer(Function function, Variable variable)
        {
            if (variable.Role.HasFlag(VariableRole.Param))
            {
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

            throw context.Handler.InternalError(variable.SourceLocation,
                new ArgumentOutOfRangeException(nameof(variable)));
        }

        private void WriteVariableAccess(TextWriter output, Function function, Variable variable)
        {
            context.Write(output, GetVariablePointer(function, variable));
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
                    throw new ArgumentOutOfRangeException(nameof(operation));
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
            else if (type.IsSameTypeAs(PrimitiveType.String))
            {
                binOpGetter = "PrtPrimGetString";
                binOpBuilder = "PrtMkStringValue";
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
                    throw new ArgumentOutOfRangeException(nameof(binOpType));
            }
        }

        #endregion Function body methods

        #region Header methods

        private void WriteHeaderPrologue(TextWriter output)
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

        private void WriteExternDeclaration(TextWriter output, IPDecl decl)
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
                    // TODO: what to do here?
                    context.WriteLine(output, $"// DECL(NamedEventSet, {decl.Name}) => {declName}");
                    break;

                case NamedModule namedModule:
                    // TODO: what to do here?
                    context.WriteLine(output, $"// DECL(NamedModule, {decl.Name}) => {declName}");
                    break;

                case PEnum pEnum:
                    string enumBody = string.Join(", ",
                        pEnum.Values.Select(val => $"{context.Names.GetNameForDecl(val)} = {val.Value}"));
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
                    // TODO: what to do here?
                    context.WriteLine(output, $"// DECL(RefinementTest, {decl.Name}) => {declName}");
                    break;

                case SafetyTest safetyTest:
                    // TODO: what to do here?
                    context.WriteLine(output, $"// DECL(SafetyTest, {decl.Name}) => {declName}");
                    break;

                case TypeDef _:
                    context.WriteLine(output, $"extern PRT_TYPE* {declName};");
                    break;

                case Variable variable:
                    throw context.Handler.InternalError(variable.SourceLocation,
                        new ArgumentOutOfRangeException(nameof(decl), "can't have global P variables"));
                case State state:
                    // TODO: what to do here?
                    context.WriteLine(output, $"// DECL(State, {decl.Name}) => {declName}");
                    break;

                case StateGroup stateGroup:
                    // TODO: what to do here?
                    context.WriteLine(output, $"// DECL(StateGroup, {decl.Name}) => {declName}");
                    break;
            }
        }

        private void WriteHeaderEpilogue(TextWriter output)
        {
            string includeGuardMacro = $"P_{Regex.Replace(context.ProjectName.ToUpperInvariant(), @"\s+", "")}_H_";
            context.WriteLine(output);
            context.WriteLine(output, "#ifdef __cplusplus");
            context.WriteLine(output, "}");
            context.WriteLine(output, "#endif");
            context.WriteLine(output);
            context.WriteLine(output, $"#endif // {includeGuardMacro}");
        }

        #endregion Header methods
    }
}