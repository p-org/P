using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.Backend.Java
{
    internal class EventGenerator : JavaSourceGenerator {

        internal EventGenerator(ICompilerConfiguration job, string filename) : base(job, filename)
        {
        }

        /// <summary>
        /// Generates Java code for a given compilation job's used events.
        ///
        /// We only emit code for events that are actually used in Monitors (i.e. they
        /// appear in at least one Monitor's `observes` set.)
        /// </summary>
        protected override void GenerateCodeImpl()
        {
            WriteLine($"public class {Constants.EventNamespaceName} {{");
            foreach (var e in monitoredEvents(GlobalScope.Machines))
            {
                WriteEventDecl(e);
                WriteLine();
            }
            WriteLine("}");
        }


        private IEnumerable<Event> monitoredEvents(IEnumerable<Machine> machines)
        {
            var events = new HashSet<Event>();

            foreach (var m in machines.Where(m => m.IsSpec))
            {
                foreach (var e in m.Observes.Events)
                {
                    events.Add(e);
                }
            }

            return events;
        }

        private void WriteEventDecl(Event e)
        {
            var eventName = Names.GetNameForDecl(e);
            var argType = Types.JavaTypeFor(e.PayloadType);

            var payloadType = argType.TypeName;
            var payloadRefType = argType.ReferenceTypeName;

            WriteLine($"public static class {eventName} extends {Constants.EventsClass}<{payloadRefType}> implements Serializable {{");

            var hasPayload = !(argType is TypeManager.JType.JVoid);
            if (hasPayload)
            {
                WriteLine($"public {eventName}({payloadType} p) {{ this.payload = p; }}");
                WriteLine($"private {payloadType} payload; ");
                WriteLine($"public {payloadRefType} getPayload() {{ return payload; }}");
            }
            else
            {
                WriteLine($"public {eventName}() {{ }}");
                WriteLine($"public Void getPayload() {{ return null; }}");
            }

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

            WriteLine();
            Write("public int hashCode() {");
            if (hasPayload)
            {
                WriteLine(" return payload.hashCode(); }");
            }
            else
            {
                WriteLine(" return 0; }");
            }

            WriteLine();
            Write($"public boolean deepEquals({eventName} other) {{");
            if (hasPayload)
            {
                if (argType.IsPrimitive)
                {
                    WriteLine(" return this.payload == other.payload; }");
                }
                else
                {
                    WriteLine($" return {Constants.PrtDeepEqualsMethodName}(this.payload, other.payload); }}");
                }
            }
            else
            {
                WriteLine("return true; }");
            }

            WriteLine();
            WriteLine("public boolean equals(Object other) {");
            Write("return this.getClass() == other.getClass()");
            if (hasPayload)
            {
                Write($" && this.payload.deepEquals((({eventName})other).payload)");
            }
            WriteLine(";");
            WriteLine("}");

            WriteLine($"}} // {eventName}");
        }
    }
}