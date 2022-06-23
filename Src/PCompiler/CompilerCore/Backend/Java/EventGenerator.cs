using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST.Declarations;
using System;
using System.Collections.Generic;

namespace Plang.Compiler.Backend.Java
{
    internal class EventGenerator : ICodeGenerator {

        private CompilationContext _context;
        private CompiledFile _source;
        private Scope _globalScope;

        /// <summary>
        /// Generates Java code for a given compilation job's machine and monitor definitions.
        ///
        /// Currently, we should be able to use nested classes to put everything we need in a single
        /// Java file, in a manner similar to how the C# extractor uses namespaces.
        /// </summary>
        /// <param name="job"></param>
        /// <param name="scope"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public IEnumerable<CompiledFile> GenerateCode(ICompilationJob job, Scope scope)
        {
            _context = new CompilationContext(job);
            _source = new CompiledFile(Constants.EventDefnFileName);
            _globalScope = scope;

            WriteLine("package PGenerated; ");

            WriteLine(Constants.DoNotEditWarning);
            WriteLine();

            WriteLine();

            WriteLine($"public class Events {Constants.EventNamespaceName} {{");
            foreach (var e in _globalScope.Events)
            {
                WriteEventDecl(e);
            }
            WriteLine("}");
            return new List<CompiledFile> { _source };
        }

        private void WriteEventDecl(PEvent e)
        {
            string eventName = _context.Names.GetNameForDecl(e);

            TypeManager.JType argType = _context.Types.JavaTypeFor(e.PayloadType);
            bool hasPayload = !(argType is TypeManager.JType.JVoid);

            WriteLine($"public static class {eventName} extends {Constants.PEventsClass}<{argType.ReferenceTypeName}> {{");

            if (hasPayload)
            {
                WriteLine($"public {eventName}({argType.TypeName} p) {{ this.payload = p; }}");
            }
            else
            {
                WriteLine($"public {eventName}() {{ }}");
            }

            WriteLine($"private {argType.ReferenceTypeName} payload; ");
            WriteLine($"public {argType.ReferenceTypeName} getPayload() {{ return payload; }}");
            WriteLine();

            WriteLine("@Override");
            WriteLine("public String toString() {");
            if (hasPayload)
            {
                WriteLine($"return \"{eventName}[\" + payload + \"]\";");
            }
            else
            {
                WriteLine($"return \"{eventName}\";");
            }
            WriteLine("} // toString()");
            WriteLine();

            WriteLine($"}} // {eventName}");
        }

        private void WriteLine(string s = "")
        {
            _context.WriteLine(_source.Stream, s);
        }

        private void Write(string s)
        {
            _context.Write(_source.Stream, s);
        }
    }
}
