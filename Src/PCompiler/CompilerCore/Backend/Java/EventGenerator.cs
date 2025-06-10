using System;
using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;

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

        private string JsonObjectGet(string objName, string field, PLanguageType type = null)
        {
            if (type != null)
            {
                if (type is EnumType enumType)
                {
                    return $"(({Constants.TypesNamespaceName}.{enumType.EnumDecl.Name}) (payload.get(\"{field}\")))";
                    // return $"{Constants.TypesNamespaceName}.{enumType.EnumDecl.Name}.from(" + JsonObjectGet(objName, field, PrimitiveType.Int) + ".intValue())";
                }
                if (type.Equals(PrimitiveType.Any))
                {
                    return $"payload.get(\"{field}\")";
                }
                else
                {
                    var javaType = Types.JavaTypeFor(type).ReferenceTypeName;
                    if (!javaType.Equals("Object"))
                    {
                        return $"({objName}.get{javaType}(\"{field}\"))";
                    }
                }
            }
            return $"({objName}.get(\"{field}\"))";
        }

        internal void WriteEventDecl(Event e, bool pinfer = false)
        {
            var eventName = Names.GetNameForDecl(e);
            var argType = Types.JavaTypeFor(e.PayloadType);

            var payloadType = argType.TypeName;
            var payloadRefType = argType.ReferenceTypeName;

            WriteLine($"public static class {eventName} extends {Constants.EventsClass()}<{payloadRefType}> implements Serializable {{");

            var hasPayload = !(argType is TypeManager.JType.JVoid);
            if (pinfer)
            {
                WriteLine("private long index;");
                WriteLine("private String sender;");
                WriteLine("private String target;");
                WriteLine("public long index() { return this.index; }");
                WriteLine("public String sender() { return this.sender; }");
                WriteLine("public String target() { return this.target; }");
            }
            if (hasPayload)
            {
                if (pinfer)
                {
                    WriteLine($"public {eventName}(long i, String sender, String target, {payloadType} p) {{ this.index = i; this.sender = sender; this.target = target; this.payload = p; }}");
                }
                else
                {
                    WriteLine($"public {eventName}({payloadType} p) {{ this.payload = p; }}");
                }
                WriteLine($"private {payloadType} payload; ");
                WriteLine($"public {payloadRefType} getPayload() {{ return payload; }}");
            }
            else
            {
                if (pinfer)
                {
                    WriteLine($"public {eventName}(int i, String sender, String target) {{ this.index = i; this.sender = sender; this.target = target; }}");
                }
                else
                {
                    WriteLine($"public {eventName}() {{ }}");
                }
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

            WriteLine($"}} // {eventName}");
        }
    }
}
