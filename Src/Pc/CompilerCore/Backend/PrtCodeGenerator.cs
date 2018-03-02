using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Pc.TypeChecker;
using Microsoft.Pc.TypeChecker.AST;

namespace Microsoft.Pc.Backend
{
    public class PrtCodeGenerator : ICodeGenerator
    {
        public IReadOnlyList<CompiledFile> GenerateCode(ITranslationErrorHandler handler, ICompilerOutput log, string projectName,
                                                        Scope globalScope)
        {
            // TODO: route naming information here, or find a better place to do it.
            var cHeader = new CompiledFile($"{projectName}.h");
            var cSource = new CompiledFile($"{projectName}.c");

            WriteGenericHeader(projectName, cHeader.Stream);

            // Manages job-specific state so that jobs can be parallelized
            var context = new CompilationContext();

            foreach (IPDecl decl in globalScope.AllDecls)
            {
                WriteForwardDeclaration(context, cHeader.Stream, decl);
            }

            WriteGenericFooter(projectName, cHeader.Stream);

            return new List<CompiledFile> {cHeader, cSource};
        }

        private static void WriteGenericFooter(string projectName, TextWriter output)
        {
            string includeGuardMacro = $"P_{Regex.Replace(projectName.ToUpperInvariant(), @"\s+", "")}_H_";
            output.WriteLine();
            output.WriteLine("#ifdef __cplusplus");
            output.WriteLine("}");
            output.WriteLine("#endif");
            output.WriteLine();
            output.WriteLine($"#endif // {includeGuardMacro}");
        }

        private static void WriteGenericHeader(string projectName, TextWriter output)
        {
            string includeGuardMacro = $"P_{Regex.Replace(projectName.ToUpperInvariant(), @"\s+", "")}_H_";
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

        private void WriteForwardDeclaration(CompilationContext context, TextWriter output, IPDecl decl)
        {
            string declName = context.Names.GetNameForNode(decl);
            output.WriteLine($"DECL({decl.GetType().Name}, {decl.Name}) => {declName}");
        }

        private class CompilationContext
        {
            public NameManager Names { get; } = new NameManager();
            public int IndentationLevel { get; set; } = 0;
        }
    }
}
