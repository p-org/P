using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Pc.TypeChecker;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.Statements;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.Backend.PSharp
{
    public class PSharpCodeGenerator : ICodeGenerator
    {
        public IReadOnlyList<CompiledFile> GenerateCode(ITranslationErrorHandler handler, ICompilerOutput log, string projectName,
                                                        Scope globalScope)
        {
            log.WriteMessage("P# code generation in progress.", SeverityKind.Warning);
            var context = new CompilationContext(handler, log, projectName);
            CompiledFile csharpSource = GenerateSource(context, globalScope);
            return new List<CompiledFile>() { csharpSource };
        }

        private CompiledFile GenerateSource(CompilationContext context, Scope globalScope)
        {
            var source = new CompiledFile(context.FileName);

            WriteSourcePrologue(context, source.Stream);

            foreach (var decl in globalScope.AllDecls)
            {
                WriteDecl(context, source.Stream, decl);
            }

            // TODO: generate tuple type classes.

            WriteSourceEpilogue(context, source.Stream);

            return source;
        }

        private void WriteSourcePrologue(CompilationContext context, StringWriter stream)
        {
            context.WriteLine(stream, "using Microsoft.PSharp;");
            context.WriteLine(stream, "using System;");
            context.WriteLine(stream, "using System.Collections.Generic;");
            context.WriteLine(stream, "using System.IO;");
            context.WriteLine(stream);
            context.WriteLine(stream, $"namespace {context.ProjectName}");
            context.WriteLine(stream, "{");
            context.WriteLine(stream, $"public static partial class {context.GlobalFunctionClassName} {{}}");
        }

        private void WriteSourceEpilogue(CompilationContext context, StringWriter stream)
        {
            context.WriteLine(stream, "}");
        }

        private void WriteDecl(CompilationContext context, StringWriter stream, IPDecl decl)
        {
            string declName = context.Names.GetNameForNode(decl);
            switch (decl)
            {
                case Function function:
                    context.WriteLine(stream, $"public static partial class {context.GlobalFunctionClassName}");
                    context.WriteLine(stream, "{");
                    WriteFunction(context, stream, function);
                    context.WriteLine(stream, "}");
                    break;
                case PEvent pEvent:
                    break;
                case Machine machine:
                    break;
                case PEnum pEnum:
                    context.WriteLine(stream, $"public enum {declName}");
                    context.WriteLine(stream, "{");
                    foreach(var enumElem in pEnum.Values)
                    {
                        context.WriteLine(stream, $"{declName} = {enumElem.Value},");
                    }
                    context.WriteLine(stream, "}");
                    break;
                default:
                    context.WriteLine(stream, $"// TODO: {decl.GetType().Name} {declName}");
                    break;
            }
        }

        private void WriteFunction(CompilationContext context, StringWriter stream, Function function)
        {
            bool isStatic = function.Owner == null;
            string staticKeyword = isStatic ? "static " : "";
            string returnType = GetCSharpType(context, function.Signature.ReturnType);
            string functionName = context.Names.GetNameForNode(function);
            var functionParameters = string.Join(", ", function.Signature.Parameters.Select(param => $"{GetCSharpType(context, param.Type)} {context.Names.GetNameForNode(param)}"));
            context.WriteLine(stream, $"public {staticKeyword}{returnType} {functionName}({functionParameters})");
            WriteStmt(context, stream, function.Body);
        }

        private void WriteStmt(CompilationContext context, StringWriter stream, IPStmt stmt)
        {
            switch(stmt)
            {
                case CompoundStmt compoundStmt:
                    context.WriteLine(stream, "{");
                    foreach(var subStmt in compoundStmt.Statements)
                    {
                        WriteStmt(context, stream, subStmt);
                    }
                    context.WriteLine(stream, "}");
                    break;
                default:
                    context.WriteLine(stream, $"//TODO : {stmt.GetType().Name}");
                    break;
            }
        }

        private string GetCSharpType(CompilationContext context, PLanguageType returnType)
        {
            // TODO: keep track of tuple types generated inside context.
            throw new NotImplementedException();
        }
    }
}