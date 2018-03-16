using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Pc.TypeChecker;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.States;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.Backend
{
    public class PrtCodeGenerator : ICodeGenerator
    {
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
            cSource.Stream.WriteLine("// Type universe for program:");
            foreach (PLanguageType type in context.UsedTypes)
            {
                WriteTypeDefinition(context, type, cSource.Stream);
            }

            cSource.Stream.WriteLine();

            cSource.Stream.WriteLine("// Function implementation prototypes:");
            foreach (Function function in AllMethods(globalScope))
            {
                string functionName = context.Names.GetNameForFunctionImpl(function);
                cSource.Stream.WriteLine($"PRT_VALUE* {functionName}(PRT_MACHINEINST *context);");
            }

            cSource.Stream.WriteLine();

            // Append all the function bodies and declaration structs (which have forward declarations in the header)
            cSource.Stream.GetStringBuilder().Append(bodyWriter);

            // All done! Return the compiled files.
            return new List<CompiledFile> {cHeader, cSource};
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

        private void WriteSourceDecl(CompilationContext context, IPDecl decl, TextWriter bodyWriter)
        {
            switch (decl)
            {
                case EnumElem enumElem:
                    return;
                case PEnum pEnum:
                    return;
                case Function function:
                    NamedTupleType functionLocalsType = function.LocalVariables.Any()
                        ? new NamedTupleType(function.LocalVariables.Select(v => new NamedTupleEntry {Name = v.Name, Type = v.Type})
                                                     .ToList())
                        : null;
                    string localsTypeName = functionLocalsType == null ? "NULL" : context.Names.GetNameForType(functionLocalsType);
                    bodyWriter.WriteLine($"PRT_FUNDECL {GetPrtNameForDecl(context, function)} =");
                    bodyWriter.WriteLine("{");
                    bodyWriter.WriteLine($"  \"{function.Name}\","); // name of function in original program
                    bodyWriter.WriteLine($"  &{context.Names.GetNameForFunctionImpl(function)},"); // pointer to implementation
                    bodyWriter.WriteLine($"  {function.Signature.Parameters.Count}U,"); // number of parameters
                    bodyWriter.WriteLine(
                        $"  {function.LocalVariables.Count()}U,"); // number of local variables TODO: count variables inside receive / remove this field
                    bodyWriter.WriteLine("  0U,"); // number of local variables in enclosing scopes
                    bodyWriter.WriteLine("  NULL,"); // payload type for anonymous functions: always NULL.
                    bodyWriter.WriteLine(
                        $"  {localsTypeName},"); // type of local variables tuple (not including nested scopes)
                    bodyWriter.WriteLine("  0U,"); // number of receive statements in body: always 0
                    bodyWriter.WriteLine("  NULL,"); // array of receive decls in body: always NULL.
                    bodyWriter.WriteLine("  0U,"); // number of annotations
                    bodyWriter.WriteLine("  NULL"); // array of annotations: always NULL.
                    bodyWriter.WriteLine("};");
                    break;
                case Implementation implementation:
                    return;
                case Interface @interface:
                    return;
                case Machine machine:
                    break;
                case NamedEventSet namedEventSet:
                    break;
                case NamedModule namedModule:
                    return;
                case PEvent pEvent when !pEvent.IsBuiltIn:
                    bodyWriter.WriteLine($"PRT_EVENTDECL {GetPrtNameForDecl(context, pEvent)} = ");
                    bodyWriter.WriteLine("{");
                    bodyWriter.WriteLine("  { PRT_VALUE_KIND_EVENT, 0U },");
                    bodyWriter.WriteLine($"  \"{pEvent.Name}\",");
                    long eventBound = Math.Min(pEvent.Assert == -1 ? uint.MaxValue : (uint) pEvent.Assert,
                                               pEvent.Assume == -1 ? uint.MaxValue : (uint) pEvent.Assume);
                    bodyWriter.WriteLine($"  {eventBound}U,");
                    bodyWriter.WriteLine($"  &{context.Names.GetNameForType(pEvent.PayloadType)}");
                    bodyWriter.WriteLine("  0U,");
                    bodyWriter.WriteLine("  NULL");
                    bodyWriter.WriteLine("};");
                    break;
                case PEvent pEvent when pEvent.IsBuiltIn:
                    return;
                case RefinementTest refinementTest:
                    return;
                case SafetyTest safetyTest:
                    return;
                case TypeDef typeDef:
                    return;
                case Variable variable:
                    return;
                case State state:
                    break;
                case StateGroup stateGroup:
                    break;
            }

            bodyWriter.WriteLine();
        }

        private string WriteTypeDefinition(CompilationContext context, PLanguageType type, TextWriter writer)
        {
            string typeGenName = context.Names.GetNameForType(type);

            if (context.WrittenTypes.Contains(type))
            {
                return typeGenName;
            }

            switch (type.Canonicalize())
            {
                case BoundedType boundedType:
                    writer.WriteLine($"// TODO: implement types like {boundedType.CanonicalRepresentation}");
                    break;
                case EnumType _:
                    writer.WriteLine($"static PRT_TYPE {typeGenName} = {{ PRT_KIND_INT, {{ NULL }} }};");
                    break;
                case ForeignType foreignType:
                    writer.WriteLine($"// TODO: implement types like {foreignType.CanonicalRepresentation}");
                    break;
                case MapType mapType:
                    string mapKeyTypeName = WriteTypeDefinition(context, mapType.KeyType, writer);
                    string mapValueTypeName = WriteTypeDefinition(context, mapType.ValueType, writer);
                    string mapTypeDeclName = context.Names.GetTemporaryName("MAPTYPE");
                    writer.WriteLine($"static PRT_MAPTYPE {mapTypeDeclName} = {{ &{mapKeyTypeName}, &{mapValueTypeName} }};");
                    writer.WriteLine($"static PRT_TYPE {typeGenName} = {{ PRT_KIND_MAP, {{ &{mapTypeDeclName} }} }};");
                    break;
                case NamedTupleType namedTupleType:
                    string ntNamesArrayName = context.Names.GetTemporaryName("NMDTUP_N");
                    string ntTypesArrayName = context.Names.GetTemporaryName("NMDTUP_T");
                    string ntStructName = context.Names.GetTemporaryName("NMDTUP");
                    var typeDeclNames = namedTupleType.Types.Select(t => WriteTypeDefinition(context, t, writer));
                    writer.WriteLine(
                        $"static PRT_STRING {ntNamesArrayName}[] = {{ {string.Join(", ", namedTupleType.Names.Select(name => "\"" + name + "\""))} }};");
                    writer.WriteLine(
                        $"static PRT_TYPE* {ntTypesArrayName}[] = {{ {string.Join(", ", typeDeclNames.Select(name => "&" + name))} }};");
                    writer.WriteLine(
                        $"static PRT_NMDTUPTYPE {ntStructName} = {{ {namedTupleType.Types.Count}, {ntNamesArrayName}, {ntTypesArrayName} }};");
                    writer.WriteLine($"static PRT_TYPE {typeGenName} = {{ PRT_KIND_NMDTUP, {{ &{ntStructName} }} }};");
                    break;
                case PermissionType permissionType:
                    writer.WriteLine($"// TODO: implement types like {permissionType.CanonicalRepresentation}");
                    break;
                case PrimitiveType primitiveType when primitiveType == PrimitiveType.Null:
                    writer.WriteLine($"static PRT_TYPE {typeGenName} = {{ PRT_KIND_NULL, {{ NULL }} }};");
                    break;
                case PrimitiveType primitiveType when primitiveType == PrimitiveType.Int:
                    writer.WriteLine($"static PRT_TYPE {typeGenName} = {{ PRT_KIND_INT, {{ NULL }} }};");
                    break;
                case PrimitiveType primitiveType when primitiveType == PrimitiveType.Float:
                    writer.WriteLine($"static PRT_TYPE {typeGenName} = {{ PRT_KIND_FLOAT, {{ NULL }} }};");
                    break;
                case PrimitiveType primitiveType:
                    writer.WriteLine($"// TODO: implement types like {primitiveType.CanonicalRepresentation}");
                    break;
                case SequenceType sequenceType:
                    string seqElementTypeName = WriteTypeDefinition(context, sequenceType.ElementType, writer);
                    string seqTypeDeclName = context.Names.GetTemporaryName("SEQTYPE");
                    writer.WriteLine($"static PRT_SEQTYPE {seqTypeDeclName} = {{ &{seqElementTypeName} }};");
                    writer.WriteLine($"static PRT_TYPE {typeGenName} = {{ PRT_KIND_SEQ, {{ &{seqTypeDeclName} }} }};");
                    break;
                case TupleType tupleType:
                    writer.WriteLine($"// TODO: implement types like {tupleType.CanonicalRepresentation}");
                    break;
                case TypeDefType typeDefType:
                    throw new ArgumentException("typedefs shouldn't be possible after canonicalization", nameof(type));
            }

            context.WrittenTypes.Add(type);
            return typeGenName;
        }

        private void WriteSourcePrologue(CompilationContext context, TextWriter cSourceStream)
        {
            cSourceStream.WriteLine($"#include \"{context.HeaderFileName}\"");
            cSourceStream.WriteLine();
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
                        return "<<prt:null-event>>";
                    }

                    if (pEvent.IsHaltEvent)
                    {
                        return "<<prt:halt-event>>";
                    }

                    break;
                case State state:
                    // TODO: merge names of state groups.
                    break;
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
            public CompilationContext(string projectName)
            {
                ProjectName = projectName;
                HeaderFileName = $"{projectName}.h";
                SourceFileName = $"{projectName}.c";
                Names = new NameManager($"P_{projectName.ToUpperInvariant()}_");
            }

            public string ProjectName { get; }
            public string HeaderFileName { get; }
            public string SourceFileName { get; }
            public NameManager Names { get; }
            public IEnumerable<PLanguageType> UsedTypes => Names.UsedTypes;
            public HashSet<PLanguageType> WrittenTypes { get; } = new HashSet<PLanguageType>();
            public int IndentationLevel { get; set; } = 0;
        }

        #region Header writing routines

        private static void WriteGenericHeader(CompilationContext context, TextWriter output)
        {
            string includeGuardMacro = $"P_{Regex.Replace(context.ProjectName.ToUpperInvariant(), @"\s+", "")}_H_";
            output.WriteLine("#pragma once");
            output.WriteLine($"#ifndef {includeGuardMacro}");
            output.WriteLine($"#define {includeGuardMacro}");
            output.WriteLine("#include \"Prt.h\"");
            output.WriteLine();
            output.WriteLine("#ifdef __cplusplus");
            output.WriteLine("extern \"C\" {");
            output.WriteLine("#endif");
            output.WriteLine();
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
                    output.WriteLine($"extern PRT_FUNDECL {declName};");
                    break;
                case Implementation implementation:
                    break;
                case Interface _:
                    output.WriteLine($"extern PRT_UINT32 {declName};");
                    break;
                case Machine _:
                    output.WriteLine($"extern PRT_MACHINEDECL {declName};");
                    break;
                case NamedEventSet namedEventSet:
                    output.WriteLine($"// DECL(NamedEventSet, {decl.Name}) => {declName}");
                    break;
                case NamedModule namedModule:
                    output.WriteLine($"// DECL(NamedModule, {decl.Name}) => {declName}");
                    break;
                case PEnum pEnum:
                    output.WriteLine(
                        $"typedef enum {declName} {{ {string.Join(", ", pEnum.Values.Select(val => $"{GetPrtNameForDecl(context, val)} = {val.Value}"))} }} {declName};");
                    output.WriteLine();
                    break;
                case PEvent pEvent:
                    if (!pEvent.IsBuiltIn)
                    {
                        output.WriteLine($"extern PRT_EVENTDECL {declName};");
                    }

                    break;
                case RefinementTest refinementTest:
                    output.WriteLine($"// DECL(RefinementTest, {decl.Name}) => {declName}");
                    break;
                case SafetyTest safetyTest:
                    output.WriteLine($"// DECL(SafetyTest, {decl.Name}) => {declName}");
                    break;
                case TypeDef typeDef:
                    output.WriteLine($"// no need to generate typedef {typeDef.Name} = {typeDef.Type.OriginalRepresentation}");
                    break;
                case Variable variable:
                    throw new ArgumentException("can't have global P variables", nameof(decl));
                case State state:
                    output.WriteLine($"// DECL(State, {decl.Name}) => {declName}");
                    break;
                case StateGroup stateGroup:
                    output.WriteLine($"// DECL(StateGroup, {decl.Name}) => {declName}");
                    break;
            }
        }

        private static void WriteGenericFooter(CompilationContext context, TextWriter output)
        {
            string includeGuardMacro = $"P_{Regex.Replace(context.ProjectName.ToUpperInvariant(), @"\s+", "")}_H_";
            output.WriteLine();
            output.WriteLine("#ifdef __cplusplus");
            output.WriteLine("}");
            output.WriteLine("#endif");
            output.WriteLine();
            output.WriteLine($"#endif // {includeGuardMacro}");
        }

        #endregion
    }
}
