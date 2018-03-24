using System;
using System.Collections.Generic;
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
            }

            context.WriteLine(cSource.Stream);

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

        private void WriteSourceDecl(CompilationContext context, IPDecl decl, TextWriter output)
        {
            switch (decl)
            {
                case EnumElem enumElem:
                    return;
                case PEnum pEnum:
                    return;
                case Function function:
                    var functionImplName = context.Names.GetNameForFunctionImpl(function);
                    context.WriteLine(output, $"PRT_FUNDECL {GetPrtNameForDecl(context, function)} =");
                    context.WriteLine(output, "{");
                    context.WriteLine(output, $"\"{function.Name}\","); // name of function in original program
                    context.WriteLine(output, $"&{functionImplName},"); // pointer to implementation
                    context.WriteLine(output, "NULL,"); // payload type for anonymous functions: always NULL.
                    context.WriteLine(output, "};");
                    context.WriteLine(output);
                    context.WriteLine(output, $"PRT_VALUE* {functionImplName}(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)");
                    context.WriteLine(output, "{");
                    WriteFunctionBody(context, function, output);
                    context.WriteLine(output, "}");
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
                case PEvent pEvent when pEvent.IsBuiltIn:
                    return;
                case PEvent pEvent when !pEvent.IsBuiltIn:
                    long eventBound = Math.Min(pEvent.Assert == -1 ? uint.MaxValue : (uint) pEvent.Assert,
                                               pEvent.Assume == -1 ? uint.MaxValue : (uint) pEvent.Assume);

                    context.WriteLine(output, $"PRT_EVENTDECL {GetPrtNameForDecl(context, pEvent)} = ");
                    context.WriteLine(output, "{");
                    context.WriteLine(output, "{ PRT_VALUE_KIND_EVENT, 0U },");
                    context.WriteLine(output, $"\"{pEvent.Name}\",");
                    context.WriteLine(output, $"{eventBound}U,");
                    context.WriteLine(output, $"&{context.Names.GetNameForType(pEvent.PayloadType)}");
                    context.WriteLine(output, "0U,");
                    context.WriteLine(output, "NULL");
                    context.WriteLine(output, "};");
                    break;
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

            context.WriteLine(output);
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
                context.WriteLine(output, $"PRT_VALUE* {varName} = PrtMkDefaultValue({varTypeName});");
            }

            if (function.Signature.Parameters.Any() || function.LocalVariables.Any())
            {
                context.WriteLine(output);
            }

            if (function.Body is CompoundStmt stmts)
            {
                foreach (IPStmt stmt in stmts.Statements)
                {
                    WriteStmt(context, stmt, output);
                }
            }
            else
            {
                WriteStmt(context, function.Body, output);
            }
        }

        private void WriteStmt(CompilationContext context, IPStmt stmt, TextWriter output)
        {
            switch (stmt)
            {
                case AnnounceStmt announceStmt:
                    break;
                case AssertStmt assertStmt:
                    break;
                case AssignStmt assignStmt:
                    break;
                case CompoundStmt compoundStmt:
                    context.WriteLine(output, "{");
                    foreach (IPStmt pStmt in compoundStmt.Statements)
                    {
                        WriteStmt(context, pStmt, output);
                    }
                    context.WriteLine(output, "}");
                    break;
                case CtorStmt ctorStmt:
                    break;
                case FunCallStmt funCallStmt:
                    break;
                case GotoStmt gotoStmt:
                    break;
                case IfStmt ifStmt:
                    break;
                case InsertStmt insertStmt:
                    break;
                case MoveAssignStmt moveAssignStmt:
                    break;
                case NoStmt noStmt:
                    break;
                case PopStmt popStmt:
                    break;
                case PrintStmt printStmt:
                    break;
                case RaiseStmt raiseStmt:
                    break;
                case ReceiveStmt receiveStmt:
                    break;
                case RemoveStmt removeStmt:
                    break;
                case ReturnStmt returnStmt:
                    context.Write(output, "return ");
                    WriteExpr(context, returnStmt.ReturnValue, output);
                    context.WriteLine(output, ";");
                    break;
                case SendStmt sendStmt:
                    break;
                case SwapAssignStmt swapAssignStmt:
                    break;
                case WhileStmt whileStmt:
                    break;
            }
        }

        private void WriteExpr(CompilationContext context, IPExpr expr, TextWriter output)
        {
            switch (expr)
            {
                case CloneExpr cloneExpr:
                    break;
                case BinOpExpr binOpExpr:
                    break;
                case BoolLiteralExpr boolLiteralExpr:
                    break;
                case CastExpr castExpr:
                    break;
                case CoerceExpr coerceExpr:
                    break;
                case ContainsKeyExpr containsKeyExpr:
                    break;
                case CtorExpr ctorExpr:
                    break;
                case DefaultExpr defaultExpr:
                    break;
                case EnumElemRefExpr enumElemRefExpr:
                    context.Write(output, GetPrtNameForDecl(context, enumElemRefExpr.EnumElem));
                    break;
                case EventRefExpr eventRefExpr:
                    break;
                case FairNondetExpr fairNondetExpr:
                    break;
                case FloatLiteralExpr floatLiteralExpr:
                    break;
                case FunCallExpr funCallExpr:
                    break;
                case IntLiteralExpr intLiteralExpr:
                    context.Write(output, $"PrtMkIntValue({intLiteralExpr.Value})");
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
                case NondetExpr nondetExpr:
                    break;
                case NullLiteralExpr nullLiteralExpr:
                    break;
                case SeqAccessExpr seqAccessExpr:
                    break;
                case SizeofExpr sizeofExpr:
                    break;
                case ThisRefExpr thisRefExpr:
                    break;
                case TupleAccessExpr tupleAccessExpr:
                    break;
                case UnaryOpExpr unaryOpExpr:
                    break;
                case UnnamedTupleExpr unnamedTupleExpr:
                    break;
                case ValuesExpr valuesExpr:
                    break;
                case VariableAccessExpr variableAccessExpr:
                    break;
            }
        }

        private string WriteTypeDefinition(CompilationContext context, PLanguageType type, TextWriter output)
        {
            string typeGenName = context.Names.GetNameForType(type);

            if (context.WrittenTypes.Contains(type))
            {
                return typeGenName;
            }

            switch (type.Canonicalize())
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
                    context.WriteLine(output, $"static PRT_TYPE {typeGenName} = {{ PRT_KIND_MAP, {{ &{mapTypeDeclName} }} }};");
                    break;
                case NamedTupleType namedTupleType:
                    string ntNamesArrayName = context.Names.GetTemporaryName("NMDTUP_N");
                    string ntTypesArrayName = context.Names.GetTemporaryName("NMDTUP_T");
                    string ntStructName = context.Names.GetTemporaryName("NMDTUP");
                    var typeDeclNames = namedTupleType.Types.Select(t => WriteTypeDefinition(context, t, output));
                    context.WriteLine(
                        output, $"static PRT_STRING {ntNamesArrayName}[] = {{ {string.Join(", ", namedTupleType.Names.Select(name => "\"" + name + "\""))} }};");
                    context.WriteLine(
                        output, $"static PRT_TYPE* {ntTypesArrayName}[] = {{ {string.Join(", ", typeDeclNames.Select(name => "&" + name))} }};");
                    context.WriteLine(
                        output, $"static PRT_NMDTUPTYPE {ntStructName} = {{ {namedTupleType.Types.Count}, {ntNamesArrayName}, {ntTypesArrayName} }};");
                    context.WriteLine(output, $"static PRT_TYPE {typeGenName} = {{ PRT_KIND_NMDTUP, {{ &{ntStructName} }} }};");
                    break;
                case PermissionType permissionType:
                    context.WriteLine(output, $"// TODO: implement types like {permissionType.CanonicalRepresentation}");
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
                case PrimitiveType primitiveType:
                    context.WriteLine(output, $"// TODO: implement types like {primitiveType.CanonicalRepresentation}");
                    break;
                case SequenceType sequenceType:
                    string seqElementTypeName = WriteTypeDefinition(context, sequenceType.ElementType, output);
                    string seqTypeDeclName = context.Names.GetTemporaryName("SEQTYPE");
                    context.WriteLine(output, $"static PRT_SEQTYPE {seqTypeDeclName} = {{ &{seqElementTypeName} }};");
                    context.WriteLine(output, $"static PRT_TYPE {typeGenName} = {{ PRT_KIND_SEQ, {{ &{seqTypeDeclName} }} }};");
                    break;
                case TupleType tupleType:
                    context.WriteLine(output, $"// TODO: implement types like {tupleType.CanonicalRepresentation}");
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
            public int IndentationLevel { get; set; }

            private bool lineHasBeenIndented = false;
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
                case Implementation implementation:
                    break;
                case Interface _:
                    context.WriteLine(output, $"extern PRT_UINT32 {declName};");
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
                        output, $"typedef enum {declName} {{ {string.Join(", ", pEnum.Values.Select(val => $"{GetPrtNameForDecl(context, val)} = {val.Value}"))} }} {declName};");
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
                    context.WriteLine(output, $"// no need to generate typedef {typeDef.Name} = {typeDef.Type.OriginalRepresentation}");
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
