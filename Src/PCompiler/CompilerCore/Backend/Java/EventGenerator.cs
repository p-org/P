using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST.Declarations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Plang.Compiler.Backend.Java
{
    internal class EventGenerator : ICodeGenerator {

        private CompilationContext _context;
        private CompiledFile _source;
        private Scope _globalScope;

        public IEnumerable<PEvent> monitoredEvents(IEnumerable<Machine> machines)
        {
            HashSet<PEvent> events = new HashSet<PEvent>();

            foreach (Machine m in machines.Where(m => m.IsSpec))
            {
                foreach (PEvent e in m.Observes.Events)
                {
                    events.Add(e);
                }
            }

            return events;
        }


        /// <summary>
        /// Generates Java code for a given compilation job's used events.
        ///
        /// We only emit code for events that are actually used in Monitors (i.e. they
        /// appear in at least one Monitor's `observes` set.)
        /// </summary>
        public IEnumerable<CompiledFile> GenerateCode(ICompilationJob job, Scope scope)
        {
            _context = new CompilationContext(job);
            _source = new CompiledFile(Constants.EventDefnFileName);
            _globalScope = scope;

            WriteLine("package PGenerated; ");

            WriteLine(Constants.DoNotEditWarning);
            WriteLine();

            WriteLine();

            WriteLine($"public class {Constants.EventNamespaceName} {{");
            foreach (var e in monitoredEvents(scope.Machines))
            {
                WriteEventDecl(e);
                WriteLine();
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
            Write("public String toString() {");
            if (hasPayload)
            {
                WriteLine($" return \"{eventName}[\" + payload + \"]\"; }}");
            }
            else
            {
                WriteLine($" return \"{eventName}\"; }}");
            }

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
