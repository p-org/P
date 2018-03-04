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
            // Manages job-specific state so that jobs can be parallelized
            var context = new CompilationContext(projectName);

            // Create header and source files.
            var cHeader = new CompiledFile($"{projectName}.h");
            var cSource = new CompiledFile($"{projectName}.c");

            // Write the header file
            WriteGenericHeader(context, cHeader.Stream);

            foreach (IPDecl decl in globalScope.AllDecls)
            {
                WriteForwardDeclaration(context, cHeader.Stream, decl);
            }

            WriteGenericFooter(context, cHeader.Stream);

            // TODO: write the source

            // All done! Return the compiled files.
            return new List<CompiledFile> {cHeader, cSource};
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
            string declName = context.Names.GetNameForNode(decl);
            output.WriteLine($"DECL({decl.GetType().Name}, {decl.Name}) => {declName}");
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

        private class CompilationContext
        {
            public string ProjectName { get; }
            public NameManager Names { get; }
            public int IndentationLevel { get; set; } = 0;

            public CompilationContext(string projectName)
            {
                ProjectName = projectName;
                Names = new NameManager($"P_{projectName.ToUpperInvariant()}_");
            }
        }
    }
}
